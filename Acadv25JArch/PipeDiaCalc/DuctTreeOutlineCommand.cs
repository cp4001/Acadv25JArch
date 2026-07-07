using AcadFunction;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Globalization;
using Node = PipeLoad2.DuctTreeBuilder.DuctNode;
using NodeType = PipeLoad2.DuctTreeBuilder.DuctNodeType;

namespace PipeLoad2
{
    /// <summary>
    /// DuctTree 분석(Apply 완료) 결과인 DuctNode 트리를 순회하며 각 접합 노드(자식 Line 1개 이상)의
    /// 위상(자식 개수·상대 방향·Leaf 여부)을 판정해 Duct_C1/Duct_C2/Duct_E/Duct_EE 중
    /// 적용할 패턴을 결정한다. 본 클래스는 판정만 수행하며 실제 외곽선 생성/Line 분할은 하지 않는다
    /// (실행은 각 명령의 TryApply 를 호출하는 별도 오케스트레이션 단계에서 수행).
    /// 설계 기준: 건축\Duct-OutLine\DuctTreeOutLine.md §4/§5 (v0.3).
    ///
    /// 주의: DuctNode.Line 은 DUCTTREE 분석 당시 커밋·종료된 옛 Transaction 에서 열린 참조이므로
    /// 그대로 재사용하면 eInvalidOpenState 오류가 난다(2026-07-07 실사용 중 확인).
    /// DuctTreeBuilder.ApplyRecursive 와 동일하게 node.Handle 로 db.GetObjectId → tr.GetObject 재획득한다.
    /// </summary>
    public class DuctTreeOutlineCommand
    {
        private const double PerpTol = 0.02;         // 직각/반대방향 판정 공용 (약 ±1.15°, 기존 Duct_C1/C2/E/EE 와 동일)
        private const double WidthEqualTol = 1e-3;   // 폭 동일 판정(직선 연속 케이스)
        private const double JunctionTol = 1.0;      // 접합점 일치 거리(mm) — DuctTreeBuilder.TOLERANCE 와 동일 값으로 통일

        public enum OutlinePattern { None, Duct_C1, Duct_C2, Duct_E, Duct_EE, Unsupported }

        /// <summary>노드 하나에 대한 위상 판정 결과. BranchA/BranchB 는 패턴별로 역할이 다르다
        /// (Duct_C2: BranchB=축소 자식(bb), BranchA=분기 자식(cc) / Duct_C1: BranchB/BranchA=좌우 자식(bb/cc, Handle 오름차순)
        /// / Duct_E·Duct_EE: BranchA=자식 1개). *Line 필드들은 판정 시점에 재획득한 Line 을 그대로 담아
        /// ApplyTree 가 재조회 없이 바로 사용할 수 있게 한다(동일 Transaction 안이라 유효).</summary>
        public class JunctionPlan
        {
            public Node Node { get; set; } = null!;
            public OutlinePattern Pattern { get; set; }
            public Node? BranchA { get; set; }
            public Node? BranchB { get; set; }
            public string Reason { get; set; } = "";

            public Line? NodeLine { get; set; }
            public Line? BranchALine { get; set; }
            public Line? BranchBLine { get; set; }
        }

        private enum Relation { CollinearOpposite, Perpendicular, Other }

        /// <summary>노드 하나에 대한 실제 적용 결과. Applied=false 이면 Message 에 스킵 사유
        /// (Unsupported/None 판정 사유, 또는 TryApply 가 반환한 [Exx] 검증 실패 메시지)가 담긴다.</summary>
        public class JunctionResult
        {
            public JunctionPlan Plan { get; set; } = null!;
            public bool Applied { get; set; }
            public string Message { get; set; } = "";
        }

