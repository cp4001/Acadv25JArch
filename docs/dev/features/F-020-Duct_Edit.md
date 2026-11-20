# F-020 Duct_Edit — 덕트 Line 1개 a·b 수정

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-20 / 2026-11-20 |
| 커밋 | |

## 요청 (원문 그대로)

> ducttre   에서 생성된 Duct Line에는  Xdata a b 에 폭 과 높이 값이 있는데 
> 사용자가  Duct line 생성 이후에  특정 Duct line을 선택해서 a b 값을 수정 하는 명령어 Duct_Edit  명령어를 작성해줘 한번에 1개 밖에 수정이 안되니까  Duct line  선택 필터 해주고 1개을 선택하면 바로 a b 값 입력하게 해서 수정하게 해줘

## 범위

- 포함: `Duct_Edit` — LINE·LWPOLYLINE + XData `a` 필터, 1개 선택 즉시 a → b 정수 입력(기본값 = 현재 값), `Disp` 의 `{a}x{b}` 부분 갱신(`[Total_CMH]` 꼬리 유지)
- 제외: 다중 선택, a≥b 검증, 이미 그려진 외곽선·사이즈 Text·태그 갱신

## 계획

1. (완료) `DuctTreeCommand.cs` `Cmd_DuctEdit` + `PromptSize` → 검증: Release 빌드
2. (미확인) 설치파일 → `Duct_Edit` 실행 → 검증: `GXD` 로 a/b/Disp 확인, `TTG` 라벨 변경

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctTreeCommand.cs` | `Duct_Edit` 명령 신규: `GetSelection(SingleOnly)` + `MakeFilterTypesRegs("LINE,LWPOLYLINE","a")` (Transaction 밖) → 읽기 Tr → `GetInteger` a/b → 쓰기 Tr 로 `a`/`b`/`Disp` 기록 |

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_20.exe` 생성
- [ ] AutoCAD 실행 확인 — 설치파일 실행까지 함, 실행 결과는 기록 안 됨 (사용자 done 요청)

## 후속 · 남은 일

- a/b 수정 후 이미 그려진 외곽선·`Insert_Duct_Size` Text·`Duct_tag` 는 갱신 안 됨 (재실행 필요) → backlog
- a≥b 검증 없음
