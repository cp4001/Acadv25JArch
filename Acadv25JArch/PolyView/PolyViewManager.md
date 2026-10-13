# PolyView / PolyViewManager 설계 사양 (v0.1 — 초안)

도면 위의 닫힌 Polyline 을 **PolyView**(이름 있는 영역 북마크)로 등록하고,
`PLVM` 팔레트의 TreeView 에서 그룹으로 분류·탐색하는 기능.

- 대상: AutoCAD 2025 / .NET 8 (`Acadv25JArch`)
- 명령: `POLYVIEW`(등록·수정), `PLVM`(팔레트)
- 상태: **설계 확정 전 초안.** §10 미결 항목을 먼저 확정한 뒤 코드 작업 착수

---

## 1. 용어

| 용어 | 정의 |
|---|---|
| **PolyView** | 이름·내용이 부여된 닫힌 Polyline 1개. 도면상의 실제 데이터 |
| **Group 노드** | 팔레트 트리에서만 존재하는 분류용 폴더. 도면 도형 없음 |
| **Root 노드** | 트리 최상위. 표시명은 현재 도면 파일명 |
| **누락(Orphan) 항목** | 트리 구조에는 있으나 도면에서 Poly 를 찾을 수 없는 항목 |
| **미편입(Unfiled) 항목** | Poly 에 PolyView XData 는 있으나 트리 구조에 자리가 없는 항목 |

---

## 2. 데이터 저장 (확정)

**XData + NOD 병행.** 이름·내용은 Poly 엔티티에, 그룹 트리 구조만 도면 NOD 에 저장한다.

### 2.1 XData — PolyView 의 정본

| RegApp | 값 | 비고 |
|---|---|---|
| `PolyView` | PolyView 이름 | 이 XData 의 존재 여부가 "PolyView 인가"의 판정 기준 |
| `PolyViewMemo` | 내용(설명) | 없을 수 있음(빈 문자열 허용) |

**색상** — PolyView 로 지정된 Poly 는 `ColorIndex = 61` 로 바꾼다. 도면에서 PolyView 영역을
한눈에 구분하기 위한 표식이다. XData 를 쓰는 지점이 `WriteXData` 한 곳뿐이라 거기서 같이 처리하며,
따라서 **속성 편집으로 이름·내용을 고칠 때도 색상이 61 로 다시 맞춰진다**
(개별 PolyView 만 다른 색으로 두고 싶으면 이 동작을 등록 시점으로 한정해야 한다).

- 기록은 기존 경로 그대로: `tr.ChecRegNames(db, "PolyView,PolyViewMemo")` 로 RegApp 선등록 후
  `JXdata.SetXdata(obj, "PolyView", name)` (→ `JArch.Xdata.SetOpen` → `JArchXData.arx`)
- 읽기는 `JXdata.GetXdata(obj, "PolyView")`
- **엔티티는 DB 에 추가된 뒤** 기록한다 (기존 `Insert_Diffuser` 규칙과 동일)
- 라이선스 만료 시 arx 가 조용히 기록을 생략하므로, 명령은 **기록 후 되읽어 검증**하고
  값이 비면 "라이선스 확인 필요" 메시지를 낸다

**이 방식의 이점**: Poly 를 다른 도면으로 복사해도 이름·내용이 따라간다.
도착한 도면에서는 §4.3 규칙에 따라 자동으로 Root 밑에 편입된다.

### 2.2 NOD — 그룹 트리 구조만

| 항목 | 값 |
|---|---|
| 사전 이름 | `JARCH_POLYVIEW` (NamedObjectsDictionary 직속) |
| 키 | `TreeJson` (Xrecord 1개) |

`TreeJson` 은 아래 JSON 을 **200자 단위로 분할**(`PolyViewStore.ChunkLen`, 한글 3바이트 기준 여유)해 여러 개의
`TypedValue((int)DxfCode.Text, chunk)` 로 저장한다(Xrecord 문자열 길이 제한 회피).
읽을 때는 모든 Text 값을 순서대로 이어붙인 뒤 파싱한다.

```json
{
  "v": 1,
  "root": {
    "g": [
      { "n": "1층", "g": [ { "n": "기계실", "g": [], "i": ["2A44"] } ], "i": ["2A3F"] },
      { "n": "2층", "g": [], "i": [] }
    ],
    "i": ["2B01"]
  }
}
```

| 키 | 의미 |
|---|---|
| `v` | 스키마 버전 (현재 1) |
| `n` | 그룹 이름 |
| `g` | 하위 그룹 배열 (순서 = 트리 표시 순서) |
| `i` | 이 그룹에 직접 속한 PolyView 의 **Handle 문자열(16진)** 배열 (순서 보존) |

