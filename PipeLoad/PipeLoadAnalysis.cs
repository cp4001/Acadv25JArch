using Autodesk.AutoCAD.ApplicationServices;
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
