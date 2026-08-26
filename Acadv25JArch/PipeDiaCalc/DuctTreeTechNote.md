# Duct Tree 기술 노트

> 프로젝트: `Acadv25JArch` / 네임스페이스: `PipeLoad2` / 폴더: `PipeDiaCalc/`
> 최종 업데이트: 2026-08-26 (연결 공차 2mm 완화, Leaf Block 탐색 SelectFence 전환)

---

## 1. 개요

덕트 네트워크의 Line + Block(디퓨져) 선택 → Tree 구성 → 디퓨져 Block 의 `"CMH"` XData(풍량) 를 Leaf 부하로 인식 → 각 Line 노드에 **누적 CMH 값을 `"Total_CMH"` XData** + **`DuctSizingCalculator.ModeD` 결과를 `"a"`(장변)/`"b"`(단변)/`"Disp"=$"{a}x{b}"` XData** 로 기록.

[[FcuLineTreeTechNote]] 의 4-Phase 처리 패턴을 그대로 따른다. 차이점:
- 부하 XData: `"LPM"` → `"CMH"`
- 누적 XData: `"TotalLPM"` → `"Total_CMH"`
- Spec 모드 선택: Supply/Return **RadioButton** (`DuctType` 으로 매핑) — H-W 공식 대신 Mode D (Huebscher 등가직경 + 50mm 표준치수 + a/b≤1.5)
- 사이즈 산정 라이브러리: `DuctSizing.Core` (`net8.0` ProjectReference, `DuctSizingCalculator.ModeD`)

---

## 2. 파일 구성

| 파일 | 위치 | 역할 |
|------|------|------|
| `DuctTreeBuilder.cs` | `PipeDiaCalc/` | Duct Tree 분석 핵심 로직, `MapLeafTerminalsToCmhBlocks`, `CalculateLoads`, `ApplyTotalCmh` |
| `DuctTreeCommand.cs` | `PipeDiaCalc/` | `DUCTTREE` 커맨드 진입점 (모드/수두 입력 없음) |
| `DuctTreeForm.cs` | `PipeDiaCalc/` | WinForms Form — TreeView 표시 + Select/Apply 버튼 |
| `DuctTreeForm.Designer.cs` | `PipeDiaCalc/` | VS 디자이너 연동 |
| `DuctTreeForm.resx` | `PipeDiaCalc/` | 리소스 파일 |
| `DuctTreeOutlineCommand.cs` | `PipeDiaCalc/` | **외곽선 일괄 적용 오케스트레이터** — `ClassifyTree`/`ApplyTree` (§8, 2026-07-07 추가) |

공통 유틸 `JXdata`, `ZoomExtensionMethods.ZoomToEntities`, `MyPlugin.LicenseDate` 는 공유.

---

## 3. 등록된 AutoCAD 커맨드

| 커맨드 | 클래스 | 설명 |
|--------|--------|------|
| `DUCTTREE` | `DuctTreeCommand` | 덕트 Tree 분석 — Line + Diffuser Block 선택, Root 지정, CMH 누적 |

---

## 4. 데이터 모델 — `DuctNode`

```csharp
public class DuctNode
{
    public Line?           Line   { get; set; }
    public BlockReference? Block  { get; set; }
    public string          Handle { get; set; }
    public DuctNode?       Parent { get; set; }
    public List<DuctNode>  Children { get; set; }
    public int             Level  { get; set; }
    public DuctNodeType    Type   { get; set; }   // Root / Mid / Leaf / Block
    public double          Load   { get; set; }   // CMH (Block) or 누적 (Line)
    public int             LeafCount { get; set; }
    public string          BlockName { get; set; }
    public string          TreeType  { get; set; }  // BuildTree 시점 스냅샷
}

public enum DuctNodeType { Root, Mid, Leaf, Block }
```

