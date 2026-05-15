# DuctSizing1

> `DuctSizing/` 의 사본 — Mode B 출력 표에서 **De′ 컬럼을 숨김** 처리하는 변형
> 그 외 동작·계산·라이브러리 의존성은 `DuctSizing/` 와 동일
> 원본은 수정하지 말 것 — 이 폴더만 독자적으로 수정

## 원본과의 차이
- `MainForm.OnCalcClick` 끝 부분에서 `colDeEq.Visible = isModeA;` 한 줄 추가
  - Mode A 결과 표: De′ 컬럼 표시
  - Mode B 결과 표: De′ 컬럼 숨김

## Obsidian
- 저장 위치: `D:\Dropbox\Obsidian\Projects\DuctSizing1\`

## QMD 키워드
- DuctSizing1, DuctSizing 사본, De 컬럼 숨김

## 프로젝트 구성
```
Acadv25JArch/
├── DuctSizing.sln
├── DuctSizing/          ← 원본 (그대로 유지)
├── DuctSizing1/         ← 본 프로젝트 (Mode B De′ 숨김 변형)
└── DuctSizing.Core/     ← 공유 로직 라이브러리 (ProjectReference)
```

## Build / Run
- 솔루션 빌드: `dotnet build DuctSizing.sln`
- 실행: `dotnet run --project DuctSizing1` (Acadv25JArch/ 에서)

## 공유 사항
- 계산 로직(`DuctSizing.Core`)·표준치수·DuctType·De_eq +10% 범위·검증 케이스 모두 원본과 동일
- Simul 탭: 각 (Q, 덕트타입) 노드당 Mode B 조합 **최대 10개**까지만 표시 (`MainForm.SimulMaxCombos`)
- 자세한 계산 규약은 `../DuctSizing/CLAUDE.md` 참조
