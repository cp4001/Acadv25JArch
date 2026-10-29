using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Globalization;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Duct_tag — XData "Tree" Line 을 고르고 태그 위치를 찍으면 Line 위 점(●)에서 지시선을 그어
    /// "{a}x{b}" Text 를 배치한다. Esc 까지 반복.
    /// 수평 Line : 점 → 클릭 높이까지 수직선 → 오른쪽으로 짧은 수평선 → 글자
    /// 수직 Line : 점 → 클릭 위치까지 수평 직선 → 글자 (클릭한 쪽으로)
    /// 글자 높이 = a × 0.8, 수평 꺾은선 길이 = a / 2, 지시선·점 ACI 1(Red), 글자 ByLayer. 현재 레이어·문자 스타일.
    /// </summary>
    public class DuctTagCommand
    {
        private const short LeaderColor = 1;      // Red
        private const double DotRatio = 0.3;      // 점 지름 = 글자 높이 × 0.3
        private const double GapRatio = 0.2;      // 지시선 끝 ~ 글자 간격 = 글자 높이 × 0.2
        private const double TextRatio = 0.8;     // 글자 높이 = a × 0.8
        private const double TailRatio = 0.5;     // 수평 Line 꺾은선 길이 = a / 2

        [CommandMethod("Duct_tag")]
        public void Cmd_DuctTag()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var peo = new PromptEntityOptions("\n태그할 덕트 Line 선택 (Esc 종료): ");
            peo.SetRejectMessage("\nLine 만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            while (true)
            {
                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                // 1. Line 정보만 복사 — 점 입력(Editor)은 Transaction 밖에서
                Point3d sp, ep;
                string aStr, bStr;
                double a;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var line = (Line)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    if (JXdata.GetXdata(line, "Tree") == null)
                    {
                        ed.WriteMessage("\n[건너뜀] XData \"Tree\" 가 없는 Line 입니다.");
                        continue;
                    }
                    aStr = JXdata.GetXdata(line, "a") ?? "";
                    bStr = JXdata.GetXdata(line, "b") ?? "";
                    if (aStr == "" || bStr == "" ||
                        !double.TryParse(aStr, NumberStyles.Any, CultureInfo.InvariantCulture, out a) || a <= 0)
                    {
                        ed.WriteMessage($"\n[건너뜀] XData a/b 가 없거나 a 가 숫자가 아닙니다. (a=\"{aStr}\", b=\"{bStr}\")");
                        continue;
                    }
                    sp = line.StartPoint;
                    ep = line.EndPoint;
                    tr.Commit();
                }

                PromptPointResult ppr = ed.GetPoint("\n태그 위치 지정: ");
                if (ppr.Status != PromptStatus.OK) return;
                Point3d pick = ppr.Value;

                // 2. 지시선 기하 — 점(foot)은 클릭점을 Line 에 투영(선분 안으로 제한)
                var seg = new LineSegment3d(sp, ep);
                Point3d foot = seg.GetClosestPointTo(pick).Point;
                Vector3d dir = ep - sp;
                bool horizontal = System.Math.Abs(dir.X) >= System.Math.Abs(dir.Y);
                double h = a * TextRatio;

                var pts = new List<Point3d> { foot };
                Point3d textPt;
                bool textRight;    // true: 글자가 지시선 끝 오른쪽 (TextLeft 정렬)
                if (horizontal)
                {
                    var corner = new Point3d(foot.X, pick.Y, foot.Z);
                    var tail = corner + Vector3d.XAxis * (a * TailRatio);
                    pts.Add(corner);
                    pts.Add(tail);
                    textRight = true;
                    textPt = tail + Vector3d.XAxis * (h * GapRatio);
                }
                else
                {
                    var end = new Point3d(pick.X, foot.Y, foot.Z);
                    pts.Add(end);
                    textRight = pick.X >= foot.X;
                    textPt = end + Vector3d.XAxis * (textRight ? h * GapRatio : -h * GapRatio);
                }
                if (pts[0].DistanceTo(pts[1]) < 1e-6)
                {
                    ed.WriteMessage("\n[건너뜀] 태그 위치가 Line 위입니다. Line 에서 떨어진 곳을 지정하세요.");
                    continue;
                }

                // 3. 생성 — 지시선(Line) + 점(Donut 형 Polyline) + Text
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    for (int i = 0; i + 1 < pts.Count; i++)
                        Append(tr, space, new Line(pts[i], pts[i + 1]) { ColorIndex = LeaderColor });

                    // 채워진 원: 중심선 반지름 D/4, 폭 D/2 인 닫힌 2-호 Polyline (DONUT 과 같은 방식)
                    double d = h * DotRatio;
                    var dot = new Autodesk.AutoCAD.DatabaseServices.Polyline { ColorIndex = LeaderColor, Closed = true, Elevation = foot.Z };
                    dot.AddVertexAt(0, new Point2d(foot.X - d / 4, foot.Y), 1.0, d / 2, d / 2);
                    dot.AddVertexAt(1, new Point2d(foot.X + d / 4, foot.Y), 1.0, d / 2, d / 2);
                    Append(tr, space, dot);

                    var txt = new DBText
                    {
                        TextString = (horizontal ? " " : "") + $"{aStr}x{bStr}",   // 수평: 꺾은선과 띄우려고 앞에 공백 1칸
                        Height = h,
                        Position = textPt,
                        TextStyleId = db.Textstyle,
                        HorizontalMode = textRight ? TextHorizontalMode.TextLeft : TextHorizontalMode.TextRight,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        AlignmentPoint = textPt,
                    };
                    Append(tr, space, txt);
                    txt.AdjustAlignment(db);

                    tr.Commit();
                }
                ed.WriteMessage($"\n태그: {aStr}x{bStr}");
            }
        }

        private static void Append(Transaction tr, BlockTableRecord space, Entity ent)
        {
            space.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);
        }
    }
}
