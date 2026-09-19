---
tags:
  - AutoCAD
  - Overrule
---

# TreeOverrule — Tree 상태 + Block Disp + Poly CFM 시각화

> 프로젝트: `Acadv25JArch` / 네임스페이스: `PipeLoad2` / 파일: `Overrule/TreeOverrule.cs`
> 최종 업데이트: 2026-09-19 (Polyline `"CFM"` 오버룰 추가 — 룸 폴리 풍량 표시용)

---

## 1. 개요

`TTG` 토글 한 번에 세 가지 시각화를 켠다:

1. **Line 오버레이** — XData `"Tree"` (Root/Mid/Leaf) 에 따라 색상/선가중치 + 중앙 라벨
2. **BlockReference 오버레이** — XData `"Disp"` 가 있으면 블록 geometric center 에 큰 Red 텍스트
3. **Polyline 오버레이** — XData `"CFM"` 이 있으면 폴리 geometric center 에 Cyan `"{값} CFM"` 텍스트 (2026-09-19 추가)

DB는 수정하지 않고 화면 렌더링만 변경. 단일 클래스(`TreeDrawOverrule`)에 **세 인스턴스**(`_lineInstance` / `_blockInstance` / `_polyInstance`)를 등록하고 생성자 인자 `Mode` enum(`Line` / `Block` / `Poly`)으로 분기한다. 각 인스턴스가 **각자의 `SetXDataFilter`** 를 가져 CLAUDE.md 의 *XData 필터 2중 검사* 컨벤션을 보존.

전제 데이터:
- Line `"Tree"` / `"Dia"` / `"15A"` / `"Total15A"` / `"TotalLPM"` — `FcuLineTreeBuilder.ApplyDiameters` / `LineTreeBuilder.ApplyDiameters` 기록
- **Line `"Disp"`** — `DuctTreeBuilder.ApplyTotalCmh` 가 Mode D 결과 + 누적 부하를 `"{a}x{b}[{Total_CMH}]"` 로 기록 (DuctTree 한정, 2026-05-19 도입 → 2026-05-20 포맷에 `[Total_CMH]` 추가)
- Block `"Disp"` — `Cmd_Block_SetLPM` / `Cmd_Block_SetCMH` 이 LPM/CMH 저장과 동시에 같은 문자열로 기록
- **Polyline `"CFM"`** — `To_RoomCFM` (`RoomCalc.cs`) 이 닫힌 LWPOLYLINE 에 입력 풍량을 문자열로 기록

---

## 2. 등록된 AutoCAD 커맨드

| 커맨드 | 클래스 | 동작 |
|--------|--------|------|
| `TTG` | `TreeOverruleCommand` | `TreeDrawOverrule` Line/Block/Poly 세 인스턴스 등록·해제 토글 + Regen |

---

## 3. 시각화 매핑

### 3.1 Line — XData `"Tree"`

| Tree 값 | 색 | ACI | 선 가중치 |
|---------|----|----|----------|
| `"Root"` | Red | 1 | `LineWeight050` (0.5mm) |
| `"Mid"` | Green | 3 | `LineWeight050` |
| `"Leaf"` | Yellow | 2 | `LineWeight050` |

`LWDISPLAY` 시스템변수가 **ON** 이어야 화면에 가중치가 실제로 표시된다.

### 3.2 Block — XData `"Disp"`

| 항목 | 값 |
|------|----|
| 대상 | `BlockReference` (XData `"Disp"` 가진 인스턴스) |
| Color | ACI 1 (Red) |
| TextHeight | `min(width, height) × 0.5` (BBox 짧은변의 절반) |
| 위치 | `(MinPoint + MaxPoint) / 2` — geometric center |
| Rotation | 0 (수평 고정) |
| Attachment | `MiddleCenter` |
| 본체 렌더 | `base.WorldDraw(drawable, wd)` 로 블록 그대로 그린 뒤 텍스트 추가 |

`width × height` 가 `1e-6` 미만이면 텍스트 생략 (degenerate BBox 보호).

### 3.3 Polyline — XData `"CFM"` (2026-09-19)

| 항목 | 값 |
|------|----|
| 대상 | `Polyline` (LWPOLYLINE, XData `"CFM"` 가진 인스턴스) |
| Color | ACI 4 (Cyan) — `COLOR_CFM` |
| 내용 | `$"{cfm} CFM"` (예: `"1200 CFM"`) |
| TextHeight | `min(width, height) × 0.1` — 룸 폴리는 크므로 Block(×0.5)보다 작게 |
| 위치 | `(MinPoint + MaxPoint) / 2` — extents 박스 중심 (L자형 룸은 폴리 밖에 찍힐 수 있음) |
| Rotation | 0 / Attachment `MiddleCenter` |
| 본체 렌더 | `base.WorldDraw` 후 텍스트 추가 (Block 분기와 동일 패턴) |

