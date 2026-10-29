---
tags:
  - AutoCAD
---

# Insert_Duct_Size 명령어

> 파일: `PipeDiaCalc/DuctSizeTextCommand.cs`
> 클래스: `PipeLoad2.DuctSizeTextCommand.Cmd_InsertDuctSize`
> 최종 업데이트: 2026-10-29 (신규, F-004)

---

## 1. 개요

XData `Tree` 가 있는 덕트 Line 을 여러 개 선택하면 **각 Line 중점에 덕트 사이즈 `"{a}x{b}"` Text** 를 놓는다. `a`/`b` 는 Line XData 값(`DUCTTREE` Mode D 산정 결과)을 **문자열 그대로** 붙인다 (예: `a=350`, `b=250` → `350x250`).

| 단계 | 동작 |
|---|---|
| ① | `사이즈를 표시할 덕트 Line 들을 선택하세요:` — `GetSelection` + `JSelFilter.MakeFilterTypesRegs("LINE", "Tree")` |
| ② | Line 별 XData `a`, `b` 읽기. 둘 중 하나라도 없거나 `a` 가 양수가 아니면 **그 Line 만 건너뜀** |
| ③ | **`a < 300`** 이면 건너뜀 |
| ④ | **Line 길이 ≤ `a × 4`** 이면 건너뜀 (짧은 구간엔 글자가 덕트 밖으로 넘침) |
| ⑤ | 중점에 DBText 생성 — MiddleCenter 정렬, 높이 `a / 2`, 현재 레이어·현재 문자 스타일 |
| ⑥ | Text 에 XData **`DuctSize` = 글자 그대로** 기록 |
| ⑦ | `Insert_Duct_Size 완료 — 표시 N개, a<300 생략 n개, 길이≤a×4 생략 m개[, XData a/b 없음·오류 k개]` 출력 |

---

## 2. 규칙

| 항목 | 값 | 코드 |
|---|---|---|
| 글자 | `"{a}x{b}"` (XData 문자열 그대로, 숫자 변환 없음) | `TextString` |
| 높이 | `a / 2` | `Height` |
| 위치 | Line 중점, 글자 중앙이 중점 | `TextCenter` + `TextVerticalMid` + `AlignmentPoint` |
| 회전 | `line.Angle` 을 (-90°, 90°] 로 정규화 — 글자가 뒤집히지 않음 | `rot -= π` (90° 초과 ~ 270° 이하) |
| 표시 조건 | `a >= 300` | `MinA = 300` |
| 길이 조건 | `Length > a × 4` | `MinLenRatio = 4` (2026-10-29 3 → 4) |
| XData | RegApp `DuctSize` = TextString | `ChecRegNames` + `SetXdata` (AppendEntity 이후) |

---

## 3. 주의

- ⚠️ **중복 검사 없음** — 같은 Line 들로 다시 실행하면 같은 자리에 Text 가 또 생긴다. 기존 Text 는 XData `DuctSize` 로 식별할 수 있다(backlog).
- `a`/`b` 무효 Line 이 섞여 있어도 명령을 중단하지 않는다 — Line 마다 독립된 Text 라서 (`Insert_UpDn` 은 하나만 무효여도 중단하는 것과 다름).
- 생성 Text 의 `DuctSize` XData 는 `XD_DelALL` 로 지울 수 있다(필터에 `TEXT` 포함).
- `TTG` 의 Line `Disp` 라벨(`350x250[1200]`)은 화면 표시용 오버룰이고, 이 명령은 **도면에 실제 Text 객체**를 만든다.
