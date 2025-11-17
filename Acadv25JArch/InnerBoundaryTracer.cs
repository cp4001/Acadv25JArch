using Acadv25JArch;
using AutoCADPlugin;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CADExtension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using FlowDirection = System.Windows.Forms.FlowDirection;
using Image = System.Drawing.Image;

namespace InnerBoundaryTracking
{
    public class InnerBoundaryTracer
    {
        private const double TOLERANCE = 10; // 연결점 허용 오차
        private const int MAX_ITERATIONS = 1000; // 무한루프 방지
        private const short HIGHLIGHT_COLOR = 2; // Yellow (ACI 2)

        /// <summary>
        /// 내측 경계선 추적 메인 커맨드
        /// </summary>
        [CommandMethod("TraceInnerBoundary")]
        public void TraceInnerBoundary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 4: Transaction으로 Line 객체들 로드 및 추적
                using var tr = db.TransactionManager.StartTransaction();
                // Step 1: 전체 대상 Line들 선택
                var allLineIds = SelectAllLines(ed);
                if (allLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    return;
                }
                ed.WriteMessage($"\n총 {allLineIds.Count}개의 Line이 선택되었습니다.");

                //// Step 2: Base Point 선택
                //Point3d basePoint = SelectBasePoint(ed);
                //if (basePoint == Point3d.Origin && basePoint.X == 0 && basePoint.Y == 0)
                //    return;

                //ed.WriteMessage($"\nBase Point: ({basePoint.X:F3}, {basePoint.Y:F3})");

                // Step 3: 시작 Line 선택 (선택된 Line들 중에서만)
                var startLineResult = SelectStartLine(ed, allLineIds);
                if (startLineResult.lineId == ObjectId.Null)
                    return;

                ObjectId startLineId = startLineResult.lineId;
                Point3d clickPoint = startLineResult.clickPoint;

               

                var allLines = allLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                Line startLine = tr.GetObject(startLineId, OpenMode.ForRead) as Line;
                if (startLine == null)
                {
                    ed.WriteMessage("\n시작 Line을 읽을 수 없습니다.");
                    return;
                }

                // Step 5: 시작점 결정 (클릭 위치 기준)
                Point3d startPoint = DetermineStartPoint(startLine, clickPoint);
                ed.WriteMessage($"\n시작점: ({startPoint.X:F3}, {startPoint.Y:F3})");

                // Step 6: 경계선 추적
               // var tracedPath = TraceBoundary(allLines, startLine, startPoint, basePoint, ed);
                var tracedPath = TraceBoundary1(allLines, startLine, startPoint, ed);

                // 정렬된 순서로 순환적 이웃 관계 형성
                List<Point3d> intersectionPoints =  LineToPolylineConverter.CalculateIntersectionPoints(tracedPath);

                // 닫힌 폴리라인 생성
                var pl = LineToPolylineConverter.CreateClosedPolyline(tr, intersectionPoints, db, Jdf.Layer.RoomPoly);


                //foreach (var line in tracedPath)
                //{
                //    tr.RegisterTempGraphic(db, line);   
                //}

                tr.Commit();

