using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using CADExtension;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;
using Region = Autodesk.AutoCAD.DatabaseServices.Region;

namespace Acadv25JArch
{
    public class PolylineBooleanOperation
    {
        [CommandMethod("POLYDIFF")]
        public void Cmd_PolylineDifference()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 첫 번째 폴리라인 선택 (A)
                PromptEntityOptions peo1 = new PromptEntityOptions("\n첫 번째 폴리라인 선택 (A): ");
                peo1.SetRejectMessage("\n폴리라인을 선택해야 합니다.");
                peo1.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per1 = ed.GetEntity(peo1);

                if (per1.Status != PromptStatus.OK)
                    return;

                // 두 번째 폴리라인 선택 (B)
                PromptEntityOptions peo2 = new PromptEntityOptions("\n두 번째 폴리라인 선택 (B): ");
                peo2.SetRejectMessage("\n폴리라인을 선택해야 합니다.");
                peo2.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per2 = ed.GetEntity(peo2);

                if (per2.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 폴리라인 A와 B 가져오기
                    Polyline polyA = tr.GetObject(per1.ObjectId, OpenMode.ForRead) as Polyline;
                    Polyline polyB = tr.GetObject(per2.ObjectId, OpenMode.ForRead) as Polyline;

                    if (polyA == null || polyB == null)
                    {
                        ed.WriteMessage("\n유효한 폴리라인이 아닙니다.");
                        return;
                    }

                    // 닫힌 폴리라인인지 확인
                    if (!polyA.Closed || !polyB.Closed)
                    {
                        ed.WriteMessage("\n두 폴리라인 모두 닫혀있어야 합니다.");
                        return;
                    }

                    // 폴리라인을 Region으로 변환
                    DBObjectCollection objCollA = new DBObjectCollection();
                    objCollA.Add(polyA);
                    DBObjectCollection regionsA = Region.CreateFromCurves(objCollA);

                    DBObjectCollection objCollB = new DBObjectCollection();
                    objCollB.Add(polyB);
                    DBObjectCollection regionsB = Region.CreateFromCurves(objCollB);

                    if (regionsA.Count == 0 || regionsB.Count == 0)
                    {
                        ed.WriteMessage("\nRegion 생성에 실패했습니다.");
                        return;
                    }

                    Region regionA = regionsA[0] as Region;
                    Region regionB = regionsB[0] as Region;

                    // A - B 불린 차집합 연산
                    regionA.BooleanOperation(BooleanOperationType.BoolSubtract, regionB);

                    // Region을 바로 Polyline으로 변환
                    DBObjectCollection polylines = ConvertRegionToPolylines(regionA, polyA);

                    if (polylines.Count > 0)
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        foreach (Polyline poly in polylines)
                        {
                            btr.AppendEntity(poly);
                            tr.AddNewlyCreatedDBObject(poly, true);
                        }

                        ed.WriteMessage($"\n차집합 연산 완료! {polylines.Count}개의 Polyline이 생성되었습니다.");
                    }
                    else
                    {
                        ed.WriteMessage("\nPolyline 변환에 실패했습니다.");
                    }

                    // Region 정리
                    regionA.Dispose();
                    regionB.Dispose();

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        [CommandMethod("REGIONTOPOLY")]
        public void Cmd_RegionToPolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nRegion 선택: ");
            peo.SetRejectMessage("\nRegion을 선택해야 합니다.");
            peo.AddAllowedClass(typeof(Region), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Region reg = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Region;

                if (reg == null)
                {
                    ed.WriteMessage("\n유효한 Region이 아닙니다.");
                    return;
                }

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Region을 Polyline으로 변환
                DBObjectCollection polylines = ConvertRegionToPolylines(reg, reg);

                if (polylines.Count > 0)
                {
                    foreach (Polyline poly in polylines)
                    {
                        btr.AppendEntity(poly);
                        tr.AddNewlyCreatedDBObject(poly, true);
                    }

                    // 원본 Region 삭제
                    reg.Erase();

                    ed.WriteMessage($"\n{polylines.Count}개의 Polyline으로 변환되었습니다.");
                }
                else
                {
                    ed.WriteMessage("\nPolyline 변환에 실패했습니다.");
                }

                tr.Commit();
            }
        }

