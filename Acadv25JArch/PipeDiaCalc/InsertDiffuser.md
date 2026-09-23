---
tags:
  - AutoCAD
---

# Insert_Diffuser / Diffuser_Spec / Insert_Diffuser_Bypoly 명령어

> 파일: `PipeDiaCalc/DiffuserInsertCommand.cs`
> 클래스: `PipeLoad2.DiffuserInsertCommand.Cmd_InsertDiffuser`
> 최종 업데이트: 2026-09-23 (`_ST` 블럭 + `SystemType` 속성 · 선정표 32행 · `Insert_Diffuser_Bypoly` 추가 · Bypoly 배치 원점을 Poly 좌측 기준으로)
> 이전: 2026-09-22 (`SystemType` 입력 · `Diffuser_Spec` 명령 · 지정 Text 표시 스타일 · 입력 순서 CFM→Type→SystemType→개수)

---

## 1. 개요

룸 필요 풍량 · 디퓨저 Type · 개수를 입력받아 **디퓨저 선정표**에서 사이즈를 자동 결정하고, 선택 지점부터 수평(+X)으로 블럭을 개수만큼 배치한다. 각 블럭에는 선정 결과와 한 대당 풍량을 XData 로 기록하므로 배치 직후 [[DuctTreeTechNote]] (`DUCTTREE`) 의 Leaf 부하로 바로 소비되고 [[TreeOverrule]] (`TTG`) 에 Red 텍스트로 표시된다.

| 단계 | 프롬프트 | 비고 |
|---|---|---|
| ① | `룸 필요 풍량(CMH)을 입력하세요:` | `PromptDoubleOptions`, 양수만 |
| ② | `디퓨저 Type 을 선택하세요 [RPD/SPD/RAD/SAD]` | `PromptKeywordOptions`, Enter = `RPD` |
| ③ | `계통(SystemType)을 선택하세요 [SA/RA/EA/OA]` | `PromptKeywordOptions`, Enter = `SA`. 급기/환기/배기/외기 |
| ④ | `디퓨저 개수를 입력하세요:` | `PromptIntegerOptions`, 1 이상, 기본 1 |
| ⑤ | (선정 결과 출력) | `선정: SA RPD 550A ND300 (표준 1300 CMH, 한 대당 1250 CMH × 2개)` |
| ⑥ | `배치 시작점을 지정하세요:` | `ed.GetPoint` |
| ⑦ | (블럭 정의 가져오기) | `JArchBlockLibrary.Import(db, ed, "JArch_" + type + "_ST")` — Transaction 밖, 실패 시 종료 ([[JArchBlockLibrary]]) |

입력 순서는 `Diffuser_Spec` 의 Text 필드 순서(`2400,RPD,RA,3` = CFM, Type, SystemType, 개수)와 **같게 맞춰 두었다** (2026-09-22). 두 명령을 번갈아 쓸 때 순서를 다시 생각하지 않도록.

---

## 2. 선정 규칙

```
한 대당 풍량 = 룸 풍량 ÷ 개수
선정 행      = 해당 Type 행을 표준풍량 오름차순 정렬 → 표준풍량 ≥ 한 대당 인 첫 행
```

- 예: 룸 2500 CMH, RPD, 2개 → 1250 → RPD 480A(850) < 1250 ≤ **550A(1300)** → `RPD 550A ND300`
- 한 대당 풍량이 Type 의 최대 표준풍량을 넘으면 **최대 행으로 배치하고 경고** 출력 (중단하지 않음)
- 최소/최대풍량 컬럼은 표에 보존만 하고 선정에는 쓰지 않는다 (표준풍량 단일 기준)

---

## 3. 디퓨저 선정표 (`DiffuserSpec[] Table`, 32행)

