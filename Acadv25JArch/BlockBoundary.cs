using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Transactions;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;
using Image = System.Drawing.Image;
using Transaction = Autodesk.AutoCAD.DatabaseServices.Transaction;


namespace BlockBoundaryPolyline
    {
        /// <summary>
        /// AutoCAD 블록의 외각 경계를 폴리라인으로 생성하는 프로그램
        /// 
        /// 주요 명령어:
        /// 1. CREATEBLOCKBOUNDARY - 기본 경계 박스 생성
        /// 2. CREATEBLOCKBOUNDARYADVANCED - 고급 옵션 (단순/개별엔티티)
        /// 3. CREATEBLOCKBOUNDARYEXACT - 원과 호의 정확한 형상으로 경계 생성
        /// 
        /// 주의사항:
        /// - 오목한 형태(L자, U자)의 블록은 정확한 외곽선 추출이 어려움
        /// - 복잡한 블록의 경우 개별 엔티티별로 경계를 생성하는 것을 권장
        /// </summary>

        // 경계 세그먼트를 나타내는 클래스
        public class BoundarySegment
        {
            public Point2d StartPoint { get; set; }
            public Point2d EndPoint { get; set; }
            public double Bulge { get; set; }

            public BoundarySegment(Point2d start, Point2d end, double bulge)
            {
                StartPoint = start;
                EndPoint = end;
                Bulge = bulge;
            }
        }

        //
        public class BlockUtils
        {
            public static Polyline GetRotatedBlockBoundary(BlockReference br)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                Polyline transformedBoundary = null;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Open the BlockTableRecord of the block definition
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);

                        // Get the bounding box of the original block definition
                        Extents3d blockExtents = GetBlockDefinitionExtents(tr, btr);

                        if (blockExtents.MinPoint.IsEqualTo(blockExtents.MaxPoint))
                        {
                            // Handle empty or invalid extents
                            return null;
                        }

                        // Get the transformation matrix from the BlockReference
                        Matrix3d matrix = br.BlockTransform;

                        // Define the four corners of the original bounding box
                        Point3d minPoint = blockExtents.MinPoint;
                        Point3d maxPoint = blockExtents.MaxPoint;

                        Point3d p1 = new Point3d(minPoint.X, minPoint.Y, 0);
                        Point3d p2 = new Point3d(maxPoint.X, minPoint.Y, 0);
                        Point3d p3 = new Point3d(maxPoint.X, maxPoint.Y, 0);
                        Point3d p4 = new Point3d(minPoint.X, maxPoint.Y, 0);

                        // Transform the corner points
                        p1 = p1.TransformBy(matrix);
                        p2 = p2.TransformBy(matrix);
                        p3 = p3.TransformBy(matrix);
                        p4 = p4.TransformBy(matrix);

                        // Create a new polyline for the transformed boundary
                        transformedBoundary = new Polyline();
                        transformedBoundary.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0, 0, 0);
                        transformedBoundary.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0, 0, 0);
                        transformedBoundary.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0, 0, 0);
                        transformedBoundary.AddVertexAt(3, new Point2d(p4.X, p4.Y), 0, 0, 0);
                        transformedBoundary.Closed = true;

                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage("\nError: " + ex.Message);
                    }
                }
                return transformedBoundary;
            }

            /// <summary>
            /// Gets the combined extents of all entities within a block definition.
            /// </summary>
            private static Extents3d GetBlockDefinitionExtents(Transaction tr, BlockTableRecord btr)
            {
                Extents3d combinedExtents = new Extents3d();
                bool hasExtents = false;

                foreach (ObjectId entId in btr)
                {
                    Entity ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                    if (ent.Bounds.HasValue)
                    {
                        if (!hasExtents)
                        {
                            combinedExtents = ent.Bounds.Value;
                            hasExtents = true;
                        }
                        else
                        {
                            combinedExtents.AddExtents(ent.Bounds.Value);
                        }
                    }
                }
                return combinedExtents;
            }
        }


        public class Commands
        {
            [CommandMethod("CREATEBLOCKBOUNDARY")]
            public void CreateBlockBoundary()
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                try
                {
                    // 사용자로부터 블록 선택
                    PromptEntityOptions peo = new PromptEntityOptions("\n블록을 선택하세요: ");
                    peo.SetRejectMessage("\n블록만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n명령이 취소되었습니다.");
                        return;
                    }

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        // 선택된 블록 참조 가져오기
                        BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blockRef == null)
                        {
                            ed.WriteMessage("\n올바른 블록이 아닙니다.");
                            return;
                        }

                        // 블록의 경계 박스 계산 (수정된 버전)
                        List<Point2d> boundaryPoints = GetBlockBoundingBox(blockRef);

                        if (boundaryPoints.Count < 3)
                        {
                            ed.WriteMessage("\n블록의 경계를 찾을 수 없습니다.");
                            return;
                        }

                        // 폴리라인 생성
                        Polyline poly = CreateBoundaryPolyline(boundaryPoints, blockRef);

                        // 현재 스페이스에 폴리라인 추가
                        BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        ObjectId polyId = currentSpace.AppendEntity(poly);
                        tr.AddNewlyCreatedDBObject(poly, true);

                        tr.Commit();

                        ed.WriteMessage($"\n블록 경계 폴리라인이 생성되었습니다. ObjectId: {polyId}");
                    }
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                }
            }

            /// <summary>
            /// 블록의 기본 경계 박스를 계산 (수정된 버전 - 이중 변환 오류 해결)
            /// </summary>
            private List<Point2d> GetBlockBoundingBox(BlockReference blockRef)
            {
                List<Point2d> points = new List<Point2d>();

                try
                {
                    // GeometricExtents는 이미 블록의 모든 변환(이동, 회전, 축척)이 적용된 WCS 좌표
                    // 추가 변환을 적용하면 안됨!
                    Extents3d extents = blockRef.GeometricExtents;

                    Point3d min = extents.MinPoint;
                    Point3d max = extents.MaxPoint;

                    // 경계 박스의 네 모서리 점들
                    points.Add(new Point2d(min.X, min.Y));
                    points.Add(new Point2d(max.X, min.Y));
                    points.Add(new Point2d(max.X, max.Y));
                    points.Add(new Point2d(min.X, max.Y));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"경계 박스 계산 중 오류: {ex.Message}");
                }

                return points;
            }

            [CommandMethod("CREATEBLOCKBOUNDARYADVANCED")]
            public void CreateBlockBoundaryAdvanced()
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                try
                {
                    // 고급 옵션 설정 (Convex Hull 제거, 더 현실적인 옵션 제공)
                    PromptKeywordOptions pko = new PromptKeywordOptions("\n경계 생성 방법을 선택하세요 [경계박스(B)/개별엔티티(I)/정확한형상(E)] <정확한형상>: ");
                    pko.Keywords.Add("B");
                    pko.Keywords.Add("I");
                    pko.Keywords.Add("E");
                    pko.Keywords.Default = "E";

                    PromptResult pkr = ed.GetKeywords(pko);
                    string boundaryType = pkr.Status == PromptStatus.OK ? pkr.StringResult : "E";

                    // 블록 선택
                    PromptEntityOptions peo = new PromptEntityOptions("\n블록을 선택하세요: ");
                    peo.SetRejectMessage("\n블록만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n명령이 취소되었습니다.");
                        return;
                    }

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blockRef == null)
                        {
                            ed.WriteMessage("\n올바른 블록이 아닙니다.");
                            return;
                        }

                        List<ObjectId> createdPolylines = new List<ObjectId>();

                        if (boundaryType == "B")
                        {
                            // 경계 박스
                            var boundaryPoints = GetBlockBoundingBox(blockRef);
                            if (boundaryPoints.Count >= 3)
                            {
                                Polyline poly = CreateBoundaryPolyline(boundaryPoints, blockRef);
                                ObjectId polyId = AddPolylineToDrawing(poly, blockRef, "BoundingBox", tr, db);
                                createdPolylines.Add(polyId);
                            }
                        }
                        else if (boundaryType == "I")
                        {
                            // 개별 엔티티별 경계 (오목한 형태 문제 해결)
                            ed.WriteMessage("\n개별 엔티티별 경계를 생성 중...");
                            createdPolylines = CreateIndividualEntityBoundaries(blockRef, tr, db);
                        }
                        else
                        {
                            // 정확한 형상 경계 (수정된 버전)
                            ed.WriteMessage("\n정확한 형상 경계를 계산 중...");
                            var boundarySegments = GetExactBoundarySegments(blockRef, tr);

                            if (boundarySegments.Count > 0)
                            {
                                Polyline poly = CreateBoundaryPolylineWithBulges(boundarySegments, blockRef);
                                ObjectId polyId = AddPolylineToDrawing(poly, blockRef, "ExactBoundary", tr, db);
                                createdPolylines.Add(polyId);
                            }
                        }

                        tr.Commit();

                        if (createdPolylines.Count > 0)
                        {
                            ed.WriteMessage($"\n블록 '{blockRef.Name}'의 경계가 성공적으로 생성되었습니다.");
                            ed.WriteMessage($"\n생성된 폴리라인 수: {createdPolylines.Count}");
                        }
                        else
                        {
                            ed.WriteMessage("\n경계를 생성할 수 없습니다.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                }
            }

            /// <summary>
            /// 개별 엔티티별로 경계를 생성 (오목한 형태 문제 해결)
            /// </summary>
            private List<ObjectId> CreateIndividualEntityBoundaries(BlockReference blockRef, Transaction tr, Database db)
            {
                List<ObjectId> createdPolylines = new List<ObjectId>();

                try
                {
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (blockDef == null) return createdPolylines;

                    Matrix3d transform = blockRef.BlockTransform;
                    int entityCount = 0;

                    foreach (ObjectId entId in blockDef)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        List<BoundarySegment> segments = new List<BoundarySegment>();
                        CollectEntityBoundarySegments(ent, segments, transform);

                        if (segments.Count > 0)
                        {
                            Polyline poly = CreateBoundaryPolylineWithBulges(segments, blockRef);
                            if (poly != null)
                            {
                                string layerSuffix = $"Entity{entityCount:D2}_{ent.GetRXClass().Name}";
                                ObjectId polyId = AddPolylineToDrawing(poly, blockRef, layerSuffix, tr, db);
                                createdPolylines.Add(polyId);
                                entityCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"개별 엔티티 경계 생성 중 오류: {ex.Message}");
                }

                return createdPolylines;
            }

            /// <summary>
            /// 정확한 형상의 경계 세그먼트 수집 (Convex Hull 제거)
            /// </summary>
            private List<BoundarySegment> GetExactBoundarySegments(BlockReference blockRef, Transaction tr)
            {
                List<BoundarySegment> allSegments = new List<BoundarySegment>();

                try
                {
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (blockDef == null) return allSegments;

                    Matrix3d transform = blockRef.BlockTransform;

                    // 블록 정의 내의 모든 엔티티 순회
                    foreach (ObjectId entId in blockDef)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        // 엔티티의 경계 세그먼트들 수집
                        CollectEntityBoundarySegments(ent, allSegments, transform);
                    }

                    // Convex Hull 제거: 모든 세그먼트를 그대로 사용
                    // 복잡한 형태는 개별 엔티티 모드를 사용하도록 안내
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"정확한 경계 세그먼트 계산 중 오류: {ex.Message}");
                }

                return allSegments;
            }

            /// <summary>
            /// 엔티티별 경계 세그먼트 수집 (수정된 버전 - 원의 bulge 값 수정)
            /// </summary>
            private void CollectEntityBoundarySegments(Entity entity, List<BoundarySegment> segments, Matrix3d transform)
            {
                try
                {
                    if (entity is Line line)
                    {
                        Point3d start = line.StartPoint.TransformBy(transform);
                        Point3d end = line.EndPoint.TransformBy(transform);
                        segments.Add(new BoundarySegment(
                            new Point2d(start.X, start.Y),
                            new Point2d(end.X, end.Y),
                            0.0)); // bulge = 0 for straight line
                    }
                    else if (entity is Polyline poly)
                    {
                        for (int i = 0; i < poly.NumberOfVertices; i++)
                        {
                            Point3d vertex = poly.GetPoint3dAt(i).TransformBy(transform);
                            Point2d currentPoint = new Point2d(vertex.X, vertex.Y);

                            int nextIndex = (i + 1) % poly.NumberOfVertices;
                            if (!poly.Closed && nextIndex == 0) break;

                            Point3d nextVertex = poly.GetPoint3dAt(nextIndex).TransformBy(transform);
                            Point2d nextPoint = new Point2d(nextVertex.X, nextVertex.Y);

                            double bulge = poly.GetBulgeAt(i);
                            segments.Add(new BoundarySegment(currentPoint, nextPoint, bulge));
                        }
                    }
                    else if (entity is Circle circle)
                    {
                        // 수정된 원 처리: 정확한 bulge 값 계산
                        Point3d center = circle.Center.TransformBy(transform);

                        // 변환된 반지름 계산 (회전/스케일 고려)
                        Vector3d radiusVector = new Vector3d(circle.Radius, 0, 0);
                        Vector3d transformedRadius = radiusVector.TransformBy(transform);
                        double radius = transformedRadius.Length;

                        Point2d centerPt = new Point2d(center.X, center.Y);

                        // 변환된 X축 방향 계산
                        Vector3d xAxis = Vector3d.XAxis.TransformBy(transform).GetNormal();
                        Vector3d yAxis = Vector3d.YAxis.TransformBy(transform).GetNormal();

                        // 4개의 90도 호로 분할
                        Point2d p1 = centerPt + new Vector2d(xAxis.X * radius, xAxis.Y * radius);
                        Point2d p2 = centerPt + new Vector2d(yAxis.X * radius, yAxis.Y * radius);
                        Point2d p3 = centerPt - new Vector2d(xAxis.X * radius, xAxis.Y * radius);
                        Point2d p4 = centerPt - new Vector2d(yAxis.X * radius, yAxis.Y * radius);

                        // 수정된 bulge 값: tan(90° / 4) = tan(22.5°) ≈ 0.41421356
                        double bulge = Math.Tan(Math.PI / 8.0); // 1.0이 아님!

                        segments.Add(new BoundarySegment(p1, p2, bulge));
                        segments.Add(new BoundarySegment(p2, p3, bulge));
                        segments.Add(new BoundarySegment(p3, p4, bulge));
                        segments.Add(new BoundarySegment(p4, p1, bulge));
                    }
                    else if (entity is Arc arc)
                    {
                        Point3d start = arc.StartPoint.TransformBy(transform);
                        Point3d end = arc.EndPoint.TransformBy(transform);

                        // 호의 bulge 계산: bulge = tan(sweep_angle / 4)
                        double sweepAngle = arc.TotalAngle;
                        double bulge = Math.Tan(sweepAngle / 4.0);

                        // 호의 방향 결정 (시계 방향인지 반시계 방향인지)
                        if (arc.StartAngle > arc.EndAngle)
                        {
                            bulge = -bulge;
                        }

                        segments.Add(new BoundarySegment(
                            new Point2d(start.X, start.Y),
                            new Point2d(end.X, end.Y),
                            bulge));
                    }
                    else if (entity is Ellipse ellipse)
                    {
                        if (Math.Abs(ellipse.RadiusRatio - 1.0) < 1e-6)
                        {
                            // 원형 타원을 원으로 처리 (수정된 bulge 값 사용)
                            Point3d center = ellipse.Center.TransformBy(transform);

                            Vector3d radiusVector = new Vector3d(ellipse.MajorRadius, 0, 0);
                            Vector3d transformedRadius = radiusVector.TransformBy(transform);
                            double radius = transformedRadius.Length;

                            Point2d centerPt = new Point2d(center.X, center.Y);

                            Vector3d xAxis = Vector3d.XAxis.TransformBy(transform).GetNormal();
                            Vector3d yAxis = Vector3d.YAxis.TransformBy(transform).GetNormal();

                            Point2d p1 = centerPt + new Vector2d(xAxis.X * radius, xAxis.Y * radius);
                            Point2d p2 = centerPt + new Vector2d(yAxis.X * radius, yAxis.Y * radius);
                            Point2d p3 = centerPt - new Vector2d(xAxis.X * radius, xAxis.Y * radius);
                            Point2d p4 = centerPt - new Vector2d(yAxis.X * radius, yAxis.Y * radius);

                            double bulge = Math.Tan(Math.PI / 8.0); // 수정된 bulge 값

                            segments.Add(new BoundarySegment(p1, p2, bulge));
                            segments.Add(new BoundarySegment(p2, p3, bulge));
                            segments.Add(new BoundarySegment(p3, p4, bulge));
                            segments.Add(new BoundarySegment(p4, p1, bulge));
                        }
                        else
                        {
                            //// 일반 타원은 다각형으로 근사
                            //var ellipsePoints = ApproximateEllipse(ellipse, transform, 16);
                            //for (int i = 0; i < ellipsePoints.Count; i++)
                            //{
                            //    int nextIndex = (i + 1) % ellipsePoints.Count;
                            //    segments.Add(new BoundarySegment(ellipsePoints[i], ellipsePoints[nextIndex], 0.0));
                            //}
                        }
                    }
                    else
                    {
                        // 기타 엔티티는 GeometricExtents를 사용한 직사각형으로 처리
                        try
                        {
                            Extents3d extents = entity.GeometricExtents;
                            Point3d min = extents.MinPoint.TransformBy(transform);
                            Point3d max = extents.MaxPoint.TransformBy(transform);

                            Point2d[] rectPoints = new Point2d[4];
                            rectPoints[0] = new Point2d(min.X, min.Y);
                            rectPoints[1] = new Point2d(max.X, min.Y);
                            rectPoints[2] = new Point2d(max.X, max.Y);
                            rectPoints[3] = new Point2d(min.X, max.Y);

                            for (int i = 0; i < 4; i++)
                            {
                                int nextIndex = (i + 1) % 4;
                                segments.Add(new BoundarySegment(rectPoints[i], rectPoints[nextIndex], 0.0));
                            }
                        }
                        catch
                        {
                            // 경계를 계산할 수 없는 엔티티는 무시
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"엔티티 경계 세그먼트 수집 중 오류: {ex.Message}");
                }
            }

            //private List<Point2d> ApproximateEllipse(Ellipse ellipse, Matrix3d transform, int segments)
            //{
            //    List<Point2d> points = new List<Point2d>();

            //    try
            //    {
            //        double startParam = ellipse.StartParameter;
            //        double endParam = ellipse.EndParameter;
            //        double deltaParam = (endParam - startParam) / segments;

            //        for (int i = 0; i < segments; i++)
            //        {
            //            double param = startParam + i * deltaParam;
            //            Point3d point = ellipse.GetPointAtParameter(param);
            //            Point3d transformedPoint = point.TransformBy(transform);
            //            points.Add(new Point2d(transformedPoint.X, transformedPoint.Y));
            //        }
            //    }
            //    catch
            //    {
            //        // 오류 발생 시 빈 리스트 반환
            //    }

            //    return points;
            //}

            private Polyline CreateBoundaryPolyline(List<Point2d> points, BlockReference blockRef)
            {
                if (points.Count < 3) return null;

                Polyline poly = new Polyline();
                poly.SetDatabaseDefaults();
                poly.Layer = blockRef.Layer;
                poly.ColorIndex = 1; // 빨간색

                // 점들을 폴리라인에 추가
                for (int i = 0; i < points.Count; i++)
                {
                    poly.AddVertexAt(i, points[i], 0, 0, 0);
                }

                poly.Closed = true;
                return poly;
            }

            private Polyline CreateBoundaryPolylineWithBulges(List<BoundarySegment> segments, BlockReference blockRef)
            {
                if (segments.Count == 0) return null;

                Polyline poly = new Polyline();
                poly.SetDatabaseDefaults();
                poly.Layer = blockRef.Layer;
                poly.ColorIndex = 2; // 노란색

                // 세그먼트들을 폴리라인에 추가
                for (int i = 0; i < segments.Count; i++)
                {
                    poly.AddVertexAt(i, segments[i].StartPoint, segments[i].Bulge, 0, 0);
                }

                poly.Closed = true;
                return poly;
            }

            private ObjectId AddPolylineToDrawing(Polyline poly, BlockReference blockRef, string layerSuffix, Transaction tr, Database db)
            {
                if (poly == null) return ObjectId.Null;

                try
                {
                    // 레이어 설정
                    string layerName = $"{blockRef.Name}_{layerSuffix}";
                    EnsureLayerExists(layerName, tr, db);
                    poly.Layer = layerName;

                    // 도면에 추가
                    BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    ObjectId polyId = currentSpace.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);

                    return polyId;
                }
                catch
                {
                    return ObjectId.Null;
                }
            }

            [CommandMethod("CREATEBLOCKBOUNDARYEXACT")]
            public void CreateBlockBoundaryExact()
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                try
                {
                    ed.WriteMessage("\n=== 정확한 형상 경계 생성 ===");
                    ed.WriteMessage("\n주의: 복잡한 블록(L자, U자 등)은 개별 엔티티 모드를 권장합니다.");

                    // 블록 선택
                    PromptEntityOptions peo = new PromptEntityOptions("\n블록을 선택하세요: ");
                    peo.SetRejectMessage("\n블록만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n명령이 취소되었습니다.");
                        return;
                    }

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blockRef == null)
                        {
                            ed.WriteMessage("\n올바른 블록이 아닙니다.");
                            return;
                        }

                        ed.WriteMessage("\n정확한 형상 경계를 계산 중...");
                        var boundarySegments = GetExactBoundarySegments(blockRef, tr);

                        if (boundarySegments.Count == 0)
                        {
                            ed.WriteMessage("\n블록의 경계를 찾을 수 없습니다.");
                            return;
                        }

                        Polyline poly = CreateBoundaryPolylineWithBulges(boundarySegments, blockRef);
                        ObjectId polyId = AddPolylineToDrawing(poly, blockRef, "ExactShape", tr, db);

                        tr.Commit();

                        ed.WriteMessage($"\n블록 '{blockRef.Name}'의 정확한 형상 경계가 생성되었습니다.");
                        ed.WriteMessage($"\n세그먼트 수: {boundarySegments.Count}");

                        // bulge가 있는 세그먼트 개수 표시
                        int curvedSegments = boundarySegments.Count(s => Math.Abs(s.Bulge) > 1e-9);
                        if (curvedSegments > 0)
                        {
                            ed.WriteMessage($"\n곡선 세그먼트 수: {curvedSegments} (원/호의 정확한 형상 반영)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                }
            }

            private void EnsureLayerExists(string layerName, Transaction tr, Database db)
            {
                LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!layerTable.Has(layerName))
                {
                    layerTable.UpgradeOpen();
                    LayerTableRecord layerRecord = new LayerTableRecord();
                    layerRecord.Name = layerName;
                    layerRecord.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);

                    ObjectId layerId = layerTable.Add(layerRecord);
                    tr.AddNewlyCreatedDBObject(layerRecord, true);
                }
            }
        }

        public class Commands_BlockUtils
        {
        [CommandMethod("GETBLOCKBOUNDARY")]
        public void GetBlockBoundaryCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect a block reference:");
            //peo.SetAllowableClass(typeof(BlockReference), false);

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    BlockReference br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);

                    Polyline rotatedBoundary = BlockUtils.GetRotatedBlockBoundary(br);

                    if (rotatedBoundary != null)
                    {
                        // Add the new polyline to the current space for visual inspection
                        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                        rotatedBoundary.ColorIndex = 1; // Red

                        btr.AppendEntity(rotatedBoundary);
                        tr.AddNewlyCreatedDBObject(rotatedBoundary, true);

                        ed.WriteMessage("\nRotated boundary drawn successfully.");
                    }

                    tr.Commit();
                }
            }
        }

    }
}

