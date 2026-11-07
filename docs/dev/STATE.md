# 현재 상태 (2026-11-07)

| 항목 | 내용 |
|---|---|
| 진행 중 | 없음 (`master` 기준). F-007 `Duct_ToRevit` 은 **보류** — 브랜치 `F-007` |
| 마지막 한 일 | F-012 `RoomAA` 실 탭(표시·저장) 완료·푸시 (`d4f44f2`, `53fba5d`). Confluence `131081` 에 행 추가(v19) |
| 다음 할 일 | **`RoomAA` 장비 탭** — 새 F-013 으로 `/devlog new` 등록 후 사용자에게 사양 확인 → 구현 (아래 「장비 탭 재개」) |
| 막힌 점 | 장비 탭 동작 사양 미정 (아래 확인 질문) |

## 장비 탭 재개

| 항목 | 내용 |
|---|---|
| 코드 위치 | `Acadv25JArch/RoomEquip/` — `RoomAACommand.cs`(명령·`RoomRow`·`LoadKeys`) · `RoomEquipForm.cs`(저장 `btnSave_Click`) · `RoomEquipForm.Designer.cs`(`tabEquip` 비어 있음) |
| Designer | 사용자가 Designer 에서 배치를 고칠 수 있음 → **재생성 금지, Edit 로 최소 추가** |
| 목업 (첫 요청 첨부 이미지) | 실명 콤보(실 탭의 실 목록, 예 "홀1") · 장비 종류 칸 "디퓨져"(고정 표시) · 장비 종류 콤보 `RPD/RAD/SPD/SAD` · 시스템 타입 콤보 `SA/RA/EA/OA` · 우상단 `+ADD` 버튼 |
| 재사용 후보 | Type·SystemType 은 `Insert_Diffuser`(`PipeDiaCalc/DiffuserInsertCommand.cs`) 와 같은 코드값. 형식명은 `DiffuserSummaryCommand.TypeInfo` |
| 테스트 절차 | AutoCAD 종료 → `dotnet build Acadv25JArch\Acadv25JArch.csproj -c Release` → `ISCC JArchitecture_Setup\JArchitecture_Setup.iss` → 설치 → `RoomAA` |

### 사용자에게 물어볼 것

1. `+ADD` 를 누르면 무엇이 생기나 — 장비 목록 그리드에 행 추가? 도면에 블럭 삽입(`Insert_Diffuser` 처럼)?
2. 추가한 장비는 어디에 저장하나 — Room Poly XData? 별도 객체? (개수·풍량 입력도 필요한지)
3. 장비 종류 "디퓨져" 칸 — 고정값인가, 다른 장비(FCU 등)도 고를 수 있게 되나?
4. 목업의 장비 종류 열이 두 개(디퓨져 / RPD…) — 이름 구분 확인

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| F-012 리셋 이슈 | 재실행 시 값이 `" "` 로 덮이던 현상 — 원인 미확정, 진단 빌드 재설치 후 정상. 재발 시 XData 원본 덤프 진단부터 (F-012 「참고」) |
| 설치 상태 | 현재 PC 설치본은 **진단 출력 포함 빌드**(명령창에 XData 덤프). 커밋 코드는 제거판 → 다음 설치 때 갱신됨 |
| 설치파일 | `JArchitecture_Setup_11_07.exe` = F-008~F-012 포함(진단판), F-007 제외 |
| F-007 재개 | `git switch F-007` → `git merge master` (README·STATE 충돌 예상). 문서는 그 브랜치의 `docs/dev/features/F-007-Duct_ToRevit.md` 「재개 방법」. JRevit 쪽은 `240716New` 브랜치 |
| 확정 규칙 | DuctTree Poly 는 Leaf 전용, Root 와 같은 레이어만 추적. Root/Mid 는 Line 만 |
| AddinManager | XData 쓰는 명령은 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 |
| 브랜치 | `master` 가 최신, `F-007` 유지 |

**재개 프롬프트:** "/devlog resume — RoomAA 장비 탭 이어서" (F-007 은 "F-007 재개")