| TYPE | SIZE | ND | 최소풍량 | 표준풍량 | 최대풍량 |
|---|---|---|---|---|---|
| RPD | 270A | 125 | 140 | 200 | 380 |
| RPD | 320A | 150 | 205 | 300 | 550 |
| RPD | 420A | 200 | 360 | 550 | 970 |
| RPD | 480A | 250 | 555 | 850 | 1480 |
| RPD | 550A | 300 | 850 | 1300 | 2160 |
| RPD | 600A | 350 | 1100 | 1800 | 2930 |
| RPD | 650A | 400 | 1410 | 2200 | 3765 |
| SPD | 300x300 | 125 | 140 | 200 | 380 |
| SPD | 300x300 | 150 | 205 | 300 | 550 |
| SPD | 300x300 | 200 | 360 | 550 | 970 |
| SPD | 410x410 | 250 | 555 | 850 | 1480 |
| SPD | 460x460 | 300 | 850 | 1300 | 2160 |
| SPD | 620x620 | 350 | 1100 | 1800 | 2930 |
| SPD | 620x620 | 375 | 1250 | 2000 | 3300 |
| RAD | 175A | 100 | 100 | 150 | 300 |
| RAD | 230A | 100 | 150 | 225 | 350 |
| RAD | 270A | 125 | 180 | 270 | 420 |
| RAD | 320A | 150 | 200 | 300 | 600 |
| RAD | 420A | 200 | 400 | 600 | 1200 |
| RAD | 480A | 250 | 600 | 900 | 1800 |
| RAD | 550A | 300 | 800 | 1200 | 2400 |
| RAD | 650A | 400 | 1200 | 1800 | 3600 |
| RAD | 820A | 450 | 1350 | 2000 | 4000 |
| RAD | 820A | 500 | 1500 | 2500 | 5000 |
| SAD | 250x250 | 100 | 100 | 150 | 300 |
| SAD | 250x250 | 125 | 140 | 200 | 380 |
| SAD | 300x300 | 150 | 200 | 300 | 600 |
| SAD | 300x300 | 200 | 400 | 600 | 1200 |
| SAD | 410x410 | 250 | 600 | 900 | 1800 |
| SAD | 460x460 | 300 | 800 | 1200 | 2400 |
| SAD | 620x620 | 350 | 1100 | 1800 | 2930 |
| SAD | 620x620 | 375 | 1250 | 2000 | 3300 |

권장풍속(RPD/SPD 3.0~8.0, RAD/SAD 2.5~7.0, SAD 2.5~6.0 m/s)은 코드에 넣지 않았다. 원본 표는 사용자 제공 이미지.

**2026-09-23 갱신 (36행 → 32행)**

| 구분 | 내용 | 선정 영향 |
|---|---|---|
| 삭제 | `RPD 650A ND375` (1200/2100/3450) | 한 대당 1801~2100 이 `650A ND400`(표준 2200)으로 올라간다 |
| 삭제 | `RAD 650A ND350` (1000/1500/3000) | 한 대당 1201~1500 이 `650A ND400`(표준 1800)으로 올라간다 |
| 값 변경 | `SAD 250x250 ND125` 150/225/450 → **140/200/380** | 표준이 낮아져 201~225 구간이 `300x300 ND150`(표준 300)으로 넘어간다 |

`ND` 가 바뀌면 **블럭 간격(ND×2)** 도 함께 달라진다. SPD 는 변경 없음.

---

## 4. 배치

