# powershell fool

# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Open Home 
# @raycast.mode silent
# @raycast.packageName System
#
# Optional parameters:
# @raycast.icon 🏠
# @raycast.currentDirectoryPath /
# @raycast.needsConfirmation false
#
# Documentation:
# @raycast.description Open the user's home folder on Windows using PowerShell
# @raycast.author Mike Griese
# @raycast.authorURL https://github.com/zadjii

# Open the folder using Windows Explorer
Invoke-Item ~
Write-Host "Opened home folder: $env:USERPROFILE"
exit 0