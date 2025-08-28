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
    public class AreaInitCommand
    {
        // 이전 레이어 상태를 저장하는 정적 변수
        private static string layer_org = string.Empty;

        [CommandMethod("area_init")]
        public void AreaInit()
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
                    if (currentLayer != null)
                    {
                        layer_org = currentLayer.Name;
                        ed.WriteMessage($"\n현재 레이어 '{layer_org}'를 layer_org에 저장했습니다.");
                    }

                    // Step 3: !AreaCalc 레이어가 존재하는지 확인하고, 없으면 생성
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

                    // Step 4: !AreaCalc 레이어를 현재 레이어로 설정
                    db.Clayer = areaCalcLayerId;
                    ed.WriteMessage($"\n현재 레이어를 '{areaCalcLayerName}'로 변경했습니다.");

                    // Step 5: 다른 모든 레이어의 잠금 상태 및 투명도 설정
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

                    // Step 6: 트랜잭션 커밋
                    tr.Commit();

                    // 결과 출력
                    ed.WriteMessage($"\n=== area_init 명령어 실행 완료 ===");
                    ed.WriteMessage($"\n- 이전 레이어 상태 저장: {layer_org}");
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

        // layer_org 값을 가져오는 헬퍼 메서드
        [CommandMethod("get_layer_org")]
        public void GetLayerOrg()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            if (!string.IsNullOrEmpty(layer_org))
            {
                ed.WriteMessage($"\nlayer_org: {layer_org}");
            }
            else
            {
                ed.WriteMessage("\nlayer_org가 설정되지 않았습니다.");
            }
        }

        // layer_org를 현재 레이어로 복원하는 헬퍼 메서드
        [CommandMethod("restore_layer_org")]
        public void RestoreLayerOrg()
        {
            if (string.IsNullOrEmpty(layer_org))
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n복원할 레이어 정보가 없습니다.");
                return;
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (layerTable.Has(layer_org))
                    {
                        ObjectId orgLayerId = layerTable[layer_org];
                        db.Clayer = orgLayerId;
                        tr.Commit();
                        ed.WriteMessage($"\n레이어를 '{layer_org}'로 복원했습니다.");
                    }
                    else
                    {
                        ed.WriteMessage($"\n레이어 '{layer_org}'를 찾을 수 없습니다.");
                    }
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
