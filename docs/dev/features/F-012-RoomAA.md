# F-012 RoomAA — 실/장비 탭 Form (실 탭)

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | M |
| 요청일 / 완료일 | 2026-11-07 / 2026-11-07 |
| 커밋 | |

## 요청 (원문 그대로)

> 첨부 이미지 같은  winform을 작성할거야.   Form  에는 실 탭과 장비 탭이 있고 
> 실 탭에는 첨주 이미지 같은 데이타그리드 첨부  이미지처럼 배치 될거야? 
> 데이타 그리드에 Room No, 실명, 난방부하 , 냉방부하 , 총풍량 , 비고  로 되어 있어. 
> 사용자가 명령러 RoomAA 를 실행하면   대상이 되는 Polyline을 멀티로 선택하는데 XData Room 인 poly 만  선택하게 한다.
> 각 Poly 내부에는 Xdata Roomtext 인 Text 가 있어서 데이타 그리드의 실명에 적용된다.   각 poly에는  난방부하(HeatLoad),냉방부하(CoolLoad),총풍량(RoomQ),비고(Etc) Xdata 에 기록된 값을 불러온다.
> 만약에 해당 Xdata 가 없으면 각 Poly에 Xdata HeatLoad,CoolLoad,RoomQ,Etc 를  생성하고 Text값으로 " " 값을 기록하고 있으면 값을 읽어서 표시한다.  장비탭은 이걸 완성한다음 작성할게 일단 여기까지 작성

> 추가 요청: "여기서 저장 버턴이 상단에 있어서 사용자가 난방부하, 냉방부하, 총풍량 , 비고 을 입력하면 데이타 그리드에 저장된 값을 해당 Poly에 Xdata 에 저장하는 기능이 있어야해."

(첨부: 실/장비 탭 엑셀 목업 — 장비 탭은 실명 콤보·장비 종류(RPD/RAD/SPD/SAD)·시스템 타입(SA/RA/EA/OA)·+ADD)

## 명확화

| 질문 | 답 |
|---|---|
| Room No. | 선택 순서대로 1, 2, 3… (임시 가정, 이의 없음) |
| Poly 안 RoomText 여러 개 | 가장 위(Y 최대) 1개 (`Diffuser_Summary` 와 동일) |
| RoomText 없음 | 실명 빈칸 + 명령창 알림 |
| 그리드 편집 → XData 저장 | 상단 「저장」 버튼. 빈 칸은 `" "` |
| 잠긴 레이어 Poly | XData 생성·저장 생략, 있는 값만 표시 |

## 범위

- 포함: `RoomAA` 명령, Room Poly 다중 선택, XData 없으면 `" "` 생성, 실/장비 탭 Form(실 탭 그리드 + 저장)
- 제외: 장비 탭 내용

## 계획

1. [x] 명령 + Form/Designer 작성 → 검증: 빌드 오류 0
2. [x] AutoCAD 실행 → 검증: Room Poly 선택 시 그리드 표시 (사용자 확인)
3. [x] 저장 버튼 → 검증: 값 입력·저장 후 `RoomAA` 재실행 시 같은 값 표시 (사용자 확인)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/RoomEquip/RoomAACommand.cs` | 신규 — `RoomAA` 명령, `RoomRow`, `LoadKeys`. 명령창에 「기존 읽음 N / 생성 M」 출력 |
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` / `.Designer.cs` | 신규 — TabControl(실/장비) + 상단 `pnlRoomTop`/`btnSave` + `dgvRoom` 6열(4열 편집). 저장: `LockDocument` + Handle 재획득 → XData 기록 → 되읽기 검증(arx 라이선스 차단 시 조용히 미기록되므로) |
| `Acadv25JArch/architecture.md` | §2 `RoomAA` 행 |
| `Acadv25JArch/CLAUDE.md` | QMD 키워드 |

## 검증 결과

- [x] 빌드 (Release, 연결 빌드로 Debug 포함)
- [x] 실제 실행 확인 (사용자, 설치파일 재설치 후) — 표시 · 저장 · 재실행 시 값 유지

## 참고 — 재실행 시 값 리셋 보고 (원인 미확정)

| 항목 | 내용 |
|---|---|
| 증상 | 저장 후 `RoomAA` 재실행 시 값이 모두 `" "` 로 덮임 |
| 조치 | 코드상 원인 없음 → XData 원본 덤프 진단을 넣은 빌드로 재설치하자 정상 동작. 진단은 제거 |
| 추정 | 이전 설치본 미반영(구 DLL 로드) 가능성 — 증거 없음. 재발 시 덤프 진단부터 다시 넣을 것 |

## 후속 · 남은 일

- 장비 탭 (실명 콤보, 장비 종류, 시스템 타입, +ADD) — 사용자 사양 대기
