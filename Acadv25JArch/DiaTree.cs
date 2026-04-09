using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class DiaTree
    {
        private const double OFFSET    = 0.5; // E 확장 거리 / G 오프셋
        private const double S_LENGTH  = 3.5; // foot1 → S 거리

        /// <summary>
        /// DiaTree: 선택된 흰색 Line들 + 삼각위치 기반 폴리라인 생성
        ///
        ///  foot1 = 삼각위치에서 가장 가까운 Line의 수선의 발
        ///  foot2 = 삼각위치에서 가장 먼   Line의 수선의 발
        ///
        ///  S : foot1 → clickPt 방향으로 3.5
        ///  E : foot2를 clickPt 반대방향으로 0.5 확장
        ///  F : E에서 선 방향(라인 방향)으로 span 이동
        ///  G : nearest 라인의 먼쪽 끝점에서 perpDir 방향으로 0.5
        ///  K : G를 S-E 선분에 수직 투영
        ///
        ///  Polyline: S → E → F → G → K
        /// </summary>
        [CommandMethod("cmd_DiaTree", CommandFlags.UsePickSet)]
        public void Cmd_DiaTree()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                // ── 1. 라인 선택 ──────────────────────────────────────────
                PromptSelectionResult selResult = ed.SelectImplied();
                if (selResult.Status != PromptStatus.OK)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = "\n흰색 Line들을 선택하세요: "
                    };
                    var filter = new SelectionFilter(
                        new[] { new TypedValue((int)DxfCode.Start, "LINE") });
                    selResult = ed.GetSelection(selOpts, filter);
                }
                if (selResult.Status != PromptStatus.OK) return;
                ed.SetImpliedSelection(new ObjectId[0]);

                // ── 2. 삼각위치(방향 결정) 클릭 ────────────────────────────
                PromptPointResult ptResult = ed.GetPoint("\n방향을 결정할 삼각위치를 클릭하세요: ");
                if (ptResult.Status != PromptStatus.OK) return;
                Point3d clickPt = ptResult.Value;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // ── 3. 선택 라인 수집 ────────────────────────────────────
                    var lines = new List<Line>();
                    foreach (SelectedObject so in selResult.Value)
                    {
                        if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Line l)
                            lines.Add(l);
                    }

                    if (lines.Count < 2)
                    {
                        ed.WriteMessage("\n2개 이상의 라인을 선택해야 합니다.");
                        tr.Commit();
                        return;
                    }

                    // ── 4. footPoint 거리 순 정렬 (extend=false) ────────────
                    var sorted = lines
                        .Select(l => new
                        {
                            Line = l,
                            Foot = l.GetClosestPointTo(clickPt, false)
                        })
                        .OrderBy(x => clickPt.DistanceTo(x.Foot))
                        .ToList();

                    Line nearestLine  = sorted.First().Line;  // clickPt에 가장 가까운 라인
                    Line farthestLine = sorted.Last().Line;   // clickPt에서 가장 먼 라인

                    // ── 5. foot 재계산 (extend=true: 정확한 수선의 발) ────────
                    Point3d foot1 = nearestLine.GetClosestPointTo(clickPt, true);
                    Point3d foot2 = farthestLine.GetClosestPointTo(clickPt, true);

                    // ── 6. 방향 벡터 ──────────────────────────────────────────
                    Vector3d dirToClick = (clickPt - foot1).GetNormal(); // foot1 → clickPt
                    Vector3d dirToFar   = (foot2 - clickPt).GetNormal(); // clickPt → foot2

                    // ── 7. S 점: foot1에서 clickPt 방향으로 S_LENGTH ──────────
                    Point3d S = foot1 + dirToClick * S_LENGTH;

                    // ── 8. E 점: foot2를 clickPt 반대방향으로 OFFSET 확장 ─────
                    Point3d E = foot2 + dirToFar * OFFSET;

                    // ── 9. S-E 방향 / EF 방향 (Z축 기준 SE를 90° 시계방향) ────
                    //  seDir  : S → E 방향
                    //  efDir  : seDir 를 Z축 기준 90° 시계방향 회전
                    //           (dx,dy,0) → (dy,-dx,0), 이미 단위벡터
                    Vector3d seDir = (E - S).GetNormal();
                    Vector3d efDir = new Vector3d(seDir.Y, -seDir.X, 0);

                    // ── 10. F 점: E에서 efDir 방향으로 OFFSET(0.5) ────────────
                    Point3d F = E + efDir * OFFSET;

                    // ── 11. K 점: foot1을 S-E 선에 투영, S 방향으로 OFFSET ────
                    //  foot1OnSE : foot1의 S-E 선 수직 투영점 (S-E 선 위)
                    //  K         : foot1OnSE 에서 S 방향(-seDir)으로 OFFSET 이동
                    double  foot1ProjLen = (foot1 - S).DotProduct(seDir);
                    Point3d foot1OnSE   = S + seDir * foot1ProjLen;
                    Point3d K           = foot1OnSE + (-seDir) * OFFSET;

                    // ── 12. G 점: K에서 efDir 방향으로 OFFSET (G-K = 0.5) ─────
                    //  F-G 방향 = -seDir (S-E 반대방향) 자동 성립
                    Point3d G = K + efDir * OFFSET;

                    // ── 13. Polyline 생성: S → E → F → G → K ─────────────────
                    var pline = new Polyline();
                    pline.AddVertexAt(0, new Point2d(S.X, S.Y), 0, 0, 0);
                    pline.AddVertexAt(1, new Point2d(E.X, E.Y), 0, 0, 0);
                    pline.AddVertexAt(2, new Point2d(F.X, F.Y), 0, 0, 0);
                    pline.AddVertexAt(3, new Point2d(G.X, G.Y), 0, 0, 0);
                    pline.AddVertexAt(4, new Point2d(K.X, K.Y), 0, 0, 0);
                    pline.Closed = false;

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    btr.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);

                    // ── 14. 결과 출력 ─────────────────────────────────────────
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n  DiaTree 폴리라인 생성 완료");
                    ed.WriteMessage($"\n  라인 수       : {lines.Count}개");
                    ed.WriteMessage($"\n  nearest 라인  : 길이={nearestLine.Length:F3}");
                    ed.WriteMessage($"\n  farthest 라인 : 길이={farthestLine.Length:F3}");
                    ed.WriteMessage($"\n  foot1  : ({foot1.X:F3}, {foot1.Y:F3})");
                    ed.WriteMessage($"\n  foot2  : ({foot2.X:F3}, {foot2.Y:F3})");
                    ed.WriteMessage($"\n  ----------------------------------------");
                    ed.WriteMessage($"\n  S : ({S.X:F3}, {S.Y:F3})  [foot1→clickPt × {S_LENGTH}]");
                    ed.WriteMessage($"\n  E : ({E.X:F3}, {E.Y:F3})  [foot2 확장 {OFFSET}]");
                    ed.WriteMessage($"\n  F : ({F.X:F3}, {F.Y:F3})  [E + SE_90°CW × {OFFSET}]");
                    ed.WriteMessage($"\n  G : ({G.X:F3}, {G.Y:F3})  [K + efDir × {OFFSET}]");
                    ed.WriteMessage($"\n  K : ({K.X:F3}, {K.Y:F3})  [foot1_onSE → S방향 × {OFFSET}]");
                    ed.WriteMessage("\n========================================");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
    

        /// <summary>
        /// DiaTreeVer: 수직 Line 선택 + 기준점 X 클릭 → 폴리라인 S-E-F-G-H 생성
        ///
        ///  foot1 : X에서 가장 가까운 수직 Line의 수선의 발 (점 1)
        ///  foot2 : X에서 가장 먼  수직 Line의 수선의 발 (점 2)
        ///
        ///  S : foot1 + (foot1→X 방향) × 3.5
        ///  E : foot2 + (X→foot2 방향) × 0.5
        ///  F : E + (0, +0.5, 0)
        ///  H : foot1 + (foot1→X 방향) × 0.5
        ///  G : H + (0, +0.5, 0)
        ///
        ///  Polyline: S → E → F → G → H
        /// </summary>
        [CommandMethod("cmd_DiaTreeVer", CommandFlags.UsePickSet)]
        public void Cmd_DiaTreeVer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            const double OFFSET_V   = 0.5;  // E→foot2 확장 / 수직 0.5 / H 거리
            const double S_LEN      = 3.5;  // foot1→S 거리

            try
            {
                // ── 1. 수직 Line 선택 ─────────────────────────────────────
                PromptSelectionResult selResult = ed.SelectImplied();
                if (selResult.Status != PromptStatus.OK)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = "\n수직 Line들을 선택하세요: "
                    };
                    var filter = new SelectionFilter(
                        new[] { new TypedValue((int)DxfCode.Start, "LINE") });
                    selResult = ed.GetSelection(selOpts, filter);
                }
                if (selResult.Status != PromptStatus.OK) return;
                ed.SetImpliedSelection(new ObjectId[0]);

                // ── 2. 기준점 X 클릭 ──────────────────────────────────────
                PromptPointResult ptResult = ed.GetPoint("\n기준점 X를 클릭하세요: ");
                if (ptResult.Status != PromptStatus.OK) return;
                Point3d X = ptResult.Value;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // ── 3. 선택 라인 수집 ──────────────────────────────────
                    var lines = new List<Line>();
                    foreach (SelectedObject so in selResult.Value)
                    {
                        if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Line l)
                            lines.Add(l);
                    }
                    if (lines.Count < 2)
                    {
                        ed.WriteMessage("\n2개 이상의 수직 라인을 선택해야 합니다.");
                        tr.Commit();
                        return;
                    }

                    // ── 4. X에서 각 라인까지 거리 정렬 ────────────────────
                    var sorted = lines
                        .Select(l => new
                        {
                            Line = l,
                            Foot = l.GetClosestPointTo(X, true)
                        })
                        .OrderBy(x => X.DistanceTo(x.Foot))
                        .ToList();

                    Point3d foot1 = sorted.First().Foot;  // 가장 가까운 수직라인 수선의 발
                    Point3d foot2 = sorted.Last().Foot;   // 가장 먼  수직라인 수선의 발

                    // ── 5. 방향 벡터 ───────────────────────────────────────
                    Vector3d dirFoot1ToX  = (X     - foot1).GetNormal(); // foot1 → X
                    Vector3d dirXToFoot2  = (foot2 - X    ).GetNormal(); // X → foot2

                    // ── 6. 각 점 계산 ──────────────────────────────────────
                    Point3d S = foot1 + dirFoot1ToX * S_LEN;          // foot1→X 방향 3.5
                    Point3d E = foot2 + dirXToFoot2 * OFFSET_V;       // foot2→반대 0.5
                    Point3d F = new Point3d(E.X, E.Y + OFFSET_V, 0);  // E 위 0.5
                    Point3d H = foot1 + dirFoot1ToX * OFFSET_V;       // foot1→X 방향 0.5
                    Point3d G = new Point3d(H.X, H.Y + OFFSET_V, 0);  // H 위 0.5

                    // ── 7. Polyline 생성: S → E → F → G → H ───────────────
                    var pline = new Polyline();
                    pline.AddVertexAt(0, new Point2d(S.X, S.Y), 0, 0, 0);
                    pline.AddVertexAt(1, new Point2d(E.X, E.Y), 0, 0, 0);
                    pline.AddVertexAt(2, new Point2d(F.X, F.Y), 0, 0, 0);
                    pline.AddVertexAt(3, new Point2d(G.X, G.Y), 0, 0, 0);
                    pline.AddVertexAt(4, new Point2d(H.X, H.Y), 0, 0, 0);
                    pline.Closed = false;

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    btr.AppendEntity(pline);
                    tr.AddNewlyCreatedDBObject(pline, true);

                    // ── 8. 결과 출력 ───────────────────────────────────────
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n  DiaTreeVer 폴리라인 생성 완료");
                    ed.WriteMessage($"\n  라인 수  : {lines.Count}개");
                    ed.WriteMessage($"\n  X (기준) : ({X.X:F3}, {X.Y:F3})");
                    ed.WriteMessage($"\n  foot1(1) : ({foot1.X:F3}, {foot1.Y:F3})  dist={X.DistanceTo(foot1):F3}");
                    ed.WriteMessage($"\n  foot2(2) : ({foot2.X:F3}, {foot2.Y:F3})  dist={X.DistanceTo(foot2):F3}");
                    ed.WriteMessage($"\n  ----------------------------------------");
                    ed.WriteMessage($"\n  S : ({S.X:F3}, {S.Y:F3})");
                    ed.WriteMessage($"\n  E : ({E.X:F3}, {E.Y:F3})");
                    ed.WriteMessage($"\n  F : ({F.X:F3}, {F.Y:F3})");
                    ed.WriteMessage($"\n  G : ({G.X:F3}, {G.Y:F3})");
                    ed.WriteMessage($"\n  H : ({H.X:F3}, {H.Y:F3})");
                    ed.WriteMessage("\n========================================");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
