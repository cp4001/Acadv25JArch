using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class TextStyleModifier
    {
        [CommandMethod("UpdateTextStyles")]
        public void UpdateTextStyles()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 잠금 설정 (도면 수정 권한 확보)
            using (DocumentLock docLock = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 문자 스타일 테이블 열기
                    TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

                    int count = 0;

                    foreach (ObjectId id in tst)
                    {
                        // 각 스타일을 쓰기 모드로 열기
                        TextStyleTableRecord tstr = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForWrite);

                        // 1. 일반 SHX 글꼴 설정 (isocp.shx)
                        // 주의: 기존이 TrueType(TTF)이어도 SHX 파일명을 지정하면 SHX로 변경됩니다.
                        tstr.FileName = "isocp.shx";

                        // 2. 큰 글꼴(Big Font) 설정 (whgtxt.shx)
                        tstr.BigFontFileName = "whgtxt.shx";

                        count++;
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n총 {count}개의 문자 스타일이 'isocp.shx' 및 'whgtxt.shx'로 업데이트되었습니다.");

                    // 변경 사항 반영을 위해 화면 재생성 (Regen)
                    ed.Regen();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
            }
        }
    }
}