                // Step 7: 결과 시각화
                if (tracedPath.Count > 0)
                {
                    //VisualizeTracedPath(tracedPath, db);
                    ed.WriteMessage($"\n총 {tracedPath.Count}개의 Line으로 구성된 내측 경계선이 추적되었습니다.");
                }
                else
                {
                    ed.WriteMessage("\n추적된 경로가 없습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 전체 대상 Line들 선택
        /// </summary>
        private List<ObjectId> SelectAllLines(Editor ed)
        {
            var lineIds = new List<ObjectId>();

            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE")
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n추적 대상이 될 모든 Line들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }

        /// <summary>
        /// Base Point 선택
        /// </summary>
        private Point3d SelectBasePoint(Editor ed)
        {
            var ppo = new PromptPointOptions("\nBase Point를 선택하세요 (내측/외측 판단 기준점): ")
            {
                AllowNone = false
            };

            var ppr = ed.GetPoint(ppo);

            if (ppr.Status == PromptStatus.OK)
                return ppr.Value;

            return Point3d.Origin;
        }

        /// <summary>
        /// 시작 Line 선택 (선택된 Line들 중에서만)
        /// </summary>
        private (ObjectId lineId, Point3d clickPoint) SelectStartLine(Editor ed, List<ObjectId> validLineIds)
        {
            var peo = new PromptEntityOptions("\n시작 Line을 선택하세요: ")
            {
                AllowNone = false
            };
            peo.SetRejectMessage("\n선택된 Line들 중에서만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            var per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                // 선택된 Line이 유효한 대상인지 확인
                if (validLineIds.Contains(per.ObjectId))
                {
                    return (per.ObjectId, per.PickedPoint);
                }
                else
                {
                    ed.WriteMessage("\n처음 선택한 Line들 중에서 선택해주세요.");
                    return (ObjectId.Null, Point3d.Origin);
                }
            }

            return (ObjectId.Null, Point3d.Origin);
        }

        /// <summary>
        /// 클릭 위치에서 가까운 끝점을 시작점으로 결정
        /// </summary>
        private Point3d DetermineStartPoint(Line line, Point3d clickPoint)
        {
            double distToStart = line.StartPoint.DistanceTo(clickPoint);
            double distToEnd = line.EndPoint.DistanceTo(clickPoint);

            return distToStart < distToEnd ? line.StartPoint : line.EndPoint;
        }

        /// <summary>
        /// 경계선 추적 메인 로직
        /// </summary>
        private List<Line> TraceBoundary(List<Line> allLines, Line startLine, Point3d startPoint, Point3d basePoint, Editor ed)
        {
            var tracedPath = new List<Line>();
            var visitedHandles = new HashSet<Handle>();

            Line currentLine = startLine;
            Point3d currentPoint = startPoint;
            Point3d previousPoint = GetOtherEnd(startLine, startPoint); // 진행 방향 결정용
            int iteration = 0;

            // 시작 Line 추가
            tracedPath.Add(currentLine);
            visitedHandles.Add(currentLine.Handle);

            ed.WriteMessage($"\n[0] Line Handle: {currentLine.Handle}, 현재점: ({currentPoint.X:F2}, {currentPoint.Y:F2})");

            while (iteration < MAX_ITERATIONS)
            {
                iteration++;

                // 현재 Line의 다음 끝점
                Point3d nextPoint = GetOtherEnd(currentLine, currentPoint);

                // 연결된 Line들 찾기
                var connectedLines =  FindConnectedLines(allLines, nextPoint, visitedHandles);

                if (connectedLines.Count == 0)
                {
                    ed.WriteMessage($"\n더 이상 연결된 Line이 없습니다. (반복: {iteration})");
                    break;
                }

                // 시작점으로 복귀 확인
                if (nextPoint.DistanceTo(startPoint) < TOLERANCE && iteration > 1)
                {
                    ed.WriteMessage($"\n시작점으로 복귀했습니다. (반복: {iteration})");
                    break;
                }

                // 내측 Line 선택 (Ray Casting + 벡터 각도)
                Line nextLine = SelectInnerMostLine(
                    connectedLines,
                    currentLine,
                    currentPoint,
                    nextPoint,
                    basePoint,
                    ed
                );

                if (nextLine == null)
                {
                    ed.WriteMessage($"\n다음 Line을 찾을 수 없습니다. (반복: {iteration})");
                    break;
                }

                // 다음 Line으로 이동
                tracedPath.Add(nextLine);
                visitedHandles.Add(nextLine.Handle);

                ed.WriteMessage($"\n[{iteration}] Line Handle: {nextLine.Handle}, 현재점: ({nextPoint.X:F2}, {nextPoint.Y:F2})");

                previousPoint = currentPoint;
                currentPoint = nextPoint;
                currentLine = nextLine;
            }

            if (iteration >= MAX_ITERATIONS)
            {
                ed.WriteMessage($"\n최대 반복 횟수({MAX_ITERATIONS})에 도달했습니다.");
            }

            return tracedPath;
        }
        private List<Line> TraceBoundary1(List<Line> allLines, Line startLine, Point3d sp,Editor ed)
        {
            var tracedPath = new List<Line>();
            //var visitedHandles = new HashSet<Handle>();

            Line currentLine = startLine;
            Point3d currentPoint = sp;// startLine.StartPoint.X > startLine.EndPoint.X ?  startLine.StartPoint : startLine.EndPoint;
            //Point3d currentPoint = startLine.Start  
            //Point3d currentPoint = startPoint;
            //Point3d previousPoint = GetOtherEnd(startLine, startPoint); // 진행 방향 결정용
            int iteration = 0;

            // 시작 Line 추가
            tracedPath.Add(currentLine);
            //visitedHandles.Add(currentLine.Handle);

            ed.WriteMessage($"\n[0] Line Handle: {currentLine.Handle}, 현재점: ({currentPoint.X:F2}, {currentPoint.Y:F2})");

            while (iteration < MAX_ITERATIONS)
            {
                iteration++;

                // 연결된 Line들 찾기
                var connectedLines = FindConnectedLines1(allLines, currentLine, currentPoint);
                if (connectedLines.Count <= 1)
                {
                    ed.WriteMessage($"\n더 이상 연결된 Line이 없습니다. (반복: {iteration})");
                    break;
                }
                var  fline =      ClockwiseLineSelector.FindMinimumClockwiseAngleLine(connectedLines);
                // 현재 Line의 다음 끝점
                Point3d nextPoint = GetOtherEnd(fline, currentPoint);

                // 시작점으로 복귀 확인
                if ((fline.StartPoint == tracedPath[0].StartPoint) && (fline.EndPoint == tracedPath[0].EndPoint) )
                {
                    ed.WriteMessage($"\n시작점으로 복귀했습니다. (반복: {iteration})");
                    break;
                }

                //if (nextLine == null)
                //{
                //    ed.WriteMessage($"\n다음 Line을 찾을 수 없습니다. (반복: {iteration})");
                //    break;
                //}

                // 다음 Line으로 이동
                tracedPath.Add(fline);
                currentPoint = nextPoint;
                currentLine = fline;
            }

            if (iteration >= MAX_ITERATIONS)
            {
                ed.WriteMessage($"\n최대 반복 횟수({MAX_ITERATIONS})에 도달했습니다.");
            }

            return tracedPath;
        }

        /// <summary>
        /// Line의 다른 쪽 끝점 반환
        /// </summary>
        private Point3d GetOtherEnd(Line line, Point3d currentEnd)
        {
            if (line.StartPoint.DistanceTo(currentEnd) < TOLERANCE)
                return line.EndPoint;
            else
                return line.StartPoint;
        }

        /// <summary>
        /// 특정 점에서 연결된 Line들 찾기 (GetClosestPointTo 사용)
        /// </summary>
        private List<Line> FindConnectedLines(List<Line> allLines, Point3d point, HashSet<Handle> visitedHandles)
        {
            var connectedLines = new List<Line>();

            foreach (var line in allLines)
            {
                // 이미 방문한 Line은 제외
                if (visitedHandles.Contains(line.Handle))
                    continue;

                // GetClosestPointTo로 가장 가까운 점 찾기
                Point3d closestPoint = line.GetClosestPointTo(point, false);
                double distance = closestPoint.DistanceTo(point);

                if (distance < TOLERANCE)
                {
                    connectedLines.Add(line);
                }
            }

            return connectedLines;
        }

        /// <summary>
        /// 특정 점에서 연결된 Line들 찾기 (GetClosestPointTo 사용)  재방문 허용 
        /// </summary>
        private List<Line> FindConnectedLines1(List<Line> allLines,Line baseLine, Point3d basepoint)
        {
            var connectedLines = new List<Line>();
            connectedLines.Add(baseLine); //기준선은 무조건 포함

            foreach (var line in allLines)
            {
                //db 에 등록되지 않는 개체는  hamdel 이 0 임
                if (line.StartPoint == baseLine.StartPoint && line.EndPoint == baseLine.EndPoint)
                { continue; }
                
                Point3d closestPoint = line.GetClosestPointTo(basepoint, false);
                double distance = closestPoint.DistanceTo(basepoint);

                if (distance < TOLERANCE)
                {
                    connectedLines.Add(line);
                }
            }

            return connectedLines;
        }

        /// <summary>
        /// Ray Casting + 벡터 각도로 내측(반시계방향) Line 선택
        /// </summary>
        private Line SelectInnerMostLine(
            List<Line> candidates,
            Line currentLine,
            Point3d currentPoint,
            Point3d nextPoint,
            Point3d basePoint,
            Editor ed)
        {
            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0];

            // 진행 방향 벡터 (현재 Line의 방향)
            Vector3d incomingDirection = (nextPoint - currentPoint).GetNormal();

            // Base Point 기준 벡터
            Vector3d toBase = (basePoint - nextPoint).GetNormal();

            double minAngle = double.MaxValue;
            Line selectedLine = null;

            foreach (var candidate in candidates)
            {
                // 후보 Line의 진행 방향 결정
                Point3d candidateNextPoint = GetOtherEnd(candidate, nextPoint);
                Vector3d outgoingDirection = (candidateNextPoint - nextPoint).GetNormal();

                // 반시계방향 각도 계산 (외적 사용)
                double angle = CalculateCounterClockwiseAngle(incomingDirection, outgoingDirection, toBase);

                ed.WriteMessage($"\n  후보 Handle: {candidate.Handle}, 각도: {angle:F2}°");

                if (angle < minAngle)
                {
                    minAngle = angle;
                    selectedLine = candidate;
                }
            }

            ed.WriteMessage($"\n  → 선택된 Line Handle: {selectedLine?.Handle}, 최소 각도: {minAngle:F2}°");

            return selectedLine;
        }

        /// <summary>
        /// 반시계방향 각도 계산 (외적 기반)
        /// </summary>
        private double CalculateCounterClockwiseAngle(Vector3d incoming, Vector3d outgoing, Vector3d toBase)
        {
            // 2D 평면으로 투영 (Z축 기준)
            Vector3d incoming2D = new Vector3d(incoming.X, incoming.Y, 0).GetNormal();
            Vector3d outgoing2D = new Vector3d(outgoing.X, outgoing.Y, 0).GetNormal();

            // 외적으로 좌회전/우회전 판단
            Vector3d cross = incoming2D.CrossProduct(outgoing2D);
            double dotProduct = incoming2D.DotProduct(outgoing2D);

            // 각도 계산 (0~360도)
            double angle = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dotProduct))) * 180.0 / Math.PI;

