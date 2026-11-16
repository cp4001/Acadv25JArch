# F-015 RoomAA 실 탭 — 실명 클릭 시 Poly 화면 중앙 이동

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-16 / 2026-11-16 |
| 커밋 | |

## 요청 (원문 그대로)

> 실탭에서 사용자가 설명을 클릭하면  CAD  도면에서  해당 poly를 화면 중앙에 배치하게 해줘.

## 명확화

| 질문 | 답 |
|---|---|
| "설명" 은 어느 열 | 실명(`colName`) 열만 — 부하값 편집 셀 클릭엔 반응 안 함 |
| 중앙 배치 방식 | 배율 유지, Poly extents 중심으로 Pan 만 |

## 범위

- 포함: `dgvRoom.CellClick` → 실명 열이면 `Row.Tag` Handle 의 Poly extents 중심으로 `ZoomCenter`(배율 1.0)
- 제외: 확대, Poly 선택(하이라이트)

## 계획

1. [x] `dgvRoom_CellClick` 추가(생성자에서 이벤트 연결 — Designer 무변경), 기존 `ZoomCenter` 재사용 → 검증: 빌드 오류 0
2. [ ] 설치 후 `RoomAA` → 실명 클릭 → 검증: 배율 그대로 Poly 가 화면 중앙 (사용자 확인)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/RoomEquip/RoomEquipForm.cs` | `dgvRoom_CellClick` — 실명 열만, Handle → Poly `GeometricExtents` 중심 → `LockDocument` 안에서 `Editor.ZoomCenter(center)` + `UpdateScreen`. 삭제된 Poly 는 경고 |

## 검증 결과

- [x] 빌드 (Release 오류 0, 설치파일 `JArchitecture_Setup_11_16.exe` 재생성 — F-014 포함)
- [ ] 실제 실행 확인 — 설치파일 실행까지 함. 사용자가 결과 보고 없이 `/devlog done` 으로 마감 (실행 결과 미기록)

## 후속 · 남은 일

- Poly 하이라이트(선택) 동시 표시는 범위 밖 — 필요 시 요청
