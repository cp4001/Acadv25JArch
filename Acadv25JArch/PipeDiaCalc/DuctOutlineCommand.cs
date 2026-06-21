using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace PipeLoad2
{
    /// <summary>
    /// DuctOutline3 — Duct 중심선 3개(메인런 collinear 2개 + 수직 분기 1개)를 선택하면
    /// 각 Line 의 XData "a"(폭, string) 를 읽어 Reducer 전이부 외곽선(Magenta)을 생성.
    /// 설계 기준: 공조덕트사이즈\Duct_OutLine_Design.md (Step 0~4).
    /// 출력: B′ 수직면 / 상단 사선 / A 수직면 / 하단 사선 (개별 Line, 선홍색, 현재 레이어).
    /// 중심선 조정: White 끝점→B, Green 연장→A, Red 단축→A.
    /// </summary>
    public class DuctOutlineCommand
    {
        private const double JunctionTol  = 1e-3;   // 공통 접합점 일치 거리
        private const double CollinearDot = -0.998; // 메인런 collinear 판정 (약 ±3.6°)
        private const double PerpDot      = 0.02;   // 분기 수직 판정 (약 ±1.15°)
        private const int    Magenta      = 6;      // 선홍색 ACI

        [CommandMethod("DuctOutline3", CommandFlags.UsePickSet)]
        public void Cmd_DuctOutline3()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [Step 0] Line 3개 선택
                    List<Line> lines = JEntity.GetEntityByTpye<Line>(
                        "Duct 중심선 3개를 선택 하세요 (메인런 2개 + 분기 1개)?",
                        JSelFilter.MakeFilterTypes("LINE"));
                    if (lines == null) return;
                    if (lines.Count != 3)
                    {
                        ed.WriteMessage($"\nLine 을 정확히 3개 선택해야 합니다 (현재 {lines.Count}개).");
                        return;
                    }

                    // 공통 접합점 O + 각 Line 의 반대편(far) 끝점
                    if (!TryFindJunction(lines, out Point3d O, out Point3d[] farEnds))
                    {
                        ed.WriteMessage("\n3개 Line 이 한 점(접합점 O)에서 만나지 않습니다.");
                        return;
                    }

                    Vector3d[] dir = new Vector3d[3];
                    for (int i = 0; i < 3; i++)
                        dir[i] = (farEnds[i] - O).GetNormal();

                    // 메인런 쌍(서로 반대 방향) 식별 → 나머지 1개가 분기(White)
                    int mi = -1, mj = -1;
                    double minDot = double.MaxValue;
                    for (int i = 0; i < 3; i++)
                        for (int j = i + 1; j < 3; j++)
                        {
                            double d = dir[i].DotProduct(dir[j]);
                            if (d < minDot) { minDot = d; mi = i; mj = j; }
                        }
                    if (minDot > CollinearDot)
                    {
                        ed.WriteMessage("\n메인런(일직선) 2개를 찾을 수 없습니다 — collinear 한 Line 2개가 필요합니다.");
                        return;
                    }
                    int wi = 3 - mi - mj; // 분기(White)

                    // 분기 수직(90°) 검증 — 비90° 분기는 미지원(에러)
                    if (Math.Abs(dir[wi].DotProduct(dir[mi])) > PerpDot)
                    {
                        ed.WriteMessage("\n분기 Line 이 메인런에 수직(90°)이 아닙니다 — 미지원.");
                        return;
                    }

                    // 폭 읽기 (XData "a")
                    if (!TryReadWidth(lines[mi], out double w_mi) ||
                        !TryReadWidth(lines[mj], out double w_mj) ||
                        !TryReadWidth(lines[wi], out double wW))
                    {
                        ed.WriteMessage("\nLine XData \"a\"(폭) 를 읽을 수 없습니다 — SetDuctWidth 로 폭을 먼저 기록하세요.");
                        return;
                    }

                    // Green = 넓은쪽(메인 시작), Red = 좁은쪽(전이 후). 리듀서는 Red 방향으로 전이.
                    int gi = w_mi >= w_mj ? mi : mj;
                    int ri = w_mi >= w_mj ? mj : mi;
                    double wG = Math.Max(w_mi, w_mj);
                    double wR = Math.Min(w_mi, w_mj);
                    double hG = wG / 2.0, hR = wR / 2.0;

                    Vector3d u = dir[ri]; // 메인런 방향 (Green→Red)
                    Vector3d n = dir[wi]; // 분기 방향 (상단 = +n)

                    Point3d Gfar = farEnds[gi]; // Green(넓은) 끝점 = 메인런 시작측
                    Point3d Rfar = farEnds[ri]; // Red(좁은) 끝점 = 전이 후측

                    // [Step 1] White trim 점 B = White 중심선 ∩ 상단벽(Green 측 +hG)
                    //          White 중심선 끝점을 B 로 이동(상단벽까지 정리)
                    Point3d B_white = O + n * hG;
                    MoveOEnd(lines[wi], O, B_white);

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    int created = 0;
                    double h = (wG - wR) / 2.0; // 편측 폭차

                    // [Step 3] 동일폭(h=0)이면 Reducer 생략 (Stretch 전용 로직은 설계상 추후)
                    if (h <= 1e-9)
                    {
                        // Reducer 없이 직선 측벽 2선만 (끝단 개방)
                        created += AddMagentaLine(tr, btr, db, Gfar + n * hG, Rfar + n * hG); // 상단벽
                        created += AddMagentaLine(tr, btr, db, Gfar - n * hG, Rfar - n * hG); // 하단벽
                        ed.WriteMessage("\n메인런 동일폭(h=0): Reducer 생략, 직선 측벽만 생성.");
                    }
                    else
                    {
                        // [Step 2] Stretch S = 1.5 × 분기폭, [Step 3] Reducer 수평 run A = h·tan60°
                        double S    = 1.5 * wW;
                        double Arun = h * Math.Sqrt(3.0); // tan60° = √3

                        Point3d Pg_top   = O + u * S          + n * hG; // B′ 상단 (Green측, +hG)
                        Point3d Pg_bot   = O + u * S          - n * hG; // B′ 하단 (Green측, −hG)
                        Point3d Pr_top   = O + u * (S + Arun) + n * hR; // A 상단 (Red측, +hR)
                        Point3d Pr_bot   = O + u * (S + Arun) - n * hR; // A 하단 (Red측, −hR)
                        Point3d A_center = O + u * (S + Arun);          // 중심선 새 교점 A

                        // [Step 1] 메인런 측벽 4선 (Green 측벽은 B′까지, Red 측벽은 A부터, 끝단 개방)
                        created += AddMagentaLine(tr, btr, db, Gfar + n * hG, Pg_top); // Green 상단벽
                        created += AddMagentaLine(tr, btr, db, Gfar - n * hG, Pg_bot); // Green 하단벽
                        created += AddMagentaLine(tr, btr, db, Pr_top, Rfar + n * hR); // Red 상단벽
                        created += AddMagentaLine(tr, btr, db, Pr_bot, Rfar - n * hR); // Red 하단벽

                        // [Step 3·4] Reducer 트라페조이드 = B′ 수직면 + 상단 사선 + A 수직면 + 하단 사선
                        created += AddMagentaLine(tr, btr, db, Pg_top, Pg_bot); // B′ 수직면 (Green, wG)
                        created += AddMagentaLine(tr, btr, db, Pg_top, Pr_top); // 상단 사선
                        created += AddMagentaLine(tr, btr, db, Pr_top, Pr_bot); // A 수직면 (Red, wR)
                        created += AddMagentaLine(tr, btr, db, Pg_bot, Pr_bot); // 하단 사선

                        // [Step 4] 중심선 조정: Green 연장 / Red 단축 (둘 다 O측 끝점 → A_center)
                        MoveOEnd(lines[gi], O, A_center);
                        MoveOEnd(lines[ri], O, A_center);
                    }

                    ed.WriteMessage($"\nDuctOutline3 완료: 외곽선 {created}개 생성 (측벽+Reducer). (wG={wG}, wR={wR}, 분기={wW})");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>3개 Line 모두가 끝점으로 공유하는 공통 접합점 O 와, 각 Line 의 반대편 끝점을 반환.</summary>
        private bool TryFindJunction(List<Line> lines, out Point3d O, out Point3d[] farEnds)
        {
            O = Point3d.Origin;
            farEnds = new Point3d[3];

            var candidates = new List<Point3d>();
            foreach (var ln in lines) { candidates.Add(ln.StartPoint); candidates.Add(ln.EndPoint); }

            foreach (var c in candidates)
            {
                bool allShare = lines.All(ln =>
                    ln.StartPoint.DistanceTo(c) <= JunctionTol ||
                    ln.EndPoint.DistanceTo(c) <= JunctionTol);
                if (!allShare) continue;

                O = c;
                for (int i = 0; i < 3; i++)
                    farEnds[i] = lines[i].StartPoint.DistanceTo(c) <= JunctionTol
                        ? lines[i].EndPoint : lines[i].StartPoint;
                return true;
            }
            return false;
        }

        /// <summary>Line 의 O측(가까운) 끝점을 newPt 로 이동.</summary>
        private void MoveOEnd(Line ln, Point3d O, Point3d newPt)
        {
            ln.UpgradeOpen();
            if (ln.StartPoint.DistanceTo(O) <= ln.EndPoint.DistanceTo(O))
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
            if (double.TryParse(s, out w)) return true;

            int idx = s.IndexOfAny(new[] { 'x', 'X', '*' });
            return idx > 0 && double.TryParse(s.Substring(0, idx).Trim(), out w);
        }
    }
}
