# Acadv25JArch

AutoCAD 2025 .NET 플러그인 — 건축/배관 자동화 (급수배관 Tree 분석, 관경 결정, Dia 노트 생성).

## Obsidian
- LineTree 관련: `D:\Dropbox\Obsidian\Projects\LineTree\`
  - `LineTreeTechNote.md` — Tree 분석/관경 결정 기술 노트 (`LINETREE`, `LINETREE_FORM`)
  - `FcuLineTreeTechNote.md` — FCU Tree 분석 (`FCULINETREE`, H-W 공식, Leaf tp CrossingWindow 방식)
- DiaNote 관련: `D:\Dropbox\Obsidian\Projects\DiaVerNote\`
  - `DiaNote 개요.md` (허브), `DiaTree.md`, `DiaTreeVer.md`, `DiaNoteVer1.md`, `NoteHor.md`, `NoteHor1.md`, `DD.md`

## QMD 키워드
- Acadv25JArch, AutoCAD plugin, AutoCAD 2025, .NET 8
- LineTree, LineTreeBuilder, LineTreeForm, PipeLoad2
- FcuLineTree, FcuLineTreeBuilder, FcuLineTreeForm, FCULINETREE, FCU
- DiaNote, DiaTree, DiaNoteVer, NoteHor, NoteHor1
- SupplyDiaCalc, 관경결정, 관균등표법, 급수배관, H-W 공식, Hazen-Williams
- JXdata, XData, Dia, 15A, LPM, DD command
- ArchOverrule, EntityArchc, LayerPalette

## Build
- TargetFramework: `net8.0-windows8.0`
- 출력 경로: `C:\Jarch25\` (csproj 고정)
- 빌드: `dotnet build Acadv25JArch.csproj -c Debug`
- AutoCAD 2025 로드: `NETLOAD` → `C:\Jarch25\Acadv25JArch.dll`
- 주요 패키지: EPPlus 8.3.1, OpenStudio.win-x64 3.10.0, Microsoft.VisualStudio.Services.Client 19.225.1

## Conventions
- **XData RegName은 `"Dia"` 단일화** — 과거 `"DD"`는 사용 금지 (2026-04 통일, DiaNote.cs/LineTreeBuilder.cs 모두 `"Dia"`)
- `[CommandMethod("DD")]`의 `"DD"`는 AutoCAD 명령어 이름 (XData 아님) — 유지
- `JXdata.GetXdata`는 string/double 모두 지원 (2026-04 업데이트)
- WinForms → AutoCAD DB 쓰기 시 반드시 `doc.LockDocument()` (eLockViolation 방지)
- Transaction Commit 후 `node.Line` 사용 금지 → `db.GetObjectId(false, handle, 0)` 재획득
- `JXdata.SetXdata`는 라이선스 만료 시 저장 불가 (`MyPlugin.LicenseDate` 체크)
- **Editor selection(`GetSelection`, `SelectCrossingWindow` 등)은 Transaction 밖에서 호출** — 엔티티 메타만 Pre-Transaction(ForRead)에서 추출 후 Commit → Editor 호출 → Main Transaction으로 분리. FCULINETREE 패턴 참고.

## 파일/폴더
- 프로젝트 루트: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch\`
- 솔루션: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch.sln`
- 프로젝트 문서/노트는 Obsidian에 기록 (본 디렉토리에 .md 두지 말 것)
