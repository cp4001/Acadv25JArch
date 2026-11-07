# 현재 상태 (2026-11-07)

| 항목 | 내용 |
|---|---|
| 진행 중 | [F-008](features/F-008-DuctTree-Poly-Leaf.md) DuctTree Leaf 단 Polyline 지원 — 브랜치 `F-008`, 계획 6단계 중 5단계 완료, **도면 확인 대기** · F-007 `Duct_ToRevit` 은 브랜치 `F-007` 에서 실연동 확인 대기 |
| 마지막 한 일 | Poly 접합부 외곽선(가상 수직 Line), Poly Fence 자유단 10mm 연장 커밋(`d62ce4b`, 해시 기록 `c0e1696`) 후 `origin/F-008` 푸시. 설치파일 재생성(14:25). Confluence `DuctTreeTechNote`·`TreeOverrule` v2 갱신 |
| 다음 할 일 | ① AutoCAD 종료 후 `JArchitecture_Setup\Output\JArchitecture_Setup_11_07.exe` 재설치 ② `Jarchi_sample.dwg` 꺾인 Poly 6개(`17F82`~`17F87`, 레이어 `0`)를 `DUCT_CL_SUPPLY` 로 이동 ③ `DUCTTREE` → Apply → `DuctOutLine` → `TTG` 로 F-008 시나리오 9건 확인 ④ `/devlog done` ⑤ `master` 병합, 완료 문서 Confluence 등록 |
| 막힌 점 | 없음 |

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| 확정 규칙 | Poly 도 Root 와 같은 레이어만 추적한다 (사용자 확정, 코드 변경 금지). Root/Mid 는 Line 만 |
| 사용자 확인 필요 | "Poly 안의 첫번째 point" 는 덕트에 붙은 Poly 끝점으로 해석해 구현함. 외곽선 결과를 보고 확인할 것 |
| 사용자 승인 대기 | Poly Fence 10mm 연장은 요청 없이 넣은 변경 — 불필요하면 제거 (`DuctTreeBuilder.ExtendFreeEnd`) |
| 브랜치 주의 | F-008 설치본에는 F-007 `Duct_ToRevit` 이 없음. `master` 미병합 |
| F-009 | `To_Group` 완료 (`0cff034`). 같은 F-008 브랜치 커밋이라 다음 설치파일에 포함됨. 현재 `JArchitecture_Setup_11_07.exe` 에는 없음 → 재설치 전 재생성 필요 |

**재개 프롬프트:** "/devlog resume"
