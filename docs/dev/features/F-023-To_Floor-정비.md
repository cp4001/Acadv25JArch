# F-023 To_Floor 정비 — Room Poly 다중 선택·사용자 입력 층 이름

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-28 / 2026-11-28 |
| 커밋 | `f727dec` · Confluence `131081` v25 |

## 요청 (원문 그대로)

> RoomAA  명령어 에서  Room poly 에  Xdata Floor  정보가 있으면 Floor Clolumn  에 Floor  를 표시해줘.
> 그리고 to_Floor 명령어를 작성할거야(  선택한  room Poly 에 Xdata Floor  정보를 입력하는 명령어 .  Room poly 멀티로 선택가능 하다.  Floor  정보는 사용자가 입력한 정보를 사용)

## 명확화

| 질문 | 답 |
|---|---|
| RoomAA Floor 열 | F-014 에서 이미 구현됨(`colFloor`, XData `Floor` 읽기) — 변경 없음 |
| `To_Floor` 가 이미 있음(`RoomCalc.cs`, Block·LWPOLYLINE 대상, 창호 Block 집계 `EntityArchc` 가 사용) | **기존 명령 정비** — Block 선택 유지 |

## 범위

- 포함: 선택 안내문 수정, 층 이름 입력 취소·빈 값이면 중단(전엔 `"Defalut Floor"` 로 진행), 기록/잠긴 레이어 건너뜀 개수 출력
- 제외: 선택 필터 변경(INSERT,LWPOLYLINE 유지), RoomAA 변경

## 계획

1. [x] `Cmd_Entity_Set_Floor` 수정 → 검증: Release 빌드 오류 0
2. [ ] (실행 결과 기록 없음) 설치 후 `To_Floor` → Room Poly 여러 개에 층 입력 → `RoomAA` 층 열 표시 확인 (사용자)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/RoomCalc.cs` | `Cmd_Entity_Set_Floor`: 층 이름 취소·빈 값이면 중단(`"Defalut Floor"` 진행 제거, 앞뒤 공백 Trim), 선택 안내문 "Room Poly 또는 Block 을 선택하세요 (여러 개 가능)", 0개 선택 시 종료, 끝에 기록·잠긴 레이어 건너뜀 개수 출력 |

## 검증 결과

- [x] 빌드 (Release, 오류 0)
- [x] 설치파일 `JArchitecture_Setup_11_28.exe` 생성·실행
- [ ] 실제 실행 확인 — 결과 기록 없음

## 후속 · 남은 일

- 실행 확인(`To_Floor` → `RoomAA` 층 열) 미기록
