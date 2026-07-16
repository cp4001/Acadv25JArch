# SCL 커맨드 사양서 — 진행방향 연속 Line 선택 (SelectConLine)

- **문서 버전**: v1.2 (XData 복사 + 확인 프롬프트 + Idle 지연 선택 반영)
- **작성일**: 2026-07-08 (v1.1: 2026-07-09, v1.2: 2026-07-16)
- **커맨드명**: `SCL`
- **메서드명**: `SCL_SelectConLine` (v1.0 당시 `SelectConLine`에서 개명 — "CommandUtil 명령어 정리" 커밋)
- **네임스페이스**: PipeLoad2 (`CommandUtil` 클래스, `Util_Command.cs`)
- **대상 환경**: AutoCAD 2025 (.NET 8.0), Windows 11
- **시스템**: 신규 시스템 — 외부 유틸 의존 없음
- **상태**: 구현 완료 (2026-07-07 커밋 "scl 연속 line 선택", 2026-07-08 커밋 "scl 거리 입력 기능 추가", 2026-07-16 XData 복사·확인·Idle 선택 추가)
- **관련 문서**: `CSS.md`(체인 선택, XData 필터 + 끝점 연결 전용), `SS1.md`(교차 분할) — SCL 은 "진행 방향"과 "collinear 점프"가 추가된 점이 다름

---

## 1. 개요

기준 Line 1개를 클릭점과 함께 선택하면(클릭 지점으로 "진행 방향" 결정),
그 방향으로 이어지는 Line들을 연속으로 찾은 뒤, **하이라이트로 미리 보여주고 [선택/취소] 확인**을 받아
선택 시 **기준 Line 의 XData 를 대상 Line 들에 복사**하고 선택 상태로 만드는 커맨드.
선택 상태는 **명령 종료 후에도 유지**된다(Idle 지연 적용).

### 1.1 추적 우선순위 및 처리 순서
1. **끝점 연결 Line 우선** — 분기(2개 이상 연결)가 있으면 모든 경로를 계속 추적
2. 끝점 연결이 없으면 **진행 방향 앞쪽의 collinear Line으로 점프** (최대 거리 이내, 최근접 1개만)
3. 둘 다 없으면 해당 경로 종료
4. 발견한 Line 전체를 **하이라이트**로 미리보기 → **[선택(Y)/취소(N)]** 확인 (v1.2)
5. 선택 시 **기준 Line XData → 대상 Line 복사** (기준 Line 에 XData 가 있을 때만, 기준 자신은 제외) (v1.2)
6. 방문 집합을 **Idle 시점에 선택** — 명령 종료 후에도 유지 (v1.2)

### 1.2 CSS 와의 차이

| 항목 | CSS | SCL |
|---|---|---|
| 대상 필터 | Line/Arc/LWPolyline + Duct/Pipe 계열 XData | Line 전체 (XData 필터 없음) |
| 추적 방식 | 끝점 연결만, 시작 엔티티 양방향 재귀 | 끝점 연결 + collinear 점프, 클릭점 기준 **단방향** |
| 순회 구현 | Stack 기반 반복 | Stack 기반 반복(분기 시 모든 경로 push) |
| 결과 확인 | 없음(바로 선택) | 하이라이트 미리보기 + [선택/취소] 확인 프롬프트 (v1.2) |
| XData 복사 | 없음 | 기준 Line XData → 대상 Line 복사(선택 시) (v1.2) |
| 결과 적용 시점 | Idle 이벤트로 지연 적용(`_pendingSelectIds` + `Application.Idle`) | **v1.2부터 CSS 와 동일한 Idle 지연 적용** (v1.1 까지는 명령 내 직접 호출) |

> **v1.2 변경**: v1.1 까지 SCL 은 CSS 의 **수정 이전 방식**(명령 내 직접 `SetImpliedSelection` 호출)이라
> "명령 종료 시 선택 무효화" 문제가 재현될 우려가 있었다(§6 확인 항목).
> v1.2 에서 CSS 와 동일하게 `_pendingSelectIds` + `Application.Idle += OnIdleSetSelection` 지연 패턴으로
> 교체하여 이 우려를 해소했다(핸들러는 CSS 와 공유).

---

## 2. 명령 절차

1. **기준 Line 선택 루프** (v1.1 신규 — `SetMessageAndKeywords`로 "D" 키워드 추가):
   - 그냥 Line 클릭 → 현재 `gapMax`(기본값 `SCL_GAP_MAX_DEFAULT`=900) 로 바로 진행
   - `D` 입력 → `PromptDoubleOptions`(기본값 = 현재 `gapMax`, 음수/0 불가, Enter = 유지)로 collinear 추적 거리를 새로 입력받고 다시 Line 선택으로 복귀(반복 가능)
   - 클릭 지점(`PickedPoint`)에서 가까운 끝점을 진행 시작점으로 결정