| 항목 | 값 |
|---|---|
| 블럭 이름 | **`"JArch_" + Type + "_ST"`** (`JArch_RPD_ST` / `JArch_SPD_ST` / `JArch_RAD_ST` / `JArch_SAD_ST`) — `BlockPrefix` + `BlockSuffix` 상수. 접두는 `RPD` 같은 흔한 이름이 사용자 도면의 기존 블럭과 충돌하는 것을 피하기 위함이고, `_ST` 접미는 **`SystemType` 속성을 가진 블럭**이라는 표시다(2026-09-23 전환 — 그전엔 속성 없는 `JArch_RPD` 를 썼다). 매 실행마다 참조 도면에서 가져오므로 도면에 없어도 된다([[JArchBlockLibrary]]) |
| 블럭 속성 | 삽입 후 `AppendAttributes` 가 정의의 `AttributeDefinition` 들로 `AttributeReference` 를 만들어 붙이고, Tag `SystemType` 에 선택한 계통을 기록. 나머지 속성은 정의의 기본값 유지 |
| 방향 | 시작점에서 **월드 +X** 로 순차 배치 (회전 0, 축척 1, 현재 레이어) |
| 간격 | **순간격 = ND × 2** (블럭 외곽선 사이 거리). 피치 = 첫 블럭 `GeometricExtents` 폭 + 순간격. **속성을 붙이기 전에 계산**한다 — 속성 텍스트가 extents 에 끼면 간격이 의도보다 벌어진다 |
| 삽입 공간 | `db.CurrentSpaceId` |
| Transaction | 단일 transaction 안에서 `AppendEntity` → `AddNewlyCreatedDBObject` → XData 기록 |

예: RPD 550A ND300, 2개 → 순간격 600. 블럭 폭이 550 이면 두 번째 블럭은 시작점 +1150.

### 블럭 속성 (`AppendAttributes`)

⚠️ **프로그램으로 `BlockReference` 를 만들면 속성이 자동 생성되지 않는다.** 정의의 `AttributeDefinition` 을 순회해 `AttributeReference` 를 직접 만들어 붙여야 하며, 빼먹으면 도면에 속성이 아예 보이지 않는다(기본값조차).

```csharp
var ar = new AttributeReference();
ar.SetAttributeFromBlock(ad, br.BlockTransform);
if (ad.Tag.Equals("SystemType", StringComparison.OrdinalIgnoreCase))
    ar.TextString = systemType;
br.AttributeCollection.AppendAttribute(ar);
tr.AddNewlyCreatedDBObject(ar, true);
```

| 항목 | 처리 |
|---|---|
| Tag `SystemType` | 선택한 계통(SA/RA/EA/OA) 기록. Tag 비교는 `OrdinalIgnoreCase` |
| 그 외 Tag | 정의의 기본값(`ad.TextString`) 유지 |
| 상수 속성(`ad.Constant`) | 정의에 포함되므로 참조를 만들지 않고 건너뜀 |
| 속성이 없을 때 | 첫 블럭에서 `[경고] 'JArch_RPD_ST' 에 'SystemType' 속성이 없어 계통을 기록하지 못했습니다.` 출력(중단하지 않음) |

계통은 **속성과 XData 양쪽에 기록**된다 — 속성은 도면에서 보이는 값, XData 는 조회·필터용.

---

## 5. XData 스펙

| RegApp | 값 | 용도 |
|---|---|---|
| `Diffuser` | `"RPD"` 등 (= Type) | 디퓨저 블럭 식별용 RegApp — `Type` 과 같은 값. **블럭명(`JArch_RPD`)이 아니라 Type(`RPD`) 을 넣는다** — 선정표·사양서 용어를 유지 |
| `SystemType` | `"SA"` / `"RA"` / `"EA"` / `"OA"` | 계통 — 급기/환기/배기/외기. **기록만 하고 선정 로직에는 쓰지 않는다** (계통별로 표·블럭을 달리 골라야 하면 여기서 분기) |
| `Type` | `"RPD"` 등 | 선정 Type |
| `Size` | `"550A"` / `"300x300"` 등 | 선정 사이즈 (문자열 그대로) |
| `ND` | `"300"` | 목 지름 |
| `CMH` | `"1250"` (한 대당, `"0.##"`) | `DUCTTREE` Leaf 부하 — [[CMH]] 명령과 동일 패턴 |
| `Disp` | `CMH` 와 동일 문자열 | `TTG` Block 텍스트 표시용 |

