# CSS 커맨드 사양서 — Duct/Pipe 체인 선택 (SelectChain)

- **문서 버전**: v1.2 (2026-11-16 — §8 변경 사항 참조, §4 의 SelectCrossingWindow·단방향 서술은 v1.1 기록)
- **작성일**: 2026-07-06
- **커맨드명**: `CSS`
- **네임스페이스**: PipeLoad2 (프로젝트에 맞게 조정)
- **대상 환경**: AutoCAD 2025 (.NET 8.0), Windows 11
- **시스템**: 신규 시스템 — 외부 유틸 의존 없음
- **상태**: 전 항목 확정 — 구현 완료 (Util_Command.cs)

---

## 1. 개요

Duct/Pipe 계열 XData를 가진 엔티티(Line, Arc, LWPolyline) 1개를 선택하면,
양 끝점에 연결된 동일 계열 엔티티를 재귀적으로 추적하여
체인 전체를 선택 상태(SetImpliedSelection)로 만드는 커맨드.

### 1.1 사용 시나리오
1. 사용자가 `CSS` 커맨드 실행 (또는 PickFirst로 사전 선택 후 실행)
2. 시작 엔티티 1개 선택
3. 시작 엔티티의 StartPoint / EndPoint 양방향으로 연결 추적
4. 추적된 전체 체인이 그립 표시된 선택 상태가 됨

---

## 2. 선택 필터 (확정)

| 항목 | 값 |
|------|-----|
| 엔티티 타입 | `LINE, ARC, LWPOLYLINE` |
| XData RegAppName | 조건 없음 (2026-11-16 F-016 에서 제거 — 이전: `Duct, Pipe, MainPipe, FirePipe`) |
| CommandFlags | `UsePickSet` (사전 선택 지원) |

- 해당 타입이면 XData 유무와 관계없이 대상
- 체인 추적 시 발견되는 엔티티도 동일 필터 조건을 만족해야 함
- **레이어**: 시작 엔티티와 같은 레이어(대소문자 무시)만 추적, 다르면 제외 (2026-11-16 F-017). 시작 엔티티가 여러 개면 그 레이어들 전체

---

## 3. 기존 코드 문제점 및 보완 사항 (확정)

| # | 기존 문제 | 보완 방향 |
|---|-----------|-----------|
| 1 | `SetImpliedSelection`이 foreach 루프 내부에서 반복 호출 | 루프 종료 후 1회만 호출 |
| 2 | 미사용 변수 (`baseLine`, `targetLines`, `pso_Ceiling`, `pso2`, `dbObjs`) | 전부 제거. 특히 `new Line()` 임시 객체 생성 금지 |
| 3 | `btr`을 ForWrite로 열지만 쓰기 작업 없음 | 선택 전용 커맨드이므로 BlockTable/BTR 접근 자체 제거 |
| 4 | try-catch 부재 | 표준 패턴 적용: try-catch + `Editor.WriteMessage` |
| 5 | `GetType() == typeof(...)` 타입 비교 | 패턴 매칭 `is Line line` 으로 전환 (캐스팅 중복 제거) |
| 6 | `new TypedValue[2] {...}` 구식 문법 | 컬렉션 표현식 `[...]` 적용 |
| 7 | sPt/ePt가 루프 밖 선언되어 이전 값 재사용 위험 | 패턴 매칭 스코프 내 지역 변수로 처리 |
| 8 | 순환 체인(loop) 시 무한 재귀 위험 | 방문 집합(HashSet&lt;ObjectId&gt;) 기반 중복 방지 |

---

## 4. 핵심 로직 (확정)

### 4.1 전체 흐름
```
CSS 실행
 ├─ 1. 필터 선택 (GetSelection + SelectionFilter)
 ├─ 2. 단일 Transaction 시작
 │    ├─ 시작 엔티티의 sPt / ePt 추출 (패턴 매칭)
 │    ├─ 방문 집합에 시작 엔티티 등록
 │    ├─ SelectChain(sPt 방향) — 재귀
 │    └─ SelectChain(ePt 방향) — 재귀
 ├─ 3. Transaction Commit
 ├─ 4. SetImpliedSelection (1회)
 └─ 5. 결과 메시지 출력 (선택된 엔티티 수)
```