`Polyline` 은 `Autodesk.AutoCAD.GraphicsInterface.Polyline` 과 이름이 겹치므로 `using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;` 별칭 필수 (기존 `Line` 별칭과 같은 방식).

---

## 4. 동작 흐름

### Register
1. `RegisterRegApp("Tree")` + `RegisterRegApp("Disp")` + `RegisterRegApp("CFM")` — RegAppTable 등록
2. **Line 인스턴스**: `new TreeDrawOverrule(Mode.Line)` → `AddOverrule(Line, ..., false)` → `SetXDataFilter("Tree")`
3. **Block 인스턴스**: `new TreeDrawOverrule(Mode.Block)` → `AddOverrule(BlockReference, ..., false)` → `SetXDataFilter("Disp")`
4. **Poly 인스턴스**: `new TreeDrawOverrule(Mode.Poly)` → `AddOverrule(Polyline, ..., false)` → `SetXDataFilter("CFM")`
5. `Overrule.Overruling = true`
6. `Editor.Regen()` — 즉시 반영

### IsApplicable (모드별 엄격 필터)
```csharp
string appName = _mode switch
{
    Mode.Block => "Disp",
    Mode.Poly  => "CFM",
    _          => "Tree"
};
using ResultBuffer? rb = entity.GetXDataForApplication(appName);
return rb != null;
```
`SetXDataFilter` 가 놓치는 엔티티가 있어도 `GetXDataForApplication` 으로 **명시적 2차 필터**. 해당 XData 없는 엔티티는 `WorldDraw` 진입조차 못함 → trait 누수 방지.

### WorldDraw — 모드 분기
- `Mode.Line` → `DrawLineTree(drawable, wd)`
- `Mode.Block` → `DrawBlockDisp(drawable, wd)`
- `Mode.Poly` → `DrawPolyCfm(drawable, wd)`

### `DrawLineTree`
1. 원본 `SubEntityTraits.Color` / `LineWeight` 저장
2. `JXdata.GetXdata(line, "Tree")` 로 값 조회 → switch 로 색 결정
3. 값이 Root/Mid/Leaf 외 → `base.WorldDraw` (trait 변경 없음)
4. 일치 시 `Color` + `LineWeight050` 설정 후 `wd.Geometry.WorldLine` 직접 그림
5. **trait 원복** — 다음 Entity 렌더에 누수 방지
6. `"15A"` / `"Total15A"` / `"Dia"` / `"TotalLPM"` XData 조회 → 라벨 합성 후 `DrawCenterLabel`

### `DrawBlockDisp`
1. `base.WorldDraw(drawable, wd)` — 블록 본체를 정상 렌더 (반환값 보존)
2. `JXdata.GetXdata(br, "Disp")` 조회, 비어있으면 종료
3. `br.GeometricExtents` → `width`/`height`/`shortSide`/`center` 계산
4. `MText` 생성 (TextHeight = `shortSide × 0.5`, Color = ACI 1, MiddleCenter, rotation 0)
5. `mtext.WorldDraw(wd)` — DB 추가 없이 렌더만
6. `using` 으로 즉시 dispose

`SubEntityTraits` 는 변경하지 않으므로 원복 불필요. MText 가 자체 Color 사용.

### `DrawPolyCfm`
`DrawBlockDisp` 와 동일 구조 — `JXdata.GetXdata(pl, "CFM")` 조회 → `pl.GeometricExtents` 중심 → `MText` (`"{cfm} CFM"`, TextHeight = `shortSide × 0.1`, ACI 4) → `mtext.WorldDraw(wd)`.

### Unregister
- 세 인스턴스 모두 `RemoveOverrule` + `SetXDataFilter(null)` + 필드 null 화
- `Editor.Regen()`

---

## 5. Line 중앙 라벨 (`DrawCenterLabel`)

### 5.1 라벨 우선순위 (위에서부터 첫 매치, 2026-05-19 갱신)

| 조건 | 형식 | 매칭 노드 |
|---|---|---|
| **Line `"Disp"` 존재** | `<Disp>` 그대로 | **DuctTree Line (`"{a}x{b}[{Total_CMH}]"`, 예: `"600x400[1234.5]"`)** |
| `"15A"` 존재 | `<Dia>[<Total15A>]-<15A>` | LineTree Leaf (분석 전후 공통) |
| `"15A"` 없고 `"Total15A"` 존재 | `<Dia>[<Total15A>]` | LineTree Mid/Root (Supply 모드) |
| 그 외 | (라벨 없음) | FCU Mid/Root, 분석 전 일반 라인 등 |

