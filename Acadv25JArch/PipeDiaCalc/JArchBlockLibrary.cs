using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using System.Reflection;

namespace PipeLoad2
{
    /// <summary>
    /// JArch 블럭 라이브러리 — 배포된 참조 도면(Blocks\JArch_Blocks.dwg)에서
    /// 블럭 정의를 현재 도면으로 가져온다. 이미 있으면 덮어쓴다(Replace).
    /// 동적 블럭의 파라미터/액션은 블럭 정의(BTR)의 extension dictionary 에 있으므로
    /// Database.Insert 가 아니라 WblockCloneObjects 로 정의 자체를 clone 해야 보존된다.
    /// </summary>
    public static class JArchBlockLibrary
    {
        public const string LibraryFileName = "JArch_Blocks.dwg";

        /// <summary>참조 도면 경로 — 이 DLL 폴더의 Blocks\JArch_Blocks.dwg.</summary>
        public static string LibraryPath =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                         "Blocks", LibraryFileName);

        /// <summary>
        /// 참조 도면의 blockName 정의를 db 로 가져온다(기존 정의는 덮어씀).
        /// 열린 Transaction 밖에서 호출할 것 — 가져온 뒤 새 Transaction 에서 bt[blockName] 을 조회한다.
        /// </summary>
        public static bool Import(Database db, Editor ed, string blockName)
        {
            string path = LibraryPath;
            if (!File.Exists(path))
            {
                ed.WriteMessage($"\n[오류] 블럭 참조 도면을 찾을 수 없습니다: {path}");
                return false;
            }

            using (var src = new Database(false, true))
            {
                try
                {
                    src.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, null);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n[오류] 참조 도면을 열 수 없습니다: {ex.Message}");
                    return false;
                }

                ObjectId srcBtrId;
                using (Transaction str = src.TransactionManager.StartTransaction())
                {
                    var srcBt = (BlockTable)str.GetObject(src.BlockTableId, OpenMode.ForRead);
                    if (!srcBt.Has(blockName))
                    {
                        ed.WriteMessage($"\n[오류] 참조 도면에 '{blockName}' 블럭이 없습니다: {path}");
                        return false;
                    }
                    srcBtrId = srcBt[blockName];
                    str.Commit();
                }

                var ids = new ObjectIdCollection { srcBtrId };
                var map = new IdMapping();
                try
                {
                    src.WblockCloneObjects(ids, db.BlockTableId, map,
                                           DuplicateRecordCloning.Replace, false);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n[오류] '{blockName}' 블럭을 가져오지 못했습니다: {ex.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}
