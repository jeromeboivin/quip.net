<#
.SYNOPSIS
This script downloads all Quip documents in a specified root folder and its subfolders in HTML format.

.DESCRIPTION
The downloaded documents are stored in a local folder on the machine where the script is run.
The script uses the Quip API V2 to retrieve document information and content.

.PARAMETER RootFolderId
The Quip root folder ID from which to start downloading documents.

.PARAMETER LocalFolderPath
The local folder path where the downloaded documents will be stored.

.PARAMETER Overwrite
Overwrite existing files (optional, default is false)

.PARAMETER RecentMonths
Only download documents that were created or modified within the last N months. 
If not specified, all documents will be downloaded.

.PARAMETER ProgressFilePath
Path to the progress file (optional, defaults to quip_download_progress.json in LocalFolderPath)

.PARAMETER SkipProcessedDays
Number of days to consider folders as recently processed and skip them (default is 7 days).
Set to 0 to never skip based on time (always reprocess all folders).
This helps minimize API calls by avoiding reprocessing of recently completed folders.

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments"

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments" -RecentMonths 3

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments" -SkipProcessedDays 1

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments" -SkipProcessedDays 0

.NOTES
Authentication:
To use this API, you need to define a user environment variable name "QuipApiKey".
Please go to https://quip.com/dev/token to generate your access token.
More information on how to use the Quip Powershell API can be found at https://github.com/jeromeboivin/quip.net

Make sure to import the Quip module before running this script:
Import-Module <path to quipps.dll>

Version 2.0 - Updated to use Quip API V2 for better performance and additional features
Version 2.1 - Added leaf folder optimization and configurable skip timing
#>

param(
    # Defines the Quip root folder ID
    [Parameter(Mandatory=$true)]
    [string]$RootFolderId,

    # Defines the local folder path to store downloaded documents
    [Parameter(Mandatory=$true)]
    [string]$LocalFolderPath,

    [Parameter(Mandatory=$false)]
    # Overwrite existing files (optional, default is false)
    [switch]$Overwrite = $false,

    [Parameter(Mandatory=$false)]
    # Only download documents created or modified within the last N months
    [int]$RecentMonths,

    [Parameter(Mandatory=$false)]
    # Path to the progress file (optional, defaults to progress.json in LocalFolderPath)
    [string]$ProgressFilePath,

    [Parameter(Mandatory=$false)]
    # Number of days to consider folders as recently processed (0 = never skip based on time)
    [int]$SkipProcessedDays = 7
)

# Initialize progress tracking
if (-not $ProgressFilePath) {
    $ProgressFilePath = Join-Path -Path $LocalFolderPath -ChildPath "quip_download_progress.json"
}

# Progress tracking structure
$script:ProgressData = @{
    LastRun = (Get-Date).ToString('o')
    RecentMonthsFilter = $RecentMonths
    ProcessedThreads = @{}  # ThreadId -> @{ IsRecent = $true/$false; LastChecked = DateTime; Downloaded = $true/$false }
    ProcessedFolders = @{}  # FolderId -> @{ LastProcessed = DateTime; ChildCount = int; IsLeaf = $true/$false; FullyProcessed = $true/$false; SubfolderCount = int }
}

