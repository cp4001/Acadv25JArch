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
| Import 시점 | XData 검증 전 | XData·교차점 검증 **후** (오류 시 정의를 덮어쓰지 않음) |

---

## 3. 주의

- `"a"` 가 `"600x400"` 같은 비숫자면 오류로 끝난다 (`Insert_Damper` 와 동일).
- 블럭 이름은 접두사 없는 `UpDn` — 참조 도면 `Blocks\JArch_Blocks.dwg` 에 동적 속성 `DS`/`DL` 을 가진 `UpDn` 정의가 있어야 한다([[JArchBlockLibrary]]).
- 삽입 레이어는 현재 레이어, 공간은 `db.CurrentSpaceId`.
