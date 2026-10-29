# F-004 Insert_Duct_Size — 선택 Tree Line 중앙에 덕트 사이즈 "axb" Text 배치

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-29 / 2026-10-29 |
| 커밋 | `450e0f1` |

## 요청 (원문 그대로)

> 사용자가 XData Reg Tree 인 line을 복수로 선택하면  선택된   lien  의 중간에 axb  a b 는 가가 line  에 있는  XData reg 이고 이 값 예를들면 300x200 Text를 lien 중앙에 표시한다. 글자의 높이는 a   값의 1/2 로 한다.   그리고 a   값이 300 이상인 경우만 표시하고 그 이하는 표시하지 않는다.  [Image #1]  첨부 이미지 참조학고
> 각 line에 있는 XData  정보는
> TypedValue 0 - type: 1001, value: Tree
> TypedValue 1 - type: 1000, value: Mid
> TypedValue 2 - type: 1001, value: Total_CMH
> TypedValue 3 - type: 1000, value: 1200
> TypedValue 4 - type: 1001, value: a
> TypedValue 5 - type: 1000, value: 350
> TypedValue 6 - type: 1001, value: b
> TypedValue 7 - type: 1000, value: 250
> TypedValue 8 - type: 1001, value: Disp
> TypedValue 9 - type: 1000, value: 350x250[1200]
> TypedValue 10 - type: 1001, value: JLicense
> TypedValue 11 - type: 1000, value: JJH
> Total of 12 Xdata.  이런식이야.    명령어 Insert_Duct_Size 로 해줘
> (첨부 그림: 덕트 외곽선 안 Magenta 중심 Line 위 중앙에 흰색 "350x200" Text)

## 명확화

| 질문 | 답 |
|---|---|
| 표시 조건 | `a >= 300` 만 표시 ("300 이상"), 미만은 건너뜀 |
| 길이 조건 (2026-10-29 추가 요청) | "line의 길이가 a 값의 3배 이하면 제외" → `Length <= 3a` 생략. 이후 "Line 길이 ≤ a × 3 에서 4 로 수정해줘" → `4a` |
| Text XData (추가 요청) | "삽입한 글자에 XData Reg는 DuctSize 로하고 값은 실제 글자로 입력해줘" → `DuctSize` = TextString |
| 위치·정렬 | Line 중점, MiddleCenter (그림처럼 중심선 위에 글자 중앙) (가정) |
| 회전 | Line 방향을 따르되 글자가 뒤집히지 않게 (-90°, 90°] 로 정규화 (가정 — 그림은 수평만) |
| 형식 | `{a}x{b}` — XData 문자열 그대로 (예 `350x250`) |
| a/b 누락·비숫자 Line | 해당 Line 만 건너뛰고 개수 보고 (가정 — Insert_UpDn 처럼 전체 중단하지 않음, 텍스트는 Line 별 독립이므로) |
| 레이어·색·스타일 | 현재 레이어 · ByLayer · 현재 문자 스타일 (가정) |

## 범위

- 포함: `Insert_Duct_Size` 명령 (DBText 생성)
- 제외: 재실행 시 기존 사이즈 Text 중복 검사/갱신

## 계획

1. [x] `PipeDiaCalc/DuctSizeTextCommand.cs` 신규 → 검증: `dotnet build`
2. [x] `architecture.md` 명령 인덱스, `CLAUDE.md` 키워드 → 검증: 문서 확인
3. [x] 설치파일 재설치 후 실행 → 검증: 사용자 확인 (a≥300 표시·미만 생략, 높이 a/2, 수직 Line 회전)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctSizeTextCommand.cs` | 신규 — `Insert_Duct_Size` |
| `Acadv25JArch/architecture.md` | 명령 인덱스 행 추가 |
| `Acadv25JArch/CLAUDE.md` | 컨벤션 항목·QMD 키워드 |
| `Acadv25JArch/PipeDiaCalc/InsertDuctSize.md` | 명령 문서 신규 (Confluence `6684674` 사본) |
| `DuctSizeTextCommand.cs` | 1차 확인 후 추가 요청: 길이 ≤ a×3 제외 (`MinLenRatio`), 생략 개수 출력 → 이후 4 로 변경. Text 에 XData `DuctSize` 기록 |

## 검증 결과

- [x] 빌드 (Debug+Release 연결 빌드, 오류 0)
- [x] 설치파일 `JArchitecture_Setup_10_29.exe` 재설치 후 사용자 확인 3회 — 기본 동작 "잘 되는데", 길이 ≤ 4a 제외 확인, `DuctSize` XData "잘 되네"
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 재실행 시 기존 사이즈 Text 중복 검사/갱신 (`DuctSize` XData 로 식별 가능) → backlog
