# F-010 Insert_Duct_Size·Duct_tag — XData Group 표시

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-07 / 2026-11-07 |
| 커밋 | `48dd2e8` |

## 요청 (원문 그대로)

> 여기서 대상  Duct  Line에  XData Group 정보가 있으면 이 정보도 표시해줘.   예를 들어 Group  에 SA 가 있으면  SA:300x200  이런 식으로. DUct_tag  명령어도 마찬가지로 적용해줘

## 명확화

| 질문 | 답 |
|---|---|
| 형식 | `{Group}:{a}x{b}` (예 `SA:300x200`). Group 값 문자열 그대로 |
| Group 없거나 빈 값 | 기존과 동일 `{a}x{b}` (가정) |
| `Insert_Duct_Size` 의 Text XData `DuctSize` | 글자 그대로 = Group 포함 (가정 — "값은 실제 글자" 규칙 유지) |
| `a<300`·길이 ≤ 4a 필터 | 변경 없음 (가정 — Group 글자 길이는 계산에 넣지 않음) |
| `Duct_tag` 수평 앞 공백 1칸 | 유지 (`" SA:300x200"`) |

## 범위

- 포함: 두 명령의 표시 글자에 Group 접두. 글자 생성은 `DuctSizeTextCommand.SizeText` 하나로 공유
- 제외: 재실행 시 중복 검사/갱신(backlog), Poly Leaf 대응

## 계획

1. [x] `SizeText` 추가, 두 명령 적용 → 검증: 빌드
2. [x] 도면 실행 → 검증: 아래 시나리오

| 시나리오 | 기대 |
|---|---|
| `Insert_Duct_Size` — Group=SA Line | `SA:300x200`, XData `DuctSize` 도 같은 값 |
| `Insert_Duct_Size` — Group 없는 Line | `300x200` |
| `Duct_tag` — Group=SA 수평 Line | `" SA:300x200"` |
| `Duct_tag` — Group=SA 수직 Line | `SA:300x200` |
| `Duct_tag` — Group 없는 Line | 기존과 동일 |

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DuctSizeTextCommand.cs` | `SizeText` 헬퍼 신규, `TextString` 에 적용 |
| `Acadv25JArch/PipeDiaCalc/DuctTagCommand.cs` | Transaction 안에서 `label` 복사, Text·완료 메시지에 적용 |
| `Acadv25JArch/PipeDiaCalc/InsertDuctSize.md`, `DuctTag.md`, `architecture.md`, `CLAUDE.md` | 글자 규칙 갱신 |

## 검증 결과

- [x] 빌드 (오류 0개, Debug, `C:\Jarch25`)
- [x] 테스트 / 실제 실행 확인 (사용자 "모두 정상" 보고, AddinManager + Debug DLL. 설치본 테스트는 안 함)

참고: AddinManager 는 임시 폴더에서 실행하며 `JArchXData.arx` 가 복사되지 않는다. `Insert_Duct_Size` 는 실행 전 `(arxload "C:/Jarch25/JArchXData.arx")` 필요 (없으면 "Exception has been thrown by the target of an invocation", 사용자 확인으로 해결).

## 후속 · 남은 일

- 설치본 반영은 F-008·F-009 와 함께 다음 설치파일 재생성 때
