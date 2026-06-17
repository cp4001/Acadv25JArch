; JArchitecture Inno Setup Script
; AutoCAD 2025 Plugin Installer

[Setup]
AppName=JArchitecture
AppVersion=1.0.0
AppPublisher=JArchitecture
DefaultDirName={commonappdata}\Autodesk\ApplicationPlugins\JArchitecture.bundle
DisableDirPage=yes
DefaultGroupName=JArchitecture
OutputDir=Output
OutputBaseFilename=JArchitecture_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayName=JArchitecture for AutoCAD 2025
SetupIconFile=
WizardStyle=modern

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; PackageContents.xml
Source: "PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion

; Main DLL
Source: "C:\Jarch25\Acadv25JArch.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "C:\Jarch25\EPPlus.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "C:\Jarch25\DuctSizing.Core.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "C:\Jarch25\x64\Debug\net8.0-windows\PipeLoad.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion   
Source: "C:\Jarch25\JArchLicense.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion


; License DLL
Source: "C:\Jarch25\JArchLicense.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion

; Excel folder
Source: "C:\Jarch25\Excel\*"; DestDir: "{app}\Contents\Excel"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "desktop.ini"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
const
  UninstallRegKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\JArchitecture_is1';

function IsAlreadyInstalled(): Boolean;
var
  UninstallString: String;
begin
  Result := RegQueryStringValue(HKLM, UninstallRegKey, 'UninstallString', UninstallString);
end;

function RunUninstaller(): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := False;
  if RegQueryStringValue(HKLM, UninstallRegKey, 'UninstallString', UninstallString) then
  begin
    Result := Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end;
end;

function InitializeSetup(): Boolean;
var
  Choice: Integer;
begin
  Result := True;

  if not DirExists(ExpandConstant('{commonappdata}\Autodesk\ApplicationPlugins')) then
  begin
    MsgBox('Autodesk ApplicationPlugins 폴더를 찾을 수 없습니다.' + #13#10 +
           'AutoCAD 2025가 설치되어 있는지 확인해주세요.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if IsAlreadyInstalled() then
  begin
    Choice := MsgBox('JArchitecture가 이미 설치되어 있습니다.' + #13#10 + #13#10 +
                     '예: 기존 버전을 삭제합니다.' + #13#10 +
                     '아니오: 기존 버전을 삭제하고 다시 설치합니다.' + #13#10 +
                     '취소: 작업을 취소합니다.',
                     mbConfirmation, MB_YESNOCANCEL);
    case Choice of
      IDYES:
      begin
        RunUninstaller();
        Result := False;
      end;
      IDNO:
      begin
        RunUninstaller();
        Result := True;
      end;
      IDCANCEL:
      begin
        Result := False;
      end;
    end;
  end;
end;