# Load existing progress if file exists
function Load-Progress {
    if (Test-Path $ProgressFilePath) {
        try {
            $existingProgress = Get-Content $ProgressFilePath -Raw | ConvertFrom-Json
            
            # Convert hashtables back from PSCustomObject
            if ($existingProgress.ProcessedThreads) {
                $script:ProgressData.ProcessedThreads = @{}
                $existingProgress.ProcessedThreads.PSObject.Properties | ForEach-Object {
                    $script:ProgressData.ProcessedThreads[$_.Name] = @{
                        IsRecent = $_.Value.IsRecent
                        LastChecked = [DateTime]$_.Value.LastChecked
                        Downloaded = $_.Value.Downloaded
                    }
                }
            }
            
            if ($existingProgress.ProcessedFolders) {
                $script:ProgressData.ProcessedFolders = @{}
                $existingProgress.ProcessedFolders.PSObject.Properties | ForEach-Object {
                    $folderInfo = @{
                        LastProcessed = [DateTime]$_.Value.LastProcessed
                        ChildCount = $_.Value.ChildCount
                    }
                    
                    # Handle new properties that might not exist in older progress files
                    if ($_.Value.PSObject.Properties.Name -contains 'IsLeaf') {
                        $folderInfo.IsLeaf = $_.Value.IsLeaf
                    }
                    if ($_.Value.PSObject.Properties.Name -contains 'FullyProcessed') {
                        $folderInfo.FullyProcessed = $_.Value.FullyProcessed
                    }
                    if ($_.Value.PSObject.Properties.Name -contains 'SubfolderCount') {
                        $folderInfo.SubfolderCount = $_.Value.SubfolderCount
                    }
                    
                    $script:ProgressData.ProcessedFolders[$_.Name] = $folderInfo
                }
            }
            
            # Update filter info
            $script:ProgressData.RecentMonthsFilter = $RecentMonths
            
            Write-Host "Loaded existing progress from $ProgressFilePath"
            Write-Host "Previous run processed $($script:ProgressData.ProcessedThreads.Count) threads and $($script:ProgressData.ProcessedFolders.Count) folders"
            
            # Show leaf folder statistics
            $leafFolders = ($script:ProgressData.ProcessedFolders.Values | Where-Object { $_.IsLeaf -eq $true }).Count
            $fullyProcessedFolders = ($script:ProgressData.ProcessedFolders.Values | Where-Object { $_.FullyProcessed -eq $true }).Count
            if ($leafFolders -gt 0 -or $fullyProcessedFolders -gt 0) {
                Write-Host "Found $leafFolders leaf folders and $fullyProcessedFolders fully processed folders from previous run"
            }
        }
        catch {
            Write-Warning "Could not load existing progress file: $($_.Exception.Message). Starting fresh."

            # Initialize progress data if loading fails
            $script:ProgressData = @{
                LastRun = (Get-Date).ToString('o')
                RecentMonthsFilter = $RecentMonths
                ProcessedThreads = @{}  # ThreadId -> @{ IsRecent = $true/$false; LastChecked = DateTime; Downloaded = $true/$false }
                ProcessedFolders = @{}  # FolderId -> @{ LastProcessed = DateTime; ChildCount = int; IsLeaf = $true/$false; FullyProcessed = $true/$false; SubfolderCount = int }
            }
        }
    }
}

# Save progress to JSON file
function Save-Progress {
    try {
        # Ensure directory exists
        $progressDir = Split-Path $ProgressFilePath -Parent
        if (-not (Test-Path $progressDir)) {
            New-Item -ItemType Directory -Force -Path $progressDir | Out-Null
        }
        
        $script:ProgressData.LastRun = (Get-Date).ToString('o')
        $progressJson = $script:ProgressData | ConvertTo-Json -Depth 5
        Set-Content -Path $ProgressFilePath -Value $progressJson -Encoding UTF8
        Write-Host "Progress saved to $ProgressFilePath"
    }
    catch {
        Write-Warning "Could not save progress: $($_.Exception.Message)"
    }
}

# Check if we should skip a thread based on progress data
function Should-SkipThread {
    param (
        [string]$ThreadId,
        [DateTime]$CutoffDate
    )
    
    # If no recent filter is applied, don't skip based on recency
    if ($CutoffDate -eq [DateTime]::MinValue) {
        return $false
    }
    
    # Check if we have cached information about this thread
    if ($script:ProgressData.ProcessedThreads.ContainsKey($ThreadId)) {
        $threadInfo = $script:ProgressData.ProcessedThreads[$ThreadId]
        
        # If we checked this thread recently and it wasn't recent, skip it
        # Use the configurable SkipProcessedDays parameter
        if ($SkipProcessedDays -gt 0) {
            $lastChecked = $threadInfo.LastChecked
            $daysSinceCheck = ((Get-Date) - $lastChecked).TotalDays
            
            if ($daysSinceCheck -lt $SkipProcessedDays -and -not $threadInfo.IsRecent) {
                Write-Host "Skipping thread $ThreadId (cached as not recent, checked $([math]::Round($daysSinceCheck, 1)) days ago)"
                return $true
            }
        }
    }
    
    return $false
}

