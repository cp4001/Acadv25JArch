# F-006 Duct_tag — 글자 0.8a, 수평 꺾은선 a/2, 글자 앞 공백

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-29 / 2026-10-29 |
| 커밋 | |

## 요청 (원문 그대로)

> dcut_tag 글자 사이즈는  a 값의 0.8  정도로 하고 수평에서 꺽은선 길이는 a/4 정도로 해줘

## 명확화

| 질문 | 답 |
|---|---|
| 대상 | `Duct_tag` (F-005) — "dcut_tag" 는 오타로 판단 |
| 추가 요청 | "수평에서 꺽은선 다음에 글자가 너무 가까우니까 글자를 쓸떄 맨앞에 스페이스 를 하나 넣고 글자를 넣어줘" → 수평 Line 만 TextString 앞에 공백 1칸 (수직은 그대로) |
| 추가 요청 2 | "꺽은선 길이가 a/4 인데 a/2  정도로 수정해줘" → `TailRatio 0.5` |
| 점 지름·글자 간격 | 글자 높이 비례 유지 (0.3h · 0.2h) → 글자와 함께 0.8배 (가정) |

## 범위

- 포함: `TextRatio = 0.8`, `TailRatio = 0.5` 상수화 (처음 0.25)
- 제외: 수직 Line 지시선 (클릭 위치까지라 변경 없음)

## 계획

1. [x] `DuctTagCommand.cs` 상수 변경 → 검증: 컴파일 (AutoCAD 실행 중이라 임시 폴더 빌드)
2. [x] Release 빌드 + 설치파일 (공백 반영 17:02 DLL) → 검증: 사용자 확인

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctTagCommand.cs` | `h = a × TextRatio(0.8)`, 꺾은선 `a × TailRatio` (0.25 → 0.5) |
| `Acadv25JArch/PipeDiaCalc/DuctTagCommand.cs` | 수평 Line 글자 앞 공백 1칸 |
| `Acadv25JArch/PipeDiaCalc/DuctTag.md` · `architecture.md` · `CLAUDE.md` | 값 반영 |

## 검증 결과

- [x] 컴파일 (임시 폴더, 오류 0)
- [x] Release 빌드 + 설치파일(17:06 DLL) 재설치 후 사용자 확인 — "잘 되네"
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 없음. Confluence `DuctTag`(6881281) 사본은 값이 바뀌어 갱신 필요 여부 확인
