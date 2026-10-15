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
