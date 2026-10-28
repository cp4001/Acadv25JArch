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
    /// Insert_UpDn — XData "Tree" 가 있는 덕트 Line 2개를 한 번에 선택하면 두 Line 이
    /// 서로 교차하는 점(선 안쪽)에 동적 블럭 "UpDn" 을 삽입한다.
    /// 블럭 정의는 매 실행마다 참조 도면(Blocks\JArch_Blocks.dwg)에서 가져와 덮어쓴다.
    /// 동적 속성 DS = 두 Line XData "a"(덕트 폭) 중 작은 값, DL = 큰 값.
    /// 회전은 작은 a Line 의 line.Angle(Start→End) 을 그대로 적용한다.
    /// </summary>
    public class UpDnInsertCommand
    {
        private const string BlockName = "UpDn";

        [CommandMethod("Insert_UpDn")]
        public void Cmd_InsertUpDn()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // XData "Tree" Line 만 한 번에 선택 — 정확히 2개가 될 때까지 다시 묻는다.
            var pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n교차하는 덕트 Line 2개를 선택하세요: "
            };
            SelectionFilter filter = JSelFilter.MakeFilterTypesRegs("LINE", "Tree");
            ObjectId id1, id2;
            while (true)
            {
                PromptSelectionResult psr = ed.GetSelection(pso, filter);
                if (psr.Status != PromptStatus.OK) return;

                ObjectId[] ids = psr.Value.GetObjectIds();
                if (ids.Length == 2)
                {
                    id1 = ids[0];
                    id2 = ids[1];
                    break;
                }
                ed.WriteMessage($"\n[다시 선택] Tree Line 이 {ids.Length}개 선택되었습니다. 2개만 선택하세요.");
            }

            // 1. 검증(XData a, 교차점) — 블럭 정의를 덮어쓰기 전에 끝낸다.
            Point3d insPt;
            double aSmall, aLarge, rotation;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var l1 = (Line)tr.GetObject(id1, OpenMode.ForRead);
                var l2 = (Line)tr.GetObject(id2, OpenMode.ForRead);

                if (!TryReadDuct(l1, ed, out double a1) || !TryReadDuct(l2, ed, out double a2)) return;

                var pts = new Point3dCollection();
                l1.IntersectWith(l2, Intersect.OnBothOperands, pts, System.IntPtr.Zero, System.IntPtr.Zero);
                if (pts.Count == 0)
                {
                    ed.WriteMessage("\n[오류] 두 Line 이 서로 교차하지 않습니다.");
                    return;
                }
                insPt = pts[0];

                // 작은 a Line 의 각도 (같으면 첫 번째 Line)
                Line small = a1 <= a2 ? l1 : l2;
                aSmall = System.Math.Min(a1, a2);
                aLarge = System.Math.Max(a1, a2);
                rotation = small.Angle;

                tr.Commit();
            }

            // 2. 참조 도면에서 블럭 정의를 가져온다(기존 정의는 덮어씀). Transaction 밖에서 수행.
            if (!JArchBlockLibrary.Import(db, ed, BlockName)) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 3. 블럭 삽입
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                var br = new BlockReference(insPt, bt[BlockName]);
                br.Rotation = rotation;
                space.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                // 4. 블럭 속성 — 코드로 만든 BlockReference 에는 속성이 자동 생성되지 않으므로
                //    정의의 AttributeDefinition 마다 기본값으로 AttributeReference 를 붙인다.
                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId id in btr)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition ad) continue;
                    if (ad.Constant) continue;

                    var ar = new AttributeReference();
                    ar.SetAttributeFromBlock(ad, br.BlockTransform);
                    br.AttributeCollection.AppendAttribute(ar);
                    tr.AddNewlyCreatedDBObject(ar, true);
                }

                // 5. 동적 속성 DS(작은 a) / DL(큰 a)
                int setCount = 0;
                foreach (DynamicBlockReferenceProperty prop in br.DynamicBlockReferencePropertyCollection)
                {
                    double? value =
                        prop.PropertyName.Equals("DS", System.StringComparison.OrdinalIgnoreCase) ? aSmall :
                        prop.PropertyName.Equals("DL", System.StringComparison.OrdinalIgnoreCase) ? aLarge :
                        null;
                    if (value == null) continue;

                    try
                    {
                        prop.Value = value.Value;
                        setCount++;
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[경고] {prop.PropertyName} 설정 실패: {ex.Message}");
                    }
                }
                if (setCount < 2)
                    ed.WriteMessage($"\n[경고] DS/DL 동적 속성 {setCount}개만 설정되었습니다.");

                tr.Commit();

                ed.WriteMessage($"\n{BlockName} 삽입 완료 — DS={aSmall}, DL={aLarge}, 회전={rotation * 180.0 / System.Math.PI:0.##}°");
            }
        }

        /// <summary>XData "a"(덕트 폭) 양수 숫자 확인. "Tree" 는 선택 필터가 보장한다.</summary>
        private static bool TryReadDuct(Line line, Editor ed, out double width)
        {
            width = 0;
            string? aStr = JXdata.GetXdata(line, "a");
            if (string.IsNullOrEmpty(aStr) ||
                !double.TryParse(aStr, NumberStyles.Any, CultureInfo.InvariantCulture, out width) ||
                width <= 0)
            {
                ed.WriteMessage($"\n[오류] 선택한 Line 에 유효한 XData \"a\"(덕트 폭)가 없습니다. (값: \"{aStr}\")");
                return false;
            }
            return true;
        }
    }
}