2. Stack 기반 반복 순회 (재귀 아님, **ForRead 트랜잭션 블록 — 종료 시 즉시 dispose, 중첩 금지**):
   - 순회 진입 시 **기준 Line 의 XData 존재 여부**(`baseHasXData = baseLine.XData != null`) 기록 — 복사 대상 판단용
   - **우선순위 1**: 진행 끝점 주변 `SEARCH_BOX`(10) Crossing 검색으로 끝점 연결된 미방문 Line 전부 추적(`FindConnectedLines`)
   - **우선순위 2**: 연결 없으면 `FindColinearJump` 로 (1단계에서 정한) `gapMax` 이내 최근접 collinear Line 1개로 점프
   - 후보가 전혀 없으면 해당 경로 종료
   - 방문 집합(`HashSet<ObjectId>`)을 `ObjectId[] allIds` 로 확정 후 Commit → 블록 종료
3. **하이라이트 미리보기** (v1.2): `SetHighlightScl(db, allIds, true)` + `ed.UpdateScreen()` — 발견한 Line 을 화면에서 강조
4. **확인 프롬프트** (v1.2): `PromptKeywordOptions` `[선택(Y)/취소(N)] <선택>` (`AllowNone = true`, Enter = 기본값 선택)
   - 프롬프트 메시지에 대상 개수 + "기준 XData 를 대상에 복사" / "기준 Line 에 XData 없음(복사 생략)" 안내 표시
   - 확인/취소 공통으로 하이라이트 해제(`SetHighlightScl(..., false)`) + `ed.UpdateScreen()`
   - **취소** 시: 아무 변경 없이 종료(복사·선택 모두 안 함)
5. **XData 복사** (v1.2, 선택 & `baseHasXData` 일 때만): 별도 ForWrite 트랜잭션에서 기준 Line 을 다시 열어
   `src.XdataCopy(tgt)` 로 대상 Line 마다 복사(**기준 Line 자신은 `ObjectId` 비교로 제외**)
6. **Idle 지연 선택** (v1.2): `_pendingSelectIds = allIds` + `Application.Idle += OnIdleSetSelection` (CSS 와 공유) — 명령 종료 후에도 선택 유지
7. 결과 메시지: 총 Line 개수 + collinear 점프 횟수 + XData 복사 개수

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
| D. 거리 | 진행 끝점 → 후보 근접 끝점 거리가 최대 거리 이내 | `gapMax` (기본 `SCL_GAP_MAX_DEFAULT = 900.0`, "D" 키워드로 실행 중 변경 가능, v1.1) |

- 조건을 만족하는 후보가 여러 개면 거리(gap)가 가장 짧은 1개만 채택

### 3.3 XData 복사 (v1.2)

- **복사 조건**: 확인 프롬프트에서 "선택"(또는 Enter) **그리고** 기준 Line 에 XData 가 있을 때(`baseHasXData`)만 수행
- **복사 방식**: `jCadExtention.cs`(`namespace CADExtension`)의 확장 메서드 `Entity.XdataCopy(Entity target)` 재사용
  - 기준 Line 의 `XData` **전체(모든 RegApp)** 를 대상 Line 에 그대로 복사
  - AutoCAD XData 특성상 대상에 있던 **다른 앱의 XData 는 유지**되고, 기준에 존재하는 RegApp 만 덮어씀
  - RegApp 은 기준 Line 이 이미 보유 중 → RegAppTable 에 등록되어 있으므로 별도 `CheckRegName` 불필요
  - DuctC1/C2/EndElbow 커맨드에서 검증된 것과 동일한 패턴
- **기준 Line 제외**: `id == per.ObjectId` 비교로 기준 Line 자신에는 복사하지 않음(자기 자신 덮어쓰기 방지)
- **라이선스**: `XdataCopy` 는 라이선스 게이트가 없어 `XdataSet` 과 달리 만료와 무관하게 동작

---

## 4. 상수 요약

| 상수 | 값 | 의미 |
|---|---|---|
| `SCL_END_TOL` | 1.0 | 끝점 연결 허용 거리 |
| `SCL_GAP_MAX_DEFAULT` | 900.0 | collinear 점프 최대 거리의 **기본값** (v1.1 — "D" 키워드로 실행 중 재입력 가능, 고정값 아님) |
| `SCL_ANGLE_TOL` | 1.0 | collinear 각도 허용 오차(도) |
| `SCL_OFFSET_TOL` | 1.0 | 수직 투영 오프셋 허용 거리 |
| `SCL_SEARCH_BOX` | 10.0 | 끝점 연결 검색 박스 크기 |

