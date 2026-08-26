# XdataSet — AutoCAD Xdata 기록 모듈 (JArch)

> **목적**: 선택/지정한 AutoCAD 엔티티에 Xdata(확장 데이터)를 기록하는 재사용 모듈.
> 기록 로직은 네이티브 ARX(`JArchXData.arx`)에 있고, C# 래퍼(`JArchXDataNet.dll`)로 호출한다.
> 사용기한 라이선스 검사(인터넷 시각 기반)가 ARX 내부에 내장되어 있다.
>
> **다른 Addin에서 이 기능이 필요하면 새로 만들지 말고 이 모듈을 재사용할 것.**

---

## 언제 이 모듈을 쓰나 (AI 판단 기준)

다음 요구가 나오면 이 모듈을 사용/참조한다.

- "엔티티에 Xdata를 기록", "RegApp 이름으로 문자열 값 저장", "도면 객체에 태그/메타데이터 심기"
- "Xdata.Set(regname, value)" 형태의 호출
- "사용기한/라이선스 만료를 코드에 숨기고 싶다", "인터넷 시간으로 만료 체크"

**핵심 계약**: `regName`, `value` 둘 다 `string`. 호출 한 번에 지정한 `(regName=value)` **와 함께 라이선스 표식 `(JLicense=JJH)` 이 항상 같이 기록**된다.

**진입점이 두 개다. 호출자 상황에 맞는 쪽을 골라야 한다.**

| 상황 | 쓸 API |
|---|---|
| ObjectId 만 있고 객체가 **닫혀** 있다 | `Xdata.Set(ObjectId, ...)` |
| Transaction 안에서 `GetObject(ForWrite)`/`UpgradeOpen` 으로 **이미 열어** 뒀다 | **`Xdata.SetOpen(DBObject, ...)`** |
| DB 에 아직 추가 안 한 **신규 엔티티**(ObjectId 가 Null) | **`Xdata.SetOpen(DBObject, ...)`** |

`Set` 은 내부에서 `acdbOpenObject(kForWrite)` 를 하므로, 이미 열린 객체에 쓰면 **`eWasOpenForWrite` 예외**가 난다. .NET Transaction 을 쓰는 코드는 거의 전부 `SetOpen` 쪽이다.

---

## 구성 파일

| 파일 | 형식 | 역할 |
|---|---|---|
| `JArchXData/JArchXData.cpp` | C++ (ObjectARX 2025) | 실제 Xdata 기록 + 라이선스 검사, export 함수 |
| `JArchXData/JArchXData.vcxproj` | MSBuild vcxproj | ARX 빌드 (Release/x64) → `JArchXData.arx` |
| `JArchXDataNet/Xdata.cs` | C# | P/Invoke 래퍼 (`JArch.Xdata`) |
| `JArchXDataNet/Commands.cs` | C# | `Startup`(자동 로드) + `SETFCD` 예제 명령 |
| `JArchXDataNet/JArchXDataNet.csproj` | .NET SDK | C# 빌드 (net8.0-windows) → `JArchXDataNet.dll` |
| `build.bat` | cmd 배치 | ARX+C# 빌드 후 `.arx`를 DLL 폴더로 복사 |

---

## C# API (이걸 호출한다) — namespace `JArch`

```csharp
public static class Xdata
{
    // 엔티티에 (1001.regName)(1000.value) + (1001."JLicense")(1000."JJH") 기록.
    // 같은 regName 의 기존 Xdata 는 교체, 다른 앱의 Xdata 는 유지.
    // 사용기한이 지났으면 아무것도 안 쓰고 조용히 반환(예외 없음).
    // 실패 시 Autodesk.AutoCAD.Runtime.Exception 던짐.
    // ※ 문서 잠금 상태(CommandMethod 안 등)에서 호출할 것.
    // ※ 네이티브가 kForWrite 로 직접 여므로, 이미 열린 객체엔 쓰면 안 된다.
    public static void Set(ObjectId id, string regName, string value);

    // Set 과 기록 내용 동일. 열기/닫기를 하지 않고 setXData 만 수행한다.
    // 이미 쓰기로 열린 객체 / DB 미등록 신규 엔티티용.
    // 열림 상태 보장은 호출자 책임(안 열려 있으면 eNotOpenForWrite 예외).
    public static void SetOpen(DBObject obj, string regName, string value);

    // 라이선스 정보 조회. 최초 호출 시 인터넷 시각을 1회 확정(이후 Set 은 재사용).
    public static LicenseInfo GetInfo();

    // JArchXData.arx 자동 로드(이미 로드돼 있으면 무시).
    public static void EnsureArxLoaded();

    // 활성 문서 명령창에 만료일 배너 출력.
    public static void ShowBanner();
}

public sealed class LicenseInfo
{
    public DateTime NowUtc;       // 확정된 현재 시각(UTC)
    public DateTime EndDateUtc;   // 만료일(UTC) — 네이티브에서 옴
    public bool     FromInternet; // true=인터넷 시각, false=로컬 폴백
    public bool     Expired;      // true=사용기한 지남
    public DateTime EndDate { get; } // 표시용 로컬 날짜(시간 제거)
}
```

