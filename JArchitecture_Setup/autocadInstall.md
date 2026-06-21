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
| `JArchLicense.dll` | **네이티브 C++** (`.vcxproj`) | **Release\|x64 필수** | `C:\Jarch25\JArchLicense.dll` |
| `PipeLoad.dll` | 관리 .NET (`.csproj`, net8.0) | Release\|x64 | `C:\Jarch25\x64\Release\net8.0-windows\PipeLoad.dll` |
| `Acadv25JArch.dll` 등 | 관리 .NET | Release(또는 Debug 로드 가능) | `C:\Jarch25\` |

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
# 1) JArchLicense (네이티브) Release|x64
$msbuild = (& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -find MSBuild\**\Bin\MSBuild.exe)
& $msbuild "JArchLicense\JArchLicense.vcxproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild
Copy-Item "JArchLicense\bin\x64\Release\JArchLicense.dll" "C:\Jarch25\JArchLicense.dll" -Force

# 2) PipeLoad (관리) Release|x64  (BaseOutputPath = C:\Jarch25)
dotnet build "PipeLoad\PipeLoad.csproj" -c Release -p:Platform=x64

# 3) 인스톨러 컴파일 (Inno Setup)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "JArchitecture_Setup\JArchitecture_Setup.iss"
# → Output\JArchitecture_Setup.exe
```

### `.iss` 작성 주의

- **소스 경로는 항상 `...\Release\...`를 가리키도록 고정** — Debug 산출물이 배포본에 섞이는 사고 방지.
- 같은 파일을 중복 `Source:` 등록하지 말 것 (덮어쓰기라 무해하지만 혼란 유발).

### 참고: .NET 8의 DllImport 네이티브 DLL 탐색

- 번들 자동 로드 구조라 `JArchLicense.dll`이 `Acadv25JArch.dll`과 **같은 `Contents\` 폴더**에 놓인다.
- .NET Core/8은 `DllImport` 해석 시 **P/Invoke를 선언한 관리 어셈블리의 디렉터리를 탐색**(`Assembly.Location` 기준)하므로, 같은 폴더 co-location만으로 로드된다 — `SetDllDirectory`/`SetDllImportResolver` 불필요.
- (주의: .NET **Framework**는 어셈블리 디렉터리를 탐색하지 않았다. 이 프로젝트는 net8.0이라 해당 없음.)
- 단, `JArchLicense.dll` **자신의** 네이티브 의존성(VC++ 런타임 등)은 어셈블리 디렉터리가 아니라 OS 로더(acad.exe·시스템·PATH) 기준으로 찾으므로, 위 Release CRT 조건이 필요하다.
