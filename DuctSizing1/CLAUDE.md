# DuctSizing1

> `DuctSizing/` 의 사본 — 두 가지 변형이 누적된 폴더
> 1) Mode B 출력 표에서 **De′ 컬럼 숨김** 처리
> 2) **Mode D 탭** 추가 (Q 100~20,000 CMH, 100 간격 일괄 출력)
> 그 외 계산 로직·라이브러리 의존성은 `DuctSizing/` 와 동일 — 원본은 수정하지 말 것

## 원본(`DuctSizing/`)과의 차이
- `OnModeChanged` 끝에 `colDeEq.Visible = rbModeA.Checked` — Mode B 시 De′ 컬럼 숨김
- **Mode D 탭** 추가
  - 입력: 단변 최소(`numBMin`, 기본 200), 단변 최대(`numBMax`, 기본 500). α는 단건 탭의 `numAlpha` 재사용. `aspectMax = 1.5` 고정
  - 동작: `Q = 100..20000 step 100` 각 행마다 Return·Supply 양쪽 `ModeD` 실행 → `(b, a)` 출력
  - 그리드: `dgvModeD` (컬럼 `Q | Return b/a | Supply b/a`)
- 그리드 컬럼은 **코드에서 초기화** (`InitializeGridColumns()` in `MainForm.cs`)
  - 이유: VS WinForms Designer가 Designer.cs 재생성 시 코드로 추가된 컬럼·`Columns.Add` 호출을 제거함 → 런타임에서 안전하게 컬럼 보장
- ComboBox 기본 선택 보강: `EnsureComboDefaults()` — `cmbDuctType.SelectedIndex = 0` 누락 방지
  - 이유: Designer 재생성 시 `SelectedIndex = 0` 자주 누락 → `(DuctType)(-1)` 캐스트로 런타임 에러
- 용어: UI 라벨/콤보 항목은 **Return / Supply** (이전 흡입/토출)

## Obsidian
- 저장 위치: `D:\Dropbox\Obsidian\Projects\DuctSizing1\`

## QMD 키워드
- DuctSizing1, DuctSizing 사본, De 컬럼 숨김, Mode D, Return, Supply

## 프로젝트 구성
```
Acadv25JArch/
├── DuctSizing.sln
├── DuctSizing/          ← 원본 (그대로 유지)
├── DuctSizing1/         ← 본 프로젝트 (De′ 숨김 + Mode D 탭)
└── DuctSizing.Core/     ← 공유 로직 라이브러리 (ProjectReference)
```

## Build / Run
- 솔루션 빌드: `dotnet build DuctSizing.sln`
- 실행: `dotnet run --project DuctSizing1` (Acadv25JArch/ 에서)

## VS Designer 작업 시 주의
- `MainForm.Designer.cs` 를 VS Designer로 열어 저장하면 다음이 자주 사라짐:
  - `DataGridView` 컬럼 `new()` / `Columns.Add(...)` 호출
  - `ComboBox.SelectedIndex` 기본값
- 이는 `MainForm.cs` 의 `InitializeGridColumns()` / `EnsureComboDefaults()` 에서 런타임에 다시 채워주므로 빌드·실행은 정상
- 단, Designer가 ClientSize/MinimumSize/AutoScaleDimensions를 사용자 DPI에 맞춰 재계산할 수 있으니 폼 크기 비정상이면 그 값부터 점검

## 공유 사항
- 계산 로직(`DuctSizing.Core`)·표준치수·`DuctType`·De_eq +10% 범위·검증 케이스 모두 원본과 동일
- Simul 탭: 각 (Q, 덕트타입) 노드당 Mode B 조합 **최대 10개**까지만 표시 (`MainForm.SimulMaxCombos`)
- 자세한 계산 규약은 `../DuctSizing/CLAUDE.md` 참조
