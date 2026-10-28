# F-002 Insert_Damper — 검증 후 블럭 Import, Line 길이 검사

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-28 / 2026-10-28 |
| 커밋 | `562bc91` |

## 요청 (원문 그대로)

> 5번은 이경우 에러메세지지 표시하고 명령어 중단. 6번 진행

(6번 = backlog "`Insert_Damper` — Import 를 XData 검증 뒤로 이동(오류 시 정의 덮어쓰기 방지), Line 길이 < 225 검사")

## 명확화

| 질문 | 답 |
|---|---|
| 5번 (`"600x400"` 형태 `a`) | 현재 동작 유지 — 오류 메시지 후 중단. DECISIONS 기록, 코드 변경 없음 |
| Line 길이 < 225 처리 | 5번과 같은 원칙 적용(가정) — 오류 메시지 후 중단, 블럭 정의도 건드리지 않음 |

## 범위

- 포함: `DamperInsertCommand.cs` — 검증(XData a, 길이)을 읽기 Transaction 으로 분리해 `JArchBlockLibrary.Import` 앞으로 이동, 길이 < `Offset` 검사 추가
- 제외: 삽입·속성·동적 속성 로직 변경

## 계획

1. [x] 검증 Transaction 분리 + 길이 검사 → 검증: `dotnet build`
2. [x] `InsertDamper.md` 단계표 갱신 (+ Confluence 사본 `98516` 갱신 여부 확인) → 검증: 문서 확인
3. [x] 설치파일 재설치 후 실행 → 검증: 사용자 확인 (정상 삽입 / 짧은 Line 오류 / a 없는 Line 오류)

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/DamperInsertCommand.cs` | XData a·길이 검증을 읽기 Transaction 으로 분리해 `Import` 앞으로 이동. 길이 검사 `< 1e-6` → `< Offset(225)`, 메시지에 실제 길이 표시. 삽입·속성 로직 그대로 |
| `Acadv25JArch/PipeDiaCalc/InsertDamper.md` | 단계표 순서·길이 검사·`600x400` 처리 확정 반영 |
| `Acadv25JArch/PipeDiaCalc/InsertUpDn.md` | Damper 비교표 Import 시점 갱신 |
| `Acadv25JArch/CLAUDE.md` | Insert_Damper / Insert_UpDn 항목 갱신 |
| `docs/dev/DECISIONS.md` | backlog #5 결정(비숫자 a → 오류 후 중단) 기록 |

## 검증 결과

- [x] 빌드 (Debug+Release, 오류 0)
- [x] 설치파일 `JArchitecture_Setup_10_28.exe`(19:50, Release 재빌드 후 ISCC) 재설치 → `Insert_Damper` 3케이스, 사용자 "테스트 잘 됐어"
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 없음 (backlog #5 는 DECISIONS 로 종료)
