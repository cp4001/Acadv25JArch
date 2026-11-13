# 현재 상태 (2026-11-13)

| 항목 | 내용 |
|---|---|
| 진행 중 | 없음. F-007 은 **보류** — 브랜치 `F-007` |
| 마지막 한 일 | F-013 `RoomAA` 장비 탭(+ADD → Spec Text, 재실행 시 그리드 복원, 즉시 화면 표시) 완료. F-012 폼 VS Designer 편집 지원 |
| 다음 할 일 | 다음 후보: backlog 상위 항목 (`RoomAA` 장비 행 삭제 → Text 삭제 등) |
| 막힌 점 | 없음 |

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| RoomEquipForm Designer | 사용자가 VS 에서 배치를 고침 → **재생성 금지, Edit 로 최소 추가** |
| 테스트 절차 | AutoCAD 종료 → `dotnet build Acadv25JArch\Acadv25JArch.csproj -c Release` → `ISCC JArchitecture_Setup\JArchitecture_Setup.iss` → 설치 → 실행 |
| F-012 리셋 이슈 | 재실행 시 값이 `" "` 로 덮이던 현상 — 원인 미확정, 이후 재발 없음. 재발 시 XData 원본 덤프 진단부터 (F-012 「참고」) |
| 설치파일 | `JArchitecture_Setup_11_13.exe` = F-008~F-013 포함, F-007 제외 |
| F-007 재개 | `git switch F-007` → `git merge master` (README·STATE 충돌 예상). 문서는 그 브랜치의 `docs/dev/features/F-007-Duct_ToRevit.md` 「재개 방법」. JRevit 쪽은 `240716New` 브랜치 |
| 확정 규칙 | DuctTree Poly 는 Leaf 전용, Root 와 같은 레이어만 추적. Root/Mid 는 Line 만 |
| AddinManager | XData 쓰는 명령은 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 |
| 브랜치 | `master` 가 최신, `F-007` 유지 |

**재개 프롬프트:** "/devlog resume" (F-007 은 "F-007 재개")
