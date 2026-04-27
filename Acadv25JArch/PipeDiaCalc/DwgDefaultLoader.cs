using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.PipeDiaCalc
{
    /// <summary>
    /// AINIT_DEFAULTS NOD 사전 ↔ DiaNote.BaseLen 영속화 어댑터.
    /// 어셈블리 등록은 MyPlugin.Initialize 가 처리 (Document 이벤트 후크).
    /// </summary>
    public static class DwgDefaultLoader
    {
        /// <summary>
        /// 도면 NOD 의 AINIT_DEFAULTS / DiaNoteHeight 를 읽어 DiaNote.BaseLen 에 설정.
        /// 사전이 없거나 키가 없으면 AinitCommand.DEFAULT_DiaNoteHeight 폴백.
        /// </summary>
        public static void LoadDwgDefaults(Document doc)
        {
            if (doc == null) return;

            bool ainitDictExists = false;
            double baseLen = AinitCommand.DEFAULT_DiaNoteHeight;
            Database db = doc.Database;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary nod = (DBDictionary)tr.GetObject(
                        db.NamedObjectsDictionaryId, OpenMode.ForRead);

                    if (nod.Contains(AinitCommand.DICT_NAME))
                    {
                        ainitDictExists = true;
                        DBDictionary ainitDict = (DBDictionary)tr.GetObject(
                            nod.GetAt(AinitCommand.DICT_NAME), OpenMode.ForRead);

                        double? v = ReadDoubleXrecord(tr, ainitDict, AinitCommand.KEY_DiaNoteHeight);
                        if (v.HasValue) baseLen = v.Value;
                    }
                    tr.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Editor ed = doc.Editor;
                if (ed != null) ed.WriteMessage("\n[DwgDefault] 로드 오류: " + ex.Message);
                // 폴백 그대로 적용. ainitDictExists 는 false 유지 → 탭 숨김
            }

            DiaNote.BaseLen = baseLen;
            // Ainit 가 실행된 도면이면 탭 보이고, 아니면 숨김 + 높이 표시 갱신
            Acadv25JArch.Ribbon.CollabRibbon.SetRibbonTabVisible(ainitDictExists);
            Acadv25JArch.Ribbon.CollabRibbon.RefreshDiaNoteHeight();
        }

        /// <summary>
        /// DiaNote.BaseLen 을 도면 NOD 에 영구 저장. AINIT_DEFAULTS 사전이 없으면 자동 생성.
        /// 저장 성공 시 DiaNote.BaseLen 도 갱신.
        /// </summary>
        public static bool SaveBaseLen(Document doc, double value)
        {
            if (doc == null) return false;
            Database db = doc.Database;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary nod = (DBDictionary)tr.GetObject(
                        db.NamedObjectsDictionaryId, OpenMode.ForWrite);

                    DBDictionary ainitDict;
                    if (nod.Contains(AinitCommand.DICT_NAME))
                    {
                        ainitDict = (DBDictionary)tr.GetObject(
                            nod.GetAt(AinitCommand.DICT_NAME), OpenMode.ForWrite);
                    }
                    else
                    {
                        ainitDict = new DBDictionary();
                        nod.SetAt(AinitCommand.DICT_NAME, ainitDict);
                        tr.AddNewlyCreatedDBObject(ainitDict, true);
                    }

                    Xrecord xrec;
                    if (ainitDict.Contains(AinitCommand.KEY_DiaNoteHeight))
                    {
                        xrec = (Xrecord)tr.GetObject(
                            ainitDict.GetAt(AinitCommand.KEY_DiaNoteHeight), OpenMode.ForWrite);
                    }
                    else
                    {
                        xrec = new Xrecord();
                        ainitDict.SetAt(AinitCommand.KEY_DiaNoteHeight, xrec);
                        tr.AddNewlyCreatedDBObject(xrec, true);
                    }
                    xrec.Data = new ResultBuffer(
                        new TypedValue((int)DxfCode.Real, value));

                    tr.Commit();
                }

                DiaNote.BaseLen = value;
                // SaveBaseLen 이 사전을 자동 생성했을 수 있으므로 탭 표시 보장 + 높이 표시 갱신
                Acadv25JArch.Ribbon.CollabRibbon.SetRibbonTabVisible(true);
                Acadv25JArch.Ribbon.CollabRibbon.RefreshDiaNoteHeight();
                return true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Editor ed = doc.Editor;
                if (ed != null) ed.WriteMessage("\n[DwgDefault] 저장 오류: " + ex.Message);
                return false;
            }
        }

        private static double? ReadDoubleXrecord(Transaction tr, DBDictionary dict, string key)
        {
            if (!dict.Contains(key)) return null;

            Xrecord xrec = (Xrecord)tr.GetObject(dict.GetAt(key), OpenMode.ForRead);
            ResultBuffer rb = xrec.Data;
            if (rb == null) return null;

            foreach (TypedValue tv in rb)
            {
                if (tv.TypeCode == (short)DxfCode.Real && tv.Value is double d)
                    return d;
            }
            return null;
        }
    }
}
