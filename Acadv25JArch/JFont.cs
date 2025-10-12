using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CADExtension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using FlowDirection = System.Windows.Forms.FlowDirection;
using Image = System.Drawing.Image;

namespace Acadv25JArch
{
    public class FontReplacer
    {
        [CommandMethod("ReplaceMissingFonts")]
        public void ReplaceMissingFonts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 대체할 폰트 이름 (여기서 변경)
            string replacementFont = "굴림.ttf"; // 또는 "arial.ttf" 등

            int replacedCount = 0;
            List<string> missingFonts = new List<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 1. TextStyleTable에서 누락된 폰트 확인 및 대체
                    TextStyleTable textStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                    foreach (ObjectId styleId in textStyleTable)
                    {
                        TextStyleTableRecord textStyle = tr.GetObject(styleId, OpenMode.ForWrite) as TextStyleTableRecord;

                        // 폰트 파일이 존재하는지 확인
                        string fontFileName = textStyle.FileName;
                        string bigFontFileName = textStyle.BigFontFileName;

                        bool fontMissing = false;

                        // 기본 폰트 확인
                        if (!string.IsNullOrEmpty(fontFileName) && !IsFontAvailable(fontFileName))
                        {
                            if (!missingFonts.Contains(fontFileName))
                                missingFonts.Add(fontFileName);
                            fontMissing = true;
                        }

                        // BigFont 확인
                        if (!string.IsNullOrEmpty(bigFontFileName) && !IsFontAvailable(bigFontFileName))
                        {
                            if (!missingFonts.Contains(bigFontFileName))
                                missingFonts.Add(bigFontFileName);
                            fontMissing = true;
                        }

                        // 누락된 폰트가 있으면 대체
                        if (fontMissing)
                        {
                            textStyle.FileName = replacementFont;
                            textStyle.BigFontFileName = ""; // BigFont 제거
                            replacedCount++;

                            ed.WriteMessage($"\n텍스트 스타일 '{textStyle.Name}' 폰트를 '{replacementFont}'로 대체했습니다.");
                        }
                    }

                    // 2. DBText 엔티티 확인 (단일 행 텍스트)
                    BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId objId in modelSpace)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent is DBText)
                        {
                            DBText text = ent as DBText;
                            TextStyleTableRecord textStyle = tr.GetObject(text.TextStyleId, OpenMode.ForRead) as TextStyleTableRecord;

                            if (!string.IsNullOrEmpty(textStyle.FileName) && !IsFontAvailable(textStyle.FileName))
                            {
                                // 이미 텍스트 스타일에서 대체했으므로 여기서는 로그만
                            }
                        }
                        else if (ent is MText)
                        {
                            // MText는 인라인 폰트 설정을 가질 수 있음
                            MText mtext = ent as MText;
                            TextStyleTableRecord textStyle = tr.GetObject(mtext.TextStyleId, OpenMode.ForRead) as TextStyleTableRecord;

                            if (!string.IsNullOrEmpty(textStyle.FileName) && !IsFontAvailable(textStyle.FileName))
                            {
                                // 이미 텍스트 스타일에서 대체했으므로 여기서는 로그만
                            }
                        }
                    }

                    tr.Commit();

                    // 결과 출력
                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage($"\n총 {replacedCount}개의 텍스트 스타일이 업데이트되었습니다.");

                    if (missingFonts.Count > 0)
                    {
                        ed.WriteMessage("\n\n누락된 폰트 목록:");
                        foreach (string font in missingFonts)
                        {
                            ed.WriteMessage($"\n  - {font}");
                        }
                    }

                    ed.WriteMessage("\n========================================");
                    ed.WriteMessage($"\n대체 폰트: {replacementFont}");
                    ed.WriteMessage("\n작업이 완료되었습니다!");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
            }

            // 도면 재생성
            ed.Regen();
        }

        // 폰트 파일이 시스템에 존재하는지 확인
        private bool IsFontAvailable(string fontFileName)
        {
            if (string.IsNullOrEmpty(fontFileName))
                return true;

            // AutoCAD 폰트 경로들
            string[] fontPaths = new string[]
            {
                @"C:\Windows\Fonts\",
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\Autodesk\AutoCAD 2025\Fonts\",
                // AutoCAD 설치 경로의 Fonts 폴더
                @"C:\Program Files\Autodesk\AutoCAD 2025\Fonts\",
                @"C:\Program Files\Autodesk\AutoCAD 2024\Fonts\",
                @"C:\Program Files\Autodesk\AutoCAD 2023\Fonts\"
            };

            foreach (string path in fontPaths)
            {
                if (Directory.Exists(path))
                {
                    string fullPath = Path.Combine(path, fontFileName);
                    if (File.Exists(fullPath))
                        return true;
                }
            }

            return false;
        }

        // 도면의 모든 레이아웃을 처리하는 확장 버전
        [CommandMethod("ReplaceAllMissingFonts")]
        public void ReplaceAllMissingFontsInAllLayouts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string replacementFont = "굴림.ttf";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // TextStyle 처리
                    TextStyleTable textStyleTable = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                    foreach (ObjectId styleId in textStyleTable)
                    {
                        TextStyleTableRecord textStyle = tr.GetObject(styleId, OpenMode.ForWrite) as TextStyleTableRecord;

                        if (!string.IsNullOrEmpty(textStyle.FileName) && !IsFontAvailable(textStyle.FileName))
                        {
                            textStyle.FileName = replacementFont;
                            textStyle.BigFontFileName = "";
                        }
                    }

                    // 모든 레이아웃 처리
                    BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    foreach (ObjectId btrId in blockTable)
                    {
                        BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                        // 텍스트 엔티티만 처리하면 됨 (스타일이 이미 업데이트됨)
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n모든 레이아웃의 누락된 폰트를 '{replacementFont}'로 대체했습니다.");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n오류 발생: {ex.Message}");
                    tr.Abort();
                }
            }

            ed.Regen();
        }
    }
}