FCU 와 비교해 **`Diameter`/`MaxFlow`/`Velocity` 필드 없음** — Mode D 결과(`a`/`b`)는 `DuctNode` 에 저장하지 않고 `ApplyRecursive` 시점에 즉시 계산 후 XData 로 직렬화한다 (Karpathy 원칙 2: 필드는 실제로 필요해진 시점에 추가).

---

## 5. 처리 흐름 (`DUCTTREE`)

FCU 와 동일한 **Pre → Zoom → Map → Main + ShowModelessDialog** 4-Phase. Editor selection 을 Transaction 밖에서 호출.

### Phase 1: Pre-Transaction (ForRead)
- Root Line 의 layer/handle 결정
- 선택 엔티티 순회:
  - `Line && Layer == rootLayer` → `lineEndpoints` 수집
  - `BlockReference && DuctTreeBuilder.HasCmh(br)` → `cmhBlockHandles` 수집
- **XData `"CMH"` 없는 Block 은 이 단계에서 제외**

### Phase 1.5: Zoom fit
- `ed.ZoomToEntities(psr.Value.GetObjectIds())` — 5% padding fit
- `SelectFence` 가 뷰포트 범위에 의존하므로 Phase 2 직전 필수

### Phase 2: `MapLeafTerminalsToCmhBlocks` (Transaction 밖)
1. Handle 기반 연결 그래프 (`TOLERANCE`=2mm — 2026-08-25 1mm 에서 완화)
2. Root BFS → `childCount[h] == 0 && h != root` = Leaf
3. Leaf 의 **중간점 → tp**(미연결 끝점) 선분으로 `ed.SelectFence(fence, INSERT filter)` (2026-08-26 변경, 구: tp ± margin(=10) 사각 `SelectCrossingWindow`)
4. 결과 중 `cmhBlockHandles.Contains(h)` 인 Block만 `map[blockHandle] = leafHandle`

> **왜 Fence 인가** — 중간점~끝점은 선분이라 `SelectCrossingWindow` 의 두 코너로 쓰면 수평/수직 덕트에서 **면적 0 사각형**이 된다(덕트 도면은 대부분 직교선이므로 이게 기본 케이스). `SelectFence` 는 `Point3dCollection` 을 받아 퇴화가 없다.
> **주의** — Fence 는 선분이 블록 형상을 **실제로 가로질러야** 잡는다. 디퓨져가 Line 끝점 바깥에 떨어져 있으면 구 방식(±10 사각)은 잡던 것을 놓친다. 진단은 로그의 `hit=0`.

### Phase 3: Main Transaction
- Entity 재취득 (CMH 없는 Block 제외)
- `builder.BuildTree(rootLine, lines, blocks, blockToLine)`:
  1. BFS 로 Line 트리 구성
  2. **`SetNodeTypes` 를 Block 부착 *전* 호출** (Leaf 판정 보호)
  3. `node.TreeType = node.Type.ToString()` 전 노드 스냅샷
  4. Leaf 에 Block 부착 (`TreeType="Block"`)
- `CalculateLoads(rootNode)` — Block `Load = JXdata.GetXdata(br, "CMH")` 파싱, Line 은 자식 합계
- **Spec 계산은 Apply 시점으로 지연** — Form 에서 Supply/Return 선택 후 일괄 산정 (모드 변경 시 재선택 가능)
- `Application.ShowModelessDialog(new DuctTreeForm(...))`

