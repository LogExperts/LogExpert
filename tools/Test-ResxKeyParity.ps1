<#
.SYNOPSIS
    Fails when resource keys differ between a neutral Resources.resx, its culture
    files (de, zh-CN, ...) and the generated Resources.Designer.cs accessors.

.DESCRIPTION
    For every Resources.resx under src/ that has at least one Resources.<culture>.resx
    sibling:
      - each culture file must contain exactly the neutral file's string keys
        (non-string entries such as images are only required in the neutral file);
      - Resources.Designer.cs, if present, must expose exactly the neutral file's keys.
    Exit code 0 when everything matches, 1 otherwise.

.EXAMPLE
    pwsh -File tools/Test-ResxKeyParity.ps1
#>
[CmdletBinding()]
param(
    [string] $Root = (Join-Path $PSScriptRoot '..' 'src')
)

$ErrorActionPreference = 'Stop'

function Get-ResxEntries([string] $Path) {
    [xml] $xml = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    foreach ($data in $xml.root.data) {
        [pscustomobject]@{
            Name     = $data.name
            IsString = [string]::IsNullOrEmpty($data.type) -and [string]::IsNullOrEmpty($data.mimetype)
        }
    }
}

function Compare-KeySet([string] $Label, [string[]] $Expected, [string[]] $Actual) {
    $missing = $Expected | Where-Object { $_ -notin $Actual } | Sort-Object
    $extra = $Actual | Where-Object { $_ -notin $Expected } | Sort-Object
    if (-not $missing -and -not $extra) { return $true }

    Write-Host "FAIL $Label" -ForegroundColor Red
    foreach ($k in $missing) { Write-Host "    missing: $k" }
    foreach ($k in $extra) { Write-Host "    extra:   $k" }
    return $false
}

$ok = $true
$neutralFiles = Get-ChildItem -LiteralPath $Root -Recurse -Filter 'Resources.resx' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

foreach ($neutral in $neutralFiles) {
    $dir = $neutral.DirectoryName
    $cultureFiles = Get-ChildItem -LiteralPath $dir -Filter 'Resources.*.resx'
    if (-not $cultureFiles) { continue }

    $relDir = [IO.Path]::GetRelativePath((Resolve-Path $Root), $dir)
    $entries = @(Get-ResxEntries $neutral.FullName)
    $allKeys = @($entries.Name)
    $stringKeys = @($entries | Where-Object IsString | ForEach-Object Name)

    foreach ($culture in $cultureFiles) {
        $cultureKeys = @(Get-ResxEntries $culture.FullName | Where-Object IsString | ForEach-Object Name)
        $ok = (Compare-KeySet "$relDir/$($culture.Name) vs Resources.resx" $stringKeys $cultureKeys) -and $ok
    }

    $designer = Join-Path $dir 'Resources.Designer.cs'
    if (Test-Path -LiteralPath $designer) {
        $accessors = @(Select-String -LiteralPath $designer -Pattern '^\s*(?:public|internal)\s+static\s+[\w.:]+\s+(\w+)\s*\{' |
            ForEach-Object { $_.Matches[0].Groups[1].Value } |
            Where-Object { $_ -notin 'ResourceManager', 'Culture' })
        $ok = (Compare-KeySet "$relDir/Resources.Designer.cs vs Resources.resx" $allKeys $accessors) -and $ok
    }
}

if ($ok) {
    Write-Host "Resx key parity OK ($($neutralFiles.Count) neutral files checked)."
    exit 0
}
exit 1
