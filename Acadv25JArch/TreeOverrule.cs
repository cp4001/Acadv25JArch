using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;

namespace PipeLoad2
{
    /// <summary>
    /// Line XData "Tree" 값(Root/Mid/Leaf)에 따라 렌더 색상만 오버레이.
    /// FcuLineTreeBuilder.ApplyDiameters 가 기록한 XData 기반.
    /// Root → Red, Mid → Blue, Leaf → Yellow.
    /// </summary>
    public class TreeDrawOverrule : DrawableOverrule
    {
        private static TreeDrawOverrule? _instance;
        private const string XDATA_REGAPP_NAME = "Tree";

        public static bool IsRegistered => _instance != null;

        // AutoCAD ACI
        private const short COLOR_ROOT = 1;  // Red
        private const short COLOR_MID  = 5;  // Blue
        private const short COLOR_LEAF = 2;  // Yellow

        public static void Register()
        {
            if (_instance != null) return;
            _instance = new TreeDrawOverrule();

            RXClass lineClass = RXObject.GetClass(typeof(Line));
            Overrule.AddOverrule(lineClass, _instance, false);

            // XData "Tree" 있는 Line만 WorldDraw 진입
            _instance.SetXDataFilter(XDATA_REGAPP_NAME);
            Overrule.Overruling = true;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Editor.WriteMessage("\n========================================");
                doc.Editor.WriteMessage("\n[TreeOverrule] 등록 완료");
                doc.Editor.WriteMessage($"\n✓ 대상: Line (XData \"{XDATA_REGAPP_NAME}\")");
                doc.Editor.WriteMessage("\n✓ Root → Red, Mid → Blue, Leaf → Yellow");
                doc.Editor.WriteMessage("\n========================================");
            }
        }

        public static void Unregister()
        {
            if (_instance == null) return;

            RXClass lineClass = RXObject.GetClass(typeof(Line));
            Overrule.RemoveOverrule(lineClass, _instance);
            _instance.SetXDataFilter(null);
            _instance = null;

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n[TreeOverrule] 제거됨");
        }

        public override bool IsApplicable(RXObject overruledSubject)
        {
            Entity? entity = overruledSubject as Entity;
            if (entity == null) return false;
            if (entity.ObjectId.IsNull || entity.ObjectId.IsErased) return false;

            // XData "Tree" 존재 여부를 엄격히 확인 — SetXDataFilter 보조 (없으면 Overrule 적용 안함)
            try
            {
                using ResultBuffer? rb = entity.GetXDataForApplication(XDATA_REGAPP_NAME);
                return rb != null;
            }
            catch { return false; }
        }

        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            if (drawable is not Line line)
                return base.WorldDraw(drawable, wd);

            // 원본 trait 저장 — 다른 Entity 그리기에 누수 방지
            short origColor = wd.SubEntityTraits.Color;
            LineWeight origLW = wd.SubEntityTraits.LineWeight;

            try
            {
                string? tree = JXdata.GetXdata(line, XDATA_REGAPP_NAME);
                short color = tree switch
                {
                    "Root" => COLOR_ROOT,
                    "Mid"  => COLOR_MID,
                    "Leaf" => COLOR_LEAF,
                    _      => (short)0
                };

                if (color == 0)
                    return base.WorldDraw(drawable, wd);

                wd.SubEntityTraits.Color      = color;
                wd.SubEntityTraits.LineWeight = LineWeight.LineWeight050;
                wd.Geometry.WorldLine(line.StartPoint, line.EndPoint);

                // trait 원복
                wd.SubEntityTraits.Color      = origColor;
                wd.SubEntityTraits.LineWeight = origLW;
                return true;
            }
            catch
            {
                wd.SubEntityTraits.Color      = origColor;
                wd.SubEntityTraits.LineWeight = origLW;
                return base.WorldDraw(drawable, wd);
            }
        }
    }

    /// <summary>TreeOverrule 토글 커맨드.</summary>
    public class TreeOverruleCommand
    {
        private const string XDATA_REGAPP_NAME = "Tree";

        [CommandMethod("TTG")]
        public void Cmd_ToggleTreeOverrule()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                if (TreeDrawOverrule.IsRegistered)
                {
                    TreeDrawOverrule.Unregister();
                }
                else
                {
                    RegisterRegApp(doc.Database, XDATA_REGAPP_NAME);
                    TreeDrawOverrule.Register();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[TTG] 오류: {ex.Message}");
            }
        }

        private static void RegisterRegApp(Database db, string appName)
        {
            using var tr = db.TransactionManager.StartTransaction();
            var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!rat.Has(appName))
            {
                rat.UpgradeOpen();
                var ratr = new RegAppTableRecord { Name = appName };
                rat.Add(ratr);
                tr.AddNewlyCreatedDBObject(ratr, true);
            }
            tr.Commit();
        }
    }
}
