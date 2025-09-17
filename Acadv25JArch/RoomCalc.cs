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
        public void Cmd_Text_To_Room()
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

    public class DirectionAnalyzer
    {
        /// <summary>
        /// 방향 벡터를 기준으로 선택된 line의 방향을 NW, NE, SE, SW로 분석하는 메인 커맨드
        /// </summary>
        [CommandMethod("rm_dir")]
        public void Cmd_AnalyzeLineDirection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 북쪽 방향 벡터를 나타내는 line 선택
                Line northVectorLine = SelectLine(ed, "\n북쪽 방향 벡터를 나타내는 line을 선택하세요 (시작점→끝점이 북쪽 방향): ");
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
                var directionResult = AnalyzeDirectionRelativeToNorth(northVectorLine, targetLine);

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
        /// 커스텀 허용 각도로 방향 분석하는 추가 커맨드
        /// </summary>
        [CommandMethod("rm_Dir_Cus")]
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
        private DirectionResult AnalyzeDirectionRelativeToNorth(Line northVectorLine, Line targetLine, double baseAngle = 10.0)
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
        private double GetAngleInDegrees(Vector3d vector)
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
        private double NormalizeAngleToSignedRange(double angle)
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
        private DirectionType DetermineDirection(double relativeAngle, double baseAngle = 10.0)
        {
            // 1단계: 먼저 주요 방향(N, E, S, W) 확인

            // N: -10 ~ 10도
            if (relativeAngle >= -baseAngle && relativeAngle <= baseAngle)
                return DirectionType.N;

            // E: 80 ~ 100도 (90±10)
            if (relativeAngle >= (90 - baseAngle) && relativeAngle <= (90 + baseAngle))
                return DirectionType.E;

            // S: 170 ~ -170도 (180±10, 170도 이상이거나 -170도 이하)
            if (relativeAngle >= (180 - baseAngle) || relativeAngle <= (-180 + baseAngle))
                return DirectionType.S;

            // W: -100 ~ -80도 (-90±10)
            if (relativeAngle >= (-90 - baseAngle) && relativeAngle <= (-90 + baseAngle))
                return DirectionType.W;

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
        [CommandMethod("ANALYZEDIRECTION_DETAIL")]
        public void ShowDetailedDirectionAnalysis()
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

}
