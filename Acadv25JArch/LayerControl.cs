using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace Acadv25JArch
{
    public class LayerControl_
    {
        [CommandMethod("LAYER_OFF")]
        public void Cmd_LayerDisplayOff()
        {
            // 현재 문서와 편집기 가져오기
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: 사용자로부터 엔티티 선택 받기
                PromptEntityOptions peo = new PromptEntityOptions("\n레이어를 끌 엔티티를 선택하세요: ");
                peo.AllowNone = false; // Enter로 종료 불허

                PromptEntityResult per = ed.GetEntity(peo);

                // 선택이 취소되거나 오류가 발생한 경우
                if (per.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n엔티티 선택이 취소되었습니다.");
                    return;
                }

                // Step 2: 트랜잭션 시작
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 선택된 엔티티 열기
                    Entity? selectedEntity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (selectedEntity == null)
                    {
                        ed.WriteMessage("\n선택된 객체가 엔티티가 아닙니다.");
                        return;
                    }

                    // Step 3: 선택된 엔티티의 레이어 ID 가져오기
                    ObjectId layerId = selectedEntity.LayerId;
                    string layerName = selectedEntity.Layer;

                    // Step 4: LayerTable에서 해당 레이어의 LayerTableRecord 가져오기
                    LayerTable? layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n레이어 테이블에 접근할 수 없습니다.");
                        return;
                    }

                    // 레이어가 존재하는지 확인
                    if (!layerTable.Has(layerName))
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'을 찾을 수 없습니다.");
                        return;
                    }

                    // LayerTableRecord를 쓰기 모드로 열기
                    LayerTableRecord? layerRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (layerRecord == null)
                    {
                        ed.WriteMessage("\n레이어 레코드에 접근할 수 없습니다.");
                        return;
                    }

                    // 현재 활성 레이어는 끌 수 없음 - AutoCAD 규칙
                    string currentLayer = db.Clayer.ToString();
                    LayerTableRecord? currentLayerRecord = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayerName = currentLayerRecord != null ? currentLayerRecord.Name : "";

                    if (layerName.Equals(currentLayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        ed.WriteMessage($"\n현재 활성 레이어 '{layerName}'은 끌 수 없습니다.");
                        return;
                    }

                    // Step 5: 레이어가 이미 꺼져있는지 확인
                    if (layerRecord.IsOff)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'은 이미 꺼져있습니다.");
                        return;
                    }

                    // Step 6: 레이어를 Display Off (IsOff = true)
                    layerRecord.IsOff = true;

                    // 트랜잭션 커밋
                    tr.Commit();

                    // 성공 메시지 출력
                    ed.WriteMessage($"\n레이어 '{layerName}'이 꺼졌습니다.");

                    // 화면 재생성을 위한 업데이트
                    ed.Regen();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }

        // 추가 기능: 레이어 다시 켜기 명령어
        [CommandMethod("LAYER_ON")]
        public void Cmd_LayerDisplayOn()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 레이어 이름 입력 받기
                PromptStringOptions pso = new PromptStringOptions("\n켤 레이어 이름을 입력하세요: ");
                pso.AllowSpaces = false;

                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n레이어 이름 입력이 취소되었습니다.");
                    return;
                }

                string layerName = pr.StringResult;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable? layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (!layerTable.Has(layerName))
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'을 찾을 수 없습니다.");
                        return;
                    }

                    ObjectId layerId = layerTable[layerName];
                    LayerTableRecord? layerRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                    if (!layerRecord.IsOff)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'은 이미 켜져있습니다.");
                        return;
                    }

                    layerRecord.IsOff = false;
                    tr.Commit();

                    ed.WriteMessage($"\n레이어 '{layerName}'이 켜졌습니다.");
                    ed.Regen();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }


        /// <summary>
        /// 선택된 entity의 레이어를 현재 레이어로 설정하는 커맨드
        /// </summary>
        [CommandMethod("Layer_SetLayer_Current")]
        public void Cmd_SetLayer_Current()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 기준이 될 entity 선택
                Entity selectedEntity = SelectSingleEntity(ed, "\n레이어를 현재 레이어로 설정할 entity를 선택하세요: ");
                if (selectedEntity == null)
                {
                    ed.WriteMessage("\n선택이 취소되었습니다.");
                    return;
                }

                // 2단계: 선택된 entity의 layer 이름과 ObjectId 가져오기
                string layerName = selectedEntity.Layer;
                ObjectId layerId = selectedEntity.LayerId;

                // 3단계: 현재 레이어인지 확인
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 현재 레이어 확인
                    LayerTableRecord currentLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayerName = currentLayer?.Name ?? "Unknown";

                    if (layerId == db.Clayer)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'는 이미 현재 레이어입니다.");
                        tr.Commit();
                        return;
                    }

                    // 4단계: 선택된 레이어가 유효한지 확인
                    LayerTableRecord targetLayer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    if (targetLayer == null)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'를 찾을 수 없습니다.");
                        tr.Commit();
                        return;
                    }

                    // 5단계: 레이어가 동결되어 있는지 확인
                    if (targetLayer.IsFrozen)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'는 동결되어 있어 현재 레이어로 설정할 수 없습니다.");
                        tr.Commit();
                        return;
                    }

                    tr.Commit();

                    // 6단계: 현재 레이어로 설정
                    db.Clayer = layerId;

                    // 7단계: 결과 출력
                    ed.WriteMessage($"\n현재 레이어가 '{currentLayerName}'에서 '{layerName}'로 변경되었습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 현재 레이어의 이름을 반환하는 함수
        /// </summary>
        /// <returns>현재 레이어 이름</returns>
        public static string GetCurrentLayerName()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 현재 레이어 ObjectId 가져오기
                    ObjectId currentLayerId = db.Clayer;

                    // LayerTableRecord 가져오기
                    LayerTableRecord currentLayer = tr.GetObject(currentLayerId, OpenMode.ForRead) as LayerTableRecord;

                    tr.Commit();
                    return currentLayer?.Name ?? "0"; // 기본값으로 "0" 레이어 반환
                }
                catch (System.Exception)
                {
                    tr.Abort();
                    return "0"; // 오류 시 기본 레이어 반환
                }
            }
        }

        /// <summary>
        /// 현재 레이어 이외의 모든 레이어를 Display Off 시키는 함수
        /// </summary>
        /// <returns>Off된 레이어 개수</returns>
        public static int TurnOffAllLayersExceptCurrent()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int layersOffCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 현재 레이어 ObjectId 가져오기
                    ObjectId currentLayerId = db.Clayer;

                    // LayerTable 가져오기
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    // LayerTable 순회
                    using (var enumerator = layerTable.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ObjectId layerId = enumerator.Current;

                            // 현재 레이어가 아닌 경우에만 처리
                            if (layerId != currentLayerId)
                            {
                                LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                                // 레이어가 현재 On 상태이고 "0" 레이어가 아닌 경우에만 Off
                                if (layer != null && !layer.IsOff && layer.Name != "0")
                                {
                                    layer.IsOff = true;
                                    layersOffCount++;
                                }
                            }
                        }
                    }

                    tr.Commit();
                    return layersOffCount;
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    throw new System.Exception($"레이어 Off 처리 중 오류가 발생했습니다: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 모든 레이어를 Display On 시키는 함수
        /// </summary>
        /// <returns>On된 레이어 개수</returns>
        public static int TurnOnAllLayers()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int layersOnCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // LayerTable 가져오기
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    // LayerTable 순회
                    using (var enumerator = layerTable.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            ObjectId layerId = enumerator.Current;
                            LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                            // 레이어가 현재 Off 상태인 경우에만 On
                            if (layer != null && layer.IsOff)
                            {
                                layer.IsOff = false;
                                layersOnCount++;
                            }
                        }
                    }

                    tr.Commit();
                    return layersOnCount;
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    throw new System.Exception($"레이어 On 처리 중 오류가 발생했습니다: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 현재 레이어 정보 출력 커맨드
        /// </summary>
        [CommandMethod("CURRENT_LAYER_info")]
        public void Cmd_ShowCurrentLayerInfo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                string currentLayerName = GetCurrentLayerName();
                ed.WriteMessage($"\n현재 레이어: {currentLayerName}");

                // 추가 정보 표시
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTableRecord currentLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    if (currentLayer != null)
                    {
                        ed.WriteMessage($"\n레이어 색상: {currentLayer.Color.ColorIndex}");
                        ed.WriteMessage($"\n레이어 상태: {(currentLayer.IsOff ? "끔" : "켜짐")}");
                        ed.WriteMessage($"\n잠금 상태: {(currentLayer.IsLocked ? "잠김" : "해제")}");
                        ed.WriteMessage($"\n동결 상태: {(currentLayer.IsFrozen ? "동결" : "해제")}");
                        ed.WriteMessage($"\n출력 가능: {(currentLayer.IsPlottable ? "예" : "아니오")}");

                        if (!string.IsNullOrEmpty(currentLayer.Description))
                        {
                            ed.WriteMessage($"\n레이어 설명: {currentLayer.Description}");
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

        /// <summary>
        /// 현재 레이어 이외 모든 레이어 Off 커맨드
        /// </summary>
        [CommandMethod("ISOLATELAYER")]
        public void Cmd_IsolateCurrentLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                string currentLayerName = GetCurrentLayerName();
                int layersOffCount = TurnOffAllLayersExceptCurrent();

                ed.WriteMessage($"\n현재 레이어 '{currentLayerName}'만 표시됩니다.");
                ed.WriteMessage($"\n{layersOffCount}개의 레이어가 비표시 처리되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 레이어 On 커맨드
        /// </summary>
        [CommandMethod("LAYER_ALL_ON")]
        public void Cmd_TurnOnAllLayersCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                int layersOnCount = TurnOnAllLayers();
                ed.WriteMessage($"\n{layersOnCount}개의 레이어가 표시 처리되었습니다.");
                ed.WriteMessage($"\n모든 레이어가 표시됩니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



        //func
        // <summary>
        /// 단일 entity 선택 메서드
        /// </summary>
        private Entity SelectSingleEntity(Editor ed, string prompt)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\n유효한 entity를 선택해야 합니다.");

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return null;

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                tr.Commit();
                return entity;
            }
        }

    }

    public class LayerStateControl_
    {
        // 현재 레이어 상태를 저장하는 명령어
        [CommandMethod("La_Save")]
        public void Cmd_LayerState_SaveCurrent()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: 레이어 상태 이름 입력 받기
                PromptStringOptions pso = new PromptStringOptions("\n저장할 레이어 상태 이름을 입력하세요: ");
                pso.AllowSpaces = true;

                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n레이어 상태 저장이 취소되었습니다.");
                    return;
                }

                string stateName = pr.StringResult.Trim();
                if (string.IsNullOrEmpty(stateName))
                {
                    ed.WriteMessage("\n유효한 레이어 상태 이름을 입력해야 합니다.");
                    return;
                }

                // Step 2: LayerStateManager 생성
                LayerStateManager layerStateManager = new LayerStateManager(db);

                // Step 3: 이미 같은 이름의 레이어 상태가 있는지 확인
                if (layerStateManager.HasLayerState(stateName))
                {
                    PromptKeywordOptions pko = new PromptKeywordOptions($"\n레이어 상태 '{stateName}'이 이미 존재합니다. 덮어쓰시겠습니까?");
                    pko.Keywords.Add("Yes");
                    pko.Keywords.Add("No");
                    pko.Keywords.Default = "No";

                    PromptResult pkr = ed.GetKeywords(pko);
                    if (pkr.Status != PromptStatus.OK || pkr.StringResult == "No")
                    {
                        ed.WriteMessage("\n레이어 상태 저장이 취소되었습니다.");
                        return;
                    }

                    // 기존 레이어 상태 삭제
                    layerStateManager.DeleteLayerState(stateName);
                }

                // Step 4: 모든 레이어 속성을 포함하는 마스크 설정
                LayerStateMasks mask = LayerStateMasks.On |           // On/Off 상태
                                     LayerStateMasks.Frozen |        // Frozen/Thawed 상태  
                                     LayerStateMasks.Locked |        // Locked/Unlocked 상태
                                     LayerStateMasks.Color |         // 색상
                                     LayerStateMasks.LineType |      // 선종류
                                     LayerStateMasks.LineWeight |    // 선 두께
                                     LayerStateMasks.Plot |          // 플롯 설정
                                     LayerStateMasks.Transparency;   // 투명도

                // Step 5: 현재 레이어 상태 저장 (viewport ID를 ObjectId.Null로 설정)
                layerStateManager.SaveLayerState(stateName, mask, ObjectId.Null);

                // 저장된 레이어 개수 정보 표시
                ArrayList layerList = layerStateManager.GetLayerStateLayers(stateName, false);

                // Step 6: 설명 추가 (선택사항)
                string description = "Date:"+ DateTime.Now.ToString("yyyy/MM/dd HH:mm") + $" : {layerList.Count} layer";
                layerStateManager.SetLayerStateDescription(stateName, description);

                // 성공 메시지
                ed.WriteMessage($"\n레이어 상태 '{stateName}'이 성공적으로 저장되었습니다.");

                
                ed.WriteMessage($"\n총 {layerList.Count}개의 레이어가 저장되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 저장 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        // 저장된 레이어 상태를 복원하는 명령어 - 번호 선택 방식으로 수정
        [CommandMethod("La_Restore")]
        public void Cmd_LayerState_Restore()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: LayerStateManager 생성
                LayerStateManager layerStateManager = new LayerStateManager(db);

                // Step 2: 저장된 레이어 상태 목록 가져오기
                ArrayList stateNames = layerStateManager.GetLayerStateNames(false, false);

                if (stateNames.Count == 0)
                {
                    ed.WriteMessage("\n저장된 레이어 상태가 없습니다.");
                    return;
                }

                // Step 3: 저장된 레이어 상태 목록을 번호와 함께 표시
                ed.WriteMessage("\n저장된 레이어 상태 목록:");
                ed.WriteMessage("\n" + new string('-', 60));

                for (int i = 0; i < stateNames.Count; i++)
                {
                    string stateName = stateNames[i].ToString();
                    string description = layerStateManager.GetLayerStateDescription(stateName);
                    //ArrayList layerList = layerStateManager.GetLayerStateLayers(stateName, false);

                    ed.WriteMessage($"\n{i + 1}. {stateName}");
                    if (!string.IsNullOrEmpty(description))
                    {
                        ed.WriteMessage($" {description}");
                    }
                    //ed.WriteMessage($"   레이어 개수: {layerList.Count}");
                    //if (i < stateNames.Count - 1)
                    //    ed.WriteMessage("");
                }
                ed.WriteMessage("\n" + new string('-', 60));

                // Step 4: 번호 입력 받기
                PromptIntegerOptions pio = new PromptIntegerOptions($"\n복원할 레이어 상태의 번호를 입력하세요 (1-{stateNames.Count}): ");
                pio.LowerLimit = 1;
                pio.UpperLimit = stateNames.Count;
                pio.UseDefaultValue = false;

                PromptIntegerResult pir = ed.GetInteger(pio);
                if (pir.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n레이어 상태 복원이 취소되었습니다.");
                    return;
                }

                // Step 5: 선택된 번호에 해당하는 레이어 상태 이름 가져오기
                int selectedIndex = pir.Value - 1; // 0-based index로 변환
                string selectedStateName = stateNames[selectedIndex].ToString();

                // Step 6: 레이어 상태 복원
                LayerStateMasks restoreMask = layerStateManager.GetLayerStateMask(selectedStateName);
                layerStateManager.RestoreLayerState(selectedStateName, ObjectId.Null, 0, restoreMask);

                // 성공 메시지
                ed.WriteMessage($"\n레이어 상태 '{selectedStateName}'이 성공적으로 복원되었습니다.");

                // 화면 재생성
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 복원 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        // 저장된 레이어 상태 목록을 보는 명령어
        [CommandMethod("La_List")]
        public void LayerStates_List()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                LayerStateManager layerStateManager = new LayerStateManager(db);
                ArrayList stateNames = layerStateManager.GetLayerStateNames(false, false);

                if (stateNames.Count == 0)
                {
                    ed.WriteMessage("\n저장된 레이어 상태가 없습니다.");
                    return;
                }

                ed.WriteMessage($"\n저장된 레이어 상태 ({stateNames.Count}개):");
                ed.WriteMessage("\n" + new string('-', 80));

                for (int i = 0; i < stateNames.Count; i++)
                {
                    string stateName = stateNames[i].ToString();
                    string description = layerStateManager.GetLayerStateDescription(stateName);
                    ArrayList layerList = layerStateManager.GetLayerStateLayers(stateName, false);
                    LayerStateMasks mask = layerStateManager.GetLayerStateMask(stateName);

                    ed.WriteMessage($"\n{i + 1}. 이름: {stateName}");
                    ed.WriteMessage($"   설명: {(string.IsNullOrEmpty(description) ? "(없음)" : description)}");
                    //ed.WriteMessage($"\n   레이어 개수: {layerList.Count}");
                    //ed.WriteMessage($"\n   저장된 속성: {GetMaskDescription(mask)}");

                    if (i < stateNames.Count - 1)
                        ed.WriteMessage("\n");
                }

                ed.WriteMessage("\n" + new string('-', 80));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 목록 표시 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        // 저장된 레이어 상태를 삭제하는 명령어
        [CommandMethod("La_Delete")]
        public void Cmd_LayerState_Delete()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                LayerStateManager layerStateManager = new LayerStateManager(db);
                ArrayList stateNames = layerStateManager.GetLayerStateNames(false, false);

                if (stateNames.Count == 0)
                {
                    ed.WriteMessage("\n삭제할 레이어 상태가 없습니다.");
                    return;
                }

                // 저장된 레이어 상태 목록 표시
                ed.WriteMessage("\n저장된 레이어 상태 목록:");
                for (int i = 0; i < stateNames.Count; i++)
                {
                    string stateName1 = stateNames[i].ToString();
                    ed.WriteMessage($"\n  {i + 1}. {stateName1}");
                }

                // 삭제할 레이어 상태 이름 입력 받기
                PromptStringOptions pso = new PromptStringOptions("\n삭제할 레이어 상태 이름을 입력하세요: ");
                pso.AllowSpaces = true;

                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n레이어 상태 삭제가 취소되었습니다.");
                    return;
                }

                string stateName = pr.StringResult.Trim();
                if (string.IsNullOrEmpty(stateName))
                {
                    ed.WriteMessage("\n유효한 레이어 상태 이름을 입력해야 합니다.");
                    return;
                }

                if (!layerStateManager.HasLayerState(stateName))
                {
                    ed.WriteMessage($"\n레이어 상태 '{stateName}'을 찾을 수 없습니다.");
                    return;
                }

                // 삭제 확인
                PromptKeywordOptions pko = new PromptKeywordOptions($"\n레이어 상태 '{stateName}'을 정말로 삭제하시겠습니까?");
                pko.Keywords.Add("Yes");
                pko.Keywords.Add("No");
                pko.Keywords.Default = "No";

                PromptResult pkr = ed.GetKeywords(pko);
                if (pkr.Status != PromptStatus.OK || pkr.StringResult == "No")
                {
                    ed.WriteMessage("\n레이어 상태 삭제가 취소되었습니다.");
                    return;
                }

                // 레이어 상태 삭제
                layerStateManager.DeleteLayerState(stateName);
                ed.WriteMessage($"\n레이어 상태 '{stateName}'이 삭제되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 삭제 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        // LayerStateMasks를 사람이 읽을 수 있는 문자열로 변환하는 헬퍼 메서드
        private string GetMaskDescription(LayerStateMasks mask)
        {
            var descriptions = new System.Collections.Generic.List<string>();

            if ((mask & LayerStateMasks.On) == LayerStateMasks.On)
                descriptions.Add("On/Off");
            if ((mask & LayerStateMasks.Frozen) == LayerStateMasks.Frozen)
                descriptions.Add("Frozen");
            if ((mask & LayerStateMasks.Locked) == LayerStateMasks.Locked)
                descriptions.Add("Locked");
            if ((mask & LayerStateMasks.Color) == LayerStateMasks.Color)
                descriptions.Add("Color");
            if ((mask & LayerStateMasks.LineType) == LayerStateMasks.LineType)
                descriptions.Add("LineType");
            if ((mask & LayerStateMasks.LineWeight) == LayerStateMasks.LineWeight)
                descriptions.Add("LineWeight");
            if ((mask & LayerStateMasks.Plot) == LayerStateMasks.Plot)
                descriptions.Add("Plot");
            if ((mask & LayerStateMasks.Transparency) == LayerStateMasks.Transparency)
                descriptions.Add("Transparency");

            return descriptions.Count > 0 ? string.Join(", ", descriptions.ToArray()) : "None";
        }
    }

    public class LayerLockControl_
    {
        [CommandMethod("UNLOCK_Layer_All")]
        public void Cmd_UnlockAllLayers()
        {
            // 현재 문서와 편집기 가져오기
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: 트랜잭션 시작
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Step 2: LayerTable 열기
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n레이어 테이블에 접근할 수 없습니다.");
                        return;
                    }

                    List<string> unlockedLayers = new List<string>();
                    List<string> alreadyUnlockedLayers = new List<string>();
                    int totalLayers = 0;

                    // Step 3: 모든 레이어를 순회하며 잠김 해제
                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerRecord != null)
                        {
                            totalLayers++;
                            string layerName = layerRecord.Name;

                            // 레이어가 잠겨있는지 확인
                            if (layerRecord.IsLocked)
                            {
                                // 쓰기 모드로 업그레이드
                                layerRecord.UpgradeOpen();

                                // 잠김 해제
                                layerRecord.IsLocked = false;

                                unlockedLayers.Add(layerName);

                                // 다시 읽기 모드로 변경
                                layerRecord.DowngradeOpen();
                            }
                            else
                            {
                                alreadyUnlockedLayers.Add(layerName);
                            }
                        }
                    }

                    // Step 4: 트랜잭션 커밋
                    tr.Commit();

                    // Step 5: 결과 보고
                    ed.WriteMessage($"\n=== 레이어 잠김 해제 완료 ===");
                    ed.WriteMessage($"\n총 레이어 수: {totalLayers}");
                    ed.WriteMessage($"\n잠김 해제된 레이어 수: {unlockedLayers.Count}");
                    ed.WriteMessage($"\n이미 잠금 해제된 레이어 수: {alreadyUnlockedLayers.Count}");

                    // 잠김 해제된 레이어 목록 표시 (5개까지만)
                    if (unlockedLayers.Count > 0)
                    {
                        ed.WriteMessage("\n\n잠김 해제된 레이어:");
                        for (int i = 0; i < Math.Min(unlockedLayers.Count, 5); i++)
                        {
                            ed.WriteMessage($"\n  - {unlockedLayers[i]}");
                        }
                        if (unlockedLayers.Count > 5)
                        {
                            ed.WriteMessage($"\n  ... 외 {unlockedLayers.Count - 5}개");
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n\n모든 레이어가 이미 잠금 해제 상태였습니다.");
                    }

                    // 화면 재생성
                    if (unlockedLayers.Count > 0)
                    {
                        ed.Regen();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }

        // 추가 기능: 모든 레이어 잠금 설정
        [CommandMethod("LOCK_ALL_Layer")]
        public void Cmd_LockAllLayers()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n레이어 테이블에 접근할 수 없습니다.");
                        return;
                    }

                    // 현재 활성 레이어 정보 가져오기
                    LayerTableRecord currentLayerRecord = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayerName = currentLayerRecord != null ? currentLayerRecord.Name : "";

                    List<string> lockedLayers = new List<string>();
                    List<string> skippedLayers = new List<string>();
                    List<string> alreadyLockedLayers = new List<string>();
                    int totalLayers = 0;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerRecord != null)
                        {
                            totalLayers++;
                            string layerName = layerRecord.Name;

                            // 현재 활성 레이어는 잠그지 않음
                            if (layerName.Equals(currentLayerName, StringComparison.OrdinalIgnoreCase))
                            {
                                skippedLayers.Add($"{layerName} (현재 활성 레이어)");
                                continue;
                            }

                            if (!layerRecord.IsLocked)
                            {
                                layerRecord.UpgradeOpen();
                                layerRecord.IsLocked = true;
                                lockedLayers.Add(layerName);
                                layerRecord.DowngradeOpen();
                            }
                            else
                            {
                                alreadyLockedLayers.Add(layerName);
                            }
                        }
                    }

                    tr.Commit();

                    // 결과 보고
                    ed.WriteMessage($"\n=== 레이어 잠금 설정 완료 ===");
                    ed.WriteMessage($"\n총 레이어 수: {totalLayers}");
                    ed.WriteMessage($"\n잠금 설정된 레이어 수: {lockedLayers.Count}");
                    ed.WriteMessage($"\n건너뛴 레이어 수: {skippedLayers.Count}");
                    ed.WriteMessage($"\n이미 잠금된 레이어 수: {alreadyLockedLayers.Count}");

                    if (lockedLayers.Count > 0)
                    {
                        ed.WriteMessage("\n\n잠금 설정된 레이어:");
                        for (int i = 0; i < Math.Min(lockedLayers.Count, 5); i++)
                        {
                            ed.WriteMessage($"\n  - {lockedLayers[i]}");
                        }
                        if (lockedLayers.Count > 5)
                        {
                            ed.WriteMessage($"\n  ... 외 {lockedLayers.Count - 5}개");
                        }
                    }

                    if (skippedLayers.Count > 0)
                    {
                        ed.WriteMessage("\n\n건너뛴 레이어:");
                        foreach (string layer in skippedLayers)
                        {
                            ed.WriteMessage($"\n  - {layer}");
                        }
                    }

                    if (lockedLayers.Count > 0)
                    {
                        ed.Regen();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }

        // 잠긴 레이어 목록 표시
        [CommandMethod("LIST_LOCKED_LAYERS")]
        public void Cmd_ListLockedLayers()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null)
                    {
                        ed.WriteMessage("\n레이어 테이블에 접근할 수 없습니다.");
                        return;
                    }

                    List<string> lockedLayers = new List<string>();
                    List<string> unlockedLayers = new List<string>();
                    int totalLayers = 0;

                    foreach (ObjectId layerId in layerTable)
                    {
                        LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (layerRecord != null)
                        {
                            totalLayers++;
                            string layerName = layerRecord.Name;

                            if (layerRecord.IsLocked)
                            {
                                lockedLayers.Add(layerName);
                            }
                            else
                            {
                                unlockedLayers.Add(layerName);
                            }
                        }
                    }

                    tr.Commit();

                    // 결과 표시
                    ed.WriteMessage($"\n=== 레이어 잠금 상태 ===");
                    ed.WriteMessage($"\n총 레이어 수: {totalLayers}");
                    ed.WriteMessage($"\n잠긴 레이어 수: {lockedLayers.Count}");
                    ed.WriteMessage($"\n잠금 해제된 레이어 수: {unlockedLayers.Count}");

                    if (lockedLayers.Count > 0)
                    {
                        ed.WriteMessage("\n\n잠긴 레이어 목록:");
                        foreach (string layerName in lockedLayers)
                        {
                            ed.WriteMessage($"\n  🔒 {layerName}");
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n\n잠긴 레이어가 없습니다.");
                    }

                    if (unlockedLayers.Count > 0 && unlockedLayers.Count <= 10)
                    {
                        ed.WriteMessage("\n\n잠금 해제된 레이어 목록:");
                        foreach (string layerName in unlockedLayers)
                        {
                            ed.WriteMessage($"\n  🔓 {layerName}");
                        }
                    }
                    else if (unlockedLayers.Count > 10)
                    {
                        ed.WriteMessage($"\n\n잠금 해제된 레이어가 {unlockedLayers.Count}개 있습니다. (목록 생략)");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류가 발생했습니다: {ex.Message}");
            }
        }
    }

    namespace LayerCountTable
    {
        public class LayerCountTable_
        {
            [CommandMethod("ll_Count")]
            public static void Cmd_LayerCountTable()
            {
                // 테이블 설정값들
                const double rowHeight = 1000.0;
                const double colWidth = 2000.0;
                const double textHeight = rowHeight * 0.25;

                // 현재 도면 가져오기
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return;

                var db = doc.Database;
                var ed = doc.Editor;

                // 테이블 삽입점 선택
                var pr = ed.GetPoint("\nEnter table insertion point: ");
                if (pr.Status != PromptStatus.OK)
                    return;

                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // 1단계: 모든 레이어 정보 수집
                        var layerInfos = GetLayerEntityCounts(tr, db);
                        if (layerInfos == null || layerInfos.Count == 0)
                        {
                            ed.WriteMessage("\nNo layers found in the drawing.");
                            return;
                        }

                        // 2단계: 테이블 생성 및 설정
                        var table = new Table();
                        table.TableStyle = db.Tablestyle;
                        table.SetRowHeight(rowHeight);
                        table.SetColumnWidth(colWidth);
                        table.Position = pr.Value;

                        // 3단계: 두 번째 컬럼 추가 (레이어명용)
                        table.InsertColumns(1, colWidth, 1);

                        // 4단계: 헤더 설정
                        SetupTableHeaders(table, textHeight);

                        // 5단계: 레이어별 데이터 행 추가
                        AddLayerDataRows(table, layerInfos, rowHeight, textHeight);

                        // 6단계: 테이블을 현재 공간에 추가
                        var currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                        currentSpace.AppendEntity(table);
                        tr.AddNewlyCreatedDBObject(table, true);

                        tr.Commit();

                        ed.WriteMessage($"\nLayer count table created successfully. Total layers: {layerInfos.Count}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nError creating layer count table: {ex.Message}");
                        tr.Abort();
                    }
                }
            }

            /// <summary>
            /// 레이어별 엔티티 수를 계산하여 반환
            /// </summary>
            private static Dictionary<string, int> GetLayerEntityCounts(Transaction tr, Database db)
            {
                var layerCounts = new Dictionary<string, int>();

                try
                {
                    // 레이어 테이블에서 모든 레이어 이름 수집
                    var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    foreach (ObjectId layerId in layerTable)
                    {
                        var layerRecord = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                        layerCounts[layerRecord.Name] = 0; // 초기값 0으로 설정
                    }

                    // 현재 공간의 모든 엔티티를 확인하여 레이어별 카운트
                    var currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                    foreach (ObjectId entityId in currentSpace)
                    {
                        var entity = (Entity)tr.GetObject(entityId, OpenMode.ForRead);
                        var layerName = entity.Layer;

                        if (layerCounts.ContainsKey(layerName))
                        {
                            layerCounts[layerName]++;
                        }
                    }
                }
                catch (System.Exception)
                {
                    return null;
                }

                return layerCounts;
            }

            /// <summary>
            /// 테이블 헤더 설정
            /// </summary>
            private static void SetupTableHeaders(Table table, double textHeight)
            {
                // 첫 번째 컬럼 헤더 - Layer Name
                var header1 = table.Cells[0, 0];
                header1.Value = "Layer Name";
                header1.Alignment = CellAlignment.MiddleCenter;
                header1.TextHeight = textHeight;

                // 두 번째 컬럼 헤더 - Entity Count
                var header2 = table.Cells[0, 1];
                header2.Value = "Entity Count";
                header2.Alignment = CellAlignment.MiddleCenter;
                header2.TextHeight = textHeight;
            }

            /// <summary>
            /// 레이어 데이터 행들을 테이블에 추가
            /// </summary>
            private static void AddLayerDataRows(Table table, Dictionary<string, int> layerInfos, double rowHeight, double textHeight)
            {
                // 레이어명으로 정렬
                var sortedLayers = layerInfos.OrderBy(x => x.Key).ToList();

                foreach (var layerInfo in sortedLayers)
                {
                    // 새 행 추가
                    table.InsertRows(table.Rows.Count, rowHeight, 1);
                    var rowIndex = table.Rows.Count - 1;

                    // 첫 번째 컬럼: 레이어명
                    var layerNameCell = table.Cells[rowIndex, 0];
                    layerNameCell.Value = layerInfo.Key;
                    layerNameCell.Alignment = CellAlignment.MiddleLeft;
                    layerNameCell.TextHeight = textHeight;

                    // 두 번째 컬럼: 엔티티 수
                    var entityCountCell = table.Cells[rowIndex, 1];
                    entityCountCell.Value = layerInfo.Value;
                    entityCountCell.Alignment = CellAlignment.MiddleCenter;
                    entityCountCell.TextHeight = textHeight;
                }
            }
        }
    }


    public class LayerManager
    {
        /// <summary>
        /// 레이어 이름을 제공받아 레이어를 생성하거나 기존 레이어를 반환하는 함수
        /// 레이어가 이미 존재하면 기존 레이어를 반환하고, 없으면 새로 생성합니다.
        /// </summary>
        /// <param name="layerName">생성할 레이어 이름</param>
        /// <returns>생성되거나 기존의 LayerTableRecord 객체</returns>
        public static LayerTableRecord GetOrCreateLayer(string layerName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // 레이어 이름이 null이거나 빈 문자열인 경우 예외 처리
            if (string.IsNullOrWhiteSpace(layerName))
            {
                throw new System.ArgumentException("레이어 이름이 유효하지 않습니다.", nameof(layerName));
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // LayerTable 가져오기
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    LayerTableRecord layerTableRecord;

                    // 레이어가 이미 존재하는지 확인
                    if (layerTable.Has(layerName))
                    {
                        // 기존 레이어 반환
                        ObjectId layerId = layerTable[layerName];
                        layerTableRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    }
                    else
                    {
                        // 새 레이어 생성
                        layerTable.UpgradeOpen();

                        layerTableRecord = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, 7), // 기본 색상 (흰색)
                            IsFrozen = false,
                            IsLocked = false,
                            IsOff = false,
                            IsPlottable = true
                        };

                        // LayerTable에 새 레이어 추가
                        ObjectId layerId = layerTable.Add(layerTableRecord);
                        tr.AddNewlyCreatedDBObject(layerTableRecord, true);
                    }

                    tr.Commit();
                    return layerTableRecord;
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    throw new System.Exception($"레이어 생성 중 오류가 발생했습니다: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 레이어를 생성하고 속성을 설정하는 고급 함수
        /// </summary>
        /// <param name="layerName">레이어 이름</param>
        /// <param name="color">레이어 색상 (ACI 인덱스, 기본값: 7)</param>
        /// <param name="description">레이어 설명 (선택사항)</param>
        /// <param name="isPlottable">출력 가능 여부 (기본값: true)</param>
        /// <returns>생성되거나 기존의 LayerTableRecord 객체</returns>
        public static LayerTableRecord GetOrCreateLayerWithProperties(
            string layerName,
            short color = 7,
            string description = "",
            bool isPlottable = true)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            if (string.IsNullOrWhiteSpace(layerName))
            {
                throw new System.ArgumentException("레이어 이름이 유효하지 않습니다.", nameof(layerName));
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    LayerTableRecord layerTableRecord;

                    if (layerTable.Has(layerName))
                    {
                        // 기존 레이어 가져오기
                        ObjectId layerId = layerTable[layerName];
                        layerTableRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        // 기존 레이어 속성 업데이트
                        layerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, color);
                        if (!string.IsNullOrEmpty(description))
                        {
                            layerTableRecord.Description = description;
                        }
                        layerTableRecord.IsPlottable = isPlottable;
                    }
                    else
                    {
                        // 새 레이어 생성
                        layerTable.UpgradeOpen();

                        layerTableRecord = new LayerTableRecord
                        {
                            Name = layerName,
                            Color = Color.FromColorIndex(ColorMethod.ByAci, color),
                            Description = description ?? "",
                            IsFrozen = false,
                            IsLocked = false,
                            IsOff = false,
                            IsPlottable = isPlottable
                        };

                        ObjectId layerId = layerTable.Add(layerTableRecord);
                        tr.AddNewlyCreatedDBObject(layerTableRecord, true);
                    }

                    tr.Commit();
                    return layerTableRecord;
                }
                catch (System.Exception ex)
                {
                    tr.Abort();
                    throw new System.Exception($"레이어 생성 중 오류가 발생했습니다: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 레이어 생성 테스트 커맨드
        /// </summary>
        [CommandMethod("TEST_LAYER")]
        public void Cmd_TestCreateLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 레이어 이름 입력받기
                var layerPrompt = new PromptStringOptions("\n생성할 레이어 이름을 입력하세요: ")
                {
                    AllowSpaces = false
                };

                var layerResult = ed.GetString(layerPrompt);
                if (layerResult.Status != PromptStatus.OK)
                    return;

                string layerName = layerResult.StringResult;

                // 레이어 생성 또는 가져오기
                LayerTableRecord layer = GetOrCreateLayer(layerName);

                if (layer != null)
                {
                    ed.WriteMessage($"\n레이어 '{layerName}'이 성공적으로 처리되었습니다.");
                    ed.WriteMessage($"\n레이어 색상: {layer.Color.ColorIndex}");
                    ed.WriteMessage($"\n레이어 상태: {(layer.IsOff ? "끔" : "켜짐")}");
                    ed.WriteMessage($"\n출력 가능: {(layer.IsPlottable ? "예" : "아니오")}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 고급 레이어 생성 테스트 커맨드
        /// </summary>
        [CommandMethod("TEST_LAYER_ADVANCED")]
        public void Cmd_TestCreateLayerAdvanced()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 레이어 이름 입력
                var layerPrompt = new PromptStringOptions("\n생성할 레이어 이름을 입력하세요: ")
                {
                    AllowSpaces = false
                };

                var layerResult = ed.GetString(layerPrompt);
                if (layerResult.Status != PromptStatus.OK)
                    return;

                string layerName = layerResult.StringResult;

                // 색상 입력
                var colorPrompt = new PromptIntegerOptions($"\n레이어 색상 인덱스를 입력하세요 (1-255, 기본값: 7): ")
                {
                    DefaultValue = 7,
                    AllowNegative = false,
                    LowerLimit = 1,
                    UpperLimit = 255
                };

                var colorResult = ed.GetInteger(colorPrompt);
                if (colorResult.Status != PromptStatus.OK)
                    return;

                short colorIndex = (short)colorResult.Value;

                // 설명 입력
                var descPrompt = new PromptStringOptions("\n레이어 설명을 입력하세요 (선택사항): ")
                {
                    AllowSpaces = true,
                    //AllowEmpty = true
                };

                var descResult = ed.GetString(descPrompt);
                string description = descResult.Status == PromptStatus.OK ? descResult.StringResult : "";

                // 고급 레이어 생성
                LayerTableRecord layer = GetOrCreateLayerWithProperties(layerName, colorIndex, description);

                if (layer != null)
                {
                    ed.WriteMessage($"\n레이어 '{layerName}'이 성공적으로 생성되었습니다.");
                    ed.WriteMessage($"\n레이어 색상: {layer.Color.ColorIndex}");
                    ed.WriteMessage($"\n레이어 설명: {layer.Description}");
                    ed.WriteMessage($"\n출력 가능: {(layer.IsPlottable ? "예" : "아니오")}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

 
    }


    public class LayerEntitySelector_
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const string COMMAND_NAME = "SELECTLAYER";
        private const string COMMAND_NAME_CUSTOM = "SELECTLAYER_CUSTOM";

        // Isolate 관련 상수
        private const string ISOLATE_XDATA_APPNAME = "LAYER_ISOLATE_STATE";
        private const string ISOLATE_XDATA_KEY = "ORIGINAL_STATE";

        /// <summary>
        /// 여러 Entity를 선택하여 해당 layer들의 모든 Entity를 isolate하는 메인 커맨드
        /// </summary>
        [CommandMethod("LyIso")]//_ISOLATE_ENTITY_LAYERS
        public void Cmd_LyIso_IsolateEntityLayers()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 여러 Entity 선택
                var selectedEntities = SelectMultipleEntities(ed, "\nIsolate할 entity들을 선택하세요: ");
                if (selectedEntities.Count == 0)
                {
                    ed.WriteMessage("\n선택된 entity가 없습니다.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 2단계: 선택된 Entity들의 layer 목록 수집 (중복 제거)
                    var targetLayers = selectedEntities
                        .Select(entity => entity.Layer)
                        .Distinct()
                        .ToHashSet();

                    ed.WriteMessage($"\n선택된 entity들의 레이어: {string.Join(", ", targetLayers)}");

                    // 3단계: 해당 layer들의 모든 Entity 수집
                    var allEntitiesInTargetLayers = CollectAllEntitiesInLayers(ed, targetLayers.ToList());

                    // 4단계: 현재 layer 상태 저장 (복원용)
                    SaveLayerStates(tr, db);

                    // 5단계: Isolate 실행 - 대상 layer가 아닌 모든 layer를 끄기
                    IsolateTargetLayers(tr, db, targetLayers);

                    tr.Commit();

                    // 6단계: 결과 출력
                    ed.WriteMessage($"\n{targetLayers.Count}개 레이어의 {allEntitiesInTargetLayers.Length}개 entity가 isolate되었습니다.");
                    ed.WriteMessage($"\nIsolate된 레이어: {string.Join(", ", targetLayers)}");
                    ed.WriteMessage($"\nUNISOLATE_LAYERS 명령으로 원래 상태로 복원할 수 있습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nIsolate 중 오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 여러 Entity 선택 메서드
        /// </summary>
        private List<Entity> SelectMultipleEntities(Editor ed, string prompt)
        {
            var entities = new List<Entity>();

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = prompt,
                AllowDuplicates = false
            };

            var psr = ed.GetSelection(opts);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objId in psr.Value.GetObjectIds())
                    {
                        if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                        {
                            entities.Add(entity);
                        }
                    }
                    tr.Commit();
                }
            }

            return entities;
        }

        /// <summary>
        /// 지정된 layer들의 모든 entity를 수집하는 메서드
        /// </summary>
        private ObjectId[] CollectAllEntitiesInLayers(Editor ed, List<string> layerNames)
        {
            var allEntityIds = new List<ObjectId>();

            foreach (string layerName in layerNames)
            {
                var layerEntities = SelectAllEntitiesOnLayer(ed, layerName);
                allEntityIds.AddRange(layerEntities);
            }

            return allEntityIds.Distinct().ToArray();
        }

        /// <summary>
        /// 현재 layer 상태를 저장하는 메서드 (Extended Data 사용)
        /// </summary>
        private void SaveLayerStates(Transaction tr, Database db)
        {
            try
            {
                // 레지스터드 애플리케이션 확인/생성
                RegAppTable regAppTable = tr.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!regAppTable.Has(ISOLATE_XDATA_APPNAME))
                {
                    regAppTable.UpgradeOpen();
                    RegAppTableRecord regApp = new RegAppTableRecord();
                    regApp.Name = ISOLATE_XDATA_APPNAME;
                    regAppTable.Add(regApp);
                    tr.AddNewlyCreatedDBObject(regApp, true);
                    regAppTable.DowngradeOpen();
                }

                LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                    // 현재 상태를 XData로 저장
                    var xdata = new List<TypedValue>
                    {
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, ISOLATE_XDATA_APPNAME),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, ISOLATE_XDATA_KEY),
                        new TypedValue((int)DxfCode.ExtendedDataInteger16, layer.IsOff ? 1 : 0)
                    };

                    layer.XData = new ResultBuffer(xdata.ToArray());
                }
            }
            catch (System.Exception ex)
            {
                // XData 저장 실패 시 무시하고 계속 진행
                System.Diagnostics.Debug.WriteLine($"Layer state save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 layer 상태를 복원하는 메서드
        /// </summary>
        private bool RestoreLayerStates(Transaction tr, Database db)
        {
            try
            {
                LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                bool hasRestoredData = false;

                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                    // XData에서 원래 상태 복원
                    ResultBuffer xdata = layer.XData;
                    if (xdata != null)
                    {
                        var values = xdata.AsArray();
                        if (values.Length >= 3 &&
                            values[0].TypeCode == (int)DxfCode.ExtendedDataRegAppName &&
                            values[0].Value.ToString() == ISOLATE_XDATA_APPNAME &&
                            values[1].TypeCode == (int)DxfCode.ExtendedDataAsciiString &&
                            values[1].Value.ToString() == ISOLATE_XDATA_KEY)
                        {
                            bool wasOff = Convert.ToInt16(values[2].Value) == 1;
                            layer.IsOff = wasOff;
                            hasRestoredData = true;

                            // XData 제거
                            layer.XData = null;
                        }
                    }
                }

                return hasRestoredData;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Layer state restore failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 대상 layer들만 켜고 나머지는 끄는 isolate 메서드
        /// </summary>
        private void IsolateTargetLayers(Transaction tr, Database db, HashSet<string> targetLayers)
        {
            LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            foreach (ObjectId layerId in layerTable)
            {
                LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                // 대상 layer가 아니면 끄기
                if (!targetLayers.Contains(layer.Name))
                {
                    layer.IsOff = true;
                }
                else
                {
                    // 대상 layer는 켜기 (isolate 상태에서 보이도록)
                    layer.IsOff = false;
                }
            }
        }

        /// <summary>
        /// 모든 layer를 켜는 안전장치 메서드
        /// </summary>
        private void TurnOnAllLayers(Transaction tr, Database db)
        {
            LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            foreach (ObjectId layerId in layerTable)
            {
                LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                layer.IsOff = false;
            }
        }


        /// <summary>
        /// 선택된 entity와 같은 layer에 있는 모든 entity를 선택하는 메인 커맨드
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void SelectEntitiesOnSameLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 기준이 될 entity 선택
                Entity selectedEntity = SelectSingleEntity(ed, "\n기준 entity를 선택하세요: ");
                if (selectedEntity == null)
                {
                    ed.WriteMessage("\n선택이 취소되었습니다.");
                    return;
                }

                // 2단계: 선택된 entity의 layer 이름 가져오기
                string layerName = selectedEntity.Layer;
                ed.WriteMessage($"\n선택된 레이어: '{layerName}'");

                // 3단계: 같은 layer에 있는 모든 entity 선택
                var selectedObjectIds = SelectAllEntitiesOnLayer(ed, layerName);

                if (selectedObjectIds.Length == 0)
                {
                    ed.WriteMessage($"\n레이어 '{layerName}'에서 선택 가능한 entity가 없습니다.");
                    return;
                }

                // 4단계: 선택된 entity들을 AutoCAD의 선택 상태로 설정
                ed.SetImpliedSelection(selectedObjectIds);

                // 5단계: 결과 출력
                ed.WriteMessage($"\n레이어 '{layerName}'에서 {selectedObjectIds.Length}개의 entity가 선택되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 단일 entity 선택 메서드
        /// </summary>
        private Entity SelectSingleEntity(Editor ed, string prompt)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\n유효한 entity를 선택해야 합니다.");

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return null;

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                Entity entity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                tr.Commit();
                return entity;
            }
        }

        /// <summary>
        /// 지정된 layer에 있는 모든 entity를 선택하는 메서드
        /// </summary>
        private ObjectId[] SelectAllEntitiesOnLayer(Editor ed, string layerName)
        {
            // SelectionFilter를 사용하여 특정 layer의 entity만 필터링
            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.LayerName, layerName)
            ];
            var filter = new SelectionFilter(filterList);

            // SelectAll 메서드로 필터 조건에 맞는 모든 entity 선택
            var selectionResult = ed.SelectAll(filter);

            if (selectionResult.Status == PromptStatus.OK && selectionResult.Value != null)
            {
                return selectionResult.Value.GetObjectIds();
            }

            return new ObjectId[0]; // 빈 배열 반환
        }

        /// <summary>
        /// 지정된 layer가 데이터베이스에 존재하는지 확인하는 메서드
        /// </summary>
        private bool LayerExists(Database db, string layerName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                bool exists = layerTable.Has(layerName);
                tr.Commit();
                return exists;
            }
        }


        /// <summary>
        /// 선택된 entity의 레이어를 현재 레이어로 설정하는 커맨드
        /// </summary>
        [CommandMethod("SETLAYERCURRENT")]
        public void SetLayerCurrent()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 기준이 될 entity 선택
                Entity selectedEntity = SelectSingleEntity(ed, "\n레이어를 현재 레이어로 설정할 entity를 선택하세요: ");
                if (selectedEntity == null)
                {
                    ed.WriteMessage("\n선택이 취소되었습니다.");
                    return;
                }

                // 2단계: 선택된 entity의 layer 이름과 ObjectId 가져오기
                string layerName = selectedEntity.Layer;
                ObjectId layerId = selectedEntity.LayerId;

                // 3단계: 현재 레이어인지 확인
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 현재 레이어 확인
                    LayerTableRecord currentLayer = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayerName = currentLayer?.Name ?? "Unknown";

                    if (layerId == db.Clayer)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'는 이미 현재 레이어입니다.");
                        tr.Commit();
                        return;
                    }

                    // 4단계: 선택된 레이어가 유효한지 확인
                    LayerTableRecord targetLayer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                    if (targetLayer == null)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'를 찾을 수 없습니다.");
                        tr.Commit();
                        return;
                    }

                    // 5단계: 레이어가 동결되어 있는지 확인
                    if (targetLayer.IsFrozen)
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'는 동결되어 있어 현재 레이어로 설정할 수 없습니다.");
                        tr.Commit();
                        return;
                    }

                    tr.Commit();

                    // 6단계: 현재 레이어로 설정
                    db.Clayer = layerId;

                    // 7단계: 결과 출력
                    ed.WriteMessage($"\n현재 레이어가 '{currentLayerName}'에서 '{layerName}'로 변경되었습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }



        /// <summary>
        /// LinetypeObjectId로부터 라인타입 이름을 가져오는 헬퍼 메서드
        /// </summary>
        private string GetLinetypeName(Transaction tr, ObjectId linetypeId)
        {
            try
            {
                if (linetypeId.IsNull)
                    return "Unknown";

                LinetypeTableRecord linetype = tr.GetObject(linetypeId, OpenMode.ForRead) as LinetypeTableRecord;
                return linetype?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }



}
