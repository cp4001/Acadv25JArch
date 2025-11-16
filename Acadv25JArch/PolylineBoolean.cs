using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Region = Autodesk.AutoCAD.DatabaseServices.Region;

namespace PolylineBooleanOperations
{
    public class PolylineBoolean
    {
        private const string COMMAND_NAME = "POLYSUBTRACT";

        /// <summary>
        /// 첫 번째 Polyline에서 두 번째 Polyline을 빼는 Boolean Subtract 연산
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void SubtractPolylines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 첫 번째 Closed Polyline 선택 (기준 도형)
                Polyline basePoly = SelectClosedPolyline(ed, "\n첫 번째 Closed Polyline을 선택하세요 (기준 도형): ");
                if (basePoly == null)
                {
                    ed.WriteMessage("\n취소되었습니다.");
                    return;
                }

                // 2단계: 두 번째 Closed Polyline 선택 (빼낼 도형)
                Polyline subtractPoly = SelectClosedPolyline(ed, "\n두 번째 Closed Polyline을 선택하세요 (빼낼 도형): ");
                if (subtractPoly == null)
                {
                    ed.WriteMessage("\n취소되었습니다.");
                    return;
                }

                // 3단계: Region으로 변환 및 Boolean 연산 수행
                Polyline resultPoly = PerformBooleanSubtract(basePoly, subtractPoly, db, ed);

                if (resultPoly == null)
                {
                    ed.WriteMessage("\nBoolean 연산에 실패했습니다.");
                    return;
                }

                // 4단계: 결과 Polyline을 데이터베이스에 추가
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // 결과 Polyline에 색상 적용 (빨간색)
                    resultPoly.ColorIndex = 1;// Color.FromColorIndex(ColorMethod.ByAci, 1); // Red

                    btr.AppendEntity(resultPoly);
                    tr.AddNewlyCreatedDBObject(resultPoly, true);

                    tr.Commit();

                    ed.WriteMessage("\n✓ Boolean Subtract 연산이 완료되었습니다.");
                    ed.WriteMessage($"\n  - 결과 Polyline 정점 수: {resultPoly.NumberOfVertices}");
                    ed.WriteMessage($"\n  - 결과 면적: {GetPolylineArea(resultPoly):F2}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Closed Polyline 선택 메서드
        /// </summary>
        private Polyline SelectClosedPolyline(Editor ed, string prompt)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nPolyline 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return null;

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                Polyline poly = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;

                // Closed 여부 확인
                if (poly != null && !poly.Closed)
                {
                    ed.WriteMessage("\n선택한 Polyline이 닫혀있지 않습니다. Closed Polyline을 선택하세요.");
                    tr.Commit();
                    return null;
                }

                tr.Commit();
                return poly;
            }
        }

        /// <summary>
        /// Boolean Subtract 연산 수행 (Region 방식)
        /// </summary>
        private Polyline PerformBooleanSubtract(Polyline basePoly, Polyline subtractPoly, Database db, Editor ed)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 1. 첫 번째 Polyline을 Region으로 변환
                    Region baseRegion = ConvertPolylineToRegion(basePoly, db);
                    if (baseRegion == null)
                    {
                        ed.WriteMessage("\n첫 번째 Polyline을 Region으로 변환하는데 실패했습니다.");
                        tr.Commit();
                        return null;
                    }

                    // 2. 두 번째 Polyline을 Region으로 변환
                    Region subtractRegion = ConvertPolylineToRegion(subtractPoly, db);
                    if (subtractRegion == null)
                    {
                        baseRegion.Dispose();
                        ed.WriteMessage("\n두 번째 Polyline을 Region으로 변환하는데 실패했습니다.");
                        tr.Commit();
                        return null;
                    }

                    // 3. Boolean Subtract 연산 수행
                    baseRegion.BooleanOperation(BooleanOperationType.BoolSubtract, subtractRegion);

                    // 4. Region을 Polyline으로 변환
                    Polyline resultPoly = ConvertRegionToPolyline(baseRegion, ed);

                    // Region 객체 정리
                    baseRegion.Dispose();
                    subtractRegion.Dispose();

