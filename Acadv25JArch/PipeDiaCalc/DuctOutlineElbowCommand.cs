using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Globalization;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace PipeLoad2
{
    /// <summary>
    /// Duct_Outline_Elbow — 직각(90°) 동일폭 Elbow 덕트 외곽선(Magenta) 생성.
    /// 기준선(aa, Green) → 직각선(bb, Red) 순서로 1개씩 선택하면
    /// 각 Line 의 XData "a"(폭) 를 읽어 Magenta 외곽 Line 4개 + Arc 2개와
    /// Green 중심 Arc 1개를 생성하고, 두 중심선의 C측 끝점을 Arc 접선점으로 이동한다.
    /// 설계 기준: 공조덕트사이즈\Duct_Outline_Elbow.md (Step 1~13).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(u, v)를 기준으로 수행.
    /// </summary>
    public class DuctOutlineElbowCommand
    {
        private const double JunctionTol = 1e-3;    // 접합점/끝점 일치 거리
        private const double PerpDot     = 0.02;    // |u·v| < 0.02 → 90° 판정 (약 ±1.15°)
        private const double WidthTol    = 1e-6;    // Wa == Wb 동일폭 판정
        private const int    Magenta     = 6;       // 선홍색 ACI
        private const int    Green       = 3;       // 녹색 ACI

        [CommandMethod("Duct_Outline_Elbow", CommandFlags.UsePickSet)]
        public void Cmd_DuctOutlineElbow()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [Step 1·2] aa(기준선) → bb(직각선) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "Elbow 기준선(aa, Green)을 선택하세요:", out Line aa)) return;
                    if (!PickLine(ed, tr, "Elbow 직각선(bb, Red)을 선택하세요:", out Line bb)) return;

                    if (aa.ObjectId == bb.ObjectId)
                    {
                        ed.WriteMessage("\n[E01] 두 Line은 서로 달라야 합니다.");
                        return;
                    }

                    // [Step 4] XData 폭 읽기
                    if (!TryReadWidth(aa, out double Wa))
                    {
                        ed.WriteMessage("\n[E02] aa XData \"a\"(폭)를 읽을 수 없습니다.");
                        return;
                    }
                    if (!TryReadWidth(bb, out double Wb))
                    {
                        ed.WriteMessage("\n[E02] bb XData \"a\"(폭)를 읽을 수 없습니다.");
                        return;
                    }
                    if (Wa <= 0 || Wb <= 0)
                    {
                        ed.WriteMessage("\n[E03] 폭은 0보다 커야 합니다.");
                        return;
                    }

                    // [Step 5] 동일폭 검증
                    if (System.Math.Abs(Wa - Wb) > WidthTol)
                    {
                        ed.WriteMessage($"\n[E04] 동일 폭 Elbow만 지원합니다 (Wa={Wa}, Wb={Wb}). Reducer가 필요한 경우 별도 Case를 사용하세요.");
                        return;
                    }

                    // [Step 6] 공통 접합점 C
                    if (!TryFindJunction(aa, bb, out Point3d C))
                    {
                        ed.WriteMessage("\n[E05] 두 Line의 공통 접합점 C를 찾을 수 없습니다.");
                        return;
                    }

                    Point3d  Ea = FarEnd(aa, C);
                    Point3d  Eb = FarEnd(bb, C);
                    Vector3d u  = (Ea - C).GetNormal();   // C→aa far (Green 방향)
                    Vector3d v  = (Eb - C).GetNormal();   // C→bb far (Red 방향)

                    // [Step 7] 90° 직교 검증
                    if (System.Math.Abs(u.DotProduct(v)) > PerpDot)
                    {
                        ed.WriteMessage("\n[E06] 두 Line이 90°로 직교하지 않습니다.");
                        return;
                    }

                    // [Step 8] 치수 계산
                    double W  = Wa;
                    double H  = W / 2.0;
                    double R  = 1.5 * W;
                    double Ri = R - H;
                    double Ro = R + H;

                    // Arc 기준점
                    Point3d Ta = C + R * u;          // Green 중심선 Arc 접선점
                    Point3d Tb = C + R * v;          // Red 중심선 Arc 접선점
                    Point3d O  = C + R * u + R * v;  // 중심 Arc 원 중심

                    // 외곽 Arc 끝점
                    Point3d Ai0 = Ta + H * v;        // 내측 Arc 시작 (Green 쪽)
                    Point3d Ai1 = Tb + H * u;        // 내측 Arc 끝   (Red 쪽)
                    Point3d Ao0 = Ta - H * v;        // 외측 Arc 시작 (Green 쪽)
                    Point3d Ao1 = Tb - H * u;        // 외측 Arc 끝   (Red 쪽)

                    // [Step 9] Line 길이 ≥ R 검증
                    if (Ea.DistanceTo(C) < R - JunctionTol)
                    {
                        ed.WriteMessage($"\n[E07] aa 길이가 부족합니다 (필요 ≥ {R:0.##}).");
                        return;
                    }
                    if (Eb.DistanceTo(C) < R - JunctionTol)
                    {
                        ed.WriteMessage($"\n[E07] bb 길이가 부족합니다 (필요 ≥ {R:0.##}).");
                        return;
                    }

                    // [E08] NaN/Infinity 검증
                    foreach (var p in new[] { Ta, Tb, O, Ai0, Ai1, Ao0, Ao1 })
                    {
                        if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                        {
                            ed.WriteMessage("\n[E08] 계산 결과에 NaN/Infinity가 포함되어 중단합니다.");
                            return;
                        }
                    }

                    // [E09] Arc 시작각·끝각 계산 (CCW 90°)
                    if (!TryArcAngles(O, Ta, Tb, out double arcStart, out double arcEnd))
                    {
                        ed.WriteMessage("\n[E09] Arc 방향 계산에 실패했습니다 (포함각이 90°가 아닙니다).");
                        return;
                    }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    int linesCnt = 0, arcCnt = 0;

                    // [Step 10] 외곽 Magenta Line 4개 + Arc 2개
                    linesCnt += AddMagentaLine(tr, btr, db, Ea + H * v, Ai0);          // 1 aa 내측 직선
                    arcCnt   += AddMagentaArc (tr, btr, db, O, Ri, arcStart, arcEnd);  // 2 내측 90° Arc
                    linesCnt += AddMagentaLine(tr, btr, db, Ai1, Eb + H * u);          // 3 bb 내측 직선
                    linesCnt += AddMagentaLine(tr, btr, db, Ea - H * v, Ao0);          // 4 aa 외측 직선
                    arcCnt   += AddMagentaArc (tr, btr, db, O, Ro, arcStart, arcEnd);  // 5 외측 90° Arc
                    linesCnt += AddMagentaLine(tr, btr, db, Ao1, Eb - H * u);          // 6 bb 외측 직선

                    // [Step 11] 중심선 C측 끝점 → 접선점(Ta, Tb)으로 이동
                    MoveEnd(aa, C, Ta);
                    MoveEnd(bb, C, Tb);

                    // [Step 12] Green 중심 Arc 생성 + XData "a" 기록
                    Arc centerArc = CreateGreenArc(tr, btr, db, O, R, arcStart, arcEnd);
                    try { JXdata.SetXdata(centerArc, "a", W.ToString(CultureInfo.InvariantCulture)); }
                    catch { /* "a" 미등록 시 XData 생략 */ }

                    ed.WriteMessage(
                        $"\nDuct_Outline_Elbow 완료: Magenta 외곽 {linesCnt + arcCnt}개 " +
                        $"(Line {linesCnt}, Arc {arcCnt}) + Green 중심 Arc 1개 생성 " +
                        $"(W={W}, H={H:0.##}, R={R:0.##}, Ri={Ri:0.##}, Ro={Ro:0.##}).");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>Line 1개를 GetEntity로 선택 (Line만 허용). 취소 시 false.</summary>
        private bool PickLine(Editor ed, Transaction tr, string msg, out Line line)
        {
            line = null;
            var peo = new PromptEntityOptions("\n" + msg);
            peo.SetRejectMessage("\nLine만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return false;
            line = (Line)tr.GetObject(per.ObjectId, OpenMode.ForRead);
            return true;
        }

        /// <summary>두 Line 중 한쪽 끝점이 공통인 접합점 C를 찾는다.</summary>
        private bool TryFindJunction(Line aa, Line bb, out Point3d C)
        {
            foreach (var ap in new[] { aa.StartPoint, aa.EndPoint })
            {
                if (bb.StartPoint.DistanceTo(ap) <= JunctionTol ||
                    bb.EndPoint.DistanceTo(ap)   <= JunctionTol)
                { C = ap; return true; }
            }
            C = Point3d.Origin;
            return false;
        }

        /// <summary>Line에서 기준점 refPt와 먼 끝점을 반환.</summary>
        private Point3d FarEnd(Line ln, Point3d refPt)
        {
            return ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt)
                ? ln.EndPoint : ln.StartPoint;
        }

        /// <summary>Line의 기준점 refPt와 가까운 끝점만 newPt로 이동.</summary>
        private void MoveEnd(Line ln, Point3d refPt, Point3d newPt)
        {
            ln.UpgradeOpen();
            if (ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt))
                ln.StartPoint = newPt;
            else
                ln.EndPoint   = newPt;
        }

        /// <summary>
        /// startPt(Ta), endPt(Tb), 중심 O를 기준으로 CCW 90° Arc의 시작각·끝각(라디안)을 구한다.
        /// 270° 호가 선택되는 경우 start/end를 교환해 90° 단호를 선택한다.
        /// </summary>
        private bool TryArcAngles(Point3d O, Point3d startPt, Point3d endPt,
                                   out double startAngle, out double endAngle)
        {
            startAngle = endAngle = 0;
            Vector3d sv = startPt - O;
            Vector3d ev = endPt   - O;
            if (sv.Length < 1e-9 || ev.Length < 1e-9) return false;

            startAngle = System.Math.Atan2(sv.Y, sv.X);
            endAngle   = System.Math.Atan2(ev.Y, ev.X);

            // [0, 2π) 로 정규화
            if (startAngle < 0) startAngle += 2 * System.Math.PI;
            if (endAngle   < 0) endAngle   += 2 * System.Math.PI;

            // CCW 포함각
            double span = endAngle - startAngle;
            if (span < 0) span += 2 * System.Math.PI;

            // 270° 호가 선택된 경우 → start/end 교환으로 90° CCW 단호 선택
            if (span > System.Math.PI)
            {
                double tmp = startAngle;
                startAngle = endAngle;
                endAngle   = tmp;
                span       = 2 * System.Math.PI - span;
            }

            // 포함각이 90°(π/2)에 충분히 가까운지 확인
            return System.Math.Abs(span - System.Math.PI / 2) < 0.1;
        }

        private int AddMagentaLine(Transaction tr, BlockTableRecord btr, Database db, Point3d a, Point3d b)
        {
            var ln = new Line(a, b);
            ln.SetDatabaseDefaults(db);
            ln.Color = Color.FromColorIndex(ColorMethod.ByAci, Magenta);
            btr.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
            return 1;
        }

        private int AddMagentaArc(Transaction tr, BlockTableRecord btr, Database db,
                                   Point3d center, double radius, double startAngle, double endAngle)
        {
            var arc = new Arc(center, radius, startAngle, endAngle);
            arc.SetDatabaseDefaults(db);
            arc.Color = Color.FromColorIndex(ColorMethod.ByAci, Magenta);
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            return 1;
        }

        private Arc CreateGreenArc(Transaction tr, BlockTableRecord btr, Database db,
                                    Point3d center, double radius, double startAngle, double endAngle)
        {
            var arc = new Arc(center, radius, startAngle, endAngle);
            arc.SetDatabaseDefaults(db);
            arc.Color = Color.FromColorIndex(ColorMethod.ByAci, Green);
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            return arc;
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
