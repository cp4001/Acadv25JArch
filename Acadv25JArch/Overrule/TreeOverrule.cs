using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using AcadColor = Autodesk.AutoCAD.Colors.Color;

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

        private const double LABEL_TEXT_HEIGHT = 30.0;
        private const short  LABEL_COLOR_ACI   = 7;  // White

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

                // trait 원복 — MText 는 자체 Color 사용
                wd.SubEntityTraits.Color      = origColor;
                wd.SubEntityTraits.LineWeight = origLW;

                string label = ComposeLabel(
                    JXdata.GetXdata(line, "Dia"),
                    JXdata.GetXdata(line, "TotalLPM"));
                if (!string.IsNullOrEmpty(label))
                    DrawCenterLabel(line, label, LABEL_COLOR_ACI, wd);

                return true;
            }
            catch
            {
                wd.SubEntityTraits.Color      = origColor;
                wd.SubEntityTraits.LineWeight = origLW;
                return base.WorldDraw(drawable, wd);
            }
        }

        private static string ComposeLabel(string? dia, string? totalLpm)
        {
            bool hasDia = !string.IsNullOrEmpty(dia);
            bool hasLpm = !string.IsNullOrEmpty(totalLpm);
            if (hasDia && hasLpm) return $"{dia}[{totalLpm}]";
            if (hasDia)           return dia!;
            if (hasLpm)           return $"[{totalLpm}]";
            return string.Empty;
        }

        private static void DrawCenterLabel(Line line, string text, short aciColor, WorldDraw wd)
        {
            Point3d sp = line.StartPoint;
            Point3d ep = line.EndPoint;
            Vector3d dir = ep - sp;
            if (dir.Length < 1e-6) return;
            dir = dir.GetNormal();

            Point3d center = new Point3d(
                (sp.X + ep.X) * 0.5,
                (sp.Y + ep.Y) * 0.5,
                (sp.Z + ep.Z) * 0.5);

            // 거꾸로 읽히지 않도록 ±90° 범위로 정규화
            double angle = System.Math.Atan2(dir.Y, dir.X);
            if (angle > System.Math.PI / 2 || angle < -System.Math.PI / 2)
                angle += System.Math.PI;

            // Line 본체와 겹치지 않게 수직 방향으로 살짝 오프셋
            Vector3d perp = new Vector3d(-dir.Y, dir.X, 0);
            Point3d loc = (perp.Length > 1e-6)
                ? center + perp.GetNormal() * (LABEL_TEXT_HEIGHT * 0.6)
                : center;

            using var mtext = new MText();
            mtext.SetDatabaseDefaults();
            mtext.Location   = loc;
            mtext.TextHeight = LABEL_TEXT_HEIGHT;
            mtext.Contents   = text;
            mtext.Rotation   = angle;
            mtext.Attachment = AttachmentPoint.MiddleCenter;
            mtext.Color      = AcadColor.FromColorIndex(ColorMethod.ByAci, aciColor);
            mtext.WorldDraw(wd);
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
