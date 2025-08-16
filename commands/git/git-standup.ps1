# powershell fool

# Note: Set currentDirectoryPath to your local repository.

# Required parameters:
# @raycast.schemaVersion 1
# @raycast.title Standup
# @raycast.mode fullOutput

# Conditional parameters:
# @raycast.refreshTime 5m

# Optional parameters:
# @raycast.icon ./images/git.png
# @raycast.packageName Git
# @raycast.currentDirectoryPath D:\dev\public\ScriptsExtension

# Documentation:
# @raycast.author Mike Griese
# @raycast.authorURL https://github.com/zadjii
# @raycast.description Lists your commits from the last 24 hours. Optionally specify since when, e.g. "1 week".



$SINCE = "yesterday.midnight"
$USER_NAME = git config user.name
git log --author="$USER_NAME" --since="$SINCE" --oneline --pretty="format:%s %Cblue(%ar)%Creset" --color
