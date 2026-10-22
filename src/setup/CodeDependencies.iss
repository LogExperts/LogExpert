[Code]
// Based on https://github.com/DomGries/InnoDependencyInstaller
//
// TRIMMED for LogExpert: this installer only needs the .NET 10 Desktop Runtime.
// All other dependency definitions from the upstream script (other .NET versions,
// VC++ redistributables, DirectX, SQL Server, WebView2, Access, VSTO, Windows App
// Runtime, Java, Python, PowerShell 7) were removed. They were dead code here, and
// their embedded download URLs and an inline PowerShell execution-policy override
// made the compiled setup.exe look like a downloader to AV heuristics. Keep this file
// minimal: only add back a dependency helper if the installer actually invokes it.

// types and variables
type
  TDependency_Entry = record
    Filename: String;
    Parameters: String;
    Title: String;
    URL: String;
    Checksum: String;
    ForceSuccess: Boolean;
    RestartAfter: Boolean;
    Components: String;
  end;

var
  Dependency_List: array of TDependency_Entry;
  Dependency_NeedToRestart, Dependency_ForceX86, Dependency_ForceX64: Boolean;
  Dependency_Components: String;
  Dependency_DownloadPage: TDownloadWizardPage;

function Dependency_IsEntryActive(const Entry: TDependency_Entry): Boolean;
begin
  Result := (Entry.Components = '') or WizardIsComponentSelected(Entry.Components);
end;

procedure Dependency_Add(const Filename, Parameters, Title, URL, Checksum: String; const ForceSuccess, RestartAfter: Boolean);
var
  Dependency: TDependency_Entry;
  DependencyCount: Integer;
begin
  Dependency.Filename := Filename;
  Dependency.Parameters := Parameters;
  Dependency.Title := Title;

  if FileExists(ExpandConstant('{tmp}{\}') + Filename) then begin
    Dependency.URL := '';
    Log('Dependency queued (already in tmp): ' + Title);
  end else begin
    Dependency.URL := URL;
    Log('Dependency queued for download: ' + Title);
  end;

  Dependency.Checksum := Checksum;
  Dependency.ForceSuccess := ForceSuccess;
  Dependency.RestartAfter := RestartAfter;
  Dependency.Components := Dependency_Components;

  DependencyCount := GetArrayLength(Dependency_List);
  SetArrayLength(Dependency_List, DependencyCount + 1);
  Dependency_List[DependencyCount] := Dependency;
end;

<event('InitializeWizard')>
procedure Dependency_InitializeWizard;
begin
  Dependency_DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

<event('PrepareToInstall')>
function Dependency_PrepareToInstall(var NeedsRestart: Boolean): String;
var
  DependencyCount, DependencyIndex, ActiveCount, ActiveIndex, ResultCode: Integer;
  Retry: Boolean;
  TempValue: String;
begin
  DependencyCount := GetArrayLength(Dependency_List);

  if DependencyCount > 0 then begin
    Dependency_DownloadPage.Show;

    for DependencyIndex := 0 to DependencyCount - 1 do begin
      if not Dependency_IsEntryActive(Dependency_List[DependencyIndex]) then begin
        continue;
      end;
      if Dependency_List[DependencyIndex].URL <> '' then begin
        Dependency_DownloadPage.Clear;
        Dependency_DownloadPage.Add(Dependency_List[DependencyIndex].URL, Dependency_List[DependencyIndex].Filename, Dependency_List[DependencyIndex].Checksum);

        Retry := True;
        while Retry do begin
          Retry := False;

          try
            Dependency_DownloadPage.Download;
          except
            if Dependency_DownloadPage.AbortedByUser then begin
              Log('Download aborted by user: ' + Dependency_List[DependencyIndex].Title);
              Result := Dependency_List[DependencyIndex].Title;
              DependencyIndex := DependencyCount;
            end else begin
              case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                IDABORT: begin
                  Result := Dependency_List[DependencyIndex].Title;
                  DependencyIndex := DependencyCount;
                end;
                IDRETRY: begin
                  Retry := True;
                end;
              end;
            end;
          end;
        end;
      end;
    end;

    if Result = '' then begin
      ActiveCount := 0;
      for DependencyIndex := 0 to DependencyCount - 1 do begin
        if Dependency_IsEntryActive(Dependency_List[DependencyIndex]) then begin
          ActiveCount := ActiveCount + 1;
        end;
      end;

      ActiveIndex := 0;
      for DependencyIndex := 0 to DependencyCount - 1 do begin
        if not Dependency_IsEntryActive(Dependency_List[DependencyIndex]) then begin
          Log('Dependency skipped (component not selected): ' + Dependency_List[DependencyIndex].Title);
          continue;
        end;
        ActiveIndex := ActiveIndex + 1;
        Dependency_DownloadPage.SetText(Dependency_List[DependencyIndex].Title, '');
        Dependency_DownloadPage.SetProgress(ActiveIndex, ActiveCount + 1);

        while True do begin
          ResultCode := 0;
