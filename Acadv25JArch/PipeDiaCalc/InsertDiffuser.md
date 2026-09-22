---
tags:
  - AutoCAD
---

# Insert_Diffuser / Diffuser_Spec 명령어

> 파일: `PipeDiaCalc/DiffuserInsertCommand.cs`
> 클래스: `PipeLoad2.DiffuserInsertCommand.Cmd_InsertDiffuser`
> 최종 업데이트: 2026-09-22 (`SystemType` 입력 추가 · `Diffuser_Spec` 명령 추가 · 지정 Text 표시 스타일 · 입력 순서 CFM→Type→SystemType→개수)

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
| ⑦ | (블럭 정의 가져오기) | `JArchBlockLibrary.Import(db, ed, "JArch_" + type)` — Transaction 밖, 실패 시 종료 ([[JArchBlockLibrary]]) |

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

## 3. 디퓨저 선정표 (`DiffuserSpec[] Table`)

| TYPE | SIZE | ND | 최소풍량 | 표준풍량 | 최대풍량 |
|---|---|---|---|---|---|
| RPD | 270A | 125 | 140 | 200 | 380 |
| RPD | 320A | 150 | 205 | 300 | 550 |
| RPD | 420A | 200 | 360 | 550 | 970 |
| RPD | 480A | 250 | 555 | 850 | 1480 |
| RPD | 550A | 300 | 850 | 1300 | 2160 |
| RPD | 600A | 350 | 1100 | 1800 | 2930 |
| RPD | 650A | 375 | 1200 | 2100 | 3450 |
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
| RAD | 650A | 350 | 1000 | 1500 | 3000 |
| RAD | 650A | 400 | 1200 | 1800 | 3600 |
| RAD | 820A | 450 | 1350 | 2000 | 4000 |
| RAD | 820A | 500 | 1500 | 2500 | 5000 |
| SAD | 250x250 | 100 | 100 | 150 | 300 |
| SAD | 250x250 | 125 | 150 | 225 | 450 |
| SAD | 300x300 | 150 | 200 | 300 | 600 |
| SAD | 300x300 | 200 | 400 | 600 | 1200 |
| SAD | 410x410 | 250 | 600 | 900 | 1800 |
| SAD | 460x460 | 300 | 800 | 1200 | 2400 |
| SAD | 620x620 | 350 | 1100 | 1800 | 2930 |
| SAD | 620x620 | 375 | 1250 | 2000 | 3300 |

권장풍속(RPD/SPD 3.0~8.0, RAD/SAD 2.5~7.0 m/s)은 코드에 넣지 않았다. 원본 표는 2026-09-19 사용자 제공 이미지.

---

## 4. 배치

| 항목 | 값 |
|---|---|
| 블럭 이름 | **`"JArch_" + Type`** (`JArch_RPD` / `JArch_SPD` / `JArch_RAD` / `JArch_SAD`) — `BlockPrefix` 상수. 접두를 붙이는 이유는 `RPD` 같은 흔한 이름이 사용자 도면의 기존 블럭과 충돌하기 때문. 매 실행마다 참조 도면에서 가져오므로 도면에 없어도 된다([[JArchBlockLibrary]]) |
| 방향 | 시작점에서 **월드 +X** 로 순차 배치 (회전 0, 축척 1, 현재 레이어) |
| 간격 | **순간격 = ND × 2** (블럭 외곽선 사이 거리). 피치 = 첫 블럭 `GeometricExtents` 폭 + 순간격 |
| 삽입 공간 | `db.CurrentSpaceId` |
| Transaction | 단일 transaction 안에서 `AppendEntity` → `AddNewlyCreatedDBObject` → XData 기록 |

예: RPD 550A ND300, 2개 → 순간격 600. 블럭 폭이 550 이면 두 번째 블럭은 시작점 +1150.

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

## 8. 관련 명령 / 문서

- [[CMH]] — 기존 블럭에 `CMH`/`Disp` 를 붙이는 명령. `Insert_Diffuser` 는 삽입과 동시에 같은 XData 를 기록
- [[DuctTreeTechNote]] — `"CMH"` XData 를 Leaf 부하로 소비
- [[TreeOverrule]] — Block `"Disp"` Red 텍스트 표시
- [[InsertDamper]] — 같은 패턴의 블럭 삽입 명령. `JArchBlockLibrary` 를 공유한다
- `To_RoomText` (`RoomCalc.cs`) — Text 에 XData 만 표시하는 같은 성격의 명령
- [[architecture]] §4 — 명령 인덱스
