# F-005 Duct_tag — 지시선 + 덕트 사이즈 Text 태그

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-29 / 2026-10-29 |
| 커밋 | |

## 요청 (원문 그대로)

> [Image #3]  그림처럼     상용자가 line(Xdata Reg Tree) 를 선택하고 원하는 위치는 선택하면 첨부그림처럼 Duct Size 글자를 표시한다.  수평인 경우는 지시선 이후에 한번 꺼은 다음 글자를 배치하고 수직은 직선 표시후 글자는 배치한다.  글자 크기는 선택된 line의 a 값을 사용한다.   명령어은 Duct_tag 이다.
> (첨부 그림: 수평 덕트 — Line 위 빨간 점에서 수직 지시선 위/아래로 → 글자 `500x350`. 수직 덕트 — Line 위 빨간 점에서 수평 직선 오른쪽 → 글자 `350x250`)

## 명확화

| 질문 | 답 |
|---|---|
| 글자 높이 | **a 그대로** (사용자 선택 — `Insert_Duct_Size` 의 a/2 와 다름) |
| 수평 Line 지시선 | 점 → 클릭 높이까지 수직선 → 오른쪽으로 짧은 수평선(길이 = 글자 높이) → 글자 (사용자 선택) |
| 수직 Line 지시선 | 점 → 클릭 X 까지 수평 직선 → 글자. 클릭이 왼쪽이면 글자도 왼쪽(오른쪽 정렬) (가정) |
| 색·레이어 | 지시선·점 ACI 1 Red, 글자 ByLayer, 현재 레이어·문자 스타일 (사용자 선택) |
| 반복 | Esc 까지 반복 (사용자 선택) |
| 점 위치 | 클릭점을 Line 선분에 투영한 점 (가정) |
| 수평/수직 판정 | Line 방향 `abs(dx) >= abs(dy)` 면 수평 (기울어진 Line 은 가까운 쪽) (가정) |
| 점 모양·크기 | 채워진 원(DONUT 방식 Polyline), 지름 = 글자 높이 × 0.3. 지시선 끝~글자 간격 = 높이 × 0.2 (가정) |
| 글자 | `{a}x{b}` XData 문자열 그대로. a 300 미만·길이 조건 없음 (사용자가 직접 고르므로) |

## 범위

- 포함: `Duct_tag` 명령 (지시선 Line + 점 Polyline + DBText)
- 제외: 생성물 XData 기록, 그룹/블럭화, 기존 태그 갱신

## 계획

1. [x] `PipeDiaCalc/DuctTagCommand.cs` 신규 → 검증: `dotnet build`
2. [x] architecture·CLAUDE.md → 검증: 문서 확인
3. [x] 설치파일 재설치 후 실행 → 검증: 사용자 확인 (수평: 꺾인 지시선, 수직: 직선, 높이 a)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctTagCommand.cs` | 신규 — `Duct_tag` |
| `Acadv25JArch/architecture.md` | 명령 인덱스 행 추가 |
| `Acadv25JArch/CLAUDE.md` | 컨벤션 항목·QMD 키워드 |
| `Acadv25JArch/PipeDiaCalc/DuctTag.md` | 명령 문서 신규 |

## 검증 결과

- [x] 빌드 (Debug+Release 연결 빌드, 오류 0) + 설치파일 `JArchitecture_Setup_10_29.exe`
- [x] 설치파일 재설치 후 사용자 확인 — "잘 되네"
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 없음 (그룹화·XData 는 요청 시)
