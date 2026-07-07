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
    /// Duct_C2 — 수평 기준선 aa(Green) + 동일선상 bb(Red) + 직각 상향 cc(Cyan) 선택 →
    /// aa 덕트 외곽선(Yellow, 레이어 "Duct_OutLine") 10선 생성
    /// + Green 연장 2선(X–B1, B1–B2) + Red X측 끝점 B2 이동 + cc 3분할(X–c1, c1–c2, 원본 수정).
    /// 설계 기준: 건축\Duct-OutLine\Duct_OutLine_Case2.md (v1.5).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(dirAA/dirBB/dirCC) 기준으로 수행.
    /// </summary>
    public class DuctC2Command
    {
        private const double JunctionTol = 1e-3;   // X 접합점 일치 거리
        private const double PerpTol = 0.02;       // 직각/역방향 판정 (약 ±1.15°)
        private const int Yellow = 2;              // 외곽선 ACI
        private const string OutlineLayer = "Duct_OutLine";

        [CommandMethod("Duct_C2", CommandFlags.UsePickSet)]
        public void Cmd_DuctC2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // [4장] aa → bb(동일선상) → cc(직각) 순서로 1개씩 선택
                    if (!PickLine(ed, tr, "기준선(aa, 수평)을 선택하세요?", out Line aa)) return;
                    if (!PickLine(ed, tr, "동일선상 기준선(bb)을 선택하세요?", out Line bb)) return;
                    if (!PickLine(ed, tr, "직각 기준선(cc)을 선택하세요?", out Line cc)) return;

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
        /// aa(수평 기준선) + 동일선상 bb + 직각 cc 로부터 aa 외곽선 10선 생성 + Green 연장 2선 +
        /// bb 끝점 B2 이동 + cc 3분할을 수행한다.
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
            if (!TryReadWidth(aa, out double Waa)) { message = "[E02] aa XData \"a\"(폭) 를 읽을 수 없습니다."; return false; }
            if (!TryReadWidth(bb, out double Wbb)) { message = "[E02] bb XData \"a\"(폭) 를 읽을 수 없습니다."; return false; }
            if (!TryReadWidth(cc, out double Wcc)) { message = "[E02] cc XData \"a\"(폭) 를 읽을 수 없습니다."; return false; }

            if (Waa <= 0 || Wbb <= 0 || Wcc <= 0)
            {
                message = "[E03] 폭은 0 보다 커야 합니다.";
                return false;
            }
            if (Waa <= Wbb)
            {
                message = "[E04] 축소 조건(W_aa > W_bb)을 만족하지 않습니다.";
                return false;
            }

            // [4장] 교차점 X = cc 위 임의의 점(cc.StartPoint)을 aa(연장 포함)에 수직 투영
            Point3d X = aa.GetClosestPointTo(cc.StartPoint, true);

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

            Vector3d dirAA = (aaFar - X).GetNormal();   // 좌측(상류) 방향
            Vector3d dirBB = (bbFar - X).GetNormal();   // 우측(하류) 방향
            Vector3d dirCC = (ccFar - X).GetNormal();   // 상향(분기) 방향

            // [2장] cc ⊥ aa 검증
            if (System.Math.Abs(dirAA.DotProduct(dirCC)) > PerpTol)
            {
                message = "[E06] cc 가 aa 와 직각이 아닙니다.";
                return false;
            }

            // [2장] aa·bb 동일선상(서로 반대 방향) 검증
            if (System.Math.Abs(dirAA.DotProduct(dirBB) + 1.0) > PerpTol)
            {
                message = "[E07] bb 가 aa 와 동일선상(반대 방향)이 아닙니다.";
                return false;
            }

            // [6장] 파생 치수
            double Haa = Waa / 2.0;
            double Hbb = Wbb / 2.0;
            double Hcc = Wcc / 2.0;
            double hh = (Waa - Wbb) / 2.0;                                      // 편측 낙차 (v1.1)
            double Lred = hh / System.Math.Tan(30.0 * System.Math.PI / 180.0); // B1–B2, 사선 수평과 30° (v1.2)
            double Lcol = Wcc / 4.0;                                            // c1–c2 = 컬러 높이

            // [7장] 분할·연장 점
            Point3d B1 = X + dirBB * (1.5 * Wcc);   // 리듀서 시작 (v1.1: X–B1 = 1.5 × W_cc)
            Point3d B2 = B1 + dirBB * Lred;         // 리듀서 끝 = bb 폭 시작
            Point3d c1 = X + dirCC * Haa;           // cc 와 aa 상단 외곽 교차점
            Point3d c2 = c1 + dirCC * Lcol;         // 컬러 상단 높이

            // 길이 충분성 검증
            if (bbFar.DistanceTo(X) < B2.DistanceTo(X) + JunctionTol)
            {
                message = "[E08] bb 길이가 부족합니다 (B2 를 지나야 합니다).";
                return false;
            }
            if (ccFar.DistanceTo(X) < c2.DistanceTo(X) + JunctionTol)
            {
                message = "[E08] cc 길이가 부족합니다 (c2 를 지나야 합니다).";
                return false;
            }
            if (aaFar.DistanceTo(X) < Hcc + Lcol + JunctionTol)
            {
                message = "[E09] aa 길이가 부족합니다 (컬러 사선 시작점보다 길어야 합니다).";
                return false;
            }

            // [8장 v1.5] 외곽선 정점 — 본체 2 + 좌측 마감 1 + 컬러 3 + 리듀서 4 (총 10선)
            Vector3d up = dirCC;
            Point3d T1 = aaFar + up * Haa;                      // 본체 상단 좌측 끝
            Point3d T2 = B1 + up * Haa;                         // 본체 상단 우측 끝 = 리듀서 B1 상단
            Point3d U1 = aaFar - up * Haa;                      // 본체 하단 좌측 끝
            Point3d U2 = B1 - up * Haa;                         // 본체 하단 우측 끝 = 리듀서 B1 하단
            Point3d S1 = X + dirAA * (Hcc + Lcol) + up * Haa;   // 컬러 45° 사선 시작 (상류측)
            Point3d S2 = X + dirAA * Hcc + up * (Haa + Lcol);   // 컬러 45° 사선 끝 = 컬러 상단 좌측
            Point3d S3 = X + dirBB * Hcc + up * (Haa + Lcol);   // 컬러 상단 우측
            Point3d S4 = X + dirBB * Hcc + up * Haa;            // 컬러 우측 수직 하단
            Point3d R3 = B2 + up * Hbb;                         // 리듀서 B2 상단
            Point3d R4 = B2 - up * Hbb;                         // 리듀서 B2 하단

            foreach (var p in new[] { B1, B2, c1, c2, T1, T2, U1, U2, S1, S2, S3, S4, R3, R4 })
            {
                if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                {
                    message = "[E10] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.";
                    return false;
                }
            }

            var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            // [8.4장] 레이어 준비
            tr.CreateLayer(OutlineLayer, Yellow, LineWeight.ByLayer);

            // [7.2장] Green(aa) 연장 2선 생성 (원본 유지, aa 속성·XData 승계)
            CreateBranchSegment(tr, btr, db, X, B1, aa);
            CreateBranchSegment(tr, btr, db, B1, B2, aa);

            // [7.2장] Red(bb) X측 끝점을 B2 로 이동 (분할 없음)
            MoveEnd(bb, X, B2);

            // [7.1장] cc 3분할 (신규 X–c1, c1–c2 + 원본 X측 끝점 c2 이동)
            SplitBranch(tr, btr, db, cc, X, c1, c2);

            // [8장 v1.5] aa 외곽선(Yellow) 10선
            int created = 0;
            created += AddOutlineLine(tr, btr, db, T1, T2); // 본체 상단 (연속, 끊김 없음)
            created += AddOutlineLine(tr, btr, db, U1, U2); // 본체 하단 (연속, 끊김 없음)
            created += AddOutlineLine(tr, btr, db, T1, U1); // 좌측 끝 수직 마감 (v1.5)
            created += AddOutlineLine(tr, btr, db, S1, S2); // 컬러 45° 사선 (상류측 1개)
            created += AddOutlineLine(tr, btr, db, S2, S3); // 컬러 상단 수평
            created += AddOutlineLine(tr, btr, db, S3, S4); // 컬러 우측 수직
            created += AddOutlineLine(tr, btr, db, T2, U2); // 리듀서 B1 수직 (폭 W_aa 이음선, v1.4)
            created += AddOutlineLine(tr, btr, db, T2, R3); // 리듀서 상단 사선 (수평과 30°)
            created += AddOutlineLine(tr, btr, db, U2, R4); // 리듀서 하단 사선
            created += AddOutlineLine(tr, btr, db, R3, R4); // 리듀서 B2 수직 (폭 W_bb 이음선)

            message = $"Duct_C2 완료: aa 외곽선 {created}개 생성, Green 연장 2선, bb 끝점 B2 이동, cc 3분할 " +
                      $"(Waa={Waa}, Wbb={Wbb}, Wcc={Wcc}, hh={hh}, B1-B2={Lred:F2}).";
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

        /// <summary>branch(cc)를 X→p1, p1→p2 신규 Line 2개로 분리하고 원본의 X측 끝점을 p2로 이동.
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
