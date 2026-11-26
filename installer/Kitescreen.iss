#ifndef AppDir
  #error AppDir is required
#endif
#ifndef HelperDir
  #error HelperDir is required
#endif
#ifndef DriverDir
  #error DriverDir is required
#endif
#ifndef AdbDir
  #error AdbDir is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif
#ifndef IconFile
  #error IconFile is required
#endif

[Setup]
AppId={{B66C69EB-BB96-4CC8-B903-EB96ABCE7097}
AppName=Kitescreen
AppVersion=0.1.0
AppPublisher=Kites Group
DefaultDirName={autopf}\Kitescreen
DefaultGroupName=Kitescreen
OutputDir={#OutputDir}
OutputBaseFilename=Kitescreen-Windows-Setup
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Kitescreen.Host.exe
SetupIconFile={#IconFile}

[Files]
Source: "{#AppDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#HelperDir}\*"; DestDir: "{app}\setup"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#DriverDir}\IddSampleDriver.inf"; DestDir: "{app}\driver"; Flags: ignoreversion
Source: "{#DriverDir}\IddSampleDriver.cat"; DestDir: "{app}\driver"; Flags: ignoreversion
Source: "{#DriverDir}\IddSampleDriver.dll"; DestDir: "{app}\driver"; Flags: ignoreversion
Source: "{#AdbDir}\adb.exe"; DestDir: "{app}\platform-tools"; Flags: ignoreversion
Source: "{#AdbDir}\AdbWinApi.dll"; DestDir: "{app}\platform-tools"; Flags: ignoreversion
Source: "{#AdbDir}\AdbWinUsbApi.dll"; DestDir: "{app}\platform-tools"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Kitescreen"; Filename: "{app}\Kitescreen.Host.exe"
Name: "{autodesktop}\Kitescreen"; Filename: "{app}\Kitescreen.Host.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"

[Run]
Filename: "{app}\Kitescreen.Host.exe"; Description: "Start Kitescreen"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ExitCode: Integer;
begin
  if CurStep <> ssPostInstall then Exit;
  ExitCode := -1;
  if not Exec(ExpandConstant('{app}\setup\Kitescreen.DriverSetup.exe'),
    'install "' + ExpandConstant('{app}\driver\IddSampleDriver.inf') + '"',
    ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ExitCode) or (ExitCode <> 0) then
    RaiseException('The Kitescreen virtual display could not be installed. Windows error code: ' + IntToStr(ExitCode));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ExitCode: Integer;
begin
  if CurUninstallStep <> usUninstall then Exit;
  ExitCode := -1;
  if not Exec(ExpandConstant('{app}\setup\Kitescreen.DriverSetup.exe'), 'remove',
    ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, ExitCode) or (ExitCode <> 0) then
    MsgBox('Windows could not remove the Kitescreen virtual display. Please remove the Kitescreen device in Device Manager.', mbError, MB_OK);
end;
