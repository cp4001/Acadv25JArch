# EC 커맨드 사양서 — 근접 끝점(L코너) 교차점 일치 (EndPoint_ConnectToIntersection)

- **문서 버전**: v1.0 (구현 완료 기준 정리)
- **작성일**: 2026-07-09
- **커맨드명**: `EC`
- **메서드명**: `EC_EndPoint_ConnectToIntersection`
- **네임스페이스**: PipeLoad2 (`CommandUtil` 클래스, `Util_Command.cs`)
- **대상 환경**: AutoCAD 2025 (.NET 8.0), Windows 11
- **시스템**: 신규 시스템 — 외부 유틸 의존 없음
- **상태**: 구현 완료 (2026-07-09 커밋 "EC")

---

## 1. 개요

선택한 여러 Line 들 중, 두 Line 의 끝점이 미세하게 어긋난 "L코너"(끝점끼리는 근접하지만
정확히 일치하지 않는 지점)를 찾아 두 Line 의 **실제 교차점**(양방향 무한 연장)으로
이동시켜 연결점을 정확히 일치시킨다.

- 대상은 **끝점–끝점(L코너) 접합만** — 중간 접속(T분기)은 다루지 않는다(CSS 의 근접 접속과 다른 점).
- Line 이 여러 개 선택되면 **모든 쌍**을 검사해 근접한 코너를 전부 정리한다.

---

## 2. 명령 절차

1. `LINE` 필터로 여러 Line 선택(개수 제한 없음)
2. 선택된 Line 전부를 리스트로 수집(`ForRead`, 이동 시 `UpgradeOpen`)
3. **모든 Line 쌍**(i < j)에 대해:
   - 두 Line 의 끝점 4조합(`a.Start/End` × `b.Start/End`) 중 **최소 거리** 계산
   - 최소 거리가 `CHAIN_END_TOL`(1.0, CSS 상수 재사용)보다 크면 근접 아님 → skip
   - 근접이면 `IntersectWith(Intersect.ExtendBoth)` 로 두 Line 의 실제 교차점 계산
     (교차점이 정확히 1개가 아니면 — 평행 등 — skip)
   - 두 Line 각각의 (코너 쪽) 가까운 끝점을 교차점으로 이동(`MoveNearEndEc`)
4. 처리된 코너 개수를 결과 메시지로 출력

---

## 3. 판정 조건

### 3.1 근접 끝점 판정

- 두 Line 의 끝점 4조합 중 최솟값이 `CHAIN_END_TOL`(1.0) 이내 — CSS 의 끝점 일치 허용오차 상수를 그대로 재사용(별도 상수 신설 없음)

### 3.2 끝점 이동 (`MoveNearEndEc`)

- `line.StartPoint`/`EndPoint` 중 교차점에 더 가까운 쪽만 교차점으로 치환
- `IsWriteEnabled`가 아니면 `UpgradeOpen()` 후 이동
- (T3 의 `MoveNearEndT3`와 동일한 로직 — EC 전용으로 별도 구현되어 있어 코드 중복은 있으나 동작은 동일)

---

## 4. 상수 요약

| 상수 | 값 | 의미 |
|---|---|---|
| `CHAIN_END_TOL` (CSS 와 공용) | 1.0 | 근접 끝점(L코너) 판정 허용 거리 |

- EC 전용 신규 상수는 없음 — 기존 `CHAIN_END_TOL` 재사용

---

## 5. 사용 API

| API | Return 유형 | 용도 |
|---|---|---|
| `Editor.GetSelection(PromptSelectionOptions, SelectionFilter)` | `PromptSelectionResult` | Line 다중 선택 |
| `Point3d.DistanceTo(Point3d)` | `double` | 끝점 4조합 최소 거리 계산 |
| `Entity.IntersectWith(Entity, Intersect, Point3dCollection, IntPtr, IntPtr)` | `void` | 실제 교차점(`ExtendBoth`) |
| `Line.StartPoint / EndPoint` (set 가능) | `Point3d` | 코너 끝점 이동 |
| `DBObject.IsWriteEnabled` / `UpgradeOpen()` | `bool`/`void` | 쓰기 모드 전환 |

---

## 6. T3 와의 차이

| 항목 | T3 | EC |
|---|---|---|
| 입력 | Line 정확히 3개(colinear 쌍 자동 판별 필요) | Line 여러 개(개수 제한 없음) |
| 대상 판정 | colinear 쌍 + 각도선 구조 판별 | 모든 쌍의 끝점 근접 여부만 확인 |
| 처리 범위 | 선택한 3개 중 1세트만 처리 | 선택된 전체 Line 쌍을 순회하며 근접한 코너 전부 처리 |
| 용도 | 정확히 정해진 T자/교차 구조 정리 | 여러 군데 흩어진 L코너 오차를 한 번에 일괄 정리 |

---

## 7. 미확정/확인 필요 사항

1. **Line 개수가 많을 때 성능** — 모든 쌍(O(n²))을 순회하므로 선택 개수가 매우 많으면 느려질 수 있음. 실사용 규모 확인 필요.
2. **연쇄 이동 가능성** — 한 Line 이 여러 다른 Line 과 동시에 근접 코너를 이루는 경우(예: 3개 이상이 한 점 근처에 모임), 쌍별로 순차 처리되므로 나중 쌍 처리 시 이미 이동된 좌표 기준으로 판정될 수 있음 — 의도된 동작인지 확인 필요.

---

## 8. 버전 이력

| 버전 | 일자 | 내용 |
|---|---|---|
| v1.0 | 2026-07-09 | 2026-07-09 커밋("EC")으로 구현 완료된 EC 커맨드 기준 사양서 최초 작성 |