`Dia` / `Total15A` 는 `JXdata.GetXdata(...) ?? "00"` 으로 폴백 — XData 가 아직 기록되지 않은 Leaf(`PPL` 만 돈 상태) 는 `00[00]-<15A>` 로 표시되어 "분석 전" 시각적 신호 역할. `Total15A` 는 `LineTreeBuilder.ApplyDiameters` 가 Supply 모드에서만 기록(균등값×동시사용율 합).

**Line `Disp` 분기 (2026-05-19 추가)** — `DuctTreeBuilder.ApplyTotalCmh` 가 Mode D 사이즈를 `"{a}x{b}"` 문자열로 Line `Disp` 에 저장하면서 도입. `JXdata.GetXdata(line, "Disp")` 가 non-empty 면 다른 파이프 XData 분기를 건너뛰고 그 값을 그대로 중앙 라벨로 표시. LineTree/FCU 는 Line 에 `Disp` 를 기록하지 않으므로 충돌 없음. RegName 자체는 Block 분기와 공유(`SetXDataFilter("Disp")` 는 Block 인스턴스만, Line 인스턴스는 `"Tree"` 필터지만 Duct Line 은 `"Tree"` + `"Disp"` 동시 보유라 통과).

**이전 동작 (참고)** — Leaf prefix 가 `"15"` 고정이었고 `TotalLPM` 기반 FCU 폴백(`<Dia>[<TotalLPM>]`)이 있었음. 현재 코드에서는 두 분기 모두 주석 처리되어 비활성. FCU Tree 의 Mid/Root 라벨이 사라진 것은 의도된 변경이며, FCU 표시가 다시 필요해지면 주석 해제하거나 별도 분기 추가 검토.

### 5.2 공통 렌더 속성

| 항목 | 값 |
|------|----|
| TextHeight | `LABEL_TEXT_HEIGHT = 30.0` |
| Color | `LABEL_COLOR_ACI = 7` (White) — Tree 색상과 무관하게 고정 |
| Rotation | Line 방향. 거꾸로 안 읽히게 `±π/2` 범위로 정규화 |
| Position | Line 중점에서 수직 방향으로 `TextHeight × 0.6` 만큼 오프셋 (선과 겹침 방지) |
| Attachment | `MiddleCenter` |
| Render | `MText` 인스턴스를 `mtext.WorldDraw(wd)` 로 렌더 (DB에 추가 안 됨) |

`Color` 모호 참조 회피용 별칭 `using AcadColor = Autodesk.AutoCAD.Colors.Color;` 필요.

---

## 6. 주요 설계 결정

- **인스턴스 분리** — Line/Block/Poly 가 서로 다른 RegApp 으로 필터링되어야 하므로 단일 인스턴스 + `SetXDataFilter` 가 불가능. 클래스를 쪼개지 않고 동일 클래스의 세 인스턴스 + 생성자 `Mode` enum 으로 분기 → 헬퍼 공유, RegisterRegApp 한 번에 처리. (2026-09-19 `bool _isBlockMode` → `enum Mode` 로 교체 — 세 번째 모드가 생기면서 bool 로는 표현 불가)
- **`SetXDataFilter` 보존** — 단일 인스턴스에서 필터를 빼는 대신 각 인스턴스에 다른 필터를 걸어 *2중 검사* 컨벤션 유지. CLAUDE.md `feedback_overrule_xdata_filter` 메모리 준수.
- **IsApplicable 2차 필터**: `SetXDataFilter`만 믿지 않고 `GetXDataForApplication` 직접 확인. 이전 테스트에서 모든 Line에 LineWeight가 적용되는 누수 목격 → 엄격 필터로 해결.
- **trait 원복**: `wd.SubEntityTraits` 의 Color/LineWeight 변경이 후속 Entity 렌더에 남아 다른 Line까지 굵어지는 현상 방지. Block/Poly 분기는 SubEntityTraits 미변경 → 원복 불필요.
- **Block/Poly 본체는 base.WorldDraw**: Line 분기는 `wd.Geometry.WorldLine` 직접 그림(Entity Color/Weight 무시 목적)이지만, Block 은 내부 정의 렌더가 복잡하고 Poly 는 원형 그대로 보여야 하므로 `base.WorldDraw` 후 텍스트만 덧칠.
- **Block 텍스트 크기 = 짧은변/2**: 폭이 큰 박스 / 키가 큰 박스 모두 박스 안에 안정적으로 들어가는 보수적 휴리스틱. 사용자 가시성 우선.
- **Poly 텍스트 크기 = 짧은변×0.1**: 룸 폴리는 수 m 단위라 ×0.5 면 텍스트가 룸을 덮음. 룸 안에 부담 없이 읽히는 크기로 축소.
- **라벨 색상 흰색 고정 (Line)**: Tree 색(빨/녹/노)으로 라벨도 칠하면 노란 배경에 노란 Leaf 라벨 등 가독성 저하. ACI 7(White)은 흰 배경에서 자동 검정으로 표시되어 양쪽 모두 안전.

