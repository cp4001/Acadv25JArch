using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Duct_ToRevit — 선택한 덕트 중심선을 열린 Revit 프로젝트에 덕트로 만든다.
    /// 대상: LINE + XData "Tree"(폭 "a", 높이 "b", 그룹 "Group"), 엘보 위치는 ARC + XData "a".
    /// 수신측은 JRevit 의 DuctBridge. Revit 쪽에 JRevit 이 실행 중이어야 한다.
    /// </summary>
    public class DuctToRevitCommand
    {
        [CommandMethod("Duct_ToRevit")]
        public void Cmd_DuctToRevit()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using var form = new DuctToRevitForm(doc);
            Application.ShowModalDialog(form);
        }
    }

    /// <summary>선택 결과를 Revit 전송용 데이터로 바꾼다.</summary>
    internal class DuctSelection
    {
        public const string NoGroup = "미지정";

        public List<RevitDuctLine> Lines { get; } = new();
        public List<RevitDuctArc> Arcs { get; } = new();

        /// <summary>a/b 가 없거나 숫자가 아니어서 제외한 Line 수 (부하 0 인 Line 등).</summary>
        public int NoSizeCount { get; private set; }

        public static DuctSelection Read(Database db, IEnumerable<ObjectId> ids)
        {
            var selection = new DuctSelection();
            using Transaction tr = db.TransactionManager.StartTransaction();
            foreach (ObjectId id in ids)
            {
                DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                if (obj is Line line) selection.AddLine(line);
                else if (obj is Arc arc) selection.Arcs.Add(new RevitDuctArc { Start = Xy(arc.StartPoint), End = Xy(arc.EndPoint) });
            }
            tr.Commit();
            return selection;
        }

        private void AddLine(Line line)
        {
            // 필터가 "Tree,a" 라 a 만 있는 Line 도 들어온다. 덕트 Tree 소속이 아니면 무시한다.
            if (JXdata.GetXdata(line, "Tree") == null) return;

            if (!DuctToRevitClient.TryParseSize(JXdata.GetXdata(line, "a"), out double width) ||
                !DuctToRevitClient.TryParseSize(JXdata.GetXdata(line, "b"), out double height))
            {
                NoSizeCount++;
                return;
            }

            string? group = JXdata.GetXdata(line, "Group");
            Lines.Add(new RevitDuctLine
            {
                Group = string.IsNullOrWhiteSpace(group) ? NoGroup : group,
                Start = Xy(line.StartPoint),
                End = Xy(line.EndPoint),
                Width = width,
                Height = height
            });
        }

        private static double[] Xy(Autodesk.AutoCAD.Geometry.Point3d p) => new[] { p.X, p.Y };
    }
}
