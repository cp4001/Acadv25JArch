/*
 * ===================================================================
 * (C) 2025 J-Tech (Chung Jun-Hoi)
 * * * AutoCAD C# API - JTech Layer Manager (전체 주석 포함 최종본)
 * *
 * * AutoCAD 2025 API 기준
 * * 사용자가 요청한 모든 명령어 목록(1~9)에 대한
 * * 구현 및 구현 가이드(Stub) 주석을 완료한 버전입니다.
 * ===================================================================
 */

// --- 필수 using 문 ---
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors; // Color
using Autodesk.AutoCAD.Geometry; // Point3d
using System.Collections.Generic; // Dictionary, List
using System.Linq; // .Count()
using System; // StringComparison, Exception
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color; // [FIX] using 별칭 추가

// C# 프로젝트의 기본 네임스페이스
namespace JTechLayerTools
{
    /// <summary>
    /// DreamPlus 레이어 기능 목록을 기반으로 C#으로 구현한 AutoCAD 명령어 클래스 (2025 호환본)
    /// </summary>
    public class JTechLayerManager
    {
        // ===================================================================
        // LBR (백업/복원) 기능을 위한 정적(Static) 멤버
        // ===================================================================

        private static Dictionary<ObjectId, LayerState> _layerStateBackup = null;

        private struct LayerState
        {
            public bool IsOff;
            public bool IsFrozen;
            public bool IsLocked;
        }

        // ===================================================================
        // 1. 레이어 변경 및 일치
        // ===================================================================
        #region 1. 레이어 변경 및 일치 (Change & Match)

        /// <summary>
        /// (LMA) 레이어 일치: 대상 객체를 원본 객체의 레이어로 변경합니다.
        /// </summary>
        [CommandMethod("LMA")]
        public static void LayerMatch()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peoSource = new PromptEntityOptions("\n원본 객체 선택 (레이어 기준): ");
            PromptEntityResult perSource = ed.GetEntity(peoSource);
            if (perSource.Status != PromptStatus.OK) return;

            ObjectId sourceLayerId;
            string sourceLayerName;

            PromptSelectionOptions psoDest = new PromptSelectionOptions
            {
                MessageForAdding = "\n변경할 대상 객체(들) 선택: "
            };
            PromptSelectionResult psrDest = ed.GetSelection(psoDest);
            if (psrDest.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity sourceEnt = tr.GetObject(perSource.ObjectId, OpenMode.ForRead) as Entity;
                    if (sourceEnt == null) return;

                    sourceLayerId = sourceEnt.LayerId;
                    LayerTableRecord ltrSource = tr.GetObject(sourceLayerId, OpenMode.ForRead) as LayerTableRecord;
                    sourceLayerName = ltrSource.Name;

                    foreach (SelectedObject so in psrDest.Value)
                    {
                        Entity destEnt = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        if (destEnt != null)
                        {
                            destEnt.LayerId = sourceLayerId;
                        }
                    }
                    tr.Commit();
                    ed.WriteMessage($"\n{psrDest.Value.Count}개 객체의 레이어를 '{sourceLayerName}'(으)로 변경했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLMA 오류: {ex.Message}");
            }
        }

        /*
         * ===================================================================
         * SCL과 CCL의 핵심 차이점
         * ===================================================================
         * SCL과 CCL의 핵심 차이점은 변경하는 대상이 '도면의 설정값'이냐 '객체의 속성'이냐의 차이입니다.
         *
         * ## SCL (Set Current Layer)
         * - **대상:** 도면의 **'현재 활성 레이어'** 설정 (`db.Clayer`)
         * - **작업:** 사용자가 선택한 객체 1개의 레이어를 확인한 뒤, 도면 전체의 '현재 레이어'를 그 객체의 레이어로 *설정*합니다.
         * - **결과:** 도면의 활성 레이어가 변경됩니다. (기존 객체는 변하지 않습니다.)
         * - **비유:** 사용할 **연필을 'B연필'에서 '4B연필'로 바꾸는** 행위입니다.
         *
         * ## CCL (Change to Current Layer)
         * - **대상:** 사용자가 선택한 **'객체(들)'** (`ent.LayerId`)
         * - **작업:** 현재 활성 레이어(예: 'A-LINE' 레이어)가 무엇인지 확인한 뒤, 사용자가 선택한 객체(들)를 모두 'A-LINE' 레이어로 *이동(변경)*시킵니다.
         * - **결과:** 선택된 객체들의 레이어 속성이 변경됩니다. (도면의 활성 레이어 설정은 변하지 않습니다.)
         * - **비유:** 이미 'B연필'로 그린 그림을 **선택해서 '4B연필'로 그린 것처럼 속성을 바꾸는** 행위입니다.
         * ===================================================================
         */

        /// <summary>
        /// (SCL) 현재 레이어로 설정: 선택한 객체의 레이어를 현재 작업 레이어로 설정합니다.
        /// </summary>
        [CommandMethod("SCL")]
        public static void SetCurrentLayerByObject()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n현재 레이어로 설정할 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;

