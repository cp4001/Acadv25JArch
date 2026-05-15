using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class SubSelCommand
    {
        [CommandMethod("SUBSEL", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void SubSel()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var implied = ed.SelectImplied();
            if (implied.Status != PromptStatus.OK || implied.Value == null || implied.Value.Count == 0)
            {
                ed.WriteMessage("\n선택된 객체가 없습니다. SELECTSIMILAR 등으로 먼저 선택한 뒤 실행하세요.");
                return;
            }
            var baseIds = new HashSet<ObjectId>(implied.Value.GetObjectIds());
            int baseCount = baseIds.Count;

            var p1 = ed.GetPoint("\n첫 번째 모서리 점: ");
            if (p1.Status != PromptStatus.OK) return;
            var p2 = ed.GetCorner("\n반대편 모서리 점: ", p1.Value);
            if (p2.Status != PromptStatus.OK) return;

            var win = ed.SelectWindow(p1.Value, p2.Value);
            if (win.Status != PromptStatus.OK || win.Value == null || win.Value.Count == 0)
            {
                ed.WriteMessage($"\n범위 내 객체 없음 ({baseCount}개 중 0개). PickFirst 해제.");
                ed.SetImpliedSelection(new ObjectId[0]);
                return;
            }
            var winIds = new HashSet<ObjectId>(win.Value.GetObjectIds());

            baseIds.IntersectWith(winIds);

            if (baseIds.Count == 0)
            {
                ed.WriteMessage($"\n교집합 없음 ({baseCount}개 중 0개). PickFirst 해제.");
                ed.SetImpliedSelection(new ObjectId[0]);
                return;
            }

            ed.SetImpliedSelection(baseIds.ToArray());
            ed.WriteMessage($"\n{baseCount}개 중 {baseIds.Count}개 필터됨.");
            doc.Window.Focus();
        }
    }
}
