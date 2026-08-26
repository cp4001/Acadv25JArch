using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(JArch.XdataCommands))]
[assembly: ExtensionApplication(typeof(JArch.Startup))]

namespace JArch
{
    /// <summary>
    /// NETLOAD 시 AutoCAD 가 자동으로 Initialize() 를 호출한다.
    /// 여기서 .arx 를 로드하고 만료일을 명령창에 출력한다.
    /// </summary>
    public class Startup : IExtensionApplication
    {
        public void Initialize()
        {
            try {
                Xdata.EnsureArxLoaded();   // arx 자동 로드
                Xdata.ShowBanner();        // 만료일 프롬프트 출력 (+ 인터넷 시각 1회 확정)
            } catch {
                // 로드 단계 오류가 AutoCAD 시작을 막지 않도록 조용히 무시.
            }
        }

        public void Terminate() { }
    }

    public class XdataCommands
    {
        // 사용 예 : 선택한 객체에 Xdata("Group" = "FCD") 기록
        [CommandMethod("SETFCD")]
        public void SetFcd()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptSelectionResult res = ed.GetSelection();
            if (res.Status != PromptStatus.OK)
                return;

            int done = 0;
            foreach (SelectedObject so in res.Value) {
                if (so == null)
                    continue;
                Xdata.Set(so.ObjectId, "Group", "FCD");
                done++;
            }

            ed.WriteMessage("\n{0}개 객체에 Xdata (\"Group\" = \"FCD\") 를 기록했습니다.", done);
        }
    }
}
