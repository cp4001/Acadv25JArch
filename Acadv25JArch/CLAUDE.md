# Acadv25JArch

AutoCAD 2025 .NET 플러그인 — 건축/배관 자동화 (급수배관 Tree 분석, 관경 결정, Dia 노트 생성).

## Obsidian
- LineTree 관련: `D:\Dropbox\Obsidian\Projects\LineTree\` (모두 `#AutoCAD` 태그, `LineTree.md` 와 `Cmd_*.excalidraw.md` 는 `#Revit`)
  - `LineTreeTechNote.md` — Tree 분석/관경 결정 기술 노트 (`LINETREE`, `LINETREE_FORM`, Supply/Return 모드)
  - `FcuLineTreeTechNote.md` — FCU Tree 분석 (`FCULINETREE`, H-W 공식, Leaf tp CrossingWindow, TreeType 스냅샷)
  - `TreeOverrule.md` — `TTG` 커맨드, Tree XData 기반 색상/LineWeight 오버레이
- DiaNote 관련: `D:\Dropbox\Obsidian\Projects\DiaVerNote\` (모두 `#AutoCAD` 태그)
  - `DiaNote 개요.md` (허브), `DiaTree.md`, `DiaTreeVer.md`, `DiaNoteVer1.md`, `DiaNoteHor.md`, `DiaNoteHor1.md`, `SetDiaNoteBase.md`, `DD.md`, `SusPipe.md`

**노트 태그 규칙**: 각 노트 frontmatter에 `tags: [AutoCAD]` 또는 `tags: [Revit]` 로 플랫폼 구분. 이 프로젝트 관련 노트는 기본 `AutoCAD`, JRevit 관련 노트(`LineTree.md` 등)는 `Revit`.

## QMD 키워드
- Acadv25JArch, AutoCAD plugin, AutoCAD 2025, .NET 8
- LineTree, LineTreeBuilder, LineTreeForm, PipeLoad2
- FcuLineTree, FcuLineTreeBuilder, FcuLineTreeForm, FCULINETREE, FCU
- TreeOverrule, TreeDrawOverrule, TTG, DrawableOverrule, SetXDataFilter
- DiaNote, DiaTree, DiaNoteVer, DiaNoteVer1, DiaNoteHor, DiaNoteHor1, SetDiaNoteBase, BaseLen, SusPipe, susDia
- SupplyDiaCalc, ReturnDiaCalc, CalcMode, 관경결정, 관균등표법, 급수배관, 환탕, 순환탕수, 누적체적, H-W 공식, Hazen-Williams
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
- **`SelectCrossingWindow` 호출 전 Zoom fit 필수** — CrossingWindow는 현재 뷰포트 범위 기준이라 대상 엔티티가 뷰 밖이면 매핑 0건. `GeometricExtents` 합산 → `view.Width/Height` 재설정 + `ed.SetCurrentView` + `UpdateScreen()` (FCULINETREE Phase 1.5).
- **Form → CAD 엔티티 선택 반영**: TreeNode에 `Tag = node.Handle` 저장 → `db.TryGetObjectId(new Handle(long))` → `ed.SetImpliedSelection(new[] { id })` → `doc.Window.Focus()`. Transaction 불필요.
- **DrawableOverrule XData 필터는 `SetXDataFilter` + `IsApplicable`의 `GetXDataForApplication` 2중 검사** — SetXDataFilter만으로는 누수 가능. `TreeOverrule` 패턴 참고.
- **`wd.SubEntityTraits` 변경 시 원복 필수** — Color/LineWeight 등 trait이 다음 Entity 렌더에 누수되면 의도치 않은 시각 효과 발생.
- **Tree/Dia XData 저장은 독립 try-catch** — `FcuLineTreeBuilder.ApplyDiaRecursive` 패턴. 한쪽 실패가 다른 쪽 기록을 막지 않도록.
- **FcuNode.TreeType 스냅샷 사용** — Apply 시점에 `node.Type.ToString()` 재계산하지 않고 BuildTree 종료시 저장된 `TreeType` 문자열 사용.
- **DiaNote.BaseLen 정적 필드** — `Cmd_DiaNoteVer1` / `Cmd_DiaNoteHor1` 의 `baseDist` 기준 길이. `Cmd_SetDiaNoteBase` 로 평행 2 Line 수선거리(`l1.StartPoint` → `l2.GetClosestPointTo(p, true)`) 계산해 저장. 미설정시 초기값 30.0. 평행 검사: `Math.Abs(d1.DotProduct(d2)) >= 0.9999`.
- **LineTreeBuilder.Mode (Supply/Return)** — `LINETREE_FORM` 시작 키워드로 선택. Return = 환탕 모드: 모든 Leaf Load 강제 0.105 (XData "15A"·길이 무시), `node.Diameter = ReturnDiaCalc.FromVolume(node.Load)` lookup. Supply는 기존 `SupplyDiaCalc.Calculate2` 균등표법. 근거 표는 `PipeDiaCalc/ALT1. 환탕관경 계산 방식.xlsx` ([§6.5](D:\Dropbox\Obsidian\Projects\LineTree\LineTreeTechNote.md)).

## 파일/폴더
- 프로젝트 루트: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch\`
- 솔루션: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch.sln`
- 프로젝트 문서/노트는 Obsidian에 기록 (본 디렉토리에 .md 두지 말 것)
- **`PipeDiaCalc/`** — LineTree / FcuLineTree 관련 소스 (관경계산 파이프라인). 모두 `namespace PipeLoad2`:
  - `LineTreeBuilder.cs`, `LineTreeForm.cs/.Designer.cs/.resx`, `LineTreeFormCommand.cs`
  - `FcuLineTreeBuilder.cs`, `FcuLineTreeForm.cs/.Designer.cs/.resx`, `FcuLineTreeFormCommand.cs`
  - csproj `<Compile Update>` / `<EmbeddedResource Update>` 경로는 `PipeDiaCalc\...` 사용 (DependentUpon 은 basename만)
