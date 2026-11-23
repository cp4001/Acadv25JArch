# 현재 상태 (2026-11-23)

| 항목 | 내용 |
|---|---|
| 진행 중 | 없음. F-007 은 **보류** — 브랜치 `F-007` |
| 마지막 한 일 | 2026-11-23: F-021 PLVM 줌 수정 완료 (`ZoomToEntities` DCS 변환 + 노드 클릭 판정, 설치파일 `JArchitecture_Setup_11_23.exe`, 실행 확인 완료). 2026-11-20: F-020 `Duct_Edit` 완료 (`DuctTreeCommand.cs`, 설치파일 `JArchitecture_Setup_11_20.exe`, 실행 결과 기록 없음). 2026-11-17: `ss1` F-019 (SelectAll 전체 탐색 + Editor 선택 Transaction 밖, 설치파일 `JArchitecture_Setup_11_17.exe`). 2026-11-16: `RoomAA` F-014·F-015 (실행 확인 완료), `CSS` F-016~018. F-016~019 는 설치까지, 실행 결과 기록 없음 |
| 다음 할 일 | 사용자 지시 대기. 후보는 아래 「다음 후보」·backlog (`ss1` XData·레이어·Arc/Poly, `CSS`/`ss1` 대형 도면 속도) |
| 막힌 점 | 없음 |
| 커밋 상태 | 모두 커밋·푸시 완료 (`master` 최신, F-021 `b61624a`), Confluence `131081` v24 (F-021 까지) · F-016 페이지 `13074433` (131081 하위) · F-017 `13107201` · F-018 `13139969`. F-019 `13238273` · F-020 `14516226` · F-021 `15237121` · PolyViewManager `66058` v4 |

## 다음 후보

| 후보 | 출처 | 메모 |
|---|---|---|
| `RoomAA` 장비 종류 확장 (디퓨져 외 FCU 등) | F-013 후속 | `txtEqKind` 는 ReadOnly "디퓨져" 고정 |
| `Insert_Diffuser_Bypoly` 로 RoomAA 가 만든 Text 배치 확인 | F-013 | 미확인 — 형식은 `Diffuser_Spec` 과 같아 동작 예상 |
| Poly Leaf 대응 (`Insert_Duct_Size`·`Duct_tag`·`Insert_UpDn`·`Duct_ToRevit`) | F-008 backlog | |

## RoomAA 분석에서 나온 점 (2026-11-16, 미결정 — 사용자가 고르면 F 등록)

코드 읽기로 찾은 것이고 실행 확인은 안 함.

| # | 내용 | 위치 |
|---|---|---|
| 1 | RoomText 가 MiddleCenter 등 정렬이면 Spec Text X 가 어긋날 수 있음 — `Position` 대신 `AlignmentPoint` 기준 필요 | `RoomEquipForm.cs` `AddSpecText` |
| 2 | 명령 실행만으로 XData `" "` 생성 → 도면 수정·Undo 기록 (F-012 요청대로, 의도된 동작) | `RoomAACommand.cs` |
| 3 | Poly 선택(`GetEntityByTpye`)이 Transaction 안 — 프로젝트 규칙(Editor 선택은 밖)과 다름, 현재 문제 없음 | `RoomAACommand.cs` |
| 4 | Poly 가 겹치면 같은 Spec Text 가 두 실에 표시·쌓기 기준에 포함 | `LoadEquip` / `AddSpecText` |
| 5 | 풍량 칸: `LoadEquip` 은 파싱값 `sp.Cfm`, +ADD 는 입력 텍스트 → 표기 차이 가능 | `RoomEquipForm.cs` |
| 6 | 폼 열린 채 도면 닫으면 `_doc` 참조로 예외 가능 (미확인) | `RoomEquipForm` |

## RoomAA 코드 지도

| 위치 | 내용 |
|---|---|
| `RoomEquip/RoomAACommand.cs` | 명령 `RoomAA`, `RoomRow`(No·Name·Floor·Heat·Cool·RoomQ·Etc·Handle), `LoadKeys`(Floor 는 미포함 — 읽기 전용) |
| `RoomEquip/RoomEquipForm.cs` | 생성자(그리드·콤보 채움 → `LoadEquip`, `dgvRoom.CellClick` 연결) · `dgvRoom_CellClick`(실명 열 → Poly extents 중심 `ZoomCenter`) · `btnEqAdd_Click` → `AddSpecText`(LockDocument, Commit 후 `FlushGraphics`+`UpdateScreen`) · `btnSave_Click`(4값 저장, 열 위치 `colHeat.Index + i`) |
| `RoomEquip/RoomEquipForm.Designer.cs` | 사용자가 VS 에서 편집 — 재생성 금지. 실 탭 열: `colNo`·`colName`·`colFloor`·`colHeat`·`colCool`·`colRoomQ`·`colEtc` |
| `PipeDiaCalc/DiffuserInsertCommand.cs` | 공유: `Types`·`SystemTypes`·`SpecText`·`TryParseSpec`·`IsInsidePoly`·`SpecColorIndex`·`SpecObliqueDeg` |
| `CadFunction.cs` `ZoomExtensionMethods` | `ZoomCenter`(배율 유지 Pan)·`Zoom`·`ZoomToEntities` — 재사용 |

## 재개 시 알아둘 것

| 항목 | 내용 |
|---|---|
| RoomEquipForm Designer | 사용자가 VS 에서 배치를 고침 → **재생성 금지, Edit 로 최소 추가**. 이벤트 연결은 생성자에서 |
| 테스트 절차 | AutoCAD 종료 → `dotnet build Acadv25JArch\Acadv25JArch.csproj -c Release` → `ISCC JArchitecture_Setup\JArchitecture_Setup.iss` → 설치 → 실행 |
| Confluence 갱신 | `131081` 은 본문이 커서 html 결과가 파일로 저장됨 → 스크립트로 해당 `data-local-id` 문단 추출·수정 → `replaceNode` → markdown 으로 다시 받아 원본 행과 대조. RoomAA 문단 id `749e001c7fc6` |
| F-012 리셋 이슈 | 재실행 시 값이 `" "` 로 덮이던 현상 — 원인 미확정, 이후 재발 없음. 재발 시 XData 원본 덤프 진단부터 (F-012 「참고」) |
| 설치파일 | `JArchitecture_Setup_11_17.exe` = F-008~F-019 포함, F-007 제외 |
| F-007 재개 | `git switch F-007` → `git merge master` (README·STATE 충돌 예상). 문서는 그 브랜치의 `docs/dev/features/F-007-Duct_ToRevit.md` 「재개 방법」. JRevit 쪽은 `240716New` 브랜치 |
| 확정 규칙 | DuctTree Poly 는 Leaf 전용, Root 와 같은 레이어만 추적. Root/Mid 는 Line 만. RoomAA 장비 행 삭제 기능은 만들지 않음 |
| AddinManager | XData 쓰는 명령은 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 |
| 브랜치 | `master` 가 최신, `F-007` 유지 |

**재개 프롬프트:** "/devlog resume" (F-007 은 "F-007 재개")
