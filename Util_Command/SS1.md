# SS1 커맨드 사양서 — Base Line 교차 분할 + Target 끝점 스냅 (MultiLineSplit)

- **문서 버전**: v1.2 (확정)
- **작성일**: 2026-07-06
- **커맨드명**: `ss2`
- **구성**: `MultiLineSplit_v2` (커맨드) + `InterPointsMLiness2` (핵심 처리 함수)
- **대상 환경**: AutoCAD 2025 (.NET 8.0), Windows 11
- **시스템**: 신규 시스템 — 외부 유틸 의존 없음, 전 기능 자체 구현
- **상태**: 전 항목 확정 — 구현 완료 (Util_Command.cs)

---

## 1. 개요

Base Line(들)을 선택하면, 각 Base Line 주변 폭 영역에 걸친
Pipe/Duct 계열 Target Line을 찾아 다음 두 가지를 수행한다.

1. **Target 끝점 스냅**: Target Line의 교차점에 가까운 끝점을 교차점으로 이동
2. **Base 분할**: Base Line을 교차점들에서 Split 하여 새 라인들로 교체 (원본 Erase)

### 1.1 사용 시나리오
1. `ss1` 실행 (UsePickSet — 사전 선택 지원)
2. Base Line(들) 선택 (일반 LINE, XData 조건 없음)
3. Base Line별로 수직 방향 ±폭 의 사각 폴리곤 영역에서 Target 후보 검색
4. 접촉 조건 만족 Target을 교차점 스냅 + Base 분할
5. 분할된 새 라인은 Base 속성 복사(CopyPropertyLine) 후 등록

---

## 2. 선택 필터 (확정)

| 구분 | 엔티티 타입 | XData RegAppName |
|------|------------|------------------|
| Base | LINE | 없음 (전체) |
| Target | LINE | `FirePipe, Pipe, Duct, MainPipe` |

- 검색 폭(ScaleFactor): 컴파일 타임 상수 `SEARCH_WIDTH = 300.0` (Q1 확정)
- Target 최소 길이: 50 초과 → 상수 `MIN_TARGET_LENGTH`
- Base 끝점 제외 거리: 10.0 → 상수 `END_EXCLUDE_DIST`

---

## 3. 기존 코드 문제점 및 보완 사항 (확정)

### 3.1 MultiLineSplit_v1 (ss1)

