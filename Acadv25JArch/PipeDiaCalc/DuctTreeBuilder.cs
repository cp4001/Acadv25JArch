using AcadFunction;
using Acadv25JArch;
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
    /// 덕트 네트워크 Tree 분석 — Leaf Line 끝에 연결된 Block(디퓨져)까지 추적.
    /// Block 의 CMH XData 를 부하로 집계 → 각 Line 노드에 누적값을 Total_CMH XData 로 저장.
    /// Duct Spec (사이즈/형상) 결정은 추후 추가 예정.
    /// </summary>
    public class DuctTreeBuilder
    {
        private const double TOLERANCE = 1.0;  // mm

        public enum DuctNodeType { Root, Mid, Leaf, Block }

        public class DuctNode
        {
            public Line? Line { get; set; }
            public BlockReference? Block { get; set; }
            public string Handle { get; set; } = "";
            public DuctNode? Parent { get; set; }
            public List<DuctNode> Children { get; set; } = new();
            public int Level { get; set; }
            public DuctNodeType Type { get; set; }
            public double Load { get; set; }            // CMH (Block) or 누적 (Line)
            public int LeafCount { get; set; }          // 하위 Block 개수
            public string BlockName { get; set; } = "";
            public string TreeType { get; set; } = "";  // BuildTree 스냅샷 — Apply 가 그대로 기록
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

        /// <summary>BlockReference 가 XData "CMH" 를 가지는지 확인.</summary>
        public static bool HasCmh(BlockReference br)
        {
            try
            {
                return !string.IsNullOrEmpty(JXdata.GetXdata(br, "CMH"));
            }
            catch { return false; }
        }

        /// <summary>
        /// Root 에서 BFS 로 Tree 분석 → Leaf Line 의 tp(타 Line 과 연결 없는 끝점)에서
        /// ±margin 사각으로 SelectCrossingWindow → CMH Block 만 매핑.
        /// Transaction 밖에서 호출.
        /// </summary>
        public static Dictionary<string, string> MapLeafTerminalsToCmhBlocks(
            Editor ed,
            string rootHandle,
            List<(string handle, Point3d s, Point3d e)> lineEndpoints,
            HashSet<string> cmhBlockHandles,
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

            // 2. BFS from root → childCount 로 leaf 판별
            if (!adj.ContainsKey(rootHandle))
            {
                ed.WriteMessage($"\n[MapLeaf] root handle 이 endpoints 에 없음: {rootHandle}");
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
            ed.WriteMessage($"\n[MapLeaf] 진입: tree nodes={visited.Count}, leaves={leaves.Count}, cmhBlocks={cmhBlockHandles.Count}, margin={margin}");

            // 3. 각 leaf 의 tp 에서 CrossingWindow
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
                    if (!cmhBlockHandles.Contains(h)) continue;
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

        /// <summary>BFS 로 Tree 구성 + Leaf Line 하위에 Block 노드 부착. blockToLine 은 사전 매핑 주입.</summary>
        public DuctNode BuildTree(Line rootLine, List<Line> allLines, List<BlockReference> allBlocks,
                                  Dictionary<string, string> blockToLine)
        {
            var lineMap     = allLines .ToDictionary(l => l.Handle.ToString(), l => l);
            var blockMap    = allBlocks.ToDictionary(b => b.Handle.ToString(), b => b);
            var connections = BuildLineConnections(allLines);

            var root = new DuctNode
            {
                Line   = rootLine,
                Handle = rootLine.Handle.ToString(),
                Level  = 0,
                Type   = DuctNodeType.Root
            };

            var visited = new HashSet<string> { root.Handle };
            var queue   = new Queue<DuctNode>();
            queue.Enqueue(root);
            var nodeMap = new Dictionary<string, DuctNode> { [root.Handle] = root };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!connections.TryGetValue(cur.Handle, out var adj)) continue;
                foreach (var h in adj)
                {
                    if (!visited.Add(h)) continue;
                    var child = new DuctNode
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

            // Block 부착 전에 Line 타입 분류 — Block 자식이 생기면 leaf 판정이 깨짐
            SetNodeTypes(root);

            // Tree 상태 스냅샷 — Apply 가 이 값을 그대로 "Tree" XData 에 기록
            foreach (var kv in nodeMap)
                kv.Value.TreeType = kv.Value.Type.ToString();

            // Leaf Line 에 Block 부착
            foreach (var node in nodeMap.Values.ToList())
            {
                if (node.Type != DuctNodeType.Leaf) continue;
                var blocksForThisLine = blockToLine
                    .Where(kv => kv.Value == node.Handle)
                    .Select(kv => blockMap[kv.Key]);
                foreach (var br in blocksForThisLine)
                {
                    var bn = new DuctNode
                    {
                        Block     = br,
                        Handle    = br.Handle.ToString(),
                        Parent    = node,
                        Level     = node.Level + 1,
                        Type      = DuctNodeType.Block,
                        BlockName = br.Name,
                        TreeType  = "Block"
                    };
                    node.Children.Add(bn);
                }
            }

            return root;
        }

        private void SetNodeTypes(DuctNode node)
        {
            foreach (var c in node.Children) SetNodeTypes(c);

            if (node.Type == DuctNodeType.Block) return;
            if (node.Parent == null)             node.Type = DuctNodeType.Root;
            else if (node.Children.Count == 0)   node.Type = DuctNodeType.Leaf;
            else                                 node.Type = DuctNodeType.Mid;
        }

        // ---- 부하 집계 (CMH 누적) ----
        public void CalculateLoads(DuctNode node)
        {
            foreach (var c in node.Children) CalculateLoads(c);

            if (node.Type == DuctNodeType.Block)
            {
                node.Load      = GetBlockCmh(node.Block!);
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

        private static double GetBlockCmh(BlockReference br)
        {
            try
            {
                string? v = JXdata.GetXdata(br, "CMH");
                if (!string.IsNullOrEmpty(v) &&
                    double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    return d;
            }
            catch { }
            return 0.0;
        }

        /// <summary>각 Line 노드에 "Total_CMH"(누적 부하), "Tree"(Root/Mid/Leaf) XData 를 저장.</summary>
        public void ApplyTotalCmh(DuctNode node, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();
            tr.CheckRegName("Tree");
            tr.CheckRegName("Total_CMH");
            ApplyRecursive(node, tr, db);
            tr.Commit();
        }

        private void ApplyRecursive(DuctNode node, Transaction tr, Database db)
        {
            if (node.Type != DuctNodeType.Block)
            {
                Line? line = null;
                try
                {
                    var handle = new Handle(Convert.ToInt64(node.Handle, 16));
                    var id     = db.GetObjectId(false, handle, 0);
                    if (id != ObjectId.Null)
                        line = tr.GetObject(id, OpenMode.ForWrite) as Line;
                }
                catch { }

                if (line != null)
                {
                    // "Tree" — BuildTree 시점의 스냅샷 (Total_CMH 실패와 독립)
                    if (!string.IsNullOrEmpty(node.TreeType))
                    {
                        try { JXdata.SetXdata(line, "Tree", node.TreeType); } catch { }
                    }

                    // "Total_CMH" — 누적 CMH 부하값
                    try
                    {
                        JXdata.SetXdata(line, "Total_CMH",
                            node.Load.ToString("F2", CultureInfo.InvariantCulture));
                    }
                    catch { }
                }
            }
            foreach (var c in node.Children) ApplyRecursive(c, tr, db);
        }

        // ---- 통계 ----
        public int CountLineNodes(DuctNode node)
        {
            int n = node.Type == DuctNodeType.Block ? 0 : 1;
            foreach (var c in node.Children) n += CountLineNodes(c);
            return n;
        }

        public int CountBlockNodes(DuctNode node)
        {
            int n = node.Type == DuctNodeType.Block ? 1 : 0;
            foreach (var c in node.Children) n += CountBlockNodes(c);
            return n;
        }
    }
}
