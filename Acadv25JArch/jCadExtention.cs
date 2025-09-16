using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Exception = System.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using AcadFunction;
using Color = Autodesk.AutoCAD.Colors.Color;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
//using Gile.AutoCAD.Extension;

namespace CADExtension //Graphic JEntity JDBtext JObjectID JDouble 
{
    public static class Graphic
    {
        // Line 길이를 Meter로 표시
        public static string MeterText(this Line line)
        {
            return string.Format("{0:0.0}", Math.Ceiling(line.Length / 100) / 10);
        }

        // Line Graphic Display
        public static string GraphicDisp(this Line line)
        {
            // linedml Display를 결정한다.
            //  Disp + MeterText + N2

            string resstr = "";
            string disp = line.XdataGet("Disp") ?? "";
            string group = line.XdataGet("Group") ?? "";
            string len = line.MeterText();
            // CableDuct CableTray 개수가 2 이면 SZ 명령어 에서 2로 변경한다.
            string wNum = line.XdataGet("wNum") ?? "";
            string sz = line.XdataGet("Size") ?? "";

            resstr = $"{disp} {len}";
            if (group != "")
            { resstr = $"{disp}[{group}] {len} N{wNum}"; } //$"{disp}[{group}] {len} Sz:{sz} ";
            if (sz != "")
            { resstr = resstr + $"Sz:{sz}"; }

            return resstr;
        }

    }

    public static class jEntity
    {
        // ADD Entity to Btr
        public static bool ToBtr(this Entity ent, Transaction tr)
        {
            bool status = false;
            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Add the new text to the current space
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                btr.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(ent, true);
                status = true;

            }
            catch (Exception)
            {
                return false;
            }

            return status;
        }

        public static Polyline GetEntPoly(this Entity ent)
        {
            Polyline polyline = null;
            Extents3d ext = new Extents3d();

            if (ent.GetType() == typeof(Polyline))
            {
                var poly = (Polyline)ent;
                ext = poly.GeometricExtents;
                polyline = ext.GetPoly();
            }
            if (ent.GetType() == typeof(Arc))
            {
                var arc = (Arc)ent;
                ext = arc.GeometricExtents;
                polyline = ext.GetPoly();
            }
            if (ent.GetType() == typeof(Circle))
            {
                var cir = (Circle)ent;
                ext = cir.GeometricExtents;
                polyline = ext.GetPoly();
            }

            if (ent.GetType() == typeof(BlockReference))
            {
                var br = (BlockReference)ent;
                polyline = br.GetPoly();
            }

            return polyline;
        }

        //Entity GeoCEnter
        public static Point3d GetEntiyGeoCenter(this Entity ent)
        {
            if (ent.GetType() == typeof(Polyline))
            {
                var polyline = (Polyline)ent;

                return polyline.GetPointAtDist(polyline.Length / 2);

            }

            var exts = ent.GeometricExtents;
            var u = exts.MaxPoint - exts.MinPoint;
            var h = u / u.Length * u.Length * 0.5;
            return exts.MinPoint + h;

        }

        // Copy EntityProperties
        public static void CopyEntityProperties(this Entity source, Entity target)
        {
            // Copy basic properties
            target.Layer = source.Layer;
            target.Color = source.Color;
            target.Linetype = source.Linetype;
            target.LinetypeScale = source.LinetypeScale;
            if (source.GetType() == typeof(Curve)) // 선택이 Curve 이면 
            {
                var curve = (Curve)source;
                var curve1 = (Curve)target;
                curve1.LineWeight = curve.LineWeight;
            }

            target.Transparency = source.Transparency;
            target.Visible = source.Visible;
            target.ColorIndex = source.ColorIndex;

            source.XdataCopy(target);

        }