# Check if we should skip a folder based on progress data
function Should-SkipFolder {
    param (
        [string]$FolderId,
        [DateTime]$CutoffDate
    )
    
    # If SkipProcessedDays is 0, don't skip based on time
    if ($SkipProcessedDays -eq 0) {
        return $false
    }
    
    # Check if we have cached information about this folder
    if ($script:ProgressData.ProcessedFolders.ContainsKey($FolderId)) {
        $folderInfo = $script:ProgressData.ProcessedFolders[$FolderId]
        
        # Skip if it's a leaf folder that was fully processed recently
        if ($folderInfo.IsLeaf -eq $true -and $folderInfo.FullyProcessed -eq $true) {
            $lastProcessed = $folderInfo.LastProcessed
            $daysSinceProcessed = ((Get-Date) - $lastProcessed).TotalDays
            
            if ($daysSinceProcessed -lt $SkipProcessedDays) {
                Write-Host "Skipping leaf folder $FolderId (fully processed $([math]::Round($daysSinceProcessed, 1)) days ago)"
                return $true
            }
        }
        
        # Skip if it's any folder that was fully processed recently and has no recent date filter
        if ($folderInfo.FullyProcessed -eq $true -and $CutoffDate -eq [DateTime]::MinValue) {
            $lastProcessed = $folderInfo.LastProcessed
            $daysSinceProcessed = ((Get-Date) - $lastProcessed).TotalDays
            
            if ($daysSinceProcessed -lt $SkipProcessedDays) {
                Write-Host "Skipping fully processed folder $FolderId (processed $([math]::Round($daysSinceProcessed, 1)) days ago, no recent filter)"
                return $true
            }
        }
    }
    
    return $false
}

# Update progress data for a thread
function Update-ThreadProgress {
    param (
        [string]$ThreadId,
        [bool]$IsRecent,
        [bool]$Downloaded = $false
    )
    
    $script:ProgressData.ProcessedThreads[$ThreadId] = @{
        IsRecent = $IsRecent
        LastChecked = (Get-Date)
        Downloaded = $Downloaded
    }
}

# Update progress data for a folder
function Update-FolderProgress {
    param (
        [string]$FolderId,
        [int]$ChildCount,
        [int]$SubfolderCount,
        [bool]$IsLeaf,
        [bool]$FullyProcessed = $false
    )
    
    if (-not $script:ProgressData.ProcessedFolders.ContainsKey($FolderId)) {
        $script:ProgressData.ProcessedFolders[$FolderId] = @{
            LastProcessed = (Get-Date)
            ChildCount = $ChildCount
            SubfolderCount = $SubfolderCount
            IsLeaf = $IsLeaf
            FullyProcessed = $FullyProcessed
        }
    }
    else {
        $script:ProgressData.ProcessedFolders[$FolderId].LastProcessed = (Get-Date)
        $script:ProgressData.ProcessedFolders[$FolderId].ChildCount = $ChildCount
        $script:ProgressData.ProcessedFolders[$FolderId].SubfolderCount = $SubfolderCount
        $script:ProgressData.ProcessedFolders[$FolderId].IsLeaf = $IsLeaf
        $script:ProgressData.ProcessedFolders[$FolderId].FullyProcessed = $FullyProcessed
    }
}

# Function to check if a thread was created or modified recently
function Test-IsRecentThread {
    param (
        [string]$ThreadId,
        [DateTime]$CutoffDate
    )

    # Check if we should skip this thread based on cached data
    if (Should-SkipThread -ThreadId $ThreadId -CutoffDate $CutoffDate) {
        return $false
    }

    $maxTry = 60
    $retryCount = 0
    
    while ($retryCount -lt $maxTry) {
        try {
            $thread = Get-QuipThreadV2 -Id $ThreadId
            $createdDate = [DateTimeOffset]::FromUnixTimeSeconds($thread.created_usec / 1000000).DateTime
            $updatedDate = [DateTimeOffset]::FromUnixTimeSeconds($thread.updated_usec / 1000000).DateTime
            
            # Determine if thread is recent
            $isRecent = ($createdDate -gt $CutoffDate) -or ($updatedDate -gt $CutoffDate)
            
            # Update progress tracking
            Update-ThreadProgress -ThreadId $ThreadId -IsRecent $isRecent
            
            return $isRecent
        }
        catch {
            # if exception message includes rate limit indicators, wait for 60 seconds and retry
            if ($_.Exception.Message -imatch "rate limit") {
                $retryCount++
                Write-Host "Rate limit reached while checking thread metadata for $ThreadId. Waiting for 60 seconds before retrying... (Attempt $retryCount of $maxTry)"
                Start-Sleep -Seconds 60
            }
            else {
                Write-Warning "Could not retrieve thread metadata for $ThreadId : $($_.Exception.Message)"
                # If we can't get metadata, include the document to be safe but don't cache the result
                return $true
            }
        }
    }
    
    if ($retryCount -eq $maxTry) {
        Write-Warning "Maximum retry attempts reached for thread metadata $ThreadId. Including document to be safe."
        # If we can't get metadata after all retries, include the document to be safe
        return $true
    }
}

