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
    /// Duct_C1 — 수직 기준선 aa(Green) + 교차점 X 좌우 직각 수평 기준선 bb(Red)/cc(Cyan) 선택 →
    /// aa 덕트 외곽선(Yellow, 레이어 "Duct_OutLine") 생성 + bb/cc 를 각각 3개 Line 으로 분리.
    /// 설계 기준: 건축\Duct-OutLine\DuctOutLine_Case_1.md (v1.4).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(dirAA/dirBB/dirCC) 기준으로 수행.
    /// </summary>
    public class DuctC1Command
    {
        private const double JunctionTol = 1e-3;  // X 접합점 일치 거리
        private const double PerpTol = 0.02;      // aa⊥bb, aa⊥cc 90° 판정 (약 ±1.15°)
        private const double BottomDrop = 200.0;  // 하부 마감 추가 깊이(고정값, v1.2)
        private const int Yellow = 2;             // 외곽선 ACI
        private const string OutlineLayer = "Duct_OutLine";

        [CommandMethod("Duct_C1", CommandFlags.UsePickSet)]
        public void Cmd_DuctC1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [4장] aa → bb(좌) → cc(우) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "기준선(aa, 수직)을 선택하세요?", out Line aa)) return;
                    if (!PickLine(ed, tr, "좌측 기준선(bb)을 선택하세요?", out Line bb)) return;
                    if (!PickLine(ed, tr, "우측 기준선(cc)을 선택하세요?", out Line cc)) return;

                    if (!TryApply(tr, db, aa, bb, cc, out string message))
                    {
                        ed.WriteMessage("\n" + message);
                        return;
                    }

                    ed.WriteMessage("\n" + message);
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// aa(수직 기준선) + bb(좌)/cc(우) 로부터 aa 외곽선 9개 생성 + bb/cc 3분할을 수행한다.
        /// 대화형 선택 없이 Line 객체를 직접 받아 검증~계산~생성까지 처리 — DuctTreeOutlineCommand 등에서 재사용.
        /// 검증 실패 시 false 반환(엔티티 생성 없음, Transaction 은 변경 없음), 성공 시 true + 결과 메시지.
        /// </summary>
        public bool TryApply(Transaction tr, Database db, Line aa, Line bb, Line cc, out string message)
        {
            message = "";

            if (aa.ObjectId == bb.ObjectId || bb.ObjectId == cc.ObjectId || aa.ObjectId == cc.ObjectId)
            {
                message = "[E01] 세 Line 은 서로 달라야 합니다.";
                return false;
            }

            // [3장] XData "a" 폭 읽기
            if (!TryReadWidth(aa, out double Waa))
            {
                message = "[E02] aa XData \"a\"(폭) 를 읽을 수 없습니다.";
                return false;
            }
            if (!TryReadWidth(bb, out double Wbb))
            {
                message = "[E02] bb XData \"a\"(폭) 를 읽을 수 없습니다.";
                return false;
            }
            if (!TryReadWidth(cc, out double Wcc))
            {
                message = "[E02] cc XData \"a\"(폭) 를 읽을 수 없습니다.";
                return false;
            }
            if (Waa <= 0 || Wbb <= 0 || Wcc <= 0)
            {
                message = "[E03] 폭은 0 보다 커야 합니다.";
                return false;
            }

            // [4장] 교차점 X = aa 위 임의의 점(aa.StartPoint)을 cc(연장 포함)에 투영
            Point3d X = cc.GetClosestPointTo(aa.StartPoint, true);

            Point3d aaFar = FarEnd(aa, X);
            Point3d bbFar = FarEnd(bb, X);
            Point3d ccFar = FarEnd(cc, X);

            Point3d aaNear = NearEnd(aa, X);
            Point3d bbNear = NearEnd(bb, X);
            Point3d ccNear = NearEnd(cc, X);

            // [2장] aa/bb/cc 의 X 측 끝점이 실제로 X 와 일치하는지 확인
            if (aaNear.DistanceTo(X) > JunctionTol || bbNear.DistanceTo(X) > JunctionTol || ccNear.DistanceTo(X) > JunctionTol)
            {
                message = "[E05] aa/bb/cc 의 한쪽 끝점이 교차점 X 와 일치하지 않습니다.";
                return false;
            }

            Vector3d dirAA = (aaFar - X).GetNormal();
            Vector3d dirBB = (bbFar - X).GetNormal();
            Vector3d dirCC = (ccFar - X).GetNormal();

            // [2장] aa⊥bb, aa⊥cc 검증
            if (System.Math.Abs(dirAA.DotProduct(dirBB)) > PerpTol || System.Math.Abs(dirAA.DotProduct(dirCC)) > PerpTol)
            {
                message = "[E04] aa 가 bb/cc 와 직각이 아닙니다.";
                return false;
            }

            // [2장] 파생 치수
            double Haa = Waa / 2.0;
            double Hbb = Wbb / 2.0;
            double Hcc = Wcc / 2.0;
            double dBb = Wbb / 4.0; // Δbb
            double dCc = Wcc / 4.0; // Δcc

            // [6.1/6.2장] 분할점
            Point3d b1 = X + dirBB * Haa;
            Point3d b2 = b1 + dirBB * dBb;
            Point3d c1 = X + dirCC * Haa;
            Point3d c2 = c1 + dirCC * dCc;

            // [6.3장] bb/cc 길이 충분성 검증 (분할점을 지나 원래 끝점까지 여유가 있어야 함)
            if (bbFar.DistanceTo(X) < b2.DistanceTo(X) + JunctionTol)
            {
                message = "[E06] bb 길이가 부족합니다.";
                return false;
            }
            if (ccFar.DistanceTo(X) < c2.DistanceTo(X) + JunctionTol)
            {
                message = "[E06] cc 길이가 부족합니다.";
                return false;
            }

            // [7.1장] aa 길이(y_top) 검증 — 리듀서 사선 시작 높이보다 길어야 함
            double yTop = aaFar.DistanceTo(X);
            if (yTop < System.Math.Max(Hbb + dBb, Hcc + dCc) + JunctionTol)
            {
                message = "[E07] aa 길이가 부족합니다.";
                return false;
            }

            // [7.2.1장] 하부 마감 깊이
            double yBot = System.Math.Max(Hbb, Hcc) + BottomDrop;

            // [7.2.2장 v1.4] 외곽선 정점 12점 — 연속 수직 2 + 하부 수평 + 리듀서(사선·끝단 수직·하단 수평) x 2
            Vector3d up = dirAA;
            Point3d PA = b1 + up * yTop;          // 좌측 수직 상단   (−Haa, yTop)
            Point3d PB = b1 + up * (Hbb + dBb);   // bb 사선 시작     (−Haa, +Hbb+Δbb)
            Point3d PC = b2 + up * Hbb;           // bb 사선 끝       (−Haa−Δbb, +Hbb)
            Point3d PD = b2 - up * Hbb;           // bb 리듀서 하단 끝 (−Haa−Δbb, −Hbb)
            Point3d PE = b1 - up * Hbb;           // bb 하단·aa 외곽 교점 (−Haa, −Hbb)
            Point3d PF = b1 - up * yBot;          // 좌측 수직 하단   (−Haa, −yBot)
            Point3d PG = c1 - up * yBot;          // 우측 수직 하단   (+Haa, −yBot)
            Point3d PH = c1 - up * Hcc;           // cc 하단·aa 외곽 교점 (+Haa, −Hcc)
            Point3d PI = c2 - up * Hcc;           // cc 리듀서 하단 끝 (+Haa+Δcc, −Hcc)
            Point3d PJ = c2 + up * Hcc;           // cc 사선 끝       (+Haa+Δcc, +Hcc)
            Point3d PK = c1 + up * (Hcc + dCc);   // cc 사선 시작     (+Haa, +Hcc+Δcc)
            Point3d PL = c1 + up * yTop;          // 우측 수직 상단   (+Haa, yTop)

            foreach (var p in new[] { b1, b2, c1, c2, PA, PB, PC, PD, PE, PF, PG, PH, PI, PJ, PK, PL })
            {
                if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                {
                    message = "[E08] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.";
                    return false;
                }
            }

            var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            // [7.3장] 레이어 준비
            tr.CreateLayer(OutlineLayer, Yellow, LineWeight.ByLayer);

            // [6.3장] bb/cc 분리 (신규 Line 2개 + 원본 X측 끝점 이동), XData/속성 승계
            SplitBranch(tr, btr, db, bb, X, b1, b2);
            SplitBranch(tr, btr, db, cc, X, c1, c2);

            // [7장 v1.4] aa 외곽선(Yellow) 9개 — 연속 수직 2 + 하부 수평 1 + 리듀서 3선 x 2
            int created = 0;
            created += AddOutlineLine(tr, btr, db, PA, PF); // 좌측 수직 (연속, 끊김 없음)
            created += AddOutlineLine(tr, btr, db, PL, PG); // 우측 수직 (연속, 끊김 없음)
            created += AddOutlineLine(tr, btr, db, PF, PG); // 하부 수평
            created += AddOutlineLine(tr, btr, db, PB, PC); // bb 리듀서 사선 (상단 1개)
            created += AddOutlineLine(tr, btr, db, PC, PD); // bb 리듀서 끝단 수직
            created += AddOutlineLine(tr, btr, db, PD, PE); // bb 리듀서 하단 수평
            created += AddOutlineLine(tr, btr, db, PK, PJ); // cc 리듀서 사선 (상단 1개)
            created += AddOutlineLine(tr, btr, db, PJ, PI); // cc 리듀서 끝단 수직
            created += AddOutlineLine(tr, btr, db, PI, PH); // cc 리듀서 하단 수평

            message = $"Duct_C1 완료: aa 외곽선 {created}개 생성, bb/cc 각 3분할 " +
                      $"(Waa={Waa}, Wbb={Wbb}, Wcc={Wcc}).";
            return true;
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

        /// <summary>branch(bb 또는 cc)를 X→p1, p1→p2 신규 Line 2개로 분리하고 원본의 X측 끝점을 p2로 이동.
        /// 신규 Line 은 원본의 색상/레이어/선종류/XData 를 그대로 승계한다.</summary>
        private void SplitBranch(Transaction tr, BlockTableRecord btr, Database db, Line branch, Point3d X, Point3d p1, Point3d p2)
        {
            CreateBranchSegment(tr, btr, db, X, p1, branch);
            CreateBranchSegment(tr, btr, db, p1, p2, branch);
            MoveEnd(branch, X, p2);
        }

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
