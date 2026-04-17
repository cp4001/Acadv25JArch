using AcadFunction;
using Acadv25JArch;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace PipeLoad2
{
    public class PipeLoadAnalysis
    {
        /// <summary>
        /// 선택  line  "15A" Xdata를 설정하는 커맨드 
        /// </summary>
        [CommandMethod("PPL")] // pipe Load 
        public void Cmd_Line_Set15A()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    double val15A = 1.0;
                    // 1. Leaf Line 선택
                    List<Line> targets = JEntity.GetEntityByTpye<Line>("Leaf Line 를 선택 하세요?", JSelFilter.MakeFilterTypes("LINE"));
                    if (targets.Count() == 0) return;
                    //2 15A Load  값 입력 
                    PromptDoubleOptions opts = new PromptDoubleOptions("\n 15A 환산값 ? ");
                    opts.DefaultValue = 1;
                    opts.UseDefaultValue = true;
                    opts.AllowNegative = false;  // 음수 허용 여부
                    opts.AllowZero = false;      // 0 허용 여부

                    PromptDoubleResult result = ed.GetDouble(opts);

                    if (result.Status == PromptStatus.OK)
                    {
                        val15A = result.Value;
                    }

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("15A"); //LL(Line)
                    
                    ////Create layerfor Wall Center Line
                    //tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var ll in targets)
                    {
                        // 2. 폴리라인 객체 가져오기
                        //Polyline poly = tr.GetObject(polyResult.ObjectId, OpenMode.ForRead) as Polyline;
                        if (ll == null)
                        {
                            ed.WriteMessage("\n라인을 읽을 수 없습니다.");
                            continue;
                        }

                        //Set Xdata
                        ll.UpgradeOpen();
                        JXdata.SetXdata(ll, "15A", val15A.ToString());


                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }   
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
            public int Diameter { get; set; }           // 결정 관경(mm)

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
        public Dictionary<string, List<string>> BuildConnectionGraph(List<Line> lines)
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
        public LineNode BuildTreeStructureBFS(Line rootLine, List<Line> allLines, 
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
        public int CountNodes(LineNode node)
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
        public int CountLeafNodes(LineNode node)
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
            const double DEFAULT_LEAF_LOAD = 1.0;
            const double MIN_LEAF_LENGTH   = 300.0;

            try
            {
                string? val = AcadFunction.JXdata.GetXdata(line, "15A");
                if (val != null && double.TryParse(val, out double result))
                    return result; // 15A XData 있으면 길이 무관하게 인정

                // 15A XData 없고 길이 < 300 이면 끝단 미인정
                if (line.Length < MIN_LEAF_LENGTH)
                    return 0.0;
            }
            catch { }

            return DEFAULT_LEAF_LOAD;
        }

        /// <summary>
        /// Tree 전체의 부하를 재귀적으로 계산 (Post-order 순회)
        /// </summary>
        public void CalculateLoads(LineNode node)
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
        /// Tree 전체 노드에 관경 계산 후 XData "Dia" 에 저장 (Post-order)
        /// 15A 값 >= 3 → 대변기(세정밸브), < 3 → 일반기구
        /// </summary>
        public void CalculateDiameters(LineNode node, Database db)
        {
            if (node == null) return;
            CalcDiaRecursive(node);
        }

        private void CalcDiaRecursive(LineNode node)
        {
            if (node == null) return;
            foreach (var child in node.Children)
                CalcDiaRecursive(child);

            double toiletEquiv  = 0; double generalEquiv = 0;
            int    toiletCount  = 0; int    generalCount  = 0;
            CollectEquiv(node, ref toiletEquiv, ref toiletCount,
                               ref generalEquiv, ref generalCount);

            node.Diameter = SupplyDiaCalc.Calculate2(
                toiletCount, toiletEquiv, generalCount, generalEquiv);
        }

        /// <summary>계산된 Diameter를 XData "Dia" 에 저장</summary>
        public void ApplyDiameters(LineNode node, Database db)
        {
            if (node == null) return;
            using var tr = db.TransactionManager.StartTransaction();
            tr.CheckRegName("Dia");
            ApplyDiaRecursive(node, tr, db);
            tr.Commit();
        }

        private void ApplyDiaRecursive(LineNode node, Transaction tr, Database db)
        {
            if (node == null) return;
            try
            {
                var handle = new Handle(Convert.ToInt64(node.Handle, 16));
                var objId  = db.GetObjectId(false, handle, 0);
                if (objId != ObjectId.Null)
                {
                    var line = tr.GetObject(objId, OpenMode.ForWrite) as Line;
                    if (line != null)
                        AcadFunction.JXdata.SetXdata(line, "Dia", node.Diameter.ToString());
                }
            }
            catch { }
            foreach (var child in node.Children)
                ApplyDiaRecursive(child, tr, db);
        }

        /// <summary>해당 노드 하위 Leaf의 균등값 합산</summary>
        private void CollectEquiv(LineNode node,
            ref double toiletEquiv,  ref int toiletCount,
            ref double generalEquiv, ref int generalCount)
        {
            if (node.Type == NodeType.Leaf && node.Load > 0)
            {
                double load = node.Load;
                if (load >= 3.0) { toiletEquiv  += load; toiletCount++;  }
                else             { generalEquiv += load; generalCount++; }
            }
            foreach (var child in node.Children)
                CollectEquiv(child, ref toiletEquiv,  ref toiletCount,
                                    ref generalEquiv, ref generalCount);
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

                var rootLine = tr.GetObject(rootLineId, OpenMode.ForRead) as Line;
                if (rootLine == null)
                {
                    ed.WriteMessage("\nRoot Line을 로드할 수 없습니다.");
                    tr.Commit();
                    return;
                }

                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null && line.Layer == rootLine.Layer)
                    .ToList();

                ed.WriteMessage($"\n레이어 [{rootLine.Layer}] 필터 적용: {lines.Count}개 Line");

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
