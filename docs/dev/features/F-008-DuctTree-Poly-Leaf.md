# F-008 DuctTree Leaf 단 Polyline 지원

| 항목 | 값 |
|---|---|
| 상태 | 진행중 |
| 규모 | M |
| 요청일 / 완료일 | 2026-11-07 / |
| 커밋 | `1a5f2f8` |

## 요청 (원문 그대로)

> 여기 DuctTree 분석에서 leaf단은  line 뿐만아니라 poly 인 경우도 있는데 poly인 경우도 지원하게 수정 해 줄수 있어? 둘다 지원 하지만 leaf 인 경우만되고  mid root 는 반드시 line

추가 요청 (2026-11-07, 도면 확인 후):

> DuctTree  결과인데  poly가 있는 접하부 외곽선을 처리 하지 않는 것으로 했는데
>  외곽선 처리 하게 수정하고 Poly에서 진행방향(Duct에서  디퓨저 방향)으로 Poly 안의   첫번째 point에서  디퓨저 방향으로 Duct  line 과 수직인 Line 이 있다고 가정하고 외곽선 처리를 해주면 좋겠어

버그 보고 (2026-11-07, 도면 확인 후):

> leaf  단 poly에 대하여 DuctTree  분석에서 부하값 자체가 분석이 안되고 Xdata  정보도 기록되지 않았어. 
> poly 라도  선 1  개로 구성된 것은 정상 작동했어. 

## 명확화

| 질문 | 답 |
|---|---|
| 브랜치 | master 에서 `F-008` 분기 (F-007 과 독립) |
| Poly 가 달린 접합부의 DuctOutLine | 처음엔 스킵 → 추가 요청으로 **가상 수직 Line 기준 외곽선 생성** |
| "Poly 안의 첫번째 point" | 덕트에 붙은 Poly 끝점(접합점 X)으로 해석 (사용자 확인 필요) |
| 디퓨져 방향 | Poly 먼 끝이 덕트의 어느 쪽에 있는지로 결정 |
| Poly 분할 | 안 함 — Poly 는 그대로, 외곽선만 생성 |
| TTG 표시 | Poly Leaf 에도 Leaf 색상 + `Disp` 라벨 |
| 디퓨져 탐색 구간 | Poly 전체 경로 Fence (곡선 구간은 분할 근사), 자유단 10mm 연장 |
| Poly 레이어 | Root 와 같아야 함 (Line 과 동일). 다른 레이어는 추적하지 않음 — 사용자 확정 |

## 범위

- 포함: 열린 LWPOLYLINE 을 Leaf 로 인식, Fence 매핑, 풍량 누적, XData 기록, TTG 표시, Poly 접합부 외곽선(`Duct_EE`/`C2E`/`C1E`)
- 제외: 닫힌 Poly, 구형 POLYLINE(2D/3D), Poly 를 Root/Mid 로 쓰는 것, Poly 자체의 외곽선
- 제외: `Insert_Duct_Size`·`Duct_tag`·`Insert_UpDn`·`Duct_ToRevit` 등 Tree Line 을 받는 후속 명령의 Poly 대응
- Poly 뒤에 이어진 선: 탐색하지 않음 → 미연결 개수 경고

## 계획

1. [x] `DuctTreeBuilder` — Line/Poly 를 `Curve` 로 통합, Poly 는 BFS 에서 확장하지 않음, 전체 경로 Fence, XData 를 `Entity` 로 기록 → 검증: 빌드
2. [x] `DuctTreeCommand` — LWPOLYLINE 선택, 경로 수집, 미연결 경고 → 검증: 빌드
3. [x] `DuctTreeOutlineCommand` — Poly 자식을 수직 가상 Line 으로 판정, 임시 Line 으로 `TryApply` → 검증: 빌드
4. [x] `TreeOverrule` — Poly `Tree` 인스턴스 추가 → 검증: 빌드
5. [x] 문서 갱신 → 검증: 코드와 대조
6. [ ] 설치본으로 실제 도면 확인 → 검증: 아래 시나리오 (사용자)

### 도면 확인 시나리오