- Root 자신은 `n` 을 갖지 않는다 (표시명은 항상 도면 파일명)
- 사전·Xrecord 가 없으면 "모든 PolyView 가 Root 직속"으로 간주 (자동 생성하지 않음)
- 저장은 트리가 바뀔 때마다 즉시 (그룹 생성/삭제/이름변경/노드 이동)

> Handle 은 같은 도면 안에서만 유효하다. 다른 도면으로 복사된 Poly 는 Handle 이 달라지므로
> 트리 구조가 따라가지 않는 것이 정상이며, §4.3 으로 처리된다.

---

## 3. `POLYVIEW` 명령 (등록 / 수정)

### 3.1 흐름

| 단계 | 동작 | 실패 시 |
|---|---|---|
| 1 | `PromptEntityOptions`: "PolyView 로 지정할 Poly 선택:" — `Polyline` 만 허용 | 취소 → 종료 |
| 2 | 닫힘 검사 (`pline.Closed == true`) | "닫힌 Polyline 만 지정할 수 있습니다." → 재선택 |
| 3 | 기존 `PolyView` XData 확인 → 있으면 **수정 모드**(기존 값이 기본값) | — |
| 4 | 이름 입력 (`PromptStringOptions`, `AllowSpaces = true`) | 빈 문자열 → 재입력 |
| 5 | 이름 중복 검사 (§3.2) | 중복 → 경고 후 재입력 |
| 6 | 내용 입력 (빈 값 허용) | — |
| 7 | RegApp 등록 → **색상 61 적용** → XData 기록 → 되읽어 검증 | 값 없음 → 라이선스 경고 |
| 8 | 신규면 NOD 트리 Root 의 `i` 배열 끝에 Handle 추가 | — |
| 9 | 팔레트가 열려 있으면 갱신 | — |

- 도형은 **만들지 않는다.** 사용자가 미리 그려둔 Poly 를 선택만 한다
- 형상 제약은 "닫힌 Polyline" 까지만. 사각형 여부·직각 여부·회전은 검사하지 않는다
  (줌은 §5 의 Extents 기준이므로 형상과 무관)
- 대상 공간은 **모델 공간**만. 페이퍼 공간 Poly 는 선택되어도 거부한다

### 3.2 이름 규칙

| 항목 | 규칙 |
|---|---|
| 유일성 | **도면 내 유일.** 중복 입력 시 거부하고 재입력 요구 |
| 길이 | 1 ~ 60자 |
| 허용 문자 | 제한 없음 (한글·공백 포함). 앞뒤 공백은 trim |
| 대소문자 | 중복 검사 시 **구분하지 않음** |

### 3.3 내용(설명) 규칙

| 항목 | 규칙 |
|---|---|
| 길이 | **최대 100자** (XData 문자열 255바이트 제한 대응 — §10-A) |
| 줄바꿈 | XData 에는 `\n` 두 글자로 이스케이프해 저장, 읽을 때 복원 (`PolyViewStore.Escape`/`Unescape`). 노드 표시에서만 공백으로 눕힌다 |
| 빈 값 | 허용 (노드에 이름만 표시) |

---

## 4. `PLVM` 팔레트

### 4.1 구조

```
PaletteSet "PolyView Manager"   (Singleton, 고정 GUID)
 └ WinForms UserControl
    ├ ToolStrip : [새로고침] [새 그룹] [누락 정리] [모두 펼치기] [모두 접기]
    └ TreeView  : ShowNodeToolTips = true, AllowDrop = true, LabelEdit = true
```

- `PaletteSet` 구현은 기존 `PipeTreeViewPalette.cs` 패턴을 따른다
  (static 필드 Singleton, 고정 `Guid`, `PaletteSetStyles.ShowAutoHideButton | ShowCloseButton | Snappable`,
  `DockEnabled = Left|Right|Top|Bottom`)
- UI 는 **WinForms**(`LayerPalette.cs` 계열). WPF/ElementHost 는 쓰지 않는다
- 도킹·자동 숨기기 동작은 AutoCAD 내장 특성(`PROPERTIES`) 팔레트와 동일하다
  (좌/우 가장자리에 붙이고 압정을 누르면 제목만 세로로 남는 형태). 별도 구현 불필요
- 내용은 **TreeView 단일 구성**. 2단 분할(속성 그리드) 은 쓰지 않는다
- `PLVM` 재실행 시 새로 만들지 않고 `Visible = true` + `Activate(0)`

