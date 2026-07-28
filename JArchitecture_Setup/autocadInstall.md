---
tags: [AutoCAD]
---

# AutoCAD Addin Bundle 설치 구조 및 파일 접근 가이드

## 1. AutoCAD Bundle 설치 경로

AutoCAD는 두 가지 ApplicationPlugins 경로를 인식합니다.

### 사용자 전용 경로
- **환경변수**: `%AppData%`
- **실제 경로**: `C:\Users\{사용자명}\AppData\Roaming\Autodesk\ApplicationPlugins\`
- **용도**: 현재 로그인한 사용자만 사용 가능
- **PowerShell 확인**: `dir "$env:AppData\Autodesk\ApplicationPlugins"`

### 모든 사용자 공용 경로
- **환경변수**: `%ProgramData%` (`{commonappdata}`)
- **실제 경로**: `C:\ProgramData\Autodesk\ApplicationPlugins\`
- **용도**: 모든 사용자가 사용 가능 (관리자 권한 필요)
- **PowerShell 확인**: `dir "$env:ProgramData\Autodesk\ApplicationPlugins"`

> **참고**: PowerShell에서는 `%AppData%` 형식이 동작하지 않습니다. 반드시 `$env:AppData` 또는 `$env:ProgramData` 형식을 사용해야 합니다.

---

## 2. JArchitecture 설치 구조 (Inno Setup 기준)

Inno Setup 스크립트(`JArchitecture_Setup.iss`)에서 `{commonappdata}`를 사용하므로 **공용 경로**에 설치됩니다.

### 설치 기본 경로
```
C:\ProgramData\Autodesk\ApplicationPlugins\JArchitecture.bundle\
```

### 폴더 구조
```
JArchitecture.bundle\
├── PackageContents.xml          ← Bundle 설정 파일
└── Contents\
    ├── Acadv25JArch.dll         ← 메인 DLL
    ├── EPPlus.dll               ← Excel 처리 라이브러리
    ├── JArchLicense.dll         ← 라이선스 DLL
    └── Excel\
        ├── load_eng.xlsm        ← Excel 데이터 파일
        └── (기타 Excel 파일들)
```

### ISS 파일 경로 매핑

| ISS 변수 | 실제 경로 |
|----------|-----------|
| `{app}` | `C:\ProgramData\Autodesk\ApplicationPlugins\JArchitecture.bundle` |
| `{app}\Contents` | `...\JArchitecture.bundle\Contents\` |
| `{app}\Contents\Excel` | `...\JArchitecture.bundle\Contents\Excel\` |

---

## 3. 런타임에서 설치 파일 접근 방법

### 핵심 원리

`System.Reflection.Assembly.GetExecutingAssembly().Location`은 현재 실행 중인 DLL의 **실제 로드 경로**를 반환합니다. 이 방식은 설치 경로가 `%AppData%`이든 `%ProgramData%`이든 관계없이 정상 동작합니다.

### 기본 코드 (DLL과 같은 폴더의 파일)

```csharp
string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
string dllFolder = System.IO.Path.GetDirectoryName(dllPath);

// DLL과 같은 폴더에 있는 파일 접근
string filePath = System.IO.Path.Combine(dllFolder, "파일명");
```

### JArchitecture 실제 사용 예시 (Excel 하위 폴더)

DLL은 `Contents\`에, Excel 파일은 `Contents\Excel\`에 있으므로:

```csharp
string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
string dllFolder = System.IO.Path.GetDirectoryName(dllPath);

// Contents\Excel\ 하위의 파일 접근
string filePath = System.IO.Path.Combine(dllFolder, "Excel", "load_eng.xlsm");
```

### 절대 경로 방식 (기존 - 비권장)

```csharp
// ❌ 비권장: 설치 경로가 바뀌면 동작하지 않음
string filePath = @"C:\Jarch25\load_eng.xlsm";
```

### 상대 경로 방식 (권장)

```csharp
// ✅ 권장: 설치 경로에 관계없이 동작
string dllFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string filePath = Path.Combine(dllFolder, "Excel", "load_eng.xlsm");
```

---

## 4. CLI에서 설치 확인

### PowerShell에서 설치된 Bundle 목록 확인

```powershell
# 공용 경로 (ProgramData)
dir "$env:ProgramData\Autodesk\ApplicationPlugins"

