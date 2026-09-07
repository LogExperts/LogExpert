# Test LogExpert's WinGet manifests

Double-click `LogExpert.wsb` on the host. The paths in that file assume this checkout is
`G:\Github\LogExpert`; update its two `HostFolder` entries if you move the checkout.
Windows Sandbox must be enabled and able to access the internet.

The guest maps only the manifests and test tools, both read-only. It installs WinGet
using [Microsoft's Sandbox bootstrap instructions](https://learn.microsoft.com/en-us/windows/package-manager/winget/#install-winget-on-windows-sandbox),
enables local manifests inside the guest, validates the YAML, and silently installs
LogExpert with its declared .NET dependency. It checks the registered version and
x64 Desktop Runtime, then opens a sample log for your manual check.

1. Confirm the application opens, both sample lines appear, filtering for `ERROR`
   works, and the columnizer menu and Help/About work without exceptions.
2. Close LogExpert, then click **Yes** in the **LogExpert application check** dialog.
   Click **No** if any application check failed. The script tests
   silent uninstall and checks that the executable and uninstall entry disappear.
3. Copy `Desktop\LogExpert-test-results` from the guest to the host before closing
   Sandbox. Files inside the guest are discarded when Sandbox closes.

If installation succeeded but a later check failed, keep that Sandbox open and
open `C:\SandboxTools` in the guest's File Explorer and double-click
`Resume-Test.cmd`. This opens PowerShell and resumes the checks. Alternatively:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\SandboxTools\Test-Manifest.ps1 -Resume
```

Resume skips WinGet setup and package installation, then checks the installed
application and tests removal. Its results go in a timestamped `resume-*` subfolder
of `Desktop\LogExpert-test-results`, preserving the original installation logs.
If the Sandbox has already been closed, open `LogExpert.wsb` for a fresh test.

If startup fails, click **No** in the check dialog and keep the guest open to inspect the exception and
logs. Successful manifest validation or installation does not establish that the
application works. To uninstall manually inside the guest:

```powershell
winget uninstall --product-code '{9C6E17B8-912C-45F9-9E7F-49CEAD8D6D7A}_is1' --silent --disable-interactivity
```

This is a clean installation test, not an upgrade test. Once the first version is
accepted into `microsoft/winget-pkgs`, also test upgrading from it before relying on
the release automation in PR #614. Submit only the three YAML files from `winget`
under `manifests/l/LogExperts/LogExpert/1.43.0/` in the community repository.
