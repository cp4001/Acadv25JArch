# 설계 결정

추가만 한다. 결정이 바뀌면 기존 행을 지우지 말고 새 행에 "대체: 이전 날짜" 를 적는다.

| 날짜 | 결정 | 이유 | 관련 |
|---|---|---|---|
| 이관 전 | 관경 결정 근거는 원본 엑셀(위생배관관경결정6.5.xls)이 정본, md 명세 신뢰 금지 | md 명세와 엑셀 불일치 전례 | 프로젝트 메모리 |
| 이관 전 | FCULINETREE: Leaf tp 에서만 CrossingWindow + LPM 필터 선행 | 전체 끝점 스캔은 오탐·성능 문제 | `LineTree\FcuLineTreeTechNote.md` |
| 이관 전 | DrawableOverrule: `SetXDataFilter` 에 더해 `IsApplicable` 에서 XData 재확인 + SubEntityTraits 원복 | 필터만으로는 오적용 | `Overrule\TreeOverrule.md` |
| 이관 전 | TTG 라벨은 Line `Disp` 최우선, 파이프 15A/Total15A 는 그 아래 | DuctTree `{a}x{b}` 표시 우선 | `Overrule\TreeOverrule.md` |
| 이관 전 | 사용자가 조정한 WinForms Designer.cs 는 재생성 금지, Edit 로 최소 변경 (코드 추가 컬럼은 생성자에서 재구성) | VS Designer 가 컬럼·SelectedIndex 누락 | 프로젝트 메모리 |
| 2026-04-30 | JArch 리본 탭은 NOD `AINIT_DEFAULTS` 가 있는 도면에서만 표시 (DLL 로드 시 자동 생성 안 함) | 도면별 BaseLen 영구 저장과 게이트 통합 | architecture.md §15·16 |
| 2026-07-28 | 배포는 Release 산출물만, 네이티브 모듈은 Release\|x64 (Debug CRT 재배포 불가), `.iss` 에 `#error` 가드 | 고객 PC 로드 실패 전례 | `JArchitecture_Setup\autocadInstall.md` |
| 2026-07-28 | Debug `C:\Jarch25\` / Release `C:\Jarch25\Release\` 출력 분리 | 경로로 구성 판별 | `Acadv25JArch\CLAUDE.md` |
| 2026-08-26 | 라이선스·XData 기록을 `JArchXData.arx` + Supabase 서버 판정으로 전환, `JArchLicense.dll` 배포 중단 | 네이티브 모듈 일원화 | architecture.md §17 |
| 이관 전 | 코드 수정 테스트는 설치파일 재설치로 (NETLOAD 무효) | 번들 DLL 이 먼저 로드, 언로드 불가 | 프로젝트 메모리 |
| 2026-10-15 | 개발 기록을 저장소 루트 devlog(`docs/dev/`)로 전환. 기능별 사양 md·architecture.md 는 레퍼런스로 유지 | 기능별 이력·세션 재개 | [F-000](features/F-000-기준선.md) |
| 2026-10-28 | XData `a` 가 `"600x400"` 처럼 숫자가 아니면 `Insert_Damper`/`Insert_UpDn` 은 오류 메시지 후 명령 중단 (첫 숫자 추출 안 함) | 사용자 확정 — 덕트 폭이 애매한 Line 은 사람이 확인 | F-001 후속, backlog #5 종료 |
| 2026-10-28 | `SuperBase_License\` 를 이 저장소에서 삭제 확정 | `AutoCADFunction_22_6_28\SuperBase_License\` 로 이전됨(같은 파일 존재 확인), 라이선스는 `JArchXData.arx` 로 전환 | backlog #1 |
| 2026-10-28 | 설치파일 `JArchitecture_Setup\Output\` 은 git 에서 제외(`.gitignore` + 기존 7개 `rm --cached`), 로컬에만 보관 | 회당 ~6MB 바이너리로 저장소 비대. ISCC 로 언제든 재생성 | backlog #2, 사용자 선택 |
| 2026-10-28 | `급수관로결정1\` 은 `.gitignore`, `.claude\settings.json`(omnisharp-lsp 플러그인) 은 커밋 | 전자는 자체 `.git` 을 가진 별도 저장소 — 커밋하면 gitlink 로 깨짐 | backlog #3 |
| 2026-10-28 | `Insert_UpDn`/`_All` 교차 판정은 3D 그대로 둔다(XY 투영 안 함). Z 가 어긋난 Line 은 `ZZERO` 로 평탄화 후 실행 | 높이가 안 맞는 도면은 다른 작업에도 문제 → 도면을 고친다. 필요해지면 추후 개발(backlog) | F-003 테스트 중 발생 |
