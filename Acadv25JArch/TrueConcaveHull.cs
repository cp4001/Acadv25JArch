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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using ProgramLicenseManager;

namespace HullAlgorithms
{
    /// <summary>
    /// Visibility Check 기반의 진정한 Concave Hull 알고리즘
    /// </summary>
    public class TrueConcaveHull
    {
        /// <summary>
        /// True Concave Hull 명령
        /// BP에서 시작하여 시계방향으로 visibility check하며 내부 경계 형성
        /// </summary>
        [CommandMethod("c_TRUECONCAVEHULL")]
        public void CreateTrueConcaveHull()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // BP(기준점) 선택
                PromptPointOptions ppo = new PromptPointOptions("\nBP(기준점)를 선택하세요: ");
                PromptPointResult ppr = ed.GetPoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n취소되었습니다.");
                    return;
                }

                Point3d basePoint = ppr.Value;

                // 최대 엣지 길이 입력 (너무 긴 엣지 방지)
                PromptDoubleOptions pdo = new PromptDoubleOptions("\n최대 엣지 길이 입력 (0=제한없음, 권장: 50-150): ");
                pdo.DefaultValue = 0;
                pdo.AllowNegative = false;
                PromptDoubleResult pdr = ed.GetDouble(pdo);

                double maxEdgeLength = pdr.Status == PromptStatus.OK ? pdr.Value : 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 도면에서 모든 Point 수집
                    List<Point3d> allPoints = GetAllPointsFromDrawing(tr, db);

                    if (allPoints.Count < 3)
                    {
                        ed.WriteMessage("\n최소 3개 이상의 점이 필요합니다.");
                        return;
                    }

                    Point3d bp2d = new Point3d(basePoint.X, basePoint.Y,0.0);

                    // True Concave Hull 계산
                    List<Point3d> hullPoints = ComputeTrueConcaveHull(allPoints, bp2d, maxEdgeLength);

                    if (hullPoints.Count < 3)
                    {
                        ed.WriteMessage("\n충분한 점을 찾을 수 없습니다.");
                        return;
                    }

                    // 노란색 폴리라인 생성
                    CreatePolylineFromPoints(hullPoints, 2, true, tr, db);

                    tr.Commit();
                    ed.WriteMessage($"\nTrue Concave Hull 생성 완료: {hullPoints.Count}개의 점");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Visibility Check 기반 True Concave Hull 알고리즘
        /// </summary>
        private List<Point3d> ComputeTrueConcaveHull(List<Point3d> allPoints, Point3d basePoint, double maxEdgeLength)
        {
            if (allPoints.Count < 3)
                return allPoints;

            // 1. BP에서 가장 가까운 점을 시작점으로 선택
            Point3d startPoint = allPoints.OrderBy(p => p.DistanceTo(basePoint)).First();

            List<Point3d> hull = new List<Point3d>();
            HashSet<Point3d> visited = new HashSet<Point3d>();

            Point3d currentPoint = startPoint;
            Point3d previousPoint = basePoint; // 초기 방향 설정을 위한 이전 점

            hull.Add(currentPoint);
            visited.Add(currentPoint);

            int maxIterations = allPoints.Count * 2; // 무한 루프 방지
            int iteration = 0;

            // 2. 시작점으로 돌아올 때까지 반복
            while (iteration < maxIterations)
            {
                // 다음 점 찾기 (시계 방향 + Visibility Check)
                Point3d nextPoint = FindNextConcavePoint(
                    currentPoint, 
                    previousPoint, 
                    allPoints, 
                    visited, 
                    basePoint,
                    maxEdgeLength
                );

                // 종료 조건: 시작점으로 돌아옴
                if (nextPoint == Point3d.Origin || 
                    (hull.Count > 2 && nextPoint.DistanceTo(startPoint) < 0.001))
                {
                    break;
                }

                // 다음 점 추가
                hull.Add(nextPoint);
                visited.Add(nextPoint);

                // 다음 반복을 위한 업데이트
                previousPoint = currentPoint;
                currentPoint = nextPoint;

                iteration++;
            }

            return hull;
        }

        /// <summary>
        /// 시계 방향 + Visibility Check로 다음 Concave 점 찾기
        /// </summary>
        private Point3d FindNextConcavePoint(
            Point3d currentPoint, 
            Point3d previousPoint, 
            List<Point3d> allPoints,
            HashSet<Point3d> visited,
            Point3d basePoint,
            double maxEdgeLength)
        {
            // 현재 진행 방향 벡터
            Vector2d currentDirection = new Vector2d(
                currentPoint.X - previousPoint.X,
                currentPoint.Y - previousPoint.Y
            );

            // 후보 점들: 아직 방문하지 않은 점들
            List<Point3d> candidates = allPoints
                .Where(p => !visited.Contains(p))
                .Where(p => p.DistanceTo(currentPoint) > 0.001) // 너무 가까운 점 제외
                .ToList();

            if (candidates.Count == 0)
                return Point3d.Origin;

            // 최대 엣지 길이 필터링 (설정된 경우)
            if (maxEdgeLength > 0)
            {
                candidates = candidates
                    .Where(p => p.DistanceTo(currentPoint) <= maxEdgeLength)
                    .ToList();
            }

            if (candidates.Count == 0)
                return Point3d.Origin;

            // 시계 방향 각도로 정렬
            var sortedCandidates = candidates
                .Select(p => new
                {
                    Point = p,
                    Angle = CalculateClockwiseAngle(currentPoint, previousPoint, p),
                    Distance = currentPoint.DistanceTo(p)
                })
                .OrderBy(x => x.Angle) // 시계 방향 (각도 작은 순)
                .ThenBy(x => x.Distance) // 거리 가까운 순
                .ToList();

            // Visibility Check: 선분에 다른 점이 없는 첫 번째 점 선택
            foreach (var candidate in sortedCandidates)
            {
                if (IsVisible(currentPoint, candidate.Point, allPoints, visited))
                {
                    return candidate.Point;
                }
            }

            // Visibility를 만족하는 점이 없으면 가장 가까운 점 반환
            return sortedCandidates.First().Point;
        }

