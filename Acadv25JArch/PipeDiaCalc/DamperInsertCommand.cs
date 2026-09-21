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
    /// Insert_Damper — Line 을 선택하면 클릭 위치에 가까운 끝점에서 Line 방향으로
    /// 225 떨어진 지점에 동적 블럭 "JDamper_Dynamic" 을 삽입한다.
    /// 블럭 크기는 Line XData "a"(덕트 폭) 의 1/2 을 Dis1/Dis2 동적 속성에 넣어 조정하고,
    /// 회전은 선택 Line 의 각도(가까운 끝점 → 먼 끝점 방향)를 적용한다.
    /// </summary>
    public class DamperInsertCommand
    {
        private const string BlockName = "JDamper_Dynamic";
        private const double Offset = 225.0;   // 가까운 끝점에서 블럭 삽입점까지 거리

        [CommandMethod("Insert_Damper")]
        public void Cmd_InsertDamper()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var peo = new PromptEntityOptions("\n댐퍼를 삽입할 Line 을 선택하세요: ");
            peo.SetRejectMessage("\nLine 만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var line = (Line)tr.GetObject(per.ObjectId, OpenMode.ForRead);

                // 1. XData "a" (덕트 폭) → Dis1/Dis2 = a/2
                string? aStr = JXdata.GetXdata(line, "a");
                if (string.IsNullOrEmpty(aStr) ||
                    !double.TryParse(aStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double width) ||
                    width <= 0)
                {
                    ed.WriteMessage($"\n[오류] 선택한 Line 에 유효한 XData \"a\"(덕트 폭)가 없습니다. (값: \"{aStr}\")");
                    return;
                }
                double dis = width / 2.0;

                // 2. 클릭 위치에 가까운 끝점 → 먼 끝점 방향으로 Offset 만큼 이동한 지점
                Point3d pick = per.PickedPoint;
                bool startIsNear = line.StartPoint.DistanceTo(pick) <= line.EndPoint.DistanceTo(pick);
                Point3d nearPt = startIsNear ? line.StartPoint : line.EndPoint;
                Point3d farPt = startIsNear ? line.EndPoint : line.StartPoint;

                Vector3d dir = farPt - nearPt;
                if (dir.Length < 1e-6)
                {
                    ed.WriteMessage("\n[오류] Line 길이가 0 입니다.");
                    return;
                }
                dir = dir.GetNormal();
                Point3d insPt = nearPt + dir * Offset;

                // 3. 블럭 삽입
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(BlockName))
                {
                    ed.WriteMessage($"\n[오류] 도면에 '{BlockName}' 블럭이 없습니다.");
                    return;
                }
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                var br = new BlockReference(insPt, bt[BlockName]);
                br.Rotation = System.Math.Atan2(dir.Y, dir.X);
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // 4. 동적 속성 Dis1 / Dis2
                int setCount = 0;
                foreach (DynamicBlockReferenceProperty prop in br.DynamicBlockReferencePropertyCollection)
                {
                    if (!prop.PropertyName.Equals("Dis1", System.StringComparison.OrdinalIgnoreCase) &&
                        !prop.PropertyName.Equals("Dis2", System.StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        prop.Value = dis;
                        setCount++;
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[경고] {prop.PropertyName} 설정 실패: {ex.Message}");
                    }
                }
                if (setCount < 2)
                    ed.WriteMessage($"\n[경고] Dis1/Dis2 동적 속성 {setCount}개만 설정되었습니다.");

                tr.Commit();

                ed.WriteMessage($"\n{BlockName} 삽입 완료 — a={width}, Dis1/Dis2={dis}, 회전={br.Rotation * 180.0 / System.Math.PI:0.##}°");
            }
        }
    }
}
