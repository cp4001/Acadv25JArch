using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
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
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;

using CADExtension;

namespace Acadv25JArch
{
    class ArchZone
    {
        [CommandMethod("ceil_height")]
        public static void Cmd_SelectFullyInsideClosedPolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Define the path to the .lin file
            string linetypePath = @"C:\JElec\zone_linetype.lin";
            string linetypeName = "ZONE_LINETYPE";


            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

                // Check if the linetype is already loaded
                if (!ltt.Has(linetypeName))
                {
                    // Load the linetype from the .lin file
                    db.LoadLineTypeFile(linetypeName, linetypePath);
                    ed.WriteMessage($"\nLinetype '{linetypeName}' loaded successfully.");
                }
                else
                {
                    ed.WriteMessage($"\nLinetype '{linetypeName}' is already loaded.");
                }


                // Prompt the user to select a closed polyline
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect a closed polyline:");
                peo.SetRejectMessage("\nOnly closed polylines allowed.");
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                // Prompt for text input
                PromptStringOptions pso = new PromptStringOptions("\n 천정고  이름를 입력하세요: ");
                pso.AllowSpaces = false; // 공백 허용
                                         // Get the text input from user
                PromptResult pr = ed.GetString(pso);
                string zName = "";
                if (pr.Status == PromptStatus.OK)
                {
                    zName = pr.StringResult;
                }


                //사용할 XData 미리 Check
                tr.ChecRegNames(db, $"CeilingHeight");


                // Get the selected polyline and ensure it's closed
                Polyline polyline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                // Get the bounding box of the polyline
                Extents3d extents = polyline.GeometricExtents;
                double height = (extents.MaxPoint.Y - extents.MinPoint.Y) / 6;

                // Calculate the center point of the bounding box
                Point3d centerPoint = new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2);

                // Create a new text entity
                DBText text = new DBText
                {
                    Position = centerPoint,
                    Height = height,
                    TextString = zName,
                    HorizontalMode = TextHorizontalMode.TextCenter,
                    VerticalMode = TextVerticalMode.TextVerticalMid,
                    AlignmentPoint = centerPoint, // Aligns the text based on the center point
                    Transparency = new Transparency(80)  // Set transparency to 80%
                };

                // Add the text to the database
                BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                btr.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);


                if (!polyline.Closed)
                {
                    ed.WriteMessage("\nThe selected polyline must be closed.");
                    return;
                }
                polyline.UpgradeOpen();
                //polyline.Linetype = linetypeName;
                //JXdata.SetXdata(polyline, "ZoneName", zName);




                //// Collect the polyline vertices into a Point3dCollection
                //Point3dCollection points = new Point3dCollection();
                //for (int i = 0; i < polyline.NumberOfVertices; i++)
                //{
                //    points.Add(polyline.GetPoint3dAt(i));
                //}

                //// Define the selection filter for specific entity types
                //SelectionFilter filter = new SelectionFilter(new TypedValue[]
                //{
                //    new TypedValue((int)DxfCode.Start, "LINE,ARC,LWPOLYLINE,CIRCLE,INSERT")
                //});

                //// Use SelectWindowPolygon to select entities fully within the polyline
                //PromptSelectionResult psr = ed.SelectWindowPolygon(points, filter);

                //if (psr.Status == PromptStatus.OK)
                //{
                //    // Loop through the selected objects (no further filtering needed)
                //    foreach (SelectedObject selObj in psr.Value)
                //    {
                //        Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                //        if (ent != null)
                //        {
                //            ent.UpgradeOpen();
                //            ent.ZoneAdd(zName);
                //            //JXdata.SetXdata(ent, "Zone", zName);
                //        }
                //    }
                //}

                tr.Commit();
            }
        }
    }

}
