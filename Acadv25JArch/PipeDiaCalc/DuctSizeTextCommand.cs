using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System.Globalization;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// Insert_Duct_Size — XData "Tree" Line 을 여러 개 선택하면 각 Line 중점에
    /// "{a}x{b}"(XData a, b 문자열 그대로) DBText 를 MiddleCenter 로 배치한다.
    /// 글자 높이 = a / 2, a &lt; MinA 이거나 Line 길이 ≤ a × MinLenRatio 인 Line 은 표시하지 않는다.
    /// 생성한 Text 에는 XData "DuctSize" = 글자 그대로 기록한다.
    /// 회전은 Line 방향을 따르되 글자가 뒤집히지 않도록 (-90°, 90°] 로 맞춘다.
    /// </summary>
    public class DuctSizeTextCommand
    {
        private const double MinA = 300.0;   // 이 값 이상만 표시
        private const double MinLenRatio = 4.0;   // Line 길이 <= a × 이 값이면 표시 안 함

        [CommandMethod("Insert_Duct_Size")]
        public void Cmd_InsertDuctSize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n사이즈를 표시할 덕트 Line 들을 선택하세요: "
            };
            SelectionFilter filter = JSelFilter.MakeFilterTypesRegs("LINE", "Tree");
            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if (psr.Status != PromptStatus.OK) return;

            int added = 0, small = 0, shortLen = 0, invalid = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                tr.ChecRegNames(db, "DuctSize");

                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    var line = (Line)tr.GetObject(id, OpenMode.ForRead);
                    string? aStr = JXdata.GetXdata(line, "a");
                    string? bStr = JXdata.GetXdata(line, "b");
                    if (string.IsNullOrEmpty(aStr) || string.IsNullOrEmpty(bStr) ||
                        !double.TryParse(aStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
                        a <= 0)
                    {
                        invalid++;
                        continue;
                    }
                    if (a < MinA)
                    {
                        small++;
                        continue;
                    }
                    if (line.Length <= a * MinLenRatio)
                    {
                        shortLen++;
                        continue;
                    }

                    // 글자가 뒤집히지 않도록 회전을 (-90°, 90°] 로 정규화
                    double rot = line.Angle;                       // [0, 2π)
                    if (rot > System.Math.PI / 2 && rot <= System.Math.PI * 1.5) rot -= System.Math.PI;

                    Point3d mid = line.StartPoint + (line.EndPoint - line.StartPoint) * 0.5;
                    var txt = new DBText
                    {
                        TextString = $"{aStr}x{bStr}",
                        Height = a / 2.0,
                        Rotation = rot,
                        Position = mid,
                        TextStyleId = db.Textstyle,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        AlignmentPoint = mid,
                    };
                    space.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);
                    txt.AdjustAlignment(db);
                    JXdata.SetXdata(txt, "DuctSize", txt.TextString);   // AppendEntity 이후 (arx SetOpen)
                    added++;
                }

                tr.Commit();
            }

            ed.WriteMessage($"\nInsert_Duct_Size 완료 — 표시 {added}개, a<{MinA} 생략 {small}개, 길이≤a×{MinLenRatio} 생략 {shortLen}개" +
                            (invalid > 0 ? $", XData a/b 없음·오류 {invalid}개" : ""));
        }
    }
}