### 4.2 노드 표시

| 노드 | 표시 |
|---|---|
| Root | 도면 파일명 (확장자 포함) |
| Group | 그룹 이름 + ` (n)` — n = 하위 PolyView 총 개수 |
| PolyView (정상) | **`이름 - 내용`**. 내용이 없으면 이름만 |
| PolyView (누락) | `(누락)  <Handle>` — 회색 `SystemColors.GrayText` |

- **툴팁(마우스 오버)은 쓰지 않는다.** `ShowNodeToolTips = false`.
  내용을 노드에 직접 붙여 보이므로 툴팁이 중복이다
- 내용에 줄바꿈이 있으면 표시할 때만 공백으로 눕힌다 (저장값은 그대로)

### 4.3 트리 구성 순서 (새로고침 시 매번)

1. 모델 공간을 스캔해 `PolyView` XData 를 가진 Polyline 을 모두 수집 → `{Handle → (이름, 내용)}`
2. NOD `TreeJson` 을 읽어 그룹 트리를 만든다
3. 트리의 Handle 중 1의 목록에 없는 것 → **누락 항목**. 회색으로 표시하고 **자리는 그대로 유지**
4. 1의 Handle 중 트리 어디에도 없는 것 → **미편입 항목**. Root 직속 끝에 자동 추가하고 NOD 에 반영
5. 펼침 상태는 새로고침 전후로 보존한다 (그룹 경로 문자열 기준)

> 3번이 "회색 표시 후 수동 정리" 정책이다. AutoCAD Undo 로 Poly 가 되살아나면
> 다음 새로고침에서 자동으로 정상 복구된다. 자동 삭제하지 않는 이유가 이것이다.

### 4.4 조작

| 조작 | 동작 |
|---|---|
| **PolyView 노드 좌클릭** | 즉시 해당 Poly 영역으로 **줌** (§5). 누락 항목은 무시 |
| PolyView 노드 더블클릭 | 좌클릭과 동일 (중복 줌 방지) |
| Group 노드 좌클릭 | 펼침/접힘만. 줌 없음 |
| **드래그앤드롭** | PolyView·Group 노드를 다른 Group 또는 Root 안으로 이동. 그룹은 자기 자손 안으로 이동 불가 |
| **F2 / 느린 더블클릭 — Group** | 그룹 이름 인라인 편집 |
| **F2 / 느린 더블클릭 — PolyView** | 인라인 편집을 취소하고 **속성 대화상자**를 연다 (이름·내용을 함께 고쳐야 하므로) |

### 4.5 우클릭 메뉴

| 대상 | 메뉴 |
|---|---|
| Root / Group | `새 그룹`, `그룹 이름 변경`, `그룹 삭제`, `모두 펼치기`, `모두 접기` (Root 는 이름변경·삭제 없음) |
| PolyView | `속성 편집`, `도면에서 선택`, `확대`, `PolyView 삭제` |
| 누락 항목 | `이 항목 삭제 (누락)` |
| 빈 영역 | `새로고침`, `새 그룹`, `누락 정리` |

- **`새 그룹`** — 별도 입력 대화상자 없이 `새 그룹` (중복이면 `새 그룹 (2)`) 으로 만들고
  **곧바로 인라인 이름 편집 상태**로 둔다. 입력창을 하나 줄이려는 의도
- **`속성 편집`** — 작은 모달 폼에서 이름·내용을 **함께** 수정 → 확인 시 즉시 Poly XData 에 기록.
  PolyView 의 이름·내용을 고치는 경로는 이 대화상자 하나뿐이다(인라인 편집은 쓰지 않는다).
  표시 텍스트가 `이름 - 내용` 이라 구분자 파싱이 필요해지는데, 이름이나 내용에 `-` 가 들어가면
  경계가 모호해지기 때문이다
- **`도면에서 선택`** — 줌 없이 해당 Poly 를 선택 상태(`ed.SetImpliedSelection`)로 만든다
- **`그룹 삭제`** — 확인 후 그룹만 제거하고, **하위 PolyView·하위 그룹은 Root 직속으로 올린다**
  (데이터 손실 없음. 하위가 비어 있으면 확인 없이 삭제)
- **`누락 정리`** — 누락 항목 개수를 보여주고 확인받은 뒤 트리에서 일괄 제거
- **`PolyView 삭제`** (2026-09-23 추가) — 확인 후 **등록만 해제**한다. Poly 의 XData
  `PolyView`/`PolyViewMemo` 를 지우고(`PolyViewStore.RemoveXData`) 트리에서 Handle 을 뺀 뒤 저장.
  **Poly 엔티티는 도면에 남고 색상(ACI 61)도 원복하지 않는다.**
  XData 를 먼저 지워야 한다 — 남아 있으면 다음 새로고침에서 미편입으로 Root 에 다시 붙는다
