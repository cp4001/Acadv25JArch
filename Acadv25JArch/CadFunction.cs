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
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace AcadFunction
{
    public class JXdata
    {
        //Check XData Reg Name 
        public static void CheckXdataRegName(Transaction acTrans, string appName)
        {
            var acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            //Document acDoc = Application.DocumentManager.MdiActiveDocument;
            RegAppTable acRegAppTbl;
            acRegAppTbl = (RegAppTable)acTrans.GetObject(acCurDb.RegAppTableId, OpenMode.ForRead);

            // Check to see if the Registered Applications table record for the custom app exists
            if (acRegAppTbl.Has(appName) == false)
            {
                using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                {
                    acRegAppTblRec.Name = appName;

                    acRegAppTbl.UpgradeOpen();
                    acRegAppTbl.Add(acRegAppTblRec);
                    acTrans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                }
            }
        }

        //Object로 부터 XData string 값 불러오기 
        public static string? GetXdata(DBObject obj, string xName) // Xdata String 값  return
        {
            if (obj == null) return null;
            string? res = null;
            ResultBuffer rb = obj.XData;
            //if (rb == null) return null;
            if (rb != null)
            {
                bool foundStart = false;
                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName &&
                        tv.Value.ToString() == xName)
                        foundStart = true;
                    else
                    {
                        if (foundStart == true)
                        {
                            if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                            {
                                res = tv.Value.ToString();
                                break;
                            }

                        }
                    }
                }
                rb.Dispose();
            }
            else
            {
                return null;
            }
            return res;
        }
        //Object에  XData xName으로  string 값 저장
        public static void SetXdata(DBObject obj, string xName, string sdata)
        {
            if (obj == null) return;
            //if(obj.AcadObject != null) obj.UpgradeOpen();
            DateTime currentDate = DateTime.Now;
            DateTime targetDate = new DateTime(2026, 3, 1);
            bool isCurrentDateBeforeTarget = currentDate > targetDate;
            if (isCurrentDateBeforeTarget) return;
            //obj.UpgradeOpen();
            //AddRegAppTableRecord(xName);
            ResultBuffer rbt =
                new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, xName),    //(int)DxfCode.ExtendedDataRegAppName 1001
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, sdata)   //Down UpDown    // (int)DxfCode.ExtendedDataAsciiString 1000
                );
            obj.XData = rbt;
            rbt.Dispose();
        }

        #region AddRegAppTable
        //public static void AddRegAppTableRecord(string regAppName)
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    // Editor ed = doc.Editor;
        //    Database db = doc.Database;
        //    //Transaction tr = doc.TransactionManager.StartTransaction();
        //    //using (tr)
        //    //{
        //        RegAppTable rat = db.RegAppTableId.GetObject(OpenMode.ForRead) as RegAppTable;
        //           //(RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead, false);
        //        if (!rat.Has(regAppName))
        //        {
        //            rat.UpgradeOpen();
        //            RegAppTableRecord ratr =
        //              new RegAppTableRecord();
        //            ratr.Name = regAppName;
        //            rat.Add(ratr);
        //            tr.AddNewlyCreatedDBObject(ratr, true);
        //        }
        //    //    tr.Commit();
        //    //}
        //}
        #endregion

        //Object Ucs XData 저장
        //public static void SetUcsXdata(DBObject obj,Point3d org,Vector3d xDir, Vector3d yDir)
        //{
        //    obj.UpgradeOpen();
        //    AddRegAppTableRecord("Ucs");
        //    ResultBuffer rbt =
        //        new ResultBuffer(
        //            new TypedValue((int)DxfCode.ExtendedDataRegAppName, "Ucs"),    //(int)DxfCode.ExtendedDataRegAppName 1001
        //            new TypedValue((int)DxfCode.ExtendedDataAsciiString, org.ToString()),
        //            new TypedValue((int)DxfCode.ExtendedDataAsciiString, xDir.ToString()),
        //            new TypedValue((int)DxfCode.ExtendedDataAsciiString, yDir.ToString())

        //            //new TypedValue((int)DxfCode.UcsOrientationX,xDir),
        //            //new TypedValue((int)DxfCode.UcsOrientationX, yDir)
        //            );
        //    obj.XData = rbt;
        //    rbt.Dispose();
        //}


        public static void DelXdata(DBObject obj, string xName)    // delete Xdata 
        {
            ResultBuffer buffer = obj.GetXDataForApplication(xName);
            // This call would ensure that the
            //Xdata of the entity associated with ADSK application
            //name only would be removed
            if (buffer != null)
            {
                //ent.UpgradeOpen(); obj 가 Write 모드로 이기때문에 UpgradeOpen이 필요 없다.
                obj.XData = new ResultBuffer(new TypedValue(1001, xName));
                buffer.Dispose();
            }

        }

        public static void DeleteAll(PromptSelectionResult acSSPrompt)
        {
            // Get the current database and start a transaction

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //string appName = "Duct";
            //string xdataStr = "This is Main Pipe";


            //TypedValue[] acTypValAr = new TypedValue[1];
            //acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "LINE,ARC,TEXT,MTEXT,POLYLINE"), 0);
            ////acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, "Duct"), 1);
            ////acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, "Damper"), 2);
            ////acTypValAr.SetValue(new TypedValue((int)DxfCode.Operator, "or>"), 3);

            //// Assign the filter criteria to a SelectionFilter object
            //SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);

            //using (Transaction acTrans = db.TransactionManager.StartTransaction())
            //{
            // Request objects to be selected in the drawing area
            //-PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();
            //PromptSelectionResult acSSPrompt = doc.Editor.GetSelection(acSelFtr);


            // If the prompt status is OK, objects were selected
            //if (acSSPrompt.Status == PromptStatus.OK)
            //{
            // Define the Xdata to add to each selected object
            SelectionSet acSSet = acSSPrompt.Value;
            // Step through the objects in the selection set
            foreach (SelectedObject acSSObj in acSSet)
            {
                // Open the selected object for write
                //Entity ent = acTrans.GetObject(acSSObj.ObjectId,
                //                                    OpenMode.ForWrite) as Entity;
                Entity ent = acSSObj.ObjectId.GetObject(OpenMode.ForWrite) as Entity;

                if (ent.XData != null)
                {
                    IEnumerable<string> appNames = from TypedValue tv in ent.XData.AsArray() where tv.TypeCode == 1001 select tv.Value.ToString();

                    ent.UpgradeOpen();
                    foreach (string appName in appNames)
                    {
                        ent.XData = new ResultBuffer(new TypedValue(1001, appName));
                    }
                    ent.LineWeight = LineWeight.LineWeight020;

                    ed.WriteMessage("\nAll XData have been deleted.");
                }
                else
                    ed.WriteMessage("\nNo XData to delete.");

                //}

            }
            // Save the new object to the database
            //acTrans.Commit();

            // Dispose of the transaction
            //}
        }
        public static void DeleteAll(DBObject obj)
        {
            Entity ent = obj.ObjectId.GetObject(OpenMode.ForWrite) as Entity;

            if (ent.XData != null)
            {
                IEnumerable<string> appNames = from TypedValue tv in ent.XData.AsArray() where tv.TypeCode == 1001 select tv.Value.ToString();

                ent.UpgradeOpen();
                foreach (string appName in appNames)
                {
                    ent.XData = new ResultBuffer(new TypedValue(1001, appName));
                }
                //ent.LineWeight = LineWeight.LineWeight020;

                //ed.WriteMessage("\nAll XData have been deleted.");
            }
        }
        public static void DeleteAll(Entity ent)
        {
            //Entity ent = obj.ObjectId.GetObject(OpenMode.ForWrite) as Entity;

            if (ent.XData != null)
            {
                IEnumerable<string> appNames = from TypedValue tv in ent.XData.AsArray() where tv.TypeCode == 1001 select tv.Value.ToString();

                ent.UpgradeOpen();
                foreach (string appName in appNames)
                {
                    ent.XData = new ResultBuffer(new TypedValue(1001, appName));
                }
                //ent.LineWeight = LineWeight.LineWeight020;

                //ed.WriteMessage("\nAll XData have been deleted.");
            }
        }

        public static void CopyXdata(DBObject obj1, DBObject obj2)
        {
            Entity ent = obj1.ObjectId.GetObject(OpenMode.ForWrite) as Entity;
            //Entity ent2 = obj2.ObjectId.GetObject(OpenMode.ForWrite) as Entity;

            if (ent.XData != null)
            {
                IEnumerable<string> appNames = from TypedValue tv in ent.XData.AsArray() where tv.TypeCode == 1001 select tv.Value.ToString();

                ent.UpgradeOpen();
                foreach (string appName in appNames)
                {
                    SetXdata(obj2, appName, GetXdata(obj1, appName));
                    //ent2.XData = new ResultBuffer(new TypedValue(1001, appName)); dell xData
                }
                //ent.LineWeight = LineWeight.LineWeight020;

                //ed.WriteMessage("\nAll XData have been deleted.");
            }

        }

        #region Set XData with filter xReg 
        public static void Entity_To_Xdata(string Message, string appName, SelectionFilter acSelFtr)
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            //appName = "Group";
            string xdataStr = "";

            //TypedValue[] acTypValAr = new TypedValue[1];
            //acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, "Pipe"), 0);
            // Assign the filter criteria to a SelectionFilter object
            //SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                //-PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection();
                PromptSelectionResult acSSPrompt = acDoc.Editor.GetSelection(acSelFtr);


                // If the prompt status is OK, objects were selected
                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    //"\nPlease Enter the Group Name: "
                    PromptStringOptions options = new PromptStringOptions("\n" + Message);
                    PromptResult result = acDoc.Editor.GetString(options);
                    if (result.Status == PromptStatus.OK)
                    {
                        xdataStr = result.StringResult;

                    }

                    // Open the Registered Applications table for read
                    RegAppTable acRegAppTbl;
                    acRegAppTbl = acTrans.GetObject(acCurDb.RegAppTableId, OpenMode.ForRead) as RegAppTable;

                    // Check to see if the Registered Applications table record for the custom app exists
                    if (acRegAppTbl.Has(appName) == false)
                    {
                        using (RegAppTableRecord acRegAppTblRec = new RegAppTableRecord())
                        {
                            acRegAppTblRec.Name = appName;

                            acRegAppTbl.UpgradeOpen();
                            acRegAppTbl.Add(acRegAppTblRec);
                            acTrans.AddNewlyCreatedDBObject(acRegAppTblRec, true);
                        }
                    }

                    // Define the Xdata to add to each selected object
                    using (ResultBuffer rb = new ResultBuffer())
                    {
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));
                        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));

                        SelectionSet acSSet = acSSPrompt.Value;

                        // Step through the objects in the selection set
                        foreach (SelectedObject acSSObj in acSSet)
                        {
                            // Open the selected object for write
                            Entity acEnt = acTrans.GetObject(acSSObj.ObjectId,
                                                                OpenMode.ForWrite) as Entity;

                            // Append the extended data to each object
                            acEnt.XData = rb;
                        }
                    }
                }

                // Save the new object to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }


        #endregion

        // CAD Command 
        #region Delete Xdata
        // 선택 Entity Xdata  전부 삭제
        //TO DuctLine
        [CommandMethod("XD_DelALL", CommandFlags.UsePickSet)]
        //2Point PolyLine은 Line으로 변경 한다. 
        // Wire Line Extend는 Line에만 적용 되기때문
        public void Cmd_Entity_To_DeleteXdata()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Entity> targets = JEntityFunc.GetEntityByTpye<Entity>("Xdata Del 대상을 선택 하세요?", JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOLYLINE,INSERT"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;

                //var btr = tr.GetModelSpaceBlockTableRecord(db);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                // Step through the objects in the selection set
                Line sLine = new Line();
                foreach (Entity acEnt in targets)
                {
                    //Check layer locked layer entity
                    var layer = (LayerTableRecord)tr.GetObject(acEnt.LayerId, OpenMode.ForRead);
                    if (!layer.IsLocked)
                    {
                        JXdata.DeleteAll(acEnt);

                    }
                }

                // Save the new object to the database
                tr.Commit();

                // Dispose of the transaction
            }
        }
        #endregion



        #region List Entity  XData
        [CommandMethod("GXD")]
        static public void Cmd_ListXData()

        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            // Ask the user to select an entity
            // for which to retrieve XData
            PromptEntityOptions opt = new PromptEntityOptions("\nSelect entity: ");
            PromptEntityResult res = ed.GetEntity(opt);
            if (res.Status == PromptStatus.OK)
            {
                Transaction tr = doc.TransactionManager.StartTransaction();
                using (tr)
                {
                    DBObject obj = tr.GetObject(res.ObjectId, OpenMode.ForRead);
                    ResultBuffer rb = obj.XData;
                    if (rb == null)
                    {
                        ed.WriteMessage("\nEntity does not have XData attached.");
                    }
                    else
                    {
                        int n = 0;
                        foreach (TypedValue tv in rb)
                        {
                            ed.WriteMessage(
                             "\nTypedValue {0} - type: {1}, value: {2}",
                              n++,
                              tv.TypeCode,
                              tv.Value
                            );
                        }
                        rb.Dispose();
                        ed.WriteMessage("\nTotal of {0} Xdata.", n);
                        ed.WriteMessage("\n");
                    }
                }
            }
        }
        #endregion

    }
    public class JBlock
    {
        public static BlockTableRecord? GetBtrFromBr(BlockReference br)
        {
            BlockTableRecord? btr = new BlockTableRecord();
            if (br.IsDynamicBlock)
            {
                btr = br.DynamicBlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            }
            else
            {
                btr = br.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            }

            return btr;
        }

        [CommandMethod("BP1")]
        public void BlockPointResetCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n [ Block Control Point Reset ]");

            int oldOsmode = 0;  // Declare oldOsmode outside the try block

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    PromptEntityResult result = ed.GetEntity("\n 블럭 선택 : ");
                    if (result.Status != PromptStatus.OK)
                        return;

                    Entity? ent = tr.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;
                    if (!(ent is BlockReference))
                    {
                        ed.WriteMessage("\n선택한 객체가 블록 참조가 아닙니다.");
                        return;
                    }

                    BlockReference? blockRef = ent as BlockReference;
                    Point3d oldInsertionPoint = blockRef.Position;

                    oldOsmode = (int)Application.GetSystemVariable("OSMODE");  // Assign value here
                    Application.SetSystemVariable("OSMODE", 111);

                    PromptPointOptions ppo = new PromptPointOptions("\n 새 기준점 : ");
                    ppo.UseBasePoint = true;
                    ppo.BasePoint = oldInsertionPoint;
                    ppo.UseDashedLine = true;

                    PromptPointResult promptResult = ed.GetPoint(ppo);
                    if (promptResult.Status != PromptStatus.OK)
                        return;

                    Point3d newInsertionPoint = promptResult.Value;

                    Application.SetSystemVariable("OSMODE", 0);

                    double scale = blockRef.ScaleFactors.X;
                    double rotation = blockRef.Rotation;

                    Vector3d displacement = newInsertionPoint - oldInsertionPoint;
                    if (scale > 0)
                    {
                        displacement = displacement.RotateBy(-rotation, Vector3d.ZAxis) / scale;
                    }
                    else
                    {
                        displacement = displacement.RotateBy(rotation, Vector3d.ZAxis) / scale;
                    }

                    BlockTableRecord? btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    btr.Origin += displacement;

                    BlockTable? bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId brefId in bt!)
                    {
                        BlockTableRecord btrRef = tr.GetObject(brefId, OpenMode.ForWrite) as BlockTableRecord;
                        if (btrRef.Name == blockRef.Name)
                        {
                            foreach (ObjectId entId in btrRef)
                            {
                                Entity entInBlock = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                                entInBlock.TransformBy(Matrix3d.Displacement(displacement));
                            }
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage("\n블록 기준점이 성공적으로 재설정되었습니다.");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
                finally
                {
                    Application.SetSystemVariable("OSMODE", oldOsmode);  // Now accessible here
                }
            }
        }
    }


    //Selection Class
    public class SelSet // Selection Set 
    {
        //static Document doc = Application.DocumentManager.MdiActiveDocument;
        //Database db = doc.Database;
        //Editor ed = doc.Editor;

        public enum WinMode { Cross, Window }
        public static ObjectIdCollection SelectByLine(Line baseLine, WinMode mode, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Vector3d perpVec = (baseLine.EndPoint - baseLine.StartPoint).CrossProduct(baseLine.Normal).GetNormal();
            Vector3d offset = perpVec * 2;

            var points = new Point3dCollection();

            //Point3d pt1 = line.StartPoint + offset;
            Vector3d baseLineVec = (baseLine.EndPoint - baseLine.StartPoint).GetNormal();
            points.Add(baseLine.StartPoint - baseLineVec * 2 + offset);  //baseLine.GetPointAtDist(1)
            points.Add(baseLine.StartPoint - baseLineVec * 2 - offset);
            points.Add(baseLine.EndPoint + baseLineVec * 2 - offset); //GetPointAtParameter(baseLine.Length - 1)
            points.Add(baseLine.EndPoint + baseLineVec * 2 + offset);

            //PromptSelectionResult psr2 = ed.SelectCrossingWindow(p1, p2, sf);
            PromptSelectionResult psr2 = null;     // = new PromptSelectionResult();

            switch (mode)
            {
                case WinMode.Cross:
                    psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity
                    break;
                case WinMode.Window:
                    psr2 = ed.SelectWindowPolygon(points, sf);    //Polygon 내부에 포함된 모든 Entity 
                    break;
                default:
                    break;
            }


            //SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();

        }

        public static ObjectIdCollection SelectByLineInside(Line baseLine, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Vector3d perpVec = (baseLine.EndPoint - baseLine.StartPoint).CrossProduct(baseLine.Normal).GetNormal();
            Vector3d offset = perpVec * 1;

            var points = new Point3dCollection();

            //Point3d pt1 = line.StartPoint + offset;
            Vector3d baseLineVec = (baseLine.EndPoint - baseLine.StartPoint).GetNormal() * 2;
            points.Add(baseLine.EndPoint + baseLineVec + offset); //+ baseLineVec
            points.Add(baseLine.StartPoint - baseLineVec + offset);  //baseLine.GetPointAtDist(1) - baseLineVec
            points.Add(baseLine.StartPoint - baseLineVec - offset);  //- baseLineVec
            points.Add(baseLine.EndPoint + baseLineVec - offset); //+ baseLineVec


            //PromptSelectionResult psr2 = ed.SelectCrossingWindow(p1, p2, sf);  // = new PromptSelectionResult();
            PromptSelectionResult psr2 = ed.SelectWindowPolygon(points, sf);    //Polygon 내부에 포함된 모든 Entity 

            //SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();

        }

        public static ObjectIdCollection SelectBy2Points(Point3d P1, Point3d P2, SelectionFilter sf, WinMode mode = WinMode.Cross)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<Line> reslines = new List<Line>();
            Line baseLine = new Line();
            baseLine = new Line(P1, P2);

            // 선택된 라인중에 MainPipe는 제외한다.
            Vector3d perpVec = (baseLine.EndPoint - baseLine.StartPoint).CrossProduct(baseLine.Normal).GetNormal();
            Vector3d offset = perpVec * 2;
            //Point3d p1 = new Point3d(baseLine.StartPoint.X - 1.0, baseLine.StartPoint.Y - 1.0, 0.0);     // baseLine.StartPoint - offset;
            //Point3d p2 = new Point3d(baseLine.EndPoint.X + 1.0, baseLine.EndPoint.Y + 1.0, 0.0);//baseLine.StartPoint + offset;
            var points = new Point3dCollection();

            Point3d p11 = baseLine.GetPointAtDist(10);
            Point3d p1 = new Point3d(p11.X - 2.0, p11.Y - 2.0, 0.0);
            Point3d p22 = baseLine.GetPointAtDist(baseLine.Length - 10.0);
            Point3d p2 = new Point3d(p22.X + 2.0, p22.Y + 2.0, 0.0);

            //Point3d pt1 = line.StartPoint + offset;
            points.Add(baseLine.StartPoint + offset);
            points.Add(baseLine.StartPoint - offset);
            points.Add(baseLine.GetPointAtParameter(baseLine.Length) - offset);
            points.Add(baseLine.GetPointAtParameter(baseLine.Length) + offset);

            //PromptSelectionResult psr2 = ed.SelectCrossingWindow(p1, p2, sf);


            PromptSelectionResult psr2 = null;
            switch (mode)
            {
                case WinMode.Cross:
                    psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity
                    break;
                case WinMode.Window:
                    psr2 = ed.SelectWindowPolygon(points, sf);    //Polygon 내부에 포함된 모든 Entity 
                    break;
                default:
                    break;
            }


            //SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();



            //사용법 
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
            //}

            //ObjectIdCollection ids = new ObjectIdCollection(psr2.Value.GetObjectIds());
            //selLines =ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();
        }

        //For Block Seklection MOde 
        public static ObjectIdCollection SelectByMinMaxPoints(Point3d P1, Point3d P2, SelectionFilter sf, WinMode mode = WinMode.Cross)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //List<Line> reslines = new List<Line>();
            //Line baseLine = new Line();
            //baseLine = new Line(P1, P2);
            Double xOffSet = Math.Abs(P2.X - P1.X);
            Double yOffSet = Math.Abs(P2.Y - P1.Y);
            //// 선택된 라인중에 MainPipe는 제외한다.
            //Vector3d perpVec = (baseLine.EndPoint - baseLine.StartPoint).CrossProduct(baseLine.Normal).GetNormal();
            //Vector3d offset = perpVec * 2;
            //Point3d p1 = new Point3d(baseLine.StartPoint.X - 1.0, baseLine.StartPoint.Y - 1.0, 0.0);     // baseLine.StartPoint - offset;
            //Point3d p2 = new Point3d(baseLine.EndPoint.X + 1.0, baseLine.EndPoint.Y + 1.0, 0.0);//baseLine.StartPoint + offset;
            var points = new Point3dCollection();

            //Point3d p1 = P1;
            //Point3d p2 = new Point3d(P1.X, P1.Y + yOffSet, 0.0);
            //Point3d p3 = new Point3d(P1.X + xOffSet, P1.Y + yOffSet, 0.0);
            //Point3d p4 = new Point3d(P1.X + xOffSet, P1.Y, 0.0);

            // --

            Point3d p1 = new Point3d(P1.X + xOffSet, P1.Y, 0.0);
            Point3d p2 = P1;
            Point3d p3 = new Point3d(P1.X, P1.Y + yOffSet, 0.0);
            Point3d p4 = new Point3d(P1.X + xOffSet, P1.Y + yOffSet, 0.0);



            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);

            ////Point3d pt1 = line.StartPoint + offset;
            //points.Add(baseLine.StartPoint + offset);
            //points.Add(baseLine.StartPoint - offset);
            //points.Add(baseLine.GetPointAtParameter(baseLine.Length) - offset);
            //points.Add(baseLine.GetPointAtParameter(baseLine.Length) + offset);

            //PromptSelectionResult psr2 = ed.SelectCrossingWindow(p1, p2, sf);


            PromptSelectionResult psr2 = null;
            switch (mode)
            {
                case WinMode.Cross:
                    psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity
                    break;
                case WinMode.Window:
                    psr2 = ed.SelectWindowPolygon(points, sf);    //Polygon 내부에 포함된 모든 Entity 
                    break;
                default:
                    break;
            }


            //SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();



            //사용법 
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
            //}

            //ObjectIdCollection ids = new ObjectIdCollection(psr2.Value.GetObjectIds());
            //selLines =ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();
        }


        // Id Selectotion 
        //-- 
        public static ObjectIdCollection GetCrossWindowSelectIDs(Point3d P1, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            double offset = 4.0;

            points.Add(new Point3d(P1.X - offset, P1.Y - offset, 0.0));
            points.Add(new Point3d(P1.X - offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X + offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X + offset, P1.Y - offset, 0.0));



            //points.Add(new Point3d(P1.X + offset-1, P1.Y - offset, 0.0));

            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //-- 선택 개체의 End 또는 StartPoint 가 선택기준 P1 과 일처점 이어야 한다(거리 1 이내)
        public static ObjectIdCollection GetCrossWindowSelectIDs1(Point3d P1, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            double offset = 4.0;


            points.Add(new Point3d(P1.X - offset, P1.Y - offset, 0.0));
            points.Add(new Point3d(P1.X - offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X + offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X + offset, P1.Y - offset, 0.0));


            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value; if (ss == null) return null;
            ObjectIdCollection filterIdCol = new ObjectIdCollection();
            ObjectIdCollection filterIdCol1 = new ObjectIdCollection();
            // Check entity 개체가 끝점  연결이 아니면 제외  
            foreach (SelectedObject acSSObj in ss)
            {
                // Check to make sure a valid SelectedObject object was returned
                if (acSSObj != null)
                {
                    // Open the selected object for write
                    Entity acEnt = acSSObj.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                    Point3d st = new Point3d();
                    Point3d et = new Point3d();
                    if (acEnt != null)
                    {
                        if (acEnt.GetType() == typeof(Line))
                        {
                            var ll = acEnt as Line;
                            st = ll.StartPoint;
                            et = ll.EndPoint;
                        }

                        if (acEnt.GetType() == typeof(Arc))
                        {
                            var ll = acEnt as Arc;
                            st = ll.StartPoint;
                            et = ll.EndPoint;
                        }

                        if (acEnt.GetType() == typeof(Polyline))
                        {
                            //var ll = acEnt as Arc;
                            st = P1;
                            et = P1;
                        }

                        // Check End Point Conectivity 끝점 연결이 아니면 제외한다.
                        if ((P1.DistanceTo(st) < 2.0) || (P1.DistanceTo(et) < 2.0))
                        {
                            filterIdCol.Add(acSSObj.ObjectId);
                        }

                    }
                }
            }

            // Step through the objects in the selection set 
            // 겹쳐진 Entity 제거 
            foreach (ObjectId acSSObj1 in filterIdCol)
            {

                // Check to make sure a valid SelectedObject object was returned
                if (acSSObj1 != null)
                {
                    // Open the selected object for write
                    Entity acEnt = acSSObj1.GetObject(OpenMode.ForRead) as Entity;
                    Point3d st = new Point3d();
                    Point3d et = new Point3d();
                    if (acEnt != null)
                    {
                        if (acEnt.GetType() == typeof(Line))
                        {
                            var ll = acEnt as Line;
                            st = ll.StartPoint;
                            et = ll.EndPoint;
                        }

                        if (acEnt.GetType() == typeof(Arc))
                        {
                            var ll = acEnt as Arc;
                            st = ll.StartPoint;
                            et = ll.EndPoint;
                        }

                        if (acEnt.GetType() == typeof(Polyline))
                        {
                            //var ll = acEnt as Arc;
                            st = P1;
                            et = P1;
                        }

                        // Check End Point Conectivity 끝점 연결이 아니면 제외한다.
                        if ((P1.DistanceTo(st) < 2.0) || (P1.DistanceTo(et) < 2.0))
                        {
                            filterIdCol1.Add(acSSObj1);
                        }

                    }
                }
            }

            if (filterIdCol1.Count > 0)
                return filterIdCol1;
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }


        //-
        //-- 
        public static ObjectIdCollection GetCrossWindow(Point3d P1, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            double offset = 4.0;

            //points.Add(new Point3d(P1.X + offset, P1.Y - offset, 0.0));
            //points.Add(new Point3d(P1.X + offset, P1.Y + offset, 0.0));
            var p1 = new Point3d(P1.X + offset, P1.Y - offset, 0.0);
            var p2 = new Point3d(P1.X - offset, P1.Y + offset, 0.0);


            PromptSelectionResult psr2 = ed.SelectCrossingWindow(p1, p2, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //-- 
        public static ObjectIdCollection GetCrossFence(Point3d P1, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            double offset = 15.0;

            points.Add(new Point3d(P1.X + offset, P1.Y - offset, 0.0));
            points.Add(new Point3d(P1.X + offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X - offset, P1.Y + offset, 0.0));
            points.Add(new Point3d(P1.X - offset, P1.Y - offset, 0.0));
            points.Add(new Point3d(P1.X + offset - 1, P1.Y - offset, 0.0));

            //points.Add(new Point3d(P1.X + offset, P1.Y, 0.0));
            //points.Add(new Point3d(P1.X , P1.Y + offset, 0.0));
            //points.Add(new Point3d(P1.X - offset, P1.Y , 0.0));
            //points.Add(new Point3d(P1.X , P1.Y - offset, 0.0));
            //points.Add(new Point3d(P1.X + offset , P1.Y , 0.0));

            PromptSelectionResult psr2 = ed.SelectFence(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }
        public static ObjectIdCollection GetCrossFence(Line ln, SelectionFilter sf, double width = 4.0)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            var points = new Point3dCollection();
            Point3d sPt = ln.GetPointAtDist(2.0);
            Point3d ePt = ln.GetPointAtDist(ln.Length - 2);
            Vector3d perpVec = (ePt - sPt).CrossProduct(ln.Normal).GetNormal();
            Vector3d offset = perpVec * width;

            points.Add(sPt + offset);
            points.Add(sPt - offset);
            points.Add(ePt - offset);
            points.Add(ePt + offset);
            points.Add(sPt + offset * 0.99);

            PromptSelectionResult psr2 = ed.SelectFence(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }
        public static ObjectIdCollection GetCrossFence(BlockReference br, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            //var points = new Point3dCollection();
            //var exts = br.GeometricExtents;
            //Point3d sPt = exts.MinPoint;
            //Point3d ePt = exts.MaxPoint;
            //var _width = Math.Abs(ePt.X - sPt.X);
            //var _hh = Math.Abs(ePt.Y- sPt.Y);


            //points.Add(sPt);
            //points.Add(new Point3d(sPt.X+_width, sPt.Y,0));
            //points.Add(ePt);
            //points.Add(new Point3d(sPt.X, sPt.Y+_hh, 0));


            // 블록의 경계 박스를 가져옴
            Extents3d blockExtents = br.GeometricExtents;

            // 경계 박스의 꼭짓점을 추출하여 다각형을 만듦
            Point3dCollection blockPolygon = new Point3dCollection
                {
                    new Point3d(blockExtents.MinPoint.X, blockExtents.MinPoint.Y, 0),
                    new Point3d(blockExtents.MaxPoint.X, blockExtents.MinPoint.Y, 0),
                    new Point3d(blockExtents.MaxPoint.X, blockExtents.MaxPoint.Y, 0),
                    new Point3d(blockExtents.MinPoint.X, blockExtents.MaxPoint.Y, 0)
                };






            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(blockPolygon, sf);  //Polygon 에 결처진 모든 Entity  ed.SelectFence(blockPolygon, sf)
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }
        public static ObjectIdCollection GetCrossFence(DBText txt, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Text 경계 박스를 가져옴
            Extents3d blockExtents = txt.GeometricExtents;
            double hh = txt.Height;
            double ww = txt.GetWidth();

            // 경계 박스의 꼭짓점을 추출하여 다각형을 만듦
            Point3dCollection blockPolygon = new Point3dCollection
                {
                    new Point3d(blockExtents.MinPoint.X-ww, blockExtents.MinPoint.Y-hh, 0),
                    new Point3d(blockExtents.MaxPoint.X+ww, blockExtents.MinPoint.Y-hh, 0),
                    new Point3d(blockExtents.MaxPoint.X+ww, blockExtents.MaxPoint.Y+hh, 0),
                    new Point3d(blockExtents.MinPoint.X-ww, blockExtents.MaxPoint.Y+hh, 0)
                };


            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(blockPolygon, sf);  //Polygon 에 결처진 모든 Entity  ed.SelectFence(blockPolygon, sf)
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        public static ObjectIdCollection GetCrossFenceDoubleSize(DBText txt, SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var txt2Polygon = txt.GetDoubleSizePoly(); // 2배 확장된 Polygon을 가져온다.
            var pts = txt2Polygon.GetPoints();

            Point3dCollection blockPolygon = new Point3dCollection();
            foreach (var pt in pts)
            {
                blockPolygon.Add(pt);
            }


            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(blockPolygon, sf);  //Polygon 에 결처진 모든 Entity  ed.SelectFence(blockPolygon, sf)
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //- 
        public static ObjectIdCollection GetPipeSlectIDs()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionResult selRes = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("LINE", "Pipe"));
            // Create a selectionset and assign the selected objects
            //SelectionSet ss = selRes.Value;

            if (selRes.Status == PromptStatus.OK)
                return new ObjectIdCollection(selRes.Value.GetObjectIds());
            else
                return new ObjectIdCollection();

        }


        //-
        public static ObjectIdCollection GetCrossWindowSelectIDs(Point3d P1, SelectionFilter sf, double offsetX = 2.0)
        {
            // Pipe Tee 위치에서 Block을 선택하기 위한 거리값 지정 - x 값 500 정도 y 값은 2 사용 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            double offsetY = 2.0;

            points.Add(new Point3d(P1.X - offsetX, P1.Y + offsetY, 0.0));
            points.Add(new Point3d(P1.X - offsetX, P1.Y - offsetY, 0.0));
            points.Add(new Point3d(P1.X + offsetX, P1.Y - offsetY, 0.0));
            points.Add(new Point3d(P1.X + offsetX, P1.Y + offsetY, 0.0));

            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //-
        public static ObjectIdCollection GetCrossPolygonSelectIDs(Polyline pl, SelectionFilter sf)
        {
            // Pipe Tee 위치에서 Block을 선택하기 위한 거리값 지정 - x 값 500 정도 y 값은 2 사용 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (pl == null) return null;
            var points = new Point3dCollection();
            //double offsetY = 2.0;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point3d pt = new Point3d();
                pt = pl.GetPoint3dAt(i);
                points.Add(pt);
            }
            //points.Add(new Point3d(P1.X - offsetX, P1.Y + offsetY, 0.0));
            //points.Add(new Point3d(P1.X - offsetX, P1.Y - offsetY, 0.0));
            //points.Add(new Point3d(P1.X + offsetX, P1.Y - offsetY, 0.0));
            //points.Add(new Point3d(P1.X + offsetX, P1.Y + offsetY, 0.0));

            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            //PromptSelectionResult psr2 = ed.SelectFence(points, sf);
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //22-04-19
        public static List<Line> GetPipesUnderBlocks(BlockReference br, SelectionFilter sf)
        {
            //List<Line> lines = new List<Line>();
            var pl = br.GetPoly();

            var ids = SelSet.GetCrossPolygonSelectIDs(pl, sf);//"INSERT"
            var pipes = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();

            if (pipes.Count == 0) return null;
            return pipes;
        }

        public static List<Line> GetPipesUnderBlocks1(BlockReference br, SelectionFilter sf)
        {
            //List<Line> lines = new List<Line>();
            var pl = br.GetPoly();   //jBlock.GetBlockBoxPolyLine1(br);

            var ids = SelSet.GetCrossPolygonSelectIDs(pl, sf);//"INSERT"
            var pipes = ids.OfType<ObjectId>().Select(x => x.GetObject(OpenMode.ForRead) as Line).ToList();

            pl.Dispose();
            if (pipes.Count == 0) return null;
            return pipes;
        }


        public static List<Entity> GetEntitys(Point3d P1, SelectionFilter sf)
        {
            List<Entity> ents = new List<Entity>();
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            var ids = GetCrossFence(P1, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            if (ids.Count == 0) { return null; }
            return ents;
        }

        //public static List<Entity> GetEntitys(Entity ent, SelectionFilter sf)
        //{
        //    List<Entity> ents = new List<Entity>();
        //    //var ids = GetCrossWindowSelectIDs(P1, sf);
        //    var ids = GetCrossFence(P1, sf);
        //    ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
        //    if (ids.Count == 0) { return null; }
        //    return ents;
        //}


        public static List<Entity> GetEntitys(Line ln, SelectionFilter sf, double width = 2.0)
        {
            List<Entity> ents = new List<Entity>();
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            var ids = GetCrossFence(ln, sf, width);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            if (ids.Count == 0) { return null; }
            return ents;
        }

        public static List<Entity> GetEntitysByEntitys(Entity en, SelectionFilter sf)// en Line,Poly,Arc
        {
            List<Entity> ents = new List<Entity>();
            Polyline pl = null;
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            if (en.GetType() == typeof(Line))
            {
                var ln = (Line)en;
                pl = ln.GetPolyline();
            }
            if (en.GetType() == typeof(Polyline))
            {
                pl = en as Polyline;
            }
            if (en.GetType() == typeof(Arc))
            {
                var arc = en as Arc;
                Polyline ply = new Polyline();
                ply.AddVertexAt(0, new Point2d(arc.StartPoint.X, arc.StartPoint.Y), 0, 0, 0);
                ply.AddVertexAt(1, new Point2d(arc.EndPoint.X, arc.EndPoint.Y), 0, 0, 0);
                pl = ply;
            }
            if (pl == null) return null;

            var ids = GetCrossPolygonSelectIDs(pl, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            if (ids.Count == 0) { return null; }
            return ents;

        }



        public static List<Entity> GetEntitys(BlockReference br, SelectionFilter sf)
        {
            List<Entity> ents = new List<Entity>();
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            var ids = GetCrossFence(br, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            if (ids.Count == 0) { return null; }
            return ents;
        }
        public static List<Entity> GetEntitys(DBText txt, SelectionFilter sf)
        {
            List<Entity> ents = new List<Entity>();
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            var ids = GetCrossFenceDoubleSize(txt, sf); // GetCrossFence(txt, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            if (ids.Count == 0) { return null; }
            return ents;
        }



        // Line ll 70% 지점과 EndPoint 에서 5 길이 확장된 지점에 걸친 Entity 찾기
        public static List<Entity> GetEntitys(Line ll, Point3d P1, SelectionFilter sf) //P1 방향성 Pt
        {
            // P1 이 EndPoint 되는 Line 생성 
            var sp = ll.StartPoint.DistanceTo(P1) > ll.EndPoint.DistanceTo(P1) ? ll.StartPoint : ll.EndPoint;
            var lll = new Line(sp, P1);
            //길이 5인 Unit Vector 
            var uVec1 = lll.GetFirstDerivative(lll.GetParameterAtPoint(lll.EndPoint)); //  lll 의 endPoint 지점의 Tangent Unit Vector
            var uVec = (lll.EndPoint - lll.StartPoint).GetNormal() * 5;
            var tp = lll.EndPoint + uVec;
            // lll 70%  지점 Point 
            var ssp = lll.GetPointAtDist(lll.Length * 0.7);
            //  Uvec 과 90도 방향 Vector 
            var ang = uVec.TransformBy(Matrix3d.Rotation(Math.PI / 2, lll.Normal, Point3d.Origin));

            //ssp tp 기점 ang 이동된 Poly
            var pl = new Polyline();

            List<Entity> ents = new List<Entity>();
            //var ids = GetCrossWindowSelectIDs(P1, sf);
            var ids = GetCrossFence(P1, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => ents.Add(x.GetObject(OpenMode.ForRead) as Entity));
            return ents;
        }


        //-- 
        public static ObjectIdCollection GetCrossWindowSelectIDs(Line L1, SelectionFilter sf, double width = 2.0)
        {
            // 대상라인 끝점은 제외 하고 안쪽에 걸쳐준 Entity른 선택 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            Point3d sPt = L1.GetPointAtDist(2.0);
            Point3d ePt = L1.GetPointAtDist(L1.Length - 2);
            Vector3d perpVec = (ePt - sPt).CrossProduct(L1.Normal).GetNormal();
            Vector3d offset = perpVec * width;

            points.Add(sPt + offset);
            points.Add(sPt - offset);
            points.Add(ePt - offset);
            points.Add(ePt + offset);


            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        //-- 
        public static ObjectIdCollection GetCrossWindowSelectIDs(Line L1, SelectionFilter sf)//extd :Line 확장 길이 
        {
            // 대상라인 끝점은 제외 하고 안쪽에 걸쳐준 Entity른 선택 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            var points = new Point3dCollection();
            var delta = L1.Delta / L1.Length * 2;


            Point3d sPt = L1.StartPoint - delta;   // L1.GetPointAtDist(2.0);
            Point3d ePt = L1.EndPoint + delta;
            //L1.GetPointAtDist(L1.Length - 2);
            Vector3d perpVec = (ePt - sPt).CrossProduct(L1.Normal).GetNormal();
            Vector3d offset = perpVec * 2;

            points.Add(sPt + offset);
            points.Add(sPt - offset);
            points.Add(ePt - offset);
            points.Add(ePt + offset);


            PromptSelectionResult psr2 = ed.SelectCrossingPolygon(points, sf);  //Polygon 에 결처진 모든 Entity 
            SelectionSet ss = psr2.Value;
            if (psr2.Status == PromptStatus.OK)
                return new ObjectIdCollection(psr2.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
            //사용법
            //ObjectIdCollection ids = FuncLine.GetCrossWindowSelectIDs(P1, P2, sf);
            //List<Line> selLines = new List<Line>();
            //foreach (var objid in ids.OfType<ObjectId>().ToList())
            //{
            //    var sLL = (Line)tr.GetObject(objid, OpenMode.ForWrite);
            //    selLines.Add(sLL);
        }

        // 
        public static List<DBObject> DBObjectSelectChain(DBObject aEn)
        {
            TypedValue[] acTypValAr = new TypedValue[2];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "LINE,ARC"), 0);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, "Duct"), 1);

            // Assign the filter criteria to a SelectionFilter object
            SelectionFilter sf = new SelectionFilter(acTypValAr);

            Point3d sPt = new Point3d();
            Point3d ePt = new Point3d();
            if (aEn.GetType() == typeof(Line))
            {
                sPt = (aEn as Line).StartPoint;
                ePt = (aEn as Line).EndPoint;
            }
            if (aEn.GetType() == typeof(Arc))
            {
                sPt = (aEn as Arc).StartPoint;
                ePt = (aEn as Arc).EndPoint;
            }

            List<DBObject> dbObjs = new List<DBObject>();

            var ids = GetCrossWindowSelectIDs(sPt, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => dbObjs.Add(x.GetObject(OpenMode.ForRead)));
            ids = GetCrossWindowSelectIDs(ePt, sf);
            ids.OfType<ObjectId>().ToList().ForEach(x => dbObjs.Add(x.GetObject(OpenMode.ForRead)));

            dbObjs.Distinct();
            dbObjs.Remove(aEn);
            return dbObjs;
        }



        // Lisp (SETQ S1 (SSGET)) 구현 !S1
        public static void SelectionSetByName(string sName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //doc.SendStringToExecute("_.move all   0,0,1e99 ", true, false, false);
            doc.SendStringToExecute($"(SETQ {sName} (SSGET)) ", true, false, false);
        }

        public static void SelectionGetByName(string sName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //doc.SendStringToExecute("_.move all   0,0,1e99 ", true, false, false);
            doc.SendStringToExecute($"_.Select {sName} ", true, false, false);
        }

        //23.05.29

        //public static string GetReducerDia(Line L1) 
        //{
        //    //string diaReducer = "";
        //    ObjectIdCollection ids1 = new ObjectIdCollection();
        //    ObjectIdCollection ids2 = new ObjectIdCollection();

        //    ids1 = GetCrossWindowSelectIDs(L1.StartPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,MainPipe"));
        //    ids2 = GetCrossWindowSelectIDs(L1.EndPoint, JSelFilter.MakeFilterTypesRegs("LINE", "Pipe,MainPipe"));

        //    List<Line> lines = new List<Line>();

        //    ids1.OfType<ObjectId>().ToList().ForEach(x => lines.Add(x.GetObject(OpenMode.ForRead) as Line));
        //    ids2.OfType<ObjectId>().ToList().ForEach(x => lines.Add(x.GetObject(OpenMode.ForRead) as Line));

        //    lines = lines.Distinct().ToList();

        //    //String dia = ""; // 해당 Linedml Dia;
        //    //String dias = ""; // StartPoinr에 걸쳐진 Line의 Dia;
        //    //String diae = ""; // EndPoinr에 걸쳐진 Line의 Dia;

        //    List<Double> dd = new List<double>();

        //    //Check Previous PipeType 
        //    PipeType myPipe = new PipeType();
        //    myPipe = JDBDictionary.GetPipeType();



        //    if (myPipe.Type == "Pipe")  // 배관과 나머지는 Dia 구하는 방식이 Double, String 으로 다르다.
        //    {
        //        lines.ForEach(x => dd.Add(JpipeFunc.GetPipeDiaFromObject(x)));
        //    }
        //    else 
        //    {
        //        lines.ForEach(x => dd.Add( Convert.ToDouble(  JXdata.GetXdata(x,"Dia"))  ));
        //    }


        //    var dd1 = dd.OrderByDescending(x => x).ToList();

        //    //if((dd1.Count==2)|| (dd1.Count == 3))  return $"{dd1[0].ToString()}^{dd1[1].ToString()}";
        //    if ((dd1.Count >= 2) && (dd1[0] > dd1[1])) return $"{dd1[0].ToString()}^{dd1[1].ToString()}";
        //    if (dd1.Count == 1) return $"{dd1[0].ToString()}";
        //    return "";
        //}

    }

    //Entity 
    public class JEntity
    {
        public static Document AcDoc
        {
            get { return Application.DocumentManager.MdiActiveDocument; }
        }

        public static SelectionSet getSelectionSet()
        {
            var _editor = AcDoc.Editor;
            var _selAll = _editor.SelectAll();
            return _selAll.Value;
        }


        public static SelectionFilter MakeSelFilter(string start) //09.05
        {
            TypedValue[] filterlist = new TypedValue[1];
            filterlist[0] = new TypedValue((int)DxfCode.Start, start);
            //filterlist[1] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName);

            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }

        public static List<Entity> GetPreSeletedEntity()
        {
            List<Entity> entities = new List<Entity>();
            var psr = AcDoc.Editor.SelectImplied();
            var acSSet = psr.Value;
            if (acSSet == null) return null;
            foreach (SelectedObject ss in acSSet) //var ii in ids2
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                if (ssen != null)
                {
                    entities.Add(ssen);
                }
            }




            return entities;
        }



        public static List<T> GetEntityByTpye<T>(string psoMessage) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }

        public static List<T> GetSelectedEntityByTpye<T>() where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            //pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.SelectImplied();                    //ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static List<ObjectId> GetSelectedEntityIds()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            var ed = doc.Editor;
            PromptSelectionOptions pso = new PromptSelectionOptions();
            PromptSelectionResult sPsr = ed.SelectImplied();
            if (sPsr.Status != PromptStatus.OK)
                return null;

            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return ids;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static List<T> GetSelectedEntityByTpye<T>(SelectionFilter sf) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            //pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.SelectImplied();                    //ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }

        public static List<T> GetEntityByTpye<T>(string psoMessage, SelectionFilter sf) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    //new TypedValue( (int)DxfCode.Start, "LINE,ARC,CIRCLE,TEXT"),
            //    new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);
            SelectionSet ss;
            PromptSelectionResult psr = ed.GetSelection(pso, sf);
            if (psr.Status == PromptStatus.OK)
            {
                ss = psr.Value;
            }
            else
            {
                return null;
            }


            //if (sLinesPsr.Status != PromptStatus.OK)
            //    return null;





            //Get Base Lines

            //SelectionSet baseSet = sLinesPsr.Value;
            var ids = psr.Value.GetObjectIds().ToList();

            //sEntitys = ids.ForEach(x=> x.GetObject(OpenMode.ForRead)).OfType<T>().ToList();


            //if (typeof(T) == typeof(Line))
            //{
            List<T> sEntitys = new List<T>();

            var aaa = ss.OfType<SelectedObject>().ToList();
            //
            foreach (var ss1 in aaa)
            {
                var ssen = ss1.ObjectId.GetObject(OpenMode.ForRead) as T;
                //var ssen = tr.GetObject(ss1.ObjectId,OpenMode.ForWrite) as T;  
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            // tr.Commit();
            //}

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static SelectionSet GetEntityByType(Editor ed, string psoMessage, SelectionFilter sf)
        {
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";
            SelectionSet ss;
            PromptSelectionResult psr = ed.GetSelection(pso, sf);
            if (psr.Status == PromptStatus.OK)
            {
                ss = psr.Value;
            }
            else
            {
                return null;
            }
            return ss;
        }

        public static List<Entity> GetAllEntityBySelcFilter(SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            var ed = doc.Editor;

            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();

            List<Entity> sEntitys = new List<Entity>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            return sEntitys;
        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static List<T> GetEntityAllByTpye<T>(SelectionFilter sf) where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();


            var aaa = baseSet.OfType<SelectedObject>().ToList();


            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            //}

            return sEntitys;

        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static IEnumerable<T> GetEntityAllByType<T>(SelectionFilter sf, String grp = "") where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) yield return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();
            if (baseSet == null)
            {
                //yield return null;// Enumerable.Empty<T>();
                yield break;
            }

            var aaa = baseSet.OfType<SelectedObject>();


            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                var grpName = JXdata.GetXdata(ssen, "Group");
                if (grp == "")
                {
                    yield return ssen;
                }
                if (ssen != null && grpName != null && grpName == grp)
                {
                    yield return ssen;
                    //sEntitys.Add(ssen);
                }
            }
            //}

            //return sEntitys;

        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static List<T> GetEntityAllByTpye<T>(Transaction tr, SelectionFilter sf) where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();
            var aaa = baseSet.OfType<SelectedObject>().ToList();

            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            //tr.Commit();

            //}

            return sEntitys;

        }

        public static List<ObjectId> GetEntityAllByType(SelectionFilter sf)
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<ObjectId> sIds = new List<ObjectId>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                sIds.Add(ss.ObjectId);
            }
            //}
            if (sIds.Count == 0) return null;
            return sIds;

        }


        public static SelectionFilter MakeSelFilter(string start, string regName) //09.05
        {
            TypedValue[] filterlist = new TypedValue[2];
            filterlist[0] = new TypedValue((int)DxfCode.Start, start);
            filterlist[1] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName);

            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }

        public static SelectionFilter MakeSelFilterAndReg(string start, string regName1, string regName2) //24.11.26
        {
            TypedValue[] filterlist =
            {
                new TypedValue((int)DxfCode.Start, start),
                new TypedValue((int) DxfCode.Operator, "<and"),
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName1),
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName2),
                new TypedValue((int) DxfCode.Operator, "and>")
            };
            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }


    }
    public class JEntityFunc
    {
        public static Document AcDoc
        {
            get { return Application.DocumentManager.MdiActiveDocument; }
        }

        public static SelectionSet getSelectionSet()
        {
            var _editor = AcDoc.Editor;
            var _selAll = _editor.SelectAll();
            return _selAll.Value;
        }


        public static SelectionFilter MakeSelFilter(string start) //09.05
        {
            TypedValue[] filterlist = new TypedValue[1];
            filterlist[0] = new TypedValue((int)DxfCode.Start, start);
            //filterlist[1] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName);

            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }

        public static List<Entity>? GetPreSeletedEntity()
        {
            List<Entity> entities = new List<Entity>();
            var psr = AcDoc.Editor.SelectImplied();
            var acSSet = psr.Value;
            if (acSSet == null) return null;
            foreach (SelectedObject ss in acSSet) //var ii in ids2
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                if (ssen != null)
                {
                    entities.Add(ssen);
                }
            }




            return entities;
        }



        public static List<T> GetEntityByTpye<T>(string psoMessage) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }

        public static List<T> GetSelectedEntityByTpye<T>() where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            //pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.SelectImplied();                    //ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static List<ObjectId> GetSelectedEntityIds()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            var ed = doc.Editor;
            PromptSelectionOptions pso = new PromptSelectionOptions();
            PromptSelectionResult sPsr = ed.SelectImplied();
            if (sPsr.Status != PromptStatus.OK)
                return null;

            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return ids;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static List<T> GetSelectedEntityByTpye<T>(SelectionFilter sf) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            //pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    new TypedValue( (int)DxfCode.Start, "LINE,ARC"),
            //    //new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionResult sPsr = ed.SelectImplied();                    //ed.GetSelection(pso);
            if (sPsr.Status != PromptStatus.OK)
                return null;


            SelectionSet baseSet = sPsr.Value;
            List<ObjectId> ids = sPsr.Value.GetObjectIds().ToList();

            List<T> sEntitys = new List<T>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }

        public static List<T> GetEntityByTpye<T>(string psoMessage, SelectionFilter sf) where T : DBObject
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;


            PromptSelectionOptions pso = new PromptSelectionOptions();
            //PromptSelectionOptions pso2 = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";

            //TypedValue[] tvs = new TypedValue[1] {
            //    //new TypedValue( (int)DxfCode.Start, "LINE,ARC,CIRCLE,TEXT"),
            //    new TypedValue( (int)DxfCode.ExtendedDataRegAppName, "FirePipe,MainPipe,Pipe,Duct")
            //    };
            //SelectionFilter sf = new SelectionFilter(tvs);
            SelectionSet ss;
            PromptSelectionResult psr = ed.GetSelection(pso, sf);
            if (psr.Status == PromptStatus.OK)
            {
                ss = psr.Value;
            }
            else
            {
                return null;
            }


            //if (sLinesPsr.Status != PromptStatus.OK)
            //    return null;





            //Get Base Lines

            //SelectionSet baseSet = sLinesPsr.Value;
            var ids = psr.Value.GetObjectIds().ToList();

            //sEntitys = ids.ForEach(x=> x.GetObject(OpenMode.ForRead)).OfType<T>().ToList();


            //if (typeof(T) == typeof(Line))
            //{
            List<T> sEntitys = new List<T>();

            var aaa = ss.OfType<SelectedObject>().ToList();
            //
            foreach (var ss1 in aaa)
            {
                var ssen = ss1.ObjectId.GetObject(OpenMode.ForRead) as T;
                //var ssen = tr.GetObject(ss1.ObjectId,OpenMode.ForWrite) as T;  
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            // tr.Commit();
            //}

            //.Select(x => x.ObjectId.GetObject(OpenMode.ForRead) as Line).ToList();
            return sEntitys;
            //}
            //var gLines = baseLines.GroupBy(x => (x.Angle / Math.PI * 180.0)).ToList();

        }
        public static SelectionSet GetEntityByType(Editor ed, string psoMessage, SelectionFilter sf)
        {
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n {psoMessage} ";
            SelectionSet ss;
            PromptSelectionResult psr = ed.GetSelection(pso, sf);
            if (psr.Status == PromptStatus.OK)
            {
                ss = psr.Value;
            }
            else
            {
                return null;
            }
            return ss;
        }

        public static List<Entity> GetAllEntityBySelcFilter(SelectionFilter sf)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //var db = doc.Database;
            var ed = doc.Editor;

            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();

            List<Entity> sEntitys = new List<Entity>();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as Entity;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }

            return sEntitys;
        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static List<T> GetEntityAllByTpye<T>(SelectionFilter sf) where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();


            var aaa = baseSet.OfType<SelectedObject>().ToList();


            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            //}

            return sEntitys;

        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static IEnumerable<T> GetEntityAllByType<T>(SelectionFilter sf, String grp = "") where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) yield return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();
            if (baseSet == null)
            {
                //yield return null;// Enumerable.Empty<T>();
                yield break;
            }

            var aaa = baseSet.OfType<SelectedObject>();


            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                var grpName = JXdata.GetXdata(ssen, "Group");
                if (grp == "")
                {
                    yield return ssen;
                }
                if (ssen != null && grpName != null && grpName == grp)
                {
                    yield return ssen;
                    //sEntitys.Add(ssen);
                }
            }
            //}

            //return sEntitys;

        }

        // 필터에 해당되는 모든 Entity Return 09.05
        public static List<T> GetEntityAllByTpye<T>(Transaction tr, SelectionFilter sf) where T : DBObject
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<T> sEntitys = new List<T>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();
            var aaa = baseSet.OfType<SelectedObject>().ToList();

            foreach (var ss in aaa)
            {
                var ssen = ss.ObjectId.GetObject(OpenMode.ForRead) as T; //tr.GetObject(ss.ObjectId, OpenMode.ForRead) as T;//   ss.ObjectId.GetObject(OpenMode.ForRead) as T;
                if (ssen != null)
                {
                    sEntitys.Add(ssen);
                }
            }
            //tr.Commit();

            //}

            return sEntitys;

        }

        public static List<ObjectId> GetEntityAllByType(SelectionFilter sf)
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            List<ObjectId> sIds = new List<ObjectId>();
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            PromptSelectionResult selRes = ed.SelectAll(sf);
            if (selRes.Status != PromptStatus.OK) return null;

            SelectionSet baseSet = selRes.Value;
            //var ids = selRes.Value.GetObjectIds().ToList();

            var aaa = baseSet.OfType<SelectedObject>().ToList();
            foreach (var ss in aaa)
            {
                sIds.Add(ss.ObjectId);
            }
            //}
            if (sIds.Count == 0) return null;
            return sIds;

        }


        public static SelectionFilter MakeSelFilter(string start, string regName) //09.05
        {
            TypedValue[] filterlist = new TypedValue[2];
            filterlist[0] = new TypedValue((int)DxfCode.Start, start);
            filterlist[1] = new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName);

            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }

        public static SelectionFilter MakeSelFilterAndReg(string start, string regName1, string regName2) //24.11.26
        {
            TypedValue[] filterlist =
            {
                new TypedValue((int)DxfCode.Start, start),
                new TypedValue((int) DxfCode.Operator, "<and"),
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName1),
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName2),
                new TypedValue((int) DxfCode.Operator, "and>")
            };
            SelectionFilter sf = new SelectionFilter(filterlist);

            return sf;
        }


    }
    public class JSelFilter
    {
        // regName을 기준으로 선택 필터 만든다.  
        static public SelectionFilter MakeRegNameFilter(string regName) //fire case -> "FirePipe,MainPipe,Unit,T,SidePipe"
        {
            TypedValue[] filterlist = new TypedValue[1];
            filterlist.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, regName), 0);
            SelectionFilter filter = new SelectionFilter(filterlist);

            return filter;
        }

        // Entity Type 과 regName을 기준으로 선택 필터 만든다.  
        static public SelectionFilter MakeFilterTypesRegs(string entitys, string regNames) //fire case -> "FirePipe,MainPipe,Unit,T,SidePipe"
        {
            //entitys : "LINE,ARC,
            //regNames " "Pipe,Duct,MainPipe"
            TypedValue[] filterlist = new TypedValue[2];
            filterlist.SetValue(new TypedValue((int)DxfCode.Start, entitys), 0);
            filterlist.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, regNames), 1);
            SelectionFilter filter = new SelectionFilter(filterlist);

            return filter;
        }

        // Entity Type  기준으로 선택 필터 만든다.  
        static public SelectionFilter MakeFilterTypes(string entitys) //fire case -> "LINE,ARC,POLYLINE,TEXT"
        {
            //entitys : "LINE,ARC,
            //regNames " "Pipe,Duct,MainPipe"
            TypedValue[] filterlist = new TypedValue[1];
            filterlist.SetValue(new TypedValue((int)DxfCode.Start, entitys), 0);
            //filterlist.SetValue(new TypedValue((int)DxfCode.ExtendedDataRegAppName, regNames), 1);
            SelectionFilter filter = new SelectionFilter(filterlist);

            return filter;
        }


    }



    public class Func
    {
        /// <summary>
        /// colinear한 선분들 중 가장 짧은 것만 남기고 나머지를 제거합니다.
        /// colinear 관계가 없는 선분은 그대로 유지됩니다.
        /// 거리가 너무먼   colinear 선분은 제외함.(대상 길이의 합보다 크면   제외) 
        /// </summary>
        /// <param name="lines">입력 선분 리스트</param>
        /// <returns>colinear 그룹별로 가장 짧은 선분과 단독 선분이 포함된 리스트</returns>
        public static List<Line> RemoveColinearLinesKeepShortest(List<Line> lines,Point3d bpt)
        {
            if (lines == null || lines.Count <= 1)
                return lines?.ToList() ?? new List<Line>();

            var result = new List<Line>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                // 이미 처리된 선분은 건너뛰기
                if (processedIndices.Contains(i))
                    continue;

                // 현재 선분과 colinear한 모든 선분들을 찾기
                var colinearGroup = new List<(Line line, int index)>() { (lines[i], i) };

                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (processedIndices.Contains(j))
                        continue;

                    var dis = (lines[i].FindNearestPointsDicetance(lines[j]));
                    var len = lines[i].Length + lines[j].Length;    
                    if ((lines[i].IsCoLinear1(lines[j])) && (dis < len/2 ))
                    {
                        colinearGroup.Add((lines[j], j));
                    }
                }

                // colinear한 다른 선분이 있는 경우
                if (colinearGroup.Count > 1)
                {
                    //// 현재 선분도 그룹에 추가
                    //colinearGroup.Add((lines[i], i));

                    // 그룹에서 기준 point 가장 짧은 선분 찾기
                    var shortestLine = colinearGroup
                        .OrderBy(item => item.line.GetCentor().DistanceTo(bpt))
                        .First();

                    // 결과에 가장 짧은 선분 추가
                    result.Add(shortestLine.line);

                    // 이 그룹의 모든 인덱스를 처리됨으로 표시
                    foreach (var (line, index) in colinearGroup)
                    {
                        processedIndices.Add(index);
                    }
                }
                else
                {
                    // colinear한 다른 선분이 없는 경우 - 원본 그대로 추가
                    result.Add(lines[i]);
                    processedIndices.Add(i);
                }
            }



            return result;
        }
    }
}
