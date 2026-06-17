# Acadv25JArch

AutoCAD 2025 .NET 플러그인 — 건축/배관 자동화 (급수배관 Tree 분석, 관경 결정, Dia 노트 생성).

## Obsidian
- LineTree 관련: `D:\Dropbox\Obsidian\Projects\LineTree\` (모두 `#AutoCAD` 태그, `LineTree.md` 와 `Cmd_*.excalidraw.md` 는 `#Revit`)
  - `LineTreeTechNote.md` — Tree 분석/관경 결정 기술 노트 (`PipeTreeCon`, `PipeTree`, Supply/Return 모드)
  - `FcuLineTreeTechNote.md` — FCU Tree 분석 (`FCUTREE`, H-W 공식, Leaf tp CrossingWindow, TreeType 스냅샷)
  - `DuctTreeTechNote.md` — Duct Tree 분석 (`DUCTTREE`, CMH 누적, `Total_CMH` XData, Spec 산정 미구현 — 2026-05-14 신규)
  - `TreeOverrule.md` — `TTG` 커맨드, Tree XData 기반 색상/LineWeight 오버레이 + Block `"Disp"` XData 센터 텍스트
  - `LPM.md` — `LPM` 명령 (Block `"LPM"`/`"Disp"` XData 동시 기록)
  - `CMH.md` — `CMH` 명령 (디퓨져 풍량, LPM 과 동일 패턴, `"CMH"`/`"Disp"` 동시 기록 — 2026-05-14 신규) + `CMHT` (키보드 입력 버전 — 2026-05-18 추가)
  - `pipeFcutree.md` — **BricsCAD V21 이식 사양서** (PipeTree/FcuTree/TTG 전체: 알고리즘·상수·XData·UI·헬퍼·namespace 매핑 단일 자급 문서 — 2026-06-05 신규)
- DiaNote 관련: `D:\Dropbox\Obsidian\Projects\DiaVerNote\` (모두 `#AutoCAD` 태그)
  - `DiaNote 개요.md` (허브), `DiaTree.md`, `DiaTreeVer.md`, `DiaNoteVer1.md`, `DiaNoteHor.md`, `DiaNoteHor1.md`, `SetDiaNoteBase.md`, `SetDiaNoteBaseText.md`, `DD.md`, `SusPipe.md`
- Architecture(인프라): `D:\Dropbox\Obsidian\Projects\Architecture\` (`#AutoCAD`)
  - `RibbonMenu.md` — JArch 리본 탭 (`Ribbon/CollabRibbon.cs`) 구조·가시성 lifecycle·`SetRibbonTabVisible` / `RefreshDiaNoteHeight` API
  - `AinitCommand.md` — `Ainit` 명령 + `DwgDefaultLoader` (NOD `AINIT_DEFAULTS/DiaNoteHeight` ↔ `DiaNote.BaseLen` 어댑터)
  - `PaletteSample.md` — `SHOWPAL`/`HIDEPAL` 도구 팔레트 (`PaletteSample.cs`), lazy init + 버튼 Tag 기반 `SendStringToExecute`
  - `Architecture.md` — 전체 명령 인덱스, `DiaTreeNote.md`

**노트 태그 규칙**: 각 노트 frontmatter에 `tags: [AutoCAD]` 또는 `tags: [Revit]` 로 플랫폼 구분. 이 프로젝트 관련 노트는 기본 `AutoCAD`, JRevit 관련 노트(`LineTree.md` 등)는 `Revit`.

