using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using NetWorkTime;
using ProgramLicenseManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class Room
    {
    
    }

    public class PolyRoomSet
    {
        #region Text To Room
        // 선택 Line Poly XData Wire 지정
        //TO DuctLine
        [CommandMethod("Text_Set_RoomText", CommandFlags.UsePickSet)] //ToRoom
                                                       //Text 에  Room 지정                                                    
        public void Cmd_Text_Set_RoomText()
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

        #region Poly To Room
        // 선택 Line Poly XData Wire 지정
        //TO DuctLine
        [CommandMethod(Jdf.Cmd.선택폴리룸지정, CommandFlags.UsePickSet)] //ToRoom
                                                                      //Text 에  Room 지정                                                    
        public void Cmd_Poly_Set_RoomPoly()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ////check license 
            //var isValid = LicenseHelper.CheckLicense();

            //if (isValid == false)
            //{
            //    ed.WriteMessage("\n라이선스가 유효하지 않습니다. 프로그램을 종료합니다.");
            //    return;
            //}

            //Check License Days

            //var days = LicenseHelper.GetDateTime();

            //ed.WriteMessage($"\n {days} ");

            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();

            //
            DateTime networkTime = NetworkTimeService.GetNetworkTime();
            if( networkTime > new DateTime(2026, 3, 2))
            {
                ed.WriteMessage("\n프로그램 사용 기간이 만료되었습니다. 관리자에게 문의하세요.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> targets = JEntityFunc.GetEntityByTpye<Polyline>("Room 대상  poly를  선택 하세요?", JSelFilter.MakeFilterTypes("LWPOLYLINE"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Arch,Room,Disp");
                //Create layerfor Room
                tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Magenta, LineWeight.ByLineWeightDefault);

                foreach (Entity acEnt in targets)
                {
                    LayerTableRecord layer = tr.GetObject(acEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (!layer.IsLocked)
                    {
                        var pl = (Polyline)acEnt;
                        if (pl.Closed != true) continue;// Open poly는 무시한다.
                        acEnt.UpgradeOpen();
                        pl.Layer = Jdf.Layer.Room;
                        JXdata.SetXdata(pl, "Arch", "Room");
                        JXdata.SetXdata(pl, "Room", "Room");
                        JXdata.SetXdata(pl, "Disp", "R");
                        //JXdata.SetXdata(pl, "Mat", "Hidden");
                    }

       

                    // Dispose of the transaction
                }

                // Save the new object to the database
                tr.Commit();
            }
        }

        #endregion

        #region Poly To Ceiling Height
        // 선택 Line Poly 천정고 지정
        [CommandMethod("To_CeilingHeight", CommandFlags.UsePickSet)] //ToRoom
                                                                      //Text 에  Room 지정                                                    
        public void Cmd_Poly_Set_CeilingHeight()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();

            // 정수 입력 받기
            PromptDoubleOptions pio = new PromptDoubleOptions("\n천정높이(Meter)를 입력하세요: ");

            // 옵션 설정 (선택사항)
            pio.AllowNegative = true;  // 음수 허용
            pio.AllowZero = true;      // 0 허용
            pio.AllowNone = false;     // Enter만 누르는 것 방지

            // 기본값 설정 (선택사항)
            pio.DefaultValue = 10;
            pio.UseDefaultValue = true;

            PromptDoubleResult pir = ed.GetDouble(pio);

            double userValue = 0;
            if (pir.Status == PromptStatus.OK)
            {
                userValue = pir.Value;
                ed.WriteMessage($"\n입력된 값: {userValue}");
            }
            else if (pir.Status == PromptStatus.Cancel)
            {
                ed.WriteMessage("\n입력이 취소되었습니다.");
                return;
            }



            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> targets = JEntityFunc.GetEntityByTpye<Polyline>("Room 대상  poly를  선택 하세요?", JSelFilter.MakeFilterTypes("LWPOLYLINE"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Arch,CeilingHeight,Disp");
                //Create layerfor Room
                tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Magenta, LineWeight.LineWeight050);

                foreach (Entity acEnt in targets)
                {
                    LayerTableRecord layer = tr.GetObject(acEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (!layer.IsLocked)
                    {
                        var pl = (Polyline)acEnt;
                        if (pl.Closed != true) continue;// Open poly는 무시한다.
                        acEnt.UpgradeOpen();
                        pl.Layer = Jdf.Layer.Room;
                        //JXdata.SetXdata(pl, "Arch", "CeilingHeight");
                        JXdata.SetXdata(pl, "CeilingHeight", $"{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Disp", $"Ceiling:{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Mat", "Hidden");
                    }



                    // Dispose of the transaction
                }

                // Save the new object to the database
                tr.Commit();
            }
        }

        #endregion

        #region Poly To Floor Height
        // 선택 Line Poly 천정고 지정
        [CommandMethod("To_FloorHeight", CommandFlags.UsePickSet)] //ToRoom
                                                                     //Text 에  Room 지정                                                    
        public void Cmd_Poly_Set_FloorHeight()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();

            // 정수 입력 받기
            PromptDoubleOptions pio = new PromptDoubleOptions("\n층고높이(Meter)를 입력하세요: ");

            // 옵션 설정 (선택사항)
            pio.AllowNegative = true;  // 음수 허용
            pio.AllowZero = true;      // 0 허용
            pio.AllowNone = false;     // Enter만 누르는 것 방지

            // 기본값 설정 (선택사항)
            pio.DefaultValue = 10;
            pio.UseDefaultValue = true;

            PromptDoubleResult pir = ed.GetDouble(pio);

            double userValue = 0;
            if (pir.Status == PromptStatus.OK)
            {
                userValue = pir.Value;
                ed.WriteMessage($"\n입력된 값: {userValue}");
            }
            else if (pir.Status == PromptStatus.Cancel)
            {
                ed.WriteMessage("\n입력이 취소되었습니다.");
                return;
            }



            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> targets = JEntityFunc.GetEntityByTpye<Polyline>("Room 대상  poly를  선택 하세요?", JSelFilter.MakeFilterTypes("LWPOLYLINE"));//, JSelFilter.MakeFilterTypes("LINE,POLYLINE,LWPOYLINE"));   //DBText-> Text  Mtext-> Mtext  
                if (targets == null) return;
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Arch,FloorHeight,Disp");
                //Create layerfor Room
                tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Magenta, LineWeight.LineWeight050);

                foreach (Entity acEnt in targets)
                {
                    LayerTableRecord layer = tr.GetObject(acEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (!layer.IsLocked)
                    {
                        var pl = (Polyline)acEnt;
                        if (pl.Closed != true) continue;// Open poly는 무시한다.
                        acEnt.UpgradeOpen();
                        pl.Layer = Jdf.Layer.Room;
                        //JXdata.SetXdata(pl, "Arch", "CeilingHeight");
                        JXdata.SetXdata(pl, "FloorHeight", $"{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Disp", $"Ceiling:{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Mat", "Hidden");
                    }



                    // Dispose of the transaction
                }

                // Save the new object to the database
                tr.Commit();
            }
        }

        #endregion

        #region Block To  Height
        // 선택 Block Height 지정   
        [CommandMethod("HH", CommandFlags.UsePickSet)] //ToRoom
                                                                   //Text 에  Room 지정                                                    
        public void Cmd_Block_Set_Height()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();

            // 정수 입력 받기
            PromptDoubleOptions pio = new PromptDoubleOptions("\n높이(Meter)를 입력하세요: ");

            // 옵션 설정 (선택사항)
            pio.AllowNegative = false;  // 음수 허용
            pio.AllowZero = true;      // 0 허용
            pio.AllowNone = false;     // Enter만 누르는 것 방지

            // 기본값 설정 (선택사항)
            pio.DefaultValue = 10;
            pio.UseDefaultValue = true;

            PromptDoubleResult pir = ed.GetDouble(pio);

            double userValue = 0;
            if (pir.Status == PromptStatus.OK)
            {
                userValue = pir.Value;
                ed.WriteMessage($"\n입력된 값: {userValue}");
            }
            else if (pir.Status == PromptStatus.Cancel)
            {
                ed.WriteMessage("\n입력이 취소되었습니다.");
                return;
            }



            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<BlockReference> targets = JEntityFunc.GetEntityByTpye<BlockReference>("WIndow or Door 대상  Block 를  선택 하세요?", JSelFilter.MakeFilterTypes("INSERT"));
                if (targets == null) return;
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Arch,FloorHeight,Disp,Height");
                //Create layerfor Room
                tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Magenta, LineWeight.LineWeight050);

                foreach (Entity acEnt in targets)
                {
                    LayerTableRecord layer = tr.GetObject(acEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    if (!layer.IsLocked)
                    {
                        acEnt.UpgradeOpen();
                        JXdata.SetXdata(acEnt, "Height", $"{userValue.ToString()}");
                        //Get Disp value
                        var disp = JXdata.GetXdata(acEnt, "Disp") ?? "";
                        JXdata.SetXdata(acEnt, "Disp", $" {disp}:H{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Disp", $"Ceiling:{userValue.ToString()}");
                        //JXdata.SetXdata(pl, "Mat", "Hidden");
                    }



                    // Dispose of the transaction
                }

                // Save the new object to the database
                tr.Commit();
            }
        }

        #endregion

    }

    public class RoomOutWall
    {
        #region Define Out Wall  
        // Line 또는 Poly를 OutWall 로   
        //TO OutWall
        [CommandMethod("OutWall", CommandFlags.UsePickSet)] 
                                                                         
        public void Cmd_OutWall_LIne()
        {
            // Get the current database and start a transaction
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;


            //UCS Elevation to World
            doc.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity;
            doc.Editor.Regen();

            //
            DateTime networkTime = NetworkTimeService.GetNetworkTime();
            if (networkTime > new DateTime(2026, 3, 2))
            {
                ed.WriteMessage("\n프로그램 사용 기간이 만료되었습니다. 관리자에게 문의하세요.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var btr = tr.GetModelSpaceBlockTableRecord(db);
                //사용할 XData 미리 Check
                tr.ChecRegNames(db, "Arch,Room,Disp,OutWall");
                //Create layerfor Room
                tr.CreateLayer(Jdf.Layer.OutWall, Jdf.Color.Cyan, LineWeight.LineWeight040);

                // 1. 레이어 테이블 열기
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                var targetLayerName = Jdf.Layer.OutWall;    
                // 2. 해당 레이어가 존재하는지 확인
                if (lt.Has(targetLayerName))
                {
                    // 3. 레이어 ID 가져오기
                    ObjectId layerId = lt[targetLayerName];

                    // 4. 현재 레이어(Clayer)로 설정 (핵심 부분)
                    db.Clayer = layerId;

                    ed.WriteMessage($"\n현재 레이어가 '{targetLayerName}'(으)로 변경되었습니다.");
                }
                else
                {
                    ed.WriteMessage($"\n오류: '{targetLayerName}' 레이어를 찾을 수 없습니다.");
                }

                // Save the new object to the database
                tr.Commit();
            }
        }

        #endregion

    }

    public class RoomCalc
    {
        public static Line northVectorDrawing = new Line (new Point3d (0,0,0),new Point3d(0,10,0));
        /// <summary>
        /// 방향 벡터를 기준으로 선택된 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택라인방위각)]
        public void Cmd_LineDir_AnalyzeLineDirection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 북쪽 방향 벡터를 나타내는 line 선택
                Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요 (시작점→끝점이 북쪽 방향): ");
                northVectorDrawing = northVectorLine;
                if (northVectorLine == null)
                {
                    ed.WriteMessage("\n북쪽 방향 벡터 선택이 취소되었습니다.");
                    return;
                }

                // 2단계: 방향을 분석할 line 선택
                Line targetLine = SelectLine(ed, "\n방향을 분석할 line을 선택하세요: ");
                if (targetLine == null)
                {
                    ed.WriteMessage("\n분석 대상 line 선택이 취소되었습니다.");
                    return;
                }

                // 3단계: 방향 분석 수행
                var directionResult = AnalyzeDirectionRelativeToNorth(northVectorDrawing, targetLine);

                // 4단계: 결과 출력
                ed.WriteMessage($"\n=== 방향 분석 결과 (기준각: ±10°) ===");
                ed.WriteMessage($"\n북쪽 기준 벡터 각도: {directionResult.northAngle:F2}°");
                ed.WriteMessage($"\n분석 대상 line 각도: {directionResult.targetAngle:F2}°");
                ed.WriteMessage($"\n상대 각도: {directionResult.relativeAngle:F2}°");
                ed.WriteMessage($"\n방향: {directionResult.direction}");
                ed.WriteMessage($"\n방향 설명: {GetDirectionDescription(directionResult.direction)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 방향 벡터를 기준으로 선택된 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// northVectorDrawing 이용해서 방위각 분석 
        /// </summary>
        [CommandMethod("LineDir1")] 
        public void Cmd_LineDir_AnalyzeLineDirection1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                //// 1단계: 북쪽 방향 벡터를 나타내는 line 선택
                //Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요 (시작점→끝점이 북쪽 방향): ");
                //northVectorDrawing = northVectorLine;
                //if (northVectorLine == null)
                //{
                //    ed.WriteMessage("\n북쪽 방향 벡터 선택이 취소되었습니다.");
                //    return;
                //}

                // 2단계: 방향을 분석할 line 선택
                Line targetLine = SelectLine(ed, "\n방향을 분석할 line을 선택하세요: ");
                if (targetLine == null)
                {
                    ed.WriteMessage("\n분석 대상 line 선택이 취소되었습니다.");
                    return;
                }

                // 3단계: 방향 분석 수행
                var directionResult = AnalyzeDirectionRelativeToNorth(northVectorDrawing, targetLine);

                // 4단계: 결과 출력
                ed.WriteMessage($"\n=== 방향 분석 결과 (기준각: ±10°) ===");
                ed.WriteMessage($"\n북쪽 기준 벡터 각도: {directionResult.northAngle:F2}°");
                ed.WriteMessage($"\n분석 대상 line 각도: {directionResult.targetAngle:F2}°");
                ed.WriteMessage($"\n상대 각도: {directionResult.relativeAngle:F2}°");
                ed.WriteMessage($"\n방향: {directionResult.direction}");
                ed.WriteMessage($"\n방향 설명: {GetDirectionDescription(directionResult.direction)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Polyline 내부에 있는 Line 객체 찾기
        /// 방향 벡터를 기준으로 선택된 각 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// </summary>
        [CommandMethod(Jdf.Cmd.AnalyzePolyLineDirection)]
        public void Cmd_PolyDir_LinesDirection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. 폴리라인 선택
                PromptEntityOptions polyOptions = new PromptEntityOptions("\n폴리라인을 선택하세요: ");
                polyOptions.SetRejectMessage("\n선택된 객체가 폴리라인이 아닙니다.");
                polyOptions.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult polyResult = ed.GetEntity(polyOptions);
                if (polyResult.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    // 2. 폴리라인 객체 가져오기
                    Polyline poly = tr.GetObject(polyResult.ObjectId, OpenMode.ForRead) as Polyline;
                    if (poly == null)
                    {
                        ed.WriteMessage("\n폴리라인을 읽을 수 없습니다.");
                        return;
                    }

                    // 3. 폴리라인이 닫혀있는지 확인
                    if (!poly.Closed)
                    {
                        ed.WriteMessage("\n폴리라인이 닫혀있지 않습니다. 닫힌 폴리라인만 처리할 수 있습니다.");
                        return;
                    }

                    // 4. 폴리라인 내부에 있는 라인들 찾기
                    var lines = poly.GetLines();
                        lines = lines.OrderBy(x=> x.Length).ToList(); // 길이순 정렬
                    var  cp = poly.CenterPoint();

                    if (lines.Count == 0)
                    {
                        ed.WriteMessage("\n폴리라인 내부에 라인이 없습니다.");
                        tr.Commit();
                        return;
                    }

                    // line 방위각 분석
                    var northVecor = new Line(cp, new Point3d(cp.X, cp.Y + 10, cp.Z));
                    foreach (var line in lines)
                    {
                        var cp1 = line.GetClosestPointTo(cp, false);
                        var lineVec  = new Line(cp,cp1);
                        var dir = AnalyzeDirectionRelativeToNorth(northVecor, lineVec);

                        DBText textEntity = new DBText();
                        textEntity.Position = line.GetPointAtDist(line.Length/2);
                        textEntity.Height = lines[0].Length/20;
                        textEntity.TextString = dir.direction.ToString()+":"+((int)line.Length).ToString();
                        textEntity.SetDatabaseDefaults();

                        btr.AppendEntity(textEntity);
                        tr.AddNewlyCreatedDBObject(textEntity, true);

                        //ed.WriteMessage($"\n라인 시작점: ({line.StartPoint.X:F3}, {line.StartPoint.Y:F3})");
                        //ed.WriteMessage($", 끝점: ({line.EndPoint.X:F3}, {line.EndPoint.Y:F3})");
                        //ed.WriteMessage($", 길이: {line.Length:F3}");
                        //ed.WriteMessage($", 각도: {dir.targetAngle:F2}°");
                        //ed.WriteMessage($", 방향: {dir.direction}");
                        //ed.WriteMessage($", 설명: {GetDirectionDescription(dir.direction)}");
                    }   



                    ed.WriteMessage($"\n폴리라인 내부에서 {lines.Count}개의 라인을 찾았습니다.");

                    // 5. 사용자 확인


                    ed.WriteMessage($"\n{lines.Count}개의 라인이 성공적으로 분석 되었습니다.");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// Room Polyline 내부에 있는 Line 분석 Text로 기록 
        /// 방향 벡터를 기준으로 선택된 각 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택폴리룸계산)]
        public void Cmd_RoomPoly_Calc()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1. 폴리라인 선택
                PromptEntityOptions polyOptions = new PromptEntityOptions("\n폴리라인을 선택하세요: ");
                polyOptions.SetRejectMessage("\n선택된 객체가 폴리라인이 아닙니다.");
                polyOptions.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult polyResult = ed.GetEntity(polyOptions);
                if (polyResult.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,RoomText,Handle,LL,Dir,LEN,RoomCalc"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    // 2. 폴리라인 객체 가져오기
                    Polyline poly = tr.GetObject(polyResult.ObjectId, OpenMode.ForRead) as Polyline;
                    if (poly == null)
                    {
                        ed.WriteMessage("\n폴리라인을 읽을 수 없습니다.");
                        return;
                    }

                    // 3. 폴리라인이 닫혀있는지 확인
                    if (!poly.Closed)
                    {
                        ed.WriteMessage("\n폴리라인이 닫혀있지 않습니다. 닫힌 폴리라인만 처리할 수 있습니다.");
                        return;
                    }

                    // 4. 폴리라인 내부에 있는 라인들 찾기
                    var lines = poly.GetLines();
                    lines = lines.OrderBy(x => x.Length).ToList(); // 길이순 정렬
                    var cp = poly.CenterPoint();

                    if (lines.Count == 0)
                    {
                        ed.WriteMessage("\n폴리라인 내부에 라인이 없습니다.");
                        tr.Commit();
                        return;
                    }

                    // line 방위각 분석
                    var northVecor = new Line(cp, new Point3d(cp.X, cp.Y + 10, cp.Z));
                    // 길이가 10 이하인 라인은 무시    
                    // poly 만들때  첫번째 point 와 마지막 point 가 같은 경우가 발생하여  길이가 0 인 line 이 발생한다.      
                    lines = lines.Where(x => x.Length >= 10).ToList();
                    var lineAvglength = lines.Average(x => x.Length);

                    //roompoly 계산완료 표시  
                    JXdata.SetXdata(poly, "RoomCalc", "RoomCalc");

                    foreach (var line in lines)
                    {
                        //if (line.Length < 10) continue;
                        //line의 X 위치가 적은 것을 StartPoint 로 지정한다.
                        // X 위차가 같으면 Y가 적은 것을 StartPoint 로 지정 한다.

                        if (line.EndPoint.X < line.StartPoint.X)
                        { 
                            var tmp = line.StartPoint;    
                            line.StartPoint = line.EndPoint;
                            line.EndPoint = tmp;                        
                        }

                        if (line.EndPoint.X == line.StartPoint.X)
                        {
                            if(line.EndPoint.Y < line.StartPoint.Y)
                            {
                                var tmp = line.StartPoint;
                                line.StartPoint = line.EndPoint;
                                line.EndPoint = tmp;
                            }   
                        }


                        var cp1 = line.GetClosestPointTo(cp, false);
                        var lineDirection = new Line(cp, cp1);
                        var lineVec2 = lineDirection.GetVector(); 
                        var dir = AnalyzeDirectionRelativeToNorth(northVecor, lineDirection);

                        DBText textEntity = new DBText();               
                        textEntity.Height = lineAvglength / 30;
                        textEntity.Position = line.GetPointAtDist(line.Length / 2) - lineVec2 * lineAvglength / 12;
                        var lineText = dir.direction.ToString() + ":" + (Math.Round(line.Length)).ToString();
                        textEntity.TextString = lineText;
                        textEntity.Rotation = line.Angle;
                        textEntity.HorizontalMode = TextHorizontalMode.TextCenter;
                        textEntity.VerticalMode = TextVerticalMode.TextVerticalMid;
                        textEntity.AlignmentPoint = line.GetPointAtDist(line.Length / 2) - lineVec2 * lineAvglength / 12;
                        textEntity.SetDatabaseDefaults();
                        textEntity.Layer = Jdf.Layer.Room;

                        //Set Xdata
                        JXdata.SetXdata(textEntity, "Arch", "RoomText");
                        JXdata.SetXdata(textEntity, "RoomText", "RoomText");
                        JXdata.SetXdata(textEntity, "Handle", poly.Handle.ToString());  
                        JXdata.SetXdata(textEntity, "LL", line.ToStr());
                        JXdata.SetXdata(textEntity, "Dir", dir.direction.ToString());
                        JXdata.SetXdata(textEntity, "LEN", (Math.Round(line.Length)).ToString());

                        btr.AppendEntity(textEntity);
                        tr.AddNewlyCreatedDBObject(textEntity, true);

                        //ed.WriteMessage($"\n라인 시작점: ({line.StartPoint.X:F3}, {line.StartPoint.Y:F3})");
                        //ed.WriteMessage($", 끝점: ({line.EndPoint.X:F3}, {line.EndPoint.Y:F3})");
                        //ed.WriteMessage($", 길이: {line.Length:F3}");
                        //ed.WriteMessage($", 각도: {dir.targetAngle:F2}°");
                        //ed.WriteMessage($", 방향: {dir.direction}");
                        //ed.WriteMessage($", 설명: {GetDirectionDescription(dir.direction)}");
                    }



                    ed.WriteMessage($"\n폴리라인 내부에서 {lines.Count}개의 라인을 찾았습니다.");

                    // 5. 사용자 확인


                    ed.WriteMessage($"\n{lines.Count}개의 라인이 성공적으로 분석 되었습니다.");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



        /// <summary>
        /// Room Polyline 내부에 있는 Line 분석 Text로 기록 
        /// 방향 벡터를 기준으로 선택된 각 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// </summary>
        [CommandMethod(Jdf.Cmd.룸폴리전체계산)]
        public void Cmd_RoomPoly_All_Calc()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<Polyline> targets = JEntity.GetEntityByTpye<Polyline>("Room Poly 를 선택 하세요?", JSelFilter.MakeFilterTypesRegs("LWPOLYLINE", "Room"));
                    if (targets.Count() == 0) return;

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,RoomText,Handle,LL,Dir,LEN"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var poly in targets)
                    {
                        // 2. 폴리라인 객체 가져오기
                        //Polyline poly = tr.GetObject(polyResult.ObjectId, OpenMode.ForRead) as Polyline;
                        if (poly == null)
                        {
                            ed.WriteMessage("\n폴리라인을 읽을 수 없습니다.");
                            continue;
                        }

                        // 3. 폴리라인이 닫혀있는지 확인
                        if (!poly.Closed)
                        {
                            ed.WriteMessage("\n폴리라인이 닫혀있지 않습니다. 닫힌 폴리라인만 처리할 수 있습니다.");
                            continue;
                        }

                        // 4. 폴리라인 내부에 있는 라인들 찾기
                        var lines = poly.GetLines();
                        lines = lines.OrderBy(x => x.Length).ToList(); // 길이순 정렬
                        var cp = poly.CenterPoint();

                        if (lines.Count == 0)
                        {
                            ed.WriteMessage("\n폴리라인 내부에 라인이 없습니다.");
                            tr.Commit();
                            return;
                        }

                        // line 방위각 분석
                        var northVecor = new Line(cp, new Point3d(cp.X, cp.Y + 10, cp.Z));
                        // 길이가 10 이하인 라인은 무시    
                        // poly 만들때  첫번째 point 와 마지막 point 가 같은 경우가 발생하여  길이가 0 인 line 이 발생한다.      
                        lines = lines.Where(x => x.Length >= 10).ToList();
                        var lineAvglength = lines.Average(x => x.Length);


                        foreach (var line in lines)
                        {
                            //if (line.Length < 10) continue;
                            //line의 X 위치가 적은 것을 StartPoint 로 지정한다.
                            // X 위차가 같으면 Y가 적은 것을 StartPoint 로 지정 한다.

                            if (line.EndPoint.X < line.StartPoint.X)
                            {
                                var tmp = line.StartPoint;
                                line.StartPoint = line.EndPoint;
                                line.EndPoint = tmp;
                            }

                            if (line.EndPoint.X == line.StartPoint.X)
                            {
                                if (line.EndPoint.Y < line.StartPoint.Y)
                                {
                                    var tmp = line.StartPoint;
                                    line.StartPoint = line.EndPoint;
                                    line.EndPoint = tmp;
                                }
                            }


                            var cp1 = line.GetClosestPointTo(cp, false);
                            var lineDirection = new Line(cp, cp1);
                            var lineVec2 = lineDirection.GetVector();
                            var dir = AnalyzeDirectionRelativeToNorth(northVecor, lineDirection);

                            DBText textEntity = new DBText();
                            textEntity.Height = lineAvglength / 30;
                            textEntity.Position = line.GetPointAtDist(line.Length / 2) - lineVec2 * lineAvglength / 12;
                            var lineText = dir.direction.ToString() + ":" + (Math.Round(line.Length)).ToString();
                            textEntity.TextString = lineText;
                            textEntity.Rotation = line.Angle;
                            textEntity.HorizontalMode = TextHorizontalMode.TextCenter;
                            textEntity.VerticalMode = TextVerticalMode.TextVerticalMid;
                            textEntity.AlignmentPoint = line.GetPointAtDist(line.Length / 2) - lineVec2 * lineAvglength / 12;
                            textEntity.SetDatabaseDefaults();
                            textEntity.Layer = Jdf.Layer.Room;

                            //Set Xdata
                            JXdata.SetXdata(textEntity, "Arch", "RoomText");
                            JXdata.SetXdata(textEntity, "RoomText", "RoomText");
                            JXdata.SetXdata(textEntity, "Handle", poly.Handle.ToString());
                            JXdata.SetXdata(textEntity, "LL", line.ToStr());
                            JXdata.SetXdata(textEntity, "Dir", dir.direction.ToString());
                            JXdata.SetXdata(textEntity, "LEN", (Math.Round(line.Length)).ToString());

                            btr.AppendEntity(textEntity);
                            tr.AddNewlyCreatedDBObject(textEntity, true);

                            //ed.WriteMessage($"\n라인 시작점: ({line.StartPoint.X:F3}, {line.StartPoint.Y:F3})");
                            //ed.WriteMessage($", 끝점: ({line.EndPoint.X:F3}, {line.EndPoint.Y:F3})");
                            //ed.WriteMessage($", 길이: {line.Length:F3}");
                            //ed.WriteMessage($", 각도: {dir.targetAngle:F2}°");
                            //ed.WriteMessage($", 방향: {dir.direction}");
                            //ed.WriteMessage($", 설명: {GetDirectionDescription(dir.direction)}");
                        }
                        ed.WriteMessage($"\n폴리라인 내부에서 {lines.Count}개의 라인을 찾았습니다.");

                        // 5. 사용자 확인
                        ed.WriteMessage($"\n{lines.Count}개의 라인이 성공적으로 분석 되었습니다.");

                    }


                    tr.Commit();
                }
               
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



        /// <summary>
        /// 선택 Room Text  제거
        /// </summary>
        [CommandMethod(Jdf.Cmd.룸텍스트제거)]
        public void Cmd_Room_texts_Delete()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<DBText> targets = JEntity.GetEntityByTpye<DBText>("Room Text 를 선택 하세요?", JSelFilter.MakeFilterTypesRegs("TEXT", "RoomText"));
                    if (targets.Count() == 0) return;

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    foreach (var txt in targets)
                    {

                        if (txt == null)
                        {
                            ed.WriteMessage("\n폴리라인을 읽을 수 없습니다.");
                            continue;
                        }
                        txt.UpgradeOpen();
                        txt.Erase();

                        // 5. 사용자 확인
                        ed.WriteMessage($"\n{targets.Count}개의 룸 텍스트가  성공적으로 제거 되었습니다.");

                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



        /// <summary>
        /// 커스텀 허용 각도로 방향 분석하는 추가 커맨드
        /// </summary>
        [CommandMethod("room_Dir_Cus")]
        public void Cmd_AnalyzeLineDirectionWithCustomRange()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 방향 구분 기준각 입력
                var anglePrompt = new PromptDoubleOptions($"\n기준각을 입력하세요 (기본값: 10°): ")
                {
                    DefaultValue = 10.0,
                    AllowNegative = false,
                    AllowZero = false
                };

                var angleResult = ed.GetDouble(anglePrompt);
                if (angleResult.Status != PromptStatus.OK)
                    return;

                double baseAngle = angleResult.Value;

                // line 선택
                Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요: ");
                if (northVectorLine == null) return;

                Line targetLine = SelectLine(ed, "\n방향을 분석할 line을 선택하세요: ");
                if (targetLine == null) return;

                // 방향 분석
                var directionResult = AnalyzeDirectionRelativeToNorth(northVectorLine, targetLine, baseAngle);

                // 결과 출력
                ed.WriteMessage($"\n=== 커스텀 방향 분석 결과 (기준각: ±{baseAngle}°) ===");
                ed.WriteMessage($"\n북쪽 기준 벡터 각도: {directionResult.northAngle:F2}°");
                ed.WriteMessage($"\n분석 대상 line 각도: {directionResult.targetAngle:F2}°");
                ed.WriteMessage($"\n상대 각도: {directionResult.relativeAngle:F2}°");
                ed.WriteMessage($"\n방향: {directionResult.direction}");
                ed.WriteMessage($"\n방향 설명: {GetDirectionDescription(directionResult.direction)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 선택 메서드
        /// </summary>
        private Line SelectLine(Editor ed, string prompt)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nLine 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return null;

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                tr.Commit();
                return line;
            }
        }

        /// <summary>
        /// 북쪽 방향 벡터를 기준으로 대상 line의 방향을 분석
        /// </summary>
        public static DirectionResult AnalyzeDirectionRelativeToNorth(Line northVectorLine, Line targetLine, double baseAngle = 22.5)
        {
            // 북쪽 방향 벡터 계산 (시작점 → 끝점)
            Vector3d northVector = northVectorLine.EndPoint - northVectorLine.StartPoint;

            // 대상 line의 방향 벡터 계산 (시작점 → 끝점)  
            Vector3d targetVector = targetLine.EndPoint - targetLine.StartPoint;

            // 벡터를 단위벡터로 정규화 (Vector3d는 자동으로 길이가 있으므로 직접 계산)
            if (northVector.Length > 0)
                northVector = northVector / northVector.Length;
            if (targetVector.Length > 0)
                targetVector = targetVector / targetVector.Length;

            // 각도를 도(degree) 단위로 계산
            double northAngle = GetAngleInDegrees(northVector);
            double targetAngle = GetAngleInDegrees(targetVector);

            // 상대 각도 계산 (북쪽을 0°로 기준)
            double relativeAngle = targetAngle - northAngle;

            // 각도를 -180° ~ 180° 범위로 정규화
            relativeAngle = NormalizeAngleToSignedRange(relativeAngle);

            // 방향 결정 (기본 기준각 10도 사용)
            DirectionType direction = DetermineDirection(relativeAngle, baseAngle);

            return new DirectionResult
            {
                northAngle = northAngle,
                targetAngle = targetAngle,
                relativeAngle = relativeAngle,
                direction = direction
            };
        }

        /// <summary>
        /// Vector3d를 도(degree) 단위 각도로 변환
        /// </summary>
        private static double GetAngleInDegrees(Vector3d vector)
        {
            // XY 평면에서의 각도 계산 (Z 축은 무시)
            double angleRad = Math.Atan2(vector.Y, vector.X);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // 0° ~ 360° 범위로 변환
            if (angleDeg < 0)
                angleDeg += 360.0;

            return angleDeg;
        }

        /// <summary>
        /// 각도를 -180° ~ 180° 범위로 정규화
        /// </summary>
        private static double NormalizeAngleToSignedRange(double angle)
        {
            while (angle > 180.0)
                angle -= 360.0;
            while (angle <= -180.0)
                angle += 360.0;
            return angle;
        }

        /// <summary>
        /// 상대 각도에 따른 방향 결정 (기준각 d=10도 사용)
        /// </summary>
        private static DirectionType DetermineDirection(double relativeAngle, double baseAngle = 10.0)
        {
            // 1단계: 먼저 주요 방향(N, E, S, W) 확인

            // N: -10 ~ 10도
            if (relativeAngle >= -baseAngle && relativeAngle <= baseAngle)
                return DirectionType.N;

            // E: 80 ~ 100도 (90±10)
            if (relativeAngle >= (90 - baseAngle) && relativeAngle <= (90 + baseAngle))
                return DirectionType.W;

            // S: 170 ~ -170도 (180±10, 170도 이상이거나 -170도 이하)
            if (relativeAngle >= (180 - baseAngle) || relativeAngle <= (-180 + baseAngle))
                return DirectionType.S;

            // W: -100 ~ -80도 (-90±10)
            if (relativeAngle >= (-90 - baseAngle) && relativeAngle <= (-90 + baseAngle))
                return DirectionType.E;

            // 2단계: 주요 방향이 아니면 중간 방향 확인

            // NW: 10 ~ 170도 (단, E와 S 범위 제외)
            if (relativeAngle > baseAngle && relativeAngle < (90 - baseAngle))
            {
                return DirectionType.NW;
            }

            // NE: -10 ~ -100도 (단, W 범위 제외)  
            if (relativeAngle < -baseAngle && relativeAngle > (-90 + baseAngle))
            {
                return DirectionType.NE;
            }

            // SW: 100 ~ 170도 (단, S 범위 제외)
            if (relativeAngle > (90 + baseAngle) && relativeAngle < (180 - baseAngle))
                return DirectionType.SW;

            // SE: -100 ~ -170도 (단, W와 S 범위 제외) 
            if (relativeAngle < (-90 - baseAngle) && relativeAngle > (-180 + baseAngle))
                return DirectionType.SE;

            // 기본값 (예외 상황)
            return DirectionType.N;
        }

        /// <summary>
        /// 방향 설명 반환
        /// </summary>
        private string GetDirectionDescription(DirectionType direction)
        {
            return direction switch
            {
                DirectionType.N => "북쪽 (North) - 정확히 북쪽 방향",
                DirectionType.NE => "북동쪽 (Northeast) - 북쪽에서 동쪽으로 기울어진 방향",
                DirectionType.E => "동쪽 (East) - 정확히 동쪽 방향",
                DirectionType.SE => "남동쪽 (Southeast) - 남쪽에서 동쪽으로 기울어진 방향",
                DirectionType.S => "남쪽 (South) - 정확히 남쪽 방향",
                DirectionType.SW => "남서쪽 (Southwest) - 남쪽에서 서쪽으로 기울어진 방향",
                DirectionType.W => "서쪽 (West) - 정확히 서쪽 방향",
                DirectionType.NW => "북서쪽 (Northwest) - 북쪽에서 서쪽으로 기울어진 방향",
                _ => "알 수 없는 방향"
            };
        }

        /// <summary>
        /// 상세한 방향 분석 정보 출력 (기준각 10도 사용)
        /// </summary>
        [CommandMethod("ANALYZE_DIRECTION_DETAIL")]
        public void Cmd_ShowDetailedDirectionAnalysis()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요: ");
                if (northVectorLine == null) return;

                Line targetLine = SelectLine(ed, "\n분석할 line을 선택하세요: ");
                if (targetLine == null) return;

                var result = AnalyzeDirectionRelativeToNorth(northVectorLine, targetLine);

                // 상세 정보 출력
                ed.WriteMessage($"\n=== 상세 방향 분석 결과 ===");
                ed.WriteMessage($"\n북쪽 기준 벡터:");
                ed.WriteMessage($"  시작점: ({northVectorLine.StartPoint.X:F3}, {northVectorLine.StartPoint.Y:F3})");
                ed.WriteMessage($"  끝점: ({northVectorLine.EndPoint.X:F3}, {northVectorLine.EndPoint.Y:F3})");
                ed.WriteMessage($"  길이: {northVectorLine.Length:F3}");
                ed.WriteMessage($"  각도: {result.northAngle:F2}°");

                ed.WriteMessage($"\n분석 대상 line:");
                ed.WriteMessage($"  시작점: ({targetLine.StartPoint.X:F3}, {targetLine.StartPoint.Y:F3})");
                ed.WriteMessage($"  끝점: ({targetLine.EndPoint.X:F3}, {targetLine.EndPoint.Y:F3})");
                ed.WriteMessage($"  길이: {targetLine.Length:F3}");
                ed.WriteMessage($"  각도: {result.targetAngle:F2}°");

                ed.WriteMessage($"\n방향 분석:");
                ed.WriteMessage($"  상대 각도: {result.relativeAngle:F2}°");
                ed.WriteMessage($"  판정된 방향: {result.direction}");
                ed.WriteMessage($"  방향 설명: {GetDirectionDescription(result.direction)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Norh Vector 설정 
        /// </summary>
        [CommandMethod("Set_North_Vector")]
        public void Cmd_Set_North_Vector()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요: ");
                if (northVectorLine == null) return;

                //Line targetLine = SelectLine(ed, "\n분석할 line을 선택하세요: ");
                //if (targetLine == null) return;

                //var result = AnalyzeDirectionRelativeToNorth(northVectorLine, targetLine);

                //Set nothVector RoomCalc  static north vector
                northVectorDrawing = northVectorLine;   

                // 상세 정보 출력
                ed.WriteMessage($"\n=== 상세 방향 분석 결과 ===");
                ed.WriteMessage($"\n북쪽 기준 벡터:");
                ed.WriteMessage($"  시작점: ({northVectorLine.StartPoint.X:F3}, {northVectorLine.StartPoint.Y:F3})");
                ed.WriteMessage($"  끝점: ({northVectorLine.EndPoint.X:F3}, {northVectorLine.EndPoint.Y:F3})");
                ed.WriteMessage($"  길이: {northVectorLine.Length:F3}");
                //ed.WriteMessage($"  각도: {result.northAngle:F2}°");

                //ed.WriteMessage($"\n분석 대상 line:");
                //ed.WriteMessage($"  시작점: ({targetLine.StartPoint.X:F3}, {targetLine.StartPoint.Y:F3})");
                //ed.WriteMessage($"  끝점: ({targetLine.EndPoint.X:F3}, {targetLine.EndPoint.Y:F3})");
                //ed.WriteMessage($"  길이: {targetLine.Length:F3}");
                //ed.WriteMessage($"  각도: {result.targetAngle:F2}°");

                //ed.WriteMessage($"\n방향 분석:");
                //ed.WriteMessage($"  상대 각도: {result.relativeAngle:F2}°");
                //ed.WriteMessage($"  판정된 방향: {result.direction}");
                //ed.WriteMessage($"  방향 설명: {GetDirectionDescription(result.direction)}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 방향 타입 열거형 (8방향)
    /// </summary>
    public enum DirectionType
    {
        N,   // 북쪽 (North)
        NE,  // 북동 (Northeast)
        E,   // 동쪽 (East)
        SE,  // 남동 (Southeast) 
        S,   // 남쪽 (South)
        SW,  // 남서 (Southwest)
        W,   // 서쪽 (West)
        NW   // 북서 (Northwest)
    }

    /// <summary>
    /// 방향 분석 결과 클래스
    /// </summary>
    public class DirectionResult
    {
        public double northAngle { get; set; }      // 북쪽 기준 벡터의 각도
        public double targetAngle { get; set; }     // 분석 대상 line의 각도
        public double relativeAngle { get; set; }   // 상대 각도
        public DirectionType direction { get; set; } // 판정된 방향
    }


    //Window 도우미 클래스
    public class Window_helper
    {
        /// <summary>
        /// 선택된 block을 Window 지정
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택블럭창지정, CommandFlags.UsePickSet)]
        public void Cmd_Blocks_To_Window()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<BlockReference> targets = JEntity.GetEntityByTpye<BlockReference>("Window Block 를 선택 하세요?", JSelFilter.MakeFilterTypes("INSERT"));
                    if (targets.Count() == 0) return;

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,Window,Disp,Type"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var br in targets)
                    {

                        // 3. 폴리라인이 닫혀있는지 확인
                        if (br != null)
                        {
                            br.UpgradeOpen();
                            JXdata.DeleteAll(br);
                            JXdata.SetXdata(br, "Arch", "Window");
                            JXdata.SetXdata(br, "Window", "Window");
                            JXdata.SetXdata(br, "Type", "Window");
                            JXdata.SetXdata(br, "Disp", "WD");

                            //ed.WriteMessage("\n폴리라인이 닫혀있지 않습니다. 닫힌 폴리라인만 처리할 수 있습니다.");
                            //continue;
                        }

                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



    }

    //Door 도우미 클래스
    public class Door_helper
    {
        /// <summary>
        /// 선택된 block을 Door 지정
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택블럭문지정, CommandFlags.UsePickSet)]
        public void Cmd_Blocks_To_Door()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<BlockReference> targets = JEntity.GetEntityByTpye<BlockReference>("Door Block 를 선택 하세요?", JSelFilter.MakeFilterTypes("INSERT"));
                    if (targets.Count() == 0) return;

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,Door,Disp,Type"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var br in targets)
                    {

                        // 3. 폴리라인이 닫혀있는지 확인
                        if (br != null)
                        {
                            br.UpgradeOpen();
                            JXdata.DeleteAll(br);
                            JXdata.SetXdata(br, "Arch", "Door");
                            JXdata.SetXdata(br, "Door", "Door");
                            JXdata.SetXdata(br, "Type", "Door");
                            JXdata.SetXdata(br, "Disp", "DR");
                        }

                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



    }

    //기둥 Column 도우미 클래스
    public class Column_helper
    {
        /// <summary>
        /// 선택된 block을 기둥 지정
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택블럭기둥지정, CommandFlags.UsePickSet)]
        public void Cmd_Blocks_To_Column()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<BlockReference> targets = JEntity.GetEntityByTpye<BlockReference>("기둥 Block 를 선택 하세요?", JSelFilter.MakeFilterTypes("INSERT"));
                    if (targets.Count() == 0) return;

                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,Column,Disp,Type"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var br in targets)
                    {

                        // 3. 폴리라인이 닫혀있는지 확인
                        if (br != null)
                        {
                            br.UpgradeOpen();
                            JXdata.DeleteAll(br);
                            JXdata.SetXdata(br, "Arch", "Column");
                            JXdata.SetXdata(br, "Column", "Column");
                            JXdata.SetXdata(br, "Type", "Column");
                            JXdata.SetXdata(br, "Disp", "CL");
                        }

                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



    }

    //Entity Symbol 도우미 클래스
    public class Entity_Symbol_helper
    {
        /// <summary>
        /// 선택된 block Poly 을 Symbol 지정
        /// </summary>
        [CommandMethod(Jdf.Cmd.선택개체구분지정, CommandFlags.UsePickSet)]
        public void Cmd_Entitys_To_Symbol()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Room Polyline 선택
                    List<Entity> targets = JEntity.GetEntityByTpye<Entity>("Symbol 구분 Entity를 선택 하세요?", JSelFilter.MakeFilterTypes("INSERT,LWPOLYLINE"));
                    if (targets.Count() == 0) return;

                    PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter Symbol  Name? ");
                    PromptResult pStrRes = doc.Editor.GetString(pStrOpts);

                    string str = "";
                    if (pStrRes.Status == PromptStatus.OK)
                    {
                        str = pStrRes.StringResult;
                    }


                    var btr = tr.GetModelSpaceBlockTableRecord(db);

                    tr.CheckRegName("Arch,SymbolType,Disp,Type"); //LL(Line)
                    //Create layerfor Wall Center Line
                    tr.CreateLayer(Jdf.Layer.Room, Jdf.Color.Red, LineWeight.LineWeight040);

                    foreach (var br in targets)
                    {

                        // 3. 폴리라인이 닫혀있는지 확인
                        if (br != null)
                        {
                            br.UpgradeOpen();
                            //JXdata.DeleteAll(br);
                            JXdata.SetXdata(br, "SymbolType", str);
                            //JXdata.SetXdata(br, "Column", "Column");
                            //JXdata.SetXdata(br, "Type", "Column");
                            //JXdata.SetXdata(br, "Disp", "CL");
                        }

                    }


                    tr.Commit();
                }

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



    }

}