        /// <summary>
        /// 시계 방향 각도 계산
        /// 이전 점 → 현재 점 방향을 기준으로 현재 점 → 후보 점의 시계방향 각도
        /// </summary>
        private double CalculateClockwiseAngle(Point3d current, Point3d previous, Point3d candidate)
        {
            // 현재 진행 방향
            Vector2d directionVector = new Vector2d(
                current.X - previous.X,
                current.Y - previous.Y
            );

            // 후보 방향
            Vector2d candidateVector = new Vector2d(
                candidate.X - current.X,
                candidate.Y - current.Y
            );

            // 현재 방향의 각도
            double baseAngle = Math.Atan2(directionVector.Y, directionVector.X);

            // 후보 방향의 각도
            double candidateAngle = Math.Atan2(candidateVector.Y, candidateVector.X);

            // 시계 방향 각도 차이 계산
            double angle = baseAngle - candidateAngle;

            // 0 ~ 2π 범위로 정규화 (시계 방향이므로 음수가 더 시계방향)
            while (angle < 0) angle += 2 * Math.PI;
            while (angle > 2 * Math.PI) angle -= 2 * Math.PI;

            return angle;
        }

        /// <summary>
        /// Visibility Check: 두 점 사이의 선분에 다른 점이 있는지 확인
        /// </summary>
        private bool IsVisible(Point3d from, Point3d to, List<Point3d> allPoints, HashSet<Point3d> visited)
        {
            double threshold = 0.5; // 선분으로부터의 허용 거리

            foreach (Point3d point in allPoints)
            {
                // 시작점, 끝점, 이미 방문한 점은 무시
                if (point == from || point == to || visited.Contains(point))
                    continue;

                // 점이 선분에 너무 가까우면 visibility 방해
                if (DistanceFromPointToLineSegment(point, from, to) < threshold)
                {
                    // 추가 확인: 점이 실제로 선분 위에 있는지 (양쪽 끝이 아닌)
                    double t = ProjectionParameter(point, from, to);
                    if (t > 0.01 && t < 0.99) // 선분 내부
                    {
                        return false; // visibility 방해됨
                    }
                }
            }

            return true; // visibility 확보됨
        }

        /// <summary>
        /// 점에서 선분까지의 최단 거리
        /// </summary>
        private double DistanceFromPointToLineSegment(Point3d point, Point3d lineStart, Point3d lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 0.0001) // 선분이 점인 경우
                return point.DistanceTo(lineStart);

            // 선분 위의 투영 파라미터 (0~1 범위로 제한)
            double t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared));

            // 투영점
            double projX = lineStart.X + t * dx;
            double projY = lineStart.Y + t * dy;

            // 점에서 투영점까지의 거리
            return Math.Sqrt(Math.Pow(point.X - projX, 2) + Math.Pow(point.Y - projY, 2));
        }

        /// <summary>
        /// 점의 선분 위 투영 파라미터 (0~1 사이면 선분 내부)
        /// </summary>
        private double ProjectionParameter(Point3d point, Point3d lineStart, Point3d lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 0.0001)
                return 0;

            return ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        }

        /// <summary>
        /// 도면에서 모든 Point 엔티티 수집
        /// </summary>
        private List<Point3d> GetAllPointsFromDrawing(Transaction tr, Database db)
        {
            List<Point3d> points = new List<Point3d>();
            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

            foreach (ObjectId objId in btr)
            {
                Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                
                if (ent is DBPoint)
                {
                    DBPoint point = ent as DBPoint;
                    points.Add(new Point3d(point.Position.X, point.Position.Y,0.0));
                }
            }

            return points;
        }

        /// <summary>
        /// 점들로부터 폴리라인 생성
        /// </summary>
        private void CreatePolylineFromPoints(List<Point3d> points, short colorIndex, 
            bool closed, Transaction tr, Database db)
        {
            if (points.Count < 2)
                return;

            Polyline pline = new Polyline();
            
            for (int i = 0; i < points.Count; i++)
            {
                pline.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
            }

            pline.Closed = closed;
            pline.ColorIndex = colorIndex;

            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], 
                OpenMode.ForWrite) as BlockTableRecord;

            btr.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
        }
    }
}
