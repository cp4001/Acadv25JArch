using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class BoundaryClosedToPoly
    {
        static int _index = 1;

        [CommandMethod("TB")]
        public void TraceBoundary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Select a seed point for our boundary
                PromptPointResult ppr = ed.GetPoint("\nSelect internal point: ");
                if (ppr.Status != PromptStatus.OK)
                    return;

                // Get the objects making up our boundary
                DBObjectCollection objs = ed.TraceBoundary(ppr.Value, true);

                if (objs.Count > 0)
                {
                    ed.WriteMessage($"\n{objs.Count} boundary objects found.");

                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        try
                        {
                            // We'll add the objects to the model space
                            BlockTable bt = (BlockTable)tr.GetObject(
                                db.BlockTableId,
                                OpenMode.ForRead
                            );

                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(
                                bt[BlockTableRecord.ModelSpace],
                                OpenMode.ForWrite
                            );

                            // Find the largest boundary by calculating areas
                            Entity? largestBoundary = null;
                            double maxArea = 0.0;

                            foreach (DBObject obj in objs)
                            {
                                Entity? ent = obj as Entity;
                                if (ent != null)
                                {
                                    double area = CalculateEntityArea(ent);
                                    if (area > maxArea)
                                    {
                                        maxArea = area;
                                        largestBoundary = ent;
                                    }
                                }
                            }

                            // Add only the largest boundary object to the drawing
                            ObjectIdCollection ids = new ObjectIdCollection();

                            if (largestBoundary != null)
                            {
                                // Set our boundary objects to be of
                                // our auto-incremented colour index
                                largestBoundary.ColorIndex = _index;

                                // Set the lineweight of our object
                                largestBoundary.LineWeight = LineWeight.LineWeight050;

                                // Add the largest boundary object to the modelspace
                                ObjectId id = btr.AppendEntity(largestBoundary);
                                ids.Add(id);
                                tr.AddNewlyCreatedDBObject(largestBoundary, true);

                                // Create area text at the center of the boundary
                                CreateAreaText(tr, btr, largestBoundary, maxArea);

                                ed.WriteMessage($"\nLargest boundary created with area: {maxArea:F2}");
                            }

                            // Dispose other boundary objects that weren't added
                            foreach (DBObject obj in objs)
                            {
                                Entity? ent = obj as Entity;
                                if (ent != null && ent != largestBoundary)
                                {
                                    ent.Dispose();
                                }
                            }

                            // Increment our colour index (reset if exceeds 255)
                            _index++;
                            if (_index > 255)
                                _index = 1;

                            // Commit the transaction
                            tr.Commit();

                            ed.WriteMessage($"\nBoundary created with color index {_index - 1} and area text.");
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nError during transaction: {ex.Message}");
                            tr.Abort();
                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nNo boundary found at the specified point.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in TraceBoundary command: {ex.Message}");
            }
        }

        /// <summary>
        /// Create area text at the center of the boundary entity
        /// </summary>
        /// <param name="tr">Current transaction</param>
        /// <param name="btr">Block table record (model space)</param>
        /// <param name="entity">The boundary entity</param>
        /// <param name="area">The area value</param>
        private void CreateAreaText(Transaction tr, BlockTableRecord btr, Entity entity, double area)
        {
            try
            {
                // Calculate the center point of the entity
                Point3d centerPoint = CalculateEntityCenter(entity);

                // Create area text with m² unit
                area = area/1000000.0; // Convert from mm² to m²
                string areaText = $"{area:F2} m²";

                // Calculate text height as 1/4 of the Y extent of the entity
                double textHeight = CalculateTextHeight(entity);

                // Create DBText object
                using (DBText text = new DBText())
                {
                    text.TextString = areaText;
                    text.Height = textHeight;
                    text.Position = centerPoint;

                    // Set text alignment to middle center
                    text.HorizontalMode = TextHorizontalMode.TextCenter;
                    text.VerticalMode = TextVerticalMode.TextVerticalMid;
                    text.AlignmentPoint = centerPoint;

                    // Set text color to match the boundary
                    text.ColorIndex = entity.ColorIndex;

                    // Add text to model space
                    ObjectId textId = btr.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }
            }
            catch (System.Exception ex)
            {
                // Log error but don't stop the main operation
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage($"\nError creating area text: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate text height based on entity's Y extent (1/4 of Y difference)
        /// </summary>
        /// <param name="entity">The entity to calculate text height for</param>
        /// <returns>The calculated text height</returns>
        private double CalculateTextHeight(Entity entity)
        {
            try
            {
                // Get the geometric extents of the entity
                Extents3d extents = entity.GeometricExtents;

                // Calculate Y extent difference
                double yExtent = extents.MaxPoint.Y - extents.MinPoint.Y;

                // Text height is 1/4 of Y extent, with minimum of 0.1 units
                double textHeight = Math.Max(yExtent / 12.0, 10);

                return textHeight;
            }
            catch
            {
                // If geometric extents fail, use alternative calculation
                return CalculateAlternativeTextHeight(entity);
            }
        }

        /// <summary>
        /// Alternative method to calculate text height for entities where GeometricExtents might fail
        /// </summary>
        /// <param name="entity">The entity to calculate text height for</param>
        /// <returns>The calculated text height</returns>
        private double CalculateAlternativeTextHeight(Entity entity)
        {
            try
            {
                // For Circle objects, use diameter / 4
                if (entity is Circle circle)
                {
                    return Math.Max(circle.Radius * 2.0 / 4.0, 0.1);
                }

                // For Polyline objects, try to calculate Y extent from vertices
                if (entity is Polyline pline)
                {
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;

                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point3d vertex = pline.GetPoint3dAt(i);
                        minY = Math.Min(minY, vertex.Y);
                        maxY = Math.Max(maxY, vertex.Y);
                    }

                    if (minY != double.MaxValue && maxY != double.MinValue)
                    {
                        double yExtent = maxY - minY;
                        return Math.Max(yExtent / 4.0, 0.1);
                    }
                }

                // Default fallback height
                return 1.0;
            }
            catch
            {
                // Final fallback
                return 1.0;
            }
        }

        /// <summary>
        /// Calculate the center point of an entity
        /// </summary>
        /// <param name="entity">The entity to calculate center for</param>
        /// <returns>The center point of the entity</returns>
        private Point3d CalculateEntityCenter(Entity entity)
        {
            try
            {
                // Get the geometric extents of the entity
                Extents3d extents = entity.GeometricExtents;

                // Calculate center point from min and max points
                Point3d minPoint = extents.MinPoint;
                Point3d maxPoint = extents.MaxPoint;

                Point3d centerPoint = new Point3d(
                    (minPoint.X + maxPoint.X) / 2.0,
                    (minPoint.Y + maxPoint.Y) / 2.0,
                    (minPoint.Z + maxPoint.Z) / 2.0
                );

                return centerPoint;
            }
            catch
            {
                // If geometric extents fail, try alternative methods
                return CalculateAlternativeCenter(entity);
            }
        }

        /// <summary>
        /// Alternative method to calculate center for entities where GeometricExtents might fail
        /// </summary>
        /// <param name="entity">The entity to calculate center for</param>
        /// <returns>The center point of the entity</returns>
        private Point3d CalculateAlternativeCenter(Entity entity)
        {
            try
            {
                // For Polyline objects, calculate centroid
                if (entity is Polyline pline)
                {
                    return CalculatePolylineCentroid(pline);
                }

                // For Circle objects, center is obvious
                if (entity is Circle circle)
                {
                    return circle.Center;
                }

                // For other entities, try to use area-based centroid calculation
                // This is a fallback method
                return new Point3d(0, 0, 0); // Default origin if all else fails
            }
            catch
            {
                return new Point3d(0, 0, 0); // Default origin if all else fails
            }
        }

        /// <summary>
        /// Calculate centroid of a polyline using vertex averaging
        /// </summary>
        /// <param name="pline">The polyline</param>
        /// <returns>The centroid point</returns>
        private Point3d CalculatePolylineCentroid(Polyline pline)
        {
            try
            {
                double sumX = 0.0, sumY = 0.0, sumZ = 0.0;
                int count = 0;

                // Average all vertices
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    Point3d vertex = pline.GetPoint3dAt(i);
                    sumX += vertex.X;
                    sumY += vertex.Y;
                    sumZ += vertex.Z;
                    count++;
                }

                if (count > 0)
                {
                    return new Point3d(sumX / count, sumY / count, sumZ / count);
                }

                return new Point3d(0, 0, 0);
            }
            catch
            {
                return new Point3d(0, 0, 0);
            }
        }

        /// <summary>
        /// Calculate the area of an entity (works for Polyline, Circle, Region, etc.)
        /// </summary>
        /// <param name="entity">The entity to calculate area for</param>
        /// <returns>The area of the entity, or 0 if area cannot be calculated</returns>
        private double CalculateEntityArea(Entity entity)
        {
            try
            {
                // For Polyline objects
                if (entity is Polyline pline)
                {
                    if (pline.Closed)
                    {
                        return pline.Area;
                    }
                    else
                    {
                        // If polyline is not closed, try to get area anyway
                        return Math.Abs(pline.Area);
                    }
                }

                // For Circle objects
                if (entity is Circle circle)
                {
                    return Math.PI * circle.Radius * circle.Radius;
                }

                // For Region objects
                if (entity is Autodesk.AutoCAD.DatabaseServices.Region region)
                {
                    return region.Area;
                }

                // For Hatch objects
                if (entity is Hatch hatch)
                {
                    return hatch.Area;
                }

                // For 2D Polyline
                if (entity is Polyline2d pline2d)
                {
                    return Math.Abs(pline2d.Area);
                }

                // For entities that have Area property through reflection
                var areaProperty = entity.GetType().GetProperty("Area");
                if (areaProperty != null)
                {
                    var areaValue = areaProperty.GetValue(entity);
                    if (areaValue is double area)
                    {
                        return Math.Abs(area);
                    }
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }




    }


    public class RoomPolyProcessor
    {
        static int _index = 1;

        /// <summary>
        /// Main function to process room polygon at specified point
        /// </summary>
        /// <param name="seedPoint">Point3d location to trace boundary</param>
        /// <param name="tr">Active transaction</param>
        /// <returns>True if successful, False if failed</returns>
        public static bool ProcessRoomPoly(Point3d seedPoint, Transaction tr)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Get the objects making up our boundary using TraceBoundary
                DBObjectCollection objs = ed.TraceBoundary(seedPoint, true);

                if (objs.Count == 0)
                {
                    ed.WriteMessage($"\nNo boundary found at point: {seedPoint.X:F2}, {seedPoint.Y:F2}");
                    return false;
                }

                ed.WriteMessage($"\n{objs.Count} boundary objects found.");

                // Get model space for adding entities
                BlockTable bt = (BlockTable)tr.GetObject(
                    db.BlockTableId,
                    OpenMode.ForRead
                );

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite
                );

                // Find the largest boundary by calculating areas
                Entity? largestBoundary = null;
                double maxArea = 0.0;

                foreach (DBObject obj in objs)
                {
                    Entity? ent = obj as Entity;
                    if (ent != null)
                    {
                        double area = CalculateEntityArea(ent);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            largestBoundary = ent;
                        }
                    }
                }

                // Process only the largest boundary object
                if (largestBoundary != null && maxArea > 0)
                {
                    // Set boundary object properties
                    largestBoundary.ColorIndex = _index;
                    largestBoundary.LineWeight = LineWeight.LineWeight050;

                    // Add the largest boundary object to the modelspace
                    ObjectId id = btr.AppendEntity(largestBoundary);
                    tr.AddNewlyCreatedDBObject(largestBoundary, true);

                    // Create area text at the center of the boundary
                    bool textCreated = CreateAreaText(tr, btr, largestBoundary, maxArea);

                    ed.WriteMessage($"\nRoom polygon created with area: {maxArea:F2} mm²");
                    if (textCreated)
                    {
                        ed.WriteMessage($" (Area text added)");
                    }

                    // Increment color index (reset if exceeds 255)
                    _index++;
                    if (_index > 255)
                        _index = 1;
                }
                else
                {
                    ed.WriteMessage("\nNo valid boundary area calculated.");
                    return false;
                }

                // Dispose other boundary objects that weren't added
                foreach (DBObject obj in objs)
                {
                    Entity? ent = obj as Entity;
                    if (ent != null && ent != largestBoundary)
                    {
                        ent.Dispose();
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in ProcessRoomPoly: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// TB1 Command - Process room polygon at user-selected point
        /// </summary>
        [CommandMethod("TB1")]
        public void Cmd_TraceRoomBoundary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Prompt user to select a point
                PromptPointOptions ppo = new PromptPointOptions("\nSelect internal point for room boundary: ");
                ppo.AllowNone = false;

                PromptPointResult ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                Point3d selectedPoint = ppr.Value;
                ed.WriteMessage($"\nProcessing boundary at point: {selectedPoint.X:F2}, {selectedPoint.Y:F2}");

                // Start transaction and process the room polygon
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    try
                    {
                        bool success = ProcessRoomPoly(selectedPoint, tr);

                        if (success)
                        {
                            tr.Commit();
                            ed.WriteMessage($"\nRoom boundary processing completed successfully with color index {_index - 1}.");
                        }
                        else
                        {
                            tr.Abort();
                            ed.WriteMessage("\nRoom boundary processing failed.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nError during transaction: {ex.Message}");
                        tr.Abort();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in TB1 command: {ex.Message}");
            }
        }

        // <summary>
        /// 도면의 모든 Text를 자동으로 선택하는 명령
        /// </summary>
        [CommandMethod("TB_rr")]
        public void Cmd_TraceRoomBoundary_From_RoomText()
        {
            try
            {
                // Step 1: 현재 문서의 Editor를 가져옴
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;

                // Step 2: Text 필터 생성
                SelectionFilter textFilter = JSelFilter.MakeFilterTypesRegs("TEXT","Room");

                // Step 3: 모든 Text를 자동 선택 (사용자 입력 없이)
                PromptSelectionResult selectionResult = ed.SelectAll(textFilter);

                // Step 4: 선택 결과 처리
                if (selectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectedObjects = selectionResult.Value;

                    // 선택된 Text 엔티티 개수를 출력
                    ed.WriteMessage($"\n도면에서 발견된 Text 엔티티 개수: {selectedObjects.Count}개");

                    // 각 Text 엔티티의 정보를 출력 (선택사항)
                    using (Transaction trans = doc.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId objId in selectedObjects.GetObjectIds())
                        {
                            if (trans.GetObject(objId, OpenMode.ForRead) is DBText textEntity)
                            {
                                ed.WriteMessage($"\nText 내용: \"{textEntity.TextString}\"");
                                //Process Room Poly
                                try
                                {
                                    bool success = ProcessRoomPoly(textEntity.Position, trans);

                                    if (success)
                                    {
                                        //trans.Commit();
                                        ed.WriteMessage($"\nRoom boundary processing completed successfully with color index {_index - 1}.");
                                    }
                                    else
                                    {
                                        //trans.Abort();
                                        ed.WriteMessage("\nRoom boundary processing failed.");
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    ed.WriteMessage($"\nError during transaction: {ex.Message}");
                                    //trans.Abort();
                                }


                            }
                            ed.WriteMessage("\n");
                        }
                        trans.Commit();
                    }

                    // 메모리 정리
                    selectedObjects.Dispose();
                }
                else
                {
                    ed.WriteMessage("\n도면에 Text 엔티티가 없습니다.");
                }
            }
            catch (System.Exception ex)
            {
                // 예외 처리
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor editor = doc.Editor;
                editor.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }


        /// <summary>
        /// Create area text at the center of the boundary entity
        /// </summary>
        /// <param name="tr">Current transaction</param>
        /// <param name="btr">Block table record (model space)</param>
        /// <param name="entity">The boundary entity</param>
        /// <param name="area">The area value in mm²</param>
        /// <returns>True if text created successfully</returns>
        /// 
        private static bool CreateAreaText(Transaction tr, BlockTableRecord btr, Entity entity, double area)
        {
            try
            {
                // Calculate the center point of the entity
                Point3d centerPoint = CalculateEntityCenter(entity);

                // Convert area from mm² to m²
                double areaInM2 = area / 1000000.0;
                string areaText = $"{areaInM2:F2} m²";

                // Calculate text height
                double textHeight = CalculateTextHeight(entity);

                // Create DBText object
                using (DBText text = new DBText())
                {
                    text.TextString = areaText;
                    text.Height = textHeight;
                    text.Position = centerPoint;

                    // Set text alignment to middle center
                    text.HorizontalMode = TextHorizontalMode.TextCenter;
                    text.VerticalMode = TextVerticalMode.TextVerticalMid;
                    text.AlignmentPoint = centerPoint;

                    // Set text color to match the boundary
                    text.ColorIndex = entity.ColorIndex;

                    // Add text to model space
                    ObjectId textId = btr.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage($"\nError creating area text: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculate text height based on entity's dimensions
        /// </summary>
        /// <param name="entity">The entity to calculate text height for</param>
        /// <returns>The calculated text height</returns>
        private static double CalculateTextHeight(Entity entity)
        {
            try
            {
                // Get the geometric extents of the entity
                Extents3d extents = entity.GeometricExtents;

                // Calculate Y extent difference
                double yExtent = extents.MaxPoint.Y - extents.MinPoint.Y;

                // Text height is 1/8 of Y extent, with minimum of 10 units
                double textHeight = Math.Max(yExtent / 12.0, 10.0);

                return textHeight;
            }
            catch
            {
                // If geometric extents fail, use alternative calculation
                return CalculateAlternativeTextHeight(entity);
            }
        }

        /// <summary>
        /// Alternative method to calculate text height
        /// </summary>
        /// <param name="entity">The entity to calculate text height for</param>
        /// <returns>The calculated text height</returns>
        private static double CalculateAlternativeTextHeight(Entity entity)
        {
            try
            {
                // For Circle objects, use diameter / 4
                if (entity is Circle circle)
                {
                    return Math.Max(circle.Radius * 2.0 / 4.0, 10.0);
                }

                // For Polyline objects, try to calculate Y extent from vertices
                if (entity is Polyline pline)
                {
                    double minY = double.MaxValue;
                    double maxY = double.MinValue;

                    for (int i = 0; i < pline.NumberOfVertices; i++)
                    {
                        Point3d vertex = pline.GetPoint3dAt(i);
                        minY = Math.Min(minY, vertex.Y);
                        maxY = Math.Max(maxY, vertex.Y);
                    }

                    if (minY != double.MaxValue && maxY != double.MinValue)
                    {
                        double yExtent = maxY - minY;
                        return Math.Max(yExtent / 4.0, 10.0);
                    }
                }

                // Default fallback height
                return 100.0;
            }
            catch
            {
                // Final fallback
                return 100.0;
            }
        }

        /// <summary>
        /// Calculate the center point of an entity
        /// </summary>
        /// <param name="entity">The entity to calculate center for</param>
        /// <returns>The center point of the entity</returns>
        private static Point3d CalculateEntityCenter(Entity entity)
        {
            try
            {
                // Get the geometric extents of the entity
                Extents3d extents = entity.GeometricExtents;

                // Calculate center point from min and max points
                Point3d minPoint = extents.MinPoint;
                Point3d maxPoint = extents.MaxPoint;

                Point3d centerPoint = new Point3d(
                    (minPoint.X + maxPoint.X) / 2.0,
                    (minPoint.Y + maxPoint.Y) / 2.0,
                    (minPoint.Z + maxPoint.Z) / 2.0
                );

                return centerPoint;
            }
            catch
            {
                // If geometric extents fail, try alternative methods
                return CalculateAlternativeCenter(entity);
            }
        }

        /// <summary>
        /// Alternative method to calculate center for entities
        /// </summary>
        /// <param name="entity">The entity to calculate center for</param>
        /// <returns>The center point of the entity</returns>
        private static Point3d CalculateAlternativeCenter(Entity entity)
        {
            try
            {
                // For Polyline objects, calculate centroid
                if (entity is Polyline pline)
                {
                    return CalculatePolylineCentroid(pline);
                }

                // For Circle objects, center is obvious
                if (entity is Circle circle)
                {
                    return circle.Center;
                }

                // Default origin if all else fails
                return new Point3d(0, 0, 0);
            }
            catch
            {
                return new Point3d(0, 0, 0);
            }
        }

        /// <summary>
        /// Calculate centroid of a polyline using vertex averaging
        /// </summary>
        /// <param name="pline">The polyline</param>
        /// <returns>The centroid point</returns>
        private static Point3d CalculatePolylineCentroid(Polyline pline)
        {
            try
            {
                double sumX = 0.0, sumY = 0.0, sumZ = 0.0;
                int count = 0;

                // Average all vertices
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    Point3d vertex = pline.GetPoint3dAt(i);
                    sumX += vertex.X;
                    sumY += vertex.Y;
                    sumZ += vertex.Z;
                    count++;
                }

                if (count > 0)
                {
                    return new Point3d(sumX / count, sumY / count, sumZ / count);
                }

                return new Point3d(0, 0, 0);
            }
            catch
            {
                return new Point3d(0, 0, 0);
            }
        }

        /// <summary>
        /// Calculate the area of an entity
        /// </summary>
        /// <param name="entity">The entity to calculate area for</param>
        /// <returns>The area of the entity in mm², or 0 if area cannot be calculated</returns>
        private static double CalculateEntityArea(Entity entity)
        {
            try
            {
                // For Polyline objects
                if (entity is Polyline pline)
                {
                    return Math.Abs(pline.Area);
                }

                // For Circle objects
                if (entity is Circle circle)
                {
                    return Math.PI * circle.Radius * circle.Radius;
                }

                // For Region objects
                if (entity is Autodesk.AutoCAD.DatabaseServices.Region region)
                {
                    return Math.Abs(region.Area);
                }

                // For Hatch objects
                if (entity is Hatch hatch)
                {
                    return Math.Abs(hatch.Area);
                }

                // For 2D Polyline
                if (entity is Polyline2d pline2d)
                {
                    return Math.Abs(pline2d.Area);
                }

                // Try to get Area property through reflection for other entity types
                var areaProperty = entity.GetType().GetProperty("Area");
                if (areaProperty != null)
                {
                    var areaValue = areaProperty.GetValue(entity);
                    if (areaValue is double area)
                    {
                        return Math.Abs(area);
                    }
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }
    }



    //
    public class EntityCloneCommands
    {
        private const string REGAPP_NAME = "Handle";
        private const string TARGET_LAYER = "!AreaCalc";

        [CommandMethod("CloneToAreaCalc")]
        public void Cmd_CloneToAreaCalc()
        {
            // Step 1: 현재 문서와 데이터베이스 가져오기
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 2: Line과 Polyline만 선택할 수 있는 SelectionFilter 생성
                TypedValue[] filterValues = new TypedValue[]
                {
                    //new TypedValue((int)DxfCode.Operator, "<OR"),
                    new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE,INSERT,TEXT"),
                    //new TypedValue((int)DxfCode.Start, "POLYLINE"),
                    //new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                    //new TypedValue((int)DxfCode.Operator, "OR>")
                };

                SelectionFilter filter = new SelectionFilter(filterValues);

                // Step 3: 사용자에게 Entity 선택 요청
                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "참조할 레이어의 Entity 을 선택하세요: ";
                selOpts.AllowDuplicates = false;
                selOpts.SingleOnly = true; // 하나만 선택

                PromptSelectionResult selResult = ed.GetSelection(selOpts, filter);

                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n선택이 취소되었거나 유효하지 않습니다.");
                    return;
                }

                // Step 4: 트랜잭션 시작
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // Step 5: RegApp 등록
                        RegisterApplication(tr, db, REGAPP_NAME);

                        // Step 6: 선택된 엔티티의 레이어 확인
                        ObjectId selectedId = selResult.Value.GetObjectIds()[0];
                        Entity? selectedEntity = tr.GetObject(selectedId, OpenMode.ForRead) as Entity;
                        string? sourceLayerName = selectedEntity?.Layer;

                        ed.WriteMessage($"\n'{sourceLayerName}' 레이어에서 Line/Polyline을 찾는 중...");

                        // Step 7: !AreaCalc 레이어 생성 또는 확인
                        ObjectId targetLayerId = CreateOrGetLayer(tr, db, TARGET_LAYER);

                        // Step 8: !AreaCalc 레이어에 이미 있는 엔티티들의 Handle 목록 수집
                        HashSet<string> existingHandles = GetExistingHandlesInTargetLayer(tr, db, targetLayerId);

                        // Step 9: 지정된 레이어의 모든 Line/Polyline 엔티티 찾기
                        List<ObjectId> sourceEntityIds = FindEntitiesInLayer(tr, db, sourceLayerName??"", filter);

                        ed.WriteMessage($"\n'{sourceLayerName}' 레이어에서 {sourceEntityIds.Count}개의 Line/Polyline/Block을 발견했습니다.");

                        // Step 10: 아직 복사되지 않은 엔티티만 필터링
                        List<ObjectId> entitiesToClone = new List<ObjectId>();
                        foreach (ObjectId entityId in sourceEntityIds)
                        {
                            Entity? entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                            string? handleValue = entity?.Handle.ToString();

                            if (!existingHandles.Contains(handleValue??""))
                            {
                                entitiesToClone.Add(entityId);
                            }
                        }

                        if (entitiesToClone.Count == 0)
                        {
                            ed.WriteMessage($"\n'{sourceLayerName}' 레이어의 모든 Line/Polyline이 이미 '{TARGET_LAYER}' 레이어에 복사되어 있습니다.");
                            tr.Commit();
                            return;
                        }

                        ed.WriteMessage($"\n{entitiesToClone.Count}개의 새로운 엔티티를 복사합니다...");

                        // Step 11: 선택된 엔티티들을 클론
                        CloneEntitiesWithXdata(tr, db, entitiesToClone, targetLayerId);

                        // Step 12: 트랜잭션 커밋
                        tr.Commit();
                        ed.WriteMessage($"\n{entitiesToClone.Count}개의 엔티티가 '{TARGET_LAYER}' 레이어에 성공적으로 복사되었습니다.");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n오류 발생: {ex.Message}");
                        tr.Abort();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n전체 프로세스 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 엔티티들을 클론하고 Xdata를 추가합니다.
        /// </summary>
        private void CloneEntitiesWithXdata(Transaction tr, Database db, List<ObjectId> entityIds, ObjectId targetLayerId)
        {
            if (entityIds.Count == 0) return;

            // 관련 레이어들의 잠금 상태를 추적하고 임시 해제
            Dictionary<ObjectId, bool> layerLockStates = new Dictionary<ObjectId, bool>();

            try
            {
                // Step 1: 관련 레이어들의 잠금 상태 확인 및 임시 해제
                UnlockRelatedLayers(tr, db, entityIds, targetLayerId, layerLockStates);

                // Step 2: ObjectIdCollection 생성
                ObjectIdCollection sourceIds = new ObjectIdCollection();
                foreach (ObjectId id in entityIds)
                {
                    sourceIds.Add(id);
                }

                // Step 3: ModelSpace 가져오기
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Step 4: DeepCloneObjects를 사용하여 클론 생성
                IdMapping idMap = new IdMapping();
                db.DeepCloneObjects(sourceIds, modelSpace.ObjectId, idMap, false);

                // Step 5: 클론된 각 엔티티에 Xdata 추가 및 레이어 변경
                foreach (IdPair idPair in idMap)
                {
                    if (idPair.IsCloned)
                    {
                        try
                        {
                            Entity clonedEntity = tr.GetObject(idPair.Value, OpenMode.ForWrite) as Entity;
                            Entity originalEntity = tr.GetObject(idPair.Key, OpenMode.ForRead) as Entity;

                            if (clonedEntity != null && originalEntity != null)
                            {
                                // 레이어 변경
                                clonedEntity.LayerId = targetLayerId;

                                // Xdata 추가 (원본 Handle 값)
                                string originalHandle = originalEntity.Handle.ToString();
                                AddXdataToEntity(clonedEntity, REGAPP_NAME, originalHandle);
                            }
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            // 개별 엔티티 처리 실패 시 로그만 남기고 계속 진행
                            Document doc = Application.DocumentManager.MdiActiveDocument;
                            Editor ed = doc.Editor;
                            ed.WriteMessage($"\n개별 엔티티 처리 실패 (Handle: {idPair.Key.Handle}): {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                // Step 6: 레이어 잠금 상태 복원
                RestoreLayerLockStates(tr, layerLockStates);
            }
        }

        /// <summary>
        /// 엔티티에 Xdata를 추가합니다.
        /// </summary>
        private void AddXdataToEntity(Entity entity, string appName, string handleValue)
        {
            using (ResultBuffer rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, handleValue)
            ))
            {
                entity.XData = rb;
            }
        }

        /// <summary>
        /// 대상 레이어에 이미 있는 엔티티들의 Handle 값들을 수집합니다.
        /// </summary>
        private HashSet<string> GetExistingHandlesInTargetLayer(Transaction tr, Database db, ObjectId layerId)
        {
            HashSet<string> handles = new HashSet<string>();

            // ModelSpace의 모든 엔티티를 순회
            BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

            foreach (ObjectId entityId in modelSpace)
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;

                // 대상 레이어의 엔티티인지 확인
                if (entity.LayerId == layerId)
                {
                    // Xdata에서 Handle 값 추출
                    using (ResultBuffer xdata = entity.GetXDataForApplication(REGAPP_NAME))
                    {
                        if (xdata != null)
                        {
                            TypedValue[] values = xdata.AsArray();
                            foreach (TypedValue tv in values)
                            {
                                if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                                {
                                    handles.Add(tv.Value.ToString());
                                    break; // 첫 번째 문자열 값만 사용
                                }
                            }
                        }
                    }
                }
            }

            return handles;
        }

        /// <summary>
        /// 지정된 레이어에서 Line/Polyline 엔티티들을 찾습니다.
        /// </summary>
        private List<ObjectId> FindEntitiesInLayer(Transaction tr, Database db, string layerName, SelectionFilter entityFilter)
        {
            List<ObjectId> entityIds = new List<ObjectId>();

            // 레이어와 엔티티 타입을 모두 필터링
            TypedValue[] combinedFilter = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Operator, "<AND"),
                new TypedValue((int)DxfCode.LayerName, layerName),
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "INSERT"),
                 new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Operator, "OR>"),
                new TypedValue((int)DxfCode.Operator, "AND>")
            };

            SelectionFilter filter = new SelectionFilter(combinedFilter);

            // Editor를 통해 전체 도면에서 선택
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptSelectionResult result = ed.SelectAll(filter);

            if (result.Status == PromptStatus.OK)
            {
                entityIds.AddRange(result.Value.GetObjectIds());
            }

            return entityIds;
        }

        /// <summary>
        /// RegApp을 등록합니다.
        /// </summary>
        private void RegisterApplication(Transaction tr, Database db, string appName)
        {
            RegAppTable regAppTable = tr.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;

            if (!regAppTable.Has(appName))
            {
                regAppTable.UpgradeOpen();

                RegAppTableRecord regAppRecord = new RegAppTableRecord();
                regAppRecord.Name = appName;

                regAppTable.Add(regAppRecord);
                tr.AddNewlyCreatedDBObject(regAppRecord, true);
            }
        }

        /// <summary>
        /// 레이어를 생성하거나 기존 레이어를 반환합니다.
        /// </summary>
        private ObjectId CreateOrGetLayer(Transaction tr, Database db, string layerName)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            // 레이어가 이미 존재하는지 확인
            if (lt.Has(layerName))
            {
                return lt[layerName];
            }
            else
            {
                // 새 레이어 생성
                lt.UpgradeOpen();

                LayerTableRecord ltr = new LayerTableRecord();
                ltr.Name = layerName;

                // 기본 색상 설정 (예: 빨간색)
                ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1);

                ObjectId layerId = lt.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);

                return layerId;
            }
        }

        /// <summary>
        /// 관련 레이어들의 잠금 상태를 임시로 해제합니다.
        /// </summary>
        private void UnlockRelatedLayers(Transaction tr, Database db, List<ObjectId> entityIds, ObjectId targetLayerId, Dictionary<ObjectId, bool> layerLockStates)
        {
            HashSet<ObjectId> layersToCheck = new HashSet<ObjectId>();

            // 대상 레이어 추가
            layersToCheck.Add(targetLayerId);

            // 원본 엔티티들의 레이어 추가
            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                layersToCheck.Add(entity.LayerId);
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 각 레이어의 잠금 상태 확인 및 임시 해제
            foreach (ObjectId layerId in layersToCheck)
            {
                LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                if (ltr != null)
                {
                    // 모든 레이어의 원래 상태를 저장 (잠긴 것과 잠기지 않은 것 모두)
                    bool originalLockState = ltr.IsLocked;
                    layerLockStates[layerId] = originalLockState;

                    if (originalLockState)
                    {
                        // 잠긴 레이어만 임시로 해제
                        ltr.IsLocked = false;
                        ed.WriteMessage($"\n레이어 '{ltr.Name}' 잠금을 임시 해제합니다. (원래 상태: 잠김)");
                    }
                    else
                    {
                        ed.WriteMessage($"\n레이어 '{ltr.Name}' 상태를 확인했습니다. (원래 상태: 잠기지 않음)");
                    }
                }
            }
        }

        /// <summary>
        /// 레이어들의 잠금 상태를 원래대로 복원합니다.
        /// </summary>
        private void RestoreLayerLockStates(Transaction tr, Dictionary<ObjectId, bool> layerLockStates)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            foreach (var kvp in layerLockStates)
            {
                try
                {
                    LayerTableRecord ltr = tr.GetObject(kvp.Key, OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        bool originalState = kvp.Value;
                        bool currentState = ltr.IsLocked;

                        // 원래 상태로 복원
                        ltr.IsLocked = originalState;

                        if (originalState != currentState)
                        {
                            string stateText = originalState ? "잠김" : "잠기지 않음";
                            ed.WriteMessage($"\n레이어 '{ltr.Name}' 상태를 '{stateText}'으로 복원했습니다.");
                        }
                        else
                        {
                            ed.WriteMessage($"\n레이어 '{ltr.Name}' 상태 변경 없음 (원래대로 유지).");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n레이어 잠금 상태 복원 실패: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// 도움말 명령어 - 사용법을 보여줍니다.
        /// </summary>
        [CommandMethod("CloneHelp")]
        public void ShowHelp()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n=== EntityCloneCommands 사용법 ===");
            ed.WriteMessage("\n명령어: CloneToAreaCalc");
            ed.WriteMessage("\n설명: 선택된 레이어의 모든 Line/Polyline을 !AreaCalc 레이어로 복사합니다.");
            ed.WriteMessage("\n- 중복 복사 방지: 이미 복사된 엔티티는 제외됩니다.");
            ed.WriteMessage("\n- Xdata 추가: 원본 엔티티의 Handle 정보를 저장합니다.");
            ed.WriteMessage("\n- 자동 레이어 생성: !AreaCalc 레이어가 없으면 자동 생성됩니다.");
            ed.WriteMessage("\n- 잠긴 레이어 처리: 잠긴 레이어는 임시로 해제 후 복원됩니다.");
            ed.WriteMessage("\n\n사용순서:");
            ed.WriteMessage("\n1. CloneToAreaCalc 명령어 실행");
            ed.WriteMessage("\n2. 참조할 레이어의 Line 또는 Polyline 선택");
            ed.WriteMessage("\n3. 해당 레이어의 모든 Line/Polyline이 !AreaCalc 레이어로 복사됩니다.");
            ed.WriteMessage("\n\n추가 명령어:");
            ed.WriteMessage("\n- CloneHelp: 이 도움말 표시");
            ed.WriteMessage("\n- CloneStatus: 복사 상태 확인");
        }

        /// <summary>
        /// 복사 상태를 확인하는 명령어
        /// </summary>
        [CommandMethod("CloneStatus")]
        public void ShowCloneStatus()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // !AreaCalc 레이어 확인
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(TARGET_LAYER))
                    {
                        ed.WriteMessage($"\n'{TARGET_LAYER}' 레이어가 존재하지 않습니다.");
                        tr.Commit();
                        return;
                    }

                    ObjectId targetLayerId = lt[TARGET_LAYER];
                    LayerTableRecord targetLayer = tr.GetObject(targetLayerId, OpenMode.ForRead) as LayerTableRecord;

                    ed.WriteMessage($"\n=== 복사 상태 정보 ===");
                    ed.WriteMessage($"\n대상 레이어: {TARGET_LAYER}");
                    ed.WriteMessage($"\n레이어 상태: {(targetLayer.IsLocked ? "잠김" : "해제")} | {(targetLayer.IsFrozen ? "동결" : "해제")}");

                    // !AreaCalc 레이어의 엔티티 개수 세기
                    int clonedCount = 0;
                    HashSet<string> uniqueHandles = new HashSet<string>();

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId entityId in modelSpace)
                    {
                        Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;

                        if (entity.LayerId == targetLayerId)
                        {
                            clonedCount++;

                            // Xdata에서 원본 Handle 확인
                            using (ResultBuffer xdata = entity.GetXDataForApplication(REGAPP_NAME))
                            {
                                if (xdata != null)
                                {
                                    TypedValue[] values = xdata.AsArray();
                                    foreach (TypedValue tv in values)
                                    {
                                        if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                                        {
                                            uniqueHandles.Add(tv.Value.ToString());
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    ed.WriteMessage($"\n복사된 엔티티 수: {clonedCount}개");
                    ed.WriteMessage($"\n고유 원본 엔티티 수: {uniqueHandles.Count}개");

                    if (clonedCount != uniqueHandles.Count)
                    {
                        ed.WriteMessage("\n⚠️ 중복된 복사본이 있을 수 있습니다.");
                    }

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n상태 확인 중 오류: {ex.Message}");
                    tr.Abort();
                }
            }
        }
    }
}