                    tr.Commit();
                    return resultPoly;
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nBoolean 연산 중 오류: {ex.Message}");
                    tr.Abort();
                    return null;
                }
            }
        }

        /// <summary>
        /// Polyline을 Region으로 변환
        /// </summary>
        private Region ConvertPolylineToRegion(Polyline poly, Database db)
        {
            try
            {
                // DBObjectCollection 생성 및 Polyline 추가
                using (DBObjectCollection curves = new DBObjectCollection())
                {
                    curves.Add(poly);

                    // Region 생성
                    DBObjectCollection regions = Region.CreateFromCurves(curves);

                    if (regions.Count > 0)
                    {
                        // 첫 번째 Region 반환 (Closed Polyline은 하나의 Region 생성)
                        return regions[0] as Region;
                    }
                }

                return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Region을 Polyline으로 변환
        /// </summary>
        private Polyline ConvertRegionToPolyline(Region region, Editor ed)
        {
            try
            {
                // Region의 외곽선 추출을 위해 Explode 사용
                using (DBObjectCollection explodedObjects = new DBObjectCollection())
                {
                    region.Explode(explodedObjects);

                    if (explodedObjects.Count == 0)
                    {
                        ed.WriteMessage("\nRegion Explode 결과가 없습니다.");
                        return null;
                    }

                    // Explode된 객체들로부터 Polyline 생성
                    Polyline resultPoly = CreatePolylineFromCurves(explodedObjects, ed);

                    return resultPoly;
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nRegion을 Polyline으로 변환 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Curve 집합으로부터 Polyline 생성
        /// </summary>
        private Polyline CreatePolylineFromCurves(DBObjectCollection curves, Editor ed)
        {
            try
            {
                // Line, Arc 등의 Curve들로부터 연결된 순서로 정점 추출
                List<Point2d> vertices = new List<Point2d>();
                List<double> bulges = new List<double>();

                foreach (Entity entity in curves)
                {
                    if (entity is Line line)
                    {
                        // Line의 시작점 추가
                        vertices.Add(new Point2d(line.StartPoint.X, line.StartPoint.Y));
                        bulges.Add(0.0); // Line은 bulge 0
                    }
                    else if (entity is Arc arc)
                    {
                        // Arc의 시작점과 bulge 계산
                        vertices.Add(new Point2d(arc.StartPoint.X, arc.StartPoint.Y));
                        
                        // Bulge 계산: tan(포함각/4)
                        double includedAngle = arc.EndAngle - arc.StartAngle;
                        if (includedAngle < 0)
                            includedAngle += 2 * Math.PI;
                        
                        double bulge = Math.Tan(includedAngle / 4.0);
                        bulges.Add(bulge);
                    }
                    else if (entity is Curve curve)
                    {
                        // 기타 Curve 타입 처리
                        vertices.Add(new Point2d(curve.StartPoint.X, curve.StartPoint.Y));
                        bulges.Add(0.0);
                    }
                }

                if (vertices.Count < 3)
                {
                    ed.WriteMessage("\n유효한 정점이 충분하지 않습니다.");
                    return null;
                }

                // 새 Polyline 생성
                Polyline newPoly = new Polyline();
                newPoly.SetDatabaseDefaults();

                // 정점 추가
                for (int i = 0; i < vertices.Count; i++)
                {
                    newPoly.AddVertexAt(i, vertices[i], bulges[i], 0, 0);
                }

                // Polyline을 닫기
                newPoly.Closed = true;

                return newPoly;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nPolyline 생성 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Polyline의 면적 계산
        /// </summary>
        private double GetPolylineArea(Polyline poly)
        {
            try
            {
                return poly.Area;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// 디버그용: Region 정보 출력
        /// </summary>
        [CommandMethod("POLYSUBTRACT_TEST")]
        public void TestBooleanSubtract()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 테스트용: 선택된 두 Polyline의 Region 변환 확인
                Polyline poly1 = SelectClosedPolyline(ed, "\n첫 번째 Polyline 선택: ");
                if (poly1 == null) return;

                Polyline poly2 = SelectClosedPolyline(ed, "\n두 번째 Polyline 선택: ");
                if (poly2 == null) return;

                ed.WriteMessage("\n=== Polyline 정보 ===");
                ed.WriteMessage($"\n첫 번째 Polyline - 정점 수: {poly1.NumberOfVertices}, 면적: {poly1.Area:F2}, Closed: {poly1.Closed}");
                ed.WriteMessage($"\n두 번째 Polyline - 정점 수: {poly2.NumberOfVertices}, 면적: {poly2.Area:F2}, Closed: {poly2.Closed}");

                // Region 변환 테스트
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Region region1 = ConvertPolylineToRegion(poly1, db);
                    Region region2 = ConvertPolylineToRegion(poly2, db);

                    if (region1 != null && region2 != null)
                    {
                        ed.WriteMessage("\n✓ Region 변환 성공");
                        ed.WriteMessage($"\n  Region 1 면적: {region1.Area:F2}");
                        ed.WriteMessage($"\n  Region 2 면적: {region2.Area:F2}");

                        region1.Dispose();
                        region2.Dispose();
                    }
                    else
                    {
                        ed.WriteMessage("\n✗ Region 변환 실패");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }
    }
}
