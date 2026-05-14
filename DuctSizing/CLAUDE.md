# DuctSizing

> 풍량(CMH) → 등가직경 De → 사각덕트(b×a) 산정 WinForms 앱 (.NET 8)
> 원본 엑셀 `DUCT_MEASURE_1204.xls` 의 D18·F25 수식 기반

## Obsidian
- 저장 위치: `D:\Dropbox\Obsidian\Projects\DuctSizing\`

## QMD 키워드
- DuctSizing, 등가직경, De, Huebscher, 사각덕트, 풍량, CMH, mmAq, WinForms, 환산표

## Build / Run
- 요구: .NET 8 SDK, Windows
- 빌드: `dotnet build`
- 실행: `dotnet run --project .`

## 진입점 문서
- `DuctSize_Design_1.md` — 셀 매핑, 공식, 알고리즘 (구현 기준)

## Conventions
- **평탄 배치**: DuctSizing/ 루트에 모든 .cs 파일, 서브폴더 없음
- **표준치수**: 50mm 간격, 100~2000mm (`StandardSizes.Default`)
- **De_eq 허용 범위**: `[De, De × 1.05]` (+5% 상한, `MainForm.DeUpperRatio`)
- **Mode A**: 단변 지정 → 범위 내 모든 장변 a 후보
- **Mode B**: 모든 (b,a) 조합 — b 오름차순, De_eq 범위 내 모든 a, `a/b ≤ aspectMax`
- **공식 출처**: 모든 계산은 `DuctSize_Design_1.md` §5
- **환산표 미사용**: 데이터 임베드하지 않고 Huebscher 공식만 사용 (§4.0)
- **원형 D**: De 이상의 가장 작은 표준치수 (`StandardSizes.NextAtLeast`)
- **DataGridView**: 컬럼 헤더 클릭 시 오름차순↔내림차순 토글 (수동 sort)

## 검증 케이스 (§7.1)
- Q=13,000 / R=0.08 / α=1.01 / b=500
- 기대: De=763, A=0.457, V=7.897, Δp=3.805, R′=0.084, (b,a)=(500,1050), De′=779, a/b=2.10
