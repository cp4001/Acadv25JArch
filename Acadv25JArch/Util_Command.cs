using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2   // ※ Util_Command 프로젝트 네임스페이스에 맞게 조정
{
    /// <summary>
    /// Util 커맨드 모음 — CSS(Duct/Pipe 체인 선택), ss2(Base 교차 분할 + Target 스냅)
    /// 신규 시스템 — 외부 유틸 의존 없음 (전 기능 자체 구현)
    /// 사양 문서: Util_Command\CSS.md (v1.1), Util_Command\SS1.md (v1.2)
    /// </summary>
    public class CommandUtil
    {
        // ===== CSS 상수 =====
        private const double CHAIN_END_TOL = 1.0;      // 끝점 일치 허용오차
        private const double CHAIN_NEAR_TOL = 1.0;     // 근접(T분기) 허용 거리
        private const double CHAIN_SEARCH_BOX = 10.0;  // 접속점 주변 검색 박스 반경

        // ===== ss2 상수 =====
        private const double SEARCH_WIDTH = 300.0;     // 검색 폭 ScaleFactor (Q1: 상수)
        private const double MIN_TARGET_LENGTH = 50.0; // Target 최소 길이
        private const double END_EXCLUDE_DIST = 10.0;  // Base 끝점 분할 제외 거리
        private const double SNAP_SKIP_TOL = 0.001;    // 이미 교차점 위 → 스냅 생략 오차
        private const double ON_LINE_TOL = 1e-6;       // 구간 위 판정 오차

        // Duct/Pipe 계열 RegAppName 필터 (공용)
        private const string PIPE_REGAPPS = "Duct,Pipe,MainPipe,FirePipe";

        // ===== To_Duct 상수 =====
        private const string DUCT_REGAPP = "Duct";  // 지정할 RegAppName
        private const string DUCT_VALUE = "Duct";   // 기록할 문자열 값 (DxfCode 1000)

        // CSS — 명령 종료 후 Idle 시점에 선택할 Entity 목록
        private static ObjectId[] _pendingSelectIds;

        // =====================================================================
        // CSS — Duct/Pipe 체인 선택 (CSS.md)
        // 시작 Entity 의 양 끝점에서 연결된 Entity 를 추적하여 전체 체인 선택
        // =====================================================================
        [CommandMethod("CSS", CommandFlags.UsePickSet)]
        public static void DuctPipe_SelectChain()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 필터: LINE/ARC/LWPOLYLINE + Pipe 계열 XData (AND 결합)
                TypedValue[] tvs =
                [
                    new((int)DxfCode.Start, "LINE,ARC,LWPOLYLINE"),
                    new((int)DxfCode.ExtendedDataRegAppName, PIPE_REGAPPS)
                ];
                var sf = new SelectionFilter(tvs);
                var pso = new PromptSelectionOptions { MessageForAdding = "\n시작 Entity 선택: " };

                PromptSelectionResult psr = ed.GetSelection(pso, sf);
                if (psr.Status != PromptStatus.OK) return;

                var visited = new HashSet<ObjectId>();  // 방문(선택) 집합 — 순환 방지
                var stack = new Stack<Point3d>();       // 탐색할 접속점 (Stack 기반 반복)

                using var tr = db.TransactionManager.StartTransaction();

                // 시작 Entity 등록 + 양 끝점 push
                // Line/Arc/Polyline 모두 Curve 상속 → StartPoint/EndPoint 공통 사용
                foreach (SelectedObject sObj in psr.Value)
                {
                    if (tr.GetObject(sObj.ObjectId, OpenMode.ForRead) is not Curve startCurve)
                        continue;
                    if (!visited.Add(sObj.ObjectId)) continue;

                    stack.Push(startCurve.StartPoint);
                    stack.Push(startCurve.EndPoint);
                }

                // Stack 기반 반복 탐색 (재귀 대신 — 긴 체인 StackOverflow 방지)
                while (stack.Count > 0)
                {
                    Point3d pt = stack.Pop();

                    // 접속점 주변 후보 검색 (Crossing Window)
                    var pLow = new Point3d(pt.X - CHAIN_SEARCH_BOX, pt.Y - CHAIN_SEARCH_BOX, 0.0);
                    var pHigh = new Point3d(pt.X + CHAIN_SEARCH_BOX, pt.Y + CHAIN_SEARCH_BOX, 0.0);

                    PromptSelectionResult res = ed.SelectCrossingWindow(pLow, pHigh, sf);
                    if (res.Status != PromptStatus.OK) continue;

                    foreach (SelectedObject so in res.Value)
                    {
                        if (visited.Contains(so.ObjectId)) continue;
                        if (tr.GetObject(so.ObjectId, OpenMode.ForRead) is not Curve cand)
                            continue;

                        // 1) 끝점-끝점 일치 → 반대편 끝점으로 계속 진행
                        if (cand.StartPoint.DistanceTo(pt) <= CHAIN_END_TOL)
                        {
                            visited.Add(so.ObjectId);
                            stack.Push(cand.EndPoint);
                        }
                        else if (cand.EndPoint.DistanceTo(pt) <= CHAIN_END_TOL)
                        {
                            visited.Add(so.ObjectId);
                            stack.Push(cand.StartPoint);
                        }
                        // 2) 근접 접속(T분기): 접속점 → 후보 몸통 수직 투영 (구간 내, extend=false)
                        else
                        {
                            Point3d proj = cand.GetClosestPointTo(pt, false);
                            if (proj.DistanceTo(pt) <= CHAIN_NEAR_TOL)
                            {
                                visited.Add(so.ObjectId);
                                stack.Push(cand.StartPoint);  // T분기는 양 끝점 모두 진행
                                stack.Push(cand.EndPoint);
                            }
                        }
                    }
                }

                tr.Commit();

                // 결과 선택 표시 — 명령 종료 후 첫 Idle 시점에 실행 (명령 내 호출은 종료 시 무효화됨)
                _pendingSelectIds = visited.ToArray();
                Application.Idle += OnIdleSetSelection;
                ed.WriteMessage($"\n체인 선택 완료: {_pendingSelectIds.Length}개 Entity");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        // CSS — 명령 종료 후 Idle 시점 선택 핸들러 (1회 실행 후 자기 해제)
        private static void OnIdleSetSelection(object sender, EventArgs e)
        {
            Application.Idle -= OnIdleSetSelection;
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || _pendingSelectIds == null) return;
            doc.Editor.SetImpliedSelection(_pendingSelectIds);
            _pendingSelectIds = null;
        }

        // =====================================================================
        // ss2 — Base Line 교차 분할 + Target 끝점 스냅 (SS1.md v1.1)
        // Base 주변 폭(2×ScaleFactor) 내 접촉 Target 을 교차점 스냅 후 Base 분할
        // =====================================================================
        [CommandMethod("ss2", CommandFlags.UsePickSet)]
        public static void MultiLineSplit_v2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 검색 폭: 컴파일 타임 상수 사용 (신규 시스템 — 외부 설정 의존 없음)
                double scaleFactor = SEARCH_WIDTH;

                // Base 필터: 일반 LINE / Target 필터: LINE + Pipe 계열 XData
                TypedValue[] tvsBase = [new((int)DxfCode.Start, "LINE")];
                TypedValue[] tvsTarget =
                [
                    new((int)DxfCode.Start, "LINE"),
                    new((int)DxfCode.ExtendedDataRegAppName, PIPE_REGAPPS)
                ];
                var sfBase = new SelectionFilter(tvsBase);
                var sfTarget = new SelectionFilter(tvsTarget);

                var pso = new PromptSelectionOptions { MessageForAdding = "\nBase Line 선택: " };
                PromptSelectionResult basePsr = ed.GetSelection(pso, sfBase);
                if (basePsr.Status != PromptStatus.OK) return;

                var erasedIds = new HashSet<ObjectId>();  // 분할로 Erase 된 라인 → 이후 skip
                int totalSplit = 0;
                int totalSnap = 0;

                using var tr = db.TransactionManager.StartTransaction();

                var baseLines = basePsr.Value.GetObjectIds()
                    .Select(id => tr.GetObject(id, OpenMode.ForWrite))
                    .OfType<Line>()
                    .ToList();

                foreach (var baseLine in baseLines)
                {
                    // 이전 처리에서 Erase 된 Base 는 skip (eWasErased 방지)
                    if (erasedIds.Contains(baseLine.ObjectId)) continue;
                    if (baseLine.Length < ON_LINE_TOL) continue;

                    // Base 수직 방향 검색 폴리곤 (폭 2 × scaleFactor)
                    Vector3d perp = (baseLine.EndPoint - baseLine.StartPoint)
                        .CrossProduct(baseLine.Normal).GetNormal();
                    Vector3d offset = perp * scaleFactor;

                    var polyPts = new Point3dCollection
                    {
                        baseLine.StartPoint + offset,
                        baseLine.StartPoint - offset,
                        baseLine.EndPoint - offset,
                        baseLine.EndPoint + offset
                    };

                    PromptSelectionResult psr2 = ed.SelectCrossingPolygon(polyPts, sfTarget);
                    if (psr2.Status != PromptStatus.OK) continue;  // 후보 없음 → 다음 Base (기존 return 버그 수정)

                    // 접촉 조건 필터링 — extend=false: Base 구간 내 수직 투영만 인정 (Q3)
                    var targets = new List<Line>();
                    foreach (SelectedObject so in psr2.Value)
                    {
                        if (so.ObjectId == baseLine.ObjectId) continue;
                        if (erasedIds.Contains(so.ObjectId)) continue;
                        if (tr.GetObject(so.ObjectId, OpenMode.ForWrite) is not Line ssl) continue;
                        if (ssl.Length <= MIN_TARGET_LENGTH) continue;

                        bool contactS = baseLine.GetClosestPointTo(ssl.StartPoint, false)
                                            .DistanceTo(ssl.StartPoint) < scaleFactor;
                        bool contactE = baseLine.GetClosestPointTo(ssl.EndPoint, false)
                                            .DistanceTo(ssl.EndPoint) < scaleFactor;

                        if (contactS || contactE) targets.Add(ssl);
                    }
                    if (targets.Count == 0) continue;

                    // 교차 분할 + 스냅 (Transaction 주입 — 단일 Transaction 원칙)
                    var (splitCount, snapCount) =
                        InterPointsMLiness2(tr, baseLine, targets, erasedIds, scaleFactor);
                    totalSplit += splitCount;
                    totalSnap += snapCount;
                }

                tr.Commit();
                ed.WriteMessage($"\n완료 — 분할 생성: {totalSplit}개, 끝점 스냅: {totalSnap}개");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Base 와 Target 들의 교차점 계산 → Target 끝점 스냅 + Base 분할
        /// Transaction 은 호출측에서 주입 (중첩 Transaction 제거)
        /// 반환: (분할 생성 라인 수, 스냅 수행 수)
        /// </summary>
        private static (int splitCount, int snapCount) InterPointsMLiness2(
            Transaction tr, Line baseLine, List<Line> targetLines,
            HashSet<ObjectId> erasedIds, double snapMaxDist)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            int snapCount = 0;
            var interPoints = new List<Point3d>();

            var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

            foreach (var sl in targetLines)
            {
                // 교차점 계산 (양쪽 연장 포함)
                var pts = new Point3dCollection();
                baseLine.IntersectWith(sl, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);

                // 평행(0개)/공선(2개 이상)은 조용히 skip — 개별 메시지 노이즈 제거
                if (pts.Count != 1) continue;

                Point3d inters = pts[0];

                // 분할점 수집: Base 끝점 제외거리 초과 + 실제 Base 구간 위에 있는 경우만
                if (baseLine.StartPoint.DistanceTo(inters) > END_EXCLUDE_DIST &&
                    baseLine.EndPoint.DistanceTo(inters) > END_EXCLUDE_DIST &&
                    IsPointOnLineSegment(baseLine, inters))
                {
                    interPoints.Add(inters);
                }

                // Target 끝점 스냅 — 분할점 채택 여부와 무관하게 항상 시도 (Q4)
                // 교차점에 가까운 쪽 끝점 선택
                bool snapStart = sl.StartPoint.DistanceTo(inters) <= sl.EndPoint.DistanceTo(inters);
                Point3d nearEnd = snapStart ? sl.StartPoint : sl.EndPoint;
                double moveDist = nearEnd.DistanceTo(inters);

                // Q5: 이미 교차점 위(0.001 이내)면 생략 / Q1: 상한(scaleFactor) 초과 시 폭주 방지
                if (moveDist > SNAP_SKIP_TOL && moveDist <= snapMaxDist)
                {
                    if (!sl.IsWriteEnabled) sl.UpgradeOpen();  // 이미 ForWrite 면 중복 호출 방지

                    if (snapStart) sl.StartPoint = inters;
                    else sl.EndPoint = inters;

                    snapCount++;
                }
            }

            // 분할점 없으면 Split 생략
            if (interPoints.Count == 0) return (0, snapCount);

            // 분할점을 Base StartPoint 거리순으로 정렬 (GetSplitCurves 는 순서 필요)
            Point3d baseSPt = baseLine.StartPoint;
            var sortPoints = new Point3dCollection();
            foreach (var p in interPoints.OrderBy(p => p.DistanceTo(baseSPt)))
            {
                sortPoints.Add(p);
            }

            DBObjectCollection mlines = baseLine.GetSplitCurves(sortPoints);
            int splitCount = 0;

            if (mlines.Count > 1)  // 분할 성공 → 새 라인 등록 + 원본 삭제
            {
                foreach (DBObject splCurve in mlines)
                {
                    if (splCurve is not Line scLine)
                    {
                        splCurve.Dispose();  // 미등록 객체 해제
                        continue;
                    }

                    // Q3: 방향 정규화 없이 분할 결과 그대로 사용
                    btr.AppendEntity(scLine);
                    tr.AddNewlyCreatedDBObject(scLine, true);

                    // 속성 + XData 복사 — DB 등록 후 수행 (XData RegApp 해석 안정)
                    CopyLineProperties(baseLine, scLine);

                    splitCount++;
                }

                // 원본 Base Erase + erasedIds 등록 (이후 루프에서 skip — Q2)
                if (!baseLine.IsWriteEnabled) baseLine.UpgradeOpen();
                erasedIds.Add(baseLine.ObjectId);
                baseLine.Erase();
            }
            else
            {
                // 미분할 시 GetSplitCurves 결과(미등록 DBObject) 해제
                foreach (DBObject obj in mlines) obj.Dispose();
            }

            return (splitCount, snapCount);
        }

        /// <summary>
        /// 원본 Line 의 기본 속성 + XData 전체를 새 Line 에 복사 (Q2)
        /// 대상 Entity 를 직접 인수로 사용. dst 는 DB 등록 완료 상태여야 함
        /// </summary>
        private static void CopyLineProperties(Line src, Line dst)
        {
            // 기본 속성 복사
            dst.Layer = src.Layer;
            dst.Color = src.Color;
            dst.Linetype = src.Linetype;
            dst.LinetypeScale = src.LinetypeScale;
            dst.LineWeight = src.LineWeight;

            // XData 전체 복사 — Pipe 계열 RegApp 데이터 유지
            // src 와 같은 Database 이므로 RegApp 은 이미 등록되어 있음
            using ResultBuffer xd = src.XData;
            if (xd != null)
            {
                dst.XData = xd;
            }
        }

        /// <summary>
        /// 점이 Line 의 실제 구간 위에 있는지 판정 (수직 투영, extend=false)
        /// </summary>
        private static bool IsPointOnLineSegment(Line line, Point3d pt)
        {
            Point3d proj = line.GetClosestPointTo(pt, false);
            return proj.DistanceTo(pt) < ON_LINE_TOL;
        }

        // =====================================================================
        // To_Duct — 선택 Entity(Line/Arc/Polyline)에 XData RegApp "Duct" 지정
        // 값: 문자열 "Duct" (DxfCode 1000)
        // =====================================================================
        [CommandMethod("To_Duct", CommandFlags.UsePickSet)]
        public static void SetXdataToDuct()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 필터: LINE/ARC/LWPOLYLINE (지정 전이므로 XData 조건 없음)
                TypedValue[] tvs = [new((int)DxfCode.Start, "LINE,ARC,LWPOLYLINE")];
                var sf = new SelectionFilter(tvs);
                var pso = new PromptSelectionOptions { MessageForAdding = "\nDuct 지정할 Entity 선택: " };

                PromptSelectionResult psr = ed.GetSelection(pso, sf);
                if (psr.Status != PromptStatus.OK) return;

                int count = 0;

                using var tr = db.TransactionManager.StartTransaction();

                // RegApp "Duct" 등록 확인 — 미등록 상태로 XData 를 쓰면 예외 발생
                EnsureRegApp(tr, db, DUCT_REGAPP);

                foreach (SelectedObject so in psr.Value)
                {
                    if (tr.GetObject(so.ObjectId, OpenMode.ForWrite) is not Entity ent) continue;

                    // XData 지정: 1001(RegAppName) + 1000(문자열 값)
                    // 해당 RegApp 항목만 교체되며 다른 RegApp 의 XData 는 보존됨
                    using var rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, DUCT_REGAPP),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, DUCT_VALUE));

                    ent.XData = rb;
                    count++;
                }

                tr.Commit();
                ed.WriteMessage($"\nXData 지정 완료: {count}개 Entity → RegApp \"{DUCT_REGAPP}\", 값 \"{DUCT_VALUE}\"");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// RegAppTable 에 해당 RegApp 이 없으면 등록
        /// </summary>
        private static void EnsureRegApp(Transaction tr, Database db, string appName)
        {
            var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (rat.Has(appName)) return;

            rat.UpgradeOpen();
            var ratr = new RegAppTableRecord { Name = appName };
            rat.Add(ratr);
            tr.AddNewlyCreatedDBObject(ratr, true);
        }
    }
}
