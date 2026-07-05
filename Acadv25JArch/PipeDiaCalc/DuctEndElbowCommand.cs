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
    /// Duct_EE — 말단 상향 분기(End Elbow). 수평 a(Green) → 직각 상향 b(Red)를
    /// 유체 흐름 방향 순서로 선택하면 a 덕트 외곽선(Yellow, 레이어 "Duct_OutLine")
    /// 6선(본체 상·하 연속 2 + 말단 마감 1 + 컬러 3)을 생성하고,
    /// a는 원본 유지 + X–a1(길이 = W_b) 연장 1선 신규(a 승계),
    /// b는 X–b1, b1–b2 신규 + 원본 b2부터로 수정(3분할, b 승계).
    /// 컬러 45° 사선은 흐름 상류측 1개, b1–b2 = W_b/4.
    /// 설계 기준: 건축\Duct-OutLine\Duct_End_Elbow.md (v1.1).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(dirA/dirB) 기준으로 수행.
    /// </summary>
    public class DuctEndElbowCommand
    {
        private const double JunctionTol = 1e-3;   // X 접합점 일치 거리
        private const double PerpTol = 0.02;       // 직각 판정 (약 ±1.15°)
        private const int Yellow = 2;              // 외곽선 ACI
        private const string OutlineLayer = "Duct_OutLine";

        [CommandMethod("Duct_EE", CommandFlags.UsePickSet)]
        public void Cmd_DuctEE()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [4장] a(흐름 상류) → b(흐름 하류) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "기준선(a, Green, 흐름 상류)을 선택하세요?", out Line a)) return;
                    if (!PickLine(ed, tr, "직각 기준선(b, Red, 흐름 하류)을 선택하세요?", out Line b)) return;

                    if (a.ObjectId == b.ObjectId)
                    {
                        ed.WriteMessage("\n[E01] 두 Line 은 서로 달라야 합니다.");
                        return;
                    }

                    // [3장] XData "a" 폭 읽기
                    if (!TryReadWidth(a, out double Wa)) { ed.WriteMessage("\n[E02] a XData \"a\"(폭) 를 읽을 수 없습니다."); return; }
                    if (!TryReadWidth(b, out double Wb)) { ed.WriteMessage("\n[E02] b XData \"a\"(폭) 를 읽을 수 없습니다."); return; }
                    if (Wa <= 0 || Wb <= 0)
                    {
                        ed.WriteMessage("\n[E03] 폭은 0 보다 커야 합니다.");
                        return;
                    }

                    // [4장] 교차점 X — b 위 임의의 점(b.StartPoint)을 a(연장 포함)에 수직 투영
                    Point3d X = a.GetClosestPointTo(b.StartPoint, true);

                    Point3d aFar = FarEnd(a, X);
                    Point3d bFar = FarEnd(b, X);
                    Point3d aNear = NearEnd(a, X);
                    Point3d bNear = NearEnd(b, X);

                    // [2장] a/b 의 X 측 끝점이 실제로 X 와 일치하는지 확인
                    if (aNear.DistanceTo(X) > JunctionTol || bNear.DistanceTo(X) > JunctionTol)
                    {
                        ed.WriteMessage("\n[E04] a/b 의 한쪽 끝점이 교차점 X 와 일치하지 않습니다.");
                        return;
                    }

                    Vector3d dirA = (X - aFar).GetNormal();   // a 의 유체 흐름 방향 (a 먼 끝 → X)
                    Vector3d dirB = (bFar - X).GetNormal();   // b 의 흐름 방향 (상향)

                    // [2장] b ⊥ a 검증
                    if (System.Math.Abs(dirA.DotProduct(dirB)) > PerpTol)
                    {
                        ed.WriteMessage("\n[E05] b 가 a 와 직각이 아닙니다.");
                        return;
                    }

                    // [6장] 파생 치수
                    double Ha = Wa / 2.0;
                    double Hb = Wb / 2.0;
                    double Lcol = Wb / 4.0;   // b1–b2 = 컬러 높이 = W_b / 4

                    // [6장] 분할·연장 점
                    Point3d a1 = X + dirA * Wb;          // 말단 연장 끝 (연장 길이 = b 의 폭)
                    Point3d b1 = X + dirB * Ha;          // b 와 a 상단 외곽 교차점
                    Point3d b2 = b1 + dirB * Lcol;       // 컬러 상단 높이

                    // [9장 3번] 길이 충분성 검증
                    if (aFar.DistanceTo(X) < Hb + Lcol + JunctionTol)
                    {
                        ed.WriteMessage("\n[E06] a 길이가 부족합니다 (컬러 사선 시작점보다 길어야 합니다).");
                        return;
                    }
                    if (bFar.DistanceTo(X) < b2.DistanceTo(X) + JunctionTol)
                    {
                        ed.WriteMessage("\n[E06] b 길이가 부족합니다 (b2 를 지나야 합니다).");
                        return;
                    }

                    // [8장] 외곽선 정점 — 본체 2 + 말단 마감 1 + 컬러 3 (총 6선)
                    Vector3d up = dirB;
                    Point3d T1 = aFar + up * Ha;                        // 본체 상단 시작 (a 먼 끝)
                    Point3d T2 = a1 + up * Ha;                          // 본체 상단 끝 (a1)
                    Point3d U1 = aFar - up * Ha;                        // 본체 하단 시작
                    Point3d U2 = a1 - up * Ha;                          // 본체 하단 끝
                    Point3d S1 = X - dirA * (Hb + Lcol) + up * Ha;      // 컬러 45° 사선 시작 (흐름 상류측)
                    Point3d S2 = X - dirA * Hb + up * (Ha + Lcol);      // 컬러 45° 사선 끝 = 상단 좌측
                    Point3d S3 = X + dirA * Hb + up * (Ha + Lcol);      // 컬러 상단 우측
                    Point3d S4 = X + dirA * Hb + up * Ha;               // 컬러 우측 수직 하단

                    foreach (var p in new[] { a1, b1, b2, T1, T2, U1, U2, S1, S2, S3, S4 })
                    {
                        if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                        {
                            ed.WriteMessage("\n[E07] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.");
                            return;
                        }
                    }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // [8장] 레이어 준비
                    tr.CreateLayer(OutlineLayer, Yellow, LineWeight.ByLayer);

                    // [7장] a(Green) 연장 1선 생성 (원본 유지, a 속성·XData 승계)
                    CreateBranchSegment(tr, btr, db, X, a1, a);

                    // [7장] b 3분할 (신규 X–b1, b1–b2 + 원본 X측 끝점 b2 이동, b 승계)
                    SplitBranch(tr, btr, db, b, X, b1, b2);

                    // [8장] a 외곽선(Yellow) 6선
                    int created = 0;
                    created += AddOutlineLine(tr, btr, db, T1, T2); // 1. 본체 상단 (연속, 끊김 없음)
                    created += AddOutlineLine(tr, btr, db, U1, U2); // 2. 본체 하단 (연속, 끊김 없음)
                    created += AddOutlineLine(tr, btr, db, T2, U2); // 3. 말단 마감 수직 (a1 위치)
                    created += AddOutlineLine(tr, btr, db, S1, S2); // 4. 컬러 45° 사선 (흐름 상류측 1개)
                    created += AddOutlineLine(tr, btr, db, S2, S3); // 5. 컬러 상단 수평
                    created += AddOutlineLine(tr, btr, db, S3, S4); // 6. 컬러 우측 수직

                    ed.WriteMessage($"\nDuct_EE 완료: a 외곽선 {created}개 생성, Green 연장 1선(X–a1, 길이 {Wb}), b 3분할 " +
                                    $"(Wa={Wa}, Wb={Wb}, b1-b2={Lcol}).");
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

        /// <summary>Line 의 기준점 refPt 와 가까운 끝점만 newPt 로 이동.</summary>
        private void MoveEnd(Line ln, Point3d refPt, Point3d newPt)
        {
            ln.UpgradeOpen();
            if (ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt))
                ln.StartPoint = newPt;
            else
                ln.EndPoint = newPt;
        }

        /// <summary>branch(b)를 X→p1, p1→p2 신규 Line 2개로 분리하고 원본의 X측 끝점을 p2로 이동.
        /// 신규 Line 은 원본의 색상/레이어/선종류/XData 를 그대로 승계한다.</summary>
        private void SplitBranch(Transaction tr, BlockTableRecord btr, Database db, Line branch, Point3d X, Point3d p1, Point3d p2)
        {
            CreateBranchSegment(tr, btr, db, X, p1, branch);
            CreateBranchSegment(tr, btr, db, p1, p2, branch);
            MoveEnd(branch, X, p2);
        }

        /// <summary>a→b 신규 Line 생성 후 source 의 색상/레이어/선종류/선가중치/XData 승계.</summary>
        private void CreateBranchSegment(Transaction tr, BlockTableRecord btr, Database db, Point3d a, Point3d b, Line source)
        {
            var ln = new Line(a, b);
            ln.SetDatabaseDefaults(db);
            btr.AppendEntity(ln);
            tr.AddNewlyCreatedDBObject(ln, true);
            ln.Layer = source.Layer;
            ln.Color = source.Color;
            ln.Linetype = source.Linetype;
            ln.LineWeight = source.LineWeight;
            source.XdataCopy(ln);
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
