# DuctSizeApp

`DuctSizeDesign.md` / `DuctDesignAPI.md`를 구현한 C# WinForm 도구.

## 빌드 / 실행

### Visual Studio (권장)

`DuctSize/DuctSizeApp.sln` 더블클릭 → F5 (디버그 실행) 또는 Ctrl+F5 (디버그 없이 실행).
`DuctSizeApp` 프로젝트가 시작 프로젝트로 자동 설정됨.

### CLI

```pwsh
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\DuctSize
dotnet build DuctSizeApp.sln -c Release
dotnet run --project DuctSizeApp/DuctSizeApp.csproj -c Release
```

빌드 산출물: `DuctSizeApp/bin/Release/net8.0-windows/DuctSizeApp.exe`

## 입력

| 필드 | 단위 | 기본 | 비고 |
|------|------|------|------|
| 덕트 길이 L | m | 30 | (0, 1000] |
| 요구 풍량 Q | CMH | 13000 | (0, 500,000] |
| 허용 손실율 (p/L) | mmAq/m | 0.08 | (0, 5] — 단위 길이당 마찰손실 |
| 여유율 SF | – | 1.00 | [0.5, 1.5] |
| 조도 ε | mm | 0.09 | 갈바나 강판 |
| De 허용오차 (옵션 D) | – | 0.05 | (0, 0.5] |
| ASP 상한 (옵션 D) | – | 3.0 | [1, 10] |

## 출력

- **옵션 A**: 단일 (a, b, De, V, R, ΔP_actual, ASP)
  - 자유도 잠금 = 정사각 가정으로 a 초기 추정 → 표준 시리즈 스냅 → ASP > 3 시 a 한 단계 ↑
- **옵션 D**: 후보 테이블 (ASP 작은 순 정렬)
  - 표준 (a, b) 조합 중 De 오차 ≤ tol & ASP ≤ asp_max 만족 셀 모두

## 코드 구성

```
DuctDesign.cs   ← DuctTables, DuctMath, DuctDesign(OptionA/OptionD), DuctDesignResult
MainForm.cs     ← WinForm UI
Program.cs      ← 엔트리 포인트
Tests/
  SmokeTest.cs       ← 콘솔 검증 (CLI에서 실행)
  SmokeTest.csproj
```

## 검증 (SmokeTest)

SmokeTest는 솔루션에 포함되지 않은 콘솔 검증 도구다. CLI에서만 실행:

```pwsh
cd C:\Users\junhoi\Desktop\Work\Acadv25JArch\DuctSize\DuctSizeApp\Tests
dotnet run --project SmokeTest.csproj -c Release
```

기준 케이스 `(L=30, Q=13000, ΔP=2.4, SF=1.0)` 기대값:
- 옵션 A → `700 × 700` (정사각, ASP 1.00, De ≈ 765 mm)
- 옵션 D → 22개 후보, 그 중 `1050 × 500` (엑셀 시나리오 1 결과)이 ASP 2.10 위치에 포함

> **참고**: 엑셀의 시나리오 1 결과(`1050 × 500`)는 사용자가 추가로 "단변 = 500" 을 입력한 케이스다. 본 도구의 옵션 A는 사용자가 한 변을 지정하지 않으므로 자동으로 가장 정사각에 가까운 후보를 선택한다 — 형상을 강제하고 싶다면 옵션 D 표에서 직접 선택.

## 핵심 공식

- **Huebscher 등가원형**: `De = 1.30 · (a·b)^0.625 / (a+b)^0.25`
- **마찰선도** (Darcy-Weisbach + Altshul-Tsal):
  - `f = 0.11 · (68/Re + ε/D)^0.25` (난류, `f < 0.018` 시 보정)
  - `R [mmAq/m] = f · ρ · V² / (2·D) / g`
- **기본 상수**: ρ=1.2 kg/m³, μ=1.81e-5 Pa·s, ε=0.09 mm, g=9.80665 m/s²

자세한 알고리즘은 `../DuctDesignAPI.md` 참조.
