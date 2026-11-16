# F-016 CSS — XData 필터 제거

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-16 / 2026-11-16 |
| 커밋 | |

## 요청 (원문 그대로)

> XData Pipe Duct MainPipe FirePipe  만  추적하게 되어 있는데 XData  필터링은 제외하고 적용해줘

> css 에서 시작 Entity  선택에서 선택후 Enter를 해야 하는데 어차피 1개만 선택 하니까 선택후 바로 실행되게 수정해줘

## 범위

- 포함: `CSS` 시작 선택·체인 추적 필터에서 XData RegApp 조건 제거 (타입 `LINE,ARC,LWPOLYLINE` 만 유지)
- 제외: `ss1` Target 필터(`PIPE_REGAPPS` 계속 사용), 줌 맞춤·T분기 양방향 등 분석에서 나온 다른 개선

## 계획

1. [x] `Util_Command.cs` `CSS_DuctPipe_SelectChain` 필터에서 `ExtendedDataRegAppName` 제거 → 검증: 빌드
2. [x] 시작 선택 `SingleOnly` — 1회 선택 후 Enter 없이 진행 → 검증: 빌드
3. [ ] XData 없는 Line 체인에서 CSS 실행 → 검증: 연결 체인 전체 선택 (사용자 설치 후 done 요청, 실행 결과 보고는 없음)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | CSS 필터 `TypedValue` 에서 RegApp 조건 삭제 |
| `Acadv25JArch/Util_Command.cs` | 시작 선택 `PromptSelectionOptions.SingleOnly`/`SinglePickInSpace` — 1회 선택 후 Enter 없이 진행 |
| `Util_Command/CSS.md` | §2 선택 필터 — XData 조건 없음으로 갱신 |

## 검증 결과

- [x] 빌드 (Release, 오류 0) · 설치파일 `JArchitecture_Setup_11_16.exe` 생성
- [ ] 테스트 / 실제 실행 확인 — 설치(`JArchitecture_Setup_11_16.exe`)까지 함, 실행 결과는 기록 안 됨

## 후속 · 남은 일

- backlog: `SelectCrossingWindow` 전 Zoom fit 없음(화면 밖 체인 끊김), T분기 한 방향만 감지(선택된 선 중간에 붙은 분기 누락)
- 영향: 레이어 무관 모든 Line/Arc/LWPolyline 이 끝점 연결로 이어지므로 벽선 등 비배관 선까지 선택될 수 있음
