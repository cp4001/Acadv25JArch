using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.PipeDiaCalc
{
    /// <summary>
    /// "Ainit" 명령어:
    /// 현재 도면의 NamedObjectsDictionary(NOD)에 AINIT_DEFAULTS 사전을 만들고
    /// DwgDefault의 static 값들을 Xrecord로 기록한다.
    /// 이미 사전이 존재하면 경고만 출력하고 종료한다.
    /// </summary>
    public class AinitCommand
    {
        public const string DICT_NAME = "AINIT_DEFAULTS";

        // ---- NOD Xrecord 키 이름 (DiaNote.BaseLen 의 도면별 영구 저장 키) ----
        public const string KEY_DiaNoteHeight = "DiaNoteHeight";

        // ---- 폴백 기본값 (NOD 에 기록이 없을 때 사용) ----
        public const double DEFAULT_DiaNoteHeight = 50.0;

        [CommandMethod("Ainit")]
        public void InitDwgDefaults()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary nod = (DBDictionary)tr.GetObject(
                    db.NamedObjectsDictionaryId, OpenMode.ForRead);

                if (nod.Contains(DICT_NAME))
                {
                    ed.WriteMessage(
                        "\n[Ainit] 경고: 이 도면에는 이미 '" + DICT_NAME +
                        "' 사전이 존재합니다. Ainit는 도면당 한 번만 실행할 수 있습니다.");
                    tr.Commit();
                    // 사전이 이미 있으니 탭도 보여야 함 (방어적 동기화)
                    Acadv25JArch.Ribbon.CollabRibbon.SetRibbonTabVisible(true);
                    return;
                }

                // NOD를 쓰기 모드로 업그레이드
                nod.UpgradeOpen();

                // AINIT_DEFAULTS 사전 생성
                DBDictionary ainitDict = new DBDictionary();
                nod.SetAt(DICT_NAME, ainitDict);
                tr.AddNewlyCreatedDBObject(ainitDict, true);

                // DwgDefault의 모든 값을 Xrecord로 기록
                WriteAllDefaults(tr, ainitDict);

                tr.Commit();

                ed.WriteMessage(
                    "\n[Ainit] '" + DICT_NAME + "' 사전이 생성되었습니다." +
                    "\n  - " + KEY_DiaNoteHeight + " = " + DEFAULT_DiaNoteHeight);
            }

            // Ainit 직후 BaseLen 을 NOD 의 DEFAULT 값과 동기화 (이후 RefreshDiaNoteHeight 가 표시)
            DiaNote.BaseLen = DEFAULT_DiaNoteHeight;

            // JArch 리본 탭 생성/표시 (Ainit 가 실행된 도면 표시) + 높이 표시 갱신
            Acadv25JArch.Ribbon.CollabRibbon.SetRibbonTabVisible(true);
            Acadv25JArch.Ribbon.CollabRibbon.RefreshDiaNoteHeight();
        }

        /// <summary>
        /// 모든 기본값을 Xrecord 로 기록 (Real DXF code 사용 — string 변환 없음).
        /// </summary>
        private void WriteAllDefaults(Transaction tr, DBDictionary dict)
        {
            WriteDoubleXrecord(tr, dict, KEY_DiaNoteHeight, DEFAULT_DiaNoteHeight);
        }

        private void WriteDoubleXrecord(Transaction tr, DBDictionary dict,
                                        string key, double value)
        {
            Xrecord xrec = new Xrecord();
            xrec.Data = new ResultBuffer(
                new TypedValue((int)DxfCode.Real, value));
            dict.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
