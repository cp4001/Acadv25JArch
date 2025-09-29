using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace XclipBoundaryToPolyline
{
    public class Commands
    {
        [CommandMethod("XCLIP2PL")]
        public static void Cmd_CreatePolylineFromXclipBoundary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 사용자에게 블록 참조 선택 요청
            PromptEntityOptions peo = new PromptEntityOptions("\nXCLIP 경계를 가진 블록을 선택하세요: ");
            peo.SetRejectMessage("\n블록 참조만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n명령이 취소되었습니다.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockReference blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

                    // SpatialFilter (XCLIP 정보) 가져오기
                    DBDictionary extDict = tr.GetObject(blockRef.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                    if (extDict == null || !extDict.Contains("ACAD_FILTER"))
                    {
                        ed.WriteMessage("\n선택된 블록에 XCLIP 경계가 없습니다.");
                        return;
                    }

                    DBDictionary filterDict = tr.GetObject(extDict.GetAt("ACAD_FILTER"), OpenMode.ForRead) as DBDictionary;
                    if (filterDict == null || !filterDict.Contains("SPATIAL"))
                    {
                        ed.WriteMessage("\n선택된 블록에서 SPATIAL 필터를 찾을 수 없습니다.");
                        return;
                    }

                    SpatialFilter spatialFilter = tr.GetObject(filterDict.GetAt("SPATIAL"), OpenMode.ForRead) as SpatialFilter;
                    if (spatialFilter == null)
                    {
                        ed.WriteMessage("\nSpatialFilter를 가져오는 데 실패했습니다.");
                        return;
                    }

                    // 경계 정점 가져오기
                    Point2dCollection boundaryPoints = spatialFilter.Definition.GetPoints();
                    if (boundaryPoints.Count < 2)
                    {
                        ed.WriteMessage("\n유효한 경계 정점을 찾을 수 없습니다.");
                        return;
                    }

                    // 새로운 폴리라인 생성
                    Polyline polyline = new Polyline();

                    // 블록의 변환 행렬(위치, 회전, 축척)을 적용하여 좌표 변환
                    Matrix3d transform = blockRef.BlockTransform;

                    for (int i = 0; i < boundaryPoints.Count; i++)
                    {
                        Point3d pt3d = new Point3d(boundaryPoints[i].X, boundaryPoints[i].Y, 0);
                        Point3d transformedPt = pt3d.TransformBy(transform);
                        polyline.AddVertexAt(i, new Point2d(transformedPt.X, transformedPt.Y), 0, 0, 0);
                    }

                    // 경계가 직사각형일 경우 두 개의 점만 반환되므로 폴리라인을 완성해야 함
                    if (boundaryPoints.Count == 2)
                    {
                        Point2d p1 = polyline.GetPoint2dAt(0);
                        Point2d p2 = polyline.GetPoint2dAt(1);
                        polyline.AddVertexAt(1, new Point2d(p1.X, p2.Y), 0, 0, 0);
                        polyline.AddVertexAt(3, new Point2d(p2.X, p1.Y), 0, 0, 0);
                    }

                    polyline.Closed = true;

                    // 현재 도면 공간에 폴리라인 추가
                    BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    currentSpace.AppendEntity(polyline);
                    tr.AddNewlyCreatedDBObject(polyline, true);

                    ed.WriteMessage($"\nXCLIP 경계로부터 폴리라인이 성공적으로 생성되었습니다.");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                }
                tr.Commit();
            }
        }
    }
}
