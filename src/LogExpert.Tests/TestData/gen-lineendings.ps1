# Regenerates the LineEndings_*.txt fixtures with byte-exact terminators.
# Run from anywhere: paths resolve relative to this script's own location.
# The fixtures are pinned `binary` in .gitattributes so git never normalizes EOLs.
$dir = $PSScriptRoot
$enc = New-Object System.Text.UTF8Encoding($false)   # UTF-8, no BOM

# Build multibyte chars from code points so the script stays pure-ASCII on disk.
$umlaut = [char]0x00FC                           # u-umlaut
$cjk    = [string][char]0x4E16 + [char]0x754C    # CJK "world"

$lines = @(
    "2026-06-25 10:00:01.000 [INFO] Line 1 - start of file",
    "2026-06-25 10:00:02.000 [INFO] Line 2 - second line",
    "2026-06-25 10:00:03.000 [WARN] Line 3 - multibyte: $umlaut $cjk",
    "2026-06-25 10:00:04.000 [INFO] Line 4 - line before the empty one",
    "",
    "2026-06-25 10:00:06.000 [ERROR] Line 6 - line after the empty one",
    "2026-06-25 10:00:07.000 [INFO] Line 7 - final line, no trailing newline"
)

function Write-File($name, $sep) {
    # Last line gets no trailing terminator (exercises the EOF tail path).
    $text = [string]::Join($sep, $lines)
    [System.IO.File]::WriteAllBytes((Join-Path $dir $name), $enc.GetBytes($text))
}

Write-File "LineEndings_LF.txt"   "`n"
Write-File "LineEndings_CRLF.txt" "`r`n"
Write-File "LineEndings_CR.txt"   "`r"

# Mixed: a different terminator after each line, last line unterminated.
$terms = @("`n", "`r`n", "`r", "`n", "`r`n", "`r")
$sb = New-Object System.Text.StringBuilder
for ($i = 0; $i -lt $lines.Count; $i++) {
    [void]$sb.Append($lines[$i])
    if ($i -lt $terms.Count) { [void]$sb.Append($terms[$i]) }
}
[System.IO.File]::WriteAllBytes((Join-Path $dir "LineEndings_Mixed.txt"), $enc.GetBytes($sb.ToString()))

Write-Output "Done."