- **`이 항목 삭제 (누락)`** — 누락 항목을 트리에서만 제거

### 4.6 도면 전환

- `Application.DocumentManager.DocumentActivated` 를 후킹해 활성 도면의 트리로 **자동 교체**
- `DocumentToBeDestroyed` 시 트리를 비운다 (다음 `DocumentActivated` 에서 새 도면 기준으로 재구성)
- 활성 도면이 없으면 트리를 비우고 "열린 도면 없음" 표시
- 이벤트 등록은 **팔레트를 처음 만들 때**(`PolyViewPalette.Create`) 한다.
  `MyPlugin.Initialize` 에 넣지 않은 이유: 팔레트를 한 번도 열지 않은 세션에서는 후킹 자체가 불필요하고,
  기존 시작 경로를 건드리지 않는 쪽이 안전하다

---

## 5. 줌 동작

| 항목 | 값 |
|---|---|
| 기준 | Poly 의 `GeometricExtents` |
| 여백 | 각 변 기준 **10%** 확대 |
| 대상 | 현재 활성 뷰포트 (모델 공간) |
| 구현 | 기존 `AcadFunction.ZoomExtensionMethods.ZoomToEntities(ids, padding)` 재사용 — `padding = 1.1` |
| 예외 | `db.TileMode == false`(페이퍼 공간)면 줌하지 않고 "모델 공간에서 실행하세요" 메시지 |

> 줌 코드는 새로 만들지 않았다. `CadFunction.cs` 의 `ZoomToEntities` 가 Extents 합산 + padding +
> `SetCurrentView` 를 이미 그대로 수행한다. 팔레트에서 호출하므로 `doc.LockDocument()` 로 감싼다.

- 줌은 **Undo 대상이 아니다.** Transaction 으로 감싸되 도면 DB 는 변경하지 않는다

---

## 6. Undo / 동기화

| 동작 | Undo 대상 | 팔레트 갱신 |
|---|---|---|
| `POLYVIEW` 등록·수정 (XData + NOD) | O — 한 Transaction 으로 묶음 | 즉시 |
| 그룹 생성/삭제/이름변경/노드 이동 (NOD) | O — 각각 한 Transaction | 즉시 |
| Poly 삭제 / Undo 복구 | O (AutoCAD 기본) | **자동 감지 안 함** → 새로고침 시 반영 |
| 줌 | X | — |

- Poly 삭제 실시간 감지(`Database.ObjectErased`)는 **초기 버전에 넣지 않는다.**
  새로고침 버튼 + 도면 전환 시 갱신으로 충분하며, 이벤트 후킹은 Undo 왕복에서 오작동하기 쉽다
- 사용자가 팔레트 밖에서 Undo 를 실행한 뒤 트리가 어긋나 보일 수 있으므로,
  **새로고침 버튼을 툴바 첫 자리**에 둔다

---

## 7. 파일 구성 (구현 완료 — 2026-09-23)

| 파일 | 역할 |
|---|---|
| `Acadv25JArch/PolyView/PolyViewItem.cs` | 데이터 모델 (`PolyViewItem`, `PolyViewGroup`) |
| `Acadv25JArch/PolyView/PolyViewStore.cs` | 모델 공간 스캔 + NOD `TreeJson` 읽기/쓰기/청크 분할 |
| `Acadv25JArch/PolyView/PolyViewCommand.cs` | `POLYVIEW`, `PLVM` 명령 진입점 |
| `Acadv25JArch/PolyView/PolyViewPalette.cs` | `PaletteSet` Singleton |
| `Acadv25JArch/PolyView/PolyViewControl.cs` (+ `.Designer.cs`) | WinForms TreeView UserControl |
| `Acadv25JArch/PolyView/PolyViewEditForm.cs` (+ `.Designer.cs`) | 이름·내용 편집 모달 |

> **Designer.cs 주의** — 이 프로젝트에서는 VS Designer 가 코드로 추가한 컨트롤 설정을
> 누락시킨 전례가 있다. 레이아웃 확정 후에는 Designer 재생성 금지, `Edit` 로 최소 변경만.

---

## 8. 검증 방법