`tr.ChecRegNames(db, "Diffuser,SystemType,Type,Size,ND,CMH,Disp")` 로 RegAppTable 선등록. 모든 값은 `JXdata.SetXdata`(→ `JArchXData.arx`, 라이선스 만료 시 기록 생략) 로 기록하므로 엔티티를 **DB 에 추가한 뒤** 호출한다.

---

## 6. 블럭 참조 도면

블럭 정의는 매 실행마다 `<DLL 폴더>\Blocks\JArch_Blocks.dwg` 에서 가져와 덮어쓴다(`JArchBlockLibrary.Import`). 상세는 [[JArchBlockLibrary]].

---

## 7. Diffuser_Spec 명령 (같은 파일)

> `[CommandMethod("Diffuser_Spec", CommandFlags.UsePickSet)]` — `Cmd_DiffuserSpec` (2026-09-22 신규)

도면에 이미 적혀 있는 **`"2400,RPD,RA,3"` 형식의 Text** 를 검증하고, 통과한 것에만 XData RegApp **`Diffuser` = Type** 을 기록해 "디퓨저 Spec Text" 로 표시한다. 블럭을 만들지는 않는다.

지정에 성공한 Text 는 **색상 ACI 41 / 기울기 5°** 로 바꾼다 — 지정된 것과 안 된 것을 화면에서 바로 구분하기 위한 표식이다. 건너뛴 Text 는 색상·기울기 모두 원본 그대로 남는다.

### 필드 형식

```
2400 , RPD , RA , 3
 │      │     │    └ 개수
 │      │     └ SystemType
 │      └ 디퓨저 Type
 └ CFM
```

| 필드 | 규칙 | 실패 시 메시지 |
|---|---|---|
| 필드 수 | 쉼표로 나눠 **정확히 4개** | `필드가 3개 (CFM,Type,SystemType,개수 = 4개 필요)` |
| CFM | 숫자 + **양수** (`double.TryParse`, InvariantCulture) | `CFM '24O0' 은 양수가 아닙니다` |
| Type | `RPD` / `SPD` / `RAD` / `SAD` | `Type 'RPX' 는 [RPD/SPD/RAD/SAD] 중 하나여야 합니다` |
| SystemType | `SA` / `RA` / `EA` / `OA` | `SystemType 'XA' 는 [SA/RA/EA/OA] 중 하나여야 합니다` |
| 개수 | **1 이상 정수** | `개수 '0' 는 1 이상의 정수여야 합니다` |

- 검증 목록은 `Insert_Diffuser` 의 `Types` / `SystemTypes` 상수를 **그대로 공유**한다 (한쪽만 바뀔 일이 없다).
- Type / SystemType 은 **대소문자를 구분하지 않고** 받아 표준 표기(대문자)로 정규화해 기록한다.
- 각 필드는 `Trim()` 하므로 `2400, RPD , RA, 3` 도 통과한다.

### 동작

1. `디퓨저 Spec Text 를 선택하세요 (예: 2400,RPD,RA,3)` — `JEntityFunc.GetEntityByTpye<DBText>` + `MakeFilterTypes("TEXT")`, **여러 개 동시 선택 가능**
2. `tr.ChecRegNames(db, "Diffuser")`
3. Text 별로 `TryParseSpec` → 실패하면 `[건너뜀] "원문" — 이유` 출력 후 **다음 Text 계속**
4. 잠긴 레이어의 Text 도 같은 방식으로 건너뜀
5. 통과분만 `txt.UpgradeOpen()` → `JXdata.SetXdata(txt, "Diffuser", type)` → **표시 스타일 적용**
6. 요약: `3건 기록 완료, 1건 건너뜀.`

### 지정 성공 Text 표시 스타일

| 항목 | 상수 | 값 | 코드 |
|---|---|---|---|
| 색상 | `SpecColorIndex` | ACI **41** | `txt.ColorIndex = 41` |
| 기울기 | `SpecObliqueDeg` | **5°** | `txt.Oblique = 5 * Math.PI / 180.0` (라디안) |

