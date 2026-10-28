# F-001 Insert_UpDn — 덕트 교차점에 UpDn 동적 블럭 배치

| 항목 | 값 |
|---|---|
| 상태 | 완료 |
| 규모 | S |
| 요청일 / 완료일 | 2026-10-28 / 2026-10-28 |
| 커밋 | `7a1a4e5` (코드·문서) · `20cdf0d` (JArch_Blocks.dwg) |

## 요청 (원문 그대로)

> 좋아 .  그럼 이 명령어 참조해서  Insert_Updn 을 작성할거야.
> 사용자가 2개의 line을 선택할거야 Xdata  Rag Tree 가 있는 lien이고 XData reg a 에 덕트 폭이 있어.
> 선택된 2 개의 Duct Line의 교차점에 Dynamic Block UpDn 을 배치 할거야.
> Block  속성에 DS,DL 이 있는데  DS 에는 선택된 2 line 중 작은 a  값을  DL 에는 큰값을 넣어서 변경한다.
> 2개의 duct line에 적은 a 값 line 의 각도을 읽어서 배치된 블럭의 각도에 일력한다.

## 명확화

| 질문 | 답 |
|---|---|
| 참조 도면의 블럭 이름 | `UpDn` (`JArch_` 접두사 없음) |
| DS / DL 종류 | 동적 속성(파라미터) — `Dis1`/`Dis2` 와 같은 방식 |
| 회전 기준 | 작은 a Line 의 `line.Angle` 그대로 (Start→End) |
| 두 Line 이 만나는 방식 | 서로 교차, 직각이 아닐 수도 있음. **끝점이 아니라 선 안쪽에서 겹침** → `Intersect.OnBothOperands` (2026-10-28 수정, 처음엔 `ExtendBoth`) |
| 선택 방식 (2026-10-28 수정) | 한 번에 선택(`GetSelection`) + XData `Tree` 필터, 2개가 아니면 재선택 |
| 참조 명령 | `Insert_Damper` (`DamperInsertCommand.cs`) |

## 범위

- 포함: Line 2개 선택(XData `Tree` 필수), XData `a` 숫자 파싱, 교차점에 `UpDn` 삽입, DS=작은 a / DL=큰 a, 회전=작은 a Line 각도, 블럭 정의는 `JArchBlockLibrary.Import`, 비상수 Attribute 기본값 부착
- 제외: `"600x400"` 형태 `a` 파싱(Insert_Damper 와 동일하게 오류), 반복 삽입, 레이어 지정

## 계획

1. [x] `PipeDiaCalc/UpDnInsertCommand.cs` 작성 → 검증: `dotnet build` 성공
2. [x] `PipeDiaCalc/InsertUpDn.md` + `architecture.md` 명령 인덱스 + `CLAUDE.md` 키워드 → 검증: 문서 확인
3. [x] AutoCAD 실제 실행 (설치파일 재설치 후) → 검증: 사용자 확인

## 변경 내역

| 파일 | 변경 |
|---|---|
| `Acadv25JArch/PipeDiaCalc/UpDnInsertCommand.cs` | 신규 — `Insert_UpDn` 명령. 검증(Tree/a/교차점)을 Import 앞에 둠 |
| `Acadv25JArch/PipeDiaCalc/UpDnInsertCommand.cs` | 수정 — 개별 `GetEntity` 2회 → `GetSelection` + `LINE`/`Tree` 필터, 2개 될 때까지 재선택. 교차 판정 `ExtendBoth` → `OnBothOperands`. `PickDuctLine`·Tree 수동 검사 제거 |
| `Acadv25JArch/PipeDiaCalc/InsertUpDn.md` | 신규 — 명령 문서 |
| `Acadv25JArch/architecture.md` | 명령 인덱스에 `Insert_UpDn` 행 추가 |
| `Acadv25JArch/CLAUDE.md` | QMD 키워드 1줄 추가 |

## 검증 결과

- [x] 빌드 (Debug+Release 연결 빌드, 오류 0)
- [x] 참조 도면 `UpDn` 블럭 존재 — accoreconsole `tblsearch` 로 확인
- [x] 설치파일 `JArchitecture_Setup_10_28.exe` (ISCC 성공) → 재설치 후 AutoCAD 실행, 사용자 "테스트 잘 됐어"
- 자동 테스트 없음 (AutoCAD 명령, 수동 확인만)

## 후속 · 남은 일

- 참조 도면 `Blocks\JArch_Blocks.dwg` 에 `UpDn` 블럭 추가 — 사용자 작업 완료
- `a` 가 `"600x400"` 형태일 때 첫 숫자 추출 → backlog