### Phase 4: Form "누적 적용" → `ApplyTotalCmh(node, db, DuctType)`
- Transaction 열고 RegName 5개 등록: `"Tree"` / `"Total_CMH"` / `"a"` / `"b"` / `"Disp"`
- 재귀적으로 Line 노드(Block 제외)마다:
  - `"Tree"` XData = `node.TreeType` — **독립 try**
  - `"Total_CMH"` XData = `node.Load.ToString("0.##", InvariantCulture)` — **독립 try**
  - **`Load > 0` 일 때만** Mode D 산정:
    - `var result = DuctSizingCalculator.ModeD(node.Load, ductType, α=ModeD_Alpha, bMin, bMax, aspectMax=ModeD_AspectMax)` (`bMin`/`bMax` 는 Form NumericUpDown 에서 전달)
    - `"a"` XData = `result.Combinations[0].A.ToString(InvariantCulture)` (장변, mm)
    - `"b"` XData = `result.Combinations[0].B.ToString(InvariantCulture)` (단변, mm)
    - `"Disp"` XData = `$"{a}x{b}[{totalCmhStr}]"` (예: `"600x400[1234.5]"` — `totalCmhStr` 은 `Total_CMH` XData 와 동일 `"0.##"` 포맷)
  - **각 XData 는 독립 try-catch** — 한쪽 실패가 다른 쪽 막지 않음
- Block 노드는 스킵 (이미 `"CMH"` XData 보유)

---

## 6. 핵심 API

### `DuctTreeBuilder.HasCmh(BlockReference) → bool`
XData RegName `"CMH"` 존재 + non-empty 값 판정. 예외는 `false`.

### `DuctTreeBuilder.MapLeafTerminalsToCmhBlocks(...)`
```csharp
public static Dictionary<string, string> MapLeafTerminalsToCmhBlocks(
    Editor ed,
    string rootHandle,
    List<(string handle, Point3d s, Point3d e)> lineEndpoints,
    HashSet<string> cmhBlockHandles)   // margin 파라미터 제거 (2026-08-26)
```
반환: `Dictionary<blockHandle, leafLineHandle>` — `BuildTree` 에 주입.

### `DuctTreeBuilder.ApplyTotalCmh(DuctNode root, Database db, DuctType ductType, int bMin, int bMax)`
별도 Transaction 으로 모든 Line 노드에 `"Tree"` / `"Total_CMH"` 기록 + `Load > 0` 시 Mode D 사이즈(`"a"`/`"b"`/`"Disp"`) 기록. `ductType` / `bMin` / `bMax` 는 Form 의 `pnlMode` 컨트롤에서 결정.

Mode D 입력은 4개 중 2개만 코드 상수, 2개는 Form 입력:

| 항목 | 출처 | 기본값 | 비고 |
|------|------|--------|------|
| `bMin` | `numBMin` (Form) | 200 | `NumericUpDown` Min=100/Max=2000/Increment=50 |
| `bMax` | `numBMax` (Form) | 500 | 동일 NumericUpDown 제약 |
| `ModeD_Alpha` | private const | 1.0 | 마찰계수 보정 (운영 중 변경 없음) |
| `ModeD_AspectMax` | private const | 1.5 | a/b 최대 |

기본값은 `DuctSizing1` 의 Mode D 탭과 동일. α/aspectMax 는 자주 바뀌지 않으므로 Form 입력 미노출 (필요해지면 `pnlMode` 추가 노출 또는 별도 토글). `bMin > bMax` 검증은 Form 의 `btnApply_Click` 에서 수행 (입력 오류 dialog 후 return).

---

## 7. Form — `DuctTreeForm`

[[FcuLineTreeTechNote]] 와 동일 shell, **FCU 의 `cmbHead`/`cmbMode` 드롭다운 대신 Supply/Return RadioButton + b 최소/최대 NumericUpDown 두 개 노출**.

