using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CADExtension;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class BlockPart : INotifyPropertyChanged
    {
        private static int IdCounter = 1;
        public int Index { get; set; }
        private ObjectId ObjID { get; set; }  // Block Id not Blockreference
        public System.Drawing.Image Img { get; set; }      // picBoxBr.Image =   btr.PreviewIcon?.GetThumbnailImage(192, 192, () => false, IntPtr.Zero);

        public string Name { get; set; }  // Block Name
        public int Count { get; set; }   // 행당 Block의 개수 
        public string PartName { get; set; } // 부품명 
        public string Type { get; set; } //  Type  // 문{Door}  창문{Windwo} 
        public string SymbolType { get; set; } // 동일 Block이라도 SymbolType이 다르면 구별해서 적용
                                               // SymbolType이 있으면 Name = Name+":+SymbolType 적용 
                                               // 이 정보는 선택된 Block을 Grouping 할 떄 분류 한다.
        //public String Attach { get; set; }  


        // 생성자 
        public BlockPart(ObjectId id)
        {
            this.Index = IdCounter;
            this.ObjID = id;
            //this.Count = num;
            var btr = id.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            this.Name = btr.Name;
            //this.IsWire = false;
            //this.IsReturn = false;
            this.Img = btr.PreviewIcon?.GetThumbnailImage(40, 40, () => false, IntPtr.Zero);
            IdCounter++;
        }

        //
        public event PropertyChangedEventHandler PropertyChanged;

        //Function
        public ObjectId GetId()
        {
            return this.ObjID;
        }
        public static void ResetCounter()
        {
            IdCounter = 1;
        }

        //Static Function
        public static List<BlockPart> GetSelectedBlockParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                var entids = JEntity.GetSelectedEntityIds();

                List<ObjectId> brids = new List<ObjectId>();
                if (entids == null)
                {
                    tr.Commit();
                    return null;
                }

                //Filter BlockReferenec 
                foreach (var id in entids)
                {
                    //Check 
                    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                    {
                        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                        brids.Add(id);
                    }
                }

                FormBlockPart.selids = brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    if (btr.IsLayout || btr.IsAnonymous) continue; // Layout과 Anonymous Block은 제외   
                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }

        public static List<BlockPart> GetAllBlockParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            // 문서 잠금 및 트랜잭션 시작
            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                //var entids = JEntity.GetSelectedEntityIds();

                //List<ObjectId> brids = new List<ObjectId>();
                //if (entids == null)
                //{
                //    tr.Commit();
                //    return null;
                //}

                ////Filter BlockReferenec 
                //foreach (var id in entids)
                //{
                //    //Check 
                //    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                //    {
                //        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                //        brids.Add(id);
                //    }
                //}

                var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("INSERT"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (ents == null) return null;
                foreach (Entity ent in ents)
                {
                    blockrefs.Add(ent as BlockReference);
                }
                FormBlockPart.selids = blockrefs.Select(x => x.ObjectId).ToList();    //          brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x =>( x.GetName() +( JXdata.GetXdata(x, "Type") ?? "")+(JXdata.GetXdata(x, "SymbolType") ?? "")));

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    if(btr.IsLayout || btr.IsAnonymous) continue; // Layout과 Anonymous Block은 제외


                    var Img = btr.PreviewIcon?.GetThumbnailImage(128, 128, () => false, IntPtr.Zero);
                    if (Img == null) continue; // Img가 없는것은 제외
                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;
                    //if (JXdata.GetXdata(brg.First(), "IsBlockCount") == "True") jbtr.IsBlockCount = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }

        public static List<BlockPart> GetZoneBlockParts(ComboBox cb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                //var entids = JEntity.GetSelectedEntityIds();

                //List<ObjectId> brids = new List<ObjectId>();
                //if (entids == null)
                //{
                //    tr.Commit();
                //    return null;
                //}

                ////Filter BlockReferenec 
                //foreach (var id in entids)
                //{
                //    //Check 
                //    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                //    {
                //        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                //        brids.Add(id);
                //    }
                //}

                var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("INSERT"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (ents == null) return null;
                foreach (Entity ent in ents)
                {
                    //Check Zone 
                    //var zn = JXdata.GetXdata(ent, "Zone");
                    //if (zn != cb.SelectedItem.ToString()) continue;
                    if (JXdata.GetXdata(ent, "Zone") == cb.Text)
                    {
                        blockrefs.Add(ent as BlockReference);
                    }
                }
                FormBlockPart.selids = blockrefs.Select(x => x.ObjectId).ToList();    //          brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }

                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }


    }
    public class WindowPart : INotifyPropertyChanged
    {
        private static int IdCounter = 1;
        public int Index { get; set; }
        private ObjectId ObjID { get; set; }  // Block Id not Blockreference
        public System.Drawing.Image Img { get; set; }      // picBoxBr.Image =   btr.PreviewIcon?.GetThumbnailImage(192, 192, () => false, IntPtr.Zero);

        public string Name { get; set; }  // Block Name
        public int Count { get; set; }   // 행당 Block의 개수 
        public string PartName { get; set; } // 부품명 
        public string Type { get; set; } //  Type  // 문{Door}  창문{Windwo} 
        public string SymbolType { get; set; } // 동일 Block이라도 SymbolType이 다르면 구별해서 적용
                                               // SymbolType이 있으면 Name = Name+":+SymbolType 적용 
                                               // 이 정보는 선택된 Block을 Grouping 할 떄 분류 한다.
                                               //public String Attach { get; set; }  


        // 생성자 
        public WindowPart(ObjectId id)
        {
            this.Index = IdCounter;
            this.ObjID = id;
            //this.Count = num;
            var btr = id.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            this.Name = btr.Name;
            //this.IsWire = false;
            //this.IsReturn = false;
            this.Img = btr.PreviewIcon?.GetThumbnailImage(40, 40, () => false, IntPtr.Zero);
            IdCounter++;
        }

        //
        public event PropertyChangedEventHandler PropertyChanged;

        //Function
        public ObjectId GetId()
        {
            return this.ObjID;
        }
        public static void ResetCounter()
        {
            IdCounter = 1;
        }

        //Static Function
        public static List<WindowPart> GetSelectedBlockParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<WindowPart> blockParts = new List<WindowPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                var entids = JEntity.GetSelectedEntityIds();

                List<ObjectId> brids = new List<ObjectId>();
                if (entids == null)
                {
                    tr.Commit();
                    return null;
                }

                //Filter BlockReferenec 
                foreach (var id in entids)
                {
                    //Check 
                    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                    {
                        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                        brids.Add(id);
                    }
                }

                FormBlockPart.selids = brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    if (btr.IsLayout || btr.IsAnonymous) continue; // Layout과 Anonymous Block은 제외   
                    WindowPart jbtr = new WindowPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }

        public static List<WindowPart> GetAllBlockParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<WindowPart> blockParts = new List<WindowPart>();

            ResetCounter();

            List<WindowPart> parts = new List<WindowPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            // 문서 잠금 및 트랜잭션 시작
            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                //var entids = JEntity.GetSelectedEntityIds();

                //List<ObjectId> brids = new List<ObjectId>();
                //if (entids == null)
                //{
                //    tr.Commit();
                //    return null;
                //}

                ////Filter BlockReferenec 
                //foreach (var id in entids)
                //{
                //    //Check 
                //    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                //    {
                //        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                //        brids.Add(id);
                //    }
                //}

                var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("INSERT","Window"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (ents == null) return null;
                foreach (Entity ent in ents)
                {
                    blockrefs.Add(ent as BlockReference);
                }
                FormBlockPart.selids = blockrefs.Select(x => x.ObjectId).ToList();    //          brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    if (btr.IsLayout || btr.IsAnonymous) continue; // Layout과 Anonymous Block은 제외


                    var Img = btr.PreviewIcon?.GetThumbnailImage(128, 128, () => false, IntPtr.Zero);
                    if (Img == null) continue; // Img가 없는것은 제외
                    WindowPart jbtr = new WindowPart(btrId);//brg.First().BlockTableRecord);

                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;
                    //if (JXdata.GetXdata(brg.First(), "IsBlockCount") == "True") jbtr.IsBlockCount = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }

        public static List<BlockPart> GetZoneBlockParts(ComboBox cb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                //var entids = JEntity.GetSelectedEntityIds();

                //List<ObjectId> brids = new List<ObjectId>();
                //if (entids == null)
                //{
                //    tr.Commit();
                //    return null;
                //}

                ////Filter BlockReferenec 
                //foreach (var id in entids)
                //{
                //    //Check 
                //    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                //    {
                //        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                //        brids.Add(id);
                //    }
                //}

                var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("INSERT"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (ents == null) return null;
                foreach (Entity ent in ents)
                {
                    //Check Zone 
                    //var zn = JXdata.GetXdata(ent, "Zone");
                    //if (zn != cb.SelectedItem.ToString()) continue;
                    if (JXdata.GetXdata(ent, "Zone") == cb.Text)
                    {
                        blockrefs.Add(ent as BlockReference);
                    }
                }
                FormBlockPart.selids = blockrefs.Select(x => x.ObjectId).ToList();    //          brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }

                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }


    }

    public class RoomPart : INotifyPropertyChanged
    {
        private static int IdCounter = 1;
        private static List<DBText> Roomtxts = new List<DBText>(); // 도면 전체에 있는 Room Wall texts

        public int Index { get; set; }
        
        private Polyline Poly { get; set; }     //
        //public System.Drawing.Image Img { get; set; }      // picBoxBr.Image =   btr.PreviewIcon?.GetThumbnailImage(192, 192, () => false, IntPtr.Zero);

        public string Name { get; set; }  // Room Name
        public Double CeilingHeight { get; set; }   // 천장 높이
        public double FloorHeight { get; set; }   // 층고 높이
        public string RoofArea { get; set; } // 지붕  면적
        public string FloorArea { get; set; } // 바닥 면적
        public string Volumn { get; set; } // 룸체적 룸면적 x 층고
        public string WallText { get; set; } // 벽체 텍스트  

        private List<Line> WallLines { get; set; } // 벽 Line List
        private List<BlockReference> Windows { get; set; } // 창 List
        private List<BlockReference> Doors { get; set; } // 문 List

        //Roomtxts 지정   
        public static void SetRoomTexts(List<DBText> txts)
        {
            RoomPart.Roomtxts = txts;
        }

        // 생성자 
        public RoomPart(Polyline poly)
        {
            this.Index = IdCounter;
            this.Poly = poly;
            this.WallLines = poly.GetLines();
            this.FloorHeight = JXdata.GetXdata(poly, "FloorHeight").ToDouble();
            this.CeilingHeight = JXdata.GetXdata(poly, "CeilingHeight").ToDouble();
            var aa = poly.Area * double.E - 6;
            this.FloorArea = Math.Round(aa, 0).ToString();
            this.RoofArea = Math.Round(aa, 0).ToString();
            this.Volumn = Math.Round(aa* FloorHeight, 0).ToString(); 
            IdCounter++;
        }

        //
        public event PropertyChangedEventHandler PropertyChanged;

        //Function
        public Polyline GetPoly()
        {
            return this.Poly;
        }
        public static void ResetCounter()
        {
            IdCounter = 1;
        }

        //Static Function
        public static List<BlockPart> GetSelectedBlockParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                var entids = JEntity.GetSelectedEntityIds();

                List<ObjectId> brids = new List<ObjectId>();
                if (entids == null)
                {
                    tr.Commit();
                    return null;
                }

                //Filter BlockReferenec 
                foreach (var id in entids)
                {
                    //Check 
                    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                    {
                        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                        brids.Add(id);
                    }
                }

                FormBlockPart.selids = brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    if (btr.IsLayout || btr.IsAnonymous) continue; // Layout과 Anonymous Block은 제외   
                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }

        public static List<RoomPart> GetAllRoomParts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<RoomPart> blockParts = new List<RoomPart>();

            ResetCounter();

            List<RoomPart> rooms = new List<RoomPart>();
            //List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            // 문서 잠금 및 트랜잭션 시작
            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var polys = JEntity.GetEntityAllByTpye<Polyline>(JEntity.MakeSelFilter("LWPOLYLINE", "Room"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (polys == null) return null;
                foreach (var po in polys)
                {
                    var room = new RoomPart(po);
                    rooms.Add(room);
                    //blockrefs.Add(ent as BlockReference);
                }

            }
            return rooms;
        }

        public static List<BlockPart> GetZoneBlockParts(ComboBox cb)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            List<BlockPart> blockParts = new List<BlockPart>();

            ResetCounter();

            List<BlockPart> parts = new List<BlockPart>();
            List<BlockReference> blockrefs = new List<BlockReference>();
            //LightPart.ResetCounter();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                //var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var entids = JEntity.GetEntityAllByTpye(JEntity.MakeSelFilter("LINE,LWPOLYLINE,INSERT", "LightPart"));
                //var ents = JEntity.GetSelectedEntityByTpye<Entity>();
                //PreSelected Entity
                //var entids = JEntity.GetSelectedEntityIds();

                //List<ObjectId> brids = new List<ObjectId>();
                //if (entids == null)
                //{
                //    tr.Commit();
                //    return null;
                //}

                ////Filter BlockReferenec 
                //foreach (var id in entids)
                //{
                //    //Check 
                //    if (id.GetObject(OpenMode.ForRead).GetType() == typeof(BlockReference))
                //    {
                //        blockrefs.Add(id.GetObject(OpenMode.ForRead) as BlockReference);
                //        brids.Add(id);
                //    }
                //}

                var ents = JEntity.GetEntityAllByTpye<Entity>(JEntity.MakeSelFilter("INSERT"));                //(JEntity.MakeSelFilter("INSERT", "ElecPart"));
                if (ents == null) return null;
                foreach (Entity ent in ents)
                {
                    //Check Zone 
                    //var zn = JXdata.GetXdata(ent, "Zone");
                    //if (zn != cb.SelectedItem.ToString()) continue;
                    if (JXdata.GetXdata(ent, "Zone") == cb.Text)
                    {
                        blockrefs.Add(ent as BlockReference);
                    }
                }
                FormBlockPart.selids = blockrefs.Select(x => x.ObjectId).ToList();    //          brids;

                //Grouping Block
                //Dynamic Block이 있어서 Grouping에 문제가 있다.
                //Block Grouping 시  Block의 SymbolType을  같이 고려한다.
                var bgrGrps = blockrefs.GroupBy(x => x.GetName() + JXdata.GetXdata(x, "SymbolType") ?? "");

                foreach (var brg in bgrGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }

                    BlockPart jbtr = new BlockPart(btrId);//brg.First().BlockTableRecord);
                    //jbtr.Attach = "천장";
                    jbtr.Count = brg.Count();
                    jbtr.PartName = JXdata.GetXdata(brg.First(), "PartName");
                    jbtr.Type = JXdata.GetXdata(brg.First(), "Type");
                    //jbtr.Attach = JXdata.GetXdata(brg.First(), "Attatch");
                    jbtr.SymbolType = JXdata.GetXdata(brg.First(), "SymbolType");

                    //if (JXdata.GetXdata(brg.First(), "IsWire") == "True") jbtr.IsWire = true;
                    //if (JXdata.GetXdata(brg.First(), "IsReturn") == "True") jbtr.IsReturn = true;

                    blockParts.Add(jbtr);
                }

            }
            return blockParts;
        }


    }

}