`DBText.Oblique` 는 **라디안** 이므로 도 단위 상수를 변환해 넣는다. AutoCAD 문자 속성의 *기울기 각도* 이며 *회전(Rotation)* 과는 다르다. 허용 범위는 ±85° 이내.

단일 transaction. **CFM / SystemType / 개수는 검증만 하고 XData 로는 남기지 않는다** — Text 자체가 데이터를 갖고 있기 때문. 이 값들도 XData 로 필요해지면 `CMH` / `SystemType` / `Count` 로 추가할 것.

⚠️ **MTEXT 는 대상이 아니다** — 필터가 `TEXT`(DBText) 뿐이다.

---

## 8. Insert_Diffuser_Bypoly 명령 (같은 파일)

> `[CommandMethod("Insert_Diffuser_Bypoly", CommandFlags.UsePickSet)]` — `Cmd_InsertDiffuserByPoly` (2026-09-23 신규)

XData `Room` 을 가진 Poly 를 선택하면 **그 안의 Spec Text**(§7 `Diffuser_Spec` 으로 지정한 것)를 찾아 `CFM,Type,SystemType,개수` 를 읽고 **Poly 센터 기준으로** 배치한다. 사용자 입력(풍량·Type·계통·개수·기준점)이 전혀 없다 — 전부 Text 에서 읽는다.

**Poly 는 여러 개를 한 번에 선택할 수 있다.** 선택 헬퍼가 `ed.GetSelection` 기반이라 다중 선택이 기본이고, Poly 마다 센터·내부 Text 를 따로 처리한다. 다만 선택 필터가 `LWPOLYLINE` + XData `Room` 이라 **XData `Room` 이 없는 폴리와 구형 `POLYLINE`(2D/3D) 은 조용히 빠진다** — 일부만 처리된 것처럼 보이면 멀티 선택이 아니라 이쪽을 의심할 것.

### 실행 순서 (4단계 — Editor 호출과 Transaction 을 분리)

| 단계 | 내용 |
|---|---|
| ① | `ed.SelectAll(MakeFilterTypesRegs("TEXT", "Diffuser"))` — 도면 전체의 Spec Text 수집. **Transaction 밖**. 하나도 없으면 오류 후 종료 |
| ② | Transaction A(읽기) — `MakeFilterTypesRegs("LWPOLYLINE", "Room")` 로 Poly 선택 → Poly 별 센터·내부 Text 파싱 → **배치 계획 목록**(원점, Spec) 작성 |
| ③ | `JArchBlockLibrary.Import` — 계획에 나온 **Type 별 1회**. **Transaction 밖**([[JArchBlockLibrary]] 요구사항) |
| ④ | Transaction B(쓰기) — 계획대로 `PlaceRow` 호출 |

`ed.SelectAll` 은 뷰포트와 무관하므로 `SelectCrossingWindow` 처럼 Zoom fit 이 필요 없다. 계획 단계에서 **좌표·값만 복사**해 두므로 Transaction A 를 닫은 뒤에도 stale 참조 문제가 없다(`DuctNode.Line` 전례 참고).

### 배치 규칙

| 항목 | 값 |
|---|---|
| 기준 도형 | Poly 의 `GeometricExtents` (좌측 끝 `MinPoint.X`, 센터 Y) |
| 내부 판정 | XY 평면 **ray casting** (`IsInsidePoly`) |
| Text 순서 | **Y 내림차순 → 같은 높이면 X 오름차순** (위→아래, 왼→오른쪽) |
| 줄 시작점 | **k 번째 항목의 원점 = ( `MinPoint.X` + `RowStartFromLeft`(800) , 센터 Y − `RowGap`(800) × k )** |
| 한 줄 | 원점부터 +X, 순간격 = ND × 2 — `Insert_Diffuser` 와 동일 (`PlaceRow` 공용) |
| 중복 주의 | Poly 가 겹치거나 중첩되면 같은 Text 가 양쪽에 잡혀 **두 번 배치**된다 |
| 선정 | `CFM ÷ 개수` → 표준풍량 ≥ 한 대당 인 최소 행 (`SelectSpec` 공용) |
| 기록 | 블럭 속성 `SystemType` + XData 7건 — §5 와 동일 |

