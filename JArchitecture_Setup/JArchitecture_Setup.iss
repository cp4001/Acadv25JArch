; JArchitecture Inno Setup Script
; AutoCAD 2025 Plugin Installer
;
; 빌드: & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "JArchitecture_Setup.iss"
; 마법사 이미지/아이콘 재생성: pwsh -File Assets\make_assets.ps1

#define AppName     "JArchitecture"
#define AppVer      "1.0.0"
#define BuildStamp  GetDateTimeString('mm/dd', '_', '')

; BinDir  : 개발(Debug) 출력 + 수동 관리 데이터(Excel) 폴더
; MainDir : 배포용 Release 산출물 (Acadv25JArch.csproj 의 Release OutputPath)
; 배포본에는 절대 BinDir 의 DLL 을 넣지 말 것 - 그쪽은 Debug 산출물이다.
#define BinDir      "C:\Jarch25"
#define MainDir     BinDir + "\Release"
#define LicenseDll  SourcePath + "..\JArchLicense\bin\x64\Release\JArchLicense.dll"
#define PipeLoadDll BinDir + "\x64\Release\net8.0-windows\PipeLoad.dll"

; Release 빌드 누락 시 인스톨러 컴파일 자체를 실패시킨다
#if !FileExists(MainDir + "\Acadv25JArch.dll")
  #error Acadv25JArch.dll (Release) 없음 - dotnet build Acadv25JArch.csproj -c Release 로 먼저 빌드할 것
#endif
#if !FileExists(LicenseDll)
  #error JArchLicense.dll (Release|x64) 없음 - MSBuild JArchLicense.vcxproj /p:Configuration=Release /p:Platform=x64 로 먼저 빌드할 것
#endif
#if !FileExists(PipeLoadDll)
  #error PipeLoad.dll (Release|x64) 없음 - dotnet build PipeLoad.csproj -c Release -p:Platform=x64 로 먼저 빌드할 것
#endif

