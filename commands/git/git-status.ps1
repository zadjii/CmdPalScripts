# powershell fool

# Note: Set currentDirectoryPath to your local repository.

# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Status
# @raycast.mode inline

# Conditional parameters:
# @raycast.refreshTime 5m

# Optional parameters:
# @raycast.icon ./images/git.png
# @raycast.packageName Git
# @raycast.currentDirectoryPath D:\dev\public\ScriptsExtension

# Documentation:
# @raycast.author Mike Griese
# @raycast.authorURL https://github.com/zadjii
# @raycast.description Shows the status of your Git repository.


# param(
#   [Parameter(Mandatory = $true)]
#   [string]$FolderPath
# )

# # Expand ~ to user profile directory if present
# if ($FolderPath -match "~") {
#   $FolderPath = $FolderPath -replace "~", $env:USERPROFILE
# }

# # Change to the target directory
# Set-Location -Path $FolderPath

# Get git status --short output
$gitStatus = git status --short

# Count added, modified, deleted, untracked files
$added     = ($gitStatus | Select-String '^\s*A').Count
$modified  = ($gitStatus | Select-String '^\s*M').Count
$deleted   = ($gitStatus | Select-String '^\s*D').Count
$untracked = ($gitStatus | Select-String '^\?\?').Count

$message = ""

if ($added -gt 0) {
  $message += "`e[32m$added Added`e[0m "
}
if ($modified -gt 0) {
  $message += "`e[33m$modified Modified`e[0m "
}
if ($deleted -gt 0) {
  $message += "`e[31m$deleted Deleted`e[0m "
}
if ($untracked -gt 0) {
  $message += "`e[34m$untracked Untracked`e[0m "
}

if ([string]::IsNullOrWhiteSpace($message)) {
  $message = "No pending changes"
}

Write-Host $message
