# F-014 RoomAA 실 탭 — 층(colFloor) 열 추가

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-16 / 2026-11-16 |
| 커밋 | |

## 요청 (원문 그대로)

> 실탭에서  실명(conName) 옆에 층( ColFloor)을 추가해줘  room Poly 에 지정된 Xdata Floor 정보를 사용한다.   편집은 ReadOnly 이다.  Room poly 의 Xdata Floor 읽어오는 기능만 있고 없어도 XData Floor 를 생성 하지는 않는다.

## 명확화

| 질문 | 답 |
|---|---|
| 위치 | `colName` 바로 오른쪽 (3번째 열) |
| Floor 없음 | 빈칸 표시, XData 생성 안 함 |
| 편집·저장 | ReadOnly, 「저장」 대상 아님 |

## 범위

- 포함: `colFloor` 열, `RoomRow.Floor`, Poly XData `Floor` 읽기
- 제외: Floor XData 생성·저장

## 계획

1. [x] Designer `colFloor` 추가(Edit 최소) + `RoomRow.Floor` + 저장 루프 열 인덱스 보정 → 검증: 빌드 오류 0
2. [x] 설치 후 `RoomAA` → 검증: Floor 있는 Poly 는 값 표시, 없는 Poly 는 빈칸·XData 미생성, 저장 후 4값 정상 (사용자 확인)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/RoomEquip/RoomAACommand.cs` | `RoomRow` 에 `Floor`(Name 다음) 추가, `JXdata.GetXdata(pl, "Floor") ?? ""` 읽기만 |
| `Acadv25JArch/RoomEquip/RoomEquipForm.Designer.cs` | `colFloor`(층, ReadOnly, 폭 80) 를 `colName` 뒤에 추가 — 기존 배치 무변경 |
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` | 행 추가에 `r.Floor`, 저장 루프 `Cells[i + 2]` → `Cells[colHeat.Index + i]` (열 삽입에도 안 어긋나게) |

## 검증 결과

- [x] 빌드 (Release, 오류 0)
- [x] 실제 실행 확인 (사용자, `JArchitecture_Setup_11_16.exe` 재설치 후 — 층 표시·저장 정상)

## 후속 · 남은 일

-
