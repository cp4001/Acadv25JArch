# Acadv25JArch

AutoCAD 2025 .NET 플러그인 — 건축/배관 자동화 (급수배관 Tree 분석, 관경 결정, Dia 노트 생성).

## Obsidian
- LineTree 관련: `D:\Dropbox\Obsidian\Projects\LineTree\` (모두 `#AutoCAD` 태그, `LineTree.md` 와 `Cmd_*.excalidraw.md` 는 `#Revit`)
  - `LineTreeTechNote.md` — Tree 분석/관경 결정 기술 노트 (`LINETREE`, `LINETREE_FORM`)
  - `FcuLineTreeTechNote.md` — FCU Tree 분석 (`FCULINETREE`, H-W 공식, Leaf tp CrossingWindow, TreeType 스냅샷)
  - `TreeOverrule.md` — `TTG` 커맨드, Tree XData 기반 색상/LineWeight 오버레이
- DiaNote 관련: `D:\Dropbox\Obsidian\Projects\DiaVerNote\` (모두 `#AutoCAD` 태그)
  - `DiaNote 개요.md` (허브), `DiaTree.md`, `DiaTreeVer.md`, `DiaNoteVer1.md`, `NoteHor.md`, `NoteHor1.md`, `DD.md`

**노트 태그 규칙**: 각 노트 frontmatter에 `tags: [AutoCAD]` 또는 `tags: [Revit]` 로 플랫폼 구분. 이 프로젝트 관련 노트는 기본 `AutoCAD`, JRevit 관련 노트(`LineTree.md` 등)는 `Revit`.

## QMD 키워드
- Acadv25JArch, AutoCAD plugin, AutoCAD 2025, .NET 8
- LineTree, LineTreeBuilder, LineTreeForm, PipeLoad2
- FcuLineTree, FcuLineTreeBuilder, FcuLineTreeForm, FCULINETREE, FCU
- TreeOverrule, TreeDrawOverrule, TTG, DrawableOverrule, SetXDataFilter
- DiaNote, DiaTree, DiaNoteVer, NoteHor, NoteHor1
- SupplyDiaCalc, 관경결정, 관균등표법, 급수배관, H-W 공식, Hazen-Williams
- JXdata, XData, Dia, Tree XData, 15A, LPM, DD command
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
- **DrawableOverrule XData 필터는 `SetXDataFilter` + `IsApplicable`의 `GetXDataForApplication` 2중 검사** — SetXDataFilter만으로는 누수 가능. `TreeOverrule` 패턴 참고.
- **`wd.SubEntityTraits` 변경 시 원복 필수** — Color/LineWeight 등 trait이 다음 Entity 렌더에 누수되면 의도치 않은 시각 효과 발생.
- **Tree/Dia XData 저장은 독립 try-catch** — `FcuLineTreeBuilder.ApplyDiaRecursive` 패턴. 한쪽 실패가 다른 쪽 기록을 막지 않도록.
- **FcuNode.TreeType 스냅샷 사용** — Apply 시점에 `node.Type.ToString()` 재계산하지 않고 BuildTree 종료시 저장된 `TreeType` 문자열 사용.

## 파일/폴더
- 프로젝트 루트: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch\`
- 솔루션: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch.sln`
- 프로젝트 문서/노트는 Obsidian에 기록 (본 디렉토리에 .md 두지 말 것)
