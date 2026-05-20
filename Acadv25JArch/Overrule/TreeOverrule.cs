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
    /// Line: XData "Tree" (Root/Mid/Leaf) → 색상/LineWeight + 라벨 오버레이.
    /// BlockReference: XData "Disp" → 블록 Geo 센터에 짧은변/2 크기 Red 텍스트.
    /// 두 인스턴스(Line용, Block용)를 각자 SetXDataFilter 와 함께 등록.
    /// </summary>
    public class TreeDrawOverrule : DrawableOverrule
    {
        private static TreeDrawOverrule? _lineInstance;
        private static TreeDrawOverrule? _blockInstance;

        private const string XDATA_TREE_NAME = "Tree";
        private const string XDATA_DISP_NAME = "Disp";

        // AutoCAD ACI
        private const short COLOR_ROOT = 1;  // Red
        private const short COLOR_MID  = 3;  // Green
        private const short COLOR_LEAF = 2;  // Yellow
        private const short COLOR_DISP = 1;  // Red

        private const double LABEL_TEXT_HEIGHT = 30.0;
        private const short  LABEL_COLOR_ACI   = 7;  // White

        public static bool IsRegistered => _lineInstance != null || _blockInstance != null;

        private readonly bool _isBlockMode;

        private TreeDrawOverrule(bool isBlockMode) { _isBlockMode = isBlockMode; }

        public static void Register()
        {
            if (IsRegistered) return;

            _lineInstance = new TreeDrawOverrule(false);
            Overrule.AddOverrule(RXObject.GetClass(typeof(Line)), _lineInstance, false);
            _lineInstance.SetXDataFilter(XDATA_TREE_NAME);

            _blockInstance = new TreeDrawOverrule(true);
            Overrule.AddOverrule(RXObject.GetClass(typeof(BlockReference)), _blockInstance, false);
            _blockInstance.SetXDataFilter(XDATA_DISP_NAME);

            Overrule.Overruling = true;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Editor.WriteMessage("\n========================================");
                doc.Editor.WriteMessage("\n[TreeOverrule] 등록 완료");
                doc.Editor.WriteMessage($"\n✓ Line   (XData \"{XDATA_TREE_NAME}\") → Root/Mid/Leaf 색상 + 라벨");
                doc.Editor.WriteMessage($"\n✓ Block  (XData \"{XDATA_DISP_NAME}\") → Geo 센터 Red 텍스트(짧은변/2)");
                doc.Editor.WriteMessage("\n========================================");
            }
        }

        public static void Unregister()
        {
            if (_lineInstance != null)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Line)), _lineInstance);
                _lineInstance.SetXDataFilter(null);
                _lineInstance = null;
            }
            if (_blockInstance != null)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(BlockReference)), _blockInstance);
                _blockInstance.SetXDataFilter(null);
                _blockInstance = null;
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n[TreeOverrule] 제거됨");
        }

        public override bool IsApplicable(RXObject overruledSubject)
        {
            Entity? entity = overruledSubject as Entity;
            if (entity == null) return false;
            if (entity.ObjectId.IsNull || entity.ObjectId.IsErased) return false;

            string appName = _isBlockMode ? XDATA_DISP_NAME : XDATA_TREE_NAME;
            try
            {
                using ResultBuffer? rb = entity.GetXDataForApplication(appName);
                return rb != null;
            }
            catch { return false; }
        }

        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            if (_isBlockMode)
                return DrawBlockDisp(drawable, wd);

            return DrawLineTree(drawable, wd);
        }

        private bool DrawLineTree(Drawable drawable, WorldDraw wd)
        {
            if (drawable is not Line line)
                return base.WorldDraw(drawable, wd);

            // 원본 trait 저장 — 다른 Entity 그리기에 누수 방지
            short origColor = wd.SubEntityTraits.Color;
            LineWeight origLW = wd.SubEntityTraits.LineWeight;

            try
            {
                string? tree = JXdata.GetXdata(line, XDATA_TREE_NAME);
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

                // Line "Disp" — 표시 캐노니컬 값 (Duct: "{a}x{b}"). 있으면 최우선.
                string? disp = JXdata.GetXdata(line, XDATA_DISP_NAME);

                string label = "";
                if (!string.IsNullOrEmpty(disp))
                {
                    label = disp;
                }
                else
                {
                    string? dia      = JXdata.GetXdata(line, "Dia")      ?? "00";
                    string? total15A = JXdata.GetXdata(line, "Total15A") ?? "00";
                    string? lpm15A   = JXdata.GetXdata(line, "15A");

                    if (!string.IsNullOrEmpty(lpm15A))
                    {
                        label = $"{dia}[{total15A}]-{lpm15A}";
                    }
                    else if (!string.IsNullOrEmpty(total15A))
                    {
                        label = $"{dia}[{total15A}]";
                    }
                }
                //else if (!string.IsNullOrEmpty(total15A))
                //{
                //    //string? dia = JXdata.GetXdata(line, "Dia");
                //    label = !string.IsNullOrEmpty(dia)
                //        ? $"{dia}[{total15A}]"
                //        : $"[{total15A}]";
                //}
                //else
                //{
                //    label = ComposeLabel(
                //        JXdata.GetXdata(line, "Dia"),
                //        JXdata.GetXdata(line, "TotalLPM"));
                //}
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

        private bool DrawBlockDisp(Drawable drawable, WorldDraw wd)
        {
            // Block 본체는 정상 렌더 → 그 위에 텍스트 추가
            bool result = base.WorldDraw(drawable, wd);

            if (drawable is not BlockReference br) return result;

            try
            {
                string? disp = JXdata.GetXdata(br, XDATA_DISP_NAME);
                if (string.IsNullOrEmpty(disp)) return result;

                Extents3d ext;
                try { ext = br.GeometricExtents; }
                catch { return result; }

                double width  = ext.MaxPoint.X - ext.MinPoint.X;
                double height = ext.MaxPoint.Y - ext.MinPoint.Y;
                double shortSide = System.Math.Min(width, height);
                if (shortSide < 1e-6) return result;

                Point3d center = new Point3d(
                    (ext.MinPoint.X + ext.MaxPoint.X) * 0.5,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) * 0.5,
                    (ext.MinPoint.Z + ext.MaxPoint.Z) * 0.5);

                using var mtext = new MText();
                mtext.SetDatabaseDefaults();
                mtext.Location   = center;
                mtext.TextHeight = shortSide * 0.5;
                mtext.Contents   = disp;
                mtext.Rotation   = 0;
                mtext.Attachment = AttachmentPoint.MiddleCenter;
                mtext.Color      = AcadColor.FromColorIndex(ColorMethod.ByAci, COLOR_DISP);
                mtext.WorldDraw(wd);
            }
            catch { }

            return result;
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
                    RegisterRegApp(doc.Database, "Tree");
                    RegisterRegApp(doc.Database, "Disp");
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
