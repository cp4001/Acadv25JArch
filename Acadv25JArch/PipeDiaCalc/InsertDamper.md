---
tags:
  - AutoCAD
---

# Insert_Damper 명령어

> 파일: `PipeDiaCalc/DamperInsertCommand.cs`
> 클래스: `PipeLoad2.DamperInsertCommand.Cmd_InsertDamper`
> 최종 업데이트: 2026-09-21 (신규)

---

## 1. 개요

덕트 Line 을 선택하면 **클릭 위치에 가까운 끝점에서 Line 방향으로 225 떨어진 지점**에 동적 블럭 `JDamper_Dynamic` 을 삽입한다. 블럭 크기는 Line XData `"a"`(덕트 폭)의 **1/2** 을 `Dis1`/`Dis2` 동적 속성에 넣어 맞추고, 회전은 선택 Line 의 각도를 적용한다.

| 단계 | 동작 |
|---|---|
| ① | `댐퍼를 삽입할 Line 을 선택하세요:` — `PromptEntityOptions` + `AddAllowedClass(typeof(Line), true)` |
| ② | Line XData `"a"` → `double.TryParse` → `Dis1`/`Dis2` = `a / 2` |
| ③ | `per.PickedPoint` 에 가까운 끝점(`nearPt`) → 먼 끝점 방향으로 `Offset = 225` 이동 → 삽입점 |
| ④ | `JDamper_Dynamic` 삽입, 회전 = `Atan2(dir.Y, dir.X)` |
| ⑤ | `DynamicBlockReferencePropertyCollection` 에서 `Dis1`/`Dis2` 에 값 대입 |
| ⑥ | `JDamper_Dynamic 삽입 완료 — a=600, Dis1/Dis2=300, 회전=90°` 출력 |

---

## 2. 기하

```
nearPt = 클릭점(per.PickedPoint) 과 가까운 Line 끝점
farPt  = 반대쪽 끝점
dir    = (farPt - nearPt).GetNormal()
삽입점  = nearPt + dir × 225
회전    = Atan2(dir.Y, dir.X)
```

- **방향은 항상 선 안쪽**(가까운 끝점 → 먼 끝점). 선 밖으로 나가면 댐퍼가 덕트를 벗어나므로.
- 회전에 `line.Angle`(항상 Start→End)을 쓰지 않는 이유: 선의 반대쪽을 클릭하면 댐퍼가 180° 뒤집혀 보인다. `nearPt → farPt` 기준이라 클릭한 쪽에서 본 방향과 항상 일치한다.
- Line 길이가 `1e-6` 미만이면 오류 후 종료.

---

## 3. 동적 블럭

| 항목 | 값 |
|---|---|
| 블럭 이름 | `JDamper_Dynamic` (상수 `BlockName`) — 도면에 정의돼 있어야 함. 없으면 `[오류] 도면에 'JDamper_Dynamic' 블럭이 없습니다.` 후 종료 |
| 동적 속성 | `Dis1`, `Dis2` (거리 파라미터) — 둘 다 `a / 2` |
| 속성 탐색 | `PropertyName` 을 `OrdinalIgnoreCase` 로 비교, 개별 `try-catch`. 2개 미만 설정되면 경고 출력(중단하지 않음) |
| 삽입 공간 | `db.CurrentSpaceId` |
| 삽입 순서 | `AppendEntity` → `AddNewlyCreatedDBObject` **이후** 동적 속성 대입 |

---

## 4. XData `"a"` 의존

`SetDuctWidth` 가 Line 에 기록한 `"a"`(덕트 폭)를 읽는다 ([[DuctTreeTechNote]] / `DuctTreeCommand.Cmd_SetDuctWidth`). 또는 `DUCTTREE` Mode D 산정이 기록한 `"a"`(장변).

- `JXdata.GetXdata(line, "a")` → `double.TryParse(NumberStyles.Any, InvariantCulture)`
- 값이 없거나 **숫자가 아니거나** 0 이하면 `[오류] 선택한 Line 에 유효한 XData "a"(덕트 폭)가 없습니다. (값: "...")` 후 종료
- ⚠️ `SetDuctWidth` 는 Text 를 **숫자 파싱 없이 그대로** 기록하므로 `"600x400"` 같은 값이 들어 있을 수 있다 → 이 명령은 그런 Line 을 처리하지 못한다. 필요해지면 `"a"` 에서 첫 숫자만 추출하는 분기를 추가할 것.

---

## 5. 관련 명령 / 문서

- `SetDuctWidth` — `"a"` XData 의 기록 시점 ([[DuctTreeTechNote]])
- [[InsertDiffuser]] — 같은 패턴의 블럭 삽입 명령 (선정표 기반, XData 다수 기록)
- [[architecture]] §4 — 명령 인덱스
