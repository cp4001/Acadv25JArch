using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeLoadAnalysis
{
    /// <summary>
    /// 배관 네트워크의 Node (교차점 또는 끝점)
    /// </summary>
    public class PipeNode
    {
        public Point3d Position { get; set; }
        public List<PipeEdge> ConnectedEdges { get; set; } = [];
        public double TotalLoad { get; set; } = 0.0; // 이 Node 하위의 총 부하
        public int NodeId { get; set; }
        public bool IsVisited { get; set; } = false;

        public PipeNode(Point3d position, int id)
        {
            Position = position;
            NodeId = id;
        }

        /// <summary>
        /// Leaf Node인지 확인 (연결이 1개만 있는 끝점)
        /// </summary>
        public bool IsLeaf() => ConnectedEdges.Count == 1;

        /// <summary>
        /// Root Node인지 확인 (여러 개의 연결이 있는 시작점)
        /// </summary>
        public bool IsRoot() => ConnectedEdges.Count > 1;
    }

    /// <summary>
    /// 배관 네트워크의 Edge (Line)
    /// </summary>
    public class PipeEdge
    {
        public Line LineEntity { get; set; }
        public ObjectId LineId { get; set; }
        public PipeNode StartNode { get; set; }
        public PipeNode EndNode { get; set; }
        public double LoadValue { get; set; } = 0.0; // Xdata에서 읽은 부하값

        public PipeEdge(Line line, ObjectId lineId)
        {
            LineEntity = line;
            LineId = lineId;
        }

        /// <summary>
        /// 주어진 Node의 반대편 Node 반환
        /// </summary>
        public PipeNode GetOtherNode(PipeNode node)
        {
            if (node == StartNode) return EndNode;
            if (node == EndNode) return StartNode;
            return null;
        }
    }

    /// <summary>
    /// 배관 네트워크 Graph
    /// </summary>
    public class PipeGraph
    {
        public List<PipeNode> Nodes { get; set; } = [];
        public List<PipeEdge> Edges { get; set; } = [];
        private const double TOLERANCE = 1e-6;

        /// <summary>
        /// 위치로 Node 찾기 (tolerance 내에서)
        /// </summary>
        public PipeNode FindNodeByPosition(Point3d position)
        {
            return Nodes.FirstOrDefault(n => n.Position.DistanceTo(position) < TOLERANCE);
        }

        /// <summary>
        /// Node 추가 (중복 체크)
        /// </summary>
        public PipeNode AddNode(Point3d position)
        {
            var existingNode = FindNodeByPosition(position);
            if (existingNode != null)
                return existingNode;

            var newNode = new PipeNode(position, Nodes.Count);
            Nodes.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// Edge 추가 및 Node 연결
        /// </summary>
        public void AddEdge(PipeEdge edge)
        {
            Edges.Add(edge);
            
            // 양방향 연결
            if (!edge.StartNode.ConnectedEdges.Contains(edge))
                edge.StartNode.ConnectedEdges.Add(edge);
            
            if (!edge.EndNode.ConnectedEdges.Contains(edge))
                edge.EndNode.ConnectedEdges.Add(edge);
        }
    }

    public class PipeLoadAnalysisCommand
    {
        private const string XDATA_APP_NAME = "Dia"; // Xdata Application Name

        [CommandMethod("PIPELOAD")]
        public void AnalyzePipeLoad()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: Line 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }

                ed.WriteMessage($"\n{selectedLineIds.Count}개의 Line이 선택되었습니다.");

                using var tr = db.TransactionManager.StartTransaction();

                // 2단계: Graph 생성
                var graph = BuildPipeGraph(selectedLineIds, tr, ed);
                ed.WriteMessage($"\n{graph.Nodes.Count}개의 Node, {graph.Edges.Count}개의 Edge 생성됨.");

                // 3단계: Root Node 선택
                var rootNode = SelectRootNode(graph, ed, tr);
                if (rootNode == null)
                {
                    ed.WriteMessage("\nRoot Node 선택이 취소되었습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\nRoot Node 선택됨: ID={rootNode.NodeId}, 위치=({rootNode.Position.X:F2}, {rootNode.Position.Y:F2})");

                // 4단계: Leaf Node에서 Xdata 읽기
                LoadXdataFromLeafNodes(graph, tr, ed);

                // 5단계: Root에서 시작하여 부하 계산
                CalculateLoadFromRoot(rootNode, graph, ed);

                // 6단계: 주요 Node 부하 정보 출력
                DisplayLoadInformation(graph, ed);

                tr.Commit();

                ed.WriteMessage("\n분석 완료!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 선택
        /// </summary>
        private List<ObjectId> SelectLines(Editor ed)
        {
            var lineIds = new List<ObjectId>();

            TypedValue[] filterList = [new TypedValue((int)DxfCode.Start, "LINE")];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n배관 Line들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }

        /// <summary>
        /// Pipe Graph 구성
        /// </summary>
        private PipeGraph BuildPipeGraph(List<ObjectId> lineIds, Transaction tr, Editor ed)
        {
            var graph = new PipeGraph();
            var lines = new List<(Line line, ObjectId id)>();

            // Line 객체 로드
            foreach (var id in lineIds)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Line line)
                {
                    lines.Add((line, id));
                }
            }

            // 교차점 찾기 및 Node 생성
            var intersectionPoints = new HashSet<Point3d>(new Point3dComparer());

            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var points = new Point3dCollection();
                    lines[i].line.IntersectWith(
                        lines[j].line,
                        Intersect.ExtendBoth,
                        points,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    foreach (Point3d pt in points)
                    {
                        intersectionPoints.Add(pt);
                        graph.AddNode(pt);
                    }
                }
            }

            // 각 Line의 끝점도 Node로 추가
            foreach (var (line, id) in lines)
            {
                graph.AddNode(line.StartPoint);
                graph.AddNode(line.EndPoint);
            }

            ed.WriteMessage($"\n총 {intersectionPoints.Count}개의 교차점 발견.");

            // Edge 생성 및 연결
            foreach (var (line, id) in lines)
            {
                var startNode = graph.FindNodeByPosition(line.StartPoint);
                var endNode = graph.FindNodeByPosition(line.EndPoint);

                if (startNode != null && endNode != null)
                {
                    var edge = new PipeEdge(line, id)
                    {
                        StartNode = startNode,
                        EndNode = endNode
                    };

                    graph.AddEdge(edge);
                }
            }

            return graph;
        }

        /// <summary>
        /// Root Node 선택 (사용자가 점을 클릭)
        /// </summary>
        private PipeNode SelectRootNode(PipeGraph graph, Editor ed, Transaction tr)
        {
            var pointOpts = new PromptPointOptions("\nRoot 지점(시작점)을 선택하세요: ")
            {
                AllowNone = false
            };

            var pointResult = ed.GetPoint(pointOpts);
            if (pointResult.Status != PromptStatus.OK)
                return null;

            Point3d selectedPoint = pointResult.Value;

            // 가장 가까운 Node 찾기
            PipeNode closestNode = null;
            double minDistance = double.MaxValue;

            foreach (var node in graph.Nodes)
            {
                double distance = node.Position.DistanceTo(selectedPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        /// <summary>
        /// Leaf Node의 Line에서 Xdata 읽기
        /// </summary>
        private void LoadXdataFromLeafNodes(PipeGraph graph, Transaction tr, Editor ed)
        {
            int loadCount = 0;

            foreach (var edge in graph.Edges)
            {
                // 이 Edge가 Leaf에 연결되어 있는지 확인
                bool isLeafEdge = edge.StartNode.IsLeaf() || edge.EndNode.IsLeaf();

                if (isLeafEdge)
                {
                    try
                    {
                        // Xdata에서 부하값 읽기
                        // 사용자 제공 함수: JXdata.GetXdata(Entity, "Dia")
                        var loadValue = JXdata.GetXdata(edge.LineEntity, XDATA_APP_NAME);
                        
                        if (loadValue != null)
                        {
                            edge.LoadValue = Convert.ToDouble(loadValue);
                            loadCount++;
                            ed.WriteMessage($"\nLeaf Edge에서 부하 읽음: {edge.LoadValue}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nXdata 읽기 실패: {ex.Message}");
                    }
                }
            }

            ed.WriteMessage($"\n총 {loadCount}개의 Leaf에서 부하 정보 읽음.");
        }

        /// <summary>
        /// Root에서 시작하여 DFS로 부하 계산
        /// </summary>
        private void CalculateLoadFromRoot(PipeNode rootNode, PipeGraph graph, Editor ed)
        {
            // 모든 Node의 방문 플래그 초기화
            foreach (var node in graph.Nodes)
            {
                node.IsVisited = false;
                node.TotalLoad = 0.0;
            }

            // DFS로 부하 계산
            double totalLoad = CalculateLoadRecursive(rootNode, null, ed);
            rootNode.TotalLoad = totalLoad;

            ed.WriteMessage($"\nRoot Node의 총 부하: {totalLoad}");
        }

        /// <summary>
        /// 재귀적으로 부하 계산 (DFS)
        /// </summary>
        private double CalculateLoadRecursive(PipeNode currentNode, PipeNode parentNode, Editor ed)
        {
            currentNode.IsVisited = true;
            double totalLoad = 0.0;

            // 연결된 모든 Edge 탐색
            foreach (var edge in currentNode.ConnectedEdges)
            {
                var nextNode = edge.GetOtherNode(currentNode);

                // 부모 Node로 역행하지 않음
                if (nextNode == parentNode || nextNode == null)
                    continue;

                // 이미 방문한 Node는 스킵 (순환 방지)
                if (nextNode.IsVisited)
                    continue;

                // Leaf Node인 경우
                if (nextNode.IsLeaf())
                {
                    double leafLoad = edge.LoadValue;
                    totalLoad += leafLoad;
                    nextNode.TotalLoad = leafLoad;

                    ed.WriteMessage($"\nLeaf Node ID={nextNode.NodeId}, 부하={leafLoad}");
                }
                else
                {
                    // 재귀 호출로 하위 부하 계산
                    double childLoad = CalculateLoadRecursive(nextNode, currentNode, ed);
                    nextNode.TotalLoad = childLoad;
                    totalLoad += childLoad;

                    ed.WriteMessage($"\nNode ID={nextNode.NodeId}, 누적 부하={childLoad}");
                }
            }

            return totalLoad;
        }

        /// <summary>
        /// 주요 Node의 부하 정보 표시
        /// </summary>
        private void DisplayLoadInformation(PipeGraph graph, Editor ed)
        {
            ed.WriteMessage("\n\n=== 주요 Node 부하 정보 ===");

            // 연결이 3개 이상인 주요 분기점만 표시
            var majorNodes = graph.Nodes
                .Where(n => n.ConnectedEdges.Count >= 3)
                .OrderByDescending(n => n.TotalLoad);

            foreach (var node in majorNodes)
            {
                ed.WriteMessage($"\nNode ID={node.NodeId}");
                ed.WriteMessage($"  위치: ({node.Position.X:F2}, {node.Position.Y:F2})");
                ed.WriteMessage($"  연결 수: {node.ConnectedEdges.Count}");
                ed.WriteMessage($"  총 부하: {node.TotalLoad:F2}");
            }
        }

        /// <summary>
        /// Node 위치에 부하 정보 Text 표시
        /// </summary>
        [CommandMethod("PIPELOAD_DISPLAY")]
        public void DisplayLoadAsText()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n이 기능은 Graph 분석 후 Text를 도면에 추가합니다.");
            ed.WriteMessage("\n(구현 예정: MText로 각 주요 Node에 부하 표시)");
        }

        /// <summary>
        /// PaletteSet으로 Tree 구조 표시
        /// </summary>
        [CommandMethod("PIPELOAD_PALETTE")]
        public void ShowPipeLoadPalette()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: Line 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }

                ed.WriteMessage($"\n{selectedLineIds.Count}개의 Line이 선택되었습니다.");

                using var tr = db.TransactionManager.StartTransaction();

                // 2단계: Graph 생성
                var graph = BuildPipeGraph(selectedLineIds, tr, ed);
                ed.WriteMessage($"\n{graph.Nodes.Count}개의 Node, {graph.Edges.Count}개의 Edge 생성됨.");

                // 3단계: Root Node 선택
                var rootNode = SelectRootNode(graph, ed, tr);
                if (rootNode == null)
                {
                    ed.WriteMessage("\nRoot Node 선택이 취소되었습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\nRoot Node 선택됨: ID={rootNode.NodeId}");

                // 4단계: Leaf Node에서 Xdata 읽기
                LoadXdataFromLeafNodes(graph, tr, ed);

                // 5단계: Root에서 시작하여 부하 계산
                CalculateLoadFromRoot(rootNode, graph, ed);

                tr.Commit();

                // 6단계: PaletteSet 표시
                PipeTreeViewPalette.ShowPalette(graph, rootNode);

                ed.WriteMessage("\n분석 완료! PaletteSet이 표시되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                ed.WriteMessage($"\n상세: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// PaletteSet 숨기기
        /// </summary>
        [CommandMethod("PIPELOAD_HIDE")]
        public void HidePipeLoadPalette()
        {
            PipeTreeViewPalette.HidePalette();
            
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ed.WriteMessage("\nPaletteSet을 숨겼습니다.");
        }

        /// <summary>
        /// PaletteSet TreeView 모두 펼치기
        /// </summary>
        [CommandMethod("PIPELOAD_EXPAND")]
        public void ExpandAllPaletteNodes()
        {
            if (PipeTreeViewPalette.IsVisible())
            {
                PipeTreeViewPalette.ExpandAll();
                
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage("\n모든 Node를 펼쳤습니다.");
            }
            else
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage("\nPaletteSet이 표시되지 않았습니다.");
            }
        }

        /// <summary>
        /// PaletteSet TreeView 모두 접기
        /// </summary>
        [CommandMethod("PIPELOAD_COLLAPSE")]
        public void CollapseAllPaletteNodes()
        {
            if (PipeTreeViewPalette.IsVisible())
            {
                PipeTreeViewPalette.CollapseAll();
                
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage("\n모든 Node를 접었습니다.");
            }
            else
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage("\nPaletteSet이 표시되지 않았습니다.");
            }
        }
    }

    /// <summary>
    /// Point3d 비교를 위한 Comparer (HashSet에서 사용)
    /// </summary>
    public class Point3dComparer : IEqualityComparer<Point3d>
    {
        private const double TOLERANCE = 1e-6;

        public bool Equals(Point3d p1, Point3d p2)
        {
            return p1.DistanceTo(p2) < TOLERANCE;
        }

        public int GetHashCode(Point3d p)
        {
            // Tolerance를 고려한 해시코드 생성
            int xHash = ((int)(p.X / TOLERANCE)).GetHashCode();
            int yHash = ((int)(p.Y / TOLERANCE)).GetHashCode();
            int zHash = ((int)(p.Z / TOLERANCE)).GetHashCode();
            return xHash ^ yHash ^ zHash;
        }
    }

    /// <summary>
    /// Xdata 읽기 유틸리티 클래스 (사용자가 구현했다고 가정)
    /// </summary>
    public static class JXdata
    {
        /// <summary>
        /// Entity에서 Xdata 읽기
        /// </summary>
        public static object GetXdata(Entity entity, string appName)
        {
            try
            {
                var rb = entity.GetXDataForApplication(appName);
                if (rb == null) return null;

                // ResultBuffer에서 값 추출
                foreach (TypedValue tv in rb)
                {
                    // DxfCode.ExtendedDataReal 또는 다른 타입으로 저장되어 있을 수 있음
                    if (tv.TypeCode == (int)DxfCode.ExtendedDataReal || 
                        tv.TypeCode == (int)DxfCode.ExtendedDataInteger16 ||
                        tv.TypeCode == (int)DxfCode.ExtendedDataInteger32)
                    {
                        rb.Dispose();
                        return tv.Value;
                    }
                }

                rb.Dispose();
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}

namespace PipeLoad2
{
    /// <summary>
    /// Line 네트워크의 Tree 구조를 분석하는 클래스
    /// </summary>
    public class LineTreeBuilder
    {
        private const double TOLERANCE = 1e-6; // 점 일치 판단 허용 오차
        private const string COMMAND_NAME = "LINETREE";

        /// <summary>
        /// Tree 노드 클래스
        /// </summary>
        public class LineNode
        {
            public Line Line { get; set; }              // Line 엔티티
            public string Handle { get; set; }          // Handle (고유 ID)
            public LineNode Parent { get; set; }        // 부모 노드
            public List<LineNode> Children { get; set; } // 자식 노드들
            public int Level { get; set; }              // 트리 깊이 (Root=0)
            public NodeType Type { get; set; }          // Root/Mid/Leaf
            public double Load { get; set; }            // 누적 부하값
            public int LeafCount { get; set; }          // 하위 Leaf 개수

            public LineNode()
            {
                Children = [];
                Level = 0;
                Type = NodeType.Leaf;
                Load = 0.0;
                LeafCount = 0;
            }
        }

        /// <summary>
        /// 노드 타입 열거형
        /// </summary>
        public enum NodeType
        {
            Root,    // 최상위 노드 (부모 없음)
            Mid,     // 중간 노드 (부모 있고 자식 있음)
            Leaf     // 말단 노드 (자식 없음)
        }

        /// <summary>
        /// Line Tree 구조 분석 메인 커맨드
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void BuildLineTree()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: Line들 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }

                // 2단계: Root Line 선택
                var rootLineId = SelectRootLine(ed);
                if (rootLineId == ObjectId.Null)
                {
                    ed.WriteMessage("\nRoot Line이 선택되지 않았습니다.");
                    return;
                }

                // 3단계: Line 객체들 로드
                using var tr = db.TransactionManager.StartTransaction();

                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                var rootLine = tr.GetObject(rootLineId, OpenMode.ForRead) as Line;
                if (rootLine == null)
                {
                    ed.WriteMessage("\nRoot Line을 로드할 수 없습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\n{lines.Count}개의 Line이 선택되었습니다.");
                ed.WriteMessage($"\nRoot Line Handle: {rootLine.Handle}");

                // 4단계: 연결 관계 구성
                var connections = BuildConnectionGraph(lines);
                ed.WriteMessage("\n연결 관계 구성 완료.");

                // 5단계: BFS로 Tree 구조 생성
                var rootNode = BuildTreeStructureBFS(rootLine, lines, connections);
                ed.WriteMessage($"\nTree 구조 생성 완료.");

                // 6단계: 통계 계산
                int totalNodes = CountNodes(rootNode);
                int leafCount = CountLeafNodes(rootNode);
                int midCount = totalNodes - leafCount - 1; // Root 제외

                ed.WriteMessage($"\n\n=== Tree 구조 통계 ===");
                ed.WriteMessage($"\n총 노드 수: {totalNodes}");
                ed.WriteMessage($"\nRoot: 1");
                ed.WriteMessage($"\nMid: {midCount}");
                ed.WriteMessage($"\nLeaf: {leafCount}");
                ed.WriteMessage($"\n최대 깊이: {GetMaxDepth(rootNode)}");

                // 7단계: 부하 계산
                CalculateLoads(rootNode);
                ed.WriteMessage($"\n부하 계산 완료.");
                ed.WriteMessage($"\n총 부하: {rootNode.Load:F2}");
                ed.WriteMessage($"\n총 Leaf 개수: {rootNode.LeafCount}");

                // 8단계: Tree 구조 출력
                ed.WriteMessage($"\n\n=== Tree 구조 (부하 포함) ===");
                PrintTreeStructure(rootNode, ed, "", true);

                tr.Commit();

                // 9단계: 색상 적용
                ApplyColorsToTree(rootNode, db);
                ed.WriteMessage("\n\n색상이 적용되었습니다. (Root=빨강, Mid=파랑, Leaf=녹색)");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 선택 메서드
        /// </summary>
        private List<ObjectId> SelectLines(Editor ed)
        {
            var lineIds = new List<ObjectId>();

            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE")
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nLine들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }

        /// <summary>
        /// Root Line 선택 메서드
        /// </summary>
        private ObjectId SelectRootLine(Editor ed)
        {
            var peo = new PromptEntityOptions("\nRoot Line을 선택하세요: ");
            peo.SetRejectMessage("\nLine 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            var per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                return per.ObjectId;
            }

            return ObjectId.Null;
        }

        /// <summary>
        /// 두 점이 연결되었는지 판단 (Tolerance 기반)
        /// </summary>
        private bool ArePointsConnected(Point3d p1, Point3d p2)
        {
            return p1.DistanceTo(p2) < TOLERANCE;
        }

        /// <summary>
        /// 두 Line이 연결되었는지 판단
        /// </summary>
        private bool AreLinesConnected(Line line1, Line line2)
        {
            // line1의 끝점과 line2의 끝점이 일치하는지 확인
            return ArePointsConnected(line1.StartPoint, line2.StartPoint) ||
                   ArePointsConnected(line1.StartPoint, line2.EndPoint) ||
                   ArePointsConnected(line1.EndPoint, line2.StartPoint) ||
                   ArePointsConnected(line1.EndPoint, line2.EndPoint);
        }

        /// <summary>
        /// 연결 관계 그래프 구성 (Handle 기반)
        /// </summary>
        private Dictionary<string, List<string>> BuildConnectionGraph(List<Line> lines)
        {
            var graph = new Dictionary<string, List<string>>();

            // 각 Line의 Handle을 키로 초기화
            foreach (var line in lines)
            {
                graph[line.Handle.ToString()] = [];
            }

            // 모든 Line 쌍에 대해 연결 관계 확인
            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (AreLinesConnected(lines[i], lines[j]))
                    {
                        string handle1 = lines[i].Handle.ToString();
                        string handle2 = lines[j].Handle.ToString();

                        graph[handle1].Add(handle2);
                        graph[handle2].Add(handle1);
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// BFS로 Tree 구조 생성
        /// </summary>
        private LineNode BuildTreeStructureBFS(Line rootLine, List<Line> allLines,
            Dictionary<string, List<string>> connections)
        {
            // Root 노드 생성
            var rootNode = new LineNode
            {
                Line = rootLine,
                Handle = rootLine.Handle.ToString(),
                Parent = null,
                Level = 0,
                Type = NodeType.Root
            };

            // 방문한 노드 추적 (Handle 기반)
            var visited = new HashSet<string> { rootNode.Handle };

            // BFS 큐
            var queue = new Queue<LineNode>();
            queue.Enqueue(rootNode);

            // Handle -> LineNode 매핑
            var nodeMap = new Dictionary<string, LineNode> { [rootNode.Handle] = rootNode };

            // Handle -> Line 매핑
            var lineMap = allLines.ToDictionary(line => line.Handle.ToString(), line => line);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                string currentHandle = currentNode.Handle;

                // 현재 노드와 연결된 모든 Handle 가져오기
                if (!connections.ContainsKey(currentHandle))
                    continue;

                foreach (string connectedHandle in connections[currentHandle])
                {
                    // 이미 방문한 노드는 건너뛰기
                    if (visited.Contains(connectedHandle))
                        continue;

                    visited.Add(connectedHandle);

                    // 자식 노드 생성
                    var childNode = new LineNode
                    {
                        Line = lineMap[connectedHandle],
                        Handle = connectedHandle,
                        Parent = currentNode,
                        Level = currentNode.Level + 1
                    };

                    // 부모-자식 관계 설정
                    currentNode.Children.Add(childNode);
                    nodeMap[connectedHandle] = childNode;

                    // 큐에 추가
                    queue.Enqueue(childNode);
                }
            }

            // NodeType 설정 (Post-order 방식)
            SetNodeTypes(rootNode);

            return rootNode;
        }

        /// <summary>
        /// 각 노드의 Type 설정 (재귀)
        /// </summary>
        private void SetNodeTypes(LineNode node)
        {
            if (node == null) return;

            // 자식들 먼저 처리
            foreach (var child in node.Children)
            {
                SetNodeTypes(child);
            }

            // Type 판단
            if (node.Parent == null)
            {
                node.Type = NodeType.Root;
            }
            else if (node.Children.Count == 0)
            {
                node.Type = NodeType.Leaf;
            }
            else
            {
                node.Type = NodeType.Mid;
            }
        }

        /// <summary>
        /// Tree 구조 콘솔 출력 (재귀)
        /// </summary>
        private void PrintTreeStructure(LineNode node, Editor ed, string indent, bool isLast)
        {
            if (node == null) return;

            // 노드 타입별 기호
            string typeSymbol = node.Type switch
            {
                NodeType.Root => "●",
                NodeType.Mid => "◆",
                NodeType.Leaf => "■",
                _ => "○"
            };

            // 트리 라인 그리기
            string prefix = isLast ? "└─" : "├─";
            if (node.Level == 0)
                prefix = "";

            // 부하 정보 포함
            string loadInfo = node.Type == NodeType.Leaf
                ? $"부하={node.Load:F2}"
                : $"누적부하={node.Load:F2}";

            ed.WriteMessage($"\n{indent}{prefix}{typeSymbol} Line[{node.Handle}] " +
                          $"(Level={node.Level}, 자식={node.Children.Count}, {loadInfo})");

            // 자식 노드들 출력
            string childIndent = indent + (isLast ? "    " : "│   ");
            if (node.Level == 0)
                childIndent = "";

            for (int i = 0; i < node.Children.Count; i++)
            {
                bool isLastChild = (i == node.Children.Count - 1);
                PrintTreeStructure(node.Children[i], ed, childIndent, isLastChild);
            }
        }

        /// <summary>
        /// 전체 노드 수 계산 (재귀)
        /// </summary>
        private int CountNodes(LineNode node)
        {
            if (node == null) return 0;

            int count = 1; // 현재 노드
            foreach (var child in node.Children)
            {
                count += CountNodes(child);
            }
            return count;
        }

        /// <summary>
        /// Leaf 노드 수 계산 (재귀)
        /// </summary>
        private int CountLeafNodes(LineNode node)
        {
            if (node == null) return 0;
            if (node.Children.Count == 0) return 1;

            int count = 0;
            foreach (var child in node.Children)
            {
                count += CountLeafNodes(child);
            }
            return count;
        }

        /// <summary>
        /// 최대 깊이 계산 (재귀)
        /// </summary>
        private int GetMaxDepth(LineNode node)
        {
            if (node == null) return -1;
            if (node.Children.Count == 0) return node.Level;

            int maxDepth = node.Level;
            foreach (var child in node.Children)
            {
                int childDepth = GetMaxDepth(child);
                if (childDepth > maxDepth)
                    maxDepth = childDepth;
            }
            return maxDepth;
        }

        /// <summary>
        /// Tree 전체에 색상 적용 (재귀)
        /// </summary>
        private void ApplyColorsToTree(LineNode node, Database db)
        {
            if (node == null) return;

            using var tr = db.TransactionManager.StartTransaction();
            ApplyColorRecursive(node, tr);
            tr.Commit();
        }

        /// <summary>
        /// 재귀적으로 색상 적용
        /// </summary>
        private void ApplyColorRecursive(LineNode node, Transaction tr)
        {
            if (node == null) return;

            try
            {
                // Handle로 ObjectId 찾기
                var handle = new Handle(Convert.ToInt64(node.Handle, 16));
                var objId = node.Line.Database.GetObjectId(false, handle, 0);

                if (objId != ObjectId.Null)
                {
                    var line = tr.GetObject(objId, OpenMode.ForWrite) as Line;
                    if (line != null)
                    {
                        // 노드 타입에 따라 색상 설정
                        Color color = node.Type switch
                        {
                            NodeType.Root => Color.FromColorIndex(ColorMethod.ByAci, 1),  // 빨강
                            NodeType.Mid => Color.FromColorIndex(ColorMethod.ByAci, 5),   // 파랑
                            NodeType.Leaf => Color.FromColorIndex(ColorMethod.ByAci, 3),  // 녹색
                            _ => Color.FromColorIndex(ColorMethod.ByAci, 7)               // 흰색
                        };

                        line.Color = color;
                    }
                }
            }
            catch (System.Exception)
            {
                // 개별 노드 색상 적용 실패 시 무시
            }

            // 자식 노드들 처리
            foreach (var child in node.Children)
            {
                ApplyColorRecursive(child, tr);
            }
        }

        /// <summary>
        /// Leaf Line의 부하값을 가져오는 메서드 (현재는 고정값, 추후 Xdata에서 읽기)
        /// </summary>
        private double GetLeafLoad(Line line)
        {
            // TODO: Xdata에서 "Dia" AppName으로 부하값 읽기
            // 현재는 모든 Leaf에 20 부하 적용
            const double DEFAULT_LEAF_LOAD = 20.0;

            /*
            // 추후 Xdata 읽기 구현 예정:
            try
            {
                ResultBuffer rb = line.GetXDataForApplication("Dia");
                if (rb != null)
                {
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.TypeCode == (int)DxfCode.Real)
                            return (double)tv.Value;
                    }
                    rb.Dispose();
                }
            }
            catch { }
            */

            return DEFAULT_LEAF_LOAD;
        }

        /// <summary>
        /// Tree 전체의 부하를 재귀적으로 계산 (Post-order 순회)
        /// </summary>
        private void CalculateLoads(LineNode node)
        {
            if (node == null) return;

            // 자식들 먼저 계산 (Post-order)
            foreach (var child in node.Children)
            {
                CalculateLoads(child);
            }

            // 현재 노드의 부하 계산
            if (node.Type == NodeType.Leaf)
            {
                // Leaf: 기본 부하값 적용 (추후 Xdata에서 읽기)
                node.Load = GetLeafLoad(node.Line);
                node.LeafCount = 1;
            }
            else
            {
                // Mid/Root: 모든 자식의 부하 합계
                node.Load = 0.0;
                node.LeafCount = 0;

                foreach (var child in node.Children)
                {
                    node.Load += child.Load;
                    node.LeafCount += child.LeafCount;
                }
            }
        }

        /// <summary>
        /// 부하 분석 결과 출력 커맨드
        /// </summary>
        [CommandMethod("LINETREE_LOADS")]
        public void ShowLoadAnalysis()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }

                var rootLineId = SelectRootLine(ed);
                if (rootLineId == ObjectId.Null)
                {
                    ed.WriteMessage("\nRoot Line이 선택되지 않았습니다.");
                    return;
                }

                using var tr = db.TransactionManager.StartTransaction();

                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                var rootLine = tr.GetObject(rootLineId, OpenMode.ForRead) as Line;
                if (rootLine == null)
                {
                    ed.WriteMessage("\nRoot Line을 로드할 수 없습니다.");
                    tr.Commit();
                    return;
                }

                var connections = BuildConnectionGraph(lines);
                var rootNode = BuildTreeStructureBFS(rootLine, lines, connections);
                CalculateLoads(rootNode);

                tr.Commit();

                // 부하 분석 결과 출력
                ed.WriteMessage($"\n\n=== 부하 분석 결과 ===");
                ed.WriteMessage($"\nRoot Line: {rootNode.Handle}");
                ed.WriteMessage($"\n총 부하: {rootNode.Load:F2}");
                ed.WriteMessage($"\n총 Leaf 개수: {rootNode.LeafCount}");
                ed.WriteMessage($"\nLeaf당 평균 부하: {(rootNode.LeafCount > 0 ? rootNode.Load / rootNode.LeafCount : 0):F2}");

                ed.WriteMessage($"\n\n=== 주요 노드별 부하 ===");
                PrintLoadsByLevel(rootNode, ed);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 레벨별 부하 정보 출력
        /// </summary>
        private void PrintLoadsByLevel(LineNode node, Editor ed)
        {
            if (node == null) return;

            // 현재 노드 정보
            string typeStr = node.Type switch
            {
                NodeType.Root => "Root",
                NodeType.Mid => "Mid",
                NodeType.Leaf => "Leaf",
                _ => "Unknown"
            };

            ed.WriteMessage($"\n[{typeStr}] Line[{node.Handle}]: 부하={node.Load:F2}, " +
                          $"Leaf개수={node.LeafCount}, Level={node.Level}");

            // 자식 노드들
            foreach (var child in node.Children)
            {
                PrintLoadsByLevel(child, ed);
            }
        }

        /// <summary>
        /// Tree 통계 출력 커맨드
        /// </summary>
        [CommandMethod("LINETREE_STATS")]
        public void ShowTreeStatistics()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n=== Line Tree 분석 도구 ===");
            ed.WriteMessage("\n명령어: LINETREE - Tree 구조 분석 및 색상 적용");
            ed.WriteMessage("\n명령어: LINETREE_LOADS - 부하 분석 상세 출력");
            ed.WriteMessage("\n명령어: LINETREE_STATS - 이 도움말 표시");
            ed.WriteMessage("\n\n색상 규칙:");
            ed.WriteMessage("\n  빨강(1) - Root Line");
            ed.WriteMessage("\n  파랑(5) - Mid Line (중간 노드)");
            ed.WriteMessage("\n  녹색(3) - Leaf Line (말단 노드)");
            ed.WriteMessage("\n\n부하 계산:");
            ed.WriteMessage("\n  Leaf Line: 기본 부하 20.0 (추후 Xdata에서 읽기)");
            ed.WriteMessage("\n  Mid/Root Line: 모든 하위 Leaf의 부하 합계");
        }
    }
}