| 컨트롤 | 역할 |
|--------|------|
| `lblStats` (Top) | `[Duct] {layer} \| Line N (Root/Mid/Leaf) \| Diffuser=K \| Root 총 풍량: X.X CMH` |
| `pnlMode` (Top, height 45 — 2026-05-20 사용자 수동 조정) | `rbSupply`/`rbReturn` + `lblBMin`/`numBMin`(200) + `lblBMax`/`numBMax`(500) |
| `treeView` (Fill, Y=73) | 노드 표시 — Root ● Red / Mid ◆ Blue / Leaf ■ Green / Block ▣ Purple |
| `btnExpand` ("전체") / `btnCollapse` ("접기") | TreeView 전체 펼치기/접기 |
| `btnApply` (Blue) | `_builder.ApplyTotalCmh(_rootNode, _db, SelectedDuctType, bMin, bMax)` — `doc.LockDocument()` 로 감쌈 |
| `btnSelect` (Green) | TreeNode.Tag(=Handle) → `db.TryGetObjectId` → `ed.SetImpliedSelection` → `doc.Window.Focus()` |
| `btnDuctOutline` | `DuctTreeOutlineCommand.ApplyTree` 호출 — 트리 전체 외곽선 일괄 생성 (§8). **항상 활성** — Apply 전에 눌러도 `"a"` 없는 노드는 자동 스킵 |
| `btnClose` (Top-Right Anchor) | Form 닫기 |

`SelectedDuctType` 프로퍼티 — `rbReturn.Checked ? DuctType.Return : DuctType.Supply`. 생성자에서 둘 다 unchecked 또는 NumericUpDown `Value < Minimum` 인 경우(Designer 재생성으로 누락 가능) 안전 초기화 (`rbSupply.Checked = true`, `numBMin/numBMax.Value = 200/500`). [[feedback-winforms-designer-regen]] 패턴.

`btnApply_Click` 은 `bMin > bMax` 검증 후 dialog return, 통과 시 `(int)numBMin.Value` / `(int)numBMax.Value` 를 `ApplyTotalCmh` 에 전달.

**FCU 대비 제외**: `cmbHead`/`btnRecalc` (α/aspectMax 는 코드 상수로 고정 — 운영 중 자주 변경되지 않음).

`btnApply` 는 **`doc.LockDocument()` 필수** — WinForms 에서 AutoCAD DB 쓰기 (eLockViolation 방지).

**레이아웃 보존 주의**: `DuctTreeForm.Designer.cs` 의 컨트롤 Location/Size 는 사용자가 VS WinForms Designer 로 직접 다듬은 값. 재생성 금지, Edit 로 최소 변경. [[feedback-ducttreeform-layout]] 메모리 참조.

---

## 8. DuctOutLine 일괄 적용 — `DuctTreeOutlineCommand`

2026-07-07 추가. `DuctTreeForm` 의 **`DuctOutLine` 버튼**(`btnDuctOutline_Click`)이 분석된 `DuctNode` 트리를 순회하며 각 접합부의 위상을 판정해 [[DuctOutLine_Case_1|Duct_C1]] / [[Duct_C2]] / [[Duct_Elbow|Duct_E]] / [[Duct_End_Elbow|Duct_EE]] 를 **대화형 선택 없이 일괄 실행**한다. 기존 4개 명령이 배치 오케스트레이터의 실행 엔진이 되는 구조.

설계 사양: [[DuctTreeOutLine]] (v0.3) — 원본은 `건축\Duct-OutLine\DuctTreeOutLine.md` (사용자 유지). 각 패턴의 기하 공식은 재정의하지 않고 개별 사양서를 참조만 한다.

**`[CommandMethod]` 없음** — AutoCAD 명령으로 등록되지 않고 Form 버튼으로만 호출된다.

### 8.1 위상 판정 (`ClassifyNode`)

`lineChildren = node.Children.Where(c => c.Type != Block)` — 디퓨져 Block 자식은 판정에서 제외(부하일 뿐 Line 분기가 아님). 접합점 **X** 는 `nodeLine` 의 두 끝점 중 `lineChildren` 전부의 근단이 모이는 쪽(`TryFindJunction`), 노드 방향 `nodeDir = (nodeOther − X).GetNormal()`.