                    db.Clayer = ent.LayerId;
                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    ed.WriteMessage($"\n현재 레이어가 '{ltr.Name}'(으)로 설정되었습니다.");
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nSCL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (CCL) 현재 레이어로 변경: 선택한 객체들을 현재 작업 레이어로 변경합니다.
        /// </summary>
        [CommandMethod("CCL")]
        public static void ChangeToCurrentLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId currentLayerId = db.Clayer;
            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n현재 레이어로 변경할 객체(들) 선택: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.LayerId = currentLayerId;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nCCL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (CTL) 현재 레이어로 복사: 객체를 현재 작업 레이어로 복사합니다.
        /// </summary>
        [CommandMethod("CTL")]
        public static void CopyToCurrentLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId currentLayerId = db.Clayer;
            ObjectId currentSpaceId = db.CurrentSpaceId;

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n현재 레이어로 복사할 객체(들) 선택: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = tr.GetObject(currentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        Entity clone = ent.Clone() as Entity;
                        if (clone == null) continue;

                        clone.LayerId = currentLayerId;

                        btr.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                    }
                    tr.Commit();
                }
                ed.Regen();
                ed.WriteMessage($"\n{psr.Value.Count}개 객체를 현재 레이어로 복사했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nCTL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (MEO) 다른 레이어로 변경
        /// <para>선택한 객체를 다른 (지정한) 레이어로 이동(변경)합니다.</para>
        /// </summary>
        [CommandMethod("MEO")]
        public static void MoveToOtherLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId targetLayerId = GetLayerIdFromUser(db, ed, "\n이동할 대상 레이어 이름 입력: ");
            if (targetLayerId.IsNull)
            {
                ed.WriteMessage("\n레이어 변경이 취소되었습니다.");
                return;
            }

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\n레이어를 변경할 객체(들) 선택: "
            };
            PromptSelectionResult psr = ed.GetSelection(pso);
            if (psr.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.LayerId = targetLayerId;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nMEO 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (MEL) 새 레이어로 변경
        /// <para>새 레이어를 만들면서 선택한 객체를 그 레이어로 즉시 변경합니다.</para>
        /// </summary>
        [CommandMethod("MEL")]
        public static void MoveToNewLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptStringOptions psoName = new PromptStringOptions("\n생성할 새 레이어 이름 입력: ");
            psoName.AllowSpaces = true;
            PromptResult pr = ed.GetString(psoName);
            if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
            {
                ed.WriteMessage("\n취소되었습니다.");
                return;
            }

            string newLayerName = pr.StringResult;
            ObjectId newLayerId;

            try
            {
                newLayerId = CreateLayerIfNotExists(db, newLayerName, Color.FromColorIndex(ColorMethod.ByAci, 7));
                if (newLayerId.IsNull)
                {
                    return;
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 생성 오류: {ex.Message}");
                return;
            }

            PromptSelectionOptions psoSel = new PromptSelectionOptions
            {
                MessageForAdding = $"\n'{newLayerName}' 레이어로 변경할 객체(들) 선택: "
            };
            PromptSelectionResult psr = ed.GetSelection(psoSel);
            if (psr.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.LayerId = newLayerId;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nMEL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (CEL) 다른 레이어로 복사 (구현 가이드)
        /// <para>선택한 객체를 다른 (지정한) 레이어로 복사합니다.</para>
        /// </summary>
        [CommandMethod("CEL")] 
        public static void CopyToOtherLayer()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nCEL 구현 가이드: MEO(대상 레이어 선택)와 CTL(복제) 로직을 조합하여 구현합니다.");
            ed.WriteMessage("1. GetLayerIdFromUser 헬퍼로 대상 레이어 ID 받기.");
            ed.WriteMessage("2. 객체 선택 (ed.GetSelection).");
            ed.WriteMessage("3. CTL 로직을 사용하되, `clone.LayerId = targetLayerId;` (1번에서 받은 ID)로 설정.");
        }


        #endregion

        // ===================================================================
        // 2. 레이어 켜기 / 끄기 (On / Off)
        // ===================================================================
        #region 2. 레이어 켜기 / 끄기 (On / Off)

        /// <summary>
        /// (LOF) 레이어 끄기: 선택한 객체의 레이어를 끕니다.
        /// </summary>
        [CommandMethod("LOF")]
        public static void LayerOff()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    if (ltr.Name != "0")
                    {
                        ltr.IsOff = true;
                    }
                },
                "\n끌 레이어의 객체 선택: "
            );
        }

        /// <summary>
        /// (LON) 모든 레이어 켜기: 모든 레이어를 켜고 동결 해제합니다.
        /// </summary>
        [CommandMethod("LON")]
        public static void LayerOn()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    ltr.IsFrozen = false;
                    if (ltr.Name != "0")
                    {
                        ltr.IsOff = false;
                    }
                },
                "\n모든 레이어를 켜고 동결 해제했습니다."
            );
        }

        /// <summary>
        /// (LOL) 선택한 레이어만 켜기 (Layer Isolate)
        /// <para>선택한 객체의 레이어만 켜고 나머지는 모두 끕니다.</para>
        /// </summary>
        [CommandMethod("LOL")]
        public static void LayerIsolate()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n켜둘 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;
                    ObjectId isolatedLayerId = ent.LayerId;

                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        if (layerId == isolatedLayerId || layerId == db.LayerZero)
                        {
                            LayerTableRecord ltrOn = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                            if (ltrOn.Name != "0") ltrOn.IsOff = false;
                            ltrOn.IsFrozen = false;
                        }
                        else
                        {
                            LayerTableRecord ltrOff = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                            if (ltrOff.Name != "0") ltrOff.IsOff = true;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLOL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (LBR) 레이어 끄기 상태 백업(Backup) 또는 복원(Restore)
        /// <para>현재 레이어의 켜기/끄기/잠금/동결 상태를 임시 저장하거나 복원합니다.</para>
        /// </summary>
        [CommandMethod("LBR")]
        public static void LayerBackupRestore()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;

            PromptKeywordOptions pko = new PromptKeywordOptions("\n[백업(B)/복원(R)] <백업>: ");
            pko.Keywords.Add("Backup", "B", "백업(B)", true, true);
            pko.Keywords.Add("Restore", "R", "복원(R)", true, true);
            pko.Keywords.Default = "Backup";
            pko.AllowNone = false;

            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK) return;

            if (pkr.StringResult == "Backup")
            {
                BackupLayerState(ed);
            }
            else if (pkr.StringResult == "Restore")
            {
                RestoreLayerState(ed);
            }
        }

        /// <summary>
        /// (FLO) 동결된 레이어만 켜기 (동결 해제)
        /// </summary>
        [CommandMethod("FLO")]
        public static void FrozenLayersOn()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    if (ltr.IsFrozen)
                    {
                        ltr.IsFrozen = false;
                    }
                },
                "\n동결된 레이어를 모두 켰습니다 (동결 해제)."
            );
        }

        /// <summary>
        /// (FOO) 동결(Frozen) 또는 꺼진(Off) 레이어만 켜기
        /// </summary>
        [CommandMethod("FOO")]
        public static void FrozenOrOffLayersOn()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    if (ltr.IsFrozen) ltr.IsFrozen = false;
                    if (ltr.IsOff && ltr.Name != "0") ltr.IsOff = false;
                },
                "\n동결되거나 꺼진 레이어를 모두 켰습니다."
            );
        }

        /// <summary>
        /// (OLO) 꺼진(Off) 레이어만 켜기
        /// </summary>
        [CommandMethod("OLO")]
        public static void OffLayersOn()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    if (ltr.IsOff && ltr.Name != "0")
                    {
                        ltr.IsOff = false;
                    }
                },
                "\n꺼진 레이어를 모두 켰습니다."
            );
        }

        #endregion

        // ===================================================================
        // 3. 레이어 동결 / 해제 (Freeze / Thaw)
        // ===================================================================
        #region 3. 레이어 동결 / 해제 (Freeze / Thaw)

        /// <summary>
        /// (LFR) 레이어 동결: 선택한 객체의 레이어를 동결합니다.
        /// </summary>
        [CommandMethod("LFR")]
        public static void LayerFreeze()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    if (ltr.Name != "0")
                    {
                        ltr.IsFrozen = true;
                    }
                },
                "\n동결할 레이어의 객체 선택: "
            );
        }

        /// <summary>
        /// (FRE) 선택 레이어외 모두 동결 (Freeze Except)
        /// <para>선택한 객체의 레이어만 남기고 나머지는 모두 동결합니다.</para>
        /// </summary>
        [CommandMethod("FRE")]
        public static void FreezeExceptSelected()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n동결에서 제외할(남겨둘) 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;
                    ObjectId isolatedLayerId = ent.LayerId;

                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        if (layerId == db.LayerZero) continue;

                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        if (layerId == isolatedLayerId)
                        {
                            ltr.IsFrozen = false;
                        }
                        else
                        {
                            ltr.IsFrozen = true;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nFRE 오류: {ex.Message}");
            }
        }





        #endregion

        // ===================================================================
        // 4. 레이어 잠금 / 해제 (Lock / Unlock)
        // ===================================================================
        #region 4. 레이어 잠금 / 해제 (Lock / Unlock)

        /// <summary>
        /// (LLO) 레이어 잠금: 선택한 객체의 레이어를 잠급니다.
        /// </summary>
        [CommandMethod("LLO")]
        public static void LayerLock()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    ltr.IsLocked = true;
                },
                "\n잠글 레이어의 객체 선택: "
            );
        }

        /// <summary>
        /// (ALO) 모든 레이어 잠금
        /// </summary>
        [CommandMethod("ALO")]
        public static void AllLayersLock()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    ltr.IsLocked = true;
                },
                "\n모든 레이어를 잠갔습니다."
            );
        }

        /// <summary>
        /// (LOE) 선택 레이어외 모두 잠금 (Lock Except)
        /// <para>선택한 객체의 레이어만 남기고 나머지는 모두 잠급니다.</para>
        /// </summary>
        [CommandMethod("LOE")]
        public static void LockExceptSelected()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n잠금에서 제외할(남겨둘) 레이어의 객체 선택: ");
            peo.AllowObjectOnLockedLayer = true;
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;
                    ObjectId isolatedLayerId = ent.LayerId;

                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        if (layerId == isolatedLayerId)
                        {
                            ltr.IsLocked = false;
                        }
                        else
                        {
                            ltr.IsLocked = true;
                        }
                    }
                    tr.Commit();
                }
                ed.WriteMessage("\n선택한 레이어를 제외하고 모두 잠갔습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLOE 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (LUL) 선택 레이어 잠금 해제
        /// </summary>
        [CommandMethod("LUL")]
        public static void LayerUnlock()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    ltr.IsLocked = false;
                },
                "\n잠금 해제할 레이어의 객체 선택: "
            );
        }

        /// <summary>
        /// (ULA) 모든 레이어 잠금 해제
        /// </summary>
        [CommandMethod("ULA")]
        public static void UnlockAllLayers()
        {
            IterateAllLayers(
                (ltr, db) =>
                {
                    ltr.IsLocked = false;
                },
                "\n모든 레이어의 잠금을 해제했습니다."
            );
        }

        #endregion

        // ===================================================================
        // 5. 레이어 플롯 (Plot) 설정
        // ===================================================================
        #region 5. 레이어 플롯 (Plot) 설정

        /// <summary>
        /// (LPF) 선택 레이어 플롯 안됨
        /// <para>선택한 객체의 레이어를 '플롯 안 함'(출력 안 됨)으로 설정합니다.</para>
        /// </summary>
        [CommandMethod("LPF")]
        public static void LayerPlotOff()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    ltr.IsPlottable = false;
                },
                "\n플롯 안 함으로 설정할 객체 선택: "
            );
        }

        /// <summary>
        /// (LPO) 선택 레이어 플롯 가능
        /// <para>선택한 객체의 레이어를 '플롯 가능'(출력 함)으로 설정합니다.</para>
        /// </summary>
        [CommandMethod("LPO")]
        public static void LayerPlotOn()
        {
            SetLayerPropertyBySelection(
                (ltr, db) =>
                {
                    ltr.IsPlottable = true;
                },
                "\n플롯 가능으로 설정할 객체 선택: "
            );
        }

        #endregion

        // ===================================================================
        // 6. 외부 참조 (Xref) 레이어
        // ===================================================================
        #region 6. 외부 참조 (Xref) 레이어

        /// <summary>
        /// (XLOF) 선택한 외부참조 레이어 끄기
        /// </summary>
        [CommandMethod("XLOF")]
        public static void XrefLayerOff()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n끌 외부참조(Xref) 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;

                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr == null) return;

                    if (ltr.Name.Contains("|"))
                    {
                        if (!ltr.Name.EndsWith("|0"))
                        {
                            ltr.IsOff = true;
                            ed.WriteMessage($"\nXref 레이어 '{ltr.Name}'를 껐습니다.");
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\n선택한 객체는 외부참조 레이어에 있지 않습니다.");
                    }

                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nXLOF 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (XLON) 모든 외부참조 레이어 켜기
        /// </summary>
        [CommandMethod("XLON")]
        public static void XrefLayerOn()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int count = 0;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        if (ltr != null && ltr.Name.Contains("|"))
                        {
                            if (!ltr.Name.EndsWith("|0"))
                            {
                                ltr.IsOff = false;
                            }
                            ltr.IsFrozen = false;
                            count++;
                        }
                    }
                    tr.Commit();
                }

                if (count > 0)
                {
                    ed.Regen();
                    ed.WriteMessage($"\n{count}개의 외부참조 레이어를 켜고 동결 해제했습니다.");
                }
                else
                {
                    ed.WriteMessage("\n켜거나 동결 해제할 외부참조 레이어가 없습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nXLON 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (XFC) 외부참조 색상 변경
        /// <para>지정한 Xref의 모든 레이어 색상을 일괄 변경합니다.</para>
        /// </summary>
        [CommandMethod("XFC")]
        public static void XrefFadeColorChange()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n색상을 변경할 외부참조(Xref) 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            PromptIntegerOptions pio = new PromptIntegerOptions("\n변경할 색상 번호 입력 (예: 8, 9, 250~255): ");
            pio.DefaultValue = 8;
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;

            Color newColor = Color.FromColorIndex(ColorMethod.ByAci, (short)pir.Value);
            string xrefPrefix = "";
            int count = 0;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;
                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;

                    if (ltr == null || !ltr.Name.Contains("|"))
                    {
                        ed.WriteMessage("\n선택한 객체는 외부참조 레이어에 있지 않습니다.");
                        return;
                    }

                    string[] nameParts = ltr.Name.Split('|');
                    if (nameParts.Length < 2) return;

                    xrefPrefix = nameParts[0] + "|";

                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltrLoop = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        if (ltrLoop.Name.StartsWith(xrefPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            ltrLoop.Color = newColor;
                            count++;
                        }
                    }
                    tr.Commit();
                }

                if (count > 0)
                {
                    ed.Regen();
                    ed.WriteMessage($"\n'{xrefPrefix.TrimEnd('|')}' Xref의 레이어 {count}개 색상을 {pir.Value}번으로 변경했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nXFC 오류: {ex.Message}");
            }
        }




        #endregion

        // ===================================================================
        // 7. 레이어 관리 및 도구
        // ===================================================================
        #region 7. 레이어 관리 및 도구 (Manage & Tools)

        /// <summary>
        /// (LWLK) 레이어별 보기 (구현 가이드)
        /// <para>대화상자에서 원하는 레이어만 선택하여 화면에 표시하고 나머지는 끕니다.</para>
        /// </summary>
        [CommandMethod("LWLK")]
        public static void LayerWalk()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nLWLK 구현 가이드: UI(WPF/Form)의 CheckedListBox로 레이어 목록을 표시하고,");
            ed.WriteMessage("선택 변경 시 IterateAllLayers 헬퍼를 호출하여 IsOff/IsFrozen 속성을 제어합니다.");
        }

        /// <summary>
        /// (MLAY) 레이어 만들기
        /// <para>여러 개의 레이어를 한 번에 생성합니다.</para>
        /// </summary>
        [CommandMethod("MLAY")]
        public static void MakeLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptStringOptions pso = new PromptStringOptions("\n생성할 새 레이어 이름 입력: ");
            pso.AllowSpaces = true;
            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
            {
                ed.WriteMessage("\n취소되었습니다.");
                return;
            }

            string newLayerName = pr.StringResult;

            try
            {
                CreateLayerIfNotExists(db, newLayerName, Color.FromColorIndex(ColorMethod.ByAci, 7));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (LALI) 레이어 목록 도면에 작성 (Table 객체 사용)
        /// <para>현재 도면의 레이어 목록을 표(Table) 형태로 도면에 그립니다.</para>
        /// </summary>
        [CommandMethod("LALI")]
        public static void LayerListToTable()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptPointOptions ppo = new PromptPointOptions("\n레이어 목록 표(Table) 삽입점 지정: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;

            Point3d insertionPoint = ppr.Value;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    Table tbl = new Table();
                    tbl.SetDatabaseDefaults(db);
                    tbl.Position = insertionPoint;

                    int layerCount = lt.Cast<ObjectId>().Count();
                    tbl.SetSize(layerCount + 1, 3);

                    tbl.SetRowHeight(10.0);
                    tbl.SetColumnWidth(50.0);

                    tbl.Cells[0, 0].TextString = "레이어 이름 (Name)";
                    tbl.Cells[0, 1].TextString = "색상 (Color)";
                    tbl.Cells[0, 2].TextString = "선종류 (Linetype)";
                    tbl.Rows[0].Style = "Title";

                    int currentRow = 1;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        LinetypeTableRecord ltrLinetype = tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;

                        tbl.Cells[currentRow, 0].TextString = ltr.Name;
                        tbl.Cells[currentRow, 1].TextString = ltr.Color.ToString();
                        tbl.Cells[currentRow, 2].TextString = ltrLinetype.Name;

                        currentRow++;
                    }

                    btr.AppendEntity(tbl);
                    tr.AddNewlyCreatedDBObject(tbl, true);

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLALI 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (WELN) 객체 레이어 이름 쓰기
        /// <para>선택한 객체의 레이어 이름을 문자(Text)로 객체 근처에 써줍니다.</para>
        /// </summary>
        [CommandMethod("WELN")]
        public static void WriteEntityLayerName()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n레이어 이름을 쓸 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
                    BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    string layerName = ltr.Name;
                    Point3d textPos = ent.GeometricExtents.MaxPoint;

                    DBText txt = new DBText
                    {
                        Position = textPos,
                        TextString = layerName,
                        Height = db.Textsize,
                        LayerId = db.Clayer
                    };

                    btr.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nWELN 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (DLF) 레이어 필터 삭제 (구현 가이드)
        /// <para>도면에 저장된 불필요한 레이어 필터를 삭제합니다.</para>
        /// </summary>
        [CommandMethod("DLF")]
        public static void DeleteLayerFilters()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nDLF 구현 가이드: `db.LayerFilters`를 통해 `LayerFilterCollection`에 접근, `LayerFilterTree`를 순회하며 필터를 `Remove()` 합니다.");
        }


        /// <summary>
        /// (DFO) 동결(Frozen) 또는 꺼진(Off) 레이어의 객체 삭제
        /// </summary>
        [CommandMethod("DFO")]
        public static void DeleteFrozenOrOffLayerObjects()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptKeywordOptions pko = new PromptKeywordOptions("\n경고: 동결되거나 꺼진 레이어의 *모든* 객체를 삭제합니다. [예(Y)/아니오(N)] <N>: ");
            pko.Keywords.Add("Yes", "Y", "예(Y)", true, true);
            pko.Keywords.Add("No", "N", "아니오(N)", true, true);
            pko.Keywords.Default = "No";
            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK || pkr.StringResult != "Yes")
            {
                ed.WriteMessage("\n취소되었습니다.");
                return;
            }

            List<string> layersToDelete = new List<string>();
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        if (ltr.IsOff || ltr.IsFrozen)
                        {
                            layersToDelete.Add(ltr.Name);
                        }
                    }
                }

                if (layersToDelete.Count == 0)
                {
                    ed.WriteMessage("\n삭제할 레이어가 없습니다.");
                    return;
                }

                string layerFilterString = string.Join(",", layersToDelete);
                TypedValue[] filter = new TypedValue[] {
                    new TypedValue((int)DxfCode.LayerName, layerFilterString)
                };
                SelectionFilter selFilter = new SelectionFilter(filter);

                PromptSelectionResult psr = ed.SelectAll(selFilter);
                if (psr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n삭제할 객체가 없습니다.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId id in psr.Value.GetObjectIds())
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            ent.Erase();
                        }
                    }
                    tr.Commit();
                    ed.WriteMessage($"\n{psr.Value.Count}개의 객체를 삭제했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nDFO 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (LC) 객체 색상별 레이어 정리 (구현 가이드)
        /// <para>객체의 색상(Color)을 기준으로 각각 다른 레이어로 자동 분리/정리합니다.</para>
        /// </summary>
        [CommandMethod("LC")]
        public static void LayerByColor()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nLC 구현 가이드: 1. 객체 선택. 2. `Dictionary<Color, ObjectId>` 생성.");
            ed.WriteMessage("3. 객체 순회하며 색상별로 `CreateLayerIfNotExists` 호출. 4. `ent.LayerId` 변경.");
        }

        /// <summary>
        /// (MLTF) 레이어 객체 파일로 저장 (구현 가이드)
        /// <para>각 레이어에 속한 객체들을 별개의 DWG 파일로 분리하여 저장합니다.</para>
        /// </summary>
        [CommandMethod("MLTF")]
        public static void MultiLayerToFiles()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nMLTF 구현 가이드: 1. LayerTable 순회. 2. 레이어별로 SelectAll(filter)로 객체 선택.");
            ed.WriteMessage("3. `ObjectIdCollection` 생성. 4. `db.Wblock(newDb, ...)` 호출. 5. `newDb.SaveAs(...)`");
        }

        /// <summary>
        /// (MDLA) 지정 레이어 일괄 삭제 (구현 가이드)
        /// <para>여러 도면 파일을 열지 않고, 특정 이름의 레이어를 해당 도면들에서 일괄 삭제합니다.</para>
        /// </summary>
        [CommandMethod("MDLA")]
        public static void MultiDeleteLayerAll()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nMDLA 구현 가이드: 1. OpenFileDialog로 다중 파일 선택.");
            ed.WriteMessage("2. `Database(false, true)` (ObjectDBX)로 각 파일을 백그라운드에서 엽니다.");
            ed.WriteMessage("3. `dbx.ReadDwgFile(...)`. 4. DFO/LME 로직으로 레이어 객체 삭제/이동 후 레이어 Erase.");
            ed.WriteMessage("5. `dbx.SaveAs(...)`로 덮어쓰기.");
        }


        /// <summary>
        /// (LME) 레이어 병합
        /// <para>여러 레이어를 하나의 대상 레이어로 합칩니다. (수동 병합/삭제 로직)</para>
        /// </summary>
        [CommandMethod("LME")]
        public static void LayerMerge()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptSelectionOptions psoSrc = new PromptSelectionOptions
            {
                MessageForAdding = "\n병합할 원본 레이어의 객체(들) 선택: "
            };
            PromptSelectionResult psr = ed.GetSelection(psoSrc);
            if (psr.Status != PromptStatus.OK) return;

            PromptEntityOptions peo = new PromptEntityOptions("\n병합될 대상 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            ObjectId targetLayerId = ObjectId.Null;
            List<string> sourceLayerNames = new List<string>();
            List<ObjectId> sourceLayerIds = new List<ObjectId>();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity targetEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    targetLayerId = targetEnt.LayerId;

                    Dictionary<ObjectId, string> uniqueSourceLayers = new Dictionary<ObjectId, string>();
                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity sourceEnt = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                        if (sourceEnt.LayerId != targetLayerId && !uniqueSourceLayers.ContainsKey(sourceEnt.LayerId))
                        {
                            LayerTableRecord ltr = tr.GetObject(sourceEnt.LayerId, OpenMode.ForRead) as LayerTableRecord;
                            uniqueSourceLayers.Add(ltr.ObjectId, ltr.Name);
                        }
                    }
                    sourceLayerIds = uniqueSourceLayers.Keys.ToList();
                    sourceLayerNames = uniqueSourceLayers.Values.ToList();
                }

                if (sourceLayerIds.Count == 0)
                {
                    ed.WriteMessage("\n병합할 원본 레이어가 없습니다.");
                    return;
                }

                string layerFilterString = string.Join(",", sourceLayerNames);
                TypedValue[] filter = new TypedValue[] {
                    new TypedValue((int)DxfCode.LayerName, layerFilterString)
                };
                SelectionFilter selFilter = new SelectionFilter(filter);
                PromptSelectionResult psrToMove = ed.SelectAll(selFilter);

                int movedCount = 0;
                int erasedCount = 0;

                using (doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (psrToMove.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId id in psrToMove.Value.GetObjectIds())
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                            if (ent != null)
                            {
                                ent.LayerId = targetLayerId;
                                movedCount++;
                            }
                        }
                    }

                    foreach (ObjectId sourceId in sourceLayerIds)
                    {
                        LayerTableRecord ltr = tr.GetObject(sourceId, OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr != null && !ltr.IsErased)
                        {
                            ltr.Erase(true);
                            erasedCount++;
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n{movedCount}개 객체를 이동하고, {erasedCount}개 레이어를 병합/삭제했습니다.");
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLME 오류: {ex.Message}");
            }
        }

        #endregion

        // ===================================================================
        // 8. 레이어 특성 변경
        // ===================================================================
        #region 8. 레이어 특성 변경 (Property Change)

        /// <summary>
        /// (LP) 레이어 특성 변경 (구현 가이드)
        /// <para>선택한 객체의 레이어 특성(색상, 선종류, 선가중치 등)을 변경합니다.</para>
        /// </summary>
        [CommandMethod("LP")]
        public static void LayerPropertyChange()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nLP 구현 가이드: 1. PromptKeywordOptions로 '색상(C)', '선종류(L)' 등 변경할 특성 입력.");
            ed.WriteMessage("2. `ed.GetEntity()`로 객체 선택. 3. `LayerTableRecord`를 열고 `ltr.Color` 또는 `ltr.LinetypeObjectId` 등 해당 속성 변경.");
        }

        /// <summary>
        /// (MOLP) 특정 레이어 특성 일괄 수정 (구현 가이드)
        /// <para>여러 도면 파일을 열지 않고, 특정 레이어의 특성을 일괄 변경합니다.</para>
        /// </summary>
        [CommandMethod("MOLP")]
        public static void ModifyObjectLayerPropertyBatch()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nMOLP 구현 가이드: 1. OpenFileDialog로 다중 파일 선택.");
            ed.WriteMessage("2. `Database(false, true)` (ObjectDBX)로 각 파일을 백그라운드에서 엽니다.");
            ed.WriteMessage("3. `dbx.ReadDwgFile(...)`. 4. `LayerTable`에서 대상 레이어를 찾아 `ltr.Color` 등 속성 변경.");
            ed.WriteMessage("5. `dbx.SaveAs(...)`로 덮어쓰기.");
        }

        /// <summary>
        /// (MLS) 레이어 상태 일괄 적용 (구현 가이드)
        /// <para>현재 도면의 레이어 상태를 다른 도면 파일들에도 동일하게 적용합니다.</para>
        /// </summary>
        [CommandMethod("MLS")]
        public static void MatchLayerStateBatch()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nMLS 구현 가이드: 1. 현재 도면의 레이어 상태를 `Dictionary<string, LayerState>`에 저장.");
            ed.WriteMessage("2. (MOLP와 동일) ObjectDBX로 대상 파일들을 열고, 딕셔너리를 참조하여 `ltr.IsOff`, `ltr.IsFrozen` 등 상태를 일괄 적용.");
        }


        /// <summary>
        /// (LLP) 레이어 색상 변경
        /// <para>선택한 객체의 레이어 색상을 변경합니다.</para>
        /// </summary>
        [CommandMethod("LLP")]
        public static void LayerColorChange()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n색상을 변경할 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            PromptIntegerOptions pio = new PromptIntegerOptions("\n새 색상 번호 (1~255) 입력: ");
            pio.AllowZero = false;
            pio.AllowNegative = false;
            PromptIntegerResult pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;

            Color newColor = Color.FromColorIndex(ColorMethod.ByAci, (short)pir.Value);

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForWrite) as LayerTableRecord;

                    ltr.Color = newColor;

                    tr.Commit();
                    ed.WriteMessage($"\n'{ltr.Name}' 레이어의 색상을 {pir.Value}번으로 변경했습니다.");
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nLLP 오류: {ex.Message}");
            }
        }



        /// <summary>
        /// (REL) 레이어 이름 변경
        /// <para>지정한 레이어의 이름을 변경합니다.</para>
        /// </summary>
        [CommandMethod("REL")]
        public static void RenameLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptStringOptions psoOld = new PromptStringOptions("\n이름을 변경할 현재 레이어 이름: ");
            PromptResult prOld = ed.GetString(psoOld);
            if (prOld.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(prOld.StringResult)) return;

            PromptStringOptions psoNew = new PromptStringOptions($"\n'{prOld.StringResult}'의 새 레이어 이름: ");
            PromptResult prNew = ed.GetString(psoNew);
            if (prNew.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(prNew.StringResult)) return;

            string oldName = prOld.StringResult;
            string newName = prNew.StringResult;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(oldName))
                    {
                        ed.WriteMessage($"\n레이어 '{oldName}'가(이) 존재하지 않습니다.");
                        return;
                    }
                    if (lt.Has(newName))
                    {
                        ed.WriteMessage($"\n레이어 '{newName}'가(이) 이미 존재합니다.");
                        return;
                    }

                    LayerTableRecord ltr = tr.GetObject(lt[oldName], OpenMode.ForWrite) as LayerTableRecord;

                    if (ltr.Name.Contains("|") ||
                        ltr.Name.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                        ltr.Name.Equals("Defpoints", StringComparison.OrdinalIgnoreCase))
                    {
                        ed.WriteMessage($"\n표준 또는 Xref 종속 레이어는 이름을 변경할 수 없습니다.");
                        return;
                    }

                    ltr.Name = newName;
                    tr.Commit();
                    ed.WriteMessage($"\n레이어 '{oldName}' -> '{newName}'(으)로 변경했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nREL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (CLN) 레이어 이름 일괄 변환 (찾기/바꾸기)
        /// <para>여러 레이어 이름의 일부 문자열을 찾아 바꿉니다.</para>
        /// </summary>
        [CommandMethod("CLN")]
        public static void ChangeLayerNamesBatch()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptStringOptions psoFind = new PromptStringOptions("\n찾을 문자열: ");
            PromptResult prFind = ed.GetString(psoFind);
            if (prFind.Status != PromptStatus.OK) return;
            string findStr = prFind.StringResult;

            PromptStringOptions psoReplace = new PromptStringOptions("\n바꿀 문자열: ");
            PromptResult prReplace = ed.GetString(psoReplace);
            if (prReplace.Status != PromptStatus.OK) return;
            string replaceStr = prReplace.StringResult;

            if (string.IsNullOrEmpty(findStr)) return;

            int count = 0;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                        if (ltr.Name.Contains("|") || ltr.Name.Equals("0") || ltr.Name.Equals("Defpoints"))
                        {
                            continue;
                        }

                        if (ltr.Name.IndexOf(findStr, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string newName = ltr.Name.Replace(findStr, replaceStr);

                            if (lt.Has(newName) && !newName.Equals(ltr.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                ed.WriteMessage($"\n경고: '{newName}' 레이어가 이미 존재하여 '{ltr.Name}'를 변경할 수 없습니다.");
                                continue;
                            }

                            ltr.Name = newName;
                            count++;
                        }
                    }
                    tr.Commit();
                }
                ed.WriteMessage($"\n{count}개 레이어의 이름을 변경했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nCLN 오류: {ex.Message}");
            }
        }

        #endregion

        // ===================================================================
        // 9. 블록 및 기타 레이어 관련
        // ===================================================================
        #region 9. 블록 및 기타 (Block & Others)

        /// <summary>
        /// (EEL) 블록을 레이어로 폭파 (Explode to Layer)
        /// <para>블록을 분해(Explode)하면서, 내부 객체들을 해당 블록의 레이어로 변경합니다.</para>
        /// </summary>
        [CommandMethod("EEL")]
        public static void ExplodeBlockToLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n레이어로 폭파할 블록 선택: ");
            peo.SetRejectMessage("\n블록 참조(BlockReference)만 선택 가능합니다.");
            peo.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            DBObjectCollection explodedObjects = new DBObjectCollection();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blkRef == null) return;

                    ObjectId blockLayerId = blkRef.LayerId;
                    BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    blkRef.UpgradeOpen();
                    blkRef.Explode(explodedObjects);

                    foreach (DBObject obj in explodedObjects)
                    {
                        Entity ent = obj as Entity;
                        if (ent != null)
                        {
                            ent.LayerId = blockLayerId;
                            btr.AppendEntity(ent);
                            tr.AddNewlyCreatedDBObject(ent, true);
                        }
                    }

                    blkRef.Erase();
                    tr.Commit();
                    ed.WriteMessage($"\n블록을 폭파하고 {explodedObjects.Count}개 객체를 원본 레이어로 변경했습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nEEL 오류: {ex.Message}");
            }
            finally
            {
                if (explodedObjects != null)
                {
                    foreach (DBObject obj in explodedObjects)
                    {
                        obj.Dispose();
                    }
                    explodedObjects.Dispose();
                }
            }
        }

        /// <summary>
        /// (RBC) 블록 레이어, 색상 변경 및 교체 (구현 가이드)
        /// <para>블록의 레이어, 색상 등을 변경하거나 다른 블록 정의로 교체합니다.</para>
        /// </summary>
        [CommandMethod("RBC")]
        public static void ReplaceBlockChange()
        {
            Editor ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nRBC 구현 가이드: 1. `ed.GetSelection()`로 'BlockReference' 객체 선택.");
            ed.WriteMessage("2. (속성 변경) `ent.LayerId = ...`, `ent.Color = ...`");
            ed.WriteMessage("3. (블록 교체) `ed.GetString()`로 새 블록 이름 입력 후 `br.BlockTableRecord = newBtrId;`");
        }


        /// <summary>
        /// (MABL) 블록과 내부객체 레이어 맞춤 (블록 정의 내부 객체를 Layer "0"로 변경)
        /// <para>블록 정의 내부 객체들의 레이어를 "0"으로 변경하여 레이어 상속이 잘 되도록 수정합니다.</para>
        /// </summary>
        [CommandMethod("MABL")]
        public static void MatchBlockInternalLayerToZero()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n내부 레이어를 '0'으로 맞출 블록 선택: ");
            peo.SetRejectMessage("\n블록 참조(BlockReference)만 선택 가능합니다.");
            peo.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            int count = 0;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blkRef == null) return;

                    ObjectId btrId = blkRef.IsDynamicBlock ? blkRef.DynamicBlockTableRecord : blkRef.BlockTableRecord;
                    if (btrId.IsNull) return;

                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForWrite) as BlockTableRecord;

                    foreach (ObjectId objId in btr)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                        {
                            if (ent.LayerId != db.LayerZero)
                            {
                                ent.LayerId = db.LayerZero;
                                count++;
                            }
                        }
                    }

                    tr.Commit();

                    if (count > 0)
                    {
                        ed.WriteMessage($"\n'{btr.Name}' 블록 정의 내부 객체 {count}개의 레이어를 '0'으로 변경했습니다.");
                        ed.Regen();
                    }
                    else
                    {
                        ed.WriteMessage($"\n'{btr.Name}' 블록 정의는 이미 내부 객체 레이어가 '0'입니다.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nMABL 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (DRLA) 레이어별 Draworder (선택한 객체의 레이어를 맨 위로)
        /// <para>선택한 객체의 레이어에 속한 모든 객체를 맨 위(DrawOrder)로 보냅니다.</para>
        /// </summary>
        [CommandMethod("DRLA")]
        public static void DrawOrderByLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\n맨 위로 보낼 레이어의 객체 선택: ");
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            string layerName = "";
            ObjectId currentSpaceId = db.CurrentSpaceId;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    layerName = ent.Layer;
                }

                TypedValue[] filter = new TypedValue[] {
                    new TypedValue((int)DxfCode.LayerName, layerName)
                };
                PromptSelectionResult psr = ed.SelectAll(new SelectionFilter(filter));
                if (psr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage($"\n'{layerName}' 레이어에 객체가 없습니다.");
                    return;
                }

                ObjectIdCollection objIds = new ObjectIdCollection(psr.Value.GetObjectIds());

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = tr.GetObject(currentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                    DrawOrderTable dot = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;

                    dot.MoveToTop(objIds);

                    tr.Commit();
                }
                ed.WriteMessage($"\n'{layerName}' 레이어의 객체 {objIds.Count}개를 맨 위로 보냈습니다.");
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nDRLA 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (OTL) 레이어로 Offset (원본 객체 레이어 기준)
        /// <para>Offset 실행 시 새 객체가 현재 레이어가 아닌 원본 레이어에 생성되도록 설정합니다.</para>
        /// </summary>
        [CommandMethod("OTL")]
        public static void OffsetToLayer()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            object oldOffsetLayer = AcApp.GetSystemVariable("OFFSETLAYER");

            try
            {
                AcApp.SetSystemVariable("OFFSETLAYER", 1);
                ed.Command("_.OFFSET");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nOTL 오류: {ex.Message}");
            }
            finally
            {
                AcApp.SetSystemVariable("OFFSETLAYER", oldOffsetLayer);
            }
        }

        #endregion

        // ===================================================================
        // Private 헬퍼 함수 (Helper Methods)
        // ===================================================================
        #region Private 헬퍼 함수 (Helper Methods)

        /// <summary>
        /// (헬퍼) 객체를 선택하여 해당 레이어의 속성을 변경합니다. (잠긴 레이어 선택 가능)
        /// </summary>
        private static void SetLayerPropertyBySelection(System.Action<LayerTableRecord, Database> layerAction, string promptMessage)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions(promptMessage);
            peo.AllowObjectOnLockedLayer = true;

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) return;

                    LayerTableRecord ltr = tr.GetObject(ent.LayerId, OpenMode.ForWrite) as LayerTableRecord;
                    if (ltr != null)
                    {
                        layerAction(ltr, db);
                    }

                    tr.Commit();
                }
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 속성 변경 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (헬퍼) 모든 레이어를 순회하며 속성을 변경합니다.
        /// </summary>
        private static void IterateAllLayers(System.Action<LayerTableRecord, Database> layerAction, string successMessage)
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr != null)
                        {
                            layerAction(ltr, db);
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
                if (!string.IsNullOrEmpty(successMessage))
                {
                    ed.WriteMessage(successMessage);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n전체 레이어 변경 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (헬퍼) LBR - 현재 모든 레이어의 상태를 백업합니다.
        /// </summary>
        private static void BackupLayerState(Editor ed)
        {
            Database db = ed.Document.Database;
            _layerStateBackup = new Dictionary<ObjectId, LayerState>();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layerId in lt)
                    {
                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        _layerStateBackup[layerId] = new LayerState
                        {
                            IsOff = ltr.IsOff,
                            IsFrozen = ltr.IsFrozen,
                            IsLocked = ltr.IsLocked
                        };
                    }
                }
                ed.WriteMessage($"\n{_layerStateBackup.Count}개 레이어의 현재 상태를 백업했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 백업 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (헬퍼) LBR - 백업된 레이어 상태를 복원합니다.
        /// </summary>
        private static void RestoreLayerState(Editor ed)
        {
            if (_layerStateBackup == null)
            {
                ed.WriteMessage("\n백업된 레이어 상태가 없습니다. 먼저 [백업]을 실행하세요.");
                return;
            }

            Database db = ed.Document.Database;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var kvp in _layerStateBackup)
                    {
                        ObjectId layerId = kvp.Key;
                        LayerState state = kvp.Value;

                        if (!layerId.IsValid || layerId.IsErased) continue;

                        LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForWrite, false, true) as LayerTableRecord;
                        if (ltr == null) continue;

                        if (ltr.Name == "0")
                        {
                            ltr.IsLocked = state.IsLocked;
                        }
                        else
                        {
                            ltr.IsOff = state.IsOff;
                            ltr.IsFrozen = state.IsFrozen;
                            ltr.IsLocked = state.IsLocked;
                        }
                    }
                    tr.Commit();
                }
                ed.Regen();
                ed.WriteMessage($"\n백업된 레이어 상태를 복원했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n레이어 상태 복원 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// (헬퍼) 사용자에게 레이어 이름을 입력받아 유효한 ObjectId를 반환합니다.
        /// </summary>
        private static ObjectId GetLayerIdFromUser(Database db, Editor ed, string message)
        {
            PromptStringOptions pso = new PromptStringOptions(message);
            pso.AllowSpaces = true;
            PromptResult pr = ed.GetString(pso);

            if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
            {
                return ObjectId.Null;
            }

            string layerName = pr.StringResult;
            ObjectId layerId = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (lt.Has(layerName))
                {
                    layerId = lt[layerName];
                }
                else
                {
                    ed.WriteMessage($"\n레이어 '{layerName}'가 존재하지 않습니다.");
                }
            }

            return layerId;
        }

        /// <summary>
        /// (헬퍼) 이름과 색상으로 새 레이어를 생성합니다. (존재할 경우 ID만 반환)
        /// </summary>
        private static ObjectId CreateLayerIfNotExists(Database db, string layerName, Color color)
        {
            ObjectId layerId = ObjectId.Null;
            bool created = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;

                if (lt.Has(layerName))
                {
                    layerId = lt[layerName];
                    AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n레이어 '{layerName}'가(이) 이미 존재합니다.");
                }
                else
                {
                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = color,
                        LinetypeObjectId = (tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable)["Continuous"]
                    };

                    layerId = lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                    created = true;
                }
                tr.Commit();
            }

            if (created)
            {
                AcApp.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n레이어 '{layerName}'를 생성했습니다.");
            }

            return layerId;
        }

        #endregion
    }
}