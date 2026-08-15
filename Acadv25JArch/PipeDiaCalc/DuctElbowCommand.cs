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
    /// Green측 Line 4개 + 외곽 Arc 2개(내측 R−H, 외측 R+H)로 생성
    /// (Red측 직선 외곽 없음, Green 시작단 폭 방향 마감선 없음).
    /// 엘보 중심선 Arc(반경 R, Green 속성·XData 승계)를 추가하고,
    /// 기준선은 Green X측 끝점 → Tg, Red X측 끝점 → Tr 로 단축한다.
    /// 엘보 중심선 반경 R = 1.5 × W_aa (Green 폭 기준).
    /// 설계 기준: 건축\Duct-OutLine\Duct_Elbow.md (v1.3).
    /// 모든 계산은 월드축이 아니라 선택 선의 방향벡터(dirAA/dirBB) 기준으로 수행.
    ///
    /// Duct_Etv — Duct_E 로 만든 원호 엘보를 각진(마이터) 엘보로 되돌리는 역변환 명령.
    /// </summary>
    public class DuctElbowCommand
    {
        private const double JunctionTol = 1e-3;   // X 접합점 일치 거리
        private const double PerpTol = 0.02;       // 직각 판정 (약 ±1.15°)
        private const double WidthTol = 1e-6;      // 폭 동일 판정
        private const double TangentTol = 0.9;     // Arc 접선 판정 (|Line 방향 · 접선|)
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

                    if (!TryApply(tr, db, aa, bb, out string message))
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
        /// Duct_Etv — 원호 엘보 → 각진(마이터) 엘보 역변환.
        /// Arc 와 Line 을 한 번에 선택하면, 선택된 각 Arc 의 양 끝점에 접선으로 연결된 Line 2개를
        /// 서로의 교차점까지 연장/단축해 뾰족한 코너로 만들고 Arc 를 삭제한다.
        /// 동심 Arc 가 2개 이상 처리되면(내측/외측/중심선) 가장 안쪽 코너점과 가장 바깥 코너점을 잇는
        /// 마이터 대각선(Yellow, "Duct_OutLine" 레이어) 1개를 그룹마다 추가 생성하고,
        /// 폭 방향 이음선(Gin→Gout / Rin→Rout)을 덕트 축 방향으로 이동시켜
        /// **외측 코너로부터 대각선 길이(L = W√2)만큼 떨어진 위치**에 맞춘다.
        /// </summary>
        [CommandMethod("Duct_Etv", CommandFlags.UsePickSet)]
        public void Cmd_DuctEtv()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Editor selection 은 Transaction 밖에서 호출 (프로젝트 컨벤션)
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<or"),
                new TypedValue((int)DxfCode.Start, "ARC"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Operator, "or>")
            });
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nArc 와 그 끝단에 연결된 Line 을 선택하세요";
            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if (psr.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var arcs = new List<Arc>();
                    var lines = new List<Line>();
                    foreach (ObjectId id in psr.Value.GetObjectIds())
                    {
                        var ent = tr.GetObject(id, OpenMode.ForRead);
                        if (ent is Arc a) arcs.Add(a);
                        else if (ent is Line ln) lines.Add(ln);
                    }

                    if (arcs.Count == 0) { ed.WriteMessage("\n[E01] 선택 안에 Arc 가 없습니다."); return; }
                    if (lines.Count < 2) { ed.WriteMessage("\n[E02] 선택 안에 Line 이 2개 이상 있어야 합니다."); return; }

                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    tr.CreateLayer(OutlineLayer, Yellow, LineWeight.ByLayer);

                    // 처리된 Arc 별 (원호 중심, 반경, 새 코너점) — 마이터 대각선 산출용
                    var corners = new List<(Point3d Center, double Radius, Point3d Corner)>();
                    // Arc 끝점에 반경 방향으로 붙은 폭 방향 이음선 — (원호 중심, 이음선 Id, 이음선 위 한 점, 덕트 축 방향)
                    var seams = new List<(Point3d Center, ObjectId Id, Point3d Pt, Vector3d Axis)>();
                    // 이음선 위치에 겹쳐 있던 중복 Line — 이동 대상 1개만 남기고 삭제
                    var dupes = new List<ObjectId>();
                    int skipped = 0;

                    foreach (Arc arc in arcs)
                    {
                        if (!TryUnfillet(tr, arc, lines, seams, dupes, out Point3d corner, out string err))
                        {
                            ed.WriteMessage($"\n{err} (Arc 핸들 {arc.Handle})");
                            skipped++;
                            continue;
                        }
                        corners.Add((arc.Center, arc.Radius, corner));
                        arc.UpgradeOpen();
                        arc.Erase();
                    }

                    // 동심(중심 일치) Arc 그룹마다 내측 코너 → 외측 코너 마이터 대각선 1개
                    int miter = 0, seamMoved = 0;
                    var used = new bool[corners.Count];
                    var movedSeams = new List<ObjectId>();
                    for (int i = 0; i < corners.Count; i++)
                    {
                        if (used[i]) continue;
                        used[i] = true;
                        int innerIdx = i, outerIdx = i, members = 1;
                        for (int j = i + 1; j < corners.Count; j++)
                        {
                            if (used[j] || corners[j].Center.DistanceTo(corners[i].Center) > JunctionTol) continue;
                            used[j] = true;
                            members++;
                            if (corners[j].Radius < corners[innerIdx].Radius) innerIdx = j;
                            if (corners[j].Radius > corners[outerIdx].Radius) outerIdx = j;
                        }
                        if (members < 2) continue;

                        Point3d innerPt = corners[innerIdx].Corner;
                        Point3d outerPt = corners[outerIdx].Corner;
                        AddOutlineLine(tr, btr, db, innerPt, outerPt);
                        miter++;

                        // 이음선 재배치 — 외측 코너에서 덕트 축 방향으로 대각선 길이 L 만큼 떨어진 위치
                        double L = innerPt.DistanceTo(outerPt);
                        foreach (var s in seams)
                        {
                            if (s.Center.DistanceTo(corners[i].Center) > JunctionTol) continue;
                            if (movedSeams.Contains(s.Id)) continue;
                            movedSeams.Add(s.Id);

                            double cur = (s.Pt - outerPt).DotProduct(s.Axis);
                            Vector3d delta = s.Axis * (L - cur);
                            if (delta.Length < JunctionTol) continue;
                            var seamLn = (Line)tr.GetObject(s.Id, OpenMode.ForWrite);
                            seamLn.TransformBy(Matrix3d.Displacement(delta));
                            seamMoved++;
                        }
                    }

                    // 이음선 자리에 겹쳐 있던 중복 Line 삭제 (이동 대상으로 채택된 것은 제외)
                    int dupErased = 0;
                    foreach (ObjectId id in dupes)
                    {
                        bool kept = false;
                        foreach (var s in seams) { if (s.Id == id) { kept = true; break; } }
                        if (kept) continue;

                        var dup = (Line)tr.GetObject(id, OpenMode.ForWrite);
                        if (dup.IsErased) continue;
                        dup.Erase();
                        dupErased++;
                    }

                    ed.WriteMessage($"\nDuct_Etv 완료: Arc {corners.Count}개 삭제 + Line {corners.Count * 2}개 교차점 연결, " +
                                    $"마이터 대각선 {miter}개 생성, 이음선 {seamMoved}개 재배치, 중복 이음선 {dupErased}개 삭제" +
                                    (skipped > 0 ? $" (건너뜀 {skipped}개)" : "") + ".");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Arc 양 끝점에 접선으로 연결된 Line 2개를 pool 에서 찾아 교차점까지 연장/단축하고
        /// 그 교차점(corner)을 돌려준다. 같은 끝점에 반경 방향으로 붙은 폭 방향 이음선은 seams 에 수집한다.
        /// Arc 자체는 삭제하지 않는다(호출측 담당).
        /// </summary>
        private bool TryUnfillet(Transaction tr, Arc arc, List<Line> pool,
            List<(Point3d Center, ObjectId Id, Point3d Pt, Vector3d Axis)> seams, List<ObjectId> dupes,
            out Point3d corner, out string err)
        {
            corner = Point3d.Origin;
            err = "";

            if (!TryPickTangentLine(arc, arc.StartPoint, pool, null, out Line l1))
            {
                err = "[E03] Arc 시작점에 접선으로 연결된 Line 을 찾지 못했습니다.";
                return false;
            }
            if (!TryPickTangentLine(arc, arc.EndPoint, pool, l1, out Line l2))
            {
                err = "[E04] Arc 끝점에 접선으로 연결된 Line 을 찾지 못했습니다.";
                return false;
            }

            using (var pts = new Point3dCollection())
            {
                l1.IntersectWith(l2, Intersect.ExtendBoth, pts, System.IntPtr.Zero, System.IntPtr.Zero);
                if (pts.Count == 0)
                {
                    err = "[E05] 연결된 두 Line 이 평행/엇갈려 교차점이 없습니다.";
                    return false;
                }
                corner = pts[0];
            }

            if (!double.IsFinite(corner.X) || !double.IsFinite(corner.Y) || !double.IsFinite(corner.Z))
            {
                err = "[E06] 교차점 계산 결과에 NaN/Infinity 가 포함됩니다.";
                return false;
            }

            // 폭 방향 이음선 수집 (Line 이동 전후 무관 — 이음선 자체는 건드리지 않음)
            CollectSeam(arc, arc.StartPoint, pool, l1, corner, seams, dupes);
            CollectSeam(arc, arc.EndPoint, pool, l2, corner, seams, dupes);

            // 각 Line 의 Arc 접점 쪽 끝점만 교차점으로 이동
            MoveEndTo(tr, l1, arc.StartPoint, corner);
            MoveEndTo(tr, l2, arc.EndPoint, corner);
            return true;
        }

        /// <summary>
        /// conn(Arc 끝점)에 끝점이 닿아 있는 Line 중 반경 방향과 가장 나란한 것(= 폭 방향 이음선)을 수집.
        /// 덕트 축 방향(Axis)은 corner → conn 방향, 즉 코너에서 멀어지는 쪽.
        /// 같은 접점에 **겹쳐 있는 중복 이음선**(평행 = 동일 직선)은 dupes 에 담아 호출측이 삭제한다.
        /// </summary>
        private void CollectSeam(Arc arc, Point3d conn, List<Line> pool, Line exclude, Point3d corner,
            List<(Point3d Center, ObjectId Id, Point3d Pt, Vector3d Axis)> seams, List<ObjectId> dupes)
        {
            Vector3d radial = (conn - arc.Center).GetNormal();
            Line best = null;
            double bestScore = TangentTol;
            var found = new List<Line>();

            foreach (Line ln in pool)
            {
                if (ln.ObjectId == exclude.ObjectId) continue;

                bool atStart = ln.StartPoint.DistanceTo(conn) <= JunctionTol;
                bool atEnd = ln.EndPoint.DistanceTo(conn) <= JunctionTol;
                if (!atStart && !atEnd) continue;

                Vector3d dir = (atStart ? ln.EndPoint : ln.StartPoint) - conn;
                if (dir.Length < JunctionTol) continue;

                double score = System.Math.Abs(dir.GetNormal().DotProduct(radial));
                if (score <= TangentTol) continue;
                found.Add(ln);
                if (score > bestScore) { bestScore = score; best = ln; }
            }
            if (best == null) return;

            Vector3d axis = conn - corner;
            if (axis.Length < JunctionTol) return;
            seams.Add((arc.Center, best.ObjectId, conn, axis.GetNormal()));

            // conn 을 공유하면서 유지 대상과 평행 ⇒ 동일 직선 위에 겹친 중복선
            Vector3d keepDir = (best.EndPoint - best.StartPoint).GetNormal();
            foreach (Line ln in found)
            {
                if (ln.ObjectId == best.ObjectId) continue;
                Vector3d d = (ln.EndPoint - ln.StartPoint).GetNormal();
                if (System.Math.Abs(d.DotProduct(keepDir)) < 0.9999) continue;
                if (!dupes.Contains(ln.ObjectId)) dupes.Add(ln.ObjectId);
            }
        }

        /// <summary>
        /// conn(Arc 끝점)에 끝점이 닿아 있는 Line 중 Arc 접선 방향과 가장 나란한 것을 고른다.
        /// 폭 방향 이음선(Gin→Gout 등)은 반경 방향이라 TangentTol 로 걸러진다.
        /// </summary>
        private bool TryPickTangentLine(Arc arc, Point3d conn, List<Line> pool, Line exclude, out Line best)
        {
            best = null;
            Vector3d radial = (conn - arc.Center).GetNormal();
            Vector3d tangent = arc.Normal.CrossProduct(radial).GetNormal();
            double bestScore = TangentTol;

            foreach (Line ln in pool)
            {
                if (exclude != null && ln.ObjectId == exclude.ObjectId) continue;

                bool atStart = ln.StartPoint.DistanceTo(conn) <= JunctionTol;
                bool atEnd = ln.EndPoint.DistanceTo(conn) <= JunctionTol;
                if (!atStart && !atEnd) continue;

                Vector3d dir = (atStart ? ln.EndPoint : ln.StartPoint) - conn;
                if (dir.Length < JunctionTol) continue;

                double score = System.Math.Abs(dir.GetNormal().DotProduct(tangent));
                if (score > bestScore) { bestScore = score; best = ln; }
            }
            return best != null;
        }

        /// <summary>ForRead 로 열린 Line 을 ForWrite 로 승격해 refPt 쪽 끝점만 newPt 로 이동.</summary>
        private void MoveEndTo(Transaction tr, Line ln, Point3d refPt, Point3d newPt)
        {
            var w = (Line)tr.GetObject(ln.ObjectId, OpenMode.ForWrite);
            if (w.StartPoint.DistanceTo(refPt) <= w.EndPoint.DistanceTo(refPt))
                w.StartPoint = newPt;
            else
                w.EndPoint = newPt;
        }

        /// <summary>
        /// aa(Green, 수평) + bb(Red, 수직 직각) 로부터 코너 엘보 외곽선(Line 4 + Arc 2 + 중심선 Arc 1)을
        /// 생성하고 기준선을 단축(Green→Tg, Red→Tr)한다. **폭이 다르면(W_aa≠W_bb) 형상을 확장하지 않고
        /// [E04]로 실패 반환** — DuctTreeOutLine.md §12 확정 사항: 트리 자동화에서도 그대로 스킵 대상.
        /// 대화형 선택 없이 Line 객체를 직접 받아 검증~계산~생성까지 처리 — DuctTreeOutlineCommand 등에서 재사용.
        /// 검증 실패 시 false 반환(엔티티 생성 없음, Transaction 은 변경 없음), 성공 시 true + 결과 메시지.
        /// </summary>
        public bool TryApply(Transaction tr, Database db, Line aa, Line bb, out string message)
        {
            message = "";

            if (aa.ObjectId == bb.ObjectId)
            {
                message = "[E01] 두 Line 은 서로 달라야 합니다.";
                return false;
            }

            // [3장] XData "a" 폭 읽기
            if (!TryReadWidth(aa, out double Waa)) { message = "[E02] aa XData \"a\"(폭) 를 읽을 수 없습니다."; return false; }
            if (!TryReadWidth(bb, out double Wbb)) { message = "[E02] bb XData \"a\"(폭) 를 읽을 수 없습니다."; return false; }
            if (Waa <= 0 || Wbb <= 0)
            {
                message = "[E03] 폭은 0 보다 커야 합니다.";
                return false;
            }
            // [9장 v1.1] 폭 불일치 시 오류 종료 (DuctTreeOutLine.md §12: 트리 자동화에서도 확장 없이 스킵)
            if (System.Math.Abs(Waa - Wbb) > WidthTol)
            {
                message = $"[E04] 두 덕트 폭이 다릅니다 (Waa={Waa}, Wbb={Wbb}). 동일 폭만 지원합니다.";
                return false;
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
                message = "[E05] aa/bb 의 한쪽 끝점이 코너 교차점 X 와 일치하지 않습니다.";
                return false;
            }

            Vector3d dirAA = (aaFar - X).GetNormal();   // Green 진행 방향
            Vector3d dirBB = (bbFar - X).GetNormal();   // Red 진행 방향

            // [2장] aa ⊥ bb 검증
            if (System.Math.Abs(dirAA.DotProduct(dirBB)) > PerpTol)
            {
                message = "[E06] aa 와 bb 가 직각이 아닙니다.";
                return false;
            }

            // Arc 각도 계산은 WCS XY 평면 전제 — 평면 검증
            Vector3d cross = dirAA.CrossProduct(dirBB);
            if (System.Math.Abs(cross.Z) < 0.999)
            {
                message = "[E08] 두 선이 WCS XY 평면 위에 있지 않습니다.";
                return false;
            }

            // [2/6장] 파생 치수 — 중심선 반경 R = 1.5 × W_aa (v1.1)
            double H = Waa / 2.0;
            double R = 1.5 * Waa;
            double Rin = R - H;     // 내측 원호 반경
            double Rout = R + H;    // 외측 원호 반경

            // [10장] 길이 충분성 검증 — 접점(Tg/Tr)까지 도달해야 함
            if (aaFar.DistanceTo(X) < R + JunctionTol)
            {
                message = "[E07] aa 길이가 부족합니다 (엘보 반경 R 보다 길어야 합니다).";
                return false;
            }
            if (bbFar.DistanceTo(X) < R + JunctionTol)
            {
                message = "[E07] bb 길이가 부족합니다 (엘보 반경 R 보다 길어야 합니다).";
                return false;
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

            foreach (var p in new[] { C, Tg, Tr, Gin, Gout, Rin_pt, Rout_pt, GfarIn, GfarOut })
            {
                if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                {
                    message = "[E09] 계산 결과에 NaN/Infinity 가 포함되어 중단합니다.";
                    return false;
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

            // [7장 v1.3] 외곽선 — Green측 Line 4개 + 외곽 Arc 2개 (Red측 직선 외곽·Green 끝단 마감 없음)
            int created = 0;
            created += AddOutlineLine(tr, btr, db, GfarIn, Gin);        // 1. Green 내측 외곽
            created += AddOutlineLine(tr, btr, db, GfarOut, Gout);      // 2. Green 외측 외곽
            created += AddOutlineLine(tr, btr, db, Gin, Gout);          // 3. Green측 이음선 (Tg)
            created += AddOutlineLine(tr, btr, db, Rin_pt, Rout_pt);    // 4. Red측 이음선 (Tr, 유지)
            created += AddOutlineArc(tr, btr, db, C, Rin, startAng, endAng);  // 5. 엘보 내측 원호
            created += AddOutlineArc(tr, btr, db, C, Rout, startAng, endAng); // 6. 엘보 외측 원호

            // [v1.2] 엘보 중심선 Arc — 반경 R, Green(aa) 속성·XData 승계
            AddCenterArc(tr, btr, db, C, R, startAng, endAng, aa);

            // [v1.2] 기준선 단축 — Green X측 끝점 → Tg, Red X측 끝점 → Tr
            MoveEnd(aa, X, Tg);
            MoveEnd(bb, X, Tr);

            message = $"Duct_E 완료: 외곽 Line 4개 + 외곽 Arc 2개 + 중심선 Arc 1개 생성, " +
                      $"기준선 단축(Green→Tg, Red→Tr) (W={Waa}, R(중심선)={R}, 내측={Rin}, 외측={Rout}).";
            return true;
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

        /// <summary>Line 의 기준점 refPt 와 가까운 끝점만 newPt 로 이동.</summary>
        private void MoveEnd(Line ln, Point3d refPt, Point3d newPt)
        {
            ln.UpgradeOpen();
            if (ln.StartPoint.DistanceTo(refPt) <= ln.EndPoint.DistanceTo(refPt))
                ln.StartPoint = newPt;
            else
                ln.EndPoint = newPt;
        }

        /// <summary>중심 c, 반경 r 의 중심선 Arc 생성 후 source(Green) 의 색상/레이어/선종류/선가중치/XData 승계.</summary>
        private void AddCenterArc(Transaction tr, BlockTableRecord btr, Database db, Point3d c, double r, double startAng, double endAng, Line source)
        {
            var arc = new Arc(c, r, startAng, endAng);
            arc.SetDatabaseDefaults(db);
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            arc.Layer = source.Layer;
            arc.Color = source.Color;
            arc.Linetype = source.Linetype;
            arc.LineWeight = source.LineWeight;
            using (ResultBuffer rb = source.GetXDataForApplication("a"))
            {
                if (rb != null) arc.XData = rb;   // RegApp "a" 는 source 가 이미 사용 중이므로 등록되어 있음
            }
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