| 자식 Line | 위상 | 결과 |
|---|---|---|
| 1 | 직선 연장 + 폭 동일 | `None` — 외곽선 불필요 |
| 1 | 직선 연장 + 폭 상이/폭 없음 | `Unsupported` — 분기 없는 순수 리듀서, 수동 처리 |
| 1 | 직각 + 손자 Line 없음 | **`Duct_EE`** (말단 상향 분기, 폭 무관) |
| 1 | 직각 + 손자 Line 있음 | **`Duct_E`** (직각 엘보, 폭 불일치 시 `[E04]` 스킵) |
| 2 | 직선 1 + 직각 1 | **`Duct_C2`** — `BranchB`=직선(bb, 축소), `BranchA`=직각(cc, 분기) |
| 2 | 직각 2개 **반대측** + **양쪽 다 Mid** | **`Duct_C1`** — bb/cc 는 기하 대칭이라 **Handle 오름차순**으로 결정적 배정 |
| 2 | 직각 2개 반대측이나 한쪽이 Leaf | `Unsupported` — `Duct_C1` 은 양쪽 모두 Mid Duct 여야 함 |
| 2 | 직각 2개 **같은측** | `Unsupported` — 4패턴에 없음 |
| 3+ | 다중 분기 | `Unsupported` |

판정 상수: `PerpTol = 0.02`(직각·반대방향 공용, 약 ±1.15° — 기존 4개 명령과 동일), `WidthEqualTol = 1e-3`, `JunctionTol = 1.0`mm(**주의**: `DuctTreeBuilder.TOLERANCE` 는 2026-08-25 에 2.0mm 로 올라가 더 이상 같은 값이 아니다).

### 8.2 실행 전략 (`ApplyTree`)

- **읽기 전용 스냅샷 선행** — `ClassifyTree` 로 트리 전체 판정을 먼저 확정한 뒤 적용 루프 진입. 적용 중 발생하는 Line 분할/이동은 각 노드의 **근단**만 건드리므로, 아직 처리하지 않은 노드의 **원단** 기반 판정에는 영향을 주지 않는다.
- **부분 실패 허용** — 한 노드의 `TryApply` 가 `false`(=`[Exx]` 검증 실패)를 반환해도 나머지 노드 처리를 계속한다.
- **단일 Transaction 공유, Commit 은 호출자 책임** — Form 버튼이 `doc.LockDocument()` + `StartTransaction()` 으로 감싸고 `tr.Commit()` 한다.
- **적용 순서는 `ClassifyTree` 순회 순서 그대로** (2026-07-08 확정 — `Duct_E` 를 뒤로 미루지 않음).

### 8.3 stale `DuctNode.Line` 방어 ⚠️

`DuctNode.Line` 은 `DUCTTREE` 분석 당시 **이미 커밋·종료된 옛 Transaction 에서 열린 참조**다. 그대로 재사용하면 `eInvalidOpenState` 가 난다(2026-07-07 실사용 중 확인). 따라서 `GetLine(tr, db, node, mode)` 이 `node.Handle` → `db.GetObjectId(false, handle, 0)` → `tr.GetObject` 로 **반드시 재획득**한다 — `DuctTreeBuilder.ApplyRecursive` 와 동일한 패턴. 재획득한 `Line` 은 `JunctionPlan.NodeLine`/`BranchALine`/`BranchBLine` 에 담아 같은 Transaction 안에서 `TryApply` 로 그대로 넘긴다.

> 부호 오류 이력(2026-07-07): `nodeDir` 를 반대로 계산하면 직각 판정은 내적이 0이라 부호와 무관하게 통과하지만, **직선 연장(반대방향) 판정만 `dot=−1` 이어야 할 것이 `+1` 로 뒤집혀** `Duct_C2` 후보가 전부 `Unsupported` 로 빠졌다.

### 8.4 전제 조건

