# Run only through LogExpert.wsb. All installations and logs stay in the guest.
param([switch]$Resume)

$ErrorActionPreference = 'Stop'
if ($env:USERNAME -ne 'WDAGUtilityAccount' -or -not (Test-Path C:\Manifest)) {
    throw 'Run this script inside Windows Sandbox using LogExpert.wsb.'
}

$results = Join-Path $env:USERPROFILE 'Desktop\LogExpert-test-results'
if ($Resume) {
    $results = Join-Path $results ('resume-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
New-Item -ItemType Directory -Path $results -Force | Out-Null
Start-Transcript -Path (Join-Path $results 'transcript.txt') -Force

function Invoke-WinGet {
    Write-Host "Running: winget $args"
    # Piping native output makes it visible to PowerShell's transcript as well as
    # saving a separate command log (direct console output can be absent there).
    & $script:winget @args | Tee-Object -FilePath (Join-Path $results 'winget-output.txt') -Append | Out-Host
    $commandExitCode = $LASTEXITCODE
    Write-Host ('WinGet exit code: {0} (0x{1:X8})' -f $commandExitCode, ($commandExitCode -band 0xffffffffL))
    if ($commandExitCode -ne 0) {
        throw "WinGet failed (exit code $commandExitCode): $args"
    }
}

function Get-LogExpertRegistration {
    $key = '{9C6E17B8-912C-45F9-9E7F-49CEAD8D6D7A}_is1'
    foreach ($root in @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
    )) {
        Get-ItemProperty -LiteralPath "$root\$key" -ErrorAction SilentlyContinue
    }
}

try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $ProgressPreference = 'SilentlyContinue'
    if (-not $Resume) {
        Write-Host 'Installing WinGet in Windows Sandbox...'
        Install-PackageProvider -Name NuGet -Force | Out-Null
        Install-Module -Name Microsoft.WinGet.Client -Repository PSGallery -Force -Scope AllUsers
        Import-Module Microsoft.WinGet.Client
        Repair-WinGetPackageManager -AllUsers
    }

    $script:winget = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps\winget.exe'
    if (-not (Test-Path $script:winget)) {
        $script:winget = Join-Path (Get-AppxPackage Microsoft.DesktopAppInstaller).InstallLocation 'winget.exe'
    }
    Invoke-WinGet --version
    Invoke-WinGet --info
    if (-not $Resume) {
        Invoke-WinGet settings --enable LocalManifestFiles
        Invoke-WinGet validate --manifest C:\Manifest
        Invoke-WinGet show --id Microsoft.DotNet.DesktopRuntime.10 --exact --source winget --accept-source-agreements --disable-interactivity
        # Let the manifest install its dependency; do not preinstall .NET.
        Invoke-WinGet install --manifest C:\Manifest --dependency-source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --verbose-logs
    }

    $registration = @(Get-LogExpertRegistration)
    if ($registration.Count -ne 1) {
        throw "Expected one machine-wide LogExpert uninstall entry, found $($registration.Count)."
    }
    $registration | Select-Object DisplayName, DisplayVersion, Publisher, InstallLocation, PSChildName |
        Format-List | Out-String | Tee-Object -FilePath (Join-Path $results 'registration.txt') | Write-Host
    $expectedVersion = (Select-String -Path C:\Manifest\LogExperts.LogExpert.yaml -Pattern '^PackageVersion:\s*(.+)$').Matches.Groups[1].Value.Trim()
    if ($registration[0].DisplayVersion -ne $expectedVersion) {
        throw "Installed version does not match manifest version $expectedVersion."
    }
    # Inno registers a versioned display name. Query installed applications without
    # requiring an exact unversioned name or membership in the published catalog.
    # The registry checks above verify the precise product code and version.
    Invoke-WinGet list --name LogExpert --accept-source-agreements --disable-interactivity
    $dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    $runtimes = & $dotnet --list-runtimes
    $runtimes | Tee-Object -FilePath (Join-Path $results 'runtimes.txt') | Write-Host
    if ($LASTEXITCODE -ne 0 -or -not ($runtimes -match '^Microsoft.WindowsDesktop.App 10\.')) {
        throw 'The x64 .NET 10 Desktop Runtime is missing.'
    }
    $exe = Join-Path $registration[0].InstallLocation 'LogExpert.exe'
    $sample = Join-Path $results 'sample.log'
    @('2026-09-06 INFO Sandbox installation completed', '2026-09-06 ERROR Sample error for filtering') |
        Set-Content -Path $sample
    Start-Process -FilePath $exe -ArgumentList ('"{0}"' -f $sample)
    Write-Host 'Check that LogExpert opens the sample log, displays both lines, and can filter for ERROR.'
    Write-Host 'Also check the columnizer menu and Help/About. An installer success alone is not an application pass.'
    # Sandbox logon may not provide usable console input. A dialog waits for an
    # explicit result independently of PowerShell's console visibility/stdin.
    $dialogs = New-Object -ComObject WScript.Shell
    do {
        $message = "Check the sample log, filtering for ERROR, the columnizer menu, and Help/About.`r`n`r`nClose LogExpert when finished, then click Yes if all checks passed, or No if something failed."
        # Yes/No, question icon, default No, bring to foreground; no timeout.
        $answer = $dialogs.Popup($message, 0, 'LogExpert application check', 4 + 32 + 256 + 65536)
        Write-Host "Manual application check dialog result: $answer (6 = Yes)"
        if ($answer -ne 6) { throw 'Manual application smoke test was not confirmed as PASS.' }
        $stillRunning = @(Get-Process LogExpert -ErrorAction SilentlyContinue).Count -gt 0
        if ($stillRunning) {
            [void]$dialogs.Popup('LogExpert is still running. Close it before confirming the check.', 0, 'Close LogExpert', 48 + 65536)
        }
    } while ($stillRunning)
    Invoke-WinGet uninstall --product-code '{9C6E17B8-912C-45F9-9E7F-49CEAD8D6D7A}_is1' --silent --disable-interactivity
    if (@(Get-LogExpertRegistration).Count -ne 0 -or (Test-Path $exe)) {
        throw 'Uninstall left the application executable or uninstall registration behind.'
    }
    if ($Resume) {
        Write-Host 'PASS: resumed registration, runtime, manual application check, and uninstall. See the original run for installation results.' -ForegroundColor Green
    }
    else {
        Write-Host 'PASS: install, dependency, registration, manual application check, and uninstall.' -ForegroundColor Green
    }
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $_ | Out-String | Set-Content (Join-Path $results 'failure.txt')
}
finally {
    $logLocations = @{
        'WinGet-logs' = Join-Path $env:LOCALAPPDATA 'Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir'
        'WinGet-temp-logs' = Join-Path $env:TEMP 'WinGet'
    }
    foreach ($entry in $logLocations.GetEnumerator()) {
        if (Test-Path $entry.Value) {
            Copy-Item -Path $entry.Value -Destination (Join-Path $results $entry.Key) -Recurse -Force
        }
    }
    Write-Host "Results: $results. Copy them to the host before closing Windows Sandbox."
    Stop-Transcript
}
