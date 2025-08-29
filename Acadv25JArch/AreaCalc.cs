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
    public class Area_
    {
        // 이전 레이어 상태를 저장하는 정적 변수
        private static string layer_org = "Drw_org";

        [CommandMethod("qq_a")]
        public void Area_Work()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Step 1: LayerTable 접근
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n오류: LayerTable에 접근할 수 없습니다.");
                        return;
                    }

                    // Step 2: 현재 레이어 상태를 layer_org에 저장
                    LayerTableRecord currentLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    //if (currentLayer != null)
                    //{
                    //    layer_org = currentLayer.Name;
                    //    ed.WriteMessage($"\n현재 레이어 '{layer_org}'를 layer_org에 저장했습니다.");
                    //}

                    // Step 3: layer_org 이름으로 Layer State 저장
                    LayerStateManager lsm = db.LayerStateManager;
                    if (lsm.HasLayerState(layer_org))
                    {
                        // 기존 Layer State가 있으면 삭제하고 새로 생성
                        lsm.DeleteLayerState(layer_org);
                        ed.WriteMessage($"\n기존 Layer State '{layer_org}'를 삭제했습니다.");
                    }

                    lsm.SaveLayerState(layer_org,
                        LayerStateMasks.On |
                        LayerStateMasks.Frozen |
                        LayerStateMasks.Locked |
                        LayerStateMasks.Plot |
                        LayerStateMasks.Color |
                        LayerStateMasks.LineType |
                        LayerStateMasks.LineWeight |
                        LayerStateMasks.PlotStyle |
                        LayerStateMasks.Transparency,
                        ObjectId.Null);
                    ed.WriteMessage($"\n현재 상태를 Layer State '{layer_org}'로 저장했습니다.");

                    // Step 4: !AreaCalc 레이어가 존재하는지 확인하고, 없으면 생성
                    ObjectId areaCalcLayerId = ObjectId.Null;
                    string areaCalcLayerName = "!AreaCalc";

                    if (layerTable.Has(areaCalcLayerName))
                    {
                        // 레이어가 이미 존재하는 경우
                        areaCalcLayerId = layerTable[areaCalcLayerName];
                        ed.WriteMessage($"\n레이어 '{areaCalcLayerName}'가 이미 존재합니다.");
                    }
                    else
                    {
                        // 새 레이어 생성
                        LayerTableRecord newLayerRecord = new LayerTableRecord();
                        newLayerRecord.Name = areaCalcLayerName;

                        // LayerTable을 쓰기 모드로 업그레이드
                        layerTable.UpgradeOpen();
                        areaCalcLayerId = layerTable.Add(newLayerRecord);
                        tr.AddNewlyCreatedDBObject(newLayerRecord, true);

                        ed.WriteMessage($"\n레이어 '{areaCalcLayerName}'를 생성했습니다.");
                    }

                    // Step 5: !AreaCalc 레이어를 현재 레이어로 설정
                    db.Clayer = areaCalcLayerId;
                    ed.WriteMessage($"\n현재 레이어를 '{areaCalcLayerName}'로 변경했습니다.");

                    // Step 6: 다른 모든 레이어의 잠금 상태 및 투명도 설정
                    int processedLayers = 0;
                    int lockedLayers = 0;
                    int transparentLayers = 0;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                        if (layer != null && layer.Name != areaCalcLayerName)
                        {
                            processedLayers++;

                            // 레이어 잠금 설정 (!AreaCalc 제외)
                            if (!layer.IsLocked)
                            {
                                layer.IsLocked = true;
                                lockedLayers++;
                            }

                            // 투명도 설정 (50% = 0.5)
                            // AutoCAD에서 투명도는 Alpha 값으로 설정됩니다.
                            // 50% 투명도 = Alpha 값 127 (255 * (100-50) / 100)
                            byte alphaValue = (byte)(255 * (100 - 50) / 100);
                            Transparency transparency = new Transparency(alphaValue);
                            layer.Transparency = transparency;
                            transparentLayers++;

                            // 레이어 색상을 다시 설정하여 재생성 트리거
                            layer.Color = layer.Color;
                        }
                    }

                    // Step 7: 트랜잭션 커밋
                    tr.Commit();

                    // 결과 출력
                    ed.WriteMessage($"\n=== qq_a 명령어 실행 완료 ===");
                    ed.WriteMessage($"\n- 이전 레이어 상태 저장: {layer_org}");
                    ed.WriteMessage($"\n- Layer State '{layer_org}' 저장 완료");
                    ed.WriteMessage($"\n- 현재 활성 레이어: {areaCalcLayerName}");
                    ed.WriteMessage($"\n- 처리된 레이어 수: {processedLayers}");
                    ed.WriteMessage($"\n- 잠금 처리된 레이어: {lockedLayers}");
                    ed.WriteMessage($"\n- 투명도 설정된 레이어: {transparentLayers}");

                    // 화면 재생성을 위한 명령어 실행
                    doc.SendStringToExecute("REGEN ", true, false, false);
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
            }
        }

        [CommandMethod("qq_aa")]
        public void Area_Work_only()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Step 1: LayerTable 접근
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n오류: LayerTable에 접근할 수 없습니다.");
                        return;
                    }

                    // Step 2: !AreaCalc 레이어가 존재하는지 확인
                    ObjectId areaCalcLayerId = ObjectId.Null;
                    string areaCalcLayerName = "!AreaCalc";

                    if (layerTable.Has(areaCalcLayerName))
                    {
                        // 레이어가 이미 존재하는 경우
                        areaCalcLayerId = layerTable[areaCalcLayerName];
                        ed.WriteMessage($"\n레이어 '{areaCalcLayerName}'를 찾았습니다.");
                    }
                    else
                    {
                        // !AreaCalc 레이어가 없으면 생성
                        LayerTableRecord newLayerRecord = new LayerTableRecord();
                        newLayerRecord.Name = areaCalcLayerName;

                        // LayerTable을 쓰기 모드로 업그레이드
                        layerTable.UpgradeOpen();
                        areaCalcLayerId = layerTable.Add(newLayerRecord);
                        tr.AddNewlyCreatedDBObject(newLayerRecord, true);

                        ed.WriteMessage($"\n레이어 '{areaCalcLayerName}'를 생성했습니다.");
                    }

                    // Step 3: !AreaCalc 레이어를 현재 레이어로 설정
                    db.Clayer = areaCalcLayerId;
                    ed.WriteMessage($"\n현재 레이어를 '{areaCalcLayerName}'로 변경했습니다.");

                    // Step 4: !AreaCalc 레이어를 켜고, 나머지 모든 레이어를 끔
                    int processedLayers = 0;
                    int turnedOffLayers = 0;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                        if (layer != null)
                        {
                            if (layer.Name == areaCalcLayerName)
                            {
                                // !AreaCalc 레이어는 반드시 켜진 상태로 설정
                                if (layer.IsOff)
                                {
                                    layer.IsOff = false;
                                    ed.WriteMessage($"\n레이어 '{areaCalcLayerName}'를 켰습니다.");
                                }
                            }
                            else
                            {
                                // 다른 모든 레이어는 끄기
                                if (!layer.IsOff)
                                {
                                    layer.IsOff = true;
                                    turnedOffLayers++;
                                }
                                processedLayers++;
                            }
                        }
                    }

                    // Step 5: 트랜잭션 커밋
                    tr.Commit();

                    // 결과 출력
                    ed.WriteMessage($"\n=== qq_QarchLayer 명령어 실행 완료 ===");
                    ed.WriteMessage($"\n- 현재 활성 레이어: {areaCalcLayerName}");
                    ed.WriteMessage($"\n- 처리된 레이어 수: {processedLayers}");
                    ed.WriteMessage($"\n- 끄기 처리된 레이어: {turnedOffLayers}");
                    ed.WriteMessage($"\n- '{areaCalcLayerName}' 레이어만 표시됩니다.");

                    // 화면 재생성을 위한 명령어 실행
                    doc.SendStringToExecute("REGEN ", true, false, false);
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
            }
        }
    }
}