#ifdef Dependency_CustomExecute
          if {#Dependency_CustomExecute}(ExpandConstant('{tmp}{\}') + Dependency_List[DependencyIndex].Filename, Dependency_List[DependencyIndex].Parameters, ResultCode) then begin
#else
          if ShellExec('', ExpandConstant('{tmp}{\}') + Dependency_List[DependencyIndex].Filename, Dependency_List[DependencyIndex].Parameters, '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then begin
#endif
            Log('Dependency exit code ' + IntToStr(ResultCode) + ': ' + Dependency_List[DependencyIndex].Title);
            if Dependency_List[DependencyIndex].RestartAfter then begin
              if DependencyIndex = DependencyCount - 1 then begin
                Dependency_NeedToRestart := True;
              end else begin
                NeedsRestart := True;
                Result := Dependency_List[DependencyIndex].Title;
              end;
              break;
            end else if (ResultCode = 0) or Dependency_List[DependencyIndex].ForceSuccess then begin // ERROR_SUCCESS (0)
              break;
            end else if ResultCode = 1641 then begin // ERROR_SUCCESS_REBOOT_INITIATED (1641)
              NeedsRestart := True;
              Result := Dependency_List[DependencyIndex].Title;
              break;
            end else if ResultCode = 3010 then begin // ERROR_SUCCESS_REBOOT_REQUIRED (3010)
              Dependency_NeedToRestart := True;
              break;
            end else if ResultCode = 1638 then begin // ERROR_PRODUCT_VERSION (1638)
              break;
            end;
          end;

          case SuppressibleMsgBox(FmtMessage(SetupMessage(msgErrorFunctionFailed), [Dependency_List[DependencyIndex].Title, IntToStr(ResultCode)]), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
            IDABORT: begin
              Result := Dependency_List[DependencyIndex].Title;
              break;
            end;
            IDIGNORE: begin
              break;
            end;
          end;
        end;

        if Result <> '' then begin
          break;
        end;
      end;

      if NeedsRestart then begin
        Log('Dependency requires restart: registering RunOnce to resume setup');
        TempValue := '"' + ExpandConstant('{srcexe}') + '" /restart=1 /LANG="' + ExpandConstant('{language}') + '" /DIR="' + WizardDirValue + '" /GROUP="' + WizardGroupValue + '" /TYPE="' + WizardSetupType(False) + '" /COMPONENTS="' + WizardSelectedComponents(False) + '" /TASKS="' + WizardSelectedTasks(False) + '"';
        if WizardNoIcons then begin
          TempValue := TempValue + ' /NOICONS';
        end;
        RegWriteStringValue(HKA, 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', '{#SetupSetting("AppName")}', TempValue);
      end;
    end;

    Dependency_DownloadPage.Hide;
  end;
end;

#ifndef Dependency_NoUpdateReadyMemo
<event('UpdateReadyMemo')>
#endif
function Dependency_UpdateReadyMemo(const Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  DependencyIndex: Integer;
  DependencyMemo: String;
begin
  Result := '';
  if MemoUserInfoInfo <> '' then begin
    Result := Result + MemoUserInfoInfo + Newline + NewLine;
  end;
  if MemoDirInfo <> '' then begin
    Result := Result + MemoDirInfo + Newline + NewLine;
  end;
  if MemoTypeInfo <> '' then begin
    Result := Result + MemoTypeInfo + Newline + NewLine;
  end;
  if MemoComponentsInfo <> '' then begin
    Result := Result + MemoComponentsInfo + Newline + NewLine;
  end;
  if MemoGroupInfo <> '' then begin
    Result := Result + MemoGroupInfo + Newline + NewLine;
  end;
  if MemoTasksInfo <> '' then begin
    Result := Result + MemoTasksInfo;
  end;

  DependencyMemo := '';
  for DependencyIndex := 0 to GetArrayLength(Dependency_List) - 1 do begin
    if Dependency_IsEntryActive(Dependency_List[DependencyIndex]) then begin
      DependencyMemo := DependencyMemo + #13#10 + '%1' + Dependency_List[DependencyIndex].Title;
    end;
  end;

  if DependencyMemo <> '' then begin
    if MemoTasksInfo = '' then begin
      Result := Result + SetupMessage(msgReadyMemoTasks);
    end;
    Result := Result + FmtMessage(DependencyMemo, [Space]);
  end;
end;

<event('NeedRestart')>
function Dependency_NeedRestart: Boolean;
begin
  Result := Dependency_NeedToRestart;
end;

function Dependency_IsArm64: Boolean;
begin
  Result := not Dependency_ForceX86 and not Dependency_ForceX64 and IsArm64;
end;

function Dependency_IsX64: Boolean;
begin
  Result := not Dependency_ForceX86 and (Dependency_ForceX64 or Is64BitInstallMode);
end;

function Dependency_String(const x86, x64, arm64: String): String;
begin
  if Dependency_IsArm64 then begin
    Result := arm64;
  end else if Dependency_IsX64 then begin
    Result := x64;
  end else begin
    Result := x86;
  end;
end;

function Dependency_ArchSuffix: String;
begin
  Result := Dependency_String('', '_x64', '_arm64');
end;

function Dependency_ArchTitle: String;
begin
  Result := Dependency_String(' (x86)', ' (x64)', ' (arm64)');
end;

function Dependency_PassiveOrQuiet(const Passive, Quiet: String): String;
begin
  if WizardSilent then begin
    Result := Quiet;
  end else begin
    Result := Passive;
  end;
end;

var
  Dependency_NetCoreRuntimesArch: String;
  Dependency_NetCoreRuntimes: TArrayOfString;

procedure Dependency_ListNetCoreRuntimes;
var
  Arch, Path: String;
  ResultCode: Integer;
  Output: TExecOutput;
begin
  Arch := Dependency_String('x86', 'x64', 'arm64');
  if Dependency_NetCoreRuntimesArch = Arch then begin
    exit;
  end;
  Dependency_NetCoreRuntimesArch := Arch;
  SetArrayLength(Dependency_NetCoreRuntimes, 0);

  if not RegQueryStringValue(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\' + Arch, 'InstallLocation', Path) or not FileExists(Path + 'dotnet.exe') then begin
    Path := ExpandConstant(Dependency_String('{commonpf32}', '{commonpf64}', '{commonpf64}')) + '\dotnet\';
  end;
  if ExecAndCaptureOutput(Path + 'dotnet.exe', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode, Output) and (ResultCode = 0) then begin
    Dependency_NetCoreRuntimes := Output.StdOut;
  end;
end;

function Dependency_IsNetCoreInstalled(Runtime: String; Major, Minor, Revision: Word): Boolean;
var
  LineIndex: Integer;
  LineParts: TArrayOfString;
  PackedVersion: Int64;
  LineMajor, LineMinor, LineRevision, LineBuild: Word;
begin
  Dependency_ListNetCoreRuntimes;

  for LineIndex := 0 to Length(Dependency_NetCoreRuntimes) - 1 do begin
    LineParts := StringSplit(Trim(Dependency_NetCoreRuntimes[LineIndex]), [' '], stExcludeEmpty);

    if (Length(LineParts) > 1) and (Lowercase(LineParts[0]) = Lowercase(Runtime)) and StrToVersion(LineParts[1], PackedVersion) then begin
      UnpackVersionComponents(PackedVersion, LineMajor, LineMinor, LineRevision, LineBuild);

      if (LineMajor = Major) and (LineMinor = Minor) and (LineRevision >= Revision) then begin
        Result := True;
        exit;
      end;
    end;
  end;
  Result := False;
end;

procedure Dependency_AddNetCore(const Prefix, Title, URL: String);
begin
  Dependency_Add(Prefix + Dependency_ArchSuffix + '.exe',
    '/lcid ' + IntToStr(GetUILanguage) + ' ' + Dependency_PassiveOrQuiet('/passive', '/quiet') + ' /norestart',
    Title + Dependency_ArchTitle,
    URL,
    '', False, False);
end;

procedure Dependency_AddDotNet100Desktop;
begin
  // https://dotnet.microsoft.com/download/dotnet/10.0
  if not Dependency_IsNetCoreInstalled('Microsoft.WindowsDesktop.App', 10, 0, 9) then begin
    Dependency_AddNetCore('dotnet100desktop', '.NET Desktop Runtime 10.0.9', Dependency_String('https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x86.exe', 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe', 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-arm64.exe'));
  end;
end;
