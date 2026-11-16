# F-017 CSS — 시작 Entity 와 같은 레이어만 추적

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-16 / 2026-11-16 |
| 커밋 | `cb9d112` · Confluence `13107201` |

## 요청 (원문 그대로)

> css엣  Xdata  필터링은 제외 됬지만 start Entity 와  같은 Layer에 있는 Entity 만 추적하게 해줘 layer 가 다르면 제외

## 범위

- 포함: 체인 후보의 레이어가 시작 Entity 레이어와 다르면 제외 (대소문자 무시)
- 제외: 시작 선택 필터(레이어 제한 없음), 그 외 CSS 동작

## 계획

1. [x] `Util_Command.cs` — 시작 Entity 레이어 집합 수집, 후보 레이어 불일치 시 `continue` → 검증: 빌드
2. [ ] 다른 레이어 선이 끝점에 닿은 도면에서 CSS → 검증: 같은 레이어만 선택

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | `layers` HashSet(OrdinalIgnoreCase) — 시작 Entity 레이어 등록, 후보 `cand.Layer` 불일치 시 제외 |
| `Util_Command/CSS.md` | §2 — 레이어 조건 추가 |

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_16.exe` 재생성
- [ ] 테스트 / 실제 실행 확인 — 설치(`JArchitecture_Setup_11_16.exe`)까지 함, 실행 결과는 기록 안 됨 (사용자 done 요청)

## 후속 · 남은 일

- 창 선택으로 시작 Entity 를 여러 레이어에서 잡으면 그 레이어들 전체를 추적
