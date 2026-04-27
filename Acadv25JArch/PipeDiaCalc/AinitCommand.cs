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

        // ---- Xrecord 키 이름 (DwgDefault 필드와 1:1 매칭) ----
        public const string KEY_DiaNoteHeight = "DiaNoteHeight";

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
                    "\n  - " + KEY_DiaNoteHeight + " = " + DwgDefault.DiaNoteHeight);
            }
        }

        /// <summary>
        /// DwgDefault의 모든 필드를 Xrecord로 기록.
        /// 새 상수 추가 시 이 메서드와 DwgDefaultLoader.LoadAllDefaults 양쪽에 같은 키로 추가.
        /// </summary>
        private void WriteAllDefaults(Transaction tr, DBDictionary dict)
        {
            WriteStringXrecord(tr, dict, KEY_DiaNoteHeight, DwgDefault.DiaNoteHeight);

            // 추후 추가:
            // WriteStringXrecord(tr, dict, KEY_Xxx, DwgDefault.Xxx);
        }

        private void WriteStringXrecord(Transaction tr, DBDictionary dict,
                                        string key, string value)
        {
            Xrecord xrec = new Xrecord();
            xrec.Data = new ResultBuffer(
                new TypedValue((int)DxfCode.Text, value ?? string.Empty));
            dict.SetAt(key, xrec);
            tr.AddNewlyCreatedDBObject(xrec, true);
        }
    }
}