### 4.2 연결 판정 — 끝점 일치
- 대상 끝점(sPt/ePt) 주변을 `SelectCrossingWindow`로 검색하여 후보 수집
- 후보 엔티티의 StartPoint 또는 EndPoint가 대상 점과 허용오차 이내면 연결로 판정
- 연결 판정 후 반대쪽 끝점으로 재귀 계속

### 4.3 순환 방지 (확정)
- `HashSet<ObjectId>` 로 방문 엔티티 기록
- 이미 방문한 ObjectId는 재귀 대상에서 제외
- Handle 기반이 아닌 ObjectId 기반 (모두 DB 등록 객체이므로 문제없음)

### 4.4 근접 연결 (T분기 등, 끝점이 아닌 중간 접속)
- 후보 엔티티에 대해 `entity.GetClosestPointTo(targetPt, false)` 호출
- 반환 Point3d와 targetPt의 거리가 허용오차 이내면 연결로 판정
- ※ extend 인수는 `false` (실제 곡선 범위 내 투영만 인정)

---

## 5. 사용 API (AutoCAD 2025 공식 확인)

| API | Return 유형 | 용도 |
|-----|------------|------|
| `Editor.GetSelection(PromptSelectionOptions, SelectionFilter)` | `PromptSelectionResult` | 시작 엔티티 선택 |
| `Editor.SelectCrossingWindow(Point3d, Point3d, SelectionFilter)` | `PromptSelectionResult` | 끝점 주변 후보 검색 |
| `Editor.SetImpliedSelection(ObjectId[])` | `void` | 결과 선택 표시 |
| `Editor.WriteMessage(string)` | `void` | 메시지 출력 |
| `Curve.GetClosestPointTo(Point3d, bool)` | `Point3d` | 근접 연결 판정 |
| `Curve.StartPoint / EndPoint` | `Point3d` | 끝점 취득 (Line/Arc/Polyline 공통, Curve 상속) |
| `Transaction.GetObject(ObjectId, OpenMode)` | `DBObject` | 객체 열기 |
| `Point3d.DistanceTo(Point3d)` | `double` | 거리 계산 |

- Line / Arc / Polyline 모두 `Curve`를 상속하므로 타입별 분기 없이
  `is Curve curve` 하나로 StartPoint/EndPoint 접근 가능 (코드 단순화)
- `Editor.Regen()`: 공식 API에 존재하나, SetImpliedSelection 자체가
  그립 표시를 하므로 본 커맨드에서는 제거

---

## 6. .NET 8.0 적용 항목 (확정)

- 컬렉션 표현식: `TypedValue[] tvs = [ ... ];`
- 패턴 매칭: `if (dbObj is Curve curve)`
- `using var tr = ...` (단일 Transaction)
- 문자열 보간: `$"\n{count}개 엔티티 선택됨"`
- 주석은 한글로 작성

---

## 7. 확정 항목 (v1.1 — 전 항목 확정)

| 질문 | 확정안 | 적용값 / 내용 |
|------|--------|---------------|
| Q1 | 1안 | 끝점 일치 허용오차 `CHAIN_END_TOL = 1.0` |
| Q2 | 1안 | 근접(T분기) 허용 거리 = 끝점 허용오차와 동일 `CHAIN_NEAR_TOL = 1.0` |
| Q3 | ~~1안~~ → **2안** (v1.2) | 양방향 — 내 끝점 → 후보 몸통, 후보 끝점 → 내 몸통 모두 인정 |
| Q4 | ~~1안~~ → **변경** (v1.2) | `SelectAll` 로 후보 전체 수집 후 메모리에서 bbox 선필터 + 판정 (`CHAIN_SEARCH_BOX` 삭제) |
| Q5 | 2안 | 다중 시작 엔티티 허용 (각 체인 모두 선택) |
| Q6 | 2안 → v1.2 Queue | Entity 단위 Queue BFS (재귀 없음) |