- **Apply 선행 필요** — `ApplyTotalCmh` 가 `Load > 0` 인 Line 에만 `"a"` 를 기록하므로, Apply 전이거나 `Load == 0` 인 노드는 `TryReadWidth` 실패로 자동 스킵된다. 버튼 자체는 항상 활성 상태로 둔다(v0.3 §12 확정).
- **`"a"` RegApp 공유** — `SetDuctWidth`(도면 Text 그대로) 와 `ApplyTotalCmh`(Mode D 장변) 가 같은 `"a"` 를 쓴다. `TryReadWidth` 는 숫자면 그대로, `"600x400"` 형태면 앞 숫자를 폭으로 채택하므로 양쪽 모두 처리된다. **Apply 이후에는 Mode D 산정값이 곧 외곽선 폭**이 된다.
- **Root 상류는 대상 아님** — 부모가 없는 쪽 끝은 트리 범위 밖(AHU/공급원). Root 의 하류 접합은 Mid 와 동일 규칙.

### 8.5 결과 리포트

`ApplyTree` 는 `List<JunctionResult>`(= `Plan` + `Applied` + `Message`)를 반환하고, Form 이 두 가지로 보고한다.

- **커맨드라인** — 노드별 1줄: `[DuctOutLine] 적용|스킵 [<handle>] <Pattern>: <Message>`
- **요약 dialog** — `적용 N건(패턴별 집계), 스킵 M건(패턴별 집계), 형상 불필요 K건(직선 연속)`
  (`형상 불필요` = `Pattern == None`, `스킵` = 그 외 `Applied == false`)

---

## 9. 진단 로그

```
[Pre] rootLayer=<L>, lines=<N>, CMH blocks=<M>
[MapLeaf] 진입: tree nodes=<V>, leaves=<K>, cmhBlocks=<M>
  [MapLeaf] Leaf#1 h=<handle> mid=(x,y) tp=(x,y)
    [MapLeaf] status=OK hit=<k>
  ...
[MapLeaf] 종료: SelectFence 호출=<K>회, 매핑=<map.Count>건
레이어 [<L>] 필터: Line <N>개, CMH Block <B>개, 매핑 <map.Count>건
[DuctTree] Line: <N>개, Diffuser: <D>개, Root 총 풍량: <X.X> CMH
```

---

## 10. 사용 워크플로우

1. **CMH 명령** (`Cmd_Block_SetCMH`) — 디퓨져 Block 의 BBox 안 Text/MText 에서 풍량값 추출 → `"CMH"` + `"Disp"` XData 동시 기록. [[CMH]] / [[LPM]] 와 동일 패턴.
2. **DUCTTREE 명령** — Line + 디퓨져 Block 선택 → Root Line 지정 → 분석 → Form 표시
3. **Form Supply/Return 선택 + "누적 적용"** → 각 Line 에 `"Tree"` / `"Total_CMH"` + (Load>0 시) `"a"` / `"b"` / `"Disp"` XData 기록
4. **Form `DuctOutLine` 버튼** (선택) — 트리 전체 접합부를 위상 판정해 `Duct_C1`/`Duct_C2`/`Duct_E`/`Duct_EE` 외곽선 일괄 생성 (§8). Apply 로 `"a"` 가 기록된 뒤에 눌러야 의미가 있다.
5. **TTG 명령** (선택) — `"Tree"` XData 기반 Line 색상/LineWeight 오버레이 + Line `"Disp"` 를 중앙 라벨로 표시 + 디퓨져 Block 의 `"Disp"` 값을 geo 센터에 Red 표시

---

## 11. TTG 연동

