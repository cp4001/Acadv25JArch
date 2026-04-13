# LineTree 기술 노트
> 프로젝트: `Acadv25JArch` / 네임스페이스: `PipeLoad2`
> 최종 업데이트: 2026-04-10

---

## 1. 파일 구성

| 파일 | 역할 |
|------|------|
| `LineTreeBuilder.cs` | Tree 분석 핵심 로직 + AutoCAD 커맨드 (`LINETREE`, `LINETREE_LOADS`, `LINETREE_STATS`, `PPL`) |
| `LineTreeForm.cs` | WinForms Form 로직 (partial) |
| `LineTreeForm.Designer.cs` | WinForms 컨트롤 배치 (VS 디자이너 연동) |
| `LineTreeForm.resx` | 리소스 파일 |
| `LineTreeFormCommand.cs` | `LINETREE_FORM` 커맨드 진입점 |
| `SupplyDiaCalc.cs` | 급수배관 관경결정 알고리즘 (관균등표법) |
| `CadFunction.cs` | `JXdata` 클래스 — `GetXdata()` / `SetXdata()` / `CheckXdataRegName()` |
| `MyPlugIn.cs` | `MyPlugin` 클래스 — `LicenseDate` (SetXdata 내부 만료 체크에 사용됨) |

---

## 2. 등록된 AutoCAD 커맨드

| 커맨드 | 클래스 | 설명 |
|--------|--------|------|
| `LINETREE` | `LineTreeBuilder` | Tree 분석 + 색상 적용 |
| `LINETREE_LOADS` | `LineTreeBuilder` | 부하 분석 텍스트 출력 |
| `LINETREE_STATS` | `LineTreeBuilder` | 도움말 |
| `LINETREE_FORM` | `LineTreeFormCommand` | WinForms Tree 분석 폼 |
| `PPL` | `PipeLoadAnalysis` | Leaf Line에 15A XData 저장 |

---

## 3. 데이터 모델 — `LineNode`

```csharp
public class LineNode
{
    public Line     Line       { get; set; }  // AutoCAD Line 엔티티
    public string   Handle     { get; set; }  // Handle (16진수 문자열)
    public LineNode Parent     { get; set; }  // 부모 노드
    public List<LineNode> Children { get; set; }
    public int      Level      { get; set; }  // Root=0
    public NodeType Type       { get; set; }  // Root / Mid / Leaf
    public double   Load       { get; set; }  // 누적 부하값 (15A 기준)
    public int      LeafCount  { get; set; }  // 하위 Leaf 수
    public int      Diameter   { get; set; }  // 결정 관경 (mm)
}

public enum NodeType { Root, Mid, Leaf }
```

---

## 4. 핵심 처리 흐름 (`LINETREE_FORM`)

```
1. Line 다중 선택 (LINE 타입 필터)
2. Root Line 단일 선택
3. Transaction ForRead로 Line 로드
4. Root Line의 Layer 기준으로 필터링
5. BuildConnectionGraph()  → Handle 기반 인접 그래프 (O(n²))
6. BuildTreeStructureBFS() → BFS로 Tree 구조 생성
7. CalculateLoads()        → Post-order로 부하 누적
8. CalculateDiameters()    → 각 노드 관경 계산 (XData 저장 없음)
9. tr.Commit()             → 원본 Line 변경 없음
10. LineTreeForm 표시
```

---

## 5. XData 규격

| RegName | 타입 | 용도 | 기록 시점 |
|---------|------|------|-----------|
| `15A` | string (double) | Leaf Line 부하값 | `PPL` 커맨드 실행 시 |
| `DD` | string (int) | 결정 관경 (mm) | Form "관경 적용" 버튼 클릭 시 |

### 15A 읽기 로직 (`GetLeafLoad`)
```
- JXdata.GetXdata(line, "15A") 로 읽기
- 값 있으면 → double 변환 후 사용 (길이 무관)
- 값 없고 길이 < 300mm → 0.0 (끝단 미인정)
- 값 없고 길이 >= 300mm → 1.0 (기본값)
```