- 추가 확정: Line/Arc/Polyline 을 `Curve` 로 통합 처리 (`is Curve` 패턴 매칭)
- ~~알려진 제약: `SelectCrossingWindow` 는 현재 화면에 보이는 영역만 검색 → 실행 전 Zoom Extents 권장~~ — v1.2 에서 해소

## 8. v1.2 변경 (2026-11-16, F-016~F-018)

| 항목 | 내용 |
|---|---|
| XData 필터 | 제거 — `LINE,ARC,LWPOLYLINE` 이면 대상 (F-016) |
| 시작 선택 | `SingleOnly`+`SinglePickInSpace` — 1회 선택 후 Enter 없이 진행 (F-016) |
| 레이어 | 시작 엔티티 레이어(대소문자 무시)만 추적 (F-017) |
| 후보 수집 | `ed.SelectAll(sf)` 1회 (Transaction 밖, 뷰포트 무관) → 화면 밖 체인도 추적, Zoom 불필요. 시작 엔티티와 같은 레이어·같은 공간(`OwnerId`)만 보관 (F-018) |
| 연결 판정 | `EndOnBody(cur, k) \|\| EndOnBody(k, cur)` — 한쪽 끝점이 상대 몸통에서 `CHAIN_NEAR_TOL(1.0)` 이내 (`GetClosestPointTo(pt, false)`). 끝점-끝점·T분기·주관 중간/Poly 꼭짓점에 붙은 분기 모두 포함 (F-018) |
| 선필터 | `GeometricExtents` XY bbox 가 tol 이내로 겹칠 때만 판정. extents 예외(퇴화 엔티티)는 제외 |
| 성능 | 후보 N × 체인 M bbox 비교 — 같은 레이어 엔티티가 매우 많으면 느려질 수 있음 |

### 이하 원 질문 기록 (참고용)

아래 항목이 확정되어야 코드 작성 가능합니다.

**Q1. 끝점 일치 허용오차 (Tolerance)**
- 1안: `1.0` (도면 단위 mm 기준)
- 2안: `0.1`
- 3안: 사용자 지정 값

**Q2. 근접 연결(T분기) 허용 거리**
- 끝점이 상대 엔티티의 중간에 접속하는 경우(T분기)를 인정할 최대 거리
- 1안: 끝점 일치 허용오차와 동일 값 사용
- 2안: 별도 상수 지정 (예: 5.0)
- 3안: 근접 연결 기능 자체를 제외하고 끝점-끝점 연결만 인정

**Q3. 근접 연결의 방향성**
- 1안: 내 끝점 → 상대 몸통 접속만 인정 (단방향)
- 2안: 상대 끝점 → 내 몸통 접속도 인정 (양방향, T분기 역방향 추적 가능)

**Q4. 후보 검색 방식**
- 1안: `SelectCrossingWindow` — 끝점 중심 작은 사각 영역 검색 (빠름, 권장)
- 2안: 도면 전체 엔티티 순회 후 거리 비교 (느리지만 누락 없음)

**Q5. 다중 시작 엔티티 허용 여부**
- 1안: 1개만 허용 (`PromptSelectionOptions.SingleOnly = true`)
- 2안: 여러 개 선택 시 각각의 체인을 모두 선택 (기존 코드 동작)

**Q6. 재귀 vs 반복(Stack/Queue) 구현**
- 1안: 재귀 (기존 코드 방식 유지, 가독성 좋음)
- 2안: 명시적 Stack 기반 반복 (매우 긴 체인에서 StackOverflow 방지)

---

## 8. 버전 이력

| 버전 | 일자 | 내용 |
|------|------|------|
| v1.0 | 2026-07-06 | 초안 작성. 기존 코드 문제점 8건 보완 방향 확정. Q1~Q6 확인 대기 |
| v1.1 | 2026-07-06 | Q1~Q6 전 항목 확정. 신규 시스템(외부 유틸 없음) 기준으로 Util_Command.cs 구현 완료 |
