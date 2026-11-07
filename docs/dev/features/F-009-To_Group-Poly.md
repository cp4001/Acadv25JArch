# F-009 To_Group — Poly 지원, 미리 선택 반영

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-07 / 2026-11-07 |
| 커밋 | `0cff034` |

## 요청 (원문 그대로)

> 사용자가 대상(Xdata Tree,line,poly) 를 선택하면 Xdata Rag Group 에   사용자가 입력한 글자를 기로하는 프로그램 
> 명령어 GG 를 작성해줘.

추가 요청 (기존 `To_Group` 확인 후, GG 신규 대신 이것으로 변경):

> to_group 이 있는지 몰랐네.   to_Group 에 poly  도 선택 가능하게 해줘.  선택 된 것이 없이 명령어가 실행되면 선택하게 하고 미리 선택된 것이 있으면 미리 선택된 개체에서 Line ,poly만 대상으로 해서 XData Group에 기록한다.

## 명확화

| 질문 | 답 |
|---|---|
| "Rag Group" 의 뜻 | XData RegApp `"Group"` (기존 `To_Group` 과 같은 키) |
| GG 신규 명령 | 만들지 않음 — `To_Group` 수정으로 대체 (사용자 요청) |
| "poly" 범위 | `LWPOLYLINE` (구형 `POLYLINE` 제외). 프로젝트 Poly 처리와 동일 |
| Tree XData 조건 | 없음 — 추가 요청에 조건 없고 기존 `To_Group` 도 없음 |

## 범위

- 포함: `To_Group` 대상에 Poly 추가, 미리 선택 시 그중 Line/Poly 만 기록, 미리 선택 없으면 선택 요청
- 제외: `.XData =` 직접 대입 경로 변경 (backlog 라이선스 항목 — 현 상태 유지 결정)

## 계획

1. [x] `To_Group` 수정 → 검증: 빌드
2. [x] AddinManager + Debug DLL 로 도면 실행 → 검증: 아래 시나리오

| 시나리오 | 기대 |
|---|---|
| 선택 없이 실행 | Line/Poly 만 선택됨 → 값 입력 → 기록 |
| Line+Poly+기타 미리 선택 | 추가 선택 없이 값 입력 → Line/Poly 만 기록 |
| 기타 개체만 미리 선택 | "미리 선택된 개체에 Line/Poly 가 없습니다." 후 종료 |

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | `ToGroup_SetXdataToGroup` — `SelectImplied` 로 미리 선택 분기, DxfName 으로 Line/Poly 추림, 선택 필터 `LINE,LWPOLYLINE` |

## 검증 결과

- [x] 빌드 (오류 0개, Debug)
- [x] 테스트 / 실제 실행 확인 (사용자 테스트 통과 보고, AddinManager + `C:Jarch25Acadv25JArch.dll`. 설치본 테스트는 안 함)

## 후속 · 남은 일

- 설치본 반영은 F-008 다음 설치파일 재생성 때 함께 (아직 설치본에 없음)
