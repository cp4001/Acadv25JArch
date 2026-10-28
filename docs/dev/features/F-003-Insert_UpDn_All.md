# F-003 Insert_UpDn_All — 선택 Line 그룹의 모든 교차점에 UpDn 일괄 배치

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-28 / 2026-10-28 |
| 커밋 | `06c405b` |

## 요청 (원문 그대로)

> Insert_UpDN  명령어 참조해서  Insert_UpDn_All을 작성 할거야.
> 사용자가  Xdata Reg TRee 필터된  lne을 복소로 선택한다.  이 선택된 Line  그룹에서 교차하는 2 Line을 찾아서 UpDn 블럭을 배치한다.  교차하는  2 line은 끝점에서 겹치는 경우는 없고 line 내부어디서 겹치게 된다.    1개의 lien에 복수의 교차 Line이 발생할수도 있다.
> (첨부 그림: Magenta 수평 Line 1개를 Cyan 수직 Line 2개가 각각 안쪽에서 가로지름)

## 명확화

| 질문 | 답 |
|---|---|
| 교차 판정 | 모든 Line 쌍 `IntersectWith(OnBothOperands)`. **교차점이 어느 한 Line 의 끝점(허용오차 `EndTol = 2.0` = `DuctTreeBuilder.TOLERANCE`) 이면 제외** — Tree 는 끝점끼리/끝점-중간(T 분기)으로 연결되므로 이것을 걸러야 분기점마다 UpDn 이 생기지 않는다 (가정, 요청의 "끝점에서 겹치는 경우는 없다" 근거) |
| 한 Line 에 교차 여러 개 | 교차점마다 블럭 1개 |
| DS/DL/회전 | `Insert_UpDn` 과 동일 — 쌍마다 작은 a / 큰 a, 작은 a Line 의 `line.Angle` |
| XData `a` 오류 | 기존 결정(DECISIONS 2026-10-28) 적용 — 선택 Line 중 하나라도 `a` 가 숫자가 아니면 오류 후 중단(블럭 정의도 건드리지 않음) (가정) |

## 범위

- 포함: `Insert_UpDn_All` 명령, `Insert_UpDn` 과 삽입 코드 공유(헬퍼 추출)
- 제외: 이미 배치된 UpDn 중복 검사(재실행 시 같은 자리에 또 생김), 3선 이상이 한 점에서 교차하는 경우 중복 제거

## 계획

1. [x] `UpDnInsertCommand.cs` 에 삽입 헬퍼 추출 + `Insert_UpDn_All` 추가 → 검증: `dotnet build`
2. [x] `InsertUpDn.md` §4 추가, architecture·CLAUDE.md → 검증: 문서 확인
3. [x] 설치파일 재설치 후 실행 → 검증: 사용자 확인 (그림처럼 1:2 교차 → 2개, Tree 분기점에는 미배치, `Insert_UpDn` 회귀 없음)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/UpDnInsertCommand.cs` | `Insert_UpDn_All` 추가. 삽입·속성·DS/DL 을 `PlaceUpDn` 헬퍼로 추출해 `Insert_UpDn` 도 사용(동작 동일). `IsEndPoint`/`EndTol=2.0` |
| `Acadv25JArch/PipeDiaCalc/InsertUpDn.md` | §4 `Insert_UpDn_All` |
| `Acadv25JArch/architecture.md` | 명령 인덱스 행 추가 |
| `Acadv25JArch/CLAUDE.md` | 컨벤션 항목·QMD 키워드. `DuctTreeBuilder.TOLERANCE` 가 실제 2.0 임을 주석 |

## 검증 결과

- [x] 빌드 (Debug, 오류 0 — AutoCAD 실행 중이라 Release 는 설치파일 만들 때)
- [x] Release 빌드 + 설치파일 `JArchitecture_Setup_10_28.exe`(20:13) 재설치 → 사용자 "잘되어써" (교차 배치·분기점 제외·`Insert_UpDn` 회귀 3항목 요청)
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 테스트 중 "교차하지 않습니다" 발생 → 원인: Line Z 미세값(특성 창은 반올림해 0 으로 표시). `ZZERO` 후 정상. 코드 변경 없이 문서(`InsertUpDn.md` §3)·DECISIONS 에 기록, XY 투영안은 backlog