- `"Tree"` XData 가 기록되므로 — [[TreeOverrule]] 의 Line 인스턴스가 Root/Mid/Leaf 색상·LineWeight 오버레이를 자동 적용 (RegName 무관, `"Tree"` 값만 봄)
- **Line `"Disp"` 가 TTG 라벨 최우선** (2026-05-19 추가) — `DrawLineTree` 라벨 분기 최상위에 `JXdata.GetXdata(line, "Disp")` 검사 추가. Duct Line 은 이 값(`"{a}x{b}"`)이 그대로 중앙 라벨로 표시됨. 파이프(LineTree/FCU)는 Line 에 `"Disp"` 를 기록하지 않으므로 충돌 없음. 자세히는 [[TreeOverrule]] §5.1
- 디퓨져 Block 의 Red 라벨은 `CMH` 명령이 함께 기록한 `"Disp"` XData 가 처리 — [[TreeOverrule]] 의 Block 인스턴스 (`SetXDataFilter("Disp")`)
- `"Total_CMH"` 값을 라벨에 노출하고 싶으면 별도 분기 추가 필요 (현재는 Line `Disp` = `"{a}x{b}"` 만 표시)

---

## 12. 주요 설계 결정

- **별도 빌더 (FCU 와 분리)**: `FcuLineTreeBuilder` 를 base class 로 추상화하지 않음. Duct 사이즈 산정은 H-W 가 아니라 Huebscher 등가직경 + 50mm 표준치수 + 종횡비 제약(`DuctSizing.Core`)이라 알고리즘이 발산. 지금 추상화는 over-engineering.
- **`DuctSizing.Core` ProjectReference**: `DuctSizingCalculator.ModeD` 를 재사용하기 위해 `Acadv25JArch.csproj` 에 `..\DuctSizing.Core\DuctSizing.Core.csproj` 추가 (2026-05-19). net8.0 라이브러리는 net8.0-windows8.0 과 호환. `CopyLocalLockFileAssemblies=true` 라 `DuctSizing.Core.dll` 은 `C:\Jarch25\` 에 자동 배포.
- **Mode D 입력 분리** (2026-05-20 갱신): 4개 중 **bMin/bMax 는 Form NumericUpDown 으로 노출**(기본 200/500), **α/aspectMax 는 코드 const**(`ModeD_Alpha=1.0`/`ModeD_AspectMax=1.5`, `DuctSizing1` 기본값). 사이즈 후보 범위는 도면별로 자주 다르고(소형 환기 vs 대형 공조), 마찰계수·종횡비는 표준 운영값이라 변경 빈도가 다름. 변경 빈도가 비대칭이라 Form 노출도 비대칭으로.
- **Mode/Head 대신 Supply/Return 라디오**: 사이즈 산정은 단지 `DuctType` 만 필요 (R=0.08 vs 0.10) → 드롭다운 대신 라디오 2개로 충분. FCU 의 H-W 입력(`cmbHead`/`cmbMode`) 처럼 다단 입력 불필요.
- **자동 Apply 제거**: 명령에서 즉시 XData 기록하던 초기 구현 → Form 의 Apply 버튼으로 이동 (FCU 패턴 일치, 사용자가 Supply/Return + bMin/bMax 검토 후 commit).
- **Total_CMH 포맷**: `"0.##"` + `InvariantCulture` — 정수는 `"800"`, 소수만 필요 자리수 표시(trailing zero 제거). 로케일 무관 round-trip 보장. *(2026-05-18: `F2` 에서 변경 — `800.00` → `800`)*
- **a/b 포맷**: `ToString(InvariantCulture)` — `RectCombination.A/B` 가 `int` 라 단순. `"600"` 형식.
- **Disp 포맷** (2026-05-20 갱신): `$"{a}x{b}[{totalCmhStr}]"` — `"600x400[1234.5]"` 형식. 사이즈 + 부하를 한 라벨로 합성. 파이프 TTG 의 `<Dia>[<Total15A>]` 와 동일한 `[...]` 스타일. `totalCmhStr` 은 `Total_CMH` XData 와 동일 `"0.##"` 포맷 (try 블록 밖에서 한 번 계산 후 양쪽 공유 — 일관성).
- **Load == 0 처리**: `"a"`/`"b"`/`"Disp"` 기록 생략 (Mode D 호출 조건 부착). `Total_CMH` 만 `"0"` 기록 — 부하 없는 Line 노드 도 Tree 색상 오버레이는 유지.
- **각 XData 독립 try-catch**: `Tree`/`Total_CMH`/`a`/`b`/`Disp` 모두 분리 — 한쪽 실패가 다른 쪽 막지 않음 (FCU 패턴 일관).

---

## 13. 관련 문서

- [[FcuLineTreeTechNote]] — FCU 배관 (`FCUTREE`, H-W 공식, Supply/Drain 모드)
- [[LineTreeTechNote]] — 급수배관 일반 (`PipeTreeCon`, `PipeTree`, Supply/Return)
- [[CMH]] — `CMH` 커맨드 (Block Text → XData `"CMH"`+`"Disp"` 자동 추출)
- [[LPM]] — `LPM` 커맨드 (FCU 부하 추출, CMH 와 같은 패턴)
- [[TreeOverrule]] — `TTG` 커맨드 (Line `"Tree"` 색상/Line `"Disp"` 라벨/Block `"Disp"` 텍스트)
- [[DuctTreeOutLine]] — **`DuctOutLine` 버튼 설계 사양** (v0.3, 위상 판정 → 4패턴 매핑) — §8 의 근거 문서
- [[DuctOutLine_Case_1]] / [[Duct_C2]] / [[Duct_Elbow]] / [[Duct_End_Elbow]] — 각 패턴의 기하 공식 (single source of truth)
- `DuctSizing.Core/DuctSizingCalculator.cs` — `ModeD(q, type, α, bMin, bMax, aspectMax)` (DuctSizing 솔루션, 별도 프로젝트)
- `DuctSizing1/CLAUDE.md` — Mode D 탭 기본값 출처 (200/500/α 재사용/aspect 1.5 고정)

---

## 14. 변경 이력

- **2026-08-21** `DuctTreeOutlineCommand` / `DuctOutLine` 버튼 문서화 (§8 신규, 이하 섹션 번호 +1). 코드는 2026-07-07-07-16 작성분이나 노트에 누락돼 있었음.
- **2026-07-07~07-16** `DuctTreeOutlineCommand.cs` 추가 — `ClassifyTree`/`ApplyTree` 위상 판정 + `Duct_C1`/`C2`/`E`/`EE` 일괄 적용, `DuctTreeForm` 에 `btnDuctOutline` 추가. stale `DuctNode.Line` → Handle 재획득 방어(`eInvalidOpenState`), `nodeDir` 부호 오류 수정.
- **2026-05-20** `DuctTreeForm pnlMode` 에 b 최소/최대 `NumericUpDown` 추가 (기본 200/500, Min/Max=100/2000, Increment 50), `ApplyTotalCmh` 시그니처에 `bMin`/`bMax` 추가, `ModeD_BMin`/`ModeD_BMax` const 제거 (α/aspectMax 만 const 유지). Form Designer 좌표는 사용자가 VS Designer 로 수동 조정(pnlMode height 32→45). `Disp` 포맷 `{a}x{b}` → `{a}x{b}[{Total_CMH}]` 변경.
- **2026-05-19** Mode D 사이즈 산정 추가 — `DuctSizing.Core` ProjectReference, `DuctTreeForm` Supply/Return 라디오, `ApplyTotalCmh(node, db, DuctType)` 시그니처, `Load > 0` 시 `"a"`/`"b"`/`"Disp"` XData 기록, `TreeOverrule` Line `Disp` 최우선 라벨
- **2026-05-18** `Total_CMH` 포맷 `F2` → `"0.##"` (trailing zero 제거)
- **2026-05-14** 초기 구현 — `DUCTTREE` / `CMH` / `CMHT` 명령, `Total_CMH` 누적, FCU shell 미러 Form

---

## 15. 샘플 / 테스트 도면

`#Sample` 태그로 색인 (frontmatter `path:` 필드에 dwg 풀패스).

- [[덕트설계방법_test_2]] — `DUCTTREE` 동작 검증용 샘플 (서울기연 2026-05-15)
