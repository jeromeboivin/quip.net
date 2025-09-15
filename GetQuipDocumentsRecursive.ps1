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

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments"

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments" -RecentMonths 3

.NOTES
Authentication:
To use this API, you need to define a user environment variable name "QuipApiKey".
Please go to https://quip.com/dev/token to generate your access token.
More information on how to use the Quip Powershell API can be found at https://github.com/jeromeboivin/quip.net

Make sure to import the Quip module before running this script:
Import-Module <path to quipps.dll>

Version 2.0 - Updated to use Quip API V2 for better performance and additional features
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
    [string]$ProgressFilePath
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
    ProcessedFolders = @{}  # FolderId -> @{ LastProcessed = DateTime; ChildCount = int }
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
                    $script:ProgressData.ProcessedFolders[$_.Name] = @{
                        LastProcessed = [DateTime]$_.Value.LastProcessed
                        ChildCount = $_.Value.ChildCount
                    }
                }
            }
            
            # Update filter info
            $script:ProgressData.RecentMonthsFilter = $RecentMonths
            
            Write-Host "Loaded existing progress from $ProgressFilePath"
            Write-Host "Previous run processed $($script:ProgressData.ProcessedThreads.Count) threads and $($script:ProgressData.ProcessedFolders.Count) folders"
        }
        catch {
            Write-Warning "Could not load existing progress file: $($_.Exception.Message). Starting fresh."
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
        
        # If we checked this thread recently (within last 24 hours) and it wasn't recent, skip it
        $lastChecked = $threadInfo.LastChecked
        $hoursSinceCheck = ((Get-Date) - $lastChecked).TotalHours
        
        if ($hoursSinceCheck -lt 24 -and -not $threadInfo.IsRecent) {
            Write-Host "Skipping thread $ThreadId (cached as not recent, checked $([math]::Round($hoursSinceCheck, 1)) hours ago)"
            return $true
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

    # Update folder progress
    $script:ProgressData.ProcessedFolders[$FolderId] = @{
        LastProcessed = (Get-Date)
        ChildCount = if ($folder.children) { $folder.children.Count } else { 0 }
    }

    # Check if the folder is not empty
    if ($null -ne $folder.children) {
        $folderPath = Join-Path -Path $LocalPath -ChildPath $FolderId

        # Ensure directory exists
        if (-not (Test-Path -Path $folderPath)) {
            New-Item -ItemType Directory -Force -Path $folderPath | Out-Null
        }

        # Loop through each child in the folder
        foreach ($child in $folder.children) {
            # If child is a document, download it
            if ($child.thread_id) {
                $documentId = $child.thread_id
                
                # Check if we should filter by recent date
                if ($CutoffDate -ne [DateTime]::MinValue) {
                    if (-not (Test-IsRecentThread -ThreadId $documentId -CutoffDate $CutoffDate)) {
                        Write-Host "Skipping document $documentId (not recent enough)"
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
                    }
                    catch {
                        Write-Error "Failed to save document $documentId to $documentPath : $($_.Exception.Message)"
                    }
                }
            }
            # If child is a folder, recursively download documents in it
            elseif ($child.folder_id) {
                Get-QuipDocumentsRecursive -FolderId $child.folder_id -LocalPath $folderPath -CutoffDate $CutoffDate
            }
        }
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
if ($cutoffDate -ne [DateTime]::MinValue) {
    Write-Host "Recent filter: Last $RecentMonths months"
}

# Call the function to start downloading documents recursively
Get-QuipDocumentsRecursive -FolderId $RootFolderId -LocalPath $LocalFolderPath -CutoffDate $cutoffDate

# Save final progress
Save-Progress

Write-Host "All Quip documents downloaded and stored in $LocalFolderPath"
Write-Host "Progress tracking saved to $ProgressFilePath"
