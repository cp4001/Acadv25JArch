# Acadv25JArch (저장소 루트)

주 플러그인은 `Acadv25JArch\` 하위 폴더 — 빌드·배포·컨벤션은 **`Acadv25JArch\CLAUDE.md`** 를 따른다.
나머지 폴더는 설치(`JArchitecture_Setup\`)·라이선스(`C++\`)·보조 도구 (구성은 `docs/dev/features/F-000-기준선.md`).

## Confluence (사본)
- 상위 페이지 `131081` "JArchitecture - AutoCAD 플러그인 명령어 정리" (원본 `Acadv25JArch\architecture.md`)
- 하위: PolyViewManager `66058` · InsertDiffuser `66025` · InsertDamper `98516` · InsertUpDn `6193153` · InsertDuctSize `6684674` · DuctTag `6881281` · JArchBlockLibrary `66042` · DuctTreeTechNote `98499` · TreeOverrule `327705` · autocadInstall `98533`
- 원본 md 를 고치면 사본 갱신 여부 확인. 전체 목차: `~/.claude/docs/confluence_index.md`

## QMD 키워드
- 상세 목록은 `Acadv25JArch\CLAUDE.md` 「QMD 키워드」 — Insert_UpDn, Insert_Duct_Size, Duct_tag, Insert_Damper, JArchBlockLibrary, DuctTree, devlog, F-001

## Devlog
- 개발 기록: `docs/dev/` (README 이력 표 · STATE 현재 위치 · features/F-NNN)
- 세션 시작 시 `docs/dev/STATE.md` 를 읽고 이어서 작업
- 커밋 메시지: `[F-NNN] 요약`
- 기능 추가/수정 요청은 `/devlog new` 로 등록 후 진행, 끝나면 `/devlog done`