# 사용자 경로 (AppData\Roaming)
dir "$env:AppData\Autodesk\ApplicationPlugins"
```

### 특정 Bundle 내부 파일 전체 확인

```powershell
dir "$env:ProgramData\Autodesk\ApplicationPlugins\JArchitecture.bundle" -Recurse
```

### CMD에서 확인 (참고)

```cmd
dir "%ProgramData%\Autodesk\ApplicationPlugins" /s
```

---

## 5. 주의사항

1. **PowerShell vs CMD 환경변수 문법**
   - PowerShell: `$env:ProgramData`, `$env:AppData`
   - CMD: `%ProgramData%`, `%AppData%`

2. **ProgramData 경로는 관리자 권한 필요**
   - `C:\ProgramData\` 하위에 파일을 쓰려면 관리자 권한이 필요합니다.
   - Inno Setup에서 `PrivilegesRequired=admin`으로 설정되어 있습니다.

3. **Bundle에 Excel 파일 포함 필수**
   - ISS의 `[Files]` 섹션에서 Excel 파일이 `{app}\Contents\Excel`로 복사되도록 설정되어 있어야 합니다.
   - 새로운 파일을 추가할 경우 ISS 파일도 함께 업데이트해야 합니다.

---

## 6. 빌드 구성(Debug/Release)과 네이티브 DLL 의존성

배포 패키지에 들어가는 DLL은 **반드시 Release 산출물**이어야 한다. 특히 네이티브 C++ DLL은 Debug 빌드를 배포하면 고객 PC에서 로드 실패한다.

### 프로젝트별 빌드 매트릭스

| DLL | 프로젝트 종류 | 배포 빌드 | `.iss` 소스 경로 |
|---|---|---|---|
| `JArchLicense.dll` | **네이티브 C++** (`.vcxproj`) | **Release\|x64 필수** | `..\JArchLicense\bin\x64\Release\JArchLicense.dll` (ISPP `{#LicenseDll}`) |
| `PipeLoad.dll` | 관리 .NET (`.csproj`, net8.0) | Release\|x64 | `C:\Jarch25\x64\Release\net8.0-windows\PipeLoad.dll` (ISPP `{#PipeLoadDll}`) |
| `Acadv25JArch.dll`·`EPPlus.dll`·`DuctSizing.Core.dll` | 관리 .NET | Release | `C:\Jarch25\Release\` (ISPP `{#MainDir}`) |

### 출력 경로 분리 (2026-07-28)

`Acadv25JArch.csproj` 는 원래 Debug/Release 모두 `C:\Jarch25\` 로 출력해서 **경로만 봐서는 Debug 산출물인지 알 수 없었고**, Release 구성마저 `Optimize=False` + `DebugType=full` 이라 이름만 Release 인 Debug 빌드였다. 다음과 같이 분리했다.

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <OutputPath>C:\Jarch25\</OutputPath>          <!-- 개발용 NETLOAD 경로 (기존과 동일) -->
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <OutputPath>C:\Jarch25\Release\</OutputPath>  <!-- 배포용 - 인스톨러가 참조 -->
</PropertyGroup>
```

Release 구성은 `Optimize=True` / `DebugType=portable` 로 교정. 실제로 산출물이 작아진다(예: `Acadv25JArch.dll` 718KB → 662KB, pdb 1.6MB → 271KB).

| 용도 | 경로 | 빌드 |
|---|---|---|
| 개발 `NETLOAD` | `C:\Jarch25\Acadv25JArch.dll` | `dotnet build -c Debug` (VS 기본) |
| 배포(인스톨러) | `C:\Jarch25\Release\Acadv25JArch.dll` | `dotnet build -c Release` / VS 구성 `Release` |
| Excel 데이터 | `C:\Jarch25\Excel\` | **빌드 산출물 아님** — 수동 관리, 구성 무관 |

### 연결 빌드 — 한 번 빌드하면 두 구성 모두 갱신

구성을 바꿔 두 번 빌드하는 걸 잊지 않도록, 어느 쪽을 빌드하든 반대편도 이어서 빌드한다(양방향).

```xml
<PropertyGroup>
  <BuildBothConfigurations Condition="'$(BuildBothConfigurations)'==''">true</BuildBothConfigurations>
  <_OtherConfiguration Condition="'$(Configuration)'=='Debug'">Release</_OtherConfiguration>
  <_OtherConfiguration Condition="'$(Configuration)'=='Release'">Debug</_OtherConfiguration>
</PropertyGroup>

<Target Name="BuildOtherConfiguration" AfterTargets="Build"
        Condition="'$(BuildBothConfigurations)'=='true' AND '$(ChainedBuild)'!='true' AND '$(_OtherConfiguration)'!=''">
  <Message Importance="high" Text="[연결 빌드] $(_OtherConfiguration) 구성도 함께 빌드합니다..." />
  <MSBuild Projects="$(MSBuildProjectFullPath)" Targets="Build"
           Properties="Configuration=$(_OtherConfiguration);Platform=$(Platform);ChainedBuild=true" />
</Target>
```

- 무한 재귀 방지는 `ChainedBuild=true` — 이어붙인 쪽 빌드에만 전달되므로 그쪽은 다시 연결하지 않는다.
- 전체 재빌드 기준 약 2배(측정값 7.6초). 반복 빌드가 부담되면 `dotnet build -p:BuildBothConfigurations=false`.
- Release 빌드가 실패하면 Debug 빌드도 실패로 보고된다 — Release 전용 오류를 조기에 잡는 이점과, 개발 중 빌드가 막히는 단점이 함께 있다.

`.iss` 는 네 소스(`{#MainDir}`·`{#LicenseDll}`·`{#PipeLoadDll}` + Excel) 모두에 대해 컴파일 시점 `FileExists` 검사를 하므로, **Release 빌드를 빠뜨리면 인스톨러 컴파일이 실패**한다.

### 왜 네이티브 DLL은 Release 필수인가

- 네이티브 C++ DLL의 **Debug 빌드는 Debug CRT**(`VCRUNTIME140D.dll`, `ucrtbased.dll`)에 링크된다.
- **Debug CRT는 Microsoft가 재배포를 금지** — VC++ 재배포 패키지에도 없고, **Visual Studio 설치 PC에만** 존재한다.
- 따라서 고객 PC(AutoCAD 2025만 설치)에서는 의존성 부재로 `JArchLicense.dll` 로드 실패 → `MyPlugin.Initialize()`의 `CheckLicense()` P/Invoke 예외 → **"라이선스 확인 오류"** 표시 + 플러그인 비활성화.
- **Release CRT**(`VCRUNTIME140.dll` + `api-ms-win-crt-*` → `ucrtbase.dll`)는 AutoCAD 2025(VC++ 재배포)·Windows 10/11 OS가 이미 제공 → 추가 설치 없이 로드된다.
- 관리 .NET DLL은 CRT에 링크되지 않으므로 Debug여도 로드는 되지만, 배포는 Release로 통일한다.

### 배포 전 의존성 점검 (네이티브 DLL)

```powershell
# dumpbin은 VS와 함께 설치됨 (예: ...\VC\bin\dumpbin.exe)
dumpbin /dependents "C:\Jarch25\JArchLicense.dll"
```

- 출력에 **`...140D.dll` / `ucrtbased.dll` 같은 `D` 접미사가 있으면 Debug 빌드** → 재빌드 필요.
- `VCRUNTIME140.dll`(D 없음) + `api-ms-win-crt-runtime-l1-1-0.dll` 만 나오면 정상.

### 빌드 & 인스톨러 재빌드 절차

```powershell
# 1) JArchLicense (네이티브) Release|x64  — .iss 가 이 경로를 직접 참조하므로 복사 불필요
$msbuild = (& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -find MSBuild\**\Bin\MSBuild.exe)
& $msbuild "JArchLicense\JArchLicense.vcxproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild

# 2) PipeLoad (관리) Release|x64  (BaseOutputPath = C:\Jarch25)
dotnet build "PipeLoad\PipeLoad.csproj" -c Release -p:Platform=x64

# 3) 메인 DLL — Release 출력은 C:\Jarch25\Release\ (Debug 경로를 건드리지 않음)
#    VS 에서는 구성 드롭다운을 Release 로 바꿔 빌드해도 동일하다
dotnet build "Acadv25JArch\Acadv25JArch.csproj" -c Release

# 4) 인스톨러 컴파일 (Inno Setup)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "JArchitecture_Setup\JArchitecture_Setup.iss"
# → Output\JArchitecture_Setup_MM_DD.exe  (날짜 자동 부여, 수동 rename 불필요)
```

### `.iss` 작성 주의

- **소스 경로는 항상 `...\Release\...`를 가리키도록 고정** — Debug 산출물이 배포본에 섞이는 사고 방지. 별도 출력 경로를 갖는 프로젝트(`JArchLicense`·`PipeLoad`)는 ISPP `#define` 으로 Release 경로를 박아두고, `#if !FileExists(...)` + `#error` 로 **빌드 누락 시 컴파일이 실패**하게 해 뒀다.
- 같은 파일을 중복 `Source:` 등록하지 말 것 (덮어쓰기라 무해하지만 혼란 유발).
- 산출물 파일명은 `OutputBaseFilename={#AppName}_Setup_{#BuildStamp}` 로 `GetDateTimeString('mm/dd','_','')` 날짜가 자동으로 붙는다 — 손으로 rename 하지 말 것. (단 `AppVersion` 은 여전히 수동이라, 기능 변경 시 `#define AppVer` 를 같이 올려야 설치 후 버전 구분이 된다.)

### ⚠ Inno 의 레지스트리는 32비트 뷰(WOW6432Node)에 있다

설치 프로그램은 32비트 프로세스이므로, `[Code]` 의 `RegQueryStringValue(HKLM, 'Software\Microsoft\...')` 는 실제로 **`HKLM\Software\WOW6432Node\Microsoft\...`** 를 읽는다.

```powershell
# ✗ 여긴 비어 있다 (64비트 뷰) — PowerShell 은 64비트라 기본이 이쪽
Test-Path 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\JArchitecture_is1'

# ✓ Inno 가 실제로 쓰고 읽는 곳
Test-Path 'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\JArchitecture_is1'
```

**64비트 뷰만 보고 "등록 항목이 없다"고 판단하면 진단이 통째로 어긋난다** (실제로 한 번 그렇게 오진했다). 설치 상태를 확인할 땐 반드시 `WOW6432Node` 쪽을 볼 것.

### 고아 언인스톨 항목 → 재설치 무음 종료 (2026-07-28 수정)

**증상** — 설치 프로그램을 실행해 **[재설치]** 를 눌러도 진행 표시나 오류 없이 **그냥 창이 닫히고 아무것도 설치되지 않는다.**

**원인** — 번들 폴더를 손으로 지우면 `unins000.exe` 는 사라지는데 **레지스트리 등록 키는 남는다**(고아 항목). 그러면:

```
IsAlreadyInstalled()  →  True   (고아 키를 발견)
  ↓ [재설치] 클릭
Exec("...\unins000.exe")  →  파일이 없어 False
  ↓
Exit → Result = False (메시지 없음)
  ↓
InitializeSetup = False → 무음 종료, 설치 0건
```

**대응** — 언인스톨러 경로를 먼저 `FileExists` 로 확인해, 없으면 **지울 대상도 없는 것이므로 키만 정리하고 설치를 계속**한다. `Exec` 자체가 실패한 경우엔 반드시 이유를 알린다.

```pascal
UninstallString := RemoveQuotes(UninstallString);

if not FileExists(UninstallString) then
begin
  RegDeleteKeyIncludingSubkeys(HKLM, UninstallRegKey);
  Exit;                    // Result = True → 설치 계속
end;

if not Exec(UninstallString, '/SILENT /NORESTART', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
begin
  MsgBox('기존 버전 삭제 프로그램을 실행하지 못했습니다.' + ..., mbError, MB_OK);
  Result := False; Exit;
end;
```

> **원칙**: `InitializeSetup` 에서 `Result := False` 로 빠져나가는 모든 경로에 **반드시 안내 메시지를 붙일 것.** 메시지 없는 `Exit` 는 사용자에게 "실행했는데 아무 일도 안 일어남"으로만 보인다.

### 언인스톨러는 삭제 완료를 기다리지 않는다

Inno 언인스톨러(`unins000.exe`)는 자기 자신을 `%TEMP%\_iu*.tmp` 로 **복사해 재실행하고 원본 프로세스는 즉시 종료**한다. 따라서 아래처럼 써도 `Exec` 는 실제 삭제가 끝나기 전에 반환한다.

```pascal
Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
```

반환 직후 설치가 파일을 복사하면 복사와 백그라운드 삭제가 경쟁할 수 있다. `RunUninstaller()` 는 ① 등록 키 소멸(최대 60초) ② 번들 폴더 소멸(최대 20초) 을 폴링으로 확인한 뒤 반환한다.

### 설치 로그는 항상 켜 둔다

`[Setup]` 에 `SetupLogging=yes` 를 두면 실행할 때마다 `%TEMP%\Setup Log YYYY-MM-DD #NNN.txt` 가 자동 생성된다. `/LOG` 플래그를 사용자가 기억할 필요가 없어 원격 진단이 훨씬 쉬워진다.

### 진단 체크리스트

설치했는데 AutoCAD 에 안 뜰 때 순서대로 확인한다.

```powershell
# 1) 번들이 실제로 깔렸는가
Get-ChildItem "$env:ProgramData\Autodesk\ApplicationPlugins\JArchitecture.bundle" -Recurse -File |
  Select-Object Name, Length, LastWriteTime | Sort-Object LastWriteTime

# 2) 등록 항목 (반드시 WOW6432Node!)
Get-ItemProperty 'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\JArchitecture_is1'

# 3) 설치 로그
Get-ChildItem $env:TEMP -Filter 'Setup Log*.txt' | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# 4) AutoCAD 가 잡고 있는 경로
Get-ChildItem 'HKCU:\Software\Autodesk\AutoCAD' -Recurse -ErrorAction SilentlyContinue |
  Where-Object { $_.PSChildName -eq 'JArchitecture' } | ForEach-Object { (Get-ItemProperty $_.PSPath).LOADER }
```

### `[Code]` 주석에 중괄호 상수 금지

Pascal 의 `{ ... }` 주석은 **중첩되지 않는다**. 주석 안에 `{app}` 같은 Inno 상수를 쓰면 `}` 에서 주석이 조기 종료되어 `Syntax error` 가 난다. 상수를 언급해야 하면 `//` 줄 주석을 쓸 것.

### 설치/삭제 전 AutoCAD 실행 체크

`InitializeSetup` / `InitializeUninstall` 이 WMI(`Win32_Process`)로 `acad.exe` 를 검사해, 실행 중이면 중단한다.
AutoCAD 가 켜져 있으면 번들의 DLL 이 잠겨 교체되지 않고, **설치는 성공한 것처럼 보이는데 구버전이 계속 로드되는** 증상이 생기기 때문이다.

> 관련: `PackageContents.xml` 의 `LoadOnAutoCADStartup="True"` 때문에 번들이 설치돼 있으면 AutoCAD 시작 시 번들 DLL 이 자동 로드된다. 이 상태에서 `C:\Jarch25\` 로 개발 빌드 후 `NETLOAD` 해도 **이미 로드된 어셈블리가 우선**이라 반영되지 않는다 — 개발 중 변경을 확인하려면 AutoCAD 를 재시작하거나 번들을 제거해야 한다.

### 참고: .NET 8의 DllImport 네이티브 DLL 탐색

- 번들 자동 로드 구조라 `JArchLicense.dll`이 `Acadv25JArch.dll`과 **같은 `Contents\` 폴더**에 놓인다.
- .NET Core/8은 `DllImport` 해석 시 **P/Invoke를 선언한 관리 어셈블리의 디렉터리를 탐색**(`Assembly.Location` 기준)하므로, 같은 폴더 co-location만으로 로드된다 — `SetDllDirectory`/`SetDllImportResolver` 불필요.
- (주의: .NET **Framework**는 어셈블리 디렉터리를 탐색하지 않았다. 이 프로젝트는 net8.0이라 해당 없음.)
- 단, `JArchLicense.dll` **자신의** 네이티브 의존성(VC++ 런타임 등)은 어셈블리 디렉터리가 아니라 OS 로더(acad.exe·시스템·PATH) 기준으로 찾으므로, 위 Release CRT 조건이 필요하다.
