---
tags:
  - AutoCAD
---

# Insert_UpDn 명령어

> 파일: `PipeDiaCalc/UpDnInsertCommand.cs`
> 클래스: `PipeLoad2.UpDnInsertCommand.Cmd_InsertUpDn`
> 최종 업데이트: 2026-10-28 (신규, F-001)

---

## 1. 개요

XData `Tree` 가 있는 덕트 Line 2개를 한 번에 선택하면 **두 Line 이 선 안쪽에서 서로 교차하는 점**에 동적 블럭 `UpDn` 을 삽입한다. 동적 속성 `DS` 에 두 Line XData `"a"`(덕트 폭) 중 **작은 값**, `DL` 에 **큰 값**을 넣고, 회전은 **작은 a Line 의 `line.Angle`** 을 그대로 쓴다. `Insert_Damper` 와 같은 패턴([[InsertDamper]]).

| 단계 | 동작 |
|---|---|
| ① | `교차하는 덕트 Line 2개를 선택하세요:` — `GetSelection` + `JSelFilter.MakeFilterTypesRegs("LINE", "Tree")`. **정확히 2개가 아니면 메시지 후 다시 선택**(Esc 로 종료) |
| ② | 각 Line: `"a"` → `double.TryParse` 양수 확인 (`TryReadDuct`). `Tree` 는 필터가 보장 |
| ③ | `IntersectWith(Intersect.OnBothOperands)` → 첫 교차점 = 삽입점. 두 선이 실제로 교차하지 않으면 오류(연장 교차 불허) |
| ④ | (블럭 정의 가져오기) `JArchBlockLibrary.Import(db, ed, "UpDn")` — **검증 통과 후**, Transaction 밖 |
| ⑤ | `UpDn` 삽입, 회전 = 작은 a Line 의 `Angle` (a 가 같으면 첫 번째 Line) |
| ⑥ | 비상수 `AttributeDefinition` 마다 기본값으로 `AttributeReference` 부착 |
| ⑦ | 동적 속성 `DS` = 작은 a, `DL` = 큰 a (`OrdinalIgnoreCase`, 개별 try-catch, 2개 미만이면 경고) |
| ⑧ | `UpDn 삽입 완료 — DS=400, DL=600, 회전=90°` 출력 |

---

## 2. Insert_Damper 와 다른 점

| 항목 | Insert_Damper | Insert_UpDn |
|---|---|---|
| 선택 | Line 1개 | Line 2개 한 번에, XData `Tree` 필터 |
| 위치 | 가까운 끝점에서 225 안쪽 | 두 Line 이 선 안쪽에서 교차하는 점 (직각 아니어도 됨) |
| 회전 | 가까운→먼 끝점 방향 | 작은 a Line 의 `line.Angle` (Start→End, 선 방향에 따라 180° 차이 가능) |
| 동적 속성 | `Dis1`/`Dis2` = a/2 | `DS` = 작은 a, `DL` = 큰 a (나누지 않음) |
| Import 시점 | XData·길이 검증 후 (2026-10-28 F-002 부터) | XData·교차점 검증 **후** (오류 시 정의를 덮어쓰지 않음) |

---

## 3. 주의

- `"a"` 가 `"600x400"` 같은 비숫자면 오류 메시지 후 중단한다 (`Insert_Damper` 와 동일, 첫 숫자 추출 안 함 — 2026-10-28 확정).
- 블럭 이름은 접두사 없는 `UpDn` — 참조 도면 `Blocks\JArch_Blocks.dwg` 에 동적 속성 `DS`/`DL` 을 가진 `UpDn` 정의가 있어야 한다([[JArchBlockLibrary]]).
- 삽입 레이어는 현재 레이어, 공간은 `db.CurrentSpaceId`.
- ⚠️ **교차 판정은 3D** — 화면에서 겹쳐 보여도 두 Line 의 Z 가 조금이라도 다르면 "교차하지 않습니다" 로 끝난다. 특성 창은 Z 를 `LUPREC` 자리수로 반올림해 `0` 으로 보여주므로 믿지 말 것(`LUPREC 8` + `LIST` 로 확인). **해결: `ZZERO` 로 평탄화 후 재실행** (2026-10-28 실제 발생). 코드에서 XY 투영(`IntersectWith(..., Plane, ...)`)으로 Z 를 무시하는 방안은 보류 — 높이가 어긋난 도면은 다른 작업에도 문제가 되므로 도면을 고치는 것이 맞다(사용자 결정, 필요하면 추후 개발).

---

## 4. Insert_UpDn_All — 교차점 일괄 배치 (2026-10-28, F-003)

`Cmd_InsertUpDnAll` (같은 파일·클래스). Tree Line 을 **여러 개** 선택하면 그 그룹 안에서 서로 교차하는 **모든 Line 쌍**을 찾아 교차점마다 `UpDn` 을 놓는다. 한 Line 을 여러 Line 이 가로지르면 교차점마다 1개씩.

| 단계 | 동작 |
|---|---|
| ① | `교차를 찾을 덕트 Line 들을 선택하세요:` — 같은 `LINE`+`Tree` 필터, **2개 미만이면 다시 선택** |
| ② | 모든 Line 의 `"a"` 확인 — **하나라도 무효면 오류 후 중단**(블럭 정의도 건드리지 않음) |
| ③ | 모든 쌍(i<j) `IntersectWith(OnBothOperands)`. 교차점이 **어느 한쪽 Line 의 끝점(`EndTol = 2.0`)이면 제외** |
| ④ | 교차가 0개면 메시지 후 종료. 있으면 `JArchBlockLibrary.Import` 1회 |
| ⑤ | 교차점마다 `PlaceUpDn` — DS/DL/회전 규칙은 `Insert_UpDn` 과 같음(쌍마다 작은 a Line 의 `line.Angle`) |
| ⑥ | `UpDn N개 삽입 완료 (선택 Line M개).` + DS/DL 미설정 블럭이 있으면 개수만 경고 |

- **끝점 제외 이유**: Tree Line 은 끝점끼리, 또는 끝점이 다른 Line 중간에 닿는(T 분기) 방식으로 연결된다. 이것까지 교차로 세면 분기점마다 UpDn 이 생긴다. `EndTol` 은 `DuctTreeBuilder.TOLERANCE`(2.0) 와 같은 값 — Tree 가 연결로 보는 거리면 교차로 보지 않는다.
- 삽입 코드는 `PlaceUpDn` 헬퍼로 `Insert_UpDn` 과 공유한다(한쪽만 고치면 어긋나지 않도록).
- ⚠️ 이미 배치된 UpDn 은 검사하지 않는다 — 같은 Line 들로 다시 실행하면 같은 자리에 또 생긴다.
- ⚠️ 3개 이상 Line 이 한 점에서 교차하면 쌍마다 1개씩 겹쳐 생긴다.
- 쌍 탐색은 O(n²) — 수백 개 선택까지는 문제없다.
