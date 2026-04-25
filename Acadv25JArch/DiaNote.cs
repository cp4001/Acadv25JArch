using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class DiaNote
    {
        private const double OFFSET    = 0.5; // E 확장 거리 / G 오프셋
        private const double S_LENGTH  = 3.5; // foot1 → S 거리

        private static double BaseLen = 30.0; // 관경 텍스트 기준 길이 (실제 상황에 맞게 조정)

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
        [CommandMethod("cmd_DiaNoteVer", CommandFlags.UsePickSet)]
        public void Cmd_DiaNoteVer()
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

                    // ── 9. 관경 텍스트 생성 (D1.D2.D3 → S 끝지점 배치) ───
                    const double TEXT_HEIGHT = 2.5;

                    // Line의 중점 X 값 오름차순 정렬 후 Dia 읽기
                    var diaList = sorted
                        .OrderBy(x => (x.Line.StartPoint.X + x.Line.EndPoint.X) / 2.0)
                        .Select(x => JXdata.GetXdata(x.Line, "Dia"))
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList();

                    if (diaList.Count > 0)
                    {
                        string diaStr = string.Join(".", diaList); // "D1.D2.D3"

                        // X가 수직라인 오른쪽: TextLeft  → 글자 시작이 S+dir*1
                        // X가 수직라인 왼쪽  : TextRight → 글자 끝이  S+dir*1
                        Point3d txtPt = S + dirFoot1ToX * 1.0;
                        bool isRight = dirFoot1ToX.X >= 0; // X방향 오른쪽 여부

                        var txt = new DBText();
                        txt.TextString   = diaStr;
                        txt.Height       = TEXT_HEIGHT;
                        txt.VerticalMode = TextVerticalMode.TextVerticalMid;
                        if (isRight)
                        {
                            txt.HorizontalMode = TextHorizontalMode.TextLeft;
                            txt.AlignmentPoint = txtPt;
                            txt.Position       = txtPt;
                        }
                        else
                        {
                            txt.HorizontalMode = TextHorizontalMode.TextRight;
                            txt.AlignmentPoint = txtPt;
                            txt.Position       = txtPt;
                        }

                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        ed.WriteMessage($"\n  관경 텍스트 : \"{diaStr}\" → ({S.X:F3}, {S.Y:F3})");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }


        /// <summary>
        /// DiaNoteVer1: cmd_DiaNoteVer 응용 — 첫째/둘째 라인 간 최단거리(base) 기반 동적 치수
        ///
        ///  base        = sorted[0].Foot ↔ sorted[1].Foot 최단거리
        ///  OFFSET_V    = base × 0.2
        ///  S_LEN       = base
        ///  TEXT_HEIGHT = base × 0.8
        ///  TEXT_OFFSET = base × 0.3
        /// </summary>
        [CommandMethod("cmd_DiaNoteVer1", CommandFlags.UsePickSet)]
        public void Cmd_DiaNoteVer1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

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

                    Point3d foot1 = sorted[0].Foot;       // 가장 가까운 수직라인 수선의 발
                    Point3d foot2 = sorted.Last().Foot;   // 가장 먼  수직라인 수선의 발

                    // ── 5. base 계산: 첫째/둘째 라인 foot 간 최단거리 ─────
                    double baseDist    = foot1.DistanceTo(sorted[1].Foot);
                    double OFFSET_V    = baseDist * 0.2;
                    double S_LEN       = baseDist;
                    double TEXT_HEIGHT = baseDist * 0.8;
                    double TEXT_OFFSET = baseDist * 0.3;

                    ed.WriteMessage($"\n  base(1-2 간격)={baseDist:F3}  OFFSET_V={OFFSET_V:F3}  S_LEN={S_LEN:F3}  TEXT_HEIGHT={TEXT_HEIGHT:F3}  TEXT_OFFSET={TEXT_OFFSET:F3}");

                    // ── 6. 방향 벡터 ───────────────────────────────────────
                    Vector3d dirFoot1ToX  = (X     - foot1).GetNormal(); // foot1 → X
                    Vector3d dirXToFoot2  = (foot2 - X    ).GetNormal(); // X → foot2

                    // ── 7. 각 점 계산 ──────────────────────────────────────
                    Point3d S = foot1 + dirFoot1ToX * S_LEN;          // foot1→X 방향 S_LEN
                    Point3d E = foot2 + dirXToFoot2 * OFFSET_V;       // foot2→반대 OFFSET_V
                    Point3d F = new Point3d(E.X, E.Y + OFFSET_V, 0);  // E 위 OFFSET_V
                    Point3d H = foot1 + dirFoot1ToX * OFFSET_V;       // foot1→X 방향 OFFSET_V
                    Point3d G = new Point3d(H.X, H.Y + OFFSET_V, 0);  // H 위 OFFSET_V

                    // ── 8. Polyline 생성: S → E → F → G → H ───────────────
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

                    // ── 9. 결과 출력 ───────────────────────────────────────
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n  DiaNoteVer1 폴리라인 생성 완료");
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

                    // ── 10. 관경 텍스트 생성 (D1.D2.D3 → S 끝지점 배치) ───
                    // Line의 중점 X 값 오름차순 정렬 후 Dia 읽기
                    var diaList = sorted
                        .OrderBy(x => (x.Line.StartPoint.X + x.Line.EndPoint.X) / 2.0)
                        .Select(x => JXdata.GetXdata(x.Line, "Dia"))
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList();

                    if (diaList.Count > 0)
                    {
                        string diaStr = string.Join(".", diaList); // "D1.D2.D3"

                        // X가 수직라인 오른쪽: TextLeft  → 글자 시작이 S+dir*TEXT_OFFSET
                        // X가 수직라인 왼쪽  : TextRight → 글자 끝이  S+dir*TEXT_OFFSET
                        Point3d txtPt = S + dirFoot1ToX * TEXT_OFFSET;
                        bool isRight = dirFoot1ToX.X >= 0; // X방향 오른쪽 여부

                        var txt = new DBText();
                        txt.TextString   = diaStr;
                        txt.Height       = TEXT_HEIGHT;
                        txt.VerticalMode = TextVerticalMode.TextVerticalMid;
                        if (isRight)
                        {
                            txt.HorizontalMode = TextHorizontalMode.TextLeft;
                            txt.AlignmentPoint = txtPt;
                            txt.Position       = txtPt;
                        }
                        else
                        {
                            txt.HorizontalMode = TextHorizontalMode.TextRight;
                            txt.AlignmentPoint = txtPt;
                            txt.Position       = txtPt;
                        }

                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        ed.WriteMessage($"\n  관경 텍스트 : \"{diaStr}\" → ({S.X:F3}, {S.Y:F3})");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }


        /// <summary>
        /// 선택  line  "DD"Xdata를 설정하는 커맨드
        /// </summary>
        [CommandMethod("DD")] // pipe Load
        public void Cmd_Line_DD()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    double valDD = 1.0;
                    // 1. Leaf Line 선택
                    List<Line> targets = JEntity.GetEntityByTpye<Line>("Pipe Line 를 선택 하세요?", JSelFilter.MakeFilterTypes("LINE"));
                    if (targets.Count() == 0) return;
                    //2 15A Load  값 입력 
                    PromptDoubleOptions opts = new PromptDoubleOptions("\n Dia 값을 입력하세요 ? ");
                    opts.DefaultValue = 1;
                    opts.UseDefaultValue = true;
                    opts.AllowNegative = false;  // 음수 허용 여부
                    opts.AllowZero = false;      // 0 허용 여부

                    PromptDoubleResult result = ed.GetDouble(opts);

                    if (result.Status == PromptStatus.OK)
                    {
                        valDD = result.Value;
                    }

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Dia"); //LL(Line)

                    ////Create layerfor Wall Center Line
                    //tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var ll in targets)
                    {
                        // 2. 폴리라인 객체 가져오기
                        //Polyline poly = tr.GetObject(polyResult.ObjectId, OpenMode.ForRead) as Polyline;
                        if (ll == null)
                        {
                            ed.WriteMessage("\n라인을 읽을 수 없습니다.");
                            continue;
                        }

                        //Set Xdata
                        ll.UpgradeOpen();
                        JXdata.SetXdata(ll, "Dia", valDD.ToString());


                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// NoteHor: 수평 Line 선택 + 기준점 X 클릭 → 폴리라인 S-E-F-G-H + 라인별 텍스트 생성
        ///
        ///  foot1 : X에서 가장 가까운 수평 Line의 수선의 발
        ///  foot2 : X에서 가장 먼  수평 Line의 수선의 발
        ///  perpDir : dirFoot1ToX × ZAxis (수평 돌출 방향)
        ///
        ///  S : foot1 + (foot1→X 방향) × 3.5
        ///  E : foot2 + (X→foot2 방향) × 0.5
        ///  F : E + perpDir × 0.5
        ///  H : foot1 + (foot1→X 방향) × 0.5
        ///  G : H + perpDir × 0.5
        ///
        ///  Polyline : S → E → F → G → H
        ///  텍스트   : 각 라인 foot에서 perpDir × 1.5 위치에 개별 배치 (Y값 오름차순)
        /// </summary>
        [CommandMethod("cmd_NoteHor", CommandFlags.UsePickSet)]
        public void Cmd_NoteHor()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            const double OFFSET_H    = 0.5;
            const double S_LEN       = 3.5;
            const double TEXT_HEIGHT = 2.5;
            const double TEXT_OFFSET = 1.5;

            try
            {
                // ── 1. 수평 Line 선택 ─────────────────────────────────────
                PromptSelectionResult selResult = ed.SelectImplied();
                if (selResult.Status != PromptStatus.OK)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = "\n수평 Line들을 선택하세요: "
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
                    // ── 3. 라인 수집 ───────────────────────────────────────
                    var lines = new List<Line>();
                    foreach (SelectedObject so in selResult.Value)
                    {
                        if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Line l)
                            lines.Add(l);
                    }
                    if (lines.Count < 1)
                    {
                        ed.WriteMessage("\n2개 이상의 수평 라인을 선택해야 합니다.");
                        tr.Commit(); return;
                    }

                    // ── 4. X까지 거리 정렬 ────────────────────────────────
                    var sorted = lines
                        .Select(l => new { Line = l, Foot = l.GetClosestPointTo(X, true) })
                        .OrderBy(x => X.DistanceTo(x.Foot))
                        .ToList();

                    Point3d foot1 = sorted.First().Foot;
                    Point3d foot2 = sorted.Last().Foot;

                    // ── 5. 방향 벡터 ───────────────────────────────────────
                    Vector3d dirFoot1ToX = (X     - foot1).GetNormal(); // foot1 → X
                    Vector3d dirXToFoot2 = (foot2 - X    ).GetNormal(); // X → foot2
                    // perpDir: foot1에서 가장 가까운 라인의 중점 방향 (항상 라인 안쪽으로)
                    Line nearLine = sorted.First().Line;
                    Point3d lineMid = nearLine.StartPoint +
                                      (nearLine.EndPoint - nearLine.StartPoint) * 0.5;
                    Vector3d toMid  = lineMid - foot1;
                    Vector3d perpDir = toMid.Length > 0.001
                        ? toMid.GetNormal()
                        : new Vector3d(1, 0, 0);

                    // ── 6. 점 계산 ─────────────────────────────────────────
                    Point3d S = foot1 + dirFoot1ToX * S_LEN;   // foot1→X 방향 3.5
                    Point3d E = foot2 + dirXToFoot2 * OFFSET_H; // foot2 방향 0.5
                    Point3d F = E + perpDir * OFFSET_H;          // perpDir 방향 0.5
                    Point3d H = foot1 + dirFoot1ToX * OFFSET_H; // foot1→X 방향 0.5
                    Point3d G = H + perpDir * OFFSET_H;          // perpDir 방향 0.5

                    // ── 7. Polyline: S → E → F → G → H ───────────────────
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

                    // ── 8, 9. Y 정렬 후 1.5 line + 텍스트 배치 ────────────
                    //  X 방향과 무관하게 "Y 큰 line → 위쪽 텍스트"가 되도록 정렬 방향 자동 결정.
                    //  basePt = S + dirFoot1ToX * S_LEN * i 로 진행하므로
                    //   - dirFoot1ToX.Y >= 0 (basePt 위로 진행) : Y 오름차순  (i=0=최하 → Y최소 line)
                    //   - dirFoot1ToX.Y <  0 (basePt 아래로 진행): Y 내림차순 (i=0=최상 → Y최대 line)
                    bool isRight    = perpDir.X >= 0;
                    bool baseGoesUp = dirFoot1ToX.Y >= 0;
                    var textSorted = (baseGoesUp
                        ? sorted.OrderBy         (x => (x.Line.StartPoint.Y + x.Line.EndPoint.Y) / 2.0)
                        : sorted.OrderByDescending(x => (x.Line.StartPoint.Y + x.Line.EndPoint.Y) / 2.0))
                        .ToList();

                    for (int i = 0; i < textSorted.Count; i++)
                    {
                        string dia = JXdata.GetXdata(textSorted[i].Line, "Dia");
                        if (string.IsNullOrEmpty(dia)) continue;

                        // 기준점: S에서 -dirFoot1ToX 방향으로 S_LEN * i
                        Point3d basePt = S + dirFoot1ToX * (S_LEN * i);

                        // 1.5 수평 line: basePt → basePt + perpDir * 1.5
                        Point3d lnEnd = basePt + perpDir * 1.5;
                        var ln = new Line(basePt, lnEnd);
                        btr.AppendEntity(ln);
                        tr.AddNewlyCreatedDBObject(ln, true);

                        // 텍스트: line 끝에서 perpDir 방향 0.5 추가 지점
                        Point3d txtPt = lnEnd + perpDir * 0.5;
                        var txt = new DBText();
                        txt.TextString     = dia;
                        txt.Height         = TEXT_HEIGHT;
                        txt.VerticalMode   = TextVerticalMode.TextVerticalMid;
                        txt.HorizontalMode = isRight
                            ? TextHorizontalMode.TextLeft
                            : TextHorizontalMode.TextRight;
                        txt.AlignmentPoint = txtPt;
                        txt.Position       = txtPt;
                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        ed.WriteMessage($"\n  D{i+1} \"{dia}\" base:({basePt.X:F3},{basePt.Y:F3})");
                    }

                    // ── 10. S에서 dirFoot1ToX 방향으로 3.5×2 = 7.0 연장 Line ──
                    var extLine = new Line(S, S + dirFoot1ToX * S_LEN * (textSorted.Count-1));
                    btr.AppendEntity(extLine);
                    tr.AddNewlyCreatedDBObject(extLine, true);

                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n  NoteHor 완료");
                    ed.WriteMessage($"\n  S:({S.X:F3},{S.Y:F3})  E:({E.X:F3},{E.Y:F3})");
                    ed.WriteMessage($"\n  F:({F.X:F3},{F.Y:F3})  G:({G.X:F3},{G.Y:F3})  H:({H.X:F3},{H.Y:F3})");
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
        /// NoteHGor1: cmd_NoteHor 응용 — 첫째/둘째 라인 간 최단거리(base) 기반 동적 치수
        ///
        ///  base       = sorted[0].Foot ↔ sorted[1].Foot 최단거리
        ///  OFFSET_H   = base × 0.1
        ///  S_LEN      = base × 0.8
        ///  TEXT_HEIGHT = base
        ///  TEXT_OFFSET = base × 0.6
        /// </summary>
        [CommandMethod("cmd_NoteHor1", CommandFlags.UsePickSet)]
        public void Cmd_NoteHor1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                // ── 1. 수평 Line 선택 ─────────────────────────────────────
                PromptSelectionResult selResult = ed.SelectImplied();
                if (selResult.Status != PromptStatus.OK)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = "\n수평 Line들을 선택하세요: "
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
                    // ── 3. 라인 수집 ───────────────────────────────────────
                    var lines = new List<Line>();
                    foreach (SelectedObject so in selResult.Value)
                    {
                        if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is Line l)
                            lines.Add(l);
                    }
                    if (lines.Count < 2)
                    {
                        ed.WriteMessage("\n2개 이상의 수평 라인을 선택해야 합니다.");
                        tr.Commit(); return;
                    }

                    // ── 4. X까지 거리 정렬 ────────────────────────────────
                    var sorted = lines
                        .Select(l => new { Line = l, Foot = l.GetClosestPointTo(X, true) })
                        .OrderBy(x => X.DistanceTo(x.Foot))
                        .ToList();

                    Point3d foot1 = sorted[0].Foot;  // 가장 가까운
                    Point3d foot2 = sorted.Last().Foot;  // 가장 먼

                    // ── 5. base 계산: 첫째/둘째 라인 foot 간 최단거리 ─────
                    double baseDist   = foot1.DistanceTo(sorted[1].Foot);
                    double OFFSET_H   = baseDist * 0.2;
                    double S_LEN      = baseDist ;
                    double TEXT_HEIGHT = baseDist*0.8;
                    double TEXT_OFFSET = baseDist * 0.6;

                    ed.WriteMessage($"\n  base(1-2 간격)={baseDist:F3}  OFFSET_H={OFFSET_H:F3}  S_LEN={S_LEN:F3}  TEXT_HEIGHT={TEXT_HEIGHT:F3}  TEXT_OFFSET={TEXT_OFFSET:F3}");

                    // ── 6. 방향 벡터 ───────────────────────────────────────
                    Vector3d dirFoot1ToX = (X     - foot1).GetNormal();
                    Vector3d dirXToFoot2 = (foot2 - X    ).GetNormal();
                    Line nearLine = sorted.First().Line;
                    Point3d lineMid = nearLine.StartPoint +
                                      (nearLine.EndPoint - nearLine.StartPoint) * 0.5;
                    Vector3d toMid  = lineMid - foot1;
                    Vector3d perpDir = toMid.Length > 0.001
                        ? toMid.GetNormal()
                        : new Vector3d(1, 0, 0);

                    // ── 7. 점 계산 ─────────────────────────────────────────
                    Point3d S = foot1 + dirFoot1ToX * S_LEN;
                    Point3d E = foot2 + dirXToFoot2 * OFFSET_H;
                    Point3d F = E + perpDir * OFFSET_H;
                    Point3d H = foot1 + dirFoot1ToX * OFFSET_H;
                    Point3d G = H + perpDir * OFFSET_H;

                    // ── 8. Polyline: S → E → F → G → H ───────────────────
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

                    // ── 9. Y 정렬 후 지시선 + 텍스트 배치 ──────────────────
                    //  X 방향과 무관하게 "Y 큰 line → 위쪽 텍스트"가 되도록 정렬 방향 자동 결정.
                    //  basePt = S + dirFoot1ToX * S_LEN * i 로 진행하므로
                    //   - dirFoot1ToX.Y >= 0 (basePt 위로 진행) : Y 오름차순  (i=0=최하 → Y최소 line)
                    //   - dirFoot1ToX.Y <  0 (basePt 아래로 진행): Y 내림차순 (i=0=최상 → Y최대 line)
                    bool isRight    = perpDir.X >= 0;
                    bool baseGoesUp = dirFoot1ToX.Y >= 0;
                    var textSorted = (baseGoesUp
                        ? sorted.OrderBy         (x => (x.Line.StartPoint.Y + x.Line.EndPoint.Y) / 2.0)
                        : sorted.OrderByDescending(x => (x.Line.StartPoint.Y + x.Line.EndPoint.Y) / 2.0))
                        .ToList();

                    for (int i = 0; i < textSorted.Count; i++)
                    {
                        string dia = JXdata.GetXdata(textSorted[i].Line, "Dia");
                        if (string.IsNullOrEmpty(dia)) continue;

                        Point3d basePt = S + dirFoot1ToX * (S_LEN * i);

                        // 지시선: basePt → basePt + perpDir * TEXT_OFFSET
                        Point3d lnEnd = basePt + perpDir * TEXT_OFFSET;
                        var ln = new Line(basePt, lnEnd);
                        btr.AppendEntity(ln);
                        tr.AddNewlyCreatedDBObject(ln, true);

                        // 텍스트: 지시선 끝에서 perpDir 방향 OFFSET_H 추가
                        Point3d txtPt = lnEnd + perpDir * OFFSET_H;
                        var txt = new DBText();
                        txt.TextString     = dia;
                        txt.Height         = TEXT_HEIGHT;
                        txt.VerticalMode   = TextVerticalMode.TextVerticalMid;
                        txt.HorizontalMode = isRight
                            ? TextHorizontalMode.TextLeft
                            : TextHorizontalMode.TextRight;
                        txt.AlignmentPoint = txtPt;
                        txt.Position       = txtPt;
                        btr.AppendEntity(txt);
                        tr.AddNewlyCreatedDBObject(txt, true);

                        ed.WriteMessage($"\n  D{i+1} \"{dia}\" base:({basePt.X:F3},{basePt.Y:F3})");
                    }

                    // ── 10. S에서 dirFoot1ToX 방향 연장 Line ──────────────
                    if (textSorted.Count > 1)
                    {
                        var extLine = new Line(S, S + dirFoot1ToX * S_LEN * (textSorted.Count - 1));
                        btr.AppendEntity(extLine);
                        tr.AddNewlyCreatedDBObject(extLine, true);
                    }

                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage("\n  NoteHGor1 완료");
                    ed.WriteMessage($"\n  base={baseDist:F3}");
                    ed.WriteMessage($"\n  S:({S.X:F3},{S.Y:F3})  E:({E.X:F3},{E.Y:F3})");
                    ed.WriteMessage($"\n  F:({F.X:F3},{F.Y:F3})  G:({G.X:F3},{G.Y:F3})  H:({H.X:F3},{H.Y:F3})");
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
