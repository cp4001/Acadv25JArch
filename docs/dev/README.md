# JArchitecture (Acadv25JArch) 개발 기록

AutoCAD 2025 .NET 플러그인 — 건축 실·면적 집계, 급수/FCU 배관 Tree 관경 결정, Duct Tree·외곽선, 디퓨저·댐퍼 배치. 설치·라이선스·보조 도구 포함.

- 현재 위치: [STATE.md](STATE.md) · 설계 결정: [DECISIONS.md](DECISIONS.md) · 보류: [backlog.md](backlog.md)
- 커밋 규칙: `[F-NNN] 요약` → `git log --grep "F-NNN"` 으로 기능별 조회
- 명령어 상세는 `Acadv25JArch\architecture.md`, 기술 상세는 기능별 md·Obsidian `LineTree` (이 폴더는 진행·이력)

## 기능 이력

| ID | 제목 | 규모 | 요청일 | 완료일 | 상태 |
|---|---|---|---|---|---|
| [F-000](features/F-000-기준선.md) | devlog 이관 기준선 | — | 2026-10-15 | 2026-10-15 | 기준선 |
| [F-001](features/F-001-Insert_UpDn.md) | Insert_UpDn — 덕트 교차점 UpDn 블럭 배치 | S | 2026-10-28 | 2026-10-28 | 완료 |

## 이관 전 이력

git log (2025-08-26 ~ 2026-10-13, 402 커밋) 주제별 요약. 기준선: [F-000](features/F-000-기준선.md)

| 기간 | 주제 | 대표 커밋 |
|---|---|---|
| 2025-08 | 저장소 생성, 레이어 상태 저장, 면적 복제(CloneToAreaCalc), 블럭 카운트·Entity 복사 | `2d8ed02` `afb4bfc` `cf069be` |
| 2025-09 | 경계 추적(TB/TB1/TB_rr), 라인 그룹핑·센터라인, 벽 라인 정리, 룸폴리 계산, 방향 분석, 블럭 Explode | `7b450d1` `24a27d2` `faabe7f` `7736a4e` |
| 2025-10 | 방위각·교차 판정 정비, Polyline Boolean, 라인선택→폴리 | `5e1308c` `725ba57` `eeadbe0` |
| 2025-11 | 가시성·거리 연산 확장, 클릭 선택, 내부 경계 추적, 층고·외벽·북쪽 벡터 기반 실 집계 | `9149cd9` `3a0d519` `e7fc673` `859ac68` |
| 2025-12 | ConvexHull, 실별 창·문·외벽 집계(RoomPart), Excel 출력(CLTD·영문), 급수관경 WinForm 도구 | `004b380` `bd2a6b8` `d73dad6` `f25c6c3` |
| 2026-01 ~ 02 | LineTree 를 PipeLoad 에 병합, 라이선스 날짜 체크(C++), Excel 시트 정비 | `031e47f` `a45b351` `8ced385` |
| 2026-04 | DiaTree·DiaNote, LineTree 개선, 급수 관경 결정 로직, FcuLineTree, SusDia, Overrule 폴더 정리 | `8386b40` `a0f44ca` `6bca262` `ece078a` |
| 2026-05 | LineTree Root/Mid/Leaf·총부하 기록, DuctSizing·DuctTree, CMHT·PWD, 리본·팔레트, Ainit | `a7332cd` `6066399` `31d3120` |
| 2026-06 ~ 07 | Duct 외곽선 Case1/2·Elbow, Duct_C1/C2/E/EE, DuctTree Outline Form, CommandUtil(CSS·SCL·T3·EC·ss1) | `c451b40` `79be1fd` `93fe758` `bc5976f` `39a717c` |
| 2026-08 | LPMT, Duct_Etv, DuctTree Leaf Fence 선택, XData·라이선스 C++ arx 전환, DuctOutline 버튼 | `8e99d30` `2f3951e` `2892f96` `8ce9480` |
| 2026-09 | Supabase 라이선스, Duct_C1E/C2E, To_RoomCFM·TTG CFM, Insert_Diffuser/Spec, JArch_Blocks.dwg, PolyView | `5e534d8` `26b6105` `d2174c0` `8f9f9eb` `767dacf` |
| 2026-10 | Diffuser_Summary, PolyView 정비, 인스톨러 블럭 도면 원본 패키징, Insert_Damper 속성 | `1f9bad2` `e43ff28` `f61e4c5` `c06af58` |