| # | 단계 | 검증 |
|---|---|---|
| 1 | `PolyViewStore` 읽기/쓰기 | 그룹 3단계 + PolyView 5개 트리를 저장 → 도면 저장/재열기 → 동일 구조 복원 확인 |
| 2 | JSON 청크 분할 | 그룹명이 긴 트리(JSON 2,000자 초과)를 저장/복원해 값이 잘리지 않음 확인 |
| 3 | `POLYVIEW` 등록 | 닫힌 Poly 등록 → `GXD` 로 `PolyView`/`PolyViewMemo` XData 확인 + **색상이 61 로 바뀌었는지** 확인 |
| 4 | 형상 거부 | 열린 Polyline, Line, 페이퍼 공간 Poly 선택 시 각각 거부 메시지 확인 |
| 5 | 이름 중복 | 같은 이름 재등록 시 거부 후 재입력 확인 |
| 6 | 누락 표시 | 등록 후 Poly 삭제 → 새로고침 → 회색 `(누락)` 표시 → Undo → 새로고침 → 정상 복구 |
| 7 | 미편입 편입 | XData 있는 Poly 를 다른 도면에 복사 → `PLVM` → Root 밑에 자동 표시 확인 |
| 8 | 그룹 삭제 | 하위가 있는 그룹 삭제 → 하위 PolyView 가 Root 로 올라오고 사라지지 않음 확인 |
| 9 | 드래그 이동 | 노드 이동 후 도면 저장/재열기 → 위치 유지 확인. 그룹을 자기 자손에 드롭 시 거부 |
| 10 | 줌 | 도면 구석의 작은 Poly 클릭 → 화면 가득 + 10% 여백 확인 |
| 11 | 도면 전환 | 도면 2개에 각각 PolyView 등록 → 탭 전환 시 트리·Root 이름이 따라 바뀜 확인 |
| 12 | Undo | 그룹 생성 직후 `U` → 트리에서 그룹이 사라짐(새로고침 후) 확인 |

---

## 9. 범위 밖 (이번 버전에서 하지 않음)

- PolyView 이름/내용을 도면에 텍스트로 표시 (Overrule)
- 팔레트 내 검색·필터
- PolyView 목록 Excel / CSV 내보내기
- 레이아웃(페이퍼 공간) 뷰포트 자동 생성
- 리본 `JArch` 탭 버튼 추가
- 여러 도면의 Root 를 한 트리에 동시 표시
- Poly 삭제 실시간 감지

---

## 10. 미결 항목 — 확정 필요

| # | 항목 | 현재 초안 가정 | 확인 필요 사항 |
|---|---|---|---|
| **A** | 이름 도면 내 유일 | 중복 거부 후 재입력 | 서로 다른 그룹에는 같은 이름이 있어도 되게 할지 |
| **B** | 모델 공간 전용 | 페이퍼 공간 Poly 는 거부 | 레이아웃에 그린 Poly 도 등록 대상인지 |
| **C** | 명령 이름 | `POLYVIEW` / `PLVM` | 기존 명령과 충돌 없는지 실제 AutoCAD 에서 확인 필요 |
| **D** | 팔레트 제목·기본 크기 | "PolyView Manager", 300×500 | 선호하는 제목/도킹 위치가 있는지 |

---

## 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-09-23 | v0.1 초안 작성. 저장 방식(XData+NOD), Poly 확보(기존 선택만), 누락 처리(회색+수동 정리), 줌(단일 클릭), 그룹 삭제(하위 Root 로), 편집(팔레트 직접), 형상(닫힌 Polyline), 도면 전환(자동 갱신) 확정 |
| 2026-09-23 | UI 확정 — 도킹 가능 `PaletteSet` + TreeView 단일 구성(2단 속성 그리드 미채택). 내용 길이 **100자 확정**(XData 유지, NOD 분리 안 함) |
| 2026-09-23 | **구현 완료** (§7 6개 파일, 빌드 정상). 문서 대비 변경 3건 — 줌은 기존 `ZoomToEntities` 재사용, 도면 이벤트 후킹 위치는 팔레트 생성 시점, 새 그룹은 대화상자 없이 인라인 편집. **AutoCAD 실동작 검증(§8)은 미실시** |
| 2026-09-23 | 표시/편집 변경 — 노드에 **`이름 - 내용`** 을 함께 표시, **툴팁 제거**(`ShowNodeToolTips = false`), PolyView 의 이름·내용 편집은 **속성 대화상자로 일원화**(F2 도 대화상자를 연다). 그룹 이름만 인라인 편집 유지 |
| 2026-09-23 | PolyView 로 지정된 Poly 의 **색상을 61 로 강제**(`WriteXData` 에서 처리 → 등록·수정 모두 적용) |
