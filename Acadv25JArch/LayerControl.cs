using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acadv25JArch
{
    public class LayerControl_
    {
        [CommandMethod("LAYEROFF")]
        public void LayerDisplayOff()
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
                    Entity selectedEntity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (selectedEntity == null)
                    {
                        ed.WriteMessage("\n선택된 객체가 엔티티가 아닙니다.");
                        return;
                    }

                    // Step 3: 선택된 엔티티의 레이어 ID 가져오기
                    ObjectId layerId = selectedEntity.LayerId;
                    string layerName = selectedEntity.Layer;

                    // Step 4: LayerTable에서 해당 레이어의 LayerTableRecord 가져오기
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
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
                    LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (layerRecord == null)
                    {
                        ed.WriteMessage("\n레이어 레코드에 접근할 수 없습니다.");
                        return;
                    }

                    // 현재 활성 레이어는 끌 수 없음 - AutoCAD 규칙
                    string currentLayer = db.Clayer.ToString();
                    LayerTableRecord currentLayerRecord = tr.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
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
        [CommandMethod("LAYERON")]
        public void LayerDisplayOn()
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
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (!layerTable.Has(layerName))
                    {
                        ed.WriteMessage($"\n레이어 '{layerName}'을 찾을 수 없습니다.");
                        return;
                    }

                    ObjectId layerId = layerTable[layerName];
                    LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

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
    }

    public class LayerStateControl_
    {
        // 현재 레이어 상태를 저장하는 명령어
        [CommandMethod("La_Save")]
        public void LayerState_SaveCurrent()
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
        public void LayerState_Restore()
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
        public void LayerState_Delete()
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
        [CommandMethod("UNLOCKALL")]
        public void UnlockAllLayers()
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
        [CommandMethod("LOCKALL")]
        public void LockAllLayers()
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
        [CommandMethod("LISTLOCKEDLAYERS")]
        public void ListLockedLayers()
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
}
