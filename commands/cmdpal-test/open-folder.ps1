# powershell fool

# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Open Folder 
# @raycast.mode silent
# @raycast.packageName System
#
# Optional parameters:
# @raycast.icon 📁
# @raycast.currentDirectoryPath /
# @raycast.needsConfirmation false
# @raycast.argument1 { "type": "text", "placeholder": "folder_path", "optional": false }
#
# Documentation:
# @raycast.description Open a folder on Windows using PowerShell
# @raycast.author Mike Griese
# @raycast.authorURL https://github.com/zadjii

param(
    [Parameter(Mandatory=$true)]
    [string]$FolderPath
)

# Expand ~ to user profile directory if present
if ($FolderPath -match "~") {
    # $FolderPath = $FolderPath -replace "~", $env:USERPROFILE
	Invoke-Item $FolderPath
	return
}

# Open the folder using Windows Explorer
Invoke-Item $FolderPath

Write-Host "Opened folder: $FolderPath"
exit 0