# Function to recursively download Quip documents in HTML format from a specified folder and its subfolders
function Get-QuipDocumentsRecursive {
    param (
        [string]$FolderId,
        [string]$LocalPath,
        [DateTime]$CutoffDate = [DateTime]::MinValue
    )

    # Check if we should skip this folder based on progress data
    if (Should-SkipFolder -FolderId $FolderId -CutoffDate $CutoffDate) {
        return
    }

    $maxTry = 60

    # Get folder information
    $retryCount = 0
    while ($retryCount -lt $maxTry) {
        try {
            $folder = Get-QuipFolder -Id $FolderId
            break
        }
        catch {
            # if exception message includes rate limit indicators, wait for 60 seconds and retry
            if ($_.Exception.Message -imatch "rate limit") {
                $retryCount++
                Write-Host "Rate limit reached. Waiting for 60 seconds before retrying... (Attempt $retryCount of $maxTry)"
                Start-Sleep -Seconds 60
            }
            else {
                Write-Host "Error: $($_.Exception.Message)"
                return
            }
        }
    }

    if ($retryCount -eq $maxTry) {
        Write-Host "Maximum retry attempts reached. Unable to retrieve folder information."
        return
    }

    # Analyze folder structure
    $documentChildren = @()
    $folderChildren = @()
    $totalChildren = 0
    
    if ($null -ne $folder.children) {
        $totalChildren = $folder.children.Count
        foreach ($child in $folder.children) {
            if ($child.thread_id) {
                $documentChildren += $child
            } elseif ($child.folder_id) {
                $folderChildren += $child
            }
        }
    }
    
    $subfolderCount = $folderChildren.Count
    $isLeaf = ($subfolderCount -eq 0)
    
    Write-Host "Processing folder $FolderId`: $totalChildren total children ($($documentChildren.Count) documents, $subfolderCount subfolders)$(if ($isLeaf) { ' [LEAF]' })"

    # Update initial folder progress
    Update-FolderProgress -FolderId $FolderId -ChildCount $totalChildren -SubfolderCount $subfolderCount -IsLeaf $isLeaf -FullyProcessed $false

    # Check if the folder is not empty
    if ($null -ne $folder.children) {
        $folderPath = Join-Path -Path $LocalPath -ChildPath $FolderId

        # Ensure directory exists
        if (-not (Test-Path -Path $folderPath)) {
            New-Item -ItemType Directory -Force -Path $folderPath | Out-Null
        }

        $processedDocuments = 0
        $skippedDocuments = 0

        # Process documents in this folder
        foreach ($child in $documentChildren) {
            $documentId = $child.thread_id
            
            # Check if we should filter by recent date
            if ($CutoffDate -ne [DateTime]::MinValue) {
                if (-not (Test-IsRecentThread -ThreadId $documentId -CutoffDate $CutoffDate)) {
                    Write-Host "Skipping document $documentId (not recent enough)"
                    $skippedDocuments++
                    continue
                }
            }
            
            # Get document metadata for better file naming
            $documentTitle = "Unknown"
            $retryCount = 0
            while ($retryCount -lt $maxTry) {
                try {
                    $threadInfo = Get-QuipThreadV2 -Id $documentId
                    $documentTitle = $threadInfo.title
                    break
                }
                catch {
                    # if exception message includes rate limit indicators, wait for 60 seconds and retry
                    if ($_.Exception.Message -imatch "rate limit") {
                        $retryCount++
                        Write-Host "Rate limit reached while retrieving document title for $documentId. Waiting for 60 seconds before retrying... (Attempt $retryCount of $maxTry)"
                        Start-Sleep -Seconds 60
                    }
                    else {
                        Write-Warning "Could not retrieve document title for $documentId : $($_.Exception.Message)"
                        break
                    }
                }
            }

            if ($retryCount -eq $maxTry) {
                Write-Warning "Maximum retry attempts reached for document title $documentId. Using default title."
            }

            # Use simple document ID as filename to avoid path issues
            $documentPath = Join-Path -Path $folderPath -ChildPath "$documentId.html"

            # Check if the file already exists and overwrite is not enabled
            if (-not $Overwrite -and (Test-Path $documentPath)) {
                Write-Host "Document already exists: $documentTitle"
                # Update progress to mark as downloaded
                Update-ThreadProgress -ThreadId $documentId -IsRecent $true -Downloaded $true
                $processedDocuments++
                continue
            }

            # Download and save the document in HTML format using V2 API
            $retryCount = 0
            while ($retryCount -lt $maxTry) {
                try {
                    # Use V2 API which returns complete HTML content by default
                    $documentHtml = Get-QuipThreadHtmlV2 -Id $documentId
                    break
                }
                catch {
                    # if exception message includes rate limit indicators, wait for 60 seconds and retry
                    if ($_.Exception.Message -imatch "rate limit") {
                        $retryCount++
                        Write-Host "Rate limit reached. Waiting for 60 seconds before retrying... (Attempt $retryCount of $maxTry)"
                        Start-Sleep -Seconds 60
                    }
                    else {
                        Write-Host "Error downloading $documentId : $($_.Exception.Message)"
                        break
                    }
                }
            }

            if ($retryCount -eq $maxTry) {
                Write-Host "Maximum retry attempts reached. Unable to retrieve document HTML for $documentId"
                continue
            }
            
            if ($documentHtml) {
                try {
                    # Use Set-Content instead of Out-File to avoid wildcard issues
                    Set-Content -Path $documentPath -Value $documentHtml -Encoding UTF8
                    Write-Host "Downloaded document: $documentTitle ($documentId)"
                    
                    # Update progress to mark as downloaded
                    Update-ThreadProgress -ThreadId $documentId -IsRecent $true -Downloaded $true
                    $processedDocuments++
                }
                catch {
                    Write-Error "Failed to save document $documentId to $documentPath : $($_.Exception.Message)"
                }
            }
        }

        # Process subfolders recursively
        foreach ($child in $folderChildren) {
            Get-QuipDocumentsRecursive -FolderId $child.folder_id -LocalPath $folderPath -CutoffDate $CutoffDate
        }

        # Mark folder as fully processed if we processed or skipped all documents and all subfolders
        $allDocumentsHandled = ($processedDocuments + $skippedDocuments) -eq $documentChildren.Count
        $fullyProcessed = $allDocumentsHandled
        
        # Update final folder progress
        Update-FolderProgress -FolderId $FolderId -ChildCount $totalChildren -SubfolderCount $subfolderCount -IsLeaf $isLeaf -FullyProcessed $fullyProcessed
        
        if ($fullyProcessed) {
            Write-Host "Folder $FolderId marked as fully processed (documents: $processedDocuments processed, $skippedDocuments skipped)$(if ($isLeaf) { ' [LEAF FOLDER]' })"
        }
    } else {
        # Empty folder - mark as fully processed
        Update-FolderProgress -FolderId $FolderId -ChildCount 0 -SubfolderCount 0 -IsLeaf $true -FullyProcessed $true
        Write-Host "Empty folder $FolderId marked as fully processed [LEAF FOLDER]"
    }
    
    # Periodically save progress (every folder)
    Save-Progress
}

