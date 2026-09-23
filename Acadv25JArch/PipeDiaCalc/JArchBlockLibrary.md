---
tags:
  - AutoCAD
---

# JArchBlockLibrary — 블럭 참조 도면

> 파일: `PipeDiaCalc/JArchBlockLibrary.cs`
> 클래스: `PipeLoad2.JArchBlockLibrary` (static)
> 최종 업데이트: 2026-09-23 (`_ST` 블럭으로 전환)

---

## 1. 개요

블럭 정의는 도면에 있든 없든 **매 실행마다 참조 도면에서 가져와 덮어쓴다**(사용자 확정, 2026-09-21).

| 항목 | 값 |
|---|---|
| 참조 도면 | `<DLL 폴더>\Blocks\JArch_Blocks.dwg` — `JArchBlockLibrary.LibraryPath` (`Assembly.GetExecutingAssembly().Location` 기준) |
| 저장소 원본 | `Acadv25JArch\Blocks\JArch_Blocks.dwg` (사용자가 직접 작성/유지, git 추적) |
| 개발 시 | csproj `None` + `CopyToOutputDirectory=PreserveNewest` 로 `C:\Jarch25[\Release]\Blocks\` 에 복사 (`JArchXData.arx` 와 같은 방식) |
| 배포 시 | `.iss` 가 `{app}\Contents\Blocks\JArch_Blocks.dwg` 로 설치. `#if !FileExists(...)` `#error` 가드로 누락 시 인스톨러 컴파일 실패 |
| 포함 블럭 | `JArch_Damper`(동적, Dis1/Dis2), `JArch_RPD_ST` / `JArch_SPD_ST` / `JArch_RAD_ST` / `JArch_SAD_ST`(속성 `SystemType`, 기본값 SA) — 필요에 따라 추가 |

**API**: `JArchBlockLibrary.Import(Database db, Editor ed, string blockName)` → `bool`

1. 참조 도면 존재 확인 (없으면 경로를 찍고 `false`)
2. `new Database(false, true)` + `ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, null)`
3. side db 의 `BlockTable[blockName]` ObjectId 획득 (없으면 오류 후 `false`)
4. `src.WblockCloneObjects(ids, db.BlockTableId, map, DuplicateRecordCloning.Replace, false)`

**왜 `Database.Insert` 가 아니라 `WblockCloneObjects` 인가** — 동적 블럭의 파라미터/액션은 블럭 정의(BTR)의 extension dictionary 에 있다. `Database.Insert(name, sourceDb, ...)` 는 소스의 **모델공간 엔티티**로 새 BTR 을 만들기 때문에 `Dis1`/`Dis2` 가 사라진다. 정의 자체를 deep clone 해야 보존된다. 블럭이 참조하는 레이어·선종류·문자스타일은 종속 객체로 함께 복사된다.

**호출 위치**: 열린 Transaction **밖에서** 호출하고, 그 뒤 새 Transaction 에서 `bt[blockName]` 을 조회한다.

⚠️ **덮어쓰기(Replace) 의 대가** — 매 실행마다 정의가 교체되므로 이미 삽입된 모든 인스턴스가 함께 regen 된다(디퓨저가 많은 도면에서 체감될 수 있음). 도면 안에서 블럭을 손으로 수정해둔 것도 매번 원복된다. 참조 도면을 정본으로 보는 전제에서는 의도한 동작이며, 비용이 문제가 되면 "없으면 가져오고 갱신은 별도 명령" 으로 바꿀 수 있다.

---

## 2. 사용처

- [[InsertDamper]] — `JArch_Damper` (동적 블럭)
- [[InsertDiffuser]] — `JArch_RPD_ST` / `JArch_SPD_ST` / `JArch_RAD_ST` / `JArch_SAD_ST` (2026-09-23 `_ST` 로 전환)

블럭을 추가할 때는 참조 도면에 그려 넣고, 쓰는 쪽에서 이름만 넘기면 된다 — `JArchBlockLibrary` 는 수정할 필요가 없다.

---

## 3. 검증 항목 (2026-09-21 기준 미검증)

동적 블럭 정의를 **기존 참조가 있는 상태에서 Replace 로 교체**하는 동작은 아직 실제 도면에서 확인하지 않았다. 첫 테스트 순서:

1. 빈 도면에서 `Insert_Damper` → 블럭이 들어오고 `Dis1` 변경 시 형상이 실제로 바뀌는지
2. 같은 도면에서 한 번 더 실행 → 기존 인스턴스가 깨지지 않는지 (Replace 정책의 핵심 위험)
3. 이름은 같지만 내용이 다른 블럭이 미리 있는 도면에서 실행 → 의도대로 교체되는지
