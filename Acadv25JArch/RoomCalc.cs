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
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace Acadv25JArch
{

    public class RoomCalc
    {
        #region ToWire
        // 선택 Line Poly XData Wire 지정
        //TO DuctLine
        [CommandMethod("RR", CommandFlags.UsePickSet)] //ToRoom
                                                       //Text 에  Room 지정                                                    
        public void Text_To_Room()
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
                List<Entity> targets = JEntityFunc.GetEntityByTpye<Entity>("Room 대상  Text를  선택 하세요?", JSelFilter.MakeFilterTypes("TEXT"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Archi,Room,Disp");
                // Step through the objects in the selection set
                Line sLine = new Line();
                foreach (Entity acEnt in targets)
                {
                    LayerTableRecord layer = tr.GetObject(acEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (!layer.IsLocked)
                    {
                        if (acEnt.GetType() == typeof(Polyline))
                        {
                            var pl = (Polyline)acEnt;
                            if (pl.Closed == true) continue;// Closed poly는 무시한다.
                            var ll = pl.Get2PonitLines();
                            if (ll != null)
                            {
                                ll.ColorIndex = acEnt.ColorIndex;
                                ll.Layer = acEnt.Layer;
                                acEnt.UpgradeOpen();
                                acEnt.Erase();
                                JXdata.SetXdata(ll, "Elec", "Wire");
                                JXdata.SetXdata(ll, "Wire", "Init Wire");
                                JXdata.SetXdata(ll, "wNum", "2"); // 전등 표시가 없으면 2 가닥
                                JXdata.SetXdata(ll, "Disp", "ww");
                                //JXdata.SetXdata(ll, "Mat", "Hidden");
                                btr.AppendEntity(ll);
                                tr.AddNewlyCreatedDBObject(ll, true);
                            }
                            else
                            {
                                acEnt.UpgradeOpen();
                                JXdata.SetXdata(pl, "Elec", "Wire");
                                JXdata.SetXdata(pl, "Wire", "Init Wire");
                                JXdata.SetXdata(pl, "wNum", "2");
                                JXdata.SetXdata(pl, "Disp", "ww");
                                //JXdata.SetXdata(pl, "Mat", "Hidden");
                            }
                        }

                        if (acEnt.GetType() == typeof(DBText))
                        {
                            JXdata.DeleteAll(acEnt);
                            JXdata.SetXdata(acEnt, "Archi", "Room");
                            JXdata.SetXdata(acEnt, "Room", "Room");
                            JXdata.SetXdata(acEnt, "Disp", "__");
                            // JXdata.SetXdata(acEnt, "Mat", "Hidden");
                            //}
                        }


                    }
                }

                // Save the new object to the database
                tr.Commit();

                // Dispose of the transaction
            }
        }
        #endregion

    }
}
