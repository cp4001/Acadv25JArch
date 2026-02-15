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
