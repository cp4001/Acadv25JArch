# DuctSizing

> 풍량(CMH) → 등가직경 De → 사각덕트(b×a) 산정 WinForms 앱 (.NET 8)
> 원본 엑셀 `DUCT_MEASURE_1204.xls` 의 D18·F25 수식 기반
> 계산 로직은 별도 라이브러리 `DuctSizing.Core` 로 분리됨 (다른 프로그램에서도 참조용)

## Obsidian
- 저장 위치: `D:\Dropbox\Obsidian\Projects\DuctSizing\`

## QMD 키워드
- DuctSizing, 등가직경, De, Huebscher, 사각덕트, 풍량, CMH, mmAq, WinForms, 환산표

## 프로젝트 구성
```
Acadv25JArch/
├── DuctSizing.sln              ← 솔루션 (두 프로젝트 묶음)
├── DuctSizing/                  ← WinForms 앱 (Mode A/B UI만)
│   └── DuctSizing.csproj         (net8.0-windows, ProjectReference로 .Core 참조)
└── DuctSizing.Core/             ← 로직 라이브러리 (Mode A/B/C 모두)
    └── DuctSizing.Core.csproj    (net8.0, 다른 프로그램 재사용 가능)
```

## Build / Run
- 요구: .NET 8 SDK, Windows
- 솔루션 빌드: `dotnet build DuctSizing.sln` (Acadv25JArch/ 에서)
- WinForms 실행: `dotnet run --project DuctSizing` (Acadv25JArch/ 에서)
- 빌드 산출물: `DuctSizing/bin/Debug/net8.0-windows/` 에 `DuctSizing.exe` + `DuctSizing.Core.dll` 함께 배치됨

## 진입점 문서
- `DuctSize_Design_1.md` — 셀 매핑, 공식, 알고리즘 (구현 기준)

## 라이브러리 공개 API (`DuctSizing.Core`)
- `DuctSizingCalculator.ModeA(q, type, alpha, b)` — 단변 지정, 범위 내 모든 장변 후보
- `DuctSizingCalculator.ModeB(q, type, alpha, aspectMax)` — 모든 (b,a) 조합
- `DuctSizingCalculator.ModeC(q, type, alpha, aspectMax)` — Mode B 중 De_eq 최소 1개 (단일 출력)
- `type`: `DuctType.Suction` (흡입, R=0.08) 또는 `DuctType.Discharge` (토출, R=0.10) — 두 값만 허용
- 모두 `DuctSizingResult` 반환 — `{De, D, Aux, Combinations}`
- 보조 클래스 `StandardSizes`, `EquivalentDiameter`, `HuebscherFormula`, `AuxiliaryCalculator` 등은 public이지만 일반적으로 facade만 호출

## Conventions
- **표준치수**: 50mm 간격, 100~2000mm (`StandardSizes.Default`)
- **De_eq 허용 범위**: `[De, De × 1.10]` (+10% 상한, `DuctSizingCalculator.DeUpperRatio`)
- **Mode A**: 단변 지정 → 범위 내 모든 장변 a 후보
- **Mode B**: 모든 (b,a) 조합 — b 오름차순, De_eq 범위 내 모든 a, `a/b ≤ aspectMax`
- **Mode C**: Mode B 결과 중 `De_eq` 가장 작은 1개 (동률이면 `Area` 더 작은 쪽)
- **WinForms UI**: Mode A·B만 노출. Mode C는 라이브러리 전용 (다른 프로그램에서 사용)
- **공식 출처**: 모든 계산은 `DuctSize_Design_1.md` §5
- **환산표 미사용**: 데이터 임베드하지 않고 Huebscher 공식만 사용 (§4.0)
- **원형 D**: De 이상의 가장 작은 표준치수 (`DuctSizingResult.D`)
- **DataGridView**: 컬럼 헤더 클릭 시 오름차순↔내림차순 토글 (수동 sort, MainForm 내)

## 검증 케이스 (§7.1)
- Q=13,000 / R=0.08 / α=1.01 / b=500
- 기대: De=763, A=0.457, V=7.897, Δp=3.805, R′=0.084, (b,a)=(500,1050), De′=779, a/b=2.10
