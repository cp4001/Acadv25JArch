using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.PolyView
{
    /// <summary>
    /// POLYVIEW : 닫힌 Poly 하나를 PolyView 로 등록/수정
    /// PLVM     : PolyView 관리 팔레트 표시
    /// 사양: PolyViewManager.md
    /// </summary>
    public class PolyViewCommands
    {
        [CommandMethod("POLYVIEW")]
        public void Cmd_PolyView()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1~2. 닫힌 Polyline 선택
            var peo = new PromptEntityOptions("\nPolyView 로 지정할 닫힌 Poly 를 선택: ");
            peo.SetRejectMessage("\n닫힌 Polyline 만 지정할 수 있습니다.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            ObjectId id = per.ObjectId;
            string handle;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline? pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                if (pl == null) { ed.WriteMessage("\nPolyline 이 아닙니다."); return; }

                if (!pl.Closed)
                {
                    ed.WriteMessage("\n닫힌 Polyline 만 지정할 수 있습니다.");
                    return;
                }

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (pl.BlockId != bt[BlockTableRecord.ModelSpace])
                {
                    ed.WriteMessage("\n모델 공간의 Poly 만 등록할 수 있습니다.");
                    return;
                }

                handle = pl.Handle.ToString();
                tr.Commit();
            }

            // 기존 값은 스캔 결과에서 가져온다 (내용의 \n 이스케이프가 풀린 상태)
            var items = PolyViewStore.ScanModelSpace(db);
            string oldName = "";
            string oldMemo = "";
            bool isEdit = items.TryGetValue(handle, out PolyViewItem? existing);
            if (isEdit && existing != null) { oldName = existing.Name; oldMemo = existing.Memo; }

            // 4~5. 이름 입력 + 중복 검사
            string name;
            while (true)
            {
                var pso = new PromptStringOptions(isEdit ? "\nPolyView 이름" : "\nPolyView 이름: ")
                {
                    AllowSpaces = true
                };
                if (isEdit) { pso.UseDefaultValue = true; pso.DefaultValue = oldName; }

                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK) return;

                name = (pr.StringResult ?? "").Trim();
                if (name.Length == 0)
                {
                    ed.WriteMessage("\n이름은 비워 둘 수 없습니다.");
                    continue;
                }
                if (name.Length > PolyViewStore.MaxNameLen)
                {
                    ed.WriteMessage($"\n이름은 {PolyViewStore.MaxNameLen}자 이내여야 합니다.");
                    continue;
                }
                if (PolyViewStore.NameExists(items, name, handle))
                {
                    ed.WriteMessage($"\n'{name}' 은(는) 이미 사용 중인 이름입니다.");
                    continue;
                }
                break;
            }

            // 6. 내용 입력
            string memo;
            while (true)
            {
                var pso = new PromptStringOptions("\n내용(설명, 생략하려면 Enter)") { AllowSpaces = true };
                if (!string.IsNullOrEmpty(oldMemo)) { pso.UseDefaultValue = true; pso.DefaultValue = oldMemo; }

                PromptResult pr = ed.GetString(pso);
                if (pr.Status != PromptStatus.OK) return;

                memo = (pr.StringResult ?? "").Trim();
                if (memo.Length > PolyViewStore.MaxMemoLen)
                {
                    ed.WriteMessage($"\n내용은 {PolyViewStore.MaxMemoLen}자 이내여야 합니다.");
                    continue;
                }
                break;
            }

            // 7. XData 기록 + 되읽기 검증
            if (!PolyViewStore.WriteXData(db, id, name, memo))
            {
                ed.WriteMessage("\nXData 기록에 실패했습니다. 라이선스 상태를 확인하세요.");
                return;
            }

            // 8. 신규면 트리 Root 직속에 편입
            if (!isEdit)
            {
                PolyViewGroup root = PolyViewStore.LoadTree(db);
                root.Handles.Add(handle);
                PolyViewStore.SaveTree(db, root);
            }

            ed.WriteMessage(isEdit
                ? $"\nPolyView '{name}' 수정 완료."
                : $"\nPolyView '{name}' 등록 완료.");

            // 9. 팔레트 갱신
            PolyViewPalette.RefreshIfVisible();
        }

        [CommandMethod("PLVM")]
        public void Cmd_PolyViewManager()
        {
            PolyViewPalette.Show();
        }
    }
}
