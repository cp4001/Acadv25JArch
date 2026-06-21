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
    /// Duct_Outline_Case2 — T형 양방향 분기 외곽선(Magenta) 생성.
    /// 기준선(Green, 주덕트) 1개를 먼저 선택하고, 좌우 분기선(Red) 2개를 선택하면
    /// 각 Line 의 XData "a"(폭, string) 를 읽어 접합점 C 기준 T 외곽선 11개를 만든다.
    /// 설계 기준: 공조덕트사이즈\Duct_OutLine_case2.md (Step 1~7).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(u, v)를 기준으로 수행.
    /// </summary>
    public class DuctOutlineCase2Command
    {
        private const double JunctionTol = 1e-3;   // 접합점/끝점 일치 거리
        private const double CollinearDot = -0.999; // Red 두 선 반대방향(일직선) 판정 (약 ±2.6°)
        private const double PerpDot = 0.02;        // Green⊥Red 90° 판정 (약 ±1.15°)
        private const double WidthTol = 1e-6;       // 좌우 분기 폭 동일 판정
        private const double D_Depth = 200.0;       // 하부 연장 깊이(고정, 폭 무관)
        private const int Magenta = 6;              // 선홍색 ACI

        [CommandMethod("Duct_Outline_Case2", CommandFlags.UsePickSet)]
        public void Cmd_DuctOutlineCase2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [Step 1] 기준선(Green) 1개 선택
                    var peo = new PromptEntityOptions("\n기준선(Green, 주덕트)을 선택하세요?");
                    peo.SetRejectMessage("\nLine 만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(Line), true);
                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;
                    Line green = (Line)tr.GetObject(per.ObjectId, OpenMode.ForRead);

                    // [Step 1] 분기선(Red) 2개 선택
                    List<Line> reds = JEntity.GetEntityByTpye<Line>(
                        "분기선(Red) 2개를 선택하세요?",
                        JSelFilter.MakeFilterTypes("LINE"));
                    if (reds == null) return;                 // E01
                    if (reds.Count != 2)
                    {
                        ed.WriteMessage($"\n[E01] 분기선(Red) 을 정확히 2개 선택해야 합니다 (현재 {reds.Count}개).");
                        return;
                    }
                    if (reds.Any(r => r.ObjectId == green.ObjectId))
                    {
                        ed.WriteMessage("\n[E01] 기준선(Green) 과 분기선(Red) 은 서로 다른 Line 이어야 합니다.");
                        return;
                    }

                    // [Step 3] 접합점 C = Green 한 끝점이 두 Red 와 모두 만나는 점
                    if (!TryFindJunction(green, reds, out Point3d C))
                    {
                        ed.WriteMessage("\n[E04] 세 선의 공통 접합점 C 를 찾을 수 없습니다.");
                        return;
                    }

                    Point3d Gfar = FarEnd(green, C);             // Green 의 C 반대쪽 끝점
                    Point3d E0 = FarEnd(reds[0], C);            // Red0 의 C 반대쪽 끝점
                    Point3d E1 = FarEnd(reds[1], C);            // Red1 의 C 반대쪽 끝점

                    Vector3d u = (Gfar - C).GetNormal();        // 주덕트 방향
                    Vector3d v = (E0 - C).GetNormal();          // 분기 방향(Red0 = +v)
                    Vector3d v1 = (E1 - C).GetNormal();          // Red1 방향

                    // [Step 3] Red 두 선이 반대 방향 일직선인지
                    if (v.DotProduct(v1) > CollinearDot)
                    {
                        ed.WriteMessage("\n[E05] 두 분기선(Red) 이 C 에서 반대 방향의 일직선이 아닙니다.");
                        return;
                    }
                    // [Step 3] Green ⊥ Red 90°
                    if (Math.Abs(u.DotProduct(v)) > PerpDot)
                    {
                        ed.WriteMessage("\n[E06] 기준선(Green) 과 분기선(Red) 이 90° 가 아닙니다 — Case 2 미지원.");
                        return;
                    }

                    // [Step 2] XData 폭 읽기
                    if (!TryReadWidth(green, out double Wm))
                    {
                        ed.WriteMessage("\n[E02] 기준선(Green) XData \"a\"(폭) 를 읽을 수 없습니다.");
                        return;
                    }
                    if (!TryReadWidth(reds[0], out double Wl) || !TryReadWidth(reds[1], out double Wr))
                    {
                        ed.WriteMessage("\n[E02] 분기선(Red) XData \"a\"(폭) 를 읽을 수 없습니다.");
                        return;
                    }
                    if (Wm <= 0 || Wl <= 0 || Wr <= 0)
                    {
                        ed.WriteMessage("\n[E03] 폭은 0 보다 커야 합니다.");
                        return;
                    }
                    if (Math.Abs(Wl - Wr) > WidthTol)
                    {
                        ed.WriteMessage($"\n[E07] 좌우 분기 폭이 서로 다릅니다 (Wl={Wl}, Wr={Wr}) — 별도 Case 필요.");
                        return;
                    }

                    // [Step 4] 파생 치수
                    double Wb = Wl;             // = Wr
                    double M = Wm / 2.0;        // 주덕트 반폭
                    double B = Wb / 2.0;        // 분기 반폭
                    double h = Wb / 4.0;        // 분기 보강 치수
                    double d = D_Depth;         // 하부 연장 깊이(고정 200)

                    // [Step 5] 선 길이 검증: 각 Red 는 (M+h), Green 은 (B+d) 이상 필요
                    double redLen0 = E0.DistanceTo(C);
                    double redLen1 = E1.DistanceTo(C);
                    if (redLen0 < M + h - JunctionTol || redLen1 < M + h - JunctionTol)
                    {
                        ed.WriteMessage($"\n[E08] 분기선 길이가 외곽선 생성에 필요한 최소 길이({M + h:0.##}) 보다 짧습니다.");
                        return;
                    }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    int created = 0;

                    // 좌우 분기: s = +1(Red0, +v), s = -1(Red1, -v)
                    var branches = new[] { (E: E0, s: +1.0), (E: E1, s: -1.0) };
                    foreach (var (E, s) in branches)
                    {
                        Point3d P1 = C + s * (M + h) * v + B * u;   // 분기 상부 외곽 (목 바깥쪽)
                        Point3d P2 = C + s * M * v + (B + h) * u;   // 45° 사선 종점
                        Point3d P3 = C + s * (M + h) * v - B * u;   // 분기 목 하단
                        Point3d P4 = C + s * M * v - B * u;         // 주덕트 측벽과 만나는 하부

                        // [Step 5] 분기 상부 벽 / 목 수직선 / 하부 벽 / 45° 사선
                        created += AddMagentaLine(tr, btr, db, E + B * u, P1); // 분기 상부 벽
                        created += AddMagentaLine(tr, btr, db, P1, P3);        // 분기 목 수직선
                        created += AddMagentaLine(tr, btr, db, E - B * u, P4); // 분기 하부 벽 (P3 지나 P4까지)
                        created += AddMagentaLine(tr, btr, db, P1, P2);        // 45° 사선
                    }

                    // [Step 5] 주덕트 측벽 2개 + 하부 캡 1개
                    Point3d Qr = C + (+1.0) * M * v - (B + d) * u; // Q(+1)
                    Point3d Ql = C + (-1.0) * M * v - (B + d) * u; // Q(-1)
                    created += AddMagentaLine(tr, btr, db, Gfar + (+1.0) * M * v, Qr); // 주덕트 측벽(+v)
                    created += AddMagentaLine(tr, btr, db, Gfar + (-1.0) * M * v, Ql); // 주덕트 측벽(-v)
                    created += AddMagentaLine(tr, btr, db, Ql, Qr);                    // 하부 캡

                    // [Step 6] Green 기준선 C측 끝점을 하부 캡 중앙 Gext 로 연장
                    Point3d Gext = C - (B + d) * u;
                    MoveEnd(green, C, Gext);

                    ed.WriteMessage($"\nDuct_Outline_Case2 완료: 외곽선 {created}개 생성 " +
                                    $"(Wm={Wm}, Wb={Wb}, M={M}, B={B}, h={h}, d={d}).");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>Green 의 한 끝점이 두 Red 와 모두 끝점으로 만나는 접합점 C 를 찾는다.</summary>
        private bool TryFindJunction(Line green, List<Line> reds, out Point3d C)
        {
            foreach (var gp in new[] { green.StartPoint, green.EndPoint })
            {
                bool allShare = reds.All(r =>
                    r.StartPoint.DistanceTo(gp) <= JunctionTol ||
                    r.EndPoint.DistanceTo(gp) <= JunctionTol);
                if (allShare) { C = gp; return true; }
            }
            C = Point3d.Origin;
            return false;
        }

        /// <summary>Line 에서 기준점 ref 와 먼 끝점을 반환.</summary>
        private Point3d FarEnd(Line ln, Point3d refPt)
        {
            return ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt)
                ? ln.EndPoint : ln.StartPoint;
        }

        /// <summary>Line 의 기준점 refPt 와 가까운 끝점만 newPt 로 이동.</summary>
        private void MoveEnd(Line ln, Point3d refPt, Point3d newPt)
        {
            ln.UpgradeOpen();
            if (ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt))
                ln.StartPoint = newPt;
            else
                ln.EndPoint = newPt;
        }

        private int AddMagentaLine(Transaction tr, BlockTableRecord btr, Database db, Point3d a, Point3d b)
        {
            var ln = new Line(a, b);
            ln.SetDatabaseDefaults(db);                                  // 현재 레이어 등 DB 기본값
            ln.Color = Color.FromColorIndex(ColorMethod.ByAci, Magenta); // 선홍색
            btr.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
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
