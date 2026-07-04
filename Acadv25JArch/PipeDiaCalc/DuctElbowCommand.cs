using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System.Globalization;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace PipeLoad2
{
    /// <summary>
    /// Duct_E — 직각으로 만나는 수평 Green(aa) + 수직 Red(bb) 선택 →
    /// 코너에 원호(Arc) 엘보를 포함한 덕트 외곽선(Yellow, 레이어 "Duct_OutLine")을
    /// Line 8개 + Arc 2개(내측 R−H, 외측 R+H)로 생성. 기준선은 원본 유지.
    /// 엘보 중심선 반경 R = 1.5 × W_aa (Green 폭 기준).
    /// 설계 기준: 건축\Duct-OutLine\Duct_Elbow.md (v1.1).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(dirAA/dirBB) 기준으로 수행.
    /// </summary>
    public class DuctElbowCommand
    {
        private const double JunctionTol = 1e-3;   // X 접합점 일치 거리
        private const double PerpTol = 0.02;       // 직각 판정 (약 ±1.15°)
        private const double WidthTol = 1e-6;      // 폭 동일 판정
        private const int Yellow = 2;              // 외곽선 ACI
        private const string OutlineLayer = "Duct_OutLine";

        [CommandMethod("Duct_E", CommandFlags.UsePickSet)]
        public void Cmd_DuctE()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [4장] aa(Green) → bb(Red) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "기준선(aa, Green)을 선택하세요?", out Line aa)) return;
                    if (!PickLine(ed, tr, "직각 기준선(bb, Red)을 선택하세요?", out Line bb)) return;

                    if (aa.ObjectId == bb.ObjectId)
                    {
                        ed.WriteMessage("\n[E01] 두 Line 은 서로 달라야 합니다.");
                        return;
                    }

                    // [3장] XData "a" 폭 읽기
                    if (!TryReadWidth(aa, out double Waa)) { ed.WriteMessage("\n[E02] aa XData \"a\"(폭) 를 읽을 수 없습니다."); return; }
                    if (!TryReadWidth(bb, out double Wbb)) { ed.WriteMessage("\n[E02] bb XData \"a\"(폭) 를 읽을 수 없습니다."); return; }
                    if (Waa <= 0 || Wbb <= 0)
                    {
                        ed.WriteMessage("\n[E03] 폭은 0 보다 커야 합니다.");
                        return;
                    }
                    // [9장 v1.1] 폭 불일치 시 오류 종료
                    if (System.Math.Abs(Waa - Wbb) > WidthTol)
                    {
                        ed.WriteMessage($"\n[E04] 두 덕트 폭이 다릅니다 (Waa={Waa}, Wbb={Wbb}). 동일 폭만 지원합니다.");
                        return;
                    }

                    // [4장] 교차점 X — bb 의 aa측 끝점을 aa(연장 포함)에 수직 투영
                    Point3d bbNearRaw = bb.StartPoint.DistanceTo(aa.GetClosestPointTo(bb.StartPoint, true))
                                      <= bb.EndPoint.DistanceTo(aa.GetClosestPointTo(bb.EndPoint, true))
                                      ? bb.StartPoint : bb.EndPoint;
                    Point3d X = aa.GetClosestPointTo(bbNearRaw, true);

                    Point3d aaFar = FarEnd(aa, X);
                    Point3d bbFar = FarEnd(bb, X);
                    Point3d aaNear = NearEnd(aa, X);
                    Point3d bbNear = NearEnd(bb, X);

                    // [2장] aa/bb 의 X 측 끝점이 실제로 X 와 일치하는지 확인
                    if (aaNear.DistanceTo(X) > JunctionTol || bbNear.DistanceTo(X) > JunctionTol)
                    {
                        ed.WriteMessage("\n[E05] aa/bb 의 한쪽 끝점이 코너 교차점 X 와 일치하지 않습니다.");
                        return;
                    }

                    Vector3d dirAA = (aaFar - X).GetNormal();   // Green 진행 방향
                    Vector3d dirBB = (bbFar - X).GetNormal();   // Red 진행 방향

                    // [2장] aa ⊥ bb 검증
                    if (System.Math.Abs(dirAA.DotProduct(dirBB)) > PerpTol)
                    {
                        ed.WriteMessage("\n[E06] aa 와 bb 가 직각이 아닙니다.");
                        return;
                    }

                    // Arc 각도 계산은 WCS XY 평면 전제 — 평면 검증
                    Vector3d cross = dirAA.CrossProduct(dirBB);
                    if (System.Math.Abs(cross.Z) < 0.999)
                    {
                        ed.WriteMessage("\n[E08] 두 선이 WCS XY 평면 위에 있지 않습니다.");
                        return;
                    }

                    // [2/6장] 파생 치수 — 중심선 반경 R = 1.5 × W_aa (v1.1)
                    double H = Waa / 2.0;
                    double R = 1.5 * Waa;
                    double Rin = R - H;     // 내측 원호 반경
                    double Rout = R + H;    // 외측 원호 반경

                    // [10장] 길이 충분성 검증 — 접점(Tg/Tr)까지 도달해야 함
                    if (aaFar.DistanceTo(X) < R + JunctionTol)
                    {
                        ed.WriteMessage("\n[E07] aa 길이가 부족합니다 (엘보 반경 R 보다 길어야 합니다).");
                        return;
                    }
                    if (bbFar.DistanceTo(X) < R + JunctionTol)
                    {
                        ed.WriteMessage("\n[E07] bb 길이가 부족합니다 (엘보 반경 R 보다 길어야 합니다).");
                        return;
                    }

                    // [6장] 엘보 기하 — 원호 중심 C, 중심선 접점 Tg/Tr
                    Point3d C = X + dirAA * R + dirBB * R;
                    Point3d Tg = X + dirAA * R;     // Green 중심선 접점
                    Point3d Tr = X + dirBB * R;     // Red 중심선 접점

                    // [7장] 외곽선 정점 — 내측 = 원호 중심 C 쪽
                    Point3d Gin = Tg + dirBB * H;       // Green 내측 접점
                    Point3d Gout = Tg - dirBB * H;      // Green 외측 접점
                    Point3d Rin_pt = Tr + dirAA * H;    // Red 내측 접점
                    Point3d Rout_pt = Tr - dirAA * H;   // Red 외측 접점
                    Point3d GfarIn = aaFar + dirBB * H; // Green 먼 끝 내측
                    Point3d GfarOut = aaFar - dirBB * H;// Green 먼 끝 외측
                    Point3d BfarIn = bbFar + dirAA * H; // Red 먼 끝 내측
                    Point3d BfarOut = bbFar - dirAA * H;// Red 먼 끝 외측

                    foreach (var p in new[] { C, Tg, Tr, Gin, Gout, Rin_pt, Rout_pt, GfarIn, GfarOut, BfarIn, BfarOut })
                    {
                        if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                        {
                            ed.WriteMessage("\n[E09] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.");
                            return;
                        }
                    }

                    // [주요 로직] Arc 각도 — startAngle→endAngle 은 반시계(CCW) 방향.
                    // C→Tg 방향(= -dirBB), C→Tr 방향(= -dirAA)의 각도를 구하고
                    // CCW sweep 이 90°가 되도록 시작/끝 순서를 결정한다.
                    double angG = System.Math.Atan2((Tg - C).Y, (Tg - C).X);
                    double angR = System.Math.Atan2((Tr - C).Y, (Tr - C).X);
                    double sweepGtoR = NormalizeAngle(angR - angG);   // angG→angR CCW 각
                    double startAng, endAng;
                    if (sweepGtoR <= System.Math.PI)   // 90° 쪽이 CCW
                    {
                        startAng = angG;
                        endAng = angR;
                    }
                    else                               // 반대 순서가 90° CCW
                    {
                        startAng = angR;
                        endAng = angG;
                    }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // [7장] 레이어 준비
                    tr.CreateLayer(OutlineLayer, Yellow, LineWeight.ByLayer);

                    // [7장 v1.1] 외곽선 — Line 8개 + Arc 2개 (기준선 aa/bb 는 원본 유지)
                    int created = 0;
                    created += AddOutlineLine(tr, btr, db, GfarIn, Gin);        // 1. Green 내측 외곽
                    created += AddOutlineLine(tr, btr, db, GfarOut, Gout);      // 2. Green 외측 외곽
                    created += AddOutlineLine(tr, btr, db, Rin_pt, BfarIn);     // 3. Red 내측 외곽
                    created += AddOutlineLine(tr, btr, db, Rout_pt, BfarOut);   // 4. Red 외측 외곽
                    created += AddOutlineLine(tr, btr, db, Gin, Gout);          // 5. Green측 이음선 (Tg)
                    created += AddOutlineLine(tr, btr, db, Rin_pt, Rout_pt);    // 6. Red측 이음선 (Tr)
                    created += AddOutlineLine(tr, btr, db, GfarIn, GfarOut);    // 7. Green 끝단 마감
                    created += AddOutlineLine(tr, btr, db, BfarIn, BfarOut);    // 8. Red 끝단 마감
                    created += AddOutlineArc(tr, btr, db, C, Rin, startAng, endAng);  // 9. 엘보 내측 원호
                    created += AddOutlineArc(tr, btr, db, C, Rout, startAng, endAng); // 10. 엘보 외측 원호

                    ed.WriteMessage($"\nDuct_E 완료: 외곽 Line 8개 + Arc 2개 생성 " +
                                    $"(W={Waa}, R(중심선)={R}, 내측={Rin}, 외측={Rout}). 기준선은 원본 유지.");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>각도를 0 ~ 2π 범위로 정규화.</summary>
        private double NormalizeAngle(double a)
        {
            double t = a % (2.0 * System.Math.PI);
            if (t < 0) t += 2.0 * System.Math.PI;
            return t;
        }

        /// <summary>Line 1개를 GetEntity 로 선택 (Line 만 허용). 취소 시 false.</summary>
        private bool PickLine(Editor ed, Transaction tr, string msg, out Line line)
        {
            line = null;
            var peo = new PromptEntityOptions("\n" + msg);
            peo.SetRejectMessage("\nLine 만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return false;
            line = (Line)tr.GetObject(per.ObjectId, OpenMode.ForRead);
            return true;
        }

        /// <summary>Line 에서 기준점 refPt 와 먼 끝점을 반환.</summary>
        private Point3d FarEnd(Line ln, Point3d refPt)
        {
            return ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt)
                ? ln.EndPoint : ln.StartPoint;
        }

        /// <summary>Line 에서 기준점 refPt 와 가까운 끝점을 반환.</summary>
        private Point3d NearEnd(Line ln, Point3d refPt)
        {
            return ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt)
                ? ln.StartPoint : ln.EndPoint;
        }

        /// <summary>a→b Yellow 외곽선 Line 을 "Duct_OutLine" 레이어에 생성.</summary>
        private int AddOutlineLine(Transaction tr, BlockTableRecord btr, Database db, Point3d a, Point3d b)
        {
            var ln = new Line(a, b);
            ln.SetDatabaseDefaults(db);
            ln.Layer = OutlineLayer;
            ln.Color = Color.FromColorIndex(ColorMethod.ByAci, Yellow);
            btr.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
            return 1;
        }

        /// <summary>중심 c, 반경 r, startAng→endAng(CCW, 라디안) Yellow 원호를 "Duct_OutLine" 레이어에 생성.</summary>
        private int AddOutlineArc(Transaction tr, BlockTableRecord btr, Database db, Point3d c, double r, double startAng, double endAng)
        {
            var arc = new Arc(c, r, startAng, endAng);
            arc.SetDatabaseDefaults(db);
            arc.Layer = OutlineLayer;
            arc.Color = Color.FromColorIndex(ColorMethod.ByAci, Yellow);
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            return 1;
        }

        /// <summary>XData "a" 문자열에서 폭(double) 추출. "600x400" 형태면 앞 숫자 사용.</summary>
        private bool TryReadWidth(Line ln, out double w)
        {
            w = 0;
            string s = JXdata.GetXdata(ln, "a");
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out w)) return true;

            int idx = s.IndexOfAny(new[] { 'x', 'X', '*' });
            return idx > 0 && double.TryParse(s.Substring(0, idx).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out w);
        }
    }
}