        /// <summary>루트에서 시작해 전체 트리를 순회하며 lineChildren(Block 제외 Line 자식)이 1개 이상인
        /// 모든 노드의 판정 결과를 모아 반환한다. tr/db 로 각 노드의 Line 을 Handle 기준 재획득한다.</summary>
        public List<JunctionPlan> ClassifyTree(Transaction tr, Database db, Node root)
        {
            var plans = new List<JunctionPlan>();
            CollectPlans(tr, db, root, plans);
            return plans;
        }

        /// <summary>
        /// ClassifyTree 로 트리 전체를 먼저 판정(읽기 전용 스냅샷, §9)한 뒤, 판정된 패턴마다
        /// 해당 명령의 TryApply 를 호출해 외곽선을 일괄 생성한다. 노드 하나가 실패(TryApply
        /// 가 false 반환)해도 나머지 노드 처리는 계속하며(§9 "부분 실패가 전체를 막지 않음"),
        /// 하나의 Transaction 을 공유한다 — Commit 은 호출자 책임(Form 버튼 등에서 처리).
        /// Duct_E 를 포함한 모든 패턴을 트리 순회(ClassifyTree) 순서 그대로 적용한다(2026-07-08 확정).
        /// </summary>
        public List<JunctionResult> ApplyTree(Transaction tr, Database db, Node root)
        {
            // 읽기 전용 스냅샷 — 이 시점에서 전체 트리의 위상/역할 배정이 확정되며,
            // 이후 루프에서 발생하는 Line 분할/이동은 각 노드의 "근단"만 건드리므로
            // 아직 처리하지 않은 노드의 "원단" 기반 판정에는 영향을 주지 않는다(§9).
            var plans = ClassifyTree(tr, db, root);
            var results = new List<JunctionResult>(plans.Count);

            var c1 = new DuctC1Command();
            var c2 = new DuctC2Command();
            var elbow = new DuctElbowCommand();
            var endElbow = new DuctEndElbowCommand();

            foreach (var plan in plans)
                results.Add(ApplyPlan(tr, db, plan, c1, c2, elbow, endElbow));

            return results;
        }

        private JunctionResult ApplyPlan(Transaction tr, Database db, JunctionPlan plan,
            DuctC1Command c1, DuctC2Command c2, DuctElbowCommand elbow, DuctEndElbowCommand endElbow)
        {
            var result = new JunctionResult { Plan = plan };

            switch (plan.Pattern)
            {
                case OutlinePattern.None:
                case OutlinePattern.Unsupported:
                    result.Applied = false;
                    result.Message = plan.Reason;
                    break;

                case OutlinePattern.Duct_C1:
                    result.Applied = c1.TryApply(tr, db, plan.NodeLine!, plan.BranchBLine!, plan.BranchALine!, out string msgC1);
                    result.Message = msgC1;
                    break;

                case OutlinePattern.Duct_C2:
                    result.Applied = c2.TryApply(tr, db, plan.NodeLine!, plan.BranchBLine!, plan.BranchALine!, out string msgC2);
                    result.Message = msgC2;
                    break;

                case OutlinePattern.Duct_E:
                    result.Applied = elbow.TryApply(tr, db, plan.NodeLine!, plan.BranchALine!, out string msgE);
                    result.Message = msgE;
                    break;

                case OutlinePattern.Duct_EE:
                    result.Applied = endElbow.TryApply(tr, db, plan.NodeLine!, plan.BranchALine!, out string msgEE);
                    result.Message = msgEE;
                    break;
            }

            return result;
        }

        private void CollectPlans(Transaction tr, Database db, Node node, List<JunctionPlan> plans)
        {
            var lineChildren = node.Children.Where(c => c.Type != NodeType.Block).ToList();
            if (lineChildren.Count > 0)
                plans.Add(ClassifyNode(tr, db, node, lineChildren));

            foreach (var c in lineChildren) CollectPlans(tr, db, c, plans);
        }

