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
    /// Line 자체가 Node인 새로운 구조
    /// </summary>
    public class PipeLineNode
    {
        public Line LineEntity { get; set; }
        public Handle LineHandle { get; set; }
        public ObjectId LineId { get; set; }
        public List<PipeLineNode> ConnectedNodes { get; set; } = [];
        public double LoadValue { get; set; } = 0.0;  // Xdata에서 읽은 부하값
        public double TotalLoad { get; set; } = 0.0;   // 하위 누적 부하
        public bool IsVisited { get; set; } = false;

        public PipeLineNode(Line line, ObjectId lineId)
        {
            LineEntity = line;
            LineHandle = line.Handle;
            LineId = lineId;
        }

        /// <summary>
        /// Leaf Node 판단: 한쪽 끝점만 연결, 다른 쪽은 막혀있음
        /// 한 점에 여러 Line이 연결되어도 Boolean으로 판단
        /// </summary>
        public bool IsLeaf(double tolerance = 1e-6)
        {
            if (ConnectedNodes.Count == 0)
                return false;

            Point3d startPt = LineEntity.StartPoint;
            Point3d endPt = LineEntity.EndPoint;

            bool startConnected = false;
            bool endConnected = false;

            foreach (var otherNode in ConnectedNodes)
            {
                Point3d otherStart = otherNode.LineEntity.StartPoint;
                Point3d otherEnd = otherNode.LineEntity.EndPoint;

                // StartPoint가 연결되어 있는지
                if (!startConnected &&
                    (startPt.DistanceTo(otherStart) < tolerance ||
                     startPt.DistanceTo(otherEnd) < tolerance))
                {
                    startConnected = true;
                }

                // EndPoint가 연결되어 있는지
                if (!endConnected &&
                    (endPt.DistanceTo(otherStart) < tolerance ||
                     endPt.DistanceTo(otherEnd) < tolerance))
                {
                    endConnected = true;
                }
            }

            // 한쪽만 연결되면 Leaf (XOR)
            return startConnected != endConnected;
        }

        /// <summary>
        /// Root Node 판단 (여러 연결이 있는 시작점)
        /// </summary>
        public bool IsRoot() => ConnectedNodes.Count > 1;

        /// <summary>
        /// 표시용 텍스트 생성
        /// </summary>
        public string GetDisplayText()
        {
            return $"Line[{LineHandle}] - 부하: {TotalLoad:F2} - 연결: {ConnectedNodes.Count}개";
        }
    }

    /// <summary>
    /// Line 기반 배관 네트워크 Graph
    /// </summary>
    public class PipeLineGraph
    {
        public List<PipeLineNode> Nodes { get; set; } = [];
        private const double TOLERANCE = 1e-6;

        /// <summary>
        /// Handle로 Node 찾기
        /// </summary>
        public PipeLineNode FindNodeByHandle(Handle handle)
        {
            return Nodes.FirstOrDefault(n => n.LineHandle == handle);
        }

        /// <summary>
        /// ObjectId로 Node 찾기
        /// </summary>
        public PipeLineNode FindNodeByObjectId(ObjectId id)
        {
            return Nodes.FirstOrDefault(n => n.LineId == id);
        }

        /// <summary>
        /// 점에 가장 가까운 Line Node 찾기
        /// </summary>
        public PipeLineNode FindClosestNode(Point3d point)
        {
            PipeLineNode closestNode = null;
            double minDistance = double.MaxValue;

            foreach (var node in Nodes)
            {
                // Line에서 점까지의 최단 거리
                Point3d closestPoint = node.LineEntity.GetClosestPointTo(point, false);
                double distance = closestPoint.DistanceTo(point);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        /// <summary>
        /// Node 추가
        /// </summary>
        public PipeLineNode AddNode(Line line, ObjectId lineId)
        {
            var existingNode = FindNodeByHandle(line.Handle);
            if (existingNode != null)
                return existingNode;

            var newNode = new PipeLineNode(line, lineId);
            Nodes.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// 두 Line이 연결되어 있는지 확인 (끝점 일치 여부)
        /// </summary>
        public static bool AreConnected(Line line1, Line line2, double tolerance = TOLERANCE)
        {
            // 4가지 경우: StartPoint와 StartPoint, StartPoint와 EndPoint 등
            return line1.StartPoint.DistanceTo(line2.StartPoint) < tolerance ||
                   line1.StartPoint.DistanceTo(line2.EndPoint) < tolerance ||
                   line1.EndPoint.DistanceTo(line2.StartPoint) < tolerance ||
                   line1.EndPoint.DistanceTo(line2.EndPoint) < tolerance;
        }

        /// <summary>
        /// 모든 Node 간 연결 관계 구성
        /// </summary>
        public void BuildConnections()
        {
            // 각 Node 쌍에 대해 연결 여부 확인
            for (int i = 0; i < Nodes.Count; i++)
            {
                for (int j = i + 1; j < Nodes.Count; j++)
                {
                    var node1 = Nodes[i];
                    var node2 = Nodes[j];

                    if (AreConnected(node1.LineEntity, node2.LineEntity))
                    {
                        // 양방향 연결
                        if (!node1.ConnectedNodes.Contains(node2))
                            node1.ConnectedNodes.Add(node2);

                        if (!node2.ConnectedNodes.Contains(node1))
                            node2.ConnectedNodes.Add(node1);
                    }
                }
            }
        }
    }

    public class PipeLoadAnalysisV2Command
    {
        private const string XDATA_APP_NAME = "Dia"; // Xdata Application Name

        [CommandMethod("PIPELOAD2")]
        public void AnalyzePipeLoadV2()
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

                // 2단계: Graph 생성 (Line = Node)
                var graph = BuildPipeLineGraph(selectedLineIds, tr, ed);
                ed.WriteMessage($"\n{graph.Nodes.Count}개의 Line Node 생성됨.");

                // 3단계: 연결 관계 구성
                graph.BuildConnections();
                ed.WriteMessage($"\n연결 관계 구성 완료.");

                // 4단계: Root Node 선택
                var rootNode = SelectRootNode(graph, ed);
                if (rootNode == null)
                {
                    ed.WriteMessage("\nRoot Node 선택이 취소되었습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\nRoot Line 선택됨: Handle={rootNode.LineHandle}");

                // 5단계: Leaf Node에서 Xdata 읽기
                LoadXdataFromLeafNodes(graph, ed);

                // 6단계: Root에서 시작하여 부하 계산
                CalculateLoadFromRoot(rootNode, graph, ed);

                // 7단계: 주요 Node 부하 정보 출력
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
        /// Line Graph 구성 (Line 자체가 Node)
        /// </summary>
        private PipeLineGraph BuildPipeLineGraph(List<ObjectId> lineIds, Transaction tr, Editor ed)
        {
            var graph = new PipeLineGraph();

            // 각 Line을 Node로 추가
            foreach (var id in lineIds)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is Line line)
                {
                    graph.AddNode(line, id);
                }
            }

            return graph;
        }

        /// <summary>
        /// Root Node 선택 (사용자가 점을 클릭)
        /// </summary>
        private PipeLineNode SelectRootNode(PipeLineGraph graph, Editor ed)
        {
            var pointOpts = new PromptPointOptions("\nRoot Line 위치를 선택하세요: ")
            {
                AllowNone = false
            };

            var pointResult = ed.GetPoint(pointOpts);
            if (pointResult.Status != PromptStatus.OK)
                return null;

            Point3d selectedPoint = pointResult.Value;

            // 가장 가까운 Line Node 찾기
            return graph.FindClosestNode(selectedPoint);
        }

        /// <summary>
        /// Leaf Line에서 Xdata 읽기
        /// </summary>
        private void LoadXdataFromLeafNodes(PipeLineGraph graph, Editor ed)
        {
            int leafCount = 0;
            int xdataCount = 0;

            ed.WriteMessage("\n=== Leaf 판단 디버그 ===");

            foreach (var node in graph.Nodes)
            {
                // 디버깅: 연결 상태 표시 (Boolean)
                bool startConnected = false;
                bool endConnected = false;
                Point3d startPt = node.LineEntity.StartPoint;
                Point3d endPt = node.LineEntity.EndPoint;
                double tolerance = 1e-6;

                // 모든 연결된 Line 확인 (조기 종료 없이)
                foreach (var other in node.ConnectedNodes)
                {
                    Point3d otherStart = other.LineEntity.StartPoint;
                    Point3d otherEnd = other.LineEntity.EndPoint;

                    if (!startConnected && (startPt.DistanceTo(otherStart) < tolerance ||
                        startPt.DistanceTo(otherEnd) < tolerance))
                        startConnected = true;

                    if (!endConnected && (endPt.DistanceTo(otherStart) < tolerance ||
                        endPt.DistanceTo(otherEnd) < tolerance))
                        endConnected = true;
                }

                // Leaf 판단
                bool isLeaf = node.IsLeaf();

                // 모든 Line 정보 출력
                ed.WriteMessage($"\nLine[{node.LineHandle}]: Start={startConnected}, End={endConnected}, IsLeaf={isLeaf}");
                
                // 11C07B 특별 디버깅
                if (node.LineHandle.ToString() == "11C07B")
                {
                    ed.WriteMessage($"\n  >>> 11C07B 상세:");
                    ed.WriteMessage($"\n  >>> ConnectedNodes: {node.ConnectedNodes.Count}개");
                    ed.WriteMessage($"\n  >>> StartPt: ({startPt.X:F2}, {startPt.Y:F2}), EndPt: ({endPt.X:F2}, {endPt.Y:F2})");
                    int idx = 0;
                    foreach (var conn in node.ConnectedNodes)
                    {
                        Point3d cStart = conn.LineEntity.StartPoint;
                        Point3d cEnd = conn.LineEntity.EndPoint;
                        ed.WriteMessage($"\n  >>> Conn[{idx}] Line[{conn.LineHandle}]:");
                        ed.WriteMessage($"\n      StartPt: ({cStart.X:F2}, {cStart.Y:F2}), EndPt: ({cEnd.X:F2}, {cEnd.Y:F2})");
                        double d1 = startPt.DistanceTo(cStart);
                        double d2 = startPt.DistanceTo(cEnd);
                        double d3 = endPt.DistanceTo(cStart);
                        double d4 = endPt.DistanceTo(cEnd);
                        ed.WriteMessage($"\n      거리: MyStart-ConnStart={d1:F6}, MyStart-ConnEnd={d2:F6}");
                        ed.WriteMessage($"\n            MyEnd-ConnStart={d3:F6}, MyEnd-ConnEnd={d4:F6}");
                        idx++;
                    }
                }
                
                if (isLeaf)
                {
                    leafCount++;
                    
                    try
                    {
                        // Xdata에서 부하값 읽기
                        var loadValue = JXdata.GetXdata(node.LineEntity, XDATA_APP_NAME);

                        if (loadValue != null)
                        {
                            node.LoadValue = Convert.ToDouble(loadValue);
                            xdataCount++;
                            ed.WriteMessage($" -> Xdata={node.LoadValue}");
                        }
                        else
                        {
                            ed.WriteMessage($" -> Xdata없음");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($" -> Xdata읽기실패: {ex.Message}");
                    }
                }
            }

            ed.WriteMessage($"\n\n=== 총계 ===");
            ed.WriteMessage($"\n총 {leafCount}개의 Leaf Line 감지됨 (한쪽 끝점만 연결).");
            ed.WriteMessage($"\n총 {xdataCount}개의 Leaf Line에서 Xdata 부하 읽음.");
        }

        /// <summary>
        /// Root에서 시작하여 DFS로 부하 계산
        /// </summary>
        private void CalculateLoadFromRoot(PipeLineNode rootNode, PipeLineGraph graph, Editor ed)
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

            ed.WriteMessage($"\nRoot Line의 총 부하: {totalLoad}");
        }

        /// <summary>
        /// 재귀적으로 부하 계산 (DFS)
        /// </summary>
        private double CalculateLoadRecursive(PipeLineNode currentNode, PipeLineNode parentNode, Editor ed)
        {
            currentNode.IsVisited = true;
            double totalLoad = 0.0;

            // 연결된 모든 Node 탐색
            foreach (var nextNode in currentNode.ConnectedNodes)
            {
                // 부모 Node로 역행하지 않음
                if (nextNode == parentNode)
                    continue;

                // 이미 방문한 Node는 스킵 (순환 방지)
                if (nextNode.IsVisited)
                    continue;

                // 재귀 호출로 하위 부하 계산
                double childLoad = CalculateLoadRecursive(nextNode, currentNode, ed);
                totalLoad += childLoad;
            }

            // 이 Node가 물리적 Leaf인 경우 (ConnectedNodes.Count == 1)
            if (currentNode.IsLeaf())
            {
                // Leaf는 Xdata 부하값 사용
                currentNode.TotalLoad = currentNode.LoadValue;
                ed.WriteMessage($"\nLeaf Line[{currentNode.LineHandle}], 부하={currentNode.LoadValue}");
                return currentNode.LoadValue;
            }

            // 중간 Node는 하위 부하 합산
            currentNode.TotalLoad = totalLoad;
            ed.WriteMessage($"\nLine[{currentNode.LineHandle}], 누적 부하={totalLoad}");
            return totalLoad;
        }

        /// <summary>
        /// 주요 Node의 부하 정보 표시
        /// </summary>
        private void DisplayLoadInformation(PipeLineGraph graph, Editor ed)
        {
            ed.WriteMessage("\n\n=== 주요 Line 부하 정보 ===");

            // 연결이 3개 이상인 주요 분기점만 표시
            var majorNodes = graph.Nodes
                .Where(n => n.ConnectedNodes.Count >= 3)
                .OrderByDescending(n => n.TotalLoad);

            foreach (var node in majorNodes)
            {
                ed.WriteMessage($"\nLine[{node.LineHandle}]");
                ed.WriteMessage($"  연결 수: {node.ConnectedNodes.Count}");
                ed.WriteMessage($"  총 부하: {node.TotalLoad:F2}");
            }
        }

        /// <summary>
        /// PaletteSet으로 Tree 구조 표시 (V2)
        /// </summary>
        [CommandMethod("PIPELOAD_PALETTE2")]
        public void ShowPipeLoadPaletteV2()
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
                var graph = BuildPipeLineGraph(selectedLineIds, tr, ed);
                ed.WriteMessage($"\n{graph.Nodes.Count}개의 Line Node 생성됨.");

                // 3단계: 연결 관계 구성
                graph.BuildConnections();

                // 4단계: Root Node 선택
                var rootNode = SelectRootNode(graph, ed);
                if (rootNode == null)
                {
                    ed.WriteMessage("\nRoot Node 선택이 취소되었습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\nRoot Line 선택됨: Handle={rootNode.LineHandle}");

                // 5단계: Leaf Node에서 Xdata 읽기
                LoadXdataFromLeafNodes(graph, ed);

                // 6단계: Root에서 시작하여 부하 계산
                CalculateLoadFromRoot(rootNode, graph, ed);

                tr.Commit();

                // 7단계: PaletteSet 표시
                PipeTreeViewPaletteV2.ShowPalette(graph, rootNode);

                ed.WriteMessage("\n분석 완료! PaletteSet이 표시되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                ed.WriteMessage($"\n상세: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// PaletteSet 숨기기 (V2)
        /// </summary>
        [CommandMethod("PIPELOAD_HIDE2")]
        public void HidePipeLoadPaletteV2()
        {
            PipeTreeViewPaletteV2.HidePalette();

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            ed.WriteMessage("\nPaletteSet을 숨겼습니다.");
        }

        /// <summary>
        /// PaletteSet TreeView 모두 펼치기 (V2)
        /// </summary>
        [CommandMethod("PIPELOAD_EXPAND2")]
        public void ExpandAllPaletteNodesV2()
        {
            if (PipeTreeViewPaletteV2.IsVisible())
            {
                PipeTreeViewPaletteV2.ExpandAll();

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
        /// 디버깅: Line의 Handle을 중심점에 표시
        /// </summary>
        [CommandMethod("SHOW_LINE_HANDLES")]
        public void ShowLineHandles()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Line 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }

                using var tr = db.TransactionManager.StartTransaction();
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                int count = 0;
                foreach (var lineId in selectedLineIds)
                {
                    if (tr.GetObject(lineId, OpenMode.ForRead) is Line line)
                    {
                        // Line 중심점 계산
                        Point3d midPoint = new Point3d(
                            (line.StartPoint.X + line.EndPoint.X) / 2.0,
                            (line.StartPoint.Y + line.EndPoint.Y) / 2.0,
                            (line.StartPoint.Z + line.EndPoint.Z) / 2.0
                        );

                        // DBText 생성
                        using DBText text = new DBText();
                        text.Position = midPoint;
                        text.Height = 100.0;  // 텍스트 크기
                        text.TextString = line.Handle.ToString();
                        text.ColorIndex = 1;  // 빨간색

                        btr.AppendEntity(text);
                        tr.AddNewlyCreatedDBObject(text, true);
                        count++;
                    }
                }

                tr.Commit();
                ed.WriteMessage($"\n{count}개 Line의 Handle을 표시했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// PaletteSet TreeView 모두 접기 (V2)
        /// </summary>
        [CommandMethod("PIPELOAD_COLLAPSE2")]
        public void CollapseAllPaletteNodesV2()
        {
            if (PipeTreeViewPaletteV2.IsVisible())
            {
                PipeTreeViewPaletteV2.CollapseAll();

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
}