---

## 7. 의존성

- `FcuLineTreeBuilder.ApplyDiameters` — Line XData `"Tree"` / `"Dia"` / `"TotalLPM"` 저장 (FCU)
- `LineTreeBuilder.ApplyDiameters` — Line XData `"Tree"` / `"Dia"` / `"Total15A"`(Supply) 저장 + Leaf `"15A"`(`PPL`) (LineTree)
- `DuctTreeBuilder.ApplyTotalCmh` — Line XData `"Tree"` / `"Total_CMH"` + Mode D 사이즈(`"a"` / `"b"` / `"Disp"`) 저장 (DuctTree, [[DuctTreeTechNote]] 참고)
- `Cmd_Block_SetLPM` / `Cmd_Block_SetCMH` — Block XData `"LPM"`/`"CMH"` + `"Disp"` 동시 저장 (`LineTreeBuilder.cs`, [[LPM]] / [[CMH]] 참고)
- `To_RoomCFM` (`RoomCalc.cs`, `Cmd_Poly_Set_CFM`) — 닫힌 LWPOLYLINE 에 `"CFM"` XData 저장 (`PromptDoubleOptions` 입력값 `.ToString()`, `To_CeilingHeight` 패턴)
- `JXdata.GetXdata(Entity, string)` — XData 읽기 (`CadFunction.cs`)
- `Autodesk.AutoCAD.GraphicsInterface.DrawableOverrule` — 기반 클래스
- `Autodesk.AutoCAD.DatabaseServices.MText` — 라벨/Block/Poly 텍스트 렌더용

---

## 8. 변경 이력

- **2026-09-19** Polyline `"CFM"` 오버룰 추가 — `_polyInstance` 신설(`AddOverrule(Polyline)` + `SetXDataFilter("CFM")`), `DrawPolyCfm`, `RegisterRegApp("CFM")`. `bool _isBlockMode` → `enum Mode { Line, Block, Poly }` 교체. `Polyline` using 별칭 추가. 짝 명령 `To_RoomCFM` 은 같은 날 `RoomCalc.cs` 에 신설. 문서 원본을 Obsidian → `Acadv25JArch\Overrule\TreeOverrule.md` 로 이관.
- **2026-05-20** DuctTree `Disp` 포맷 갱신(`{a}x{b}` → `{a}x{b}[{Total_CMH}]`) — `TreeOverrule` 코드 변경 없이 새 포맷 자동 반영 (Disp 값 그대로 사용). 예시 라벨 `"600x400[1234.5]"`.
- **2026-05-19** Line `"Disp"` 라벨 최우선 분기 추가 — `DrawLineTree` 라벨 합성 직전에 `JXdata.GetXdata(line, "Disp")` 검사 → non-empty 면 그 값을 그대로 사용 (DuctTree 의 `"{a}x{b}"` 사이즈 표시용). 기존 `15A`/`Total15A` 분기는 그대로 보존 (LineTree/FCU 는 Line `Disp` 미기록이라 충돌 없음).
- **2026-05-07** Line 라벨 포맷 단순화 — Leaf prefix `"15"` 고정 → `<Dia>` 동적, `TotalLPM` 기반 FCU 폴백 제거(주석 보존), `Dia`/`Total15A` 누락 시 `"00"` 폴백 (`?? "00"`). Leaf 분기는 `15A` 단독 케이스가 사라지고 항상 `<Dia>[<Total15A>]-<15A>` 형식으로 통일.
- **2026-05-05** Block `"Disp"` XData 분기 추가 — `_blockInstance` 신설, `DrawBlockDisp` 메서드, `RegisterRegApp("Disp")`
- **2026-05-05** Mid 색상 ACI 5(Blue) → 3(Green), `Total15A` 라벨 우선순위 도입
- **2026-04-27** `Overrule/` 폴더 분리

---

## 9. 관련 문서

- [[FcuLineTreeTechNote]] — Tree 분석 + `"Tree"` XData 기록의 원천
- [[LineTreeTechNote]] — Tree 분석 공통
- [[LPM]] — `"Disp"` XData 의 기록 시점
- [[architecture]] §2 — `To_RoomCFM` / `To_RoomText` / `To_RoomPoly` 명령 인덱스
- ArchOverrule.cs 의 `XDataFilterDrawOverrule` — 패턴 참고 (수정 금지)
