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
using Exception = System.Exception;

namespace Acadv25JArch
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
            obj.UpgradeOpen();
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
        public void Entity_To_DeleteXdata()
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
                List<Entity> targets = JEntity.GetEntityByTpye<Entity>("Xdata Del 대상을 선택 하세요?", JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOLYLINE,INSERT"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;

                //var btr = tr.GetModelSpaceBlockTableRecord(db);
                BlockTableRecord btr =(BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
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

                    Entity ent = tr.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;
                    if (!(ent is BlockReference))
                    {
                        ed.WriteMessage("\n선택한 객체가 블록 참조가 아닙니다.");
                        return;
                    }

                    BlockReference blockRef = ent as BlockReference;
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

                    BlockTableRecord btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    btr.Origin += displacement;

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId brefId in bt)
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
}