            // Z 성분이 음수면 우회전(시계방향) → 360 - angle
            if (cross.Z < 0)
            {
                angle = 360.0 - angle;
            }

            // Base Point 방향 고려 (내측 판단)
            // Base Point 쪽으로 향하는 것이 내측
            Vector3d toBase2D = new Vector3d(toBase.X, toBase.Y, 0).GetNormal();
            double baseAngle = incoming2D.DotProduct(toBase2D);

            // Base Point와의 관계를 고려하여 각도 보정
            // 내측으로 향하는 것을 우선 선택
            Vector3d outgoingCross = outgoing2D.CrossProduct(toBase2D);
            if (outgoingCross.Z > 0) // outgoing이 base 방향의 왼쪽에 있으면
            {
                angle += 0; // 내측으로 간주
            }

            return angle;
        }

        /// <summary>
        /// TransientGraphics로 추적된 경로 시각화
        /// </summary>
        private void VisualizeTracedPath(List<Line> tracedPath, Database db)
        {
            var transientManager = TransientManager.CurrentTransientManager;

            foreach (var line in tracedPath)
            {
                // 복제본 생성
                using Line highlightLine = line.Clone() as Line;
                if (highlightLine != null)
                {
                    highlightLine.ColorIndex = HIGHLIGHT_COLOR; // Yellow

                    // Transient로 추가
                    transientManager.AddTransient(
                        highlightLine,
                        TransientDrawingMode.Highlight,
                        128,
                        []
                    );
                }
            }
        }

        /// <summary>
        /// Transient 그래픽 지우기
        /// </summary>
        [CommandMethod("CLEARTRACED")]
        public void ClearTracedPath()
        {
            var transientManager = TransientManager.CurrentTransientManager;
            transientManager.EraseTransients(TransientDrawingMode.Highlight, 128, []);

            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.WriteMessage("\nTransient 그래픽이 제거되었습니다.");
        }
    }
}
