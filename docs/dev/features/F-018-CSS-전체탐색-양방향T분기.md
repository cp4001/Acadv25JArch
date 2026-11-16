# F-018 CSS — 화면 밖 체인 추적 + T분기 양방향

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-16 / 2026-11-16 |
| 커밋 | `cb9d112` · Confluence `13139969` |

## 요청 (원문 그대로)

> 1번(줌 맞춤 추가)과 2번(T분기 양방향 감지)  수정해줘

## 명확화

| 질문 | 답 |
|---|---|
| 1번 줌 맞춤 방식 | Zoom 대신 `SelectAll` 로 후보 전체 수집(뷰포트 무관) — 체인 범위를 미리 알 수 없어 줌 대상을 정할 수 없고, 축소 화면에서 10 단위 crossing 창은 화소 이하가 돼 신뢰 어려움. 화면도 바뀌지 않음 (Claude 판단, 사용자에게 보고) |

## 범위

- 포함: 후보 수집을 `SelectAll` 로, 연결 판정 양방향(후보 끝점 → 현재 몸통 추가), Poly 꼭짓점 분기 포함
- 제외: 허용 거리 값 변경, `ss1`/`SCL`/`EC`

## 계획

1. [x] `Util_Command.cs` CSS 탐색부 재작성 — SelectAll + Entity BFS + `EndOnBody` 양방향 → 검증: 빌드
2. [ ] 화면 밖까지 이어진 체인, 주관 중간에 붙은 분기 도면에서 CSS → 검증: 전부 선택

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | `SelectCrossingWindow` 점 스택 → `SelectAll` 후보 목록(같은 레이어·같은 `OwnerId`) + Entity Queue BFS. 헬퍼 `EndOnBody`·`ExtentsNear`·`TryExtents` 추가, `CHAIN_SEARCH_BOX` 삭제 (`CHAIN_END_TOL` 은 다른 명령이 써서 유지) |
| `Util_Command/CSS.md` | v1.2 — §7 표 Q3/Q4/Q6 갱신, §8 변경 사항 표 추가 |

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_16.exe` 재생성
- [ ] 테스트 / 실제 실행 확인 — 설치(`JArchitecture_Setup_11_16.exe`)까지 함, 실행 결과는 기록 안 됨 (사용자 done 요청)

## 후속 · 남은 일

- 같은 레이어 엔티티가 매우 많은 도면에서 속도 확인 (후보 N × 체인 M bbox 비교)
- 동작 변화: 주관 중간에 끝점이 닿은 같은 레이어 선은 이제 모두 선택됨