        // Region을 Polyline(s)로 변환하는 핵심 메서드
        private DBObjectCollection ConvertRegionToPolylines(Region reg, Entity sourceEntity)
        {
            DBObjectCollection result = new DBObjectCollection();

            // Explode Region -> collection of Curves
            DBObjectCollection curves = new DBObjectCollection();
            reg.Explode(curves);

            if (curves.Count == 0)
                return result;

            // Region의 평면 생성 (3D 좌표를 Region 좌표계로 변환)
            Plane pl = new Plane(new Point3d(0, 0, 0), reg.Normal);

            // 곡선들을 그룹화 (연결된 곡선들끼리)
            while (curves.Count > 0)
            {
                Polyline poly = new Polyline();

                // 원본 엔티티의 속성 복사
                poly.SetPropertiesFrom(sourceEntity);

                // 첫 번째 곡선으로 시작
                Curve cv1 = curves[0] as Curve;

                // 첫 번째 정점 추가
                poly.AddVertexAt(
                    poly.NumberOfVertices,
                    cv1.StartPoint.Convert2d(pl),
                    BulgeFromCurve(cv1, false),
                    0, 0
                );

                // 두 번째 정점 추가
                poly.AddVertexAt(
                    poly.NumberOfVertices,
                    cv1.EndPoint.Convert2d(pl),
                    0, 0, 0
                );

                curves.Remove(cv1);

                // 다음에 찾을 점
                Point3d nextPt = cv1.EndPoint;

                // 무한 루프 방지
                int prevCnt = curves.Count + 1;

                while (curves.Count > 0 && curves.Count < prevCnt)
                {
                    prevCnt = curves.Count;

                    foreach (Curve cv in curves)
                    {
                        // 연결된 곡선 찾기
                        if (cv.StartPoint.IsEqualTo(nextPt) || cv.EndPoint.IsEqualTo(nextPt))
                        {
                            // Bulge 계산 및 이전 정점에 설정
                            double bulge = BulgeFromCurve(cv, cv.EndPoint.IsEqualTo(nextPt));
                            poly.SetBulgeAt(poly.NumberOfVertices - 1, bulge);

                            // 다음 점 결정
                            if (cv.StartPoint.IsEqualTo(nextPt))
                                nextPt = cv.EndPoint;
                            else
                                nextPt = cv.StartPoint;

                            // 새 정점 추가
                            poly.AddVertexAt(
                                poly.NumberOfVertices,
                                nextPt.Convert2d(pl),
                                0, 0, 0
                            );

                            curves.Remove(cv);
                            break;
                        }
                    }
                }

                // 연결 실패 체크
                if (curves.Count >= prevCnt)
                {
                    poly.Dispose();
                    // 연결 실패 시 다음 그룹 시도
                    if (curves.Count > 0)
                    {
                        curves.Remove(curves[0]);
                        continue;
                    }
                    break;
                }

                // 폴리라인을 원래 Region의 평면으로 변환
                poly.TransformBy(Matrix3d.PlaneToWorld(pl));

                // 닫힌 폴리라인 체크 (시작점과 끝점이 같으면)
                if (poly.NumberOfVertices > 2)
                {
                    Point3d start = poly.GetPoint3dAt(0);
                    Point3d end = poly.GetPoint3dAt(poly.NumberOfVertices - 1);
                    if (start.IsEqualTo(end, new Tolerance(0.001, 0.001)))
                    {
                        // 마지막 정점 제거하고 닫기
                        poly.RemoveVertexAt(poly.NumberOfVertices - 1);
                        poly.Closed = true;
                    }
                }

                result.Add(poly);
            }

            return result;
        }

        // Arc에서 Bulge 계산하는 헬퍼 메서드
        private static double BulgeFromCurve(Curve cv, bool clockwise)
        {
            double bulge = 0.0;
            Arc arc = cv as Arc;

            if (arc != null)
            {
                double newStart;

                // Arc는 모두 반시계방향이므로 시작각이 끝각보다 큼
                // (0도 라인을 넘는 경우는 시작각에서 2PI를 뺌)
                if (arc.StartAngle > arc.EndAngle)
                    newStart = arc.StartAngle - 8 * Math.Atan(1); // 2 * PI
                else
                    newStart = arc.StartAngle;

                // Bulge는 포함각의 1/4의 탄젠트
                bulge = Math.Tan((arc.EndAngle - newStart) / 4);

                // 시계방향이면 부호 반전
                if (clockwise)
                    bulge = -bulge;
            }

            return bulge;
        }
    }
}
