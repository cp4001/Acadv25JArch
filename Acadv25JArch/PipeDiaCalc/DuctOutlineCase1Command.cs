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
    /// Duct_Outline_Case1 — 상류 Main(aa) → 하류 Main(bb) 축소 전이 + 직각 분기(cc) 외곽선(Magenta) 생성.
    /// 기준선(aa, Green) → collinear선(bb) → 수직선(cc) 순서로 1개씩 선택하면
    /// 각 Line 의 XData "a"(폭, string) 를 읽어 접합점 C 기준 외곽선 13개를 만든다.
    /// 설계 기준: 공조덕트사이즈\Duct_OutLine_case1.md (Step 1~9).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(u, v)를 기준으로 수행.
    /// </summary>
    public class DuctOutlineCase1Command
    {
        private const double JunctionTol = 1e-3;    // 접합점/끝점 일치 거리
        private const double CollinearDot = -0.999; // aa·bb 반대방향(일직선) 판정 (약 ±2.6°)
        private const double PerpDot = 0.02;        // cc⊥Main 90° 판정 (약 ±1.15°)
        private const int Magenta = 6;              // 선홍색 ACI

        [CommandMethod("Duct_Outline_Case1", CommandFlags.UsePickSet)]
        public void Cmd_DuctOutlineCase1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [Step 1] aa(기준선) → bb(collinear) → cc(수직) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "기준선(aa, 상류 Main)을 선택하세요?", out Line aa)) return;
                    if (!PickLine(ed, tr, "collinear 선(bb, 하류 Main)을 선택하세요?", out Line bb)) return;
                    if (!PickLine(ed, tr, "수직 선(cc, 분기 Branch)을 선택하세요?", out Line cc)) return;

                    if (aa.ObjectId == bb.ObjectId || bb.ObjectId == cc.ObjectId || aa.ObjectId == cc.ObjectId)
                    {
                        ed.WriteMessage("\n[E01] 세 Line 은 서로 달라야 합니다.");
                        return;
                    }

                    // [Step 3] 접합점 C = 세 Line 이 모두 끝점으로 공유하는 점
                    if (!TryFindJunction(aa, bb, cc, out Point3d C))
                    {
                        ed.WriteMessage("\n[E04] 세 선의 공통 접합점 C 를 찾을 수 없습니다.");
                        return;
                    }

                    Point3d Ea = FarEnd(aa, C);  // aa 의 C 반대쪽 끝점 (−u 측)
                    Point3d Eb = FarEnd(bb, C);  // bb 의 C 반대쪽 끝점 (+u 측)
                    Point3d Ec = FarEnd(cc, C);  // cc 의 C 반대쪽 끝점 (+v 측)

                    Vector3d u = (Eb - C).GetNormal();   // Main 방향 (C→bb far)
                    Vector3d v = (Ec - C).GetNormal();   // Branch 방향 (C→cc far)
                    Vector3d ua = (Ea - C).GetNormal();  // C→aa far (≈ −u)

                    // [Step 3] aa·bb 동일선(반대방향) 검증
                    if (u.DotProduct(ua) > CollinearDot)
                    {
                        ed.WriteMessage("\n[E05] aa 와 bb 가 C 에서 반대 방향의 일직선이 아닙니다.");
                        return;
                    }
                    // [Step 3] cc ⊥ Main 검증
                    if (System.Math.Abs(u.DotProduct(v)) > PerpDot)
                    {
                        ed.WriteMessage("\n[E06] cc 가 Main(aa/bb) 과 직각이 아닙니다.");
                        return;
                    }

                    // [Step 2] XData 폭 읽기
                    if (!TryReadWidth(aa, out double Wa))
                    {
                        ed.WriteMessage("\n[E02] aa XData \"a\"(폭) 를 읽을 수 없습니다.");
                        return;
                    }
                    if (!TryReadWidth(bb, out double Wb))
                    {
                        ed.WriteMessage("\n[E02] bb XData \"a\"(폭) 를 읽을 수 없습니다.");
                        return;
                    }
                    if (!TryReadWidth(cc, out double Wc))
                    {
                        ed.WriteMessage("\n[E02] cc XData \"a\"(폭) 를 읽을 수 없습니다.");
                        return;
                    }
                    if (Wa <= 0 || Wb <= 0 || Wc <= 0)
                    {
                        ed.WriteMessage("\n[E03] 폭은 0 보다 커야 합니다.");
                        return;
                    }
                    if (Wa <= Wb)
                    {
                        ed.WriteMessage($"\n[E07] Wa > Wb 여야 합니다 (Wa={Wa}, Wb={Wb}) — 동일폭/확대형은 별도 Case.");
                        return;
                    }

                    // [Step 4] 파생 치수
                    double Ha = Wa / 2.0;                                  // aa 반폭
                    double Hb = Wb / 2.0;                                  // bb 반폭
                    double Hc = Wc / 2.0;                                  // cc 반폭
                    double hb = Wc / 4.0;                                  // Branch 45° 투영 길이
                    double S = 1.5 * Wc;                                   // C→CR 연장거리
                    double hr = (Wa - Wb) / 2.0;                           // Reducer 편측 폭차
                    double ww = hr / System.Math.Tan(System.Math.PI / 6.0); // CR→CD Main 방향 거리 (30°)

                    // [Step 5·6] bb 길이(≥ S+ww) / cc 길이(≥ Ha+hb) 검증
                    if (Eb.DistanceTo(C) < S + ww - JunctionTol)
                    {
                        ed.WriteMessage($"\n[E08] bb 길이가 부족합니다 (필요 ≥ {S + ww:0.##}).");
                        return;
                    }
                    if (Ec.DistanceTo(C) < Ha + hb - JunctionTol)
                    {
                        ed.WriteMessage($"\n[E09] cc 길이가 부족합니다 (필요 ≥ {Ha + hb:0.##}).");
                        return;
                    }

                    // [Step 4] 기준점
                    Point3d J0 = C - (Hc + hb) * u + Ha * v;
                    Point3d J1 = C - Hc * u + (Ha + hb) * v;
                    Point3d J2 = C + Hc * u + (Ha + hb) * v;
                    Point3d J3 = C + Hc * u + Ha * v;

                    Point3d CR = C + S * u;
                    Point3d CD = CR + ww * u;
                    Point3d RTop = CR + Ha * v;
                    Point3d RBot = CR - Ha * v;
                    Point3d DTop = CD + Hb * v;
                    Point3d DBot = CD - Hb * v;

                    // [E10] 계산 결과 유효성
                    foreach (var p in new[] { J0, J1, J2, J3, CR, CD, RTop, RBot, DTop, DBot })
                    {
                        if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                        {
                            ed.WriteMessage("\n[E10] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.");
                            return;
                        }
                    }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    int created = 0;

                    // [Step 7.1] aa 및 Branch 접합부
                    created += AddMagentaLine(tr, btr, db, Ea + Ha * v, J0); // 1 aa 상부 측벽 상류
                    created += AddMagentaLine(tr, btr, db, J0, J1);          // 2 45° 사선
                    created += AddMagentaLine(tr, btr, db, J1, J2);          // 3 cc 폭 수평 접합선
                    created += AddMagentaLine(tr, btr, db, Ec - Hc * u, J1); // 4 cc 왼쪽 측벽
                    created += AddMagentaLine(tr, btr, db, Ec + Hc * u, J3); // 5 cc 오른쪽 측벽(J2 지나 J3)
                    created += AddMagentaLine(tr, btr, db, J3, RTop);        // 6 aa 상부 측벽 CR까지 연장
                    created += AddMagentaLine(tr, btr, db, Ea - Ha * v, RBot); // 7 aa 하부 측벽

                    // [Step 7.2] Reducer
                    created += AddMagentaLine(tr, btr, db, RTop, DTop);      // 8 상부 30° 사선
                    created += AddMagentaLine(tr, btr, db, RBot, DBot);      // 9 하부 30° 사선
                    created += AddMagentaLine(tr, btr, db, RTop, RBot);      // 10 CR 수직면 (Wa)
                    created += AddMagentaLine(tr, btr, db, DTop, DBot);      // 11 CD 수직면 (Wb)

                    // [Step 7.3] bb 측벽
                    created += AddMagentaLine(tr, btr, db, DTop, Eb + Hb * v); // 12 bb 상부 측벽
                    created += AddMagentaLine(tr, btr, db, DBot, Eb - Hb * v); // 13 bb 하부 측벽

                    // [Step 8] 중심선 수정: aa·bb 의 C측 끝점을 CR 로 이동 (cc 불변)
                    MoveEnd(aa, C, CR);
                    MoveEnd(bb, C, CR);

                    ed.WriteMessage($"\nDuct_Outline_Case1 완료: 외곽선 {created}개 생성 " +
                                    $"(Wa={Wa}, Wb={Wb}, Wc={Wc}, S={S:0.##}, ww={ww:0.##}).");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
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

        /// <summary>세 Line 이 모두 끝점으로 공유하는 공통 접합점 C 를 찾는다.</summary>
        private bool TryFindJunction(Line aa, Line bb, Line cc, out Point3d C)
        {
            foreach (var ap in new[] { aa.StartPoint, aa.EndPoint })
            {
                bool shareBb = bb.StartPoint.DistanceTo(ap) <= JunctionTol || bb.EndPoint.DistanceTo(ap) <= JunctionTol;
                bool shareCc = cc.StartPoint.DistanceTo(ap) <= JunctionTol || cc.EndPoint.DistanceTo(ap) <= JunctionTol;
                if (shareBb && shareCc) { C = ap; return true; }
            }
            C = Point3d.Origin;
            return false;
        }

        /// <summary>Line 에서 기준점 refPt 와 먼 끝점을 반환.</summary>
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