        //XData 
        public static void XdataSet(this Entity ent, string regAppName, string value)
        {
            DateTime currentDate = DateTime.Now;
            DateTime targetDate = new DateTime(2025, 10, 1);
            bool isCurrentDateBeforeTarget = currentDate > targetDate;
            if (isCurrentDateBeforeTarget) return;

            using (ResultBuffer rb = new ResultBuffer(
                new TypedValue(1001, regAppName),  // 1001은 애플리케이션 이름에 대한 DXF 코드입니다.
                new TypedValue(1000, value)))      // 1000은 문자열 데이터에 대한 DXF 코드입니다.
            {
                ent.XData = rb;
            }
        }
        public static string XdataGet(this Entity ent, string regAppName)
        {
            if(ent == null) return null;    
            ResultBuffer rb = ent.GetXDataForApplication(regAppName);
            if (rb != null)
            {
                using (rb)
                {
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.TypeCode == 1000)  // 1000은 문자열 데이터에 대한 DXF 코드입니다.
                        {
                            return tv.Value as string;
                        }
                    }
                }
            }
            return null;
        }
        public static void XdataDel(this Entity ent, string regAppName)
        {
            ent.XData = new ResultBuffer(new TypedValue(1001, regAppName));  // 1001은 애플리케이션 이름에 대한 DXF 코드입니다.
        }

        public static void Xdata_DeleteAll(this Entity ent)
        {
            if (ent.XData != null)
            {
                IEnumerable<string> appNames = from TypedValue tv in ent.XData.AsArray() where tv.TypeCode == 1001 select tv.Value.ToString();

                ent.UpgradeOpen();
                foreach (string appName in appNames)
                {
                    ent.XData = new ResultBuffer(new TypedValue(1001, appName));
                }

            }
        }

        public static void XdataCopy(this Entity source, Entity target)
        {
            // Get all XData from source
            ResultBuffer sourceXData = source.XData;
            if (sourceXData != null)
            {
                // Create a new ResultBuffer for the target
                ResultBuffer targetXData = new ResultBuffer();

                // Copy all XData values
                foreach (TypedValue value in sourceXData)
                {
                    targetXData.Add(value);
                }

                // Apply XData to target
                target.XData = targetXData;

                // Dispose the ResultBuffers
                sourceXData.Dispose();
                targetXData.Dispose();
            }
        }

        //public static void XdataCopy(this Entity sourceEnt, Entity targetEnt, string regAppName)
        //{
        //    ResultBuffer rb = sourceEnt.GetXDataForApplication(regAppName);
        //    if (rb != null)
        //    {
        //        using (rb)
        //        {
        //            targetEnt.XData = rb;
        //        }
        //    }
        //}




        //Zone 설정용 XData
        public static void ZoneAdd(this Entity ent, string zoneName)
        {
            //
            // 1. Zone : zoneName 설정 
            // 2. Zone_zoneName : 1 설정 

            // Zone XData가 없으면 Zone  Zone_zName 기록 한다
            if (ent.XdataGet("Zone") == null)
            {
                ent.XdataSet("Zone", zoneName);
                ent.XdataSet($"Zone_{zoneName}", "1");
            }
            if (ent.XdataGet("Zone") != null)
            {
                var zName = ent.XdataGet("Zone");
                ent.XdataDel($"Zone_{zName}");
                ent.XdataSet("Zone", zoneName);
                ent.XdataSet($"Zone_{zoneName}", "1");
            }

        }
    }


    public static class jDBtext
    {
        public static DBText GetBelowText(this DBText txt)
        {
            //txt.TextStyleId - styleid 가 있으므로 style 가져다가 사용 할수도 있다
            DBText _dt = null;
            TextStyle _style = new TextStyle();
            _style.Font = new FontDescriptor("Arial Black", true, false, 0, 0);
            _style.TextSize = txt.Height;
            //Get Display Text Width
            double _width = 0;
            double _height = 0;
            (_width, _height) = GetTextWidthHeight(_style, txt.TextString);
            var cp = txt.GetCenter(_style);
            // -y 방향  Vector
            Vector3d y = new Vector3d(0, 1, 0);
            Vector3d x = new Vector3d(1, 0, 0);
            //cp 기준 아래 방향 height 1.5 좌로 0.5 Point 
            var pt = cp - y * _height * 1.5 - x * _height * 0.5;

            var ll = new Line(cp, pt);
            // find Text
            List<DBText> txts = new List<DBText>();
            var dts = SelSet.GetEntitys(ll, JSelFilter.MakeFilterTypes("TEXT"))?
                  .OfType<Entity>().Select(xx => xx as DBText).ToList();

            if (dts.Count == 2) //  원본 과 below 2개 이어야 한다.
            {
                dts.ForEach(xx => txts.Add(xx));
            }
            else
            {
                return null;
            }

            // txts 에서 원본 제거
            txts.Remove(txt);
            _dt = txts.First();

            return _dt;
        }

        public static Point3d GetCenter(this DBText text, TextStyle txtStyle)
        {
            Point3d pt = new Point3d();
            Extents2d extents = txtStyle.ExtentsBox(text.TextString, false, true, null);
            var vec = (extents.MaxPoint - extents.MinPoint).GetNormal();
            var dist = extents.MinPoint.GetDistanceTo(extents.MaxPoint);

            var pt1 = extents.MinPoint + vec * dist / 2.0;
            pt = new Point3d(text.Position.X + pt1.X, text.Position.Y + pt1.Y, 0.0);

            return pt;
        }

        //Function
        public static (double, double) GetTextWidthHeight(TextStyle _txtStyle, string str)
        {
            Extents2d extents = _txtStyle.ExtentsBox(str, false, true, null);
            var width = extents.MaxPoint.X - extents.MinPoint.X;
            var height = extents.MaxPoint.Y - extents.MinPoint.Y;

            return (width, height);
        }

        public static double GetWidth(this DBText dbtext)
        {
            if (string.IsNullOrEmpty(dbtext.TextString)) return 0;
            double a = Math.Abs(dbtext.GeometricExtents.MaxPoint.Y - dbtext.GeometricExtents.MinPoint.Y);
            double b = Math.Abs(dbtext.GeometricExtents.MaxPoint.X - dbtext.GeometricExtents.MinPoint.X);
            double angle = new Vector2d(Math.Abs(Math.Cos(dbtext.Rotation)), Math.Abs(Math.Sin(dbtext.Rotation))).GetAngleTo(Vector2d.XAxis);
            double[] t = new double[9]
            {
                 Math.Sin(angle),Math.Cos(angle),0,
                 Math.Cos(angle),Math.Sin(angle),0,
                 0,0,1
            };
            Matrix2d mat = new Matrix2d(t);
            Vector2d vec = mat.Inverse() * new Vector2d(a, b);
            return Math.Abs(vec.X);
        }


        public static String GetIntString(this DBText text)
        {
            var strNum = Regex.Replace(text.TextString, @"\D", "");
            int val = 0;
            var can = int.TryParse(strNum, out val);
            return can ? strNum : null;
        }
        public static int GetIntValue(this DBText text)
        {
            var strNum = Regex.Replace(text.TextString, @"\D", "");
            int val = 0;
            var can = int.TryParse(strNum, out val);
            return can ? val : -1;

        }

        public static Polyline GetPoly(this DBText text)
        {
            // null 체크
            if (text == null) return null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;

            Editor ed = doc.Editor;

            try
            {
                // 텍스트의 실제 경계 상자 (GeometricExtents) 가져오기
                Extents3d extents = text.GeometricExtents;

                // 경계 상자 좌표 직접 사용
                Point3d minPoint = extents.MinPoint;
                Point3d maxPoint = extents.MaxPoint;

                // 텍스트 경계 중심점 계산
                Point3d centerPoint = new Point3d(
                    (minPoint.X + maxPoint.X) / 2.0,
                    (minPoint.Y + maxPoint.Y) / 2.0,
                    0);

                // 텍스트 경계 크기 계산
                double width = maxPoint.X - minPoint.X;
                double height = maxPoint.Y - minPoint.Y;

                // 2배 크기 폴리라인 생성 (중심점 기준)
                // 2배 크기의 폴리라인 좌표 계산
                Point3d outerMinPoint = new Point3d(
                    centerPoint.X - width,
                    centerPoint.Y - height,
                    0);
                Point3d outerMaxPoint = new Point3d(
                    centerPoint.X + width,
                    centerPoint.Y + height,
                    0);

                Polyline outerPline = new Polyline();
                outerPline.AddVertexAt(0, new Point2d(outerMinPoint.X, outerMinPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(1, new Point2d(outerMaxPoint.X, outerMinPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(2, new Point2d(outerMaxPoint.X, outerMaxPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(3, new Point2d(outerMinPoint.X, outerMaxPoint.Y), 0, 0, 0);
                outerPline.Closed = true;

                //// 디버깅 정보 출력
                //ed.WriteMessage($"\n--- 텍스트 정보 ---");
                //ed.WriteMessage($"\n텍스트 내용: {text.TextString}");
                //ed.WriteMessage($"\n경계 상자: 좌하({minPoint.X}, {minPoint.Y}), 우상({maxPoint.X}, {maxPoint.Y})");
                //ed.WriteMessage($"\n중심점: ({centerPoint.X}, {centerPoint.Y})");
                //ed.WriteMessage($"\n텍스트 크기: 가로={width}, 세로={height}");
                //ed.WriteMessage($"\n폴리라인 크기: 가로={width * 2}, 세로={height * 2}");

                return outerPline;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                return null;
            }


        }

        public static Polyline GetDoubleSizePoly(this DBText text)
        {
            // null 체크
            if (text == null) return null;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;

            Editor ed = doc.Editor;

            try
            {
                // 텍스트의 실제 경계 상자 (GeometricExtents) 가져오기
                Extents3d extents = text.GeometricExtents;

                // 경계 상자 좌표 직접 사용
                Point3d minPoint = extents.MinPoint;
                Point3d maxPoint = extents.MaxPoint;

                // 텍스트 경계 중심점 계산
                Point3d centerPoint = new Point3d(
                    (minPoint.X + maxPoint.X) / 2.0,
                    (minPoint.Y + maxPoint.Y) / 2.0,
                    0);

                // 텍스트 경계 크기 계산
                double width = (maxPoint.X - minPoint.X)*1.3;
                double height = maxPoint.Y - minPoint.Y;

                // 2배 크기 폴리라인 생성 (중심점 기준)
                // 2배 크기의 폴리라인 좌표 계산
                Point3d outerMinPoint = new Point3d(
                    centerPoint.X - width,
                    centerPoint.Y - height,
                    0);
                Point3d outerMaxPoint = new Point3d(
                    centerPoint.X + width,
                    centerPoint.Y + height,
                    0);

                Polyline outerPline = new Polyline();
                outerPline.AddVertexAt(0, new Point2d(outerMinPoint.X, outerMinPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(1, new Point2d(outerMaxPoint.X, outerMinPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(2, new Point2d(outerMaxPoint.X, outerMaxPoint.Y), 0, 0, 0);
                outerPline.AddVertexAt(3, new Point2d(outerMinPoint.X, outerMaxPoint.Y), 0, 0, 0);
                outerPline.Closed = true;

                //// 디버깅 정보 출력
                //ed.WriteMessage($"\n--- 텍스트 정보 ---");
                //ed.WriteMessage($"\n텍스트 내용: {text.TextString}");
                //ed.WriteMessage($"\n경계 상자: 좌하({minPoint.X}, {minPoint.Y}), 우상({maxPoint.X}, {maxPoint.Y})");
                //ed.WriteMessage($"\n중심점: ({centerPoint.X}, {centerPoint.Y})");
                //ed.WriteMessage($"\n텍스트 크기: 가로={width}, 세로={height}");
                //ed.WriteMessage($"\n폴리라인 크기: 가로={width * 2}, 세로={height * 2}");

                return outerPline;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                return null;
            }


        }


    }

    public static class JObjectID
    {
        public static DBObject GetObject(this ObjectId objid)
        {
            return objid.GetObject(OpenMode.ForRead);
        }
        public static Entity GetEntity(this ObjectId objid)
        {
            return objid.GetObject(OpenMode.ForRead) as Entity;
        }

    }

    public static class JDouble
    {
        public static string GetWireLenTxt(this double dd) //// (Math.Ceiling(ll.Length * 10 / 1000) / 10).ToString();
        {
            string res = null;
            if (dd == 0.0) return res;
            res = (Math.Round(dd / 1000.0) / 2).ToString(); //(Math.Ceiling(dd * 10 / 1000) / 10).ToString();
            return res;
        }
    }

}

namespace CADExtension //Curve Line Poly Geometry Point
{
    public static class JCurve
    {
        // 2 line에서 가장 가까운 2 Point return 순서는 line1 위의 점 나중이 line2 위의 점
        public static (Point3d, Point3d) FindNearestPointsFromStartEnd(this Curve cur1, Curve cur2)
        {
            var distances = new[]
            {
                (cur1.StartPoint.DistanceTo(cur2.StartPoint), (cur1.StartPoint, cur2.StartPoint)),
                (cur1.StartPoint.DistanceTo(cur2.EndPoint), (cur1.StartPoint, cur2.EndPoint)),
                (cur1.EndPoint.DistanceTo(cur2.StartPoint), (cur1.EndPoint, cur2.StartPoint)),
                (cur1.EndPoint.DistanceTo(cur2.EndPoint), (cur1.EndPoint, cur2.EndPoint))
            };
            var minDistance = distances.OrderBy(d => d.Item1).First();
            return minDistance.Item2;

        }


        public static Line GetEndVectorLine(this Curve curve, Point3d pt)
        {
            // 시작점과 끝점에서의 위치와 방향 벡터 계산
            Point3d startPoint = curve.StartPoint;
            Point3d endPoint = curve.EndPoint;

            Point3d tp = pt;
            if (startPoint.DistanceTo(pt) < endPoint.DistanceTo(pt))
            {
                tp = startPoint;
            }
            else
            {
                tp = endPoint;
            }

            // GetFirstDerivative는 곡선의 접선 벡터를 반환합니다
            Vector3d tpVector = curve.GetFirstDerivative(curve.GetParameterAtPoint(tp));

            // 벡터 정규화
            tpVector = tpVector.GetNormal();

            return new Line(tp, tp + tpVector * 100);

        }

        // 대상 Curve가  제한 각도 내에서 colliner 한지 Check
        public static bool IsCollinearVec(this Curve cur1, Curve cur2, double widthDis = 2.0)
        {
            bool status = false;
            var (p1, p2) = cur1.FindNearestPointsFromStartEnd(cur2);
            var l1 = cur1.GetEndVectorLine(p1);
            var l2 = cur2.GetEndVectorLine(p2);
            if ((l1.Check2LineAngle(l2, 1.0)) &&
               (p1.DistanceTo(p2) < widthDis))
            {
                status = true;
            }
            return status;
        }


        //  대상 Curve의 GetPointAtDist  함수 작성 
        public static Point3d GetPointAtDist(this Curve ent, double dt)
        {
            Point3d midPoint = new Point3d();
            if (ent is Line)
            {
                Line line = ent as Line;
                midPoint = line.StartPoint + (line.EndPoint - line.StartPoint) * dt;
            }
            else if (ent is Arc)
            {
                Arc arc = ent as Arc;
                double param = (arc.StartParam + arc.EndParam) * dt;
                midPoint = arc.GetPointAtParameter(param);
            }
            else if (ent is Polyline)
            {
                Polyline pline = ent as Polyline;
                double length = pline.Length;
                midPoint = pline.GetPointAtDist(length * dt);
            }
            return midPoint;
        }

        // 대상 Curve의 length 구하는 함수 작성 
        public static double GetLength(this Curve ent)
        {
            double dist = 0;
            if (ent is Line)
            {
                Line line = ent as Line;
                dist = line.Length;
            }
            else if (ent is Arc)
            {
                Arc arc = ent as Arc;
                dist = arc.Length;
            }
            else if (ent is Polyline)
            {
                Polyline pline = ent as Polyline;
                dist = pline.Length;
            }
            return dist;
        }

    }

    public static class jLineEntension
    {
        public static Vector3d GetVector(this Line line)
        {
            var vec = line.EndPoint - line.StartPoint;
            return vec;
        }

        public static Point3d GetCentor(this Line line)
        {
            return line.GetPointAtDist(line.Length / 2.0);
        }
        //public static double GetAngle(this Line line ,Line line1)
        //{ 
        //    var  angle = Math.Abs(line.Angle - line1.Angle);
        //    angle = 180.0 * Math.PI / angle;
        //    return angle;   
        //}

        public static Point3d GetInterSectPoint3dFrom2Line(this Line line, Line line1)
        {
            Point3d inter = new Point3d(0, 0, 0);
            // get the intersection
            var pts = new Point3dCollection();
            line.IntersectWith(line1, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count >= 1)
            {
                inter = pts[0];
            }

            if (
                (inter.DistanceTo(line.StartPoint) > 10) ||
                (inter.DistanceTo(line.EndPoint) > 10) ||
                (inter.DistanceTo(line1.StartPoint) > 10) ||
                (inter.DistanceTo(line1.EndPoint) > 10)
                )
            {
                inter = new Point3d(0, 0, 0);
            }
            return inter;
        }

        public static Point3d GetInterSectPoint(this Line line, Line line1)
        {
            Point3d inter = new Point3d(0.0, 0.0, 0.0);
            // get the intersection
            var pts = new Point3dCollection();
            line.IntersectWith(line1, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count >= 1)
            {
                inter = pts[0];
            }

            return inter;
        }


        public static Boolean IsParallel(this Line line, Line line1)
        {
            return line.GetVector().IsParallelTo(line1.GetVector());
        }


        public static bool IsCoLinear1(this Line L1, Line L2)
        {
            var a11 = L2.StartPoint - L1.EndPoint;
            var a12 = L2.StartPoint - L1.StartPoint;

            var a21 = L2.EndPoint - L1.EndPoint;
            var a22 = L2.EndPoint - L1.StartPoint;

            double sumLength = L1.Length + L2.Length;

            // 비교 대상이 너무 멀면 제외 
            if (L1.StartPoint.DistanceTo(L2.StartPoint) > sumLength * 20) return false;

            //--
            double a1Ang = (a11.GetAngleTo(a12, Vector3d.ZAxis.Negate())) * 180.0 / Math.PI;
            double a2Ang = (a21.GetAngleTo(a22, Vector3d.ZAxis.Negate())) * 180.0 / Math.PI;

            if (Math.Abs(a1Ang) > 90.0) a1Ang = Math.Abs(a1Ang) % 180.0;   //180.0 - Math.Abs(a1Ang);
            if (Math.Abs(a2Ang) > 90.0) a2Ang = Math.Abs(a2Ang) % 180.0;   //180.0 - Math.Abs(a2Ang)

            if (Math.Abs(a1Ang) > 90.0) a1Ang = 180.0 - a1Ang;   //180.0 - Math.Abs(a1Ang);
            if (Math.Abs(a2Ang) > 90.0) a2Ang = 180.0 - a2Ang;  //180.0 - Math.Abs(a2Ang)

            if ((a1Ang < 0.01) && (a2Ang < 0.01))
                return true;
            else
                return false;

        }

        // 두 선이 Collinear인지 확인하는 함수 (1도 이하의 예각 기준)
        public static bool IsCoLinear(this Line line1, Line line2)
        {
            Vector3d vec1 = line1.EndPoint - line1.StartPoint;
            Vector3d vec2 = line2.EndPoint - line2.StartPoint;

            // 벡터 크기 계산
            double magnitude1 = vec1.Length;
            double magnitude2 = vec2.Length;

            // 내적을 사용한 각도 계산 (라디안)
            double dotProduct = vec1.DotProduct(vec2);
            double angle = Math.Acos(dotProduct / (magnitude1 * magnitude2)) * (180.0 / Math.PI); // 각도를 도로 변환

            // 예각이 1도 이하인 경우 Collinear로 판단
            return angle <= 1.0 || Math.Abs(angle - 180) <= 1.0;

        }

        // 두 선이 수직인지 확인하는 함수 (89 ~ 91도 범위)
        public static bool IsPerpendicular(this Line line1, Line line2)
        {
            Vector3d vec1 = line1.EndPoint - line1.StartPoint;
            Vector3d vec2 = line2.EndPoint - line2.StartPoint;

            // 벡터 크기 계산
            double magnitude1 = vec1.Length;
            double magnitude2 = vec2.Length;

            // 내적을 사용한 각도 계산 (라디안)
            double dotProduct = vec1.DotProduct(vec2);
            double angle = Math.Acos(dotProduct / (magnitude1 * magnitude2)) * (180.0 / Math.PI); // 각도를 도로 변환

            // 각도가 89 ~ 91도 범위에 있으면 수직으로 판단
            return angle >= 89.0 && angle <= 91.0;

        }




        public static bool IsCoLinearVec(this Line line1, Line line2, double widthDis = 2.0)
        {
            bool status = false;
            var (cp1, cp2) = line1.FindNearestPoints(line2);
            if (cp1.DistanceTo(cp2) > widthDis) return false;
            if (line1.Check2LineAngle(line2, 1.0)) return true;
            return status;
        }


        public static bool IsConnected(this Line L1, Line L2, double delta)
        {
            bool status = false;
            if ((L1.StartPoint.DistanceTo(L2.StartPoint) < delta) ||
                (L1.StartPoint.DistanceTo(L2.EndPoint) < delta) ||
                (L1.EndPoint.DistanceTo(L2.StartPoint) < delta) ||
                (L1.EndPoint.DistanceTo(L2.EndPoint) < delta))
            { status = true; }


            return status;
        }

        public static bool IsContacted(this Line l1, Line l2, double delta)
        {
            // connected 된 2 line은  contacted 되어 있지만
            // Contacted 되었다고 반드시 connected 된 것은 아니다.
            bool status = false;

            var cp = l1.GetInterSectPoint(l2);
            if (cp == new Point3d(0.0, 0.0, 0.0)) return false;

            if ((cp.DistanceTo(l2.StartPoint) < delta) ||
                (cp.DistanceTo(l2.EndPoint) < delta) ||
                (cp.DistanceTo(l1.StartPoint) < delta) ||
                (cp.DistanceTo(l1.EndPoint) < delta))
            { status = true; }

            return status;
        }

        public static double GetAngle(this Line L1, Line L2)
        {
            var a = L1.EndPoint - L1.StartPoint;
            var b = L2.EndPoint - L2.StartPoint;

            var ang = Math.Acos(a.DotProduct(b) / (a.Length * b.Length)) * 180.0 / Math.PI;
            return Math.Abs(ang);
        }

        public static List<Line> GetCrossedLines(this Line L1, string RegName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<Line> lines = new List<Line>();

            if (L1 == null) return lines;
            //List<Line> lines = new List<Line>();
            var delta = L1.Normal * 10.0;

            Point3d sPt = L1.GetPointAtDist(10.0);  // L1.GetPointAtDist(2.0);
            Point3d ePt = L1.GetPointAtDist(L1.Length - 10.0);
            //L1.GetPointAtDist(L1.Length - 2);
            Vector3d perpVec = (ePt - sPt).CrossProduct(L1.Normal).GetNormal();
            Vector3d offset = perpVec * 2;

            var points = new Point3dCollection();
            points.Add(sPt + offset);
            points.Add(sPt - offset);
            points.Add(ePt - offset);
            points.Add(ePt + offset);

            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, JSelFilter.MakeFilterTypesRegs("LINE", RegName));
            SelectionSet ss = psr2.Value;
            var ids1 = new ObjectIdCollection(psr2.Value.GetObjectIds());

            lines = ids1.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();
            lines.Remove(L1);
            return lines;
        }

        // Base line 과 주어진 Line이 지나 가는지 Check
        public static bool IsCrossed(this Line L1, Line L2)
        {
            bool status = false;
            if (!((L1.GetAngle(L2) > 89.0) && (L1.GetAngle(L2) < 91.0))) return false;
            // get the intersection
            Point3d inter = new Point3d();
            var pts = new Point3dCollection();
            L1.IntersectWith(L2, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
            if (pts.Count == 1)
            {
                inter = pts[0];
            }
            else { return false; }




            // L2 Point 중에 interdhk 가까운 point 찾는다
            //Point3d tp;
            double dist = 0;
            if (inter.DistanceTo(L2.StartPoint) < inter.DistanceTo(L2.EndPoint))
            {
                dist = inter.DistanceTo(L2.EndPoint);
            }
            else
            {
                dist = inter.DistanceTo(L2.StartPoint);
            }

            // 
            if (L2.Length >= dist) return true;
            if (L2.Length < dist) return false;

            return status;
        }

        // Base line 기준 offset ploy 생성 
        public static Polyline GetOffsetPloy(this Line L1, double Offset, bool IsInside)
        {
            Polyline poly = new Polyline();

            return poly;
        }

        // Line에 포함된 Points
        public static List<Point3d> GetPoints(this Line L1)
        {
            var pts = new List<Point3d>();
            pts.Add(L1.StartPoint);
            pts.Add(L1.EndPoint);
            return pts;
        }


        // Line에서 일정폭을 가지는 PoyLine 
        public static Polyline GetPoly(this Line l1, double width)
        {
            Polyline poly = new Polyline();

            return poly;
        }

        // line Vector의 직각 Vector 
        public static Vector3d PerpVec(this Line line)
        {
            Vector3d perpVec = (line.EndPoint - line.StartPoint).CrossProduct(line.Normal).GetNormal();
            return perpVec;
        }

        // line 에서 지정된 point 보다 먼 거리에 있는 Point(Start, End 중에서 선택)
        public static Point3d GetOtherPoint(this Line line, Point3d pt)
        {
            Point3d tp = new Point3d();
            if (line.StartPoint.DistanceTo(pt) > line.EndPoint.DistanceTo(pt))
            {
                tp = line.StartPoint;
            }
            else
            {
                tp = line.EndPoint;
            }
            return tp;
        }

        // Get Single line Poly
        public static Polyline GetPolyline(this Line line)
        {
            Polyline ply = new Polyline();
            ply.AddVertexAt(0, new Point2d(line.StartPoint.X, line.StartPoint.Y), 0, 0, 0);
            ply.AddVertexAt(1, new Point2d(line.EndPoint.X, line.EndPoint.Y), 0, 0, 0);
            return ply;
        }

        // Get Line걸펴진 Block GeoCeneter 까지 확장괸 Line 
        public static Line GetBlockEndExtendLine(this Line ln)
        {
            //Line line1 = new Line();
            //Get Block on StartPoin
            // Calc pt1 
            BlockReference br1 = SelSet.GetEntitys(ln.StartPoint, JSelFilter.MakeFilterTypes("INSERT"))?
            .OfType<Entity>()?.Select(x => x as BlockReference)?.ToList()?.First();
            Point3d pt1;
            if (br1 != null)
            {
                var bc1 = br1.GetGeoCenter();
                var u = (ln.StartPoint - ln.EndPoint).GetNormal();
                var v = (bc1 - ln.StartPoint).GetNormal();
                var ang = u.GetAngleTo(v);
                var len1 = bc1.DistanceTo(ln.StartPoint) * Math.Cos(ang);
                pt1 = ln.StartPoint + u * len1;
            }
            else
            {
                pt1 = ln.StartPoint;
            }
            // Calc pt2 
            BlockReference br2 = SelSet.GetEntitys(ln.EndPoint, JSelFilter.MakeFilterTypes("INSERT"))?
            .OfType<Entity>()?.Select(x => x as BlockReference)?.ToList()?.First();

            Point3d pt2;
            if (br2 != null)
            {
                //var br2 = br2s.First() as BlockReference;
                var bc2 = br2.GetGeoCenter();
                var u = (ln.EndPoint - ln.StartPoint).GetNormal();
                var v = (bc2 - ln.EndPoint).GetNormal();
                var ang = u.GetAngleTo(v);
                var len1 = bc2.DistanceTo(ln.EndPoint) * Math.Cos(ang);
                pt2 = ln.EndPoint + u * len1;
            }
            else
            {
                pt2 = ln.EndPoint;
            }
            ////.Cast<Entity>().Select(x => x as BlockReference)?.ToList()?.First();
            //var bc2 = br2.GetGeoCenter();
            //u = (ln.EndPoint - ln.StartPoint).GetNormal();
            //v = (bc2 - ln.EndPoint).GetNormal();
            //ang = u.GetAngleTo(v);
            //len1 = bc2.DistanceTo(ln.EndPoint) * Cos(ang);
            //var pt2 = ln.EndPoint + u * len1;

            Line line1 = new Line(pt1, pt2);

            return line1;
        }

        // 2 line에서 가장 가까운 2 Point return 순서는 line1 위의 점 나중이 line2 위의 점
        public static (Point3d, Point3d) FindNearestPoints(this Line line1, Line line2)
        {
            var distances = new[]
            {
                (line1.StartPoint.DistanceTo(line2.StartPoint), (line1.StartPoint, line2.StartPoint)),
                (line1.StartPoint.DistanceTo(line2.EndPoint), (line1.StartPoint, line2.EndPoint)),
                (line1.EndPoint.DistanceTo(line2.StartPoint), (line1.EndPoint, line2.StartPoint)),
                (line1.EndPoint.DistanceTo(line2.EndPoint), (line1.EndPoint, line2.EndPoint))
            };
            var minDistance = distances.OrderBy(d => d.Item1).First();
            return minDistance.Item2;

        }

        public static bool Check2LineAngle(this Line line1, Line line2, double angleThreshold)
        {
            var (closestPoint1, closestPoint2) = line1.FindNearestPoints(line2);

            // V1, V2 벡터 생성 (L2의 점에서 L1의 시작점과 끝점으로)
            Vector3d v1 = line1.StartPoint - closestPoint2;
            Vector3d v2 = line1.EndPoint - closestPoint2;

            // V11, V22 벡터 생성 (L1의 점에서 L2의 시작점과 끝점으로)
            Vector3d v11 = line2.StartPoint - closestPoint1;
            Vector3d v22 = line2.EndPoint - closestPoint1;

            // 벡터들 간의 각도 계산 (라디안에서 도로 변환)
            double angleV1V2 = v1.GetAngleTo(v2) * (180.0 / Math.PI);
            double angleV11V22 = v11.GetAngleTo(v22) * (180.0 / Math.PI);

            return (angleV1V2 <= angleThreshold) && (angleV11V22 <= angleThreshold);
        }

        // Line 객체에 대한 IsPointOnLine Extension Method
        public static bool IsPointOnLine(this Line line, Point3d point)
        {
            // 라인의 시작점부터 끝점까지의 거리
            double lineLength = line.StartPoint.DistanceTo(line.EndPoint);

            // 시작점부터 검사 지점까지의 거리와 검사 지점부터 끝점까지의 거리의 합
            double distSum = line.StartPoint.DistanceTo(point) + point.DistanceTo(line.EndPoint);

            // 부동소수점 오차를 고려하여 비교
            return Math.Abs(distSum - lineLength) < Tolerance.Global.EqualPoint;
        }

        //line 과 point  사이의 수직 거리   
        public static double ShortestDistanceToPoint(this Line line, Point3d point)
        {
            var pt1 = line.GetClosestPointTo(point, true);
            return  (point.DistanceTo(pt1));
        }

        /// <summary>
        /// 특정 Point가 특정 Line의 확장되지 않은 실제 선분 위에 수직 투영될 수 있는지 확인하는 함수
        /// (이전에 작성한 함수를 여기서 재사용)
        /// </summary>
        public static bool IsPointProjectableOnLine(this Line line,Point3d targetPoint, double tolerance = 1e-6)
        {
            try
            {
                if (line == null)
                    return false;

                // 선분 자체에서만 최근점을 구함 (extend = false)
                Point3d closestOnSegment = line.GetClosestPointTo(targetPoint, false);

                // 연장선을 포함하여 최근점을 구함 (extend = true)
                Point3d closestOnExtended = line.GetClosestPointTo(targetPoint, true);

                // 두 점이 같으면 투영점이 선분 자체에 있음
                double distance = closestOnSegment.DistanceTo(closestOnExtended);
                bool isOnSegment = distance <= tolerance;

                return isOnSegment;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 특정 점에서 라인의 시작점과 끝점으로 이어지는 두 선분 사이의 각도를 계산합니다.
        /// </summary>
        /// <param name="pt1">기준점</param>
        /// <param name="line1">대상 라인</param>
        /// <returns>두 선분 사이의 각도 (도 단위)</returns>
        public static double GetAngleSpEp(this Line line1,Point3d pt1)
        {
            try
            {
                // pt1에서 line1.StartPoint로의 벡터 생성
                Vector3d vector1 = line1.StartPoint - pt1;

                // pt1에서 line1.EndPoint로의 벡터 생성
                Vector3d vector2 = line1.EndPoint - pt1;

                // 벡터의 길이가 0인지 확인 (점이 겹치는 경우)
                if (vector1.Length < 1e-6 || vector2.Length < 1e-6)
                {
                    return 0.0; // 점이 겹치면 각도는 0
                }

                // 벡터를 단위벡터로 정규화
                vector1 = vector1.GetNormal();
                vector2 = vector2.GetNormal();

                // 두 벡터의 내적 계산
                double dotProduct = vector1.DotProduct(vector2);

                // 부동소수점 오차 방지를 위해 범위 제한 (-1.0 ~ 1.0)
                dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));

                // 아크코사인을 사용하여 각도 계산 (라디안)
                double angleInRadians = Math.Acos(dotProduct);

                // 라디안을 도(degree)로 변환
                double angleInDegrees = angleInRadians * 180.0 / Math.PI;

                return angleInDegrees;
            }
            catch (System.Exception)
            {
                // 오류 발생 시 0 반환
                return 0.0;
            }
        }

        /// <summary>
        /// 특정 점에서 라인의 센터 까지의 거리를 계산 .
        /// </summary>
        /// <param name="pt1">기준점</param>
        /// <param name="line1">대상 라인</param>
        /// <returns>두 선분 사이의 각도 (도 단위)</returns>
        public static double GetCenDisTance(this Line line1, Point3d pt1)
        {
            try
            {
                // Line 의 벡터
                Vector3d vector1 = line1.EndPoint - line1.StartPoint;

                Point3d pt = line1.GetCentor();

                return pt1.DistanceTo(pt);
            }
            catch (System.Exception)
            {
                // 오류 발생 시 0 반환
                return 0.0;
            }
        }


    }

    public static class jPolyLineExtension
    {
        public static List<Line> GetCressPolySelectLines(this Polyline poly)
        {
            List<Line> lines = null;
            var ids = SelSet.GetCrossPolygonSelectIDs(poly, JSelFilter.MakeFilterTypes("LINE"));
            lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList();
            return lines;
        }

        public static List<Line> GetLines(this Polyline poly)
        {
            //PolyLine이 Closed 일수 있고 아닐수도 있다.
            int num = 0;
            if (poly.Closed) num = poly.NumberOfVertices;
            if (!poly.Closed) num = poly.NumberOfVertices - 1;
            List<Line> lines = new List<Line>();
            //int n = poly.Closed ? 0 : 1;
            for (int i = 0; i < num; i++)
            {
                //LineSegment2d seg1 = poly.GetLineSegment2dAt(i);
                LineSegment3d ll = poly.GetLineSegmentAt(i);
                Line lll = new Line();
                lll.StartPoint = ll.StartPoint;
                lll.EndPoint = ll.EndPoint;
                lines.Add(lll);
            }
            return lines;
        }
        public static Line Get2PonitLines(this Polyline poly)
        {
            //PolyLine이 Closed 일수 있고 아닐수도 있다.
            int num = 0;
            if (poly.Closed) num = poly.NumberOfVertices;
            if (!poly.Closed) num = poly.NumberOfVertices - 1;
            if (num == 1)
            {
                LineSegment3d ll = poly.GetLineSegmentAt(0);
                Line line1 = new Line(ll.StartPoint, ll.EndPoint);
                return line1;
            }
            List<Line> lines = new List<Line>();
            return null;
        }

        // Ploy 내부 points
        public static List<Point3d> GetPoints(this Polyline poly)
        {
            List<Point3d> pts = new List<Point3d>();
            //int n = poly.Closed ? 0 : 1;
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                //LineSegment2d seg1 = poly.GetLineSegment2dAt(i);
                LineSegment3d ll = poly.GetLineSegmentAt(i);
                //Line lll = new Line();
                //lll.StartPoint = ll.StartPoint;
                //lll.EndPoint = ll.EndPoint;
                pts.Add(ll.StartPoint);
                pts.Add(ll.EndPoint);
            }
            pts = pts.Distinct().ToList();

            return pts;
        }

        public static Point3dCollection GetPointCollections(this Polyline poly)
        {
            Point3dCollection ptcols = new Point3dCollection();
            List<Point3d> pts = new List<Point3d>();//int n = poly.Closed ? 0 : 1;
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                Curve3d seg = null; //Line or Arc
                SegmentType segType = poly.GetSegmentType(i);
                if (segType == SegmentType.Arc)
                    seg = poly.GetArcSegmentAt(i);
                else if (segType == SegmentType.Line)
                    seg = poly.GetLineSegmentAt(i);
                //LineSegment2d seg1 = poly.GetLineSegment2dAt(i);
                //LineSegment3d ll = poly.GetLineSegmentAt(i);
                //Line lll = new Line();
                //lll.StartPoint = ll.StartPoint;
                //lll.EndPoint = ll.EndPoint;
                pts.Add(seg.StartPoint);
                pts.Add(seg.EndPoint);
            }
            pts = pts.Distinct().ToList();

            if (pts.Count == 0) return null;
            pts.Select(x => ptcols.Add(x));

            return ptcols;
        }


        // Ploy 내부에 해당 point를 포함 하는지 Check
        public static bool Contains(this Polyline pl, Point3d p)
        {
            bool status = false;
            double offset = 5.0;
            Vector3d minVec = new Vector3d(-offset, -offset, 0);
            Vector3d maxVec = new Vector3d(offset, offset, 0);

            var exts = pl.GeometricExtents;
            var minPt = exts.MinPoint + minVec;
            var maxPt = exts.MaxPoint + maxVec;

            if ((minPt.X <= p.X) &&
                (maxPt.X >= p.X) &&
                (minPt.Y <= p.Y) &&
                (maxPt.Y >= p.Y))
            {
                return true;
            }
            return status;
        }

        //가대용 line 생성 
        public static List<Line> J_GadeLines(this Polyline poly, double gap)
        {
            List<Line> lines = new List<Line>();

            var plines = poly.GetLines();
            List<Line> plines1 = new List<Line>();
            //Sort line Lenth
            plines = plines.OrderBy(l => l.Length).ThenBy(l => l.StartPoint.X).ToList();
            plines1.Add(plines[0]); plines1.Add(plines[1]);
            plines1 = plines1.OrderBy(l => l.GetCentor().X + l.GetCentor().Y).ToList();

            Line L1 = plines1[0]; Line L2 = plines1[1];
            //L1 L2 사이의 거리 
            var dist = L1.GetPointAtDist(L1.Length / 2.0).DistanceTo(L2.GetPointAtDist(L2.Length / 2.0));
            // L1 L2 Vector 
            var dir = (L2.GetPointAtDist(L2.Length / 2.0) - L1.GetPointAtDist(L1.Length / 2.0)).GetNormal();

            //Get L1sp L1ep  L2sp L2ep
            Point3d L1sp, L1ep, L2sp, L2ep;
            L1sp = L1.StartPoint; L1ep = L1.EndPoint;
            L2sp = L2.StartPoint; L2ep = L2.EndPoint;
            if (L1sp.DistanceTo(L2.EndPoint) < L1sp.DistanceTo(L2.StartPoint))
            {
                L2sp = L2.EndPoint; L2ep = L2.StartPoint;
            }

            lines.Add(L1);
            Point3d sPt, ePt;
            for (var i = 1; i < Math.Truncate(dist / gap); i++)
            {
                sPt = L1sp + dir * gap * i;
                ePt = L1ep + dir * gap * i;
                Line ll = new Line(sPt, ePt);
                lines.Add(ll);
            }
            return lines;
        }

        //Offset ploy
        public static Polyline GetOffsetPloy(this Polyline poly, int offset)
        {
            Polyline pl = null;
            DBObjectCollection curvCols = poly.GetOffsetCurves(offset);
            //idx = 0;
            foreach (DBObject obj in curvCols) // curvCols 가 1개이다. 
            {
                Curve subCurv = obj as Curve; // 이 curve는 poly에서 offset 되었으니 poly 이다. 
                                              //pl1.AddVertexAt(idx, new Point2d(subCurv.StartPoint.X, subCurv.StartPoint.Y), 0.0, 0.0, 0.0);
                                              //idx++;
                pl = subCurv as Polyline;
                //if (subCurv != null)
                //{
                //    btr.AppendEntity(subCurv);
                //    tr.AddNewlyCreatedDBObject(subCurv, true);
                //}
            }

            return pl;
        }

        //Ploy 내부의 Entity 가준으로  걸쳐진 Pipe Line 개체 
        public static List<Line> GetPipes(this Polyline poly)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            List<Point3d> pts = new List<Point3d>();
            //var objids = bref.GetBlockEntities();
            List<Line> lns = new List<Line>();
            ObjectIdCollection ids;

            //if (obj.GetType() == typeof(Line))
            //{
            //    Line ln = obj as Line;
            //    ids = SelSet.SelectByLine(ln, SelSet.WinMode.Cross, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            //    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
            //}

            //if (obj.GetType() == typeof(BlockReference))
            //{
            //    BlockReference br = obj as BlockReference;
            //    var pl1 = br.GetPoly();
            //    ids = SelSet.GetCrossPolygonSelectIDs(pl1, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            //    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
            //}

            //Polyline poly = obj as Polyline;
            //Poly Vertics Point에 있는 Pipe
            pts = poly.GetPoints();
            foreach (var p in pts)
            {
                lns.AddRange(SelSet.GetCrossWindowSelectIDs(p, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"))
                                    .OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
            }

            ids = SelSet.GetCrossPolygonSelectIDs(poly, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());

            lns = lns.Distinct().ToList();
            //var lines = new List<Line>();
            //var pl = bref.GetPoly();
            //var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LINE"));
            //lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList();
            return lns;
        }

        //Poly 에 접하고 있는 line 을 선택
        public static List<Line> GetContactLines(this Polyline poly)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            List<Point3d> pts = new List<Point3d>();
            //var objids = bref.GetBlockEntities();
            List<Line> lns = new List<Line>();
            ObjectIdCollection ids;

            //if (obj.GetType() == typeof(Line))
            //{
            //    Line ln = obj as Line;
            //    ids = SelSet.SelectByLine(ln, SelSet.WinMode.Cross, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            //    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
            //}

            //if (obj.GetType() == typeof(BlockReference))
            //{
            //    BlockReference br = obj as BlockReference;
            //    var pl1 = br.GetPoly();
            //    ids = SelSet.GetCrossPolygonSelectIDs(pl1, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            //    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
            //}

            //Polyline poly = obj as Polyline;
            ids = SelSet.GetCrossPolygonSelectIDs(poly, JSelFilter.MakeFilterTypes("LINE"));
            lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());

            //var lines = new List<Line>();
            //var pl = bref.GetPoly();
            //var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LINE"));
            //lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList();
            return lns;
        }

        // poly와 list lines를 구성하는 모즌 Points 
        public static List<Point3d> GetPointWithLines(this Polyline poly, List<Line> lines)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.AddRange(poly.GetPoints());
            lines.ForEach(x => { pts.Add(x.StartPoint); pts.Add(x.EndPoint); });
            var pts1 = pts.Distinct().ToList();
            return pts1;
        }
        // 2Line Poly 기준 Dia 문자 찾기
        public static DBText GetDimPolyText(this Polyline poly)
        {
            DBText text = new DBText();
            var lines = poly.GetLines();
            if (lines.Count != 2) return null;
            List<Line> dLines = new List<Line>();
            var ln1 = lines[0];
            var ln2 = lines[1];
            Line l1 = null, l2 = null;
            Vector3d dir = new Vector3d();
            Point3d basePt = new Point3d();
            if (lines.Count == 2)
            {
                //Find L1 -> Duct와 연결된 Line
                //     L2 -> L1 과 연결된 L2 
                //var dbObjs = ids.OfType<ObjectId>().ToList().Select(x => x.GetObject(OpenMode.ForRead)).ToList();
                //var ents1 = SelSet.GetEntitys(oPt, JSelFilter.MakeFilterTypes("LINE,ARC,LWPOLYLINE,INSERT"))
                var lns1s = SelSet.GetEntitys(ln1.StartPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,Duct"))?
                                  .OfType<Entity>().Select(x => x as Line).ToList();
                if (lns1s != null) dLines.AddRange(lns1s);

                var lns1e = SelSet.GetEntitys(ln1.EndPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,Duct"))?
                                  .OfType<Entity>().Select(x => x as Line).ToList();
                if (lns1e != null) dLines.AddRange(lns1e);

                var lns2s = SelSet.GetEntitys(ln2.StartPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,Duct"))?
                                   .OfType<Entity>().Select(x => x as Line).ToList();
                if (lns2s != null) dLines.AddRange(lns2s);

                var lns2e = SelSet.GetEntitys(ln2.EndPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,Duct"))?
                                   .OfType<Entity>().Select(x => x as Line).ToList();
                if (lns2e != null) dLines.AddRange(lns2e);


                if (((lns1s.Count == 1) || (lns1e.Count == 1)) &&
                   ((lns2s.Count == 0) && (lns2e.Count == 0)))
                { l1 = lines[0]; l2 = lines[1]; }
                if (((lns2s.Count == 1) || (lns2e.Count == 1)) &&
                   ((lns1s.Count == 0) && (lns1e.Count == 0)))
                { l1 = lines[1]; l2 = lines[0]; }
                if (l2 == null) return null;

                //Find l2 방향 
                //var lc2 = SelSet.GetEntitys(l2.StartPoint, JSelFilter.MakeFilterTypes("LINE"))?
                //           .OfType<Entity>().Select(x => x as Line).ToList();
                //if (lc2.Count == 2)
                //{
                //    dir = (l2.EndPoint - l2.StartPoint).GetNormal();
                //    basePt = l2.StartPoint;
                //}
                //else
                //{
                //    dir = -(l2.StartPoint - l2.EndPoint).GetNormal();
                //    basePt = l2.EndPoint;
                //}
                if ((l2.StartPoint.DistanceTo(l1.StartPoint) < 1.0) || (l2.StartPoint.DistanceTo(l1.EndPoint) < 1.0))
                {
                    dir = (l2.EndPoint - l2.StartPoint).GetNormal();
                    basePt = l2.EndPoint;
                }
                else
                {
                    dir = -(l2.EndPoint - l2.StartPoint).GetNormal();
                    basePt = l2.StartPoint;
                }

                //dir = (l2.EndPoint - l2.StartPoint)/l2.Length;


            }
            // l2 양 방향 길이 확장된 Line 찾기
            //Line ll2 = new Line(l2.StartPoint-dir*l2.Length*0.5, l2.EndPoint + dir * l2.Length * 0.5);
            var u = dir;
            var prep = l2.PerpVec();
            var l = l2.Length;
            var mp = basePt;
            //Line ll2 = new Line(basePt, basePt + dir * l2.Length * 3);
            Line ll2 = new Line(mp, mp + 2 * u * l + prep * l * 0.4);
            //Find Dia Text
            //var ids1 = SelSet.GetCrossPolygonSelectIDs(pl2, JSelFilter.MakeFilterTypes("TEXT"));
            var ids1 = SelSet.GetEntitys(ll2, JSelFilter.MakeFilterTypes("TEXT"));
            if (ids1.Count == 0) return null;
            text = ids1.OfType<Entity>().Select(x => x as DBText).ToList().First();
            return text;
        }
        //
        public static DBText GetDimPolyText1(this Polyline poly) // 지시선 poly가 Multi이면 
        {
            DBText text = new DBText();
            List<Line> lines = poly.GetLines();
            //if (lines.Count != 2) return null;
            List<Line> dLines = new List<Line>();
            // line 중에 가장 길이가 짧은 Line 이 ln2
            // ln2 와 연결된 Line이 ln1 으로 설정 
            lines = lines.OrderBy(x => x.Length).ToList(); // 길이로 정열 
            Line ln2 = lines.First(); //lines.OrderBy(x => x.Length).ToList().First();  //lines[1];
            Line ln1 = lines.Last(); // null
            //foreach (var ll in lines)
            //{
            //    if ((ln2.StartPoint.DistanceTo(ll.StartPoint) < 1.0) ||
            //        (ln2.StartPoint.DistanceTo(ll.EndPoint) < 1.0) ||
            //        (ln2.EndPoint.DistanceTo(ll.StartPoint) < 1.0) ||
            //        (ln2.EndPoint.DistanceTo(ll.EndPoint) < 1.0))
            //    {
            //        ln1 = ll;
            //    }

            //}
            if (ln1 == null) return null;
            //var ln1 = lines[0];

            Vector3d dir = new Vector3d();
            Point3d basePt = new Point3d();
            Line l1 = ln1;
            Line l2 = ln2;
            if ((l2.StartPoint.DistanceTo(l1.StartPoint) < 1.0) || (l2.StartPoint.DistanceTo(l1.EndPoint) < 1.0))
            {
                dir = (l2.EndPoint - l2.StartPoint).GetNormal();
                basePt = l2.EndPoint;
            }
            else
            {
                dir = -(l2.EndPoint - l2.StartPoint).GetNormal();
                basePt = l2.StartPoint;
            }


            // l2 양 방향 길이 확장된 Line 찾기
            //Line ll2 = new Line(l2.StartPoint-dir*l2.Length*0.5, l2.EndPoint + dir * l2.Length * 0.5);
            var u = dir;
            var prep = l2.PerpVec();
            var l = l2.Length;
            var mp = basePt;
            //Line ll2 = new Line(basePt, basePt + dir * l2.Length * 3);
            Line ll2 = new Line(mp, mp + 2 * u * l + prep * l);//prep * l * 0.4
            //Find Dia Text
            //var ids1 = SelSet.GetCrossPolygonSelectIDs(pl2, JSelFilter.MakeFilterTypes("TEXT"));
            var ids1 = SelSet.GetEntitys(ll2, JSelFilter.MakeFilterTypes("TEXT"));
            if (ids1.Count == 0) return null;
            text = ids1.OfType<Entity>().Select(x => x as DBText).ToList().First();
            return text;
        }

        public static void ModifyByBlockCenter(this Polyline poly)
        {
            poly.UpgradeOpen();
            //Start Point 에 걸쳐진 Block 
            BlockReference br1 = SelSet.GetEntitys(poly.StartPoint, JSelFilter.MakeFilterTypes("INSERT"))?
            .OfType<Entity>()?.Select(x => x as BlockReference)?.ToList()?.First();
            if (br1 != null)
            {
                if (br1.GetArea() < 1000000.0)
                {
                    var br1cp = br1.GetGeoCenter();
                    Point2d pt1 = new Point2d(br1cp.X, br1cp.Y);
                    poly.SetPointAt(0, pt1);
                }
            }

            //End Point 에 걸쳐진 Block 
            BlockReference br2 = SelSet.GetEntitys(poly.EndPoint, JSelFilter.MakeFilterTypes("INSERT"))?
            .OfType<Entity>()?.Select(x => x as BlockReference)?.ToList()?.First();
            if (br2 != null)
            {
                if (br2.GetArea() < 1000000.0)
                {
                    var br1cp = br2.GetGeoCenter();
                    Point2d pt1 = new Point2d(br1cp.X, br1cp.Y);
                    poly.SetPointAt(poly.NumberOfVertices - 1, pt1);
                }
            }

        }

        ////특정 Point 가 PolyLine 내부에 있는지 Check From _gile
        //// https://forums.autodesk.com/t5/net/how-to-check-if-a-point2d-is-inside-a-polyline-or-not/td-p/8234741
        //// Helper method to check if a point is within a closed polyline using ray-casting
        //public static bool ContainsPoint(this Polyline pline, Point3d point)
        //{
        //    double tolerance = Tolerance.Global.EqualPoint;
        //    using (MPolygon mpg = new MPolygon())
        //    {
        //        mpg.AppendLoopFromBoundary(pline, true, tolerance);
        //        return mpg.IsPointInsideMPolygon(point, tolerance).Count == 1;
        //    }
        //}

        //Get Poly Center
        public static Point3d  CenterPoint(this Polyline pline)
        {
            var exts = pline.GeometricExtents;
            var u = exts.MaxPoint - exts.MinPoint;
            return exts.MinPoint + u.GetNormal() * u.Length / 2;
        }

        //
        /// <summary>
        /// Polyline 내부의 Line segment들을 Line Entity로 변환하여 반환하는 확장 함수
        /// </summary>
        /// <param name="polyline">대상 Polyline 객체</param>
        /// <returns>Line Entity들의 리스트</returns>
        public static List<Line> GetLineEntities(this Polyline polyline)
        {
            var lineEntities = new List<Line>();

            if (polyline == null || polyline.NumberOfVertices < 2)
                return lineEntities;

            try
            {
                // Polyline의 segment 개수 계산
                // Closed polyline: NumberOfVertices개의 segment
                // Open polyline: NumberOfVertices - 1개의 segment
                int segmentCount = polyline.Closed ? polyline.NumberOfVertices : polyline.NumberOfVertices - 1;

                for (int i = 0; i < segmentCount; i++)
                {
                    // 각 segment의 타입 확인
                    SegmentType segmentType = polyline.GetSegmentType(i);

                    // Line segment인 경우에만 처리
                    if (segmentType == SegmentType.Line)
                    {
                        try
                        {
                            // LineSegment3d 정보 가져오기 (World Coordinates)
                            LineSegment3d lineSegment3d = polyline.GetLineSegmentAt(i);

                            // LineSegment3d에서 시작점과 끝점 추출
                            Point3d startPoint = lineSegment3d.StartPoint;
                            Point3d endPoint = lineSegment3d.EndPoint;

                            // Line Entity 생성
                            Line lineEntity = new Line(startPoint, endPoint);

                            //// 원본 Polyline의 속성 복사
                            //CopyEntityProperties(polyline, lineEntity);

                            lineEntities.Add(lineEntity);
                        }
                        catch (System.Exception)
                        {
                            // 개별 segment 처리 실패 시 건너뛰고 계속 진행
                            continue;
                        }
                    }
                    // Arc segment나 기타 타입은 무시하고 Line segment만 처리
                }
            }
            catch (System.Exception)
            {
                // 전체 처리 실패 시 빈 리스트 반환
                return new List<Line>();
            }

            return lineEntities;
        }

    }

    public static class jGeoExtens3dExtension
    {
        public static Polyline GetPoly(this Extents3d ext)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Polyline pl = new Polyline(4);
            Point3d p1 = ext.MinPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            Point3d p3 = ext.MaxPoint.TransformBy(ed.CurrentUserCoordinateSystem);
            Point3d p2 = new Point3d(p3.X, p1.Y, p1.Z).TransformBy(ed.CurrentUserCoordinateSystem);
            Point3d p4 = new Point3d(p1.X, p3.Y, p1.Z).TransformBy(ed.CurrentUserCoordinateSystem);

            pl.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(3, new Point2d(p4.X, p4.Y), 0.0, 0.0, 0.0);

            pl.Closed = true;

            return pl;
        }

    }

    public static class jPointExtension
    {

        // Create Poly
        public static Polyline GetPoly(this Point3d po, double offset)
        {
            Polyline pl = new Polyline(4);
            Point2d p1 = new Point2d(po.X - offset, po.Y - offset);
            Point2d p3 = new Point2d(po.X - offset, po.Y + offset);
            Point2d p2 = new Point2d(po.X + offset, po.Y + offset);
            Point2d p4 = new Point2d(po.X + offset, po.Y - offset);
            Point2d p5 = new Point2d(p1.X, p1.Y);
            //Point3d p5 = ext.MinPoint.TransformBy(ed.CurrentUserCoordinateSystem);


            pl.AddVertexAt(0, p1, 0.0, 0.0, 0.0);
            pl.AddVertexAt(1, p2, 0.0, 0.0, 0.0);
            pl.AddVertexAt(2, p3, 0.0, 0.0, 0.0);
            pl.AddVertexAt(3, p4, 0.0, 0.0, 0.0);
            pl.AddVertexAt(4, p5, 0.0, 0.0, 0.0);


            return pl;
        }

        // 각도가 90도 이상이면 180 도에서 해당 각을 빼서 출력 
        public static double ShortAngle(this Point3d po, Point3d p1)
        {
            double ang = 0.0;

            Vector3d v1 = new Vector3d(po.X, po.Y, 0.0);
            Vector3d v2 = new Vector3d(p1.X, p1.Y, 0.0);

            ang = v1.GetAngleTo(v2);
            if (ang > Math.PI / 2) ang = Math.PI - ang;

            return ang * 180.0 / Math.PI;
        }

        public static double ShortAngleHor(this Point3d po, Point3d p1)
        {
            double ang = 0.0;

            Vector3d v1 = new Vector3d(p1.X - po.X, -po.Y, 0.0);
            Vector3d hor = new Vector3d(1.0, 0.0, 0.0);

            ang = v1.GetAngleTo(hor);
            if (ang > Math.PI / 2) ang = Math.PI - ang;

            return ang * 180.0 / Math.PI;
        }


        public static Line FindClosestLine(this Point3d pt, List<Line> lines)
        {
            if ((lines == null) || (lines.Count == 0)) return null;
            var closet = lines.Select(ll => new { Line = ll, Dist = pt.DistanceTo(ll.GetClosestPointTo(pt, false)) })
                                         .Aggregate((l1, l2) => l1.Dist < l2.Dist ? l1 : l2).Line;
            return closet;

        }


    }
}

namespace CADExtension  //tr Editor Block 
{
    public static class TransactionExtension
    {
        public static BlockTableRecord GetModelSpaceBlockTableRecord(this Transaction tr, Database db)
        {
            BlockTableRecord btr = null;
            // Open the Block table for read
            BlockTable acBlkTbl;
            acBlkTbl = tr.GetObject(db.BlockTableId,
                                        OpenMode.ForRead) as BlockTable;

            // Open the Block table record Model space for write
            btr = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                            OpenMode.ForWrite) as BlockTableRecord;


            return btr;
        }

        public static void ChecRegNames(this Transaction tr, Database db, string regAppNams) // regApp Name은 "Dia,Pipe 을 멀티로
        {
            string[] regNames = regAppNams.Split(',');
            RegAppTable rat = db.RegAppTableId.GetObject(OpenMode.ForRead) as RegAppTable;
            //(RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false);
            foreach (var reg in regNames)
            {
                if (!rat.Has(reg))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr =
                      new RegAppTableRecord();
                    ratr.Name = reg;
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
            }

        }

        //Check regNames
        public static void CheckRegName(this Transaction tr, string regAppName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            RegAppTable regAppTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);

            string[] appNames = regAppName.Split(',');

            foreach (string appName in appNames)
            {
                if (!regAppTable.Has(appName))
                {
                    regAppTable.UpgradeOpen();
                    RegAppTableRecord regAppTableRecord = new RegAppTableRecord();
                    regAppTableRecord.Name = appName;
                    regAppTable.Add(regAppTableRecord);
                    tr.AddNewlyCreatedDBObject(regAppTableRecord, true);
                    ed.WriteMessage($"\n레지스터 애플리케이션 '{appName}'이(가) 생성되었습니다.");
                }
            }
        }


        //Create Layer 
        public static bool CreateLayer(this Transaction tr, string layerName, short colorIdx, LineWeight lineWeight)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            //Editor ed = doc.Editor;


            //Database db = HostApplicationServices.WorkingDatabase;
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            Color color = Color.FromColorIndex(ColorMethod.ByAci, colorIdx);

            if (lt.Has(layerName))
            {
                //throw new System.Exception($"Layer '{layerName}' already exists.");
                return false;
            }

            //using (Transaction tr1 = db.TransactionManager.StartTransaction())
            //{
            LayerTableRecord ltr = new LayerTableRecord();
            ltr.Name = layerName;
            ltr.Color = color;
            ltr.LineWeight = lineWeight;

            lt.UpgradeOpen();
            ObjectId layerId = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            //tr.Commit();   
            return true;
        }

    }
    public static class EditorExtension
    {
        public static void Zoom(this Editor ed, Extents3d ext)
        {
            if (ed == null)
                throw new ArgumentNullException("ed");
            using (ViewTableRecord view = ed.GetCurrentView())
            {
                Matrix3d worldToEye = Matrix3d.WorldToPlane(view.ViewDirection) *
                    Matrix3d.Displacement(Point3d.Origin - view.Target) *
                    Matrix3d.Rotation(view.ViewTwist, view.ViewDirection, view.Target);
                ext.TransformBy(worldToEye);
                view.Width = ext.MaxPoint.X - ext.MinPoint.X;
                view.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
                view.CenterPoint = new Point2d(
                    (ext.MaxPoint.X + ext.MinPoint.X) / 2.0,
                    (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
                ed.SetCurrentView(view);
            }
        }

        public static void ZoomExtents(this Editor ed)
        {
            Database db = ed.Document.Database;
            db.UpdateExt(false);
            Extents3d ext = (short)Application.GetSystemVariable("cvport") == 1 ?
                new Extents3d(db.Pextmin, db.Pextmax) :
                new Extents3d(db.Extmin, db.Extmax);
            ed.Zoom(ext);
        }

        // Get string from user Input
        public static string GetStringInput(this Editor ed, string msg)
        {
            string str = null;
            // Set up the prompt options
            PromptStringOptions pso = new PromptStringOptions($"\n{msg}");
            pso.AllowSpaces = false;      // 공백 입력 허용 않음
            pso.UseDefaultValue = false;  // 기본값 사용하지 않음
                                          // Prompt for string input

            PromptResult result = ed.GetString(pso);
            if (result.Status == PromptStatus.OK)
            {
                string input = result.StringResult;

                if (!string.IsNullOrEmpty(input))
                {
                    ed.WriteMessage("\n입력된 문자열: {0}", input);
                    ed.WriteMessage("\n문자열 길이: {0}", input.Length);
                    str = input;
                }
                else
                {
                    ed.WriteMessage("\n문자열이 입력되지 않았습니다.");
                }
            }
            else
            {
                ed.WriteMessage("\n입력이 취소되었습니다.");
            }

            return str;
        }

    }
    public static class jBlockReferenceExtension
    {
        public static Polyline GetPoly(this BlockReference bref)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            if (bref.IsDynamicBlock)
            {
                var aa = bref.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;

                //this.Name = (bref.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord).Name;

            }

            double offset = 20.0;
            Vector3d minVec = new Vector3d(-offset, -offset, 0);
            Vector3d maxVec = new Vector3d(offset, offset, 0);

            //Polyline po = new Polyline();
            BlockReference clon = null;
            clon = bref.Clone() as BlockReference;
            clon.Rotation = 0;
            clon.TransformBy(ed.CurrentUserCoordinateSystem);

            //tr.TransactionManager.QueueForGraphicsFlush();
            //Extents3d ext = clon.GeometryExtentsBestFit(ed.CurrentUserCoordinateSystem);
            Extents3d ext = clon.GeometricExtents; //GeometryExtentsBestFit(ed.CurrentUserCoordinateSystem);
            //Extents3d ext = clon.G;
            ext.TransformBy(ed.CurrentUserCoordinateSystem);
            Polyline pl = new Polyline(4);
            Point3d p1 = ext.MinPoint.TransformBy(ed.CurrentUserCoordinateSystem) + minVec;
            Point3d p3 = ext.MaxPoint.TransformBy(ed.CurrentUserCoordinateSystem) + maxVec;
            Point3d p2 = new Point3d(p3.X, p1.Y, 0.0).TransformBy(ed.CurrentUserCoordinateSystem);
            Point3d p4 = new Point3d(p1.X, p3.Y, 0.0).TransformBy(ed.CurrentUserCoordinateSystem);
            Point3d p5 = new Point3d(p1.X, p1.Y, 0);
            //Point3d p5 = ext.MinPoint.TransformBy(ed.CurrentUserCoordinateSystem);


            pl.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(3, new Point2d(p4.X, p4.Y), 0.0, 0.0, 0.0);
            pl.AddVertexAt(4, new Point2d(p5.X, p4.Y), 0.0, 0.0, 0.0);
            //pl.AddVertexAt(4, new Point2d(p5.X, p5.Y), 0.0, 0.0, 0.0);

            pl.Closed = true;
            pl.ColorIndex = 121;

            //pl.GetOffsetCurves(2.0);

            clon.Dispose();
            pl.TransformBy(ed.CurrentUserCoordinateSystem);
            Matrix3d rot = Matrix3d.Rotation(bref.Rotation, bref.Normal.GetNormal(), bref.Position);
            pl.TransformBy(rot);
            return pl;
        }



        public static List<Point3d> GetBlockPoints(this BlockReference bref)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            List<Point3d> pts = new List<Point3d>();
            var objids = bref.GetBlockEntities();
            foreach (DBObject obj in objids)
            {
                if (obj.GetType() == typeof(Line))
                {
                    Line ln = obj as Line;
                    pts.Add(ln.StartPoint);
                    pts.Add(ln.EndPoint);
                }

                if (obj.GetType() == typeof(BlockReference))
                {
                    BlockReference br = obj as BlockReference;
                    var bpts = br.GetBlockPoints();
                    pts.AddRange(bpts);

                }
                if (obj.GetType() == typeof(Polyline))
                {
                    Polyline poly = obj as Polyline;
                    var ppts = poly.GetPoints();
                    pts.AddRange(ppts);
                }


            }

            //Polyline pl = new Polyline(4);

            return pts;
        }
        public static List<Point3d> GetBlockOffSetPoints(this BlockReference bref, double offset)
        {
            Random rdm = new Random();
            var pts = bref.GetBlockPoints();
            List<Point3d> pts1 = new List<Point3d>();
            //double rsc = 1;
            foreach (var pt in pts)
            {

                pts1.Add(new Point3d(pt.X + offset * rdm.Next(0, 100) / 100, pt.Y + offset * rdm.Next(0, 100) / 100, 0.0));
                pts1.Add(new Point3d(pt.X - offset * rdm.Next(0, 100) / 100, pt.Y + offset * rdm.Next(0, 100) / 100, 0.0));
                pts1.Add(new Point3d(pt.X - offset * rdm.Next(0, 100) / 100, pt.Y - offset * rdm.Next(0, 100) / 100, 0.0));
                pts1.Add(new Point3d(pt.X + offset * rdm.Next(0, 100) / 100, pt.Y - offset * rdm.Next(0, 100) / 100, 0.0));
            }


            //Polyline pl = new Polyline(4);

            return pts1;
        }

        public static DBObjectCollection GetBlockEntities(this BlockReference bref)
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            //Database db = doc.Database;
            //Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            //public DBObject[] GetBlockEntities(ObjectId blockId)
            //{
            //    // Get the current document and database, and start a transaction

            List<ObjectId> objectIds = new List<ObjectId>();

            // Collect our exploded objects in a single collection



            DBObjectCollection objs = new DBObjectCollection();
            bref.Explode(objs);

            //DBObject[] dbObjectCollection = null;
            //DBObjectCollection coll = new DBObjectCollection();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{



            //    ////BlockReference bref = trx.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
            //    //BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
            //    //foreach (ObjectId id in btr)
            //    //{
            //    //    //ed.WriteMessage("{0} - {1}\n", id.ObjectClass.DxfName, id.ToString());
            //    //    objectIds.Add(id);

            //    //}
            //}

            return objs;
        }

        public static List<Entity> GetBlockEntities1(this BlockReference bref)
        {
            // Get the current document and database, and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            // Change every entity to be of color ByBlock

            // Open the entity using a transaction
            List<Entity> ents = new List<Entity>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead);

                // Iterate through the BlockTableRecord contents

                foreach (ObjectId id in btr)
                {
                    ents.Add((Entity)tr.GetObject(id, OpenMode.ForWrite));
                    //// Open the entity
                    //Entity ent2 =(Entity)tr.GetObject(id, OpenMode.ForWrite);
                    //// Change each entity's color to ByBlock
                    //ent2.Color =Color.FromColorIndex(ColorMethod.ByBlock, 0);
                }
                tr.Commit();
            }
            return ents;
        }

        // Block 에 걸쳐진 line을 찾는다.
        public static List<Line> GetLines(this BlockReference bref)
        {
            var lines = new List<Line>();
            var pl = bref.GetPoly();
            var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LINE"));
            lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();
            return lines;
        }

        // Block 에 걸쳐진 Wire(LINE,Poly)을 찾는다.
        public static List<Entity> GetWires(this BlockReference bref)
        {
            var ents = new List<Entity>();
            var pl = bref.GetPoly();
            var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypesRegs("LINE,LWPOLYLINE,ARC", "Wire"));
            ents = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Entity).ToList();
            if (ents.Count == 0) return new List<Entity>();
            return ents;
        }

        // Block 에 걸쳐진 line Poly Arc을 찾는다.
        public static List<Entity> GetLinePolyArcs(this BlockReference bref)
        {
            var ents = new List<Entity>();
            var pl = bref.GetPoly();
            var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LINE,LWPOLYLINE,ARC"));
            ents = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Entity).ToList();
            return ents;
        }


        //Block(벽부등 에 걸친 Wire Num을 출력 2-2-2  2-2-2-3
        public static string GetWireSpec(this BlockReference bref)
        {
            string res = null;
            var ents = new List<Entity>();
            var pl = bref.GetPoly();
            var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypesRegs("LINE,LWPOLYLINE,ARC", "Wire"));
            ents = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Entity).ToList();
            if (ents.Count == 0) return res;
            List<String> specs = new List<string>();
            ents.ForEach(x => specs.Add(JXdata.GetXdata(x, "wNum")));
            specs = specs.OrderBy(x => x).ToList();
            foreach (var sp in specs)
            {
                if (sp == null) continue;
                res = res + sp + "-";
            }
            return res;
        }


        public static List<Polyline> GetPolys(this BlockReference bref)
        {
            var lines = new List<Polyline>();
            var pl = bref.GetPoly();
            var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LWPOLYLINE"));
            lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Polyline).ToList();
            return lines;
        }

        //Block 내부의 Entity 가준으로  걸쳐진 Pipe Line 개체 
        public static List<Line> GetPipes(this BlockReference bref)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            List<Point3d> pts = new List<Point3d>();
            var objids = bref.GetBlockEntities();
            List<Line> lns = new List<Line>();
            ObjectIdCollection ids;
            foreach (DBObject obj in objids)
            {
                if (obj.GetType() == typeof(Line))
                {
                    Line ln = obj as Line;
                    ids = SelSet.SelectByLine(ln, SelSet.WinMode.Cross, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
                    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
                }

                if (obj.GetType() == typeof(BlockReference))
                {
                    BlockReference br = obj as BlockReference;
                    var pl1 = br.GetPoly();
                    ids = SelSet.GetCrossPolygonSelectIDs(pl1, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
                    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
                }
                if (obj.GetType() == typeof(Polyline))
                {
                    Polyline poly = obj as Polyline;
                    ids = SelSet.GetCrossPolygonSelectIDs(poly, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
                    lns.AddRange(ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList());
                }


            }


            //var lines = new List<Line>();
            //var pl = bref.GetPoly();
            //var ids = SelSet.GetCrossPolygonSelectIDs(pl, JSelFilter.MakeFilterTypes("LINE"));
            //lines = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForWrite) as Line).ToList();
            return lns;
        }

        //Block min Max Point


        // Block 내부에 해당 point를 포함 하는지 Check
        public static bool Contains(this BlockReference bref, Point3d p)
        {
            bool status = false;
            double offset = 5.0;
            Vector3d minVec = new Vector3d(-offset, -offset, 0);
            Vector3d maxVec = new Vector3d(offset, offset, 0);

            var exts = bref.GeometricExtents;
            var minPt = exts.MinPoint + minVec;
            var maxPt = exts.MaxPoint + maxVec;

            if ((minPt.X <= p.X) &&
                (maxPt.X >= p.X) &&
                (minPt.Y <= p.Y) &&
                (maxPt.Y >= p.Y))
            {
                return true;
            }
            return status;
        }

        // Block 내부에 해당 point를 포함 하는지 Check
        public static Point3d GetGeoCenter(this BlockReference bref)
        {
            var exts = bref.GeometricExtents;
            var u = exts.MaxPoint - exts.MinPoint;
            var h = u.GetNormal() * u.Length * 0.5;
            return exts.MinPoint + h;
        }

        public static Point3d GetGeoCenter1(this BlockReference bref)
        {
            Point3d? cp = null;
            var bounds = bref.Bounds;
            if (bounds.HasValue)
            {
                var ext = bounds.Value;
                var u = ext.MaxPoint - ext.MinPoint;
                var h = u.GetNormal() * u.Length * 0.5;
                return ext.MinPoint + h;
            }
            return cp.Value;

        }

        public static (double width, double height) GetBlockWidthHeight(this BlockReference bref)
        {
            //var db = HostApplicationServices.WorkingDatabase;
            double width = 100;
            double height = 100;
            //using (var tr = db.TransactionManager.StartTransaction())
            //{
            //    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            Extents3d? bounds;
            bounds = bref.Bounds;

            if (bounds.HasValue)
            {
                var ext = bounds.Value;
                width = ext.MaxPoint.X - ext.MinPoint.X;
                height = ext.MaxPoint.Y - ext.MinPoint.Y;
            }
            //    else
            //    {
            //        var bref1 = new BlockReference(bref.Position, bt[bref.Name]);
            //        bounds = bref1.Bounds;
            //        var ext = bounds.Value;
            //        width = ext.MaxPoint.X - ext.MinPoint.X;
            //        height = ext.MaxPoint.Y - ext.MinPoint.Y;
            //        bref1.Dispose();
            //    }
            //    tr.Commit();
            //}

            return (width, height);
        }


        // Block 내부 Entity Color -> bylayer로 변경 
        public static void SetEntityBylayer(this BlockReference bref, BlockTableRecord btr)
        {
            var ents = bref.GetBlockEntities1();
            foreach (Entity ent in ents)
            {
                //var ent = obj as Entity;
                ent.UpgradeOpen();
                ent.Layer = "j_DimGuide";
                ent.ColorIndex = 256;
                ent.LineWeight = LineWeight.ByLayer;
                ent.UpgradeOpen();
                btr.AppendEntity(ent);
            }
        }
        //
        public static void SetJDimLayer(this BlockReference bref, BlockTableRecord btr)
        {
            // Get the current document and database, and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            //Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            // Change every entity to be of color ByBlock

            // Open the entity using a transaction

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr1 = (BlockTableRecord)tr.GetObject(bref.BlockTableRecord, OpenMode.ForRead);

                // Iterate through the BlockTableRecord contents

                foreach (ObjectId id in btr1)
                {
                    // Open the entity
                    Entity ent2 = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    // Change each entity's color to ByBlock
                    //ent2.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                    ent2.Layer = "!_j_DimGuide";
                    ent2.ColorIndex = 256;
                    ent2.LineWeight = LineWeight.ByLayer;
                }
                tr.Commit();
            }
            //foreach (Entity ent in ents)
            //{
            //    //var ent = obj as Entity;
            //    ent.UpgradeOpen();
            //    ent.Layer = "j_DimGuide";
            //    ent.ColorIndex = 256;
            //    ent.LineWeight = LineWeight.ByLayer;
            //    ent.UpgradeOpen();
            //    btr.AppendEntity(ent);
            //}
        }

        //Get Geo Area
        public static double GetArea(this BlockReference bref)
        {
            double area = 0;
            var ext = bref.GeometricExtents;
            var width = ext.MaxPoint.X - ext.MinPoint.X;
            var height = ext.MaxPoint.Y - ext.MinPoint.Y;
            area = Math.Abs(width * height);
            return area;

        }

        //Get Block Name  
        // AutoCad는 Dynmaic Block이 있을 수있기 떄문에  BlockReference 에서 직접  .Name 사용하지 않고 
        // BlockReference의 BlockTableRecord를 이용해서 Block 이름을 취듳한다.
        public static string GetName(this BlockReference bref)
        {
            string res = "";
            ObjectId btrId = new ObjectId();
            BlockTableRecord btr = new BlockTableRecord();
            if (bref.IsDynamicBlock)
            {
                btrId = bref.DynamicBlockTableRecord;
                btr = btrId.GetObject(OpenMode.ForRead) as BlockTableRecord;
            }
            else
            {
                btrId = bref.BlockTableRecord;
                btr = btrId.GetObject(OpenMode.ForRead) as BlockTableRecord;
            }

            res = btr.Name;
            return res;
        }

        //SetIsWire
        //Br이 전선관 구분이면  연결된 line poly에  Block PartNmae을 설정
        public static bool IsWireSet(this BlockReference br, string partName) //status True False
        {
            bool res = false;
            //var lines = br.GetLines();
            //var polys = br.GetPolys();
            var ents = br.GetWires();//.GetLinePolyArcs();
            //Set IsWire lines and polys 
            foreach (var en in ents)
            {
                en.UpgradeOpen();
                JXdata.SetXdata(en, "IsWire", partName);
                res = true;
            }

            ////Set IsWire lines and polys 
            //foreach (Line line in lines)
            //{
            //    line.UpgradeOpen();
            //    JXdata.SetXdata(line, "IsWire", partName);
            //    res = true;
            //}
            //foreach (Polyline pl in polys)
            //{
            //    pl.UpgradeOpen();
            //    JXdata.SetXdata(pl, "IsWire", partName);
            //    res = true;
            //}

            JXdata.SetXdata(br, "IsWire", "True");
            return res;
        }

        //SetIsReturn
        //Br이 Return(귀로) 구분이면  연결된 line poly에  Block PartNmae을 설정
        public static bool IsReturnSet(this BlockReference br, string partName) //status True False
        {
            bool res = false;
            //var lines = br.GetLines();
            //var polys = br.GetPolys();
            var ents = br.GetWires();//GetLinePolyArcs();
            //Set IsWire lines and polys 
            foreach (var en in ents)
            {
                en.UpgradeOpen();
                JXdata.SetXdata(en, "IsReturn", partName);
                res = true;
            }

            ////Set IsWire lines and polys 
            //foreach (Line line in lines)
            //{
            //    line.UpgradeOpen();
            //    JXdata.SetXdata(line, "IsReturn", partName);
            //    res = true;
            //}
            //foreach (Polyline pl in polys)
            //{
            //    pl.UpgradeOpen();
            //    JXdata.SetXdata(pl, "IsReturn", partName);
            //    res = true;
            //}

            JXdata.SetXdata(br, "IsReturn", "True");
            return res;
        }

        //IsWireSetDelete
        //Br이 Return(귀로) 구분이면  연결된 line poly에  XData 삭제
        public static bool IsWireSetDelete(this BlockReference br) //status True False
        {
            bool res = false;
            var lines = br.GetLines();
            var polys = br.GetPolys();

            //Set IsWire lines and polys 
            foreach (Line line in lines)
            {
                line.UpgradeOpen();
                JXdata.DelXdata(line, "IsWire");
                //JXdata.SetXdata(line, "IsReturn", status);
                res = true;
            }
            foreach (Polyline pl in polys)
            {
                pl.UpgradeOpen();
                JXdata.DelXdata(pl, "IsWire");
                //JXdata.SetXdata(pl, "IsReturn", status);
                res = true;
            }

            JXdata.SetXdata(br, "IsWire", "False");
            return res;
        }

        //IsReturnSetDelete
        //Br이 Return(귀로) 구분이면  연결된 line poly에  XData 삭제
        public static bool IsReturnSetDelete(this BlockReference br) //status True False
        {
            bool res = false;
            var lines = br.GetLines();
            var polys = br.GetPolys();

            //Set IsWire lines and polys 
            foreach (Line line in lines)
            {
                line.UpgradeOpen();
                JXdata.DelXdata(line, "IsReturn");
                //JXdata.SetXdata(line, "IsReturn", status);
                res = true;
            }
            foreach (Polyline pl in polys)
            {
                pl.UpgradeOpen();
                JXdata.DelXdata(pl, "IsReturn");
                //JXdata.SetXdata(pl, "IsReturn", status);
                res = true;
            }

            JXdata.SetXdata(br, "IsReturn", "False");
            return res;
        }

    }
}