## QMD 키워드
- Acadv25JArch, AutoCAD plugin, AutoCAD 2025, .NET 8
- LineTree, LineTreeBuilder, LineTreeForm, PipeLoad2, PipeTree, PipeTreeCon (구 LINETREE_FORM/LINETREE)
- FcuLineTree, FcuLineTreeBuilder, FcuLineTreeForm, FCUTREE, FCU
- DuctTree, DuctTreeBuilder, DuctTreeForm, DuctTreeCommand, DUCTTREE, CMH, CMHT, Total_CMH, 디퓨져, 풍량, 공조, Cmd_Block_SetCMH, Cmd_Block_SetCMH_Typed
- TreeOverrule, TreeDrawOverrule, TTG, DrawableOverrule, SetXDataFilter, Disp, BlockReference Disp
- DiaNote, DiaTree, DiaNoteVer, DiaNoteVer1, DiaNoteHor, DiaNoteHor1, SetDiaNoteBase, BaseLen, SusPipe, susDia
- SupplyDiaCalc, ReturnDiaCalc, CalcMode, 관경결정, 관균등표법, 급수배관, 환탕, 순환탕수, 누적체적, H-W 공식, Hazen-Williams
- JXdata, XData, Dia, Tree XData, 15A, Total15A, TotalLPM, LPM, DD command, TotalEquiv
- ArchOverrule, EntityArchc, LayerPalette
- Ribbon, CollabRibbon, JArch tab, RibbonPanel, RibbonRowPanel, RibbonRowBreak, RibbonLabel, SetRibbonTabVisible, RefreshDiaNoteHeight, AINIT_DEFAULTS, Ainit, DwgDefaultLoader, KEY_DiaNoteHeight
- PaletteSample, PaletteHost, PaletteSet, SHOWPAL, HIDEPAL, MyToolsControl, SendStringToExecute, FlowLayoutPanel

