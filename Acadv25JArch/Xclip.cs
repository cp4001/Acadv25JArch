using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acadv25JArch
{
    public class XClipCommands
    {
        [CommandMethod("PROCESSXCLIP")]
        public void ProcessXClippedObjects()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 객체 선택
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nXCLIP 처리할 객체를 선택하세요: ";

                PromptSelectionResult psr = ed.GetSelection(pso);
                if (psr.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    SelectionSet ss = psr.Value;
                    List<ObjectId> objectsToDelete = new List<ObjectId>();
                    List<Entity> newEntities = new List<Entity>();

                    foreach (SelectedObject so in ss)
                    {
                        Entity ent = (Entity)tr.GetObject(so.ObjectId, OpenMode.ForRead);

                        if (ent is BlockReference)
                        {
                            BlockReference blkRef = (BlockReference)ent;

                            // XCLIP 확인 및 처리
                            if (HasXClip(blkRef))
                            {
                                ed.WriteMessage($"\n블록 '{blkRef.Name}'에 XCLIP이 적용되어 있습니다.");

                                // XCLIP 경계 가져오기
                                Point2dCollection clipBoundary = GetXClipBoundary(blkRef);

                                if (clipBoundary != null && clipBoundary.Count > 0)
                                {
                                    // 클리핑된 형상만 생성
                                    List<Entity> clippedEntities = CreateClippedGeometry(blkRef, clipBoundary, tr);
                                    newEntities.AddRange(clippedEntities);

                                    // 원본 블록 참조를 삭제 목록에 추가
                                    objectsToDelete.Add(blkRef.ObjectId);
                                }
                            }
                            else
                            {
                                ed.WriteMessage($"\n블록 '{blkRef.Name}'에 XCLIP이 적용되지 않았습니다.");
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\n선택한 객체가 블록 참조가 아닙니다.");
                        }
                    }

                    // 새로운 엔티티 추가
                    if (newEntities.Count > 0)
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        foreach (Entity newEnt in newEntities)
                        {
                            btr.AppendEntity(newEnt);
                            tr.AddNewlyCreatedDBObject(newEnt, true);
                        }
                    }

                    // 원본 객체 삭제
                    foreach (ObjectId id in objectsToDelete)
                    {
                        Entity entToDelete = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        entToDelete.Erase();
                    }

                    tr.Commit();

                    ed.WriteMessage($"\n{objectsToDelete.Count}개의 원본 객체를 삭제하고 {newEntities.Count}개의 클리핑된 객체를 생성했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 블록 참조에 XCLIP이 적용되었는지 확인
        /// </summary>
        private bool HasXClip(BlockReference blkRef)
        {
            // Extension Dictionary 확인
            if (blkRef.ExtensionDictionary.IsNull)
                return false;

            using (Transaction tr = blkRef.Database.TransactionManager.StartTransaction())
            {
                DBDictionary extDict = tr.GetObject(blkRef.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;

                // ACAD_FILTER dictionary 확인
                if (extDict.Contains("ACAD_FILTER"))
                {
                    ObjectId filterId = extDict.GetAt("ACAD_FILTER");
                    DBDictionary filterDict = tr.GetObject(filterId, OpenMode.ForRead) as DBDictionary;

                    // SPATIAL filter 확인
                    if (filterDict.Contains("SPATIAL"))
                    {
                        return true;
                    }
                }

                tr.Commit();
            }

            return false;
        }

        /// <summary>
        /// XCLIP 경계 좌표 가져오기
        /// </summary>
        private Point2dCollection GetXClipBoundary(BlockReference blkRef)
        {
            Point2dCollection boundary = new Point2dCollection();

            if (blkRef.ExtensionDictionary.IsNull)
                return boundary;

            using (Transaction tr = blkRef.Database.TransactionManager.StartTransaction())
            {
                DBDictionary extDict = tr.GetObject(blkRef.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;

                if (extDict.Contains("ACAD_FILTER"))
                {
                    ObjectId filterId = extDict.GetAt("ACAD_FILTER");
                    DBDictionary filterDict = tr.GetObject(filterId, OpenMode.ForRead) as DBDictionary;

                    if (filterDict.Contains("SPATIAL"))
                    {
                        ObjectId spatialId = filterDict.GetAt("SPATIAL");
                        SpatialFilter spatialFilter = tr.GetObject(spatialId, OpenMode.ForRead) as SpatialFilter;

                        // 클리핑 경계 점들 가져오기
                        Point2dCollection clipPoints = spatialFilter.Definition.GetPoints();

                        // 블록 참조의 변환 매트릭스 적용
                        Matrix3d transform = blkRef.BlockTransform;

                        foreach (Point2d pt2d in clipPoints)
                        {
                            Point3d pt3d = new Point3d(pt2d.X, pt2d.Y, 0);
                            pt3d = pt3d.TransformBy(transform);
                            boundary.Add(new Point2d(pt3d.X, pt3d.Y));
                        }
                    }
                }

                tr.Commit();
            }

            return boundary;
        }

        /// <summary>
        /// 클리핑된 형상 생성
        /// </summary>
        private List<Entity> CreateClippedGeometry(BlockReference blkRef, Point2dCollection clipBoundary, Transaction tr)
        {
            List<Entity> clippedEntities = new List<Entity>();

            // 블록 정의 가져오기
            BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

            // 클리핑 경계를 Polyline으로 변환
            Polyline clipPolyline = new Polyline();
            for (int i = 0; i < clipBoundary.Count; i++)
            {
                clipPolyline.AddVertexAt(i, clipBoundary[i], 0, 0, 0);
            }
            clipPolyline.Closed = true;

            // 블록 내의 각 엔티티 처리
            foreach (ObjectId entId in btr)
            {
                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                // 엔티티 복사 및 변환
                Entity clonedEnt = ent.Clone() as Entity;
                clonedEnt.TransformBy(blkRef.BlockTransform);

                // 클리핑 경계 내부에 있는지 확인
                if (IsEntityInsideClipBoundary(clonedEnt, clipPolyline))
                {
                    // 색상 및 레이어 설정
                    clonedEnt.Color = blkRef.Color;
                    clonedEnt.Layer = blkRef.Layer;
                    clonedEnt.Linetype = blkRef.Linetype;
                    clonedEnt.LinetypeScale = blkRef.LinetypeScale;

                    clippedEntities.Add(clonedEnt);
                }
                else
                {
                    // 경계와 교차하는 경우 트림 처리
                    Entity trimmedEnt = TrimEntityByBoundary(clonedEnt, clipPolyline);
                    if (trimmedEnt != null)
                    {
                        trimmedEnt.Color = blkRef.Color;
                        trimmedEnt.Layer = blkRef.Layer;
                        trimmedEnt.Linetype = blkRef.Linetype;
                        trimmedEnt.LinetypeScale = blkRef.LinetypeScale;

                        clippedEntities.Add(trimmedEnt);
                    }
                }
            }

            clipPolyline.Dispose();

            return clippedEntities;
        }

        /// <summary>
        /// 엔티티가 클리핑 경계 내부에 있는지 확인
        /// </summary>
        private bool IsEntityInsideClipBoundary(Entity ent, Polyline boundary)
        {
            // 엔티티의 경계 상자 가져오기
            Extents3d? extents = ent.Bounds;

            if (extents.HasValue)
            {
                Point3d minPt = extents.Value.MinPoint;
                Point3d maxPt = extents.Value.MaxPoint;

                // 경계 상자의 네 모서리 점 확인
                Point3d[] corners = new Point3d[]
                {
                    minPt,
                    new Point3d(maxPt.X, minPt.Y, minPt.Z),
                    maxPt,
                    new Point3d(minPt.X, maxPt.Y, minPt.Z)
                };

                // 모든 모서리가 경계 내부에 있는지 확인
                foreach (Point3d pt in corners)
                {
                    if (!IsPointInsideBoundary(pt, boundary))
                        return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 점이 경계 내부에 있는지 확인
        /// </summary>
        private bool IsPointInsideBoundary(Point3d point, Polyline boundary)
        {
            // Ray casting 알고리즘 사용
            int crossings = 0;
            Point2d pt = new Point2d(point.X, point.Y);

            for (int i = 0; i < boundary.NumberOfVertices; i++)
            {
                Point2d p1 = boundary.GetPoint2dAt(i);
                Point2d p2 = boundary.GetPoint2dAt((i + 1) % boundary.NumberOfVertices);

                if (((p1.Y <= pt.Y) && (p2.Y > pt.Y)) || ((p1.Y > pt.Y) && (p2.Y <= pt.Y)))
                {
                    double vt = (pt.Y - p1.Y) / (p2.Y - p1.Y);
                    if (pt.X < p1.X + vt * (p2.X - p1.X))
                    {
                        crossings++;
                    }
                }
            }

            return (crossings % 2) == 1;
        }

        /// <summary>
        /// 엔티티를 경계로 트림
        /// </summary>
        private Entity TrimEntityByBoundary(Entity ent, Polyline boundary)
        {
            // 간단한 구현 - 실제로는 더 복잡한 트림 로직이 필요
            // Line의 경우만 처리하는 예제
            if (ent is Line)
            {
                Line line = ent as Line;

                // 시작점과 끝점이 경계 내부에 있는지 확인
                bool startInside = IsPointInsideBoundary(line.StartPoint, boundary);
                bool endInside = IsPointInsideBoundary(line.EndPoint, boundary);

                if (startInside && endInside)
                {
                    return line.Clone() as Entity;
                }
                else if (!startInside && !endInside)
                {
                    // 완전히 외부에 있음
                    return null;
                }
                else
                {
                    // 부분적으로 내부에 있음 - 교차점 찾기
                    // 여기서는 간단한 구현만 제공
                    return line.Clone() as Entity;
                }
            }

            // 다른 엔티티 타입의 경우 원본 반환
            return ent.Clone() as Entity;
        }
    }
}