| # | 도면 | 기대 결과 |
|---|---|---|
| 1 | Leaf 가 Line 인 기존 도면 | 기존과 동일한 트리·풍량 |
| 2 | 끝이 직선 Poly + 디퓨져 | Poly 가 Leaf, 풍량 누적, `a`/`b`/`Disp` 기록 |
| 3 | 끝이 곡선 포함 Poly, 디퓨져가 경로 중간에 걸침 | Block 매핑됨 (`hit` ≥ 1) |
| 4 | Poly 뒤에 Line 이 이어짐 | 그 Line 은 제외, `⚠` 개수 표시 |
| 5 | Poly 달린 접합부에서 DuctOutLine | 직선+Poly `Duct_C2E`, 말단 Poly `Duct_EE`, 좌우 분기 `Duct_C1E` 외곽선 생성. Poly·중심선에 임시 Line 잔존 없음 |
| 5-1 | 비스듬하거나 꺾인 Poly | 컬러가 덕트와 수직, 디퓨져 쪽으로 생성 |
| 6 | TTG 켜기 | Poly 가 노란색 굵은 선(곡선 유지) + 라벨, 다음 도형으로 색 누수 없음 |
| 7 | Root 로 Poly 클릭 | 선택 거부 |
| 8 | `Jarchi_sample.dwg` 꺾인 Poly 6개를 Root 레이어로 옮긴 뒤 실행 | Leaf 인식, 풍량 누적, `a`/`b`/`Disp` 기록. 끝이 Block 외곽에 딱 닿은 `17F82` 도 매핑 |
| 9 | Root 와 다른 레이어의 Poly | 트리에서 제외 |

## 변경 내역

| 파일 | 변경 |
|---|---|
| `PipeDiaCalc/DuctTreeBuilder.cs` | `IsPoly`, `IsDuctCurve`, `GetPolyPath`, `ExtendFreeEnd`, 연결 판정 `Curve` 화, BFS Poly 중단, Poly 전체 경로 Fence, `ApplyRecursive` 를 `Entity` 로 |
| `PipeDiaCalc/DuctTreeCommand.cs` | LWPOLYLINE 선택 필터, `polyPaths` 수집, 미포함 개수 경고 |
| `PipeDiaCalc/DuctTreeOutlineCommand.cs` | Poly 자식 수직 가상 분기 판정, `CreateVirtualBranch`, `GetCurve` |
| `PipeDiaCalc/DuctEndElbowCommand.cs`, `DuctC2ECommand.cs`, `DuctC1ECommand.cs` | `TryApply` 에 분기 분할 끄기 옵션 (기본값 기존 동작) |
| `Overrule/TreeOverrule.cs` | `_polyTreeInstance`, `DrawPolyTree`, 라벨 렌더 `DrawLabelAt` 분리 |
| `PipeDiaCalc/DuctTreeTechNote.md`, `Overrule/TreeOverrule.md`, `CLAUDE.md` | 문서 갱신 |

## 검증 결과

- [x] 빌드 — Debug·Release 오류 0 (2026-11-07)
- [x] 원인 확인 — `Jarchi_sample.dwg` 복사본 DXF 분석: 트리에 닿는 꺾인 Poly 6개 전부 레이어 `0`, Root 레이어 `DUCT_CL_SUPPLY` 와 달라 탈락. 1선분 Poly 는 `DUCT_CL_SUPPLY` 에 있어 정상이었음. 코드 버그가 아니라 도면 레이어 문제 — 레이어 조건은 유지
- [ ] 테스트 / 실제 실행 확인 — 수정본 미실행. AutoCAD 전용 형식이라 단위 테스트 없음, 위 시나리오를 도면에서 확인 필요

## 후속 · 남은 일

- `Insert_Duct_Size`·`Duct_tag`·`Insert_UpDn`·`Duct_ToRevit` 은 Line 전용 — Poly Leaf 는 대상에서 빠짐
- `Duct_C2E` 는 `Waa > Wbb` 필수 — 상하류 폭이 같은 Poly 분기는 `[E04]` 스킵 (Line 분기와 동일, 미변경)
- 곡선 Poly 의 `TTG` 렌더는 도면에서 확인 필요 (base.WorldDraw + trait 변경 방식)
- 이 브랜치로 만든 설치본에는 F-007 `Duct_ToRevit` 이 없음
- 꺾인 Poly 6개는 도면에서 Root 레이어로 옮겨야 분석됨