## Build
- TargetFramework: `net8.0-windows8.0`
- 출력 경로: `C:\Jarch25\` (csproj 고정)
- 빌드: `dotnet build Acadv25JArch.csproj -c Debug`
- AutoCAD 2025 로드: `NETLOAD` → `C:\Jarch25\Acadv25JArch.dll` (`DuctSizing.Core.dll` 도 같은 폴더에 자동 배포 — `CopyLocalLockFileAssemblies=true`)
- 주요 패키지: EPPlus 8.3.1, OpenStudio.win-x64 3.10.0, Microsoft.VisualStudio.Services.Client 19.225.1
- **ProjectReference**: `..\DuctSizing.Core\DuctSizing.Core.csproj` (net8.0, `DuctSizingCalculator.ModeD` 등 덕트 사이즈 산정 로직 공유) — 2026-05-19 추가

## Conventions
- **XData RegName은 `"Dia"` 단일화** — 과거 `"DD"`는 사용 금지 (2026-04 통일, DiaNote.cs/LineTreeBuilder.cs 모두 `"Dia"`)
- `[CommandMethod("DD")]`의 `"DD"`는 AutoCAD 명령어 이름 (XData 아님) — 유지
- `JXdata.GetXdata`는 string/double 모두 지원 (2026-04 업데이트)
- WinForms → AutoCAD DB 쓰기 시 반드시 `doc.LockDocument()` (eLockViolation 방지)
- Transaction Commit 후 `node.Line` 사용 금지 → `db.GetObjectId(false, handle, 0)` 재획득
- `JXdata.SetXdata`는 라이선스 만료 시 저장 불가 (`MyPlugin.LicenseDate` 체크)
- **Editor selection(`GetSelection`, `SelectCrossingWindow` 등)은 Transaction 밖에서 호출** — 엔티티 메타만 Pre-Transaction(ForRead)에서 추출 후 Commit → Editor 호출 → Main Transaction으로 분리. FCUTREE 패턴 참고.
- **`SelectCrossingWindow` / `SelectWindow` 호출 전 Zoom fit 필수** — CrossingWindow/Window는 현재 뷰포트 범위 기준이라 대상 엔티티가 뷰 밖이면 매핑 0건. `ZoomExtensionMethods.ZoomToEntities(this Editor, IEnumerable<ObjectId>, double padding=1.1)` 확장 메서드 사용 (`CadFunction.cs`, `namespace AcadFunction`). 빈 시퀀스/`GeometricExtents` 예외 안전 + 기본 5% 패딩. `FCUTREE` Phase 1.5, `LPM` `Cmd_Block_SetLPM` 가 사용. 같은 클래스의 `ZoomObjects` 는 패딩 없이 정확 fit (UCS 변환 사용) — 경계 매칭이 필요 없는 일반 zoom fit 용도.
- **Form → CAD 엔티티 선택 반영**: TreeNode에 `Tag = node.Handle` 저장 → `db.TryGetObjectId(new Handle(long))` → `ed.SetImpliedSelection(new[] { id })` → `doc.Window.Focus()`. Transaction 불필요.
- **DrawableOverrule XData 필터는 `SetXDataFilter` + `IsApplicable`의 `GetXDataForApplication` 2중 검사** — SetXDataFilter만으로는 누수 가능. `TreeOverrule` 패턴 참고.
- **`wd.SubEntityTraits` 변경 시 원복 필수** — Color/LineWeight 등 trait이 다음 Entity 렌더에 누수되면 의도치 않은 시각 효과 발생.
- **Tree/Dia XData 저장은 독립 try-catch** — `FcuLineTreeBuilder.ApplyDiaRecursive` / `LineTreeBuilder.ApplyDiaRecursive` 동일 패턴 (2026-05 LineTree에도 `Tree` XData 기록 적용). 한쪽 실패가 다른 쪽 기록을 막지 않도록. `Tree` 값: FCU는 `node.TreeType`(BuildTree 스냅샷), LineTree는 `node.Type.ToString()` 직접.
- **FcuNode.TreeType 스냅샷 사용** — Apply 시점에 `node.Type.ToString()` 재계산하지 않고 BuildTree 종료시 저장된 `TreeType` 문자열 사용.
- **DiaNote.BaseLen 정적 필드** — `Cmd_DiaNoteVer1` / `Cmd_DiaNoteHor1` 의 `baseDist` 기준 길이. 두 가지 설정 방법: ① `Cmd_SetDiaNoteBase` — 평행 2 Line 수선거리(`l1.StartPoint` → `l2.GetClosestPointTo(p, true)`) 계산. 평행 검사 `Math.Abs(d1.DotProduct(d2)) >= 0.9999`. ② `Cmd_SetDiaNoteBaseText` (2026-05-18) — `PromptDoubleOptions`(기본값 현재 `BaseLen`, 음수/0 금지)로 키보드 입력값 직접 저장. 두 명령 모두 `DwgDefaultLoader.SaveBaseLen` 으로 NOD `AINIT_DEFAULTS/DiaNoteHeight` 영속화. 미설정시 초기값 30.0.
- **LineTreeBuilder.Mode (Supply/Return)** — `PipeTree` 시작 키워드로 선택. Return = 환탕 모드: 모든 Leaf Load 강제 0.105 (XData "15A"·길이 무시), `node.Diameter = ReturnDiaCalc.FromVolume(node.Load)` lookup. Supply는 기존 `SupplyDiaCalc.Calculate2` 균등표법. 근거 표는 `PipeDiaCalc/ALT1. 환탕관경 계산 방식.xlsx` ([§6.5](D:\Dropbox\Obsidian\Projects\LineTree\LineTreeTechNote.md)).
- **`Total15A` XData** (Supply 모드 한정, 2026-05-05) — `SupplyDiaCalc.Calculate2(..., out double total)` 오버로드로 캡처 → `LineNode.TotalEquiv` → `ApplyDiaRecursive` 가 `Mode == Supply && TotalEquiv > 0` 일 때만 `JXdata.SetXdata(line, "Total15A", val.ToString("0.###"))` 기록. 독립 try-catch (Tree/Dia와 분리). `tr.CheckRegName("Total15A")` 등록 필요. Return 모드에서는 의미 없음 → 저장 생략. `TTG` 라벨이 이 값을 읽어 `15[Total15A]-15A`(Leaf) / `<Dia>[<Total15A>]`(Mid/Root) 표시.
- **`TTG` 라벨 우선순위** (`TreeOverrule.DrawLineTree`, 2026-05-19 갱신) — ⓪ **Line `Disp` 존재 → 그 값을 그대로 라벨로 사용 (Duct: `"{a}x{b}"`)** ① `15A` 존재 → `<Dia>[<Total15A>]-<15A>` (LineTree Leaf, 분석 전후 동일 — 분석 전엔 Dia/Total15A가 없어 `"00"` 폴백 표시) ② `15A` 없고 `Total15A` 존재 → `<Dia>[<Total15A>]` (LineTree Mid/Root, Supply 모드) ③ 그 외 → 라벨 없음. `Dia` / `Total15A` 가 비면 `"00"` 으로 폴백 (`?? "00"`). LineTree/FCU 는 Line 에 `Disp` 를 기록하지 않으므로 충돌 없음. **이전(`15[...]` 고정 prefix + `TotalLPM` FCU 폴백)은 주석 처리되어 제거됨** — FCU Mid/Root는 더이상 라벨 표시 안 함. Mid 색상은 ACI 3(Green) — 기존 Blue(5)에서 변경.
- **`TTG` Block `"Disp"` 텍스트** (2026-05-05 추가) — `TreeDrawOverrule` 을 Line용/Block용 두 인스턴스로 분리. Line: `SetXDataFilter("Tree")`. BlockReference: `SetXDataFilter("Disp")` + `IsApplicable` 에서 `GetXDataForApplication("Disp")` 2중 검사. Block 본체는 `base.WorldDraw` 로 정상 렌더 후 `MText` 추가 — `GeometricExtents` 에서 `min(width,height) * 0.5` 를 TextHeight 로, geo 센터(MinPoint+MaxPoint)/2 에 ACI 1(Red) MiddleCenter rotation 0 으로 표시. 단일 클래스 + 생성자 `bool isBlockMode` 분기 (`DrawLineTree` / `DrawBlockDisp`).
- **Block `"Disp"` XData 동시 기록** (2026-05-05 추가) — `Cmd_Block_SetLPM` 가 LPM 값을 저장할 때 `"LPM"` 과 `"Disp"` 양쪽에 같은 문자열 기록. `tr.CheckRegName("Disp")` 등록 필요. `"LPM"` 은 PipeFitting 분석용 원본값, `"Disp"` 는 `TTG` 시각화용 표시값(향후 표시 포맷 분리 가능). `TTG` 토글 시에도 `RegisterRegApp(db, "Disp")` 호출. **`Cmd_Block_SetCMH` 도 동일** — `"CMH"` + `"Disp"` 동시 기록 (2026-05-14).
- **CMH / DUCTTREE 워크플로우** (2026-05-14 추가, 2026-05-20 Mode D + Form 입력 확장) — `Cmd_Block_SetCMH` 가 Block BBox 안 Text 에서 풍량값 추출 → `"CMH"` + `"Disp"` XData 기록 (LPM 과 동일 패턴, 내부 헬퍼 `TryGetLpmByWindowSelect` 공유 — 이름만 LPM, 로직 RegName-agnostic). `DUCTTREE` 명령 (`DuctTreeCommand` / `DuctTreeBuilder`) 이 Leaf Block `"CMH"` 를 부하로 인식, 각 Line 노드에 누적값을 `"Total_CMH"` XData 로 기록 + `"Tree"`(Root/Mid/Leaf) 스냅샷 동시 기록. **`DuctTreeForm` 상단 `pnlMode` (Dock=Top, height 45)** 에 Supply/Return RadioButton + b 최소/최대 `NumericUpDown` 두 개 노출 (Min/Max=100/2000, Increment=50, 기본값 200/500 — `DuctSizing1` Mode D 탭 기본값 일치). Apply 시 `ApplyTotalCmh(node, db, DuctType, bMin, bMax)` 로 전달 (`bMin > bMax` 검증 + 입력 오류 dialog). **Mode D 사이즈 산정** — `Load > 0` 인 Line 노드마다 `DuctSizingCalculator.ModeD(Load, ductType, α=ModeD_Alpha, bMin, bMax, aspectMax=ModeD_AspectMax)` 호출(`ModeD_Alpha=1.0` / `ModeD_AspectMax=1.5` 만 `DuctTreeBuilder` 내 private const, bMin/bMax 는 파라미터화) → `"a"`(장변)/`"b"`(단변)/`"Disp"=$"{a}x{b}[{Total_CMH}]"` XData 기록 (예: `"600x400[1234.5]"` — `Total_CMH` 와 동일 `"0.##"` 포맷 공유, `totalCmhStr` 로컬 변수에 한 번 계산 후 `Total_CMH` XData 와 `Disp` 양쪽에 재사용). `Total_CMH`/`Tree`/`a`/`b`/`Disp` 모두 **독립 try-catch**. `tr.CheckRegName` 5건 등록 필요. `TTG` 토글 시 Line `Disp` 가 최우선 라벨로 표시(위 `TTG 라벨 우선순위` ⓪).

## 파일/폴더
- 프로젝트 루트: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch\`
- 솔루션: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\Acadv25JArch.sln`
- 프로젝트 문서/노트는 Obsidian에 기록 (본 디렉토리에 .md 두지 말 것)
- **`PipeDiaCalc/`** — LineTree / FcuLineTree / DuctTree / DiaNote 관련 소스 (관경계산·치수기입·덕트분석 파이프라인):
  - `namespace PipeLoad2`: `LineTreeBuilder.cs` (+ `PipeLoadAnalysis` 클래스: `PPL`/`LPM`/`CMH`/`LineExtend2Block` 명령), `LineTreeForm.cs/.Designer.cs/.resx`, `LineTreeFormCommand.cs`, `FcuLineTreeBuilder.cs`, `FcuLineTreeForm.cs/.Designer.cs/.resx`, `FcuLineTreeFormCommand.cs`, `DuctTreeBuilder.cs`, `DuctTreeCommand.cs`, `DuctTreeForm.cs/.Designer.cs/.resx` (2026-05-14 신규)
  - `namespace Acadv25JArch`: `DiaNote.cs` (`DiaNote` 클래스 — `cmd_DiaTree`/`cmd_DiaNoteVer[1]`/`cmd_DiaNoteHor[1]`/`Cmd_SetDiaNoteBase`/`Cmd_SetDiaNoteBaseText`/`DD` 명령 + `SusPipe` 클래스 — `susDia`/`susDia1` 명령)
  - `namespace Acadv25JArch.PipeDiaCalc`: `AinitCommand.cs` (`Ainit` 명령 — NamedObjectsDictionary `AINIT_DEFAULTS` 사전에 DwgDefault static 값을 Xrecord로 저장; `Application` 모호 참조 회피용 `using Application = Autodesk.AutoCAD.ApplicationServices.Application;` 필수)
  - csproj `<Compile Update>` / `<EmbeddedResource Update>` 경로는 `PipeDiaCalc\...` 사용 (DependentUpon 은 basename만)
