# F-011 To_Group — XData Tree 선택 필터

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-11-07 / 2026-11-07 |
| 커밋 | `5c5a49d` |

## 요청 (원문 그대로)

> to_group  명령어에서  선택할떄 Xdata Tree   선택 필터링이 없는데

## 명확화

| 질문 | 답 |
|---|---|
| 배경 | F-009 원 요청의 "대상(Xdata Tree,line,poly)" 을 Tree 조건 없음으로 잘못 해석 → 이번에 정정 |
| 해석 | XData `Tree` 가 있는 Line/Poly 만 대상 (가정 — "Tree 있음 또는 Line/Poly" 가 아님) |
| 범위 적용 | 직접 선택·미리 선택 둘 다 |

## 범위

- 포함: 선택 필터에 RegApp `Tree` 추가, 미리 선택도 같은 조건으로 추림
- 제외: `.XData =` 직접 대입 경로 변경 (backlog)

## 계획

1. [x] `To_Group` 수정 → 검증: 빌드
2. [ ] 설치본으로 도면 실행 → 검증: 아래 시나리오 (사용자 결과 미보고)

| 시나리오 | 기대 |
|---|---|
| 선택 없이 실행 | Tree 있는 Line/Poly 만 선택됨 |
| Tree 있는·없는 Line/Poly + 기타 미리 선택 | Tree 있는 Line/Poly 만 기록 |
| Tree 없는 개체만 미리 선택 | "미리 선택된 개체에 XData Tree 가 있는 Line/Poly 가 없습니다." 후 종료 |

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/Util_Command.cs` | `ToGroup_SetXdataToGroup` — 선택 필터 `MakeFilterTypesRegs("LINE,LWPOLYLINE", "Tree")`, 미리 선택에 `JXdata.GetXdata(.., "Tree")` 조건, `using AcadFunction` |
| `Acadv25JArch/architecture.md` | `To_Group` 행 갱신 |

## 검증 결과

- [x] 빌드 (오류 0개, Debug·Release)
- [x] 설치 확인 (번들 `Acadv25JArch.dll` 해시가 Release 빌드와 일치, `JArchitecture_Setup_11_07.exe` 17:25)
- [ ] 테스트 / 실제 실행 확인 — 안 함. 시나리오 3건 사용자 결과 미보고 상태로 `/devlog done` 처리

## 후속 · 남은 일

- 도면 시나리오 3건 확인
- 실패 시 F-011 재개 또는 새 F 등록
