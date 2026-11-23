# F-021 PLVM 줌 수정 — DCS 변환 + 클릭 판정

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-23 / 2026-11-23 |
| 커밋 | |

## 요청 (원문 그대로)

> plvm  tree 에서 한개를 선택하면 해당 폴리가 화면에 꽉차게 표시 되어야 하는데  되는 홀리도 있고 안되는 폴리도 있네.

> 1, 2 둘 다 수정해줘

## 원인 (코드 읽기 — 도면 `TARGET`/`VIEWTWIST` 값은 미확인)

| # | 원인 | 위치 |
|---|---|---|
| 1 | `ZoomToEntities` 가 WCS extents 를 그대로 `view.CenterPoint`(DCS) 에 대입 → `TARGET ≠ 0` 또는 `VIEWTWIST ≠ 0` 이면 화면이 어긋남. 작은 Poly 일수록 화면 밖으로 밀림 | `CadFunction.cs` `ZoomToEntities` |
| 2 | 노드 클릭을 `e.Node.Bounds`(글자 영역)로만 판정 → 글자 오른쪽 빈 곳 클릭 무시 | `PolyViewControl.cs` `TreeView_NodeMouseClick` |

## 범위

- 포함: 위 2건
- 제외: `ZoomObjects` 등 다른 줌 함수 (이미 `WorldToEye` 사용 또는 미사용)

## 계획

1. (완료) `ZoomToEntities` — extents 를 `view.WorldToEye()` 로 변환 후 Width/Height/Center 계산 → 검증: Release 빌드
2. (완료) 클릭 판정 → `treeView.HitTest(...).Location == PlusMinus` 만 무시 → 검증: Release 빌드
3. (완료, 설치파일 `JArchitecture_Setup_11_23.exe`) `PLVM` 에서 실패하던 Poly 클릭 → 검증: 화면 가득 + 10% 여백

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/CadFunction.cs` | `ZoomToEntities`: `ext.TransformBy(view.WorldToEye())` 추가, w/h 계산을 변환 뒤로 이동 |
| `Acadv25JArch/PolyView/PolyViewControl.cs` | `TreeView_NodeMouseClick`: `Bounds.Contains` → `HitTest` PlusMinus 제외 |

> `ZoomToEntities` 는 `FCUTREE`·`LPM`·`CMH`·`SetDuctWidth` 의 CrossingWindow 전 줌에도 쓰인다 — 같은 조건 도면에서 매핑 누락이 있었다면 함께 해소. 평면뷰·TARGET 0 도면은 변환이 항등이라 동작 동일.

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_23.exe` 생성
- [x] AutoCAD 실행 확인 — 사용자 "잘되" (PLVM 노드 클릭 줌). 원인 1·2 중 어느 쪽이었는지(`TARGET`/`VIEWTWIST` 값)는 확인 안 함
- [ ] `FCUTREE`·`LPM`·`CMH`·`SetDuctWidth` 회귀 확인 안 함 (같은 `ZoomToEntities` 사용)

## 후속 · 남은 일

- `ZoomToEntities` 공유 명령 회귀 확인 (위 미확인 항목)