- **`Overrule/`** — DrawableOverrule 기반 시각화 명령 모음 (2026-04-27 분리):
  - `TreeOverrule.cs` (`namespace PipeLoad2`) — `TTG` 명령. ① Line XData `"Tree"` (Root/Mid/Leaf) 기반 색상/LineWeight 오버레이 + 라벨. ② BlockReference XData `"Disp"` 기반 geo 센터 Red 텍스트(짧은변/2 크기). 두 인스턴스로 분리 등록 (각자 `SetXDataFilter`). `FcuLineTreeBuilder.ApplyDiameters` / `LineTreeBuilder.ApplyDiameters` / `Cmd_Block_SetLPM` 이 저장한 XData 전제.
  - `ArchOverrule.cs` — `aag`/`aag3` (Wire Graphic), `REGISTERXDATAFILTER`/`UNREGISTERXDATAFILTER`, `ADDARCHXDATA`/`ADDARCHXDATABATCH`/`REMOVEARCHXDATA`, `TESTXDATAFILTER`. `XDataFilterDrawOverrule` 패턴은 `TreeOverrule.cs` 의 참고 원본.
- **`Ribbon/`** — `Autodesk.Windows` 기반 커스텀 리본 (2026-04-30 추가):
  - `CollabRibbon.cs` (`namespace Acadv25JArch.Ribbon`) — JArch 탭 (Id `ACADV_COLLAB_JARCH`). 4개 패널(DiaNote/Autodesk Docs/트레이스/비교). DiaNote 패널: Row 1 LargeButton 2개 (`"DiaNote 높이 변경"` / `"SET"`, 둘 다 `Cmd_SetDiaNoteBase` 호출) + Row 2 `"크기: <값>"` 표시. 가시성은 `AINIT_DEFAULTS` 사전 존재 여부에 연동 (`SetRibbonTabVisible`). `DiaNote.BaseLen` 변경 시 `RefreshDiaNoteHeight()` 로 라벨 갱신 — `Ainit` / `LoadDwgDefaults` / `SaveBaseLen` 직후 호출.
- **`PaletteSample.cs`** (`namespace PaletteSample`, 2026-05-01 추가) — `SHOWPAL`/`HIDEPAL` 도구 팔레트 샘플. `LayerManagerPalette`와 동일한 lazy init 패턴(첫 명령 호출 시 `PaletteHost.EnsureCreated`). `MyToolsControl : UserControl` — `FlowLayoutPanel` 가로 + 그룹별 세로 스택. 버튼 `Tag`에 명령 문자열(`"_LINE "` 등) 저장 → Click 시 `doc.SendStringToExecute`. **주의: `[assembly: ExtensionApplication]` / `[assembly: CommandClass]` 속성은 `MyPlugIn.cs`와 충돌하므로 절대 추가 금지.**
