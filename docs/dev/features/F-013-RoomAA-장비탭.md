# F-013 RoomAA 장비 탭 — +ADD 로 Diffuser Spec Text 생성

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | M |
| 요청일 / 완료일 | 2026-11-13 / 2026-11-13 |
| 커밋 | `d67cb50` (F-012 Designer 등록 `3918bfd`) · Confluence `131081` v20 |

## 요청 (원문 그대로)

> 장비 탭에  첨부파일처럼  실명 설비종류 설비타입 시스템 타임을 선택하고 Add  버턴을 누르면  아래쪽  테이타 그리드뷰에 설비가 추가 되는 기능을 추가 하고 싶어.  추가된 설비는 명령어 Diffuser_Spec  명령어를 참조해서 해단 롬에 Diffuser Spec Text를 첨가 해줘.

(첨부: 장비 탭 엑셀 목업 — 실명 콤보 · 장비 종류 "디퓨져" · 장비 종류 RPD/RAD/SPD/SAD · 시스템 타입 SA/RA/EA/OA · +ADD / 도면 예 — HALL-3 아래 `2400,RPD,SA,3`, `2400,RPD,RA,3`)

## 명확화

| 질문 | 답 |
|---|---|
| Spec 의 CFM·개수 출처 | 장비 탭에 입력칸 추가 (풍량 TextBox, 개수 NumericUpDown) |
| Text 생성 시점 | +ADD 누를 때 즉시 |
| Text 위치 | RoomText 아래로 쌓기 — 실 안 RoomText·기존 Spec Text 중 가장 아래 것 밑 (줄 간격 높이×1.8). 높이·스타일은 RoomText, RoomText 없으면 Poly extents 중심 |

## 범위

- 포함: 장비 탭 입력부 + `dgvEquip`, +ADD → Spec Text 생성(ACI 41, 기울기 5°, XData `Diffuser`=Type — `Diffuser_Spec` 과 동일)
- 포함(추가): 폼 재실행 시 기존 Spec Text 를 그리드에 불러오기
- 제외: 그리드 행 삭제 → Text 삭제

## 계획

1. [x] Designer 장비 탭 컨트롤 추가(기존 배치 유지) + `btnEqAdd_Click` → 검증: 컴파일 오류 0
2. [x] 설치 후 `RoomAA` → 장비 탭 +ADD → 검증: 실 안 RoomText 아래 Spec Text 생성, `Insert_Diffuser_Bypoly` 로 배치 가능 (사용자 확인 — Text 생성·재실행 복원·즉시 표시. Bypoly 배치는 별도 확인 안 함)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/RoomEquip/RoomEquipForm.Designer.cs` | `pnlEquipTop`(실명·장비 종류·설비 타입·시스템 타입·풍량·개수·+ADD) + `dgvEquip` 6열. 실 탭 배치 무변경 |
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` | 콤보 채움, `btnEqAdd_Click`, `AddSpecText`(LockDocument, Handle 재획득, XData 되읽기 검증) |
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` | `LoadEquip` — 폼 열 때 각 실 Poly 안 기존 Spec Text(XData `Diffuser`)를 `TryParseSpec` 으로 읽어 `dgvEquip` 에 채움 (실 순서, 위→아래). 사용자 요청: 재실행 시 입력 내용 다시 표시 |
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` | `AddSpecText` Commit 후 `FlushGraphics` + `ed.UpdateScreen()` — 모덜리스 폼이라 도면 클릭 전까지 새 Text 가 안 보이던 것 수정 |
| `Acadv25JArch/PipeDiaCalc/DiffuserInsertCommand.cs` | `Types`·`SpecText`·`SpecColorIndex`·`SpecObliqueDeg`·`TryParseSpec` private → internal (공유) |

## 검증 결과

- [x] 빌드 — Release 오류 0, 설치파일 `JArchitecture_Setup_11_13.exe`
- [x] 실제 실행 확인 (사용자) — +ADD 시 RoomText 아래 Spec Text 생성, 재실행 시 그리드 복원, 즉시 화면 표시

## 후속 · 남은 일

- 그리드 행 삭제 → 도면 Text 삭제 (backlog)
- 장비 종류는 "디퓨져" 고정 — 다른 장비(FCU 등) 추가 시 확장