        /// <summary>node.Handle 로 db.GetObjectId → tr.GetObject 재획득(DuctTreeBuilder.ApplyRecursive 와 동일 패턴).
        /// 옛 Transaction 에서 열린 DuctNode.Line 을 그대로 쓰면 eInvalidOpenState 가 나므로 반드시 이 경로를 거친다.</summary>
        private Line? GetLine(Transaction tr, Database db, Node node, OpenMode mode)
        {
            try
            {
                var handle = new Handle(System.Convert.ToInt64(node.Handle, 16));
                var id = db.GetObjectId(false, handle, 0);
                if (id == ObjectId.Null) return null;
                return tr.GetObject(id, mode) as Line;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>단일 노드의 위상을 DuctTreeOutLine.md §4 표 규칙대로 판정한다.</summary>
        private JunctionPlan ClassifyNode(Transaction tr, Database db, Node node, List<Node> lineChildren)
        {
            var plan = new JunctionPlan { Node = node };

            Line? nodeLine = GetLine(tr, db, node, OpenMode.ForRead);
            if (nodeLine == null)
            {
                plan.Pattern = OutlinePattern.Unsupported;
                plan.Reason = $"node Line[{node.Handle}] 을 재획득할 수 없습니다(핸들 조회 실패).";
                return plan;
            }
            plan.NodeLine = nodeLine;

            var childLines = new Dictionary<Node, Line>();
            foreach (var c in lineChildren)
            {
                var cl = GetLine(tr, db, c, OpenMode.ForRead);
                if (cl == null)
                {
                    plan.Pattern = OutlinePattern.Unsupported;
                    plan.Reason = $"자식 Line[{c.Handle}] 을 재획득할 수 없습니다(핸들 조회 실패).";
                    return plan;
                }
                childLines[c] = cl;
            }

            // 접합점 X = nodeLine 의 두 끝점 중 lineChildren 전부가 모이는 쪽 (§3)
            if (!TryFindJunction(nodeLine, lineChildren, childLines, out Point3d x, out Point3d nodeOther))
            {
                plan.Pattern = OutlinePattern.Unsupported;
                plan.Reason = "접합점 산출 실패 — lineChildren 이 node 의 한쪽 끝점에서 모이지 않습니다(복합 위상).";
                return plan;
            }

            // "outward" 관례(자식들의 dirChild = X→자식 먼 끝 과 동일 부호) — nodeOther 는 node 자신의
            // 먼 끝(접합점과 반대쪽)이므로 X→nodeOther 방향이어야 한다(2026-07-07 실사용 중 부호 오류 확인:
            // 반대로 계산하면 직각 판정은 내적 0이라 부호 무관하게 맞지만, 직선 연장(반대방향) 판정만
            // dot=-1 이어야 할 것이 dot=+1 로 뒤집혀 Duct_C2 후보가 전부 Unsupported 로 빠졌었다).
            Vector3d nodeDir = (nodeOther - x).GetNormal();

            if (lineChildren.Count == 1)
            {
                var child = lineChildren[0];
                ClassifySingleChild(nodeLine, nodeDir, x, child, childLines[child], plan);
            }
            else if (lineChildren.Count == 2)
            {
                var c1 = lineChildren[0];
                var c2 = lineChildren[1];
                ClassifyTwoChildren(nodeDir, x, c1, childLines[c1], c2, childLines[c2], plan);
            }
            else
            {
                plan.Pattern = OutlinePattern.Unsupported;
                plan.Reason = $"다중 분기({lineChildren.Count}개) — 4패턴에 없음, 수동 처리.";
            }

            return plan;
        }

        /// <summary>lineChildren 1개 케이스 — §4 표: 직선 연속 / Duct_EE / Duct_E / 미지원.</summary>
        private void ClassifySingleChild(Line nodeLine, Vector3d nodeDir, Point3d x, Node child, Line childLine, JunctionPlan plan)
        {
            var rel = Classify(nodeDir, x, childLine);

            if (rel == Relation.CollinearOpposite)
            {
                if (TryReadWidth(nodeLine, out double wNode) && TryReadWidth(childLine, out double wChild)
                    && System.Math.Abs(wNode - wChild) < WidthEqualTol)
                {
                    plan.Pattern = OutlinePattern.None;
                    plan.Reason = "직선 연속(폭 동일) — 외곽선 불필요.";
                }
                else
                {
                    plan.Pattern = OutlinePattern.Unsupported;
                    plan.Reason = "분기 없는 순수 리듀서(폭 상이 또는 폭 XData 없음) — 4패턴에 없음, 수동 처리.";
                }
                return;
            }

            if (rel == Relation.Perpendicular)
            {
                var grandChildren = child.Children.Where(c => c.Type != NodeType.Block).ToList();
                if (grandChildren.Count == 0)
                {
                    plan.Pattern = OutlinePattern.Duct_EE;
                    plan.BranchA = child;
                    plan.BranchALine = childLine;
                    plan.Reason = "Leaf 수직 분기 — Duct_EE 적용 대상(폭 무관).";
                }
                else
                {
                    plan.Pattern = OutlinePattern.Duct_E;
                    plan.BranchA = child;
                    plan.BranchALine = childLine;
                    plan.Reason = "직각 엘보(분기 없음) — Duct_E 후보(폭 불일치 시 TryApply 가 [E04] 로 스킵, §12 확정).";
                }
                return;
            }

            plan.Pattern = OutlinePattern.Unsupported;
            plan.Reason = "지원하지 않는 각도(직선 연장/직각 아님).";
        }

        /// <summary>lineChildren 2개 케이스 — §4 표: Duct_C2 / Duct_C1 / 미지원.</summary>
        private void ClassifyTwoChildren(Vector3d nodeDir, Point3d x, Node c1, Line c1Line, Node c2, Line c2Line, JunctionPlan plan)
        {
            var rel1 = Classify(nodeDir, x, c1Line);
            var rel2 = Classify(nodeDir, x, c2Line);

            if (rel1 == Relation.CollinearOpposite && rel2 == Relation.Perpendicular)
            {
                plan.Pattern = OutlinePattern.Duct_C2;
                plan.BranchB = c1; plan.BranchBLine = c1Line; // bb = 축소(직선) 자식
                plan.BranchA = c2; plan.BranchALine = c2Line; // cc = 분기(직각) 자식
                plan.Reason = "직선 축소(bb) + 직각 분기(cc) — Duct_C2 후보(W_node>W_bb 아니면 TryApply 가 [E04] 로 스킵).";
                return;
            }
            if (rel1 == Relation.Perpendicular && rel2 == Relation.CollinearOpposite)
            {
                plan.Pattern = OutlinePattern.Duct_C2;
                plan.BranchB = c2; plan.BranchBLine = c2Line;
                plan.BranchA = c1; plan.BranchALine = c1Line;
                plan.Reason = "직선 축소(bb) + 직각 분기(cc) — Duct_C2 후보(W_node>W_bb 아니면 TryApply 가 [E04] 로 스킵).";
                return;
            }

            if (rel1 == Relation.Perpendicular && rel2 == Relation.Perpendicular)
            {
                Vector3d dirC1 = (FarEnd(c1Line, x) - x).GetNormal();
                Vector3d dirC2 = (FarEnd(c2Line, x) - x).GetNormal();
                double dot = dirC1.DotProduct(dirC2);

                if (System.Math.Abs(dot + 1.0) < PerpTol)
                {
                    // Duct_C1 조건(사용자 확정, 2026-07-08): 기준 Line 끝점에서 양쪽으로 동일선상(반대측)
                    // 직각 분기 2개 — 단, 두 분기 모두 "Mid Duct"(더 아래로 이어짐, Leaf 아님)여야 한다.
                    bool c1IsMid = c1.Children.Where(cc => cc.Type != NodeType.Block).Any();
                    bool c2IsMid = c2.Children.Where(cc => cc.Type != NodeType.Block).Any();

                    if (!c1IsMid || !c2IsMid)
                    {
                        plan.Pattern = OutlinePattern.Unsupported;
                        plan.Reason = "좌우 반대측 직각 분기 2개이나 한쪽 이상이 Leaf(말단) — Duct_C1 은 양쪽 모두 Mid Duct 여야 함.";
                    }
                    else
                    {
                        // bb/cc 는 기하적으로 대칭이라 Handle 오름차순으로 결정적 배정 (§7.3/§12 확정)
                        bool c1First = System.StringComparer.Ordinal.Compare(c1.Handle, c2.Handle) <= 0;
                        plan.Pattern = OutlinePattern.Duct_C1;
                        plan.BranchB = c1First ? c1 : c2; plan.BranchBLine = c1First ? c1Line : c2Line;
                        plan.BranchA = c1First ? c2 : c1; plan.BranchALine = c1First ? c2Line : c1Line;
                        plan.Reason = "좌우 반대측 직각 분기 2개(모두 Mid) — Duct_C1 후보(폭 제약 없음).";
                    }
                }
                else if (dot > 1.0 - PerpTol)
                {
                    plan.Pattern = OutlinePattern.Unsupported;
                    plan.Reason = "같은 측 이중 분기 — 4패턴에 없음, 수동 처리.";
                }
                else
                {
                    plan.Pattern = OutlinePattern.Unsupported;
                    plan.Reason = "지원하지 않는 각도 조합(두 분기가 반대측/같은측 아님).";
                }
                return;
            }

            plan.Pattern = OutlinePattern.Unsupported;
            plan.Reason = "지원하지 않는 위상 조합(collinear-collinear 등 비정상 위상).";
        }

        /// <summary>node 자신의 방향(nodeDir) 대비 child 의 관계(직선 연장/직각/기타)를 판정한다.</summary>
        private Relation Classify(Vector3d nodeDir, Point3d x, Line childLine)
        {
            Vector3d dirChild = (FarEnd(childLine, x) - x).GetNormal();
            double dot = nodeDir.DotProduct(dirChild);
            if (System.Math.Abs(dot + 1.0) < PerpTol) return Relation.CollinearOpposite;
            if (System.Math.Abs(dot) < PerpTol) return Relation.Perpendicular;
            return Relation.Other;
        }

        /// <summary>nodeLine 의 두 끝점 중 lineChildren 전부의 근단이 모이는 쪽을 접합점 X 로 반환.
        /// 다른 한쪽 끝점은 (부모 쪽/Root 는 임의 기준) other 로 반환한다.</summary>
        private bool TryFindJunction(Line nodeLine, List<Node> lineChildren, Dictionary<Node, Line> childLines, out Point3d x, out Point3d other)
        {
            x = default;
            other = default;

            Point3d[] ends = { nodeLine.StartPoint, nodeLine.EndPoint };
            for (int i = 0; i < ends.Length; i++)
            {
                Point3d candidate = ends[i];
                bool allMatch = lineChildren.All(c =>
                    NearEnd(childLines[c], candidate).DistanceTo(candidate) < JunctionTol);
                if (allMatch)
                {
                    x = candidate;
                    other = ends[1 - i];
                    return true;
                }
            }
            return false;
        }

        private Point3d FarEnd(Line ln, Point3d refPt) =>
            ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt) ? ln.EndPoint : ln.StartPoint;

        private Point3d NearEnd(Line ln, Point3d refPt) =>
            ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt) ? ln.StartPoint : ln.EndPoint;

        /// <summary>XData "a" 문자열에서 폭(double) 추출. "600x400" 형태면 앞 숫자 사용.</summary>
        private bool TryReadWidth(Line ln, out double w)
        {
            w = 0;
            string s = JXdata.GetXdata(ln, "a");
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out w)) return true;

            int idx = s.IndexOfAny(new[] { 'x', 'X', '*' });
            return idx > 0 && double.TryParse(s.Substring(0, idx).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out w);
        }
    }
}