---

## 5. 사용 API

| API | Return 유형 | 용도 |
|---|---|---|
| `Editor.GetEntity(PromptEntityOptions)` | `PromptEntityResult` | 기준 Line + 클릭점(`PickedPoint`) 획득 |
| `PromptEntityOptions.SetMessageAndKeywords` / `PromptStatus.Keyword` | - | "D"(거리 변경) 키워드 처리(v1.1) |
| `Editor.GetDouble(PromptDoubleOptions)` | `PromptDoubleResult` | `gapMax` 값 재입력(v1.1, `DefaultValue`/`AllowNone` 사용) |
| `Editor.SelectCrossingWindow(Point3d, Point3d, SelectionFilter)` | `PromptSelectionResult` | 끝점 연결/collinear 후보 검색 |
| `Editor.GetKeywords(PromptKeywordOptions)` | `PromptResult` | [선택/취소] 확인 프롬프트(v1.2, `AllowNone`으로 Enter=선택) |
| `Entity.Highlight()` / `Entity.Unhighlight()` | `void` | 발견 Line 미리보기 하이라이트 on/off (v1.2, `SetHighlightScl` 헬퍼) |
| `Editor.UpdateScreen()` | `void` | 하이라이트 변경 즉시 반영 (v1.2) |
| `Entity.XdataCopy(Entity)` (`CADExtension`) | `void` | 기준 Line XData 전체를 대상에 복사 (v1.2) |
| `Editor.SetImpliedSelection(ObjectId[])` | `void` | 결과 선택 표시 — v1.2부터 Idle 핸들러(`OnIdleSetSelection`) 안에서 호출 |
| `Curve.GetClosestPointTo(Point3d, bool)` | `Point3d` | collinear 직선상 판정(수직 투영, `extend=true`) |
| `Vector3d.DotProduct` / `GetNormal` | `double`/`Vector3d` | 평행·전방 판정 |
| `Point3d.DistanceTo` | `double` | 거리 계산 |

---

## 6. 미확정/확인 필요 사항

1. ~~**`SetImpliedSelection` 호출 시점**~~ — **v1.2 로 해결**: CSS 와 동일한 `_pendingSelectIds` + `Application.Idle` 지연 패턴으로 교체(핸들러 `OnIdleSetSelection` 공유). 명령 종료 후 선택 유지 확보.
2. ~~`GAP_MAX = 900.0` 고정값의 근거~~ — **v1.1 로 해결**: "D" 키워드로 실행 중 사용자가 직접 조정 가능해짐(기본값만 900 유지).
3. **XData 복사 범위** — 현재 기준 Line 의 **모든 RegApp XData** 를 복사한다. 특정 RegApp(예: `Duct`/`Pipe`)만 선택 복사해야 하는 요구가 생기면 `XdataCopy` 대신 RegApp 지정 복사로 분리 필요.
4. **하이라이트 미리보기 가시성** — 발견 Line 이 현재 뷰 밖이면 하이라이트가 화면에 보이지 않을 수 있음(Zoom fit 미적용). 필요 시 확인 프롬프트 전에 `ZoomToEntities` 추가 검토.

---

## 7. 버전 이력

| 버전 | 일자 | 내용 |
|---|---|---|
| v1.0 | 2026-07-08 | 2026-07-07 커밋("scl 연속 line 선택")으로 구현 완료된 SCL 커맨드 기준 사양서 최초 작성 |
| v1.1 | 2026-07-09 | 2026-07-08 커밋("scl 거리 입력 기능 추가") 반영 — `gapMax`를 "D" 키워드로 실행 중 재입력 가능하도록 변경(`SCL_GAP_MAX` → `SCL_GAP_MAX_DEFAULT`), 메서드명 `SCL_SelectConLine`으로 개명 |
| v1.2 | 2026-07-16 | XData 복사 + 확인 프롬프트 + Idle 지연 선택 추가 — ① 발견 Line 하이라이트 미리보기 후 `[선택(Y)/취소(N)]` 확인(`SetHighlightScl` 헬퍼 신규) ② 선택 시 기준 Line XData 를 대상 Line 에 복사(`Entity.XdataCopy`, `using CADExtension;` 추가, 기준 자신 제외) ③ `SetImpliedSelection` 을 CSS 와 동일한 Idle 지연(`OnIdleSetSelection`)으로 교체해 명령 종료 후 선택 유지 ④ 순회를 ForRead 블록 → 확인 → ForWrite 복사 3단계로 분리(중첩 트랜잭션 금지) |