| # | 기존 문제 | 보완 방향 |
|---|-----------|-----------|
| 1 | `psr2.Status` 미확인 → 후보 0개 시 `return`으로 전체 롤백 | `Status != OK` 이면 `continue` (다음 Base 진행) |
| 2 | `Line ssl = new Line()` 직후 재할당 (임시 객체 누수) | `var ssl = ...` 직접 할당 |
| 3 | 강제 캐스팅 `(Line)tr.GetObject(...)` | 패턴 매칭 `is Line ssl` |
| 4 | `ObjectId.GetObject()` 로 Transaction 우회 | `tr.GetObject()` 일관 사용 |
| 5 | 색상 비교 죽은 코드 (`baseLineColor` 등) 잔존 | 전부 제거 (#08.26.1 이후 미사용) |
| 6 | `GetPointAtParameter(baseLine.Length)` 우회 표현 | `baseLine.EndPoint` 로 통일 |
| 7 | try-catch 부재 | 표준 패턴: try-catch + `Editor.WriteMessage` |
| 8 | 매직 넘버 50, 10.0 | 컴파일 타임 상수화 |

### 3.2 InterPointsMLiness

| # | 기존 문제 | 보완 방향 |
|---|-----------|-----------|
| 9 | **중첩 Transaction**: 호출측 tr 안에서 또 StartTransaction | Transaction을 인수로 받도록 시그니처 변경: `InterPointsMLiness(Transaction tr, Line baseLine, List<Line> targets)` |
| 10 | 바깥 tr에서 ForWrite로 연 객체에 `UpgradeOpen()` 호출 (충돌 위험) | 호출측에서 이미 ForWrite → `UpgradeOpen()` 제거. `IsWriteEnabled` 확인 후 필요 시에만 업그레이드 |
| 11 | **Target 이동 조건 미검증**: ExtendBoth 교차점이면 무조건 스냅 → 연장선 먼 교차점으로 끝점 폭주 | 스냅 전 교차점-끝점 거리 상한 체크 (Q1) |
| 12 | Base 끼리 서로 Target이 될 수 있음 → Erase 후 접근 시 `eWasErased` | 처리 완료/Erase 된 ObjectId 를 HashSet 으로 관리, 이후 루프에서 제외 (Q2) |
| 13 | 분할점 0개여도 `GetSplitCurves` 호출 | `sortPoints.Count > 0` 가드 추가 |
| 14 | 미분할 시 GetSplitCurves 결과 미해제 | 미등록 DBObject Dispose 처리 |
| 15 | 평행선마다 "do not intersect" 메시지 노이즈 | 개별 메시지 제거, 최종 요약 1회 출력 |
| 16 | `Line scLine = new Line()` 임시 객체 + 불필요한 `ref` | 직접 할당, `ref` 제거 검토 (외부 함수 시그니처 확인 필요) |
| 17 | 죽은 주석 다수 | 정리 |

---

## 4. 보완 후 핵심 로직 (확정)

### 4.1 전체 흐름 (단일 Transaction)
```
ss2 실행
 ├─ 1. 검색 폭 = SEARCH_WIDTH 상수 (300.0)
 ├─ 2. Base Line 선택 (GetSelection + sf)
 ├─ 3. 단일 Transaction 시작
 │    └─ Base Line 별 루프:
 │        ├─ 처리 완료 HashSet 에 있으면 skip
 │        ├─ 수직 벡터 계산: (E-S).CrossProduct(Normal).GetNormal()
 │        ├─ 폴리곤 4점 생성 (S±offset, E±offset)
 │        ├─ SelectCrossingPolygon(points, sf1)
 │        │    └─ Status != OK → continue
 │        ├─ 접촉 필터: GetClosestPointTo 거리 < ScaleFactor
 │        ├─ InterPointsMLiness2(tr, baseLine, targets, erasedIds)
 │        │    ├─ IntersectWith(ExtendBoth) 교차점 계산
 │        │    ├─ Target 끝점 스냅 (거리 상한 체크 후)
 │        │    ├─ 분할점 수집 (끝점 10.0 제외 + 구간 위 확인)
 │        │    ├─ StartPoint 거리순 정렬 → GetSplitCurves
 │        │    └─ 새 라인 등록 → 속성+XData 복사 → 원본 Erase
 │        └─ Erase 된 ObjectId → HashSet 등록
 ├─ 4. Transaction Commit (1회)
 └─ 5. 결과 요약 출력 (분할 수 / 스냅 수)
```

### 4.2 접촉 판정 (확정 — Q3: extend=false)
- `baseLine.GetClosestPointTo(ssl.StartPoint, false)` 수직 투영 우선 사용
- **extend=false**: 실제 Base 구간 내 투영만 인정 (연장선 오탐 제거)
- Target 끝점 중 하나라도 투영 거리 < ScaleFactor 이면 채택

### 4.3 함수 시그니처 (확정)
```csharp
// 대상 Entity 를 직접 인수로 사용 (가독성)
// Transaction 은 호출측에서 주입 — 단일 Transaction 원칙
// 반환: 분할 수 / 스냅 수 (Q6=3안)
static (int splitCount, int snapCount) InterPointsMLiness2(Transaction tr, Line baseLine, List<Line> targetLines, HashSet<ObjectId> erasedIds)
```

### 4.4 속성 복사 — 자체 구현 `CopyLineProperties(Line src, Line dst)` (신규)
- 기본 속성: `Layer, Color, Linetype, LinetypeScale, LineWeight`
- **XData 전체 복사**: `dst.XData = src.XData` — Pipe 계열 RegApp 데이터 유지
- 새 라인을 DB 등록(AppendEntity + AddNewlyCreatedDBObject) **후** 복사 수행
  (XData RegApp 해석은 Database 소속 상태에서 안정적)
- 방향 정규화(구 SetPostionXY)는 수행하지 않음 — GetSplitCurves 결과는 원본 방향 유지

---

## 5. 사용 API (AutoCAD 2025 공식 확인)

| API | Return 유형 | 용도 |
|-----|------------|------|
| `Editor.GetSelection(pso, sf)` | `PromptSelectionResult` | Base 선택 |
| `Editor.SelectCrossingPolygon(Point3dCollection, SelectionFilter)` | `PromptSelectionResult` | Target 후보 검색 |
| `Vector3d.CrossProduct(Vector3d)` | `Vector3d` | 수직 벡터 |
| `Vector3d.GetNormal()` | `Vector3d` | 단위 벡터 |
| `Curve.GetClosestPointTo(Point3d, bool)` | `Point3d` | 수직 투영 (우선 사용) |
| `Entity.IntersectWith(Entity, Intersect, Point3dCollection, IntPtr, IntPtr)` | `void` | 교차점 (ExtendBoth) |
| `Curve.GetSplitCurves(Point3dCollection)` | `DBObjectCollection` | Base 분할 |
| `Line.StartPoint / EndPoint` (set 가능) | `Point3d` | Target 스냅 |
| `DBObject.IsWriteEnabled` | `bool` | UpgradeOpen 필요 여부 판단 |
| `Entity.Erase()` | `void` | 원본 삭제 |
| `BlockTableRecord.AppendEntity(Entity)` | `ObjectId` | 새 라인 등록 |
| `Transaction.AddNewlyCreatedDBObject(DBObject, bool)` | `void` | 등록 |
| `Entity.XData` (get/set) | `ResultBuffer` | XData 전체 복사 |
| `Point3d.DistanceTo(Point3d)` | `double` | 거리 |

- **외부 의존 없음** — 신규 시스템: 속성/XData 복사(`CopyLineProperties`),
  구간 위 판정(`IsPointOnLineSegment`) 모두 자체 구현
- 구 코드의 `Dwgdefaults`, `IsPointOnCurveGDAP`, `LineUtil.SetPostionXY`,
  `CommandJp.CopyPropertyLine` 은 사용하지 않음

---

## 6. .NET 8.0 적용 항목 (확정)

- 컬렉션 표현식: `TypedValue[] tvs = [ ... ];`
- 패턴 매칭: `is Line line`
- `using var tr = ...` (커맨드에서 1회)
- 문자열 보간, 한글 주석
- 컴파일 타임 상수: `MIN_TARGET_LENGTH`, `END_EXCLUDE_DIST`

---

## 7. 확정 항목 (v1.1 — 전 항목 확정)

| 질문 | 확정안 | 내용 |
|------|--------|------|
| Q1 | 1안 | 스냅 거리 상한 = ScaleFactor (초과 시 스냅 안 함) |
| Q2 | 1안 | Erase 된 ObjectId → HashSet 기록, 이후 루프 skip |
| Q3 | **false** | 접촉 판정 `GetClosestPointTo(pt, false)` — Base 구간 내 투영만 인정 |
| Q4 | 1안 | 분할점 제외 교차점에도 스냅 항상 수행 (접합점 정리) |
| Q5 | 1안 | 끝점-교차점 거리 0.001 이내면 스냅 생략 (불필요 DB 쓰기 방지) |
| Q6 | 3안 | 반환값 `(int splitCount, int snapCount)` — 요약 출력용 |

### 이하 원 질문 기록 (참고용)

**Q1. Target 끝점 스냅 거리 상한**
- ExtendBoth 교차점으로 끝점을 이동하기 전, 교차점-끝점 거리 상한 값
- 1안: ScaleFactor 와 동일 값 (폴리곤 폭과 일치, 권장)
- 2안: ScaleFactor × 2
- 3안: 상한 없음 (기존 동작 유지 — 폭주 위험 감수)

**Q2. Base 간 상호 Target 문제 처리**
- 1안: 처리/Erase 된 ObjectId HashSet 으로 이후 루프에서 제외 (권장)
- 2안: Target 후보에서 Base 선택 집합 전체를 처음부터 제외
- 3안: 기존 동작 유지 (Base 가 Pipe XData 없으면 실제로 발생 안 함)

**Q3. 접촉 판정의 extend 인수**
- 1안: `true` 유지 (연장선 투영 — 기존 동작, 폴리곤이 1차 필터)
- 2안: `false` 로 변경 (실제 Base 범위 내 투영만 인정 — 오탐 감소)

**Q4. Target 스냅 조건**
- 교차점이 Base 끝점 10.0 이내라 분할점에서 제외된 경우에도 Target 스냅은 수행할지
- 1안: 스냅은 항상 수행 (기존 동작 — 접합점 정리 목적)
- 2안: 분할점으로 채택된 교차점에만 스냅

**Q5. Target 이미 교차점 위에 있는 경우**
- 끝점이 이미 교차점과 일치(오차 이내)하면 스냅 생략 여부
- 1안: 오차(예: 0.001) 이내면 스냅 생략 (불필요한 DB 쓰기 방지)
- 2안: 항상 대입 (기존 동작)

**Q6. InterPointsMLiness 반환값 확장**
- 1안: `bool` 유지 (분할 성공 여부)
- 2안: `(bool splited, List<ObjectId> newLineIds)` 반환 — 분할 결과를 호출측에서 후속 처리 가능
- 3안: `(int splitCount, int snapCount)` — 요약 출력용

---

## 8. 버전 이력

| 버전 | 일자 | 내용 |
|------|------|------|
| v1.0 | 2026-07-06 | 초안. ss1 문제 8건 + InterPointsMLiness 문제 9건 보완 방향 확정. Q1~Q6 확인 대기 |
| v1.1 | 2026-07-06 | Q1~Q6 전 항목 확정 (Q3=false). 커맨드명 ss2, 함수명 InterPointsMLiness2 로 확정. 구현 진행 가능 |
| v1.2 | 2026-07-06 | 신규 시스템 전환 — 외부 유틸 의존 제거. 검색 폭 상수 SEARCH_WIDTH=300.0, 속성+XData 복사 자체 구현(CopyLineProperties), 방향 정규화 제거. Util_Command.cs 구현 완료 |
