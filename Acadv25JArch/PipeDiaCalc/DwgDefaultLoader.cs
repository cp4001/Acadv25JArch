using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

// [assembly: ExtensionApplication(typeof(Acadv25JArch.PipeDiaCalc.DwgDefaultLoader))]

namespace Acadv25JArch.PipeDiaCalc
{
    /// <summary>
    /// AutoCAD 어셈블리 로드 시 자동 실행되는 IExtensionApplication.
    /// DocumentManager.DocumentCreated 이벤트에 핸들러를 등록하여
    /// DWG가 열릴 때마다 AINIT_DEFAULTS 사전을 읽어 DwgDefault의 static 값에 채워준다.
    /// </summary>
    public class DwgDefaultLoader : IExtensionApplication
    {
        public void Initialize()
        {
            // 도면이 새로 만들어지거나 열릴 때마다 호출됨
            Application.DocumentManager.DocumentCreated += OnDocumentCreated;

            // 어셈블리가 NETLOAD 등으로 늦게 로드되어
            // 이미 활성 문서가 존재하는 경우에도 즉시 로드 시도
            Document activeDoc = Application.DocumentManager.MdiActiveDocument;
            if (activeDoc != null)
            {
                LoadDwgDefaults(activeDoc);
            }
        }

        public void Terminate()
        {
            Application.DocumentManager.DocumentCreated -= OnDocumentCreated;
        }

        private void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            LoadDwgDefaults(e.Document);
        }

        /// <summary>
        /// 주어진 Document의 NOD에서 AINIT_DEFAULTS 사전을 찾아
        /// DwgDefault static 필드에 값을 채운다.
        /// 사전이 없으면 DwgDefault는 컴파일 시 기본값을 그대로 유지한다.
        /// </summary>
        public static void LoadDwgDefaults(Document doc)
        {
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary nod = (DBDictionary)tr.GetObject(
                        db.NamedObjectsDictionaryId, OpenMode.ForRead);

                    if (!nod.Contains(AinitCommand.DICT_NAME))
                    {
                        // 이 도면에는 아직 Ainit이 실행되지 않음 → 기본값 유지
                        tr.Commit();
                        return;
                    }

                    DBDictionary ainitDict = (DBDictionary)tr.GetObject(
                        nod.GetAt(AinitCommand.DICT_NAME), OpenMode.ForRead);

                    LoadAllDefaults(tr, ainitDict);

                    tr.Commit();

                    ed.WriteMessage(
                        "\n[DwgDefault] 로드 완료: " +
                        AinitCommand.KEY_DiaNoteHeight + " = " + DwgDefault.DiaNoteHeight);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                if (ed != null)
                    ed.WriteMessage("\n[DwgDefault] 로드 오류: " + ex.Message);
            }
        }

        /// <summary>
        /// AINIT_DEFAULTS 사전의 Xrecord를 읽어 DwgDefault static 필드에 대입.
        /// 새 상수 추가 시 AinitCommand.WriteAllDefaults 와 함께 이 메서드에도 같은 키로 추가.
        /// </summary>
        private static void LoadAllDefaults(Transaction tr, DBDictionary dict)
        {
            string v;

            v = ReadStringXrecord(tr, dict, AinitCommand.KEY_DiaNoteHeight);
            if (v != null) DwgDefault.DiaNoteHeight = v;

            // 추후 추가:
            // v = ReadStringXrecord(tr, dict, AinitCommand.KEY_Xxx);
            // if (v != null) DwgDefault.Xxx = v;
        }

        private static string ReadStringXrecord(Transaction tr, DBDictionary dict, string key)
        {
            if (!dict.Contains(key)) return null;

            Xrecord xrec = (Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForRead);
            ResultBuffer rb = xrec.Data;
            if (rb == null) return null;

            foreach (TypedValue tv in rb)
            {
                if (tv.TypeCode == (short)DxfCode.Text)
                {
                    return tv.Value == null ? null : tv.Value.ToString();
                }
            }
            return null;
        }
    }
}