### DD 저장 로직 (`ApplyDiameters`)
```csharp
// WinForms 스레드에서 반드시 LockDocument 필요
using (doc.LockDocument())
{
    _builder.ApplyDiameters(_rootNode, _db);
}
// ApplyDiaRecursive에서 db.GetObjectId()로 ObjectId 획득
// node.Line.Database 사용 금지 (Transaction Commit 후 닫힌 객체)
```

---

## 6. 관경 결정 알고리즘 (`SupplyDiaCalc`)

출처: 건축 급배수·위생설비 (세진사) — 관균등표법

### 분류 기준
- Leaf `Load >= 3.0` → **대변기(세정밸브)** 그룹
- Leaf `Load < 3.0`  → **일반기구** 그룹

### Calculate2 호출
```csharp
int dia = SupplyDiaCalc.Calculate2(
    toiletCount,   // 대변기 수량
    toiletEquiv,   // 대변기 균등값 합계
    generalCount,  // 일반기구 수량
    generalEquivSum // 일반기구 균등값 합계
);
```

### 관경 테이블
| 관경(mm) | 균등값 상한 |
|:--------:|:-----------:|
| 15 | 1.0 | 20 | 2.6 | 25 | 4.9 | 32 | 9.2 |
| 40 | 14.5 | 50 | 30.0 | 65 | 53.0 | 80 | 84.6 |
| 100 | 178.0 | 125 | 280.0 | 150 | 452.0 |

---

## 7. LineTreeForm UI 구성

```
┌─────────────────────────────────────────────────┐
│ lblStats: 레이어 | 총노드 | Root/Mid/Leaf | 부하  │ ← DockStyle.Top
├─────────────────────────────────────────────────┤
│                                                 │
│  TreeView (Consolas 9.5pt)                      │ ← DockStyle.Fill
│  ● Root  [Handle] Lv=0  누적부하=xx  관경=xxmm  │   색상: Red
│  ◆ Mid   [Handle] Lv=1  누적부하=xx  관경=xxmm  │   색상: Blue
│  ■ Leaf  [Handle] Lv=2  부하=xx      관경=xxmm  │   색상: Green
│                                                 │
├─────────────────────────────────────────────────┤
│ [전체 펼치기] [전체 접기] [관경 적용] ... [닫기] │ ← DockStyle.Bottom
└─────────────────────────────────────────────────┘
```

### 버튼 동작
| 버튼 | 동작 |
|------|------|
| 전체 펼치기 | `treeView.ExpandAll()` |
| 전체 접기 | `treeView.CollapseAll()` |
| 관경 적용 | `doc.LockDocument()` → `ApplyDiameters()` → XData `"DD"` 저장 |
| 닫기 | `this.Close()` |

---

## 8. 주요 주의사항

### eLockViolation 방지
WinForms 버튼 클릭은 AutoCAD 외부 스레드 → ForWrite 접근 시 `eLockViolation` 발생
→ 반드시 `doc.LockDocument()` 사용

### node.Line 객체 사용 금지 (Commit 이후)
Transaction Commit 후 `node.Line` 은 닫힌 상태
→ `db.GetObjectId(false, handle, 0)` 으로 ObjectId 재획득

### Layer 필터
Root Line의 Layer와 동일한 Layer의 Line만 Tree 분석 대상

### SetXdata 라이선스 체크
`JXdata.SetXdata()` 내부에 라이선스 만료 체크 존재
→ `MyPlugin.LicenseDate` 가 현재일 이전이면 저장 불가

---

## 9. 추가 개발 포인트

- [ ] `LINETREE` 커맨드에도 Layer 필터 적용 (`LINETREE_LOADS` 와 동일하게)
- [ ] TreeView 노드 클릭 시 AutoCAD에서 해당 Line 하이라이트
- [ ] 관경별 색상 적용 (현재는 Root/Mid/Leaf 타입별 색상)
- [ ] 대량 Line 성능 개선 (현재 O(n²) → 공간 인덱싱)
- [ ] `GetLeafLoad` 에서 300mm 기준값 사용자 설정 가능하게
