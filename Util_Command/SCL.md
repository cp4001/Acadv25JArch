# SCL 커맨드 사양서 — 진행방향 연속 Line 선택 (SelectConLine)

- **문서 버전**: v1.0 (구현 완료 기준 정리)
- **작성일**: 2026-07-08
- **커맨드명**: `SCL`
- **네임스페이스**: PipeLoad2 (`CommandUtil` 클래스, `Util_Command.cs`)
- **대상 환경**: AutoCAD 2025 (.NET 8.0), Windows 11
- **시스템**: 신규 시스템 — 외부 유틸 의존 없음
- **상태**: 구현 완료 (2026-07-07 커밋 "scl 연속 line 선택")
- **관련 문서**: `CSS.md`(체인 선택, XData 필터 + 끝점 연결 전용), `SS1.md`(교차 분할) — SCL 은 "진행 방향"과 "collinear 점프"가 추가된 점이 다름

---

## 1. 개요

기준 Line 1개를 클릭점과 함께 선택하면(클릭 지점으로 "진행 방향" 결정),
그 방향으로 이어지는 Line들을 연속으로 찾아 선택 상태(`SetImpliedSelection`)로 만드는 커맨드.

### 1.1 추적 우선순위
1. **끝점 연결 Line 우선** — 분기(2개 이상 연결)가 있으면 모든 경로를 계속 추적
2. 끝점 연결이 없으면 **진행 방향 앞쪽의 collinear Line으로 점프** (최대 거리 이내, 최근접 1개만)
3. 둘 다 없으면 해당 경로 종료

### 1.2 CSS 와의 차이

| 항목 | CSS | SCL |
|---|---|---|
| 대상 필터 | Line/Arc/LWPolyline + Duct/Pipe 계열 XData | Line 전체 (XData 필터 없음) |
| 추적 방식 | 끝점 연결만, 시작 엔티티 양방향 재귀 | 끝점 연결 + collinear 점프, 클릭점 기준 **단방향** |
| 순회 구현 | Stack 기반 반복 | Stack 기반 반복(분기 시 모든 경로 push) |
| 결과 적용 시점 | Idle 이벤트로 지연 적용(`_pendingSelectIds` + `Application.Idle`) | 명령 실행 중 즉시 `ed.SetImpliedSelection` 호출 |

> **확인 필요(§6)**: CSS 는 원래 명령 내에서 바로 `SetImpliedSelection`을 호출했다가
> "명령 종료 시 선택이 무효화된다"는 문제가 있어 Idle 이벤트로 지연 적용하도록 수정된 이력이 있다
> (`Util_Command.cs` 내 CSS 주석 "명령 내 호출은 종료 시 무효화됨" 참고).
> SCL 은 현재 CSS 의 **수정 이전 방식**(명령 내 직접 호출)으로 구현되어 있어 동일한 문제가
> 재현될 가능성이 있다 — 실사용 확인 필요.

---

## 2. 명령 절차

1. `PromptEntityOptions` 로 Line 1개 선택 — 클릭 지점(`PickedPoint`)에서 가까운 끝점을 진행 시작점으로 결정
2. Stack 기반 반복 순회 (재귀 아님):
   - **우선순위 1**: 진행 끝점 주변 `SEARCH_BOX`(10) Crossing 검색으로 끝점 연결된 미방문 Line 전부 추적(`FindConnectedLines`)
   - **우선순위 2**: 연결 없으면 `FindColinearJump` 로 `GAP_MAX`(900) 이내 최근접 collinear Line 1개로 점프
   - 후보가 전혀 없으면 해당 경로 종료
3. 방문 집합(`HashSet<ObjectId>`) 전체를 `ed.SetImpliedSelection`
4. 결과 메시지: 총 Line 개수 + collinear 점프 횟수

---

## 3. 판정 조건

### 3.1 끝점 연결 (`FindConnectedLines`)

- 진행 끝점 p 중심 `SEARCH_BOX`(10) Crossing 검색
- 후보의 `StartPoint` 또는 `EndPoint` 가 p 와 `END_TOL`(1.0) 이내면 연결로 판정

### 3.2 Collinear 점프 (`FindColinearJump`) — 4개 조건 AND

| 조건 | 내용 | 관련 상수 |
|---|---|---|
| A. 평행 | 현재 Line 과 평행 (방향 반대 허용, 내적 절댓값 기준) | `ANGLE_TOL = 1.0°` |
| B. 직선상 | p 를 후보의 무한직선에 수직 투영한 거리가 오차 이내 | `OFFSET_TOL = 1.0` |
| C. 전방 | 후보의 가까운 끝점이 진행 방향(dir) 앞쪽(내적 > 0) | - |
| D. 거리 | 진행 끝점 → 후보 근접 끝점 거리가 최대 거리 이내 | `GAP_MAX = 900.0` |

- 조건을 만족하는 후보가 여러 개면 거리(gap)가 가장 짧은 1개만 채택

---

## 4. 상수 요약

| 상수 | 값 | 의미 |
|---|---|---|
| `SCL_END_TOL` | 1.0 | 끝점 연결 허용 거리 |
| `SCL_GAP_MAX` | 900.0 | collinear 점프 최대 거리 |
| `SCL_ANGLE_TOL` | 1.0 | collinear 각도 허용 오차(도) |
| `SCL_OFFSET_TOL` | 1.0 | 수직 투영 오프셋 허용 거리 |
| `SCL_SEARCH_BOX` | 10.0 | 끝점 연결 검색 박스 크기 |

---

## 5. 사용 API

| API | Return 유형 | 용도 |
|---|---|---|
| `Editor.GetEntity(PromptEntityOptions)` | `PromptEntityResult` | 기준 Line + 클릭점(`PickedPoint`) 획득 |
| `Editor.SelectCrossingWindow(Point3d, Point3d, SelectionFilter)` | `PromptSelectionResult` | 끝점 연결/collinear 후보 검색 |
| `Editor.SetImpliedSelection(ObjectId[])` | `void` | 결과 선택 표시 |
| `Curve.GetClosestPointTo(Point3d, bool)` | `Point3d` | collinear 직선상 판정(수직 투영, `extend=true`) |
| `Vector3d.DotProduct` / `GetNormal` | `double`/`Vector3d` | 평행·전방 판정 |
| `Point3d.DistanceTo` | `double` | 거리 계산 |

---

## 6. 미확정/확인 필요 사항

1. **`SetImpliedSelection` 호출 시점** — §1.2 표 참고. CSS 는 Idle 지연 방식으로 수정된 반면 SCL 은
   명령 내 직접 호출 — 실사용 중 선택이 유지되지 않으면 CSS 와 동일한 Idle 지연 패턴 적용 필요.
2. **`GAP_MAX = 900.0` 고정값의 근거** — 도면/축척에 따라 조정이 필요한지 확인 필요(현재 하드코딩).

---

## 7. 버전 이력

| 버전 | 일자 | 내용 |
|---|---|---|
| v1.0 | 2026-07-08 | 2026-07-07 커밋("scl 연속 line 선택")으로 구현 완료된 SCL 커맨드 기준 사양서 최초 작성 |