namespace CADExtension
{
    public static class jListExtension
    {
        /// <summary>
        /// Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }


    }

    public static class StringExtension
    {
        public static string GetStrNum(this string strText)
        {
            string res = "";
            res = Regex.Replace(strText, @"\D", "");
            return res;
        }

        public static ObjectId GetCadId(this string strId)
        {
            return new ObjectId(new IntPtr(Convert.ToInt64(strId)));
        }

        // 문자열이 숫자로 변환 가능한지 확인
        public static bool IsNumeric(this string input, out double result)
        {
            // 1. 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(input))
            {
                result = 0;
                return false;
            }

            // 2. 소수점, 양수/음수 부호를 포함하여 숫자 형식 검사
            if (double.TryParse(input,
                System.Globalization.NumberStyles.Float |
                System.Globalization.NumberStyles.AllowThousands,
                System.Globalization.CultureInfo.InvariantCulture,
                out result))
            {
                return true;
            }

            return false;

        }

    }

    public static class LinqExtensions
    {
        public static void Dictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<TKey, TValue> invoke)
        {
            foreach (var kvp in dictionary)
                invoke(kvp.Key, kvp.Value);
        }
    }

    public static class ListListEntension
    {
        public static List<Line> AddConLines(this List<Line> lines, List<Line> inll)
        {
            //inll 의 일부가 lines에 있으면 inll 의 나머지 line lines에 Add
            foreach (var ll in inll)
            {
                if (lines.Contains(ll)) // 공통 인자가 있다면
                {
                    lines.AddRange(inll);
                    break;
                }
            }
            lines = lines.Distinct().ToList();
            return lines;
        }

        public static bool IsConLines(this List<Line> lines, List<Line> inll)
        {
            bool status = false;
            foreach (var ll in inll)
            {
                if (lines.Contains(ll)) // 공통 인자가 있다면
                {
                    //lines.AddRange(inll);
                    status = true;
                    break;
                }
            }
            //lines = lines.Distinct().ToList();
            return status;
        }

        public static bool IsHasItem(this List<List<Line>> gg, List<Line> inll)
        {
            bool status = false;
            foreach (var g in gg)
            {
                if (g.IsConLines(inll))
                { status = true; break; }
            }

            return status;
        }


    }

}


