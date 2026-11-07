# 현재 상태 (2026-11-07)

| 항목 | 내용 |
|---|---|
| 진행 중 | 없음 (`master` 기준). F-007 `Duct_ToRevit` 은 **보류** — 브랜치 `F-007` 에서 추후 추가 개발 |
| 마지막 한 일 | F-008~F-011 완료·`master` 병합(`c4a46c6`) 후 푸시. F-007 을 보류로 바꾸고 재개 방법을 브랜치 문서에 기록(`3a7f80f`) |
| 다음 할 일 | 새 요청 대기. 후보: ① F-007 재개 ② backlog 상위 항목 (Poly Leaf 후속 명령 대응 등) |
| 막힌 점 | 없음 |

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| F-007 재개 | `git switch F-007` → `git merge master` (README·STATE 충돌 예상). 문서·남은 계획은 그 브랜치의 `docs/dev/features/F-007-Duct_ToRevit.md` 「재개 방법」. JRevit 쪽은 `240716New` 브랜치 |
| F-007 상태 | 빌드·크기 파싱만 확인, Revit 실연동 미실행. 추가 개발 범위는 재개 시 사용자에게 확인 |
| 확정 규칙 | DuctTree Poly 는 Leaf 전용, Root 와 같은 레이어만 추적. Root/Mid 는 Line 만 |
| 설치파일 | `JArchitecture_Setup_11_07.exe` (17:25) = F-008~F-011 포함, F-007 제외 |
| AddinManager | 잠금 수정판으로 번들 교체 (fork `cp4001/CadAddinManager` `jarch-acad2025`). XData 쓰는 명령은 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 |
| 브랜치 | `master` 가 최신. `F-008` 은 병합 후 로컬·원격 삭제(2026-11-07), `F-007` 은 유지 |

**재개 프롬프트:** "/devlog resume" (F-007 이어서 하려면 "F-007 재개")
