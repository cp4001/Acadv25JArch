using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PipeLoad2
{
    /// <summary>
    /// FCU 배관 네트워크 Tree 분석 — Leaf Line 끝에 연결된 Block(FCU)까지 추적
    /// Block의 LPM XData를 부하로 집계 → H-W 공식으로 관경 결정 → XData "Dia"에 저장
    /// 계산 로직: C:\Users\junhoi\Desktop\Work\Revit24\FCD_Dia_Calc\FCU_Calc.md
    /// </summary>
    public class FcuLineTreeBuilder
    {
        private const double TOLERANCE = 1.0;  // 단위: mm — AutoCAD 작도 오차 허용

        public enum FcuNodeType { Root, Mid, Leaf, Block }

        /// <summary>계산 모드: Supply=냉온수(H-W, LPM), Drain=배수(FCU 수량 Lookup)</summary>
        public enum CalcMode { Supply, Drain }

        public class FcuNode
        {
            public Line? Line { get; set; }
            public BlockReference? Block { get; set; }
            public string Handle { get; set; } = "";
            public FcuNode? Parent { get; set; }
            public List<FcuNode> Children { get; set; } = new();
            public int Level { get; set; }
            public FcuNodeType Type { get; set; }
            public double Load { get; set; }        // LPM (Block) or 누적합 (Line)
            public int LeafCount { get; set; }      // 하위 Block 개수
            public string Diameter { get; set; } = "-"; // "25A" 등
            public double MaxFlow { get; set; }     // 선정 관경의 최대유량 (LPM)
            public double Velocity { get; set; }    // 해당 관경 유속 (m/s)
            public string BlockName { get; set; } = ""; // Block 노드 전용
        }

        // ---- H-W 공식 / 강관 데이터 (KS D 3507) ----
        public static readonly string[] PipeSizes =
        {
            "15A","20A","25A","32A","40A","50A",
            "65A","80A","100A","125A","150A","200A","250A","300A"
        };

        public static readonly double[] InnerDiameter =
        {
             16.4,  21.9,  27.5,  36.2,  42.1,  53.2,
             69.0,  81.0, 105.3, 130.1, 156.5, 204.6, 254.7, 304.5
        };

        public const double C_STEEL = 130.0;
        public static readonly int[] HeadOptions = { 10, 16, 20, 30 };
        public const double V_WARN = 2.0;

        /// <summary>H-W 공식으로 최대유량(LPM) 계산</summary>
        public static double CalcMaxFlow(double dP_mmAq, double innerDiaMm, double C = C_STEEL)
        {
            return Math.Pow(
                (dP_mmAq / 10000.0) * Math.Pow(C, 1.852) * Math.Pow(innerDiaMm, 4.871)
                / 6.174e5,
                1.0 / 1.852);
        }

        /// <summary>유속(m/s) 계산</summary>
        public static double CalcVelocity(double flowLpm, double innerDiaMm)
        {
            double area = Math.PI * Math.Pow(innerDiaMm / 1000.0, 2) / 4.0;
            return (flowLpm / 60000.0) / area;
        }

        /// <summary>유량 → 최소 호칭경 선정 (FCS/FCR 로직과 동일)</summary>
        public static (string size, double maxFlow, double velocity) SelectPipe(double flowLpm, int head)
        {
            if (flowLpm <= 0) return ("-", 0, 0);

            for (int i = 0; i < PipeSizes.Length; i++)
            {
                double d   = InnerDiameter[i];
                double lpm = CalcMaxFlow(head, d);
                if (flowLpm <= lpm)
                {
                    double vel = CalcVelocity(flowLpm, d);
                    return (PipeSizes[i], lpm, vel);
                }
            }
            // 초과: 최대경으로 반환 + 실제 유속
            double dMax   = InnerDiameter[^1];
            double lpmMax = CalcMaxFlow(head, dMax);
            double velMax = CalcVelocity(flowLpm, dMax);
            return (PipeSizes[^1] + "!", lpmMax, velMax);
        }

        /// <summary>FCD 배수관: FCU 수량 → 호칭경 (FCU_Calc.md §3)</summary>
        public static string GetDrainPipe(int fcuCount)
        {
            if (fcuCount <= 0)  return "-";
            if (fcuCount <= 1)  return "20A";
            if (fcuCount <= 5)  return "25A";
            if (fcuCount <= 10) return "32A";
            if (fcuCount <= 20) return "40A";
            if (fcuCount <= 40) return "50A";
            return "65A";
        }

        // ---- 연결 그래프 ----
        private static bool Near(Point3d a, Point3d b) => a.DistanceTo(b) < TOLERANCE;

        private static bool LinesConnected(Line l1, Line l2) =>
            Near(l1.StartPoint, l2.StartPoint) || Near(l1.StartPoint, l2.EndPoint) ||
            Near(l1.EndPoint,   l2.StartPoint) || Near(l1.EndPoint,   l2.EndPoint);

        public Dictionary<string, List<string>> BuildLineConnections(List<Line> lines)
        {
            var graph = new Dictionary<string, List<string>>();
            foreach (var l in lines) graph[l.Handle.ToString()] = new List<string>();

            for (int i = 0; i < lines.Count; i++)
                for (int j = i + 1; j < lines.Count; j++)
                    if (LinesConnected(lines[i], lines[j]))
                    {
                        string h1 = lines[i].Handle.ToString();
                        string h2 = lines[j].Handle.ToString();
                        graph[h1].Add(h2);
                        graph[h2].Add(h1);
                    }
            return graph;
        }

        /// <summary>BlockReference가 XData "LPM"을 가지는지 확인.</summary>
        public static bool HasLpm(BlockReference br)
        {
            try
            {
                return !string.IsNullOrEmpty(JXdata.GetXdata(br, "LPM"));
            }
            catch { return false; }
        }

        /// <summary>
        /// Root에서 BFS로 Tree 분석 → Leaf Line의 tp(타 Line과 연결 없는 끝점)를 구하고,
        /// 각 tp ±margin 윈도우에서 INSERT 필터로 SelectCrossingWindow → LPM Block만 매핑.
        /// Transaction 밖에서 호출.
        /// </summary>
        public static Dictionary<string, string> MapLeafTerminalsToLpmBlocks(
            Editor ed,
            string rootHandle,
            List<(string handle, Point3d s, Point3d e)> lineEndpoints,
            HashSet<string> lpmBlockHandles,
            double margin = 10.0)
        {
            var map   = new Dictionary<string, string>();
            var epMap = lineEndpoints.ToDictionary(x => x.handle, x => (x.s, x.e));

            // 1. handle 기반 연결 그래프
            var adj = new Dictionary<string, List<string>>();
            foreach (var le in lineEndpoints) adj[le.handle] = new List<string>();
            for (int i = 0; i < lineEndpoints.Count; i++)
                for (int j = i + 1; j < lineEndpoints.Count; j++)
                {
                    var a = lineEndpoints[i];
                    var b = lineEndpoints[j];
                    if (Near(a.s, b.s) || Near(a.s, b.e) || Near(a.e, b.s) || Near(a.e, b.e))
                    {
                        adj[a.handle].Add(b.handle);
                        adj[b.handle].Add(a.handle);
                    }
                }

            // 2. BFS from root → childCount로 leaf 판별
            if (!adj.ContainsKey(rootHandle))
            {
                ed.WriteMessage($"\n[MapLeaf] root handle이 endpoints에 없음: {rootHandle}");
                return map;
            }
            var visited    = new HashSet<string> { rootHandle };
            var queue      = new Queue<string>();
            var childCount = new Dictionary<string, int>();
            foreach (var h in adj.Keys) childCount[h] = 0;
            queue.Enqueue(rootHandle);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var n in adj[cur])
                {
                    if (!visited.Add(n)) continue;
                    childCount[cur]++;
                    queue.Enqueue(n);
                }
            }

            var leaves = visited.Where(h => childCount[h] == 0 && h != rootHandle).ToList();
            ed.WriteMessage($"\n[MapLeaf] 진입: tree nodes={visited.Count}, leaves={leaves.Count}, lpmBlocks={lpmBlockHandles.Count}, margin={margin}");

            // 3. 각 leaf의 tp(타 line과 연결 없는 끝점)에서 CrossingWindow
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT")
            });

            int leafIdx = 0;
            int winCallCount = 0;
            foreach (var leafHandle in leaves)
            {
                leafIdx++;
                var (s, e) = epMap[leafHandle];
                bool sConn = IsConnectedToOther(s, leafHandle, lineEndpoints);
                bool eConn = IsConnectedToOther(e, leafHandle, lineEndpoints);

                Point3d tp;
                if (!sConn && eConn)       tp = s;
                else if (sConn && !eConn)  tp = e;
                else
                {
                    ed.WriteMessage($"\n  [MapLeaf] Leaf#{leafIdx} h={leafHandle} skip (sConn={sConn}, eConn={eConn})");
                    continue;
                }

                ed.WriteMessage($"\n  [MapLeaf] Leaf#{leafIdx} h={leafHandle} tp=({tp.X:F1},{tp.Y:F1})");

                var p1 = new Point3d(tp.X - margin, tp.Y - margin, tp.Z);
                var p2 = new Point3d(tp.X + margin, tp.Y + margin, tp.Z);
                PromptSelectionResult psr;
                try
                {
                    psr = ed.SelectCrossingWindow(p1, p2, filter);
                    winCallCount++;
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n    [MapLeaf] EXCEPTION: {ex.Message}");
                    continue;
                }

                int hitCount = (psr.Status == PromptStatus.OK && psr.Value != null) ? psr.Value.Count : 0;
                ed.WriteMessage($"\n    [MapLeaf] status={psr.Status} hit={hitCount}");

                if (psr.Status != PromptStatus.OK) continue;

                foreach (var id in psr.Value.GetObjectIds())
                {
                    string h = id.Handle.ToString();
                    if (!lpmBlockHandles.Contains(h)) continue;
                    map[h] = leafHandle;
                }
            }

            ed.WriteMessage($"\n[MapLeaf] 종료: SelectCrossingWindow 호출={winCallCount}회, 매핑={map.Count}건");
            return map;
        }

        private static bool IsConnectedToOther(Point3d p, string selfHandle,
            List<(string handle, Point3d s, Point3d e)> lineEndpoints)
        {
            foreach (var (h, s, e) in lineEndpoints)
            {
                if (h == selfHandle) continue;
                if (Near(p, s) || Near(p, e)) return true;
            }
            return false;
        }

        /// <summary>BFS로 Tree 구성 + Leaf Line 하위에 Block 노드 부착. blockToLine은 사전 매핑 주입.</summary>
        public FcuNode BuildTree(Line rootLine, List<Line> allLines, List<BlockReference> allBlocks,
                                 Dictionary<string, string> blockToLine)
        {
            var lineMap     = allLines .ToDictionary(l  => l .Handle.ToString(), l  => l);
            var blockMap    = allBlocks.ToDictionary(b  => b .Handle.ToString(), b  => b);
            var connections = BuildLineConnections(allLines);

            var root = new FcuNode
            {
                Line   = rootLine,
                Handle = rootLine.Handle.ToString(),
                Level  = 0,
                Type   = FcuNodeType.Root
            };

            var visited = new HashSet<string> { root.Handle };
            var queue   = new Queue<FcuNode>();
            queue.Enqueue(root);
            var nodeMap = new Dictionary<string, FcuNode> { [root.Handle] = root };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!connections.TryGetValue(cur.Handle, out var adj)) continue;
                foreach (var h in adj)
                {
                    if (!visited.Add(h)) continue;
                    var child = new FcuNode
                    {
                        Line   = lineMap[h],
                        Handle = h,
                        Parent = cur,
                        Level  = cur.Level + 1
                    };
                    cur.Children.Add(child);
                    nodeMap[h] = child;
                    queue.Enqueue(child);
                }
            }

            // Leaf Line 식별(자식 없음) → 매칭된 Block들을 Block 노드로 부착
            foreach (var node in nodeMap.Values.ToList())
            {
                if (node.Children.Count > 0) continue;  // leaf line만 대상
                var blocksForThisLine = blockToLine
                    .Where(kv => kv.Value == node.Handle)
                    .Select(kv => blockMap[kv.Key]);
                foreach (var br in blocksForThisLine)
                {
                    var bn = new FcuNode
                    {
                        Block     = br,
                        Handle    = br.Handle.ToString(),
                        Parent    = node,
                        Level     = node.Level + 1,
                        Type      = FcuNodeType.Block,
                        BlockName = br.Name
                    };
                    node.Children.Add(bn);
                }
            }

            SetNodeTypes(root);
            return root;
        }

        private void SetNodeTypes(FcuNode node)
        {
            foreach (var c in node.Children) SetNodeTypes(c);

            if (node.Type == FcuNodeType.Block) return;   // 이미 설정됨
            if (node.Parent == null)             node.Type = FcuNodeType.Root;
            else if (node.Children.Count == 0)   node.Type = FcuNodeType.Leaf;
            else                                 node.Type = FcuNodeType.Mid;
        }

        // ---- 부하 집계 ----
        public void CalculateLoads(FcuNode node)
        {
            foreach (var c in node.Children) CalculateLoads(c);

            if (node.Type == FcuNodeType.Block)
            {
                node.Load      = GetBlockLpm(node.Block!);
                node.LeafCount = node.Load > 0 ? 1 : 0;
                return;
            }

            node.Load      = 0;
            node.LeafCount = 0;
            foreach (var c in node.Children)
            {
                node.Load      += c.Load;
                node.LeafCount += c.LeafCount;
            }
        }

        private static double GetBlockLpm(BlockReference br)
        {
            try
            {
                string? v = JXdata.GetXdata(br, "LPM");
                if (!string.IsNullOrEmpty(v) &&
                    double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    return d;
            }
            catch { }
            return 0.0;
        }

        // ---- 관경 결정 ----
        public void CalculateDiameters(FcuNode node, int head, CalcMode mode = CalcMode.Supply)
        {
            foreach (var c in node.Children) CalculateDiameters(c, head, mode);

            if (node.Type == FcuNodeType.Block)
            {
                node.Diameter = "-";
                node.MaxFlow  = 0;
                node.Velocity = 0;
                return;
            }

            if (mode == CalcMode.Drain)
            {
                node.Diameter = GetDrainPipe(node.LeafCount);
                node.MaxFlow  = 0;
                node.Velocity = 0;
            }
            else
            {
                var (size, maxFlow, vel) = SelectPipe(node.Load, head);
                node.Diameter = size;
                node.MaxFlow  = maxFlow;
                node.Velocity = vel;
            }
        }

        /// <summary>계산된 Diameter를 Line XData "Dia"에 저장</summary>
        public void ApplyDiameters(FcuNode node, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();
            tr.CheckRegName("Dia");
            ApplyDiaRecursive(node, tr, db);
            tr.Commit();
        }

        private void ApplyDiaRecursive(FcuNode node, Transaction tr, Database db)
        {
            if (node.Type != FcuNodeType.Block && !string.IsNullOrEmpty(node.Diameter) && node.Diameter != "-")
            {
                try
                {
                    var handle = new Handle(Convert.ToInt64(node.Handle, 16));
                    var id     = db.GetObjectId(false, handle, 0);
                    if (id != ObjectId.Null)
                    {
                        var line = tr.GetObject(id, OpenMode.ForWrite) as Line;
                        if (line != null)
                        {
                            // XData "Dia" 는 숫자 문자열만 기록 ("25A"/"25A!" → "25")
                            string diaVal = node.Diameter.Replace("A", "").Replace("!", "").Trim();
                            if (!string.IsNullOrEmpty(diaVal))
                                JXdata.SetXdata(line, "Dia", diaVal);
                        }
                    }
                }
                catch { }
            }
            foreach (var c in node.Children) ApplyDiaRecursive(c, tr, db);
        }

        // ---- 통계 ----
        public int CountNodes(FcuNode node)
        {
            int n = 1;
            foreach (var c in node.Children) n += CountNodes(c);
            return n;
        }

        public int CountLineNodes(FcuNode node)
        {
            int n = node.Type == FcuNodeType.Block ? 0 : 1;
            foreach (var c in node.Children) n += CountLineNodes(c);
            return n;
        }

        public int CountBlockNodes(FcuNode node)
        {
            int n = node.Type == FcuNodeType.Block ? 1 : 0;
            foreach (var c in node.Children) n += CountBlockNodes(c);
            return n;
        }

        public int CountLeafLines(FcuNode node)
        {
            int n = (node.Type == FcuNodeType.Leaf) ? 1 : 0;
            foreach (var c in node.Children) n += CountLeafLines(c);
            return n;
        }
    }
}