**X 는 Poly 센터가 아니라 좌측 끝 기준이다** (2026-09-23 변경). 센터 기준(구: 센터 − 600)으로 하면 룸이 넓어질수록 블럭 그룹이 오른쪽으로 밀려나 룸마다 위치가 달라진다. 좌측 벽 기준이면 룸 크기와 무관하게 항상 같은 자리에서 시작한다. Y 만 센터를 쓴다.

| 변경 이력 | X 원점 | 줄 간격 |
|---|---|---|
| 최초 | 센터 | 400 |
| 2026-09-23 ① | 센터 − 600 | 800 |
| 2026-09-23 ② (현재) | **좌측 끝 + 800** | 800 |

Poly 안에 유효한 Text 가 없으면 `[알림] Poly(핸들 …) 안에 유효한 Spec Text 가 없습니다.` 를 출력하고 다음 Poly 로 넘어간다. 형식이 틀린 Text 는 `[건너뜀] "원문" — 이유` 출력 후 계속.

### 공용 헬퍼 (Insert_Diffuser 와 공유)

새 명령이 삽입 로직을 복사하지 않도록 2026-09-23 에 뽑아낸 것들이다. **한쪽만 고치면 두 명령이 어긋나므로 여기만 고칠 것.**

| 헬퍼 | 역할 |
|---|---|
| `TryParseSpec(text, out SpecText?, out reason)` | Spec Text 파싱·검증. `record SpecText(Cfm, Type, SystemType, Count)` 반환 (구: `out string type` 만) |
| `SelectSpec(type, perUnit, out exceeded)` | 선정표 조회 |
| `PlaceRow(tr, space, btrId, basePt, spec, systemType, count, cmhStr, blockName, ed)` | 한 줄 배치 + 속성 + XData 7건 |
| `AppendAttributes(tr, br, btrId, systemType)` | 블럭 속성 생성 (§4 참고) |

### 한계 / 주의

- ⚠️ **원호(bulge) 구간은 정점 사이 현으로 근사**한다. 곡선 경계 룸은 경계 근처 Text 를 오판할 수 있다.
- ⚠️ **센터가 Poly 밖일 수 있다** — extents 중심이라 L 자형 룸에서는 배치 원점이 룸 바깥으로 나간다. 무게중심이 필요하면 `PolyCenter` 를 바꿀 것.
- ⚠️ `jCadExtention.cs` 의 `Polyline.Contains(Point3d)` 확장은 **bbox + 5.0 여유 검사일 뿐**이라 이 명령에서는 쓰지 않는다(L 자형에서 밖의 Text 를 안이라고 판정). 기존 확장은 그대로 두고 private `IsInsidePoly` 를 따로 두었다.

---

## 9. 관련 명령 / 문서

- [[CMH]] — 기존 블럭에 `CMH`/`Disp` 를 붙이는 명령. `Insert_Diffuser` 는 삽입과 동시에 같은 XData 를 기록
- [[DuctTreeTechNote]] — `"CMH"` XData 를 Leaf 부하로 소비
- [[TreeOverrule]] — Block `"Disp"` Red 텍스트 표시
- [[InsertDamper]] — 같은 패턴의 블럭 삽입 명령. `JArchBlockLibrary` 를 공유한다
- `To_RoomText` (`RoomCalc.cs`) — Text 에 XData 만 표시하는 같은 성격의 명령
- [[architecture]] §4 — 명령 인덱스
