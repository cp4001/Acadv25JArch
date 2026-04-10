using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    public class LineTreeFormCommand
    {
        [CommandMethod("LINETREE_FORM")]
        public void Cmd_LineTreeForm()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                // 1. Line 선택
                TypedValue[] filterList = [new TypedValue((int)DxfCode.Start, "LINE")];
                var filter = new SelectionFilter(filterList);
                var opts   = new PromptSelectionOptions { MessageForAdding = "\nLine들을 선택하세요: " };
                var psr    = ed.GetSelection(opts, filter);
                if (psr.Status != PromptStatus.OK) return;
                var selectedIds = psr.Value.GetObjectIds().ToList();

                // 2. Root Line 선택
                var peo = new PromptEntityOptions("\nRoot Line을 선택하세요: ");
                peo.SetRejectMessage("\nLine만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Line), true);
                var per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                // 3. Transaction - ForRead만
                using var tr = db.TransactionManager.StartTransaction();

                var rootLine = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                if (rootLine == null) { tr.Commit(); return; }

                var allLines = selectedIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(l => l != null && l.Layer == rootLine.Layer)
                    .ToList();

                ed.WriteMessage($"\n레이어 [{rootLine.Layer}] 필터: {allLines.Count}개 Line");

                // 4. Tree 구성
                var builder     = new LineTreeBuilder();
                var connections = builder.BuildConnectionGraph(allLines);
                var rootNode    = builder.BuildTreeStructureBFS(rootLine, allLines, connections);
                builder.CalculateLoads(rootNode);
                builder.CalculateDiameters(rootNode, db);

                int totalNodes = builder.CountNodes(rootNode);
                int leafCount  = builder.CountLeafNodes(rootNode);

                tr.Commit();

                // 5. Form 표시
                Application.ShowModelessDialog(
                    new LineTreeForm(rootNode, totalNodes, leafCount, rootLine.Layer, db));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }
    }
}