[Setup]
AppName={#AppName}
AppVersion={#AppVer}
AppPublisher=JArchitecture
DefaultDirName={commonappdata}\Autodesk\ApplicationPlugins\JArchitecture.bundle
DisableDirPage=yes
DefaultGroupName={#AppName}
OutputDir=Output
OutputBaseFilename={#AppName}_Setup_{#BuildStamp}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
; 실행할 때마다 %TEMP%\Setup Log YYYY-MM-DD #NNN.txt 자동 기록 (설치 실패 원인 추적용)
SetupLogging=yes
UninstallDisplayName={#AppName} for AutoCAD 2025
VersionInfoVersion={#AppVer}
WizardStyle=modern
SetupIconFile=Assets\JArchitecture.ico
WizardImageFile=Assets\WizardImage.png
WizardSmallImageFile=Assets\WizardSmallImage.png
WizardImageStretch=no

[Languages]
; 항목이 하나면 설치 시작 시 언어 선택 다이얼로그가 생략된다
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
; Bundle 디스크립터
Source: "PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion

; 관리 .NET 산출물 (Release 전용 폴더)
Source: "{#MainDir}\Acadv25JArch.dll";    DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "{#MainDir}\EPPlus.dll";          DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "{#MainDir}\DuctSizing.Core.dll"; DestDir: "{app}\Contents"; Flags: ignoreversion

; 별도 출력 경로 프로젝트 (Release 고정)
Source: "{#PipeLoadDll}"; DestDir: "{app}\Contents"; Flags: ignoreversion
Source: "{#LicenseDll}";  DestDir: "{app}\Contents"; Flags: ignoreversion

; Excel 데이터 (빌드 산출물이 아니라 수동 관리 폴더이므로 BinDir 그대로)
Source: "{#BinDir}\Excel\*"; DestDir: "{app}\Contents\Excel"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "desktop.ini"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
const
  UninstallRegKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\JArchitecture_is1';

{ WMI 로 프로세스 실행 여부 확인 }
function IsAppRunning(const FileName: string): Boolean;
var
  FSWbemLocator, FWMIService, FWbemObjectSet: Variant;
begin
  Result := False;
  try
    FSWbemLocator := CreateOleObject('WBemScripting.SWbemLocator');
    FWMIService := FSWbemLocator.ConnectServer('', 'root\CIMV2', '', '');
    FWbemObjectSet := FWMIService.ExecQuery(Format('SELECT Name FROM Win32_Process Where Name="%s"', [FileName]));
    Result := (FWbemObjectSet.Count > 0);
  except
    Result := False;
  end;
end;

function WarnIfAutoCadRunning(const ActionText: string): Boolean;
begin
  Result := IsAppRunning('acad.exe');
  if Result then
    MsgBox('AutoCAD가 실행 중입니다.' + #13#10 + #13#10 +
           'AutoCAD를 완전히 종료한 뒤 다시 ' + ActionText + '해주세요.' + #13#10 +
           '실행 중이면 기존 DLL이 잠겨 있어 교체되지 않습니다.', mbError, MB_OK);
end;

// InitializeSetup 시점에는 app 상수가 아직 초기화되지 않으므로 DefaultDirName 과 같은 경로를 직접 만든다
// (Pascal 주석 {} 안에 중괄호 상수를 쓰면 주석이 조기 종료되어 문법 오류가 난다 -> // 주석 사용)
function BundleDir(): String;
begin
  Result := ExpandConstant('{commonappdata}\Autodesk\ApplicationPlugins\JArchitecture.bundle');
end;

function IsAlreadyInstalled(): Boolean;
var
  UninstallString: String;
begin
  Result := RegQueryStringValue(HKLM, UninstallRegKey, 'UninstallString', UninstallString);
end;

{ 기존 버전 삭제. 반드시 "삭제 완료"까지 기다린 뒤 반환해야 한다.

  Inno 언인스톨러(unins000.exe)는 자기 자신을 %TEMP%\_iu*.tmp 로 복사해 재실행하고
  원본 프로세스는 즉시 종료한다. 따라서 ewWaitUntilTerminated 를 줘도 Exec 는 실제 삭제가
  끝나기 전에 반환한다. 그대로 설치를 진행하면 파일 복사와 백그라운드 삭제가 경쟁 상태가 되어,
  설치가 써 넣은 레지스트리 키/파일을 뒤늦게 끝난 언인스톨러가 지워버린다.
  (증상: "설치 완료" 인데 번들에는 구버전 DLL 이 남고 언인스톨 등록 키가 사라짐) }
function RunUninstaller(): Boolean;
var
  UninstallString: String;
  ResultCode, i: Integer;
begin
  Result := True;

  // 설치 이력 자체가 없으면 할 일 없음 (성공으로 간주하고 설치 진행)
  if not RegQueryStringValue(HKLM, UninstallRegKey, 'UninstallString', UninstallString) then
    Exit;

  UninstallString := RemoveQuotes(UninstallString);

  // 고아 항목 처리: 번들 폴더를 수동 삭제하면 unins000.exe 는 없는데 레지스트리 키만 남는다.
  // 이때 Exec 는 실패하는데, 그대로 중단하면 "재설치를 눌렀는데 아무 안내 없이 종료"가 된다.
  // 지울 대상이 이미 없는 것이므로 키만 정리하고 설치를 계속한다.
  if not FileExists(UninstallString) then
  begin
    RegDeleteKeyIncludingSubkeys(HKLM, UninstallRegKey);
    Exit;
  end;

  if not Exec(UninstallString, '/SILENT /NORESTART', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('기존 버전 삭제 프로그램을 실행하지 못했습니다.' + #13#10 + #13#10 +
           UninstallString + #13#10 + #13#10 +
           '제어판 > 프로그램에서 먼저 삭제한 뒤 다시 실행해주세요.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  { 1) 언인스톨 등록 키가 사라질 때까지 (최대 60초) }
  for i := 1 to 300 do
  begin
    if not IsAlreadyInstalled() then
      Break;
    Sleep(200);
  end;

  { 2) 번들 폴더까지 정리될 때까지 (최대 20초) }
  for i := 1 to 100 do
  begin
    if not DirExists(BundleDir()) then
      Break;
    Sleep(200);
  end;

  Result := not IsAlreadyInstalled();
  if not Result then
    MsgBox('기존 버전 삭제가 제한 시간 안에 끝나지 않았습니다.' + #13#10 + #13#10 +
           '제어판 > 프로그램에서 JArchitecture 를 먼저 삭제한 뒤 다시 실행해주세요.', mbError, MB_OK);
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

  if WarnIfAutoCadRunning('설치') then
  begin
    Result := False;
    Exit;
  end;

  if IsAlreadyInstalled() then
  begin
    Choice := TaskDialogMsgBox('JArchitecture가 이미 설치되어 있습니다.', '기존 버전을 어떻게 할까요?',
      mbConfirmation, MB_YESNOCANCEL, ['재설치 (삭제 후 새 버전 설치)', '삭제만 (설치하지 않음)', '취소'], IDYES);
    case Choice of
      IDYES:
        { 재설치 — 삭제가 완전히 끝난 뒤에만 설치를 진행한다 }
        Result := RunUninstaller();
      IDNO:
        begin
          RunUninstaller();
          Result := False;
        end;
      IDCANCEL:
        Result := False;
    end;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := not WarnIfAutoCadRunning('삭제');
end;
