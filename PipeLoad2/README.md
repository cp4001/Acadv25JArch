# PipeLoad2 - Line Tree 분석 도구

## 📋 프로젝트 개요

AutoCAD 도면에서 끝점으로 연결된 Line 네트워크를 분석하여 Tree 구조를 파악하고 시각화하는 도구입니다.

---

## 🎯 주요 기능

### 1. Tree 구조 분석
- 선택된 Line들의 연결 관계 자동 파악
- Root Line부터 시작하여 BFS(너비 우선 탐색)로 계층 구조 생성
- 각 노드를 Root/Mid/Leaf로 자동 분류

### 2. 시각적 피드백
- **색상 구분**:
  - 빨강(ACI 1): Root Line (최상위)
  - 파랑(ACI 5): Mid Line (중간 노드)
  - 녹색(ACI 3): Leaf Line (말단 노드)

### 3. 상세 통계
- 전체 노드 수
- Root/Mid/Leaf 개수
- 트리 최대 깊이
- 계층 구조 텍스트 출력

---

## 🔧 기술 스택

- **언어**: C# (.NET 8.0)
- **플랫폼**: AutoCAD 2025 .NET API
- **OS**: Windows 11
- **알고리즘**: BFS (너비 우선 탐색)

---

## 📁 프로젝트 구조

```
PipeLoad2/
├── LineTreeBuilder.cs      # 메인 로직 (Tree 분석 클래스)
├── PipeLoad2.csproj        # 프로젝트 파일 (.NET 8.0)
└── README.md               # 이 문서
```

---

## 🚀 사용 방법

### 1. 빌드
```bash
# Visual Studio에서 열기
# 또는 명령줄에서:
dotnet build PipeLoad2.csproj
```

### 2. AutoCAD에 로드
```
NETLOAD
→ PipeLoad2.dll 선택
```

### 3. 명령어 실행

#### LINETREE - Tree 구조 분석
```
명령: LINETREE
1. Line들을 선택하세요: [전체 Line 선택]
2. Root Line을 선택하세요: [시작점 Line 선택]
→ 자동으로 분석 및 색상 적용
```

#### LINETREE_STATS - 도움말 표시
```
명령: LINETREE_STATS
→ 사용 가능한 명령어 및 색상 규칙 표시
```

---

## 📊 출력 예시

### 콘솔 출력
```
12개의 Line이 선택되었습니다.
Root Line Handle: 11C08B
연결 관계 구성 완료.
Tree 구조 생성 완료.

=== Tree 구조 통계 ===
총 노드 수: 12
Root: 1
Mid: 4
Leaf: 7
최대 깊이: 3

=== Tree 구조 ===
● Line[11C08B] (Level=0, 자식=2)
├─◆ Line[11C08F] (Level=1, 자식=3)
│  ├─◆ Line[11C0AE] (Level=2, 자식=2)
│  │  ├─■ Line[11C0AF] (Level=3, 자식=0)
│  │  └─■ Line[11C0B0] (Level=3, 자식=0)
│  ├─■ Line[11C0B1] (Level=2, 자식=0)
│  └─■ Line[11C0B2] (Level=2, 자식=0)
└─◆ Line[11C073] (Level=1, 자식=2)
   ├─■ Line[11C074] (Level=2, 자식=0)
   └─■ Line[11C075] (Level=2, 자식=0)

색상이 적용되었습니다. (Root=빨강, Mid=파랑, Leaf=녹색)
```

---

## ⚙️ 핵심 알고리즘

### 1. 연결 판단 (Tolerance 기반)
```csharp
private const double TOLERANCE = 1e-6;

bool ArePointsConnected(Point3d p1, Point3d p2)
{
    return p1.DistanceTo(p2) < TOLERANCE;
}
```

### 2. BFS Tree 구성
```csharp
Queue<LineNode> queue = new Queue<LineNode>();
queue.Enqueue(rootNode);

while (queue.Count > 0)
{
    var current = queue.Dequeue();
    // 연결된 자식 노드들을 큐에 추가
    foreach (var connected in GetConnectedLines(current))
    {
        if (!visited.Contains(connected))
        {
            queue.Enqueue(connected);
        }
    }
}
```

### 3. NodeType 자동 분류
```csharp
NodeType type = parent == null ? NodeType.Root :
                children.Count == 0 ? NodeType.Leaf :
                NodeType.Mid;
```

---

## 🎨 데이터 구조

### LineNode 클래스
```csharp
public class LineNode
{
    public Line Line { get; set; }              // Line 엔티티
    public string Handle { get; set; }          // Handle (고유 ID)
    public LineNode Parent { get; set; }        // 부모 노드
    public List<LineNode> Children { get; set; } // 자식 노드들
    public int Level { get; set; }              // 트리 깊이
    public NodeType Type { get; set; }          // Root/Mid/Leaf
}
```

---

## 📝 제약사항

### 입력 요구사항
- Line 엔티티만 선택 가능
- Root Line은 선택한 Line 중 하나여야 함
- 연결되지 않은 Line은 Tree에 포함되지 않음

### 연결 판단 기준
- 두 Line의 끝점 거리가 1e-6 이내면 "연결됨"
- StartPoint ↔ StartPoint
- StartPoint ↔ EndPoint
- EndPoint ↔ StartPoint
- EndPoint ↔ EndPoint

---

## 🔄 향후 확장 계획

- [ ] WPF PaletteSet으로 TreeView UI 추가
- [ ] Xdata 읽기/쓰기 기능
- [ ] 부하 계산 기능
- [ ] Excel/CSV 내보내기
- [ ] 순환 구조 탐지

---

## 📌 참고사항

### AutoCAD API
- 모든 메서드는 AutoCAD 2025 공식 API 사용
- Transaction 관리로 안전한 데이터베이스 접근
- Handle 기반 엔티티 추적

### .NET 8.0 기능
- Collection expressions: `Children = []`
- Switch expressions
- using 선언
- var 타입 추론

---

**작성일**: 2025-12-27  
**프로젝트 위치**: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\PipeLoad2\`  
**개발자**: 준