# Load existing progress
Load-Progress

# Calculate cutoff date if RecentMonths parameter is provided
$cutoffDate = [DateTime]::MinValue
if ($PSBoundParameters.ContainsKey('RecentMonths') -and $RecentMonths -gt 0) {
    $cutoffDate = (Get-Date).AddMonths(-$RecentMonths)
    Write-Host "Only downloading documents created or modified after: $($cutoffDate.ToString('yyyy-MM-dd HH:mm:ss'))"
}

Write-Host "Starting download of Quip documents..."
Write-Host "Root Folder ID: $RootFolderId"
Write-Host "Local Path: $LocalFolderPath"
Write-Host "Progress File: $ProgressFilePath"
Write-Host "Overwrite: $Overwrite"
Write-Host "Skip processed folders after: $(if ($SkipProcessedDays -eq 0) { 'Never (always reprocess)' } else { "$SkipProcessedDays day(s)" })"
if ($cutoffDate -ne [DateTime]::MinValue) {
    Write-Host "Recent filter: Last $RecentMonths months"
}

# Call the function to start downloading documents recursively
Get-QuipDocumentsRecursive -FolderId $RootFolderId -LocalPath $LocalFolderPath -CutoffDate $cutoffDate

# Save final progress
Save-Progress

Write-Host "All Quip documents downloaded and stored in $LocalFolderPath"
Write-Host "Progress tracking saved to $ProgressFilePath"
