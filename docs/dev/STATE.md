# 현재 상태 (2026-11-07)

| 항목 | 내용 |
|---|---|
| 진행 중 | 없음 (브랜치 `F-008`, `master` 미병합) · F-007 `Duct_ToRevit` 은 브랜치 `F-007` 에서 실연동 확인 대기 |
| 마지막 한 일 | F-008 마감 — 사용자 "DuctTree 는 잘 작동" 보고로 완료 처리. `TreeOverrule.md` 개요를 네 인스턴스로 수정 |
| 다음 할 일 | ① `F-008` 브랜치 커밋·푸시 ② `master` 병합 (F-008~F-011 포함) ③ F-011 도면 시나리오 3건 확인 ④ 다음 후보: backlog 상위 항목 |
| 막힌 점 | 없음 |

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| 확정 규칙 | Poly 도 Root 와 같은 레이어만 추적 (사용자 확정). Root/Mid 는 Line 만 |
| 브랜치 주의 | `F-008` 에 F-008~F-011 이 쌓여 있음. F-007 `Duct_ToRevit` 은 별도 브랜치 |
| F-011 | 도면 시나리오 3건 결과 미보고 — F 문서 후속 참조 |
| 설치파일 | `JArchitecture_Setup_11_07.exe` (17:25) 는 F-008~F-011 포함, F-007 제외 |
| AddinManager | 잠금 수정판으로 번들 교체 (fork `cp4001/CadAddinManager` `jarch-acad2025`). XData 쓰는 명령은 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 |

**재개 프롬프트:** "/devlog resume"
