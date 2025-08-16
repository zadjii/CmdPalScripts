# powershell fool

# @raycast.title Vaporwave Text
# @raycast.description Convert clipboard text to ｖａｐｏｒｗａｖｅ
#
# @raycast.icon 🌇
#
# @raycast.mode silent
# @raycast.packageName Conversion
# @raycast.schemaVersion 1

# Documentation:
# @raycast.author Mike Griese
# @raycast.authorURL https://github.com/zadjii


function Convert-ToVaporwave {
    param([string]$text)

    $vaporwaveMap = New-Object System.Collections.Hashtable 
    $vaporwaveMap[' '] = '　'  # Full-width space
    $vaporwaveMap['!'] = '！'
    # $vaporwaveMap["'"] = '＇'
    $vaporwaveMap['#'] = '＃'
    $vaporwaveMap['$'] = '＄'
    $vaporwaveMap['%'] = '％'
    $vaporwaveMap['&'] = '＆'
    $vaporwaveMap['"'] = '\"'
    $vaporwaveMap['('] = '（'
    $vaporwaveMap[')'] = '）'
    $vaporwaveMap['*'] = '＊'
    $vaporwaveMap['+'] = '＋'
    $vaporwaveMap[','] = '，'
    $vaporwaveMap['-'] = '－'
    $vaporwaveMap['.'] = '．'
    $vaporwaveMap['/'] = '／'
    $vaporwaveMap['0'] = '０'
    $vaporwaveMap['1'] = '１'
    $vaporwaveMap['2'] = '２'
    $vaporwaveMap['3'] = '３'
    $vaporwaveMap['4'] = '４'
    $vaporwaveMap['5'] = '５'
    $vaporwaveMap['6'] = '６'
    $vaporwaveMap['7'] = '７'
    $vaporwaveMap['8'] = '８'
    $vaporwaveMap['9'] = '９'
    $vaporwaveMap[':'] = '：'
    $vaporwaveMap[';'] = '；'
    $vaporwaveMap['<'] = '＜'
    $vaporwaveMap['='] = '＝'
    $vaporwaveMap['>'] = '＞'
    $vaporwaveMap['?'] = '？'
    $vaporwaveMap['@'] = '＠'
    $vaporwaveMap['A'] = 'Ａ'
    $vaporwaveMap['B'] = 'Ｂ'
    $vaporwaveMap['C'] = 'Ｃ'
    $vaporwaveMap['D'] = 'Ｄ'
    $vaporwaveMap['E'] = 'Ｅ'
    $vaporwaveMap['F'] = 'Ｆ'
    $vaporwaveMap['G'] = 'Ｇ'
    $vaporwaveMap['H'] = 'Ｈ'
    $vaporwaveMap['I'] = 'Ｉ'
    $vaporwaveMap['J'] = 'Ｊ'
    $vaporwaveMap['K'] = 'Ｋ'
    $vaporwaveMap['L'] = 'Ｌ'
    $vaporwaveMap['M'] = 'Ｍ'
    $vaporwaveMap['N'] = 'Ｎ'
    $vaporwaveMap['O'] = 'Ｏ'
    $vaporwaveMap['P'] = 'Ｐ'
    $vaporwaveMap['Q'] = 'Ｑ'
    $vaporwaveMap['R'] = 'Ｒ'
    $vaporwaveMap['S'] = 'Ｓ'
    $vaporwaveMap['T'] = 'Ｔ'
    $vaporwaveMap['U'] = 'Ｕ'
    $vaporwaveMap['V'] = 'Ｖ'
    $vaporwaveMap['W'] = 'Ｗ'
    $vaporwaveMap['X'] = 'Ｘ'
    $vaporwaveMap['Y'] = 'Ｙ'
    $vaporwaveMap['Z'] = 'Ｚ'
    $vaporwaveMap['['] = '['
    $vaporwaveMap['\\'] = '\\'
    $vaporwaveMap[']'] = ']'
    $vaporwaveMap['^'] = '^'
    $vaporwaveMap['_'] = '_'
    $vaporwaveMap['`'] = '`'
    $vaporwaveMap['a'] = 'ａ'
    $vaporwaveMap['b'] = 'ｂ'
    $vaporwaveMap['c'] = 'ｃ'
    $vaporwaveMap['d'] = 'ｄ'
    $vaporwaveMap['e'] = 'ｅ'
    $vaporwaveMap['f'] = 'ｆ'
    $vaporwaveMap['g'] = 'ｇ'
    $vaporwaveMap['h'] = 'ｈ'
    $vaporwaveMap['i'] = 'ｉ'
    $vaporwaveMap['j'] = 'ｊ'
    $vaporwaveMap['k'] = 'ｋ'
    $vaporwaveMap['l'] = 'ｌ'
    $vaporwaveMap['m'] = 'ｍ'
    $vaporwaveMap['n'] = 'ｎ'
    $vaporwaveMap['o'] = 'ｏ'
    $vaporwaveMap['p'] = 'ｐ'
    $vaporwaveMap['q'] = 'ｑ'
    $vaporwaveMap['r'] = 'ｒ'
    $vaporwaveMap['s'] = 'ｓ'
    $vaporwaveMap['t'] = 'ｔ'
    $vaporwaveMap['u'] = 'ｕ'
    $vaporwaveMap['v'] = 'ｖ'
    $vaporwaveMap['w'] = 'ｗ'
    $vaporwaveMap['x'] = 'ｘ'
    $vaporwaveMap['y'] = 'ｙ'
    $vaporwaveMap['z'] = 'ｚ'
    $vaporwaveMap['{'] = '{'
    $vaporwaveMap['|'] = '|'
    $vaporwaveMap['}'] = '}'
    $vaporwaveMap['~'] = '~'
    
    ($text.ToCharArray() | ForEach-Object {
        $s = $_.ToString()
        if ($vaporwaveMap.ContainsKey($s)) {
            $vaporwaveMap[$s]
        } else {
            $_
        }
    }) -join ''
}

# # Example usage:
# param([string]$input)
# 
# if (-not $input) {
#     $input = Read-Host "Enter text to vaporwave"
# }
$input = Get-Clipboard
if (-not $input) {
    Write-Error "No text on clipboard"
    exit 1
}
$vaporwaveOutput = Convert-ToVaporwave $input

$vaporwaveOutput | Set-Clipboard

Write-Output "$vaporwaveOutput copied to clipboard"
