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

namespace Acadv25JArch
{
    public class Commands
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
                            Entity largestBoundary = null;
                            double maxArea = 0.0;

                            foreach (DBObject obj in objs)
                            {
                                Entity ent = obj as Entity;
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

                                ed.WriteMessage($"\nLargest boundary created with area: {maxArea:F2}");
                            }

                            // Dispose other boundary objects that weren't added
                            foreach (DBObject obj in objs)
                            {
                                Entity ent = obj as Entity;
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

                            ed.WriteMessage($"\nBoundary created with color index {_index - 1}.");
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
                if (entity is Region region)
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
}
