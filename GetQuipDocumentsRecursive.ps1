<#
.SYNOPSIS
This script downloads all Quip documents in a specified root folder and its subfolders in HTML format.

.DESCRIPTION
The downloaded documents are stored in a local folder on the machine where the script is run.
The script uses the Quip API to retrieve document information and content.

.PARAMETER RootFolderId
The Quip root folder ID from which to start downloading documents.

.PARAMETER LocalFolderPath
The local folder path where the downloaded documents will be stored.

.EXAMPLE
.\GetQuipDocumentsRecursive.ps1 -RootFolderId "root_folder_id" -LocalFolderPath "C:\QuipDocuments"

.NOTES
Authentication:
To use this API, you need to define a user environment variable name "QuipApiKey".
Please go to https://quip.com/dev/token to generate your access token.
More information on how to use the Quip Powershell API can be found at https://github.com/jeromeboivin/quip.net

Make sure to import the Quip module before running this script:
Import-Module <path to quipps.dll>
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
    [switch]$Overwrite = $false
)

# Function to recursively download Quip documents in HTML format from a specified folder and its subfolders
function Get-QuipDocumentsRecursive {
    param (
        [string]$FolderId,
        [string]$LocalPath
    )

    $maxTry = 10

    # Get folder information
    $retryCount = 0
    while ($retryCount -lt $maxTry) {
        try {
            $folder = Get-QuipFolder -Id $FolderId
            break
        }
        catch {
            # if exception message includes "Over Rate Limit", wait for 60 seconds and retry
            if ($_.Exception.Message -match "Over Rate Limit") {
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

    # Check if the folder is not empty
    if ($null -ne $folder.children) {
        $folderPath = Join-Path -Path $LocalPath -ChildPath $FolderId

        New-Item -ItemType Directory -Force -Path $folderPath | Out-Null

        # Loop through each child in the folder
        foreach ($child in $folder.children) {
            # If child is a document, download it
            if ($child.thread_id) {
                $documentId = $child.thread_id
                $documentPath = Join-Path -Path $folderPath -ChildPath "$documentId.html"

                # Check if the file already exists and overwrite is not enabled
                if (-not $Overwrite -and (Test-Path $documentPath)) {
                    Write-Host "Document already exists: $documentTitle"
                    continue
                }

                # Download and save the document in HTML format
                $retryCount = 0
                while ($retryCount -lt $maxTry) {
                    try {
                        $documentHtml = Get-QuipThreadHtml -Id $documentId
                        break
                    }
                    catch {
                        # if exception message includes "Over Rate Limit", wait for 60 seconds and retry
                        if ($_.Exception.Message -match "Over Rate Limit") {
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
                    Write-Host "Maximum retry attempts reached. Unable to retrieve document HTML."
                    return
                }
                
                $documentHtml | Out-File -FilePath $documentPath -Encoding UTF8
                Write-Host "Downloaded document: $documentTitle"
            }
            # If child is a folder, recursively download documents in it
            elseif ($child.folder_id) {
                Get-QuipDocumentsRecursive -FolderId $child.folder_id -LocalPath $folderPath
            }
        }
    }
}

# Call the function to start downloading documents recursively
Get-QuipDocumentsRecursive -FolderId $RootFolderId -LocalPath $LocalFolderPath

Write-Host "All Quip documents downloaded and stored in $LocalFolderPath"