### 자동 로드 (NETLOAD 시)

`Commands.cs` 에 `[assembly: ExtensionApplication(typeof(JArch.Startup))]` 가 있어,
**이 어셈블리를 직접 NETLOAD** 하면 `Startup.Initialize()` 가 자동 실행되어:
1. `JArchXData.arx` 로드
2. 만료일 배너를 명령창에 출력

**주의**: 다른 메인 Addin이 이 DLL을 *참조로만* 쓰면 `Startup.Initialize()` 는 자동 호출되지 않는다.
그 경우 메인 Addin의 `IExtensionApplication.Initialize()` 안에서 아래 두 줄을 직접 호출한다.

```csharp
JArch.Xdata.EnsureArxLoaded();
JArch.Xdata.ShowBanner();
```

---

## 사용 예

```csharp
[CommandMethod("SETFCD")]
public void SetFcd()
{
    var doc = Application.DocumentManager.MdiActiveDocument;
    var ed  = doc.Editor;

    var res = ed.GetSelection();
    if (res.Status != PromptStatus.OK) return;

    foreach (SelectedObject so in res.Value)
        JArch.Xdata.Set(so.ObjectId, "Group", "FCD");  // regName, value 둘 다 string
}
```

검증(LISP):
```lisp
(entget (car (entsel)) '("JLicense" "Group"))
;; => (... (-3 ("JLicense" (1000 . "JJH")) ("Group" (1000 . "FCD"))))
```

---

## 네이티브 export (P/Invoke 대상) — `JArchXData.arx`

| export | 시그니처 | 반환 |
|---|---|---|
| `JArchXDataSet` | `int __cdecl (void* objIdPtr, const wchar_t* regName, const wchar_t* value)` | `Acad::ErrorStatus` (0=eOk) |
| `JArchXDataSetEnt` | `int __cdecl (void* pEntPtr, const wchar_t* regName, const wchar_t* value)` | `Acad::ErrorStatus` (0=eOk) |
| `JArchGetLicenseInfo` | `int __cdecl (SYSTEMTIME* nowUtc, SYSTEMTIME* endUtc, int* fromInternet)` | 0=사용가능, 1=만료 |
| `acrxEntryPoint` / `acrxGetApiVersion` | ARX 표준 | — |

- `objIdPtr` = C#의 `ObjectId.OldIdPtr` (= `AcDbStub*`) — 네이티브가 `acdbOpenObject(kForWrite)` 후 `close()`
- `pEntPtr` = C#의 `DBObject.UnmanagedObject` (= `AcDbEntity*`) — **열지도 닫지도 않고** `setXData` 만 수행
- 두 export 는 기록 본체 `writeXData()` 를 공유하므로 기록 내용·만료 동작이 항상 같다
- 호출 규약 `__cdecl`, 문자열 `wchar_t*`(UTF-16). C#에서 `CallingConvention.Cdecl`, `CharSet.Unicode`.
- **acad.exe 프로세스 내에서만 동작** (외부 콘솔 앱 P/Invoke 불가).

---

## 라이선스/보안 동작 (ARX 내부)

- **현재 시각**: 인터넷 HTTPS 응답 `Date` 헤더(WinHTTP, 호스트 microsoft→google→cloudflare, 각 3초). 실패 시 로컬 시계 폴백.
- **만료일**: 2027-04-01 (UTC). 소스에서 XOR 은닉 → `strings` 로 안 보임.
- **롤백 방어**: 관측한 최신 시각을 레지스트리 `HKCU\Software\Microsoft\Windows\CurrentVersion\Ext\Stat\h` 에 XOR 8바이트로 저장. 시계를 과거로 돌려도 이 값 밑으로 못 내려감.
- **세션당 1회**: 최초 호출에서 시각을 확정·캐시. 이후 `Set` 은 네트워크를 다시 타지 않음.
- **만료 시**: `Set` 은 아무것도 기록하지 않고 `eOk` 로 조용히 반환(에러/메시지 없음). → `JLicense` 표식이 없는 결과물 = 만료 상태 산출물.

> 이 상수(만료일/레지스트리 경로)를 바꾸려면 **C++ 소스만** 수정하고 재빌드한다. C# 쪽엔 날짜가 없다(의도적 은닉).

---

## 빌드 & 배포

```bat
build.bat
```
- `%~dp0` 기준 상대경로 → **폴더째 이동해도 동작**(같은 PC).
- 3단계: ARX 빌드 → C# 빌드 → `.arx` 를 DLL 폴더로 복사.
- 산출물: `JArchXDataNet/bin/Release/net8.0-windows/` 에 `JArchXDataNet.dll` + `JArchXData.arx`.

**배포 규칙**
- `.arx` 와 `.dll` 을 **같은 폴더**에 둔다(`EnsureArxLoaded` 가 자기 폴더에서 `.arx` 를 찾음). 다른 폴더면 `LoadModule` 에 전체 경로 지정.
- **`.pdb` 는 배포 금지** (네이티브 함수명·소스경로 복원 방지 → 보안).
- ARX 는 **Release/x64** 만 유효.

**환경 의존(다른 PC로 옮길 때)**
- `JArchXData.vcxproj`: ObjectARX SDK 경로 `C:\Autodesk\ObjectARX_2025` (환경변수 `ARXSDK` 로 오버라이드 가능).
- `JArchXDataNet.csproj`: AutoCAD 참조 `C:\Program Files\Autodesk\AutoCAD 2025\` (환경변수 `AcadDir` 로 오버라이드 가능). 참조는 `Private=false`(복사 안 함).

---

## 다른 Addin에서 재사용하는 법

1. `JArchXData.arx` 와 `JArchXDataNet.dll` 을 배포 폴더에 함께 넣는다.
2. 방법 A — **wrapper를 직접 NETLOAD**: 로드 즉시 arx 자동 로드 + 만료 배너 출력. `JArch.Xdata.Set(...)` 호출.
3. 방법 B — **메인 Addin이 `JArchXDataNet.dll` 을 참조**: 메인의 `Initialize()` 에서 `JArch.Xdata.EnsureArxLoaded(); JArch.Xdata.ShowBanner();` 를 부른 뒤, 필요한 곳에서 `JArch.Xdata.Set(id, regName, value)` 사용.
4. Xdata 기록은 반드시 **문서 잠금**이 걸린 컨텍스트(`[CommandMethod]` 내부, 또는 `doc.LockDocument()`)에서 호출한다.

---

## 적용 사례 — Acadv25JArch (2026-08-26)

`Acadv25JArch` 의 자체 Xdata 기록 함수 2개를 이 모듈로 전환했다. **방법 B(참조)** 를 사용한다.

### 전환 방식 — 호출부는 건드리지 않았다

호출부가 121곳(`JXdata.SetXdata` 117 + `.XdataSet()` 4)이라 전면 재작성 대신 **기존 함수 본체만 교체**했다.

```csharp
// Acadv25JArch/CadFunction.cs — 기존 시그니처 유지, 본체만 교체
public static void SetXdata(DBObject obj, string xName, string sdata)
{
    if (obj == null) return;
    JArch.Xdata.SetOpen(obj, xName, sdata);
}

// Acadv25JArch/jCadExtention.cs — 확장메서드도 동일
public static void XdataSet(this Entity ent, string regAppName, string value)
{
    if (ent == null) return;
    JArch.Xdata.SetOpen(ent, regAppName, value);
}
```

기존 본체에 있던 `MyPlugin.LicenseDate` (로컬 시계) 비교는 삭제. 만료 판정은 arx 가 전담한다.

### `SetOpen` 이 생긴 이유

이 전환 때문에 `JArchXDataSetEnt` / `SetOpen` 을 신설했다. 기존 `Set(ObjectId)` 로는 전환이 **불가능**했다.

| 충돌 | 내용 |
|---|---|
| 이미 열린 객체 | 호출부 대부분이 Transaction 안 `UpgradeOpen()` 상태 → `acdbOpenObject` 가 `eWasOpenForWrite` 반환 |
| 신규 엔티티 | DB 추가 전이라 `ObjectId` 가 Null → `eNullObjectId` |

### 남겨둔 것

`MyPlugin.LicenseDate` 와 `JArchLicense.dll` 은 **제거하지 않았다**. Xdata 기록 경로에서만 빠졌을 뿐,
시작 배너와 명령 차단(`RoomCalc.cs:144,449`)에는 계속 쓰인다. 두 라이선스 체계가 공존한다.

### 배포

- `Acadv25JArch.csproj` 가 `JArchXDataNet.csproj` 를 `ProjectReference` + `.arx` 를 `CopyToOutputDirectory` → `C:\Jarch25\` / `C:\Jarch25\Release\` 에 3개 파일이 함께 배치된다.
- `JArchitecture_Setup.iss` 의 `[Files]` 에 두 파일 추가 + `#error` 가드 추가 → 빌드 누락 시 인스톨러 컴파일이 실패한다.
- 빌드 순서: **`C++\build.bat` (arx+wrapper) → `dotnet build Acadv25JArch.csproj`** . 순서를 바꾸면 `.arx` 복사가 누락된다.

### 실행 시 유의

- **첫 Xdata 기록에서 최대 9초 멈춤** — arx 가 인터넷 시각을 확인(3 호스트 × 3초). 세션당 1회.
- **모든 Xdata 에 `JLicense=JJH` 가 함께 붙는다** — 전환 이전 도면과 달라지는 지점.
