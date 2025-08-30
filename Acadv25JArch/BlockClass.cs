using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Exception = System.Exception;
using Image = System.Drawing.Image;

namespace Acadv25JArch
{
 

    /// <summary>
    /// 선택된 블록의 PreviewIcon 이미지를 224x224 PNG로 추출하는 서브루틴
    /// </summary>
    public class BlockIconExtractor_
    {
        /// <summary>
        /// 선택된 블록의 PreviewIcon을 224x224 PNG 파일로 추출
        /// </summary>
        /// <param name="blockRef">블록 참조 객체</param>
        /// <param name="outputPath">출력 파일 경로 (확장자 포함)</param>
        /// <returns>추출 성공 여부</returns>
        public static bool ExtractBlockIconImage(BlockReference blockRef, string outputPath)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 블록 정의 가져오기
                    BlockTableRecord blockDef = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);

                    if (blockDef == null)
                    {
                        doc.Editor.WriteMessage("\n블록 정의를 찾을 수 없습니다.");
                        return false;
                    }

                    // PreviewIcon에서 이미지 추출
                    var iconImage = ExtractFromPreviewIcon(blockDef);
                    Image image = iconImage as Image;

                    if (iconImage != null)
                    {

                        iconImage.Save(outputPath, ImageFormat.Png);
                        iconImage.Dispose();

                        doc.Editor.WriteMessage("\n✓ PreviewIcon에서 추출 완료");
                        tr.Commit();
                        return true;
                        // 224x224로 리사이즈하여 저장
                        //using (Bitmap resized = ResizeImage(iconImage, 224, 224))
                        //{
                        //    resized.Save(outputPath, ImageFormat.Png);
                        //    iconImage.Dispose();

                        //    doc.Editor.WriteMessage("\n✓ PreviewIcon에서 추출 완료");
                        //    tr.Commit();
                        //    return true;
                        //}
                    }
                    else
                    {
                        doc.Editor.WriteMessage("\n✗ PreviewIcon이 없거나 추출할 수 없습니다.");
                    }

                    tr.Commit();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// AutoCAD 내장 PreviewIcon에서 이미지 추출
        /// </summary>
        /// <param name="blockDef">블록 정의</param>
        /// <returns>추출된 Bitmap 또는 null</returns>
        public static Bitmap ExtractFromPreviewIcon(BlockTableRecord blockDef)
        {
            try
            {
                // PreviewIcon이 있는지 확인
                if (blockDef.PreviewIcon == null)
                {
                    return null;
                }

                // 다양한 크기로 시도 (큰 크기부터 더 좋은 품질)
                int[] sizes = { 32 };//{ 256, 128, 64, 48, 32, 16 }; //

                foreach (int size in sizes)
                {
                    try
                    {
                        Image thumbnailImage = blockDef.PreviewIcon.GetThumbnailImage(
                            size, size,
                            () => false,
                            IntPtr.Zero);

                        if (thumbnailImage != null)
                        {
                            // Image를 Bitmap으로 변환
                            Bitmap bitmap = new Bitmap(thumbnailImage);
                            thumbnailImage.Dispose();

                            Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\n  추출된 크기: {size}x{size}");
                            return bitmap;
                        }
                    }
                    catch
                    {
                        // 이 크기는 지원하지 않음, 다음 크기 시도
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\n  PreviewIcon 추출 실패: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 이미지를 지정된 크기로 리사이즈
        /// </summary>
        /// <param name="original">원본 이미지</param>
        /// <param name="width">목표 폭</param>
        /// <param name="height">목표 높이</param>
        /// <returns>리사이즈된 Bitmap</returns>
        public static Bitmap ResizeImage(Bitmap original, int width, int height)
        {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.White);

                // 원본 비율 유지하면서 중앙에 배치
                int sourceWidth = original.Width;
                int sourceHeight = original.Height;
                float ratio = Math.Min((float)width / sourceWidth, (float)height / sourceHeight);

                int newWidth = (int)(sourceWidth * ratio);
                int newHeight = (int)(sourceHeight * ratio);
                int x = (width - newWidth) / 2;
                int y = (height - newHeight) / 2;

                g.DrawImage(original, x, y, newWidth, newHeight);
            }
            return resized;
        }

        /// <summary>
        /// 블록 정의의 PreviewIcon 정보 분석
        /// </summary>
        /// <param name="blockDef">블록 정의</param>
        public static void AnalyzeBlockPreviewIcon(BlockTableRecord blockDef)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;

                ed.WriteMessage($"\n=== 블록 '{blockDef.Name}' PreviewIcon 분석 ===");

                if (blockDef.PreviewIcon == null)
                {
                    ed.WriteMessage("\n  PreviewIcon: 없음");
                    return;
                }

                ed.WriteMessage("\n  PreviewIcon: 존재함");

                // 원본 이미지 정보
                try
                {
                    ed.WriteMessage($"\n  원본 크기: {blockDef.PreviewIcon.Width} x {blockDef.PreviewIcon.Height}");
                    ed.WriteMessage($"\n  픽셀 포맷: {blockDef.PreviewIcon.PixelFormat}");
                    ed.WriteMessage($"\n  해상도: {blockDef.PreviewIcon.HorizontalResolution:F1} x {blockDef.PreviewIcon.VerticalResolution:F1} DPI");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n  원본 정보 읽기 실패: {ex.Message}");
                }

                // 지원되는 썸네일 크기 테스트
                int[] testSizes = { 16, 24, 32, 40, 48, 64, 96, 128, 256 };
                ed.WriteMessage("\n  지원되는 썸네일 크기:");

                foreach (int size in testSizes)
                {
                    try
                    {
                        using (Image thumbnail = blockDef.PreviewIcon.GetThumbnailImage(
                            size, size, () => false, IntPtr.Zero))
                        {
                            if (thumbnail != null)
                            {
                                ed.WriteMessage($" {size}x{size}");
                            }
                        }
                    }
                    catch
                    {
                        // 이 크기는 지원하지 않음
                    }
                }

                ed.WriteMessage("\n");
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n분석 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 명령어: 선택된 블록의 PreviewIcon 추출
        /// </summary>
        [CommandMethod("EXTRACTBLOCKICON")]
        public static void ExtractSelectedBlockIcon()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 블록 선택
            PromptEntityOptions options = new PromptEntityOptions("\n블록을 선택하세요: ");
            options.SetRejectMessage("\n블록만 선택할 수 있습니다.");
            options.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult result = ed.GetEntity(options);
            if (result.Status != PromptStatus.OK)
                return;

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tr.GetObject(result.ObjectId, OpenMode.ForRead) as BlockReference;
                if (blockRef == null)
                {
                    ed.WriteMessage("\n선택된 객체가 블록이 아닙니다.");
                    return;
                }

                // 블록 정의 가져오기
                BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                string blockName = blockDef?.Name ?? "Unknown";

                // PreviewIcon 분석 표시
                ed.WriteMessage($"\n=== 블록 '{blockName}' 아이콘 추출 ===");
                AnalyzeBlockPreviewIcon(blockDef);

                // 출력 파일 경로 설정
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"{blockName}_icon_224x224.png"
                );

                // 아이콘 추출
                bool success = ExtractBlockIconImage(blockRef, outputPath);

                if (success)
                {
                    ed.WriteMessage($"\n✓ 블록 아이콘이 성공적으로 추출되었습니다: {outputPath}");

                    // 파일 크기 표시
                    if (File.Exists(outputPath))
                    {
                        FileInfo fileInfo = new FileInfo(outputPath);
                        ed.WriteMessage($"\n파일 크기: {fileInfo.Length:N0} bytes");
                    }
                }
                else
                {
                    ed.WriteMessage("\n✗ 블록 아이콘 추출에 실패했습니다.");
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// 명령어: 선택된 블록의 PreviewIcon 상세 분석
        /// </summary>
        [CommandMethod("ANALYZEBLOCKICON")]
        public static void AnalyzeSelectedBlockIcon()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 블록 선택
            PromptEntityOptions options = new PromptEntityOptions("\n분석할 블록을 선택하세요: ");
            options.SetRejectMessage("\n블록만 선택할 수 있습니다.");
            options.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult result = ed.GetEntity(options);
            if (result.Status != PromptStatus.OK)
                return;

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = tr.GetObject(result.ObjectId, OpenMode.ForRead) as BlockReference;
                if (blockRef == null)
                {
                    ed.WriteMessage("\n선택된 객체가 블록이 아닙니다.");
                    return;
                }

                BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                // 상세 분석 수행
                AnalyzeBlockPreviewIcon(blockDef);

                // 테스트 이미지 저장
                if (blockDef.PreviewIcon != null)
                {
                    try
                    {
                        Bitmap previewBitmap = ExtractFromPreviewIcon(blockDef);
                        if (previewBitmap != null)
                        {
                            string testPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                $"{blockDef.Name}_PreviewIcon_Test.png"
                            );

                            previewBitmap.Save(testPath, ImageFormat.Png);
                            previewBitmap.Dispose();

                            ed.WriteMessage($"\n✓ 테스트 이미지 저장: {testPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ed.WriteMessage($"\n테스트 이미지 저장 실패: {ex.Message}");
                    }
                }

                tr.Commit();
            }
        }
    }

    /// <summary>
    /// 추가 기능: 블록 아이콘 일괄 추출 유틸리티 (PreviewIcon 전용)
    /// </summary>
    public class BlockIconBatchExtractor
    {
        /// <summary>
        /// 도면 내 모든 블록 정의의 PreviewIcon을 일괄 추출
        /// </summary>
        [CommandMethod("EXTRACTALLBLOCKICONS")]
        public static void ExtractAllBlockIcons()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 출력 폴더 설정
            string outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "BlockIcons"
            );

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            int extractedCount = 0;
            int totalCount = 0;
            int previewIconCount = 0;
            int noPreviewIconCount = 0;

            ed.WriteMessage("\n=== 블록 아이콘 일괄 추출 시작 (PreviewIcon 전용) ===");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId blockId in blockTable)
                {
                    BlockTableRecord blockDef = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;

                    // 시스템 블록과 익명 블록 제외
                    if (blockDef.IsAnonymous || blockDef.IsLayout ||
                        blockDef.Name.StartsWith("*") || blockDef.Name.StartsWith("~"))
                        continue;

                    totalCount++;
                    ed.WriteMessage($"\n처리 중: {blockDef.Name} ({totalCount}번째)");

                    try
                    {
                        string outputPath = Path.Combine(outputFolder, $"{blockDef.Name}_224x224.png");

                        // PreviewIcon에서 이미지 추출
                        Bitmap iconImage = BlockIconExtractor_.ExtractFromPreviewIcon(blockDef);

                        if (iconImage != null)
                        {
                            using (Bitmap resized = BlockIconExtractor_.ResizeImage(iconImage, 224, 224))
                            {
                                resized.Save(outputPath, ImageFormat.Png);
                                iconImage.Dispose();
                                previewIconCount++;
                                extractedCount++;
                                ed.WriteMessage(" [PreviewIcon 추출]");
                            }
                        }
                        else
                        {
                            noPreviewIconCount++;
                            ed.WriteMessage(" [PreviewIcon 없음]");
                        }
                    }
                    catch (Exception ex)
                    {
                        ed.WriteMessage($"\n블록 '{blockDef.Name}' 처리 중 오류: {ex.Message}");
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n=== 일괄 추출 완료 ===");
            ed.WriteMessage($"\n총 블록 수: {totalCount}개");
            ed.WriteMessage($"\nPreviewIcon 보유: {previewIconCount}개");
            ed.WriteMessage($"\nPreviewIcon 없음: {noPreviewIconCount}개");
            ed.WriteMessage($"\n성공적으로 추출: {extractedCount}개");
            ed.WriteMessage($"\n출력 폴더: {outputFolder}");

            // 통계 요약
            if (totalCount > 0)
            {
                double successRate = (double)previewIconCount / totalCount * 100;
                ed.WriteMessage($"\nPreviewIcon 보유율: {successRate:F1}%");
            }
        }

        /// <summary>
        /// 명령어: PreviewIcon 통계 분석
        /// </summary>
        [CommandMethod("ANALYZEPREVIEWICONS")]
        public static void AnalyzePreviewIcons()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int totalCount = 0;
            int withPreviewIcon = 0;
            int withoutPreviewIcon = 0;

            ed.WriteMessage("\n=== 도면 내 블록 PreviewIcon 통계 분석 ===");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId blockId in blockTable)
                {
                    BlockTableRecord blockDef = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;

                    // 시스템 블록과 익명 블록 제외
                    if (blockDef.IsAnonymous || blockDef.IsLayout ||
                        blockDef.Name.StartsWith("*") || blockDef.Name.StartsWith("~"))
                        continue;

                    totalCount++;

                    if (blockDef.PreviewIcon != null)
                    {
                        withPreviewIcon++;
                        ed.WriteMessage($"\n✓ {blockDef.Name} - PreviewIcon 있음 ({blockDef.PreviewIcon.Width}x{blockDef.PreviewIcon.Height})");
                    }
                    else
                    {
                        withoutPreviewIcon++;
                        ed.WriteMessage($"\n✗ {blockDef.Name} - PreviewIcon 없음");
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n=== 분석 결과 ===");
            ed.WriteMessage($"\n총 블록 수: {totalCount}개");
            ed.WriteMessage($"\nPreviewIcon 보유: {withPreviewIcon}개");
            ed.WriteMessage($"\nPreviewIcon 없음: {withoutPreviewIcon}개");

            if (totalCount > 0)
            {
                double withRate = (double)withPreviewIcon / totalCount * 100;
                double withoutRate = (double)withoutPreviewIcon / totalCount * 100;
                ed.WriteMessage($"\nPreviewIcon 보유율: {withRate:F1}%");
                ed.WriteMessage($"\nPreviewIcon 없음율: {withoutRate:F1}%");
            }
        }

        /// <summary>
        /// 명령어: PreviewIcon이 있는 블록만 추출
        /// </summary>
        [CommandMethod("EXTRACTONLYPREVIEWICONS")]
        public static void ExtractOnlyPreviewIcons()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 출력 폴더 설정
            string outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "PreviewIcons_Only"
            );

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            int extractedCount = 0;
            int checkedCount = 0;

            ed.WriteMessage("\n=== PreviewIcon이 있는 블록만 추출 ===");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId blockId in blockTable)
                {
                    BlockTableRecord blockDef = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;

                    // 시스템 블록과 익명 블록 제외
                    if (blockDef.IsAnonymous || blockDef.IsLayout ||
                        blockDef.Name.StartsWith("*") || blockDef.Name.StartsWith("~"))
                        continue;

                    checkedCount++;

                    // PreviewIcon이 있는 블록만 처리
                    if (blockDef.PreviewIcon != null)
                    {
                        try
                        {
                            ed.WriteMessage($"\n추출 중: {blockDef.Name}");

                            string outputPath = Path.Combine(outputFolder, $"{blockDef.Name}_224x224.png");

                            Bitmap iconImage = BlockIconExtractor_.ExtractFromPreviewIcon(blockDef);
                            if (iconImage != null)
                            {
                                using (Bitmap resized = BlockIconExtractor_.ResizeImage(iconImage, 224, 224))
                                {
                                    resized.Save(outputPath, ImageFormat.Png);
                                    iconImage.Dispose();
                                    extractedCount++;
                                    ed.WriteMessage(" [완료]");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ed.WriteMessage($" [오류: {ex.Message}]");
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n=== 추출 완료 ===");
            ed.WriteMessage($"\n확인한 블록 수: {checkedCount}개");
            ed.WriteMessage($"\n추출된 블록 수: {extractedCount}개");
            ed.WriteMessage($"\n출력 폴더: {outputFolder}");
        }
    }



    //
    public class BlockImageSaveCommands
    {
        // Windows API for HBITMAP deletion
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        /// <summary>
        /// 선택된 블록을 이미지로 바탕화면에 저장하는 메인 명령
        /// </summary>
        [CommandMethod("SAVEBLOCKIMAGE")]
        public void SaveBlockImageToDesktop()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 블록 선택
                ObjectId blockId = SelectBlock(ed);
                if (blockId == ObjectId.Null)
                {
                    ed.WriteMessage("\n블록이 선택되지 않았습니다.");
                    return;
                }

                // 바탕화면 경로 생성
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blockRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null)
                    {
                        ed.WriteMessage("\n선택된 객체가 블록이 아닙니다.");
                        return;
                    }

                    // 블록 정의 가져오기
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    string blockName = blockDef.Name;

                    // 이미지 크기 설정 (사용자 지정 가능)
                    int imgWidth = 224;//512
                    int imgHeight = 224;//512
                    Autodesk.AutoCAD.Colors.Color backColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 7); // 흰색

                    // 블록 이미지 생성
                    IntPtr imgPtr = Utils.GetBlockImage(blockRef.BlockTableRecord, imgWidth, imgHeight, backColor);

                    if (imgPtr != IntPtr.Zero)
                    {
                        // IntPtr을 Bitmap으로 변환
                        Bitmap blockImage = ConvertIntPtrToBitmap(imgPtr, imgWidth, imgHeight);

                        // 파일명 생성 (시간 포함하여 중복 방지)
                        string fileName = $"{blockName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        string fullPath = Path.Combine(desktopPath, fileName);

                        // 이미지 저장
                        blockImage.Save(fullPath, ImageFormat.Png);
                        blockImage.Dispose();

                        ed.WriteMessage($"\n블록 이미지가 성공적으로 저장되었습니다: {fullPath}");

                        // HBITMAP 핸들 해제
                        DeleteObject(imgPtr);
                    }
                    else
                    {
                        ed.WriteMessage("\n블록 이미지를 생성할 수 없습니다.");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 블록 선택 함수
        /// </summary>
        private ObjectId SelectBlock(Editor ed)
        {
            // 블록만 선택할 수 있도록 필터 설정
            SelectionFilter filter = new SelectionFilter(new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT")
            });

            PromptSelectionOptions selOpts = new PromptSelectionOptions();
            selOpts.MessageForAdding = "\n이미지로 저장할 블록을 선택하세요: ";
            selOpts.SingleOnly = true;
            selOpts.SinglePickInSpace = true;

            PromptSelectionResult selResult = ed.GetSelection(selOpts, filter);

            if (selResult.Status == PromptStatus.OK && selResult.Value.Count > 0)
            {
                return selResult.Value[0].ObjectId;
            }

            return ObjectId.Null;
        }

        /// <summary>
        /// IntPtr을 Bitmap으로 변환하는 함수 (안전한 방법)
        /// </summary>
        private Bitmap ConvertIntPtrToBitmap(IntPtr imgPtr, int width, int height)
        {
            // Utils.GetBlockImage()가 반환하는 IntPtr은 HBITMAP 핸들이므로
            // Image.FromHbitmap()을 사용하여 직접 변환
            return new Bitmap(System.Drawing.Image.FromHbitmap(imgPtr));
        }

        /// <summary>
        /// 블록 이름으로 이미지 생성 및 저장하는 유틸리티 함수
        /// </summary>
        public static IntPtr GetBlockImagePointer(string blockName, Database db, int imgWidth, int imgHeight,
            Autodesk.AutoCAD.Colors.Color backColor)
        {
            ObjectId blockId = GetBlockTableRecordId(blockName, db);
            if (!blockId.IsNull)
            {
                return Utils.GetBlockImage(blockId, imgWidth, imgHeight, backColor);
            }
            else
            {
                throw new ArgumentException($"데이터베이스에서 블록 정의 '{blockName}'을 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 블록 이름으로 BlockTableRecord ID 가져오기
        /// </summary>
        private static ObjectId GetBlockTableRecordId(string blockName, Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt.Has(blockName))
                {
                    return bt[blockName];
                }
                tr.Commit();
            }
            return ObjectId.Null;
        }

        /// <summary>
        /// 도면의 모든 블록을 이미지로 저장하는 추가 기능
        /// </summary>
        [CommandMethod("SAVEALLBLOCKSIMAGE")]
        public void SaveAllBlocksImageToDesktop()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folderName = $"AutoCAD_Block_Images_{DateTime.Now:yyyyMMdd_HHmmss}";
                string saveFolderPath = Path.Combine(desktopPath, folderName);
                Directory.CreateDirectory(saveFolderPath);

                int imgWidth = 256;
                int imgHeight = 256;
                Autodesk.AutoCAD.Colors.Color backColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 7);

                int savedCount = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    foreach (ObjectId blockId in bt)
                    {
                        BlockTableRecord btr = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;

                        // 시스템 블록과 익명 블록 제외
                        if (!btr.IsAnonymous && !btr.IsLayout &&
                            !btr.Name.StartsWith("*") && btr.Name != "_ARCHTICK")
                        {
                            try
                            {
                                IntPtr imgPtr = Utils.GetBlockImage(blockId, imgWidth, imgHeight, backColor);

                                if (imgPtr != IntPtr.Zero)
                                {
                                    Bitmap blockImage = ConvertIntPtrToBitmap(imgPtr, imgWidth, imgHeight);
                                    string fileName = $"{btr.Name}.png";
                                    string fullPath = Path.Combine(saveFolderPath, fileName);

                                    blockImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                                    blockImage.Dispose();
                                    DeleteObject(imgPtr);

                                    savedCount++;
                                }
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage($"\n블록 '{btr.Name}' 이미지 생성 실패: {ex.Message}");
                            }
                        }
                    }
                    tr.Commit();
                }

                ed.WriteMessage($"\n{savedCount}개의 블록 이미지가 성공적으로 저장되었습니다: {saveFolderPath}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 사용자 정의 크기로 블록 이미지 저장
        /// </summary>
        [CommandMethod("SAVEBLOCKIMAGEWITHSIZE")]
        public void SaveBlockImageWithCustomSize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 이미지 크기 입력받기
                PromptIntegerOptions widthOpts = new PromptIntegerOptions("\n이미지 너비를 입력하세요 [기본값: 512]: ");
                widthOpts.DefaultValue = 512;
                widthOpts.AllowNegative = false;
                widthOpts.AllowZero = false;

                PromptIntegerResult widthResult = ed.GetInteger(widthOpts);
                if (widthResult.Status != PromptStatus.OK) return;

                PromptIntegerOptions heightOpts = new PromptIntegerOptions("\n이미지 높이를 입력하세요 [기본값: 512]: ");
                heightOpts.DefaultValue = 512;
                heightOpts.AllowNegative = false;
                heightOpts.AllowZero = false;

                PromptIntegerResult heightResult = ed.GetInteger(heightOpts);
                if (heightResult.Status != PromptStatus.OK) return;

                // 블록 선택
                ObjectId blockId = SelectBlock(ed);
                if (blockId == ObjectId.Null) return;

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blockRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    Autodesk.AutoCAD.Colors.Color backColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 7);
                    IntPtr imgPtr = Utils.GetBlockImage(blockRef.BlockTableRecord, widthResult.Value, heightResult.Value, backColor);

                    if (imgPtr != IntPtr.Zero)
                    {
                        Bitmap blockImage = ConvertIntPtrToBitmap(imgPtr, widthResult.Value, heightResult.Value);
                        string fileName = $"{blockDef.Name}_{widthResult.Value}x{heightResult.Value}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        string fullPath = Path.Combine(desktopPath, fileName);

                        blockImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                        blockImage.Dispose();
                        DeleteObject(imgPtr);

                        ed.WriteMessage($"\n블록 이미지가 성공적으로 저장되었습니다: {fullPath}");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }

    public class BlockCommands

    {
        const double rowHeight = 3.0, colWidth = 5.0;
        const double textHeight = rowHeight * 0.25;

        [CommandMethod("CBT")]

        static public void CreateBlockTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var db = doc.Database;
            var ed = doc.Editor;

            var pr = ed.GetPoint("\nEnter table insertion point");
            if (pr.Status != PromptStatus.OK)

                return;


            using (var tr = doc.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // Create the table, set its style and default row/column size
                var tb = new Table();
                tb.TableStyle = db.Tablestyle;
                tb.SetRowHeight(rowHeight);
                tb.SetColumnWidth(colWidth);
                tb.Position = pr.Value;

                // Set the header cell
                var head = tb.Cells[0, 0];
                head.Value = "Blocks";
                head.Alignment = CellAlignment.MiddleCenter;
                head.TextHeight = textHeight;


                // Insert an additional column
                tb.InsertColumns(0, colWidth, 1);


                // Loop through the blocks in the drawing, creating rows
                foreach (var id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);

                    // Only care about user-insertable blocks
                    if (!btr.IsLayout && !btr.IsAnonymous)
                    {
                        // Add a row
                        tb.InsertRows(tb.Rows.Count, rowHeight, 1);
                        var rowIdx = tb.Rows.Count - 1;

                        // The first cell will hold the block name
                        var first = tb.Cells[rowIdx, 0];
                        first.Value = btr.Name;
                        first.Alignment = CellAlignment.MiddleCenter;
                        first.TextHeight = textHeight;


                        // The second will contain a thumbnail of the block
                        var second = tb.Cells[rowIdx, 1];
                        second.BlockTableRecordId = id;

                    }

                }


                // Now we add the table to the current space

                var sp = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                sp.AppendEntity(tb);


                // And to the transaction, which we then commit
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();

            }

        }


        // Insert Block 
        [CommandMethod("Insert_Block")]
        public void InsertInCurrentUcs()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptResult pr = ed.GetString("\nEnter the block name: ");
            if (pr.Status != PromptStatus.OK)
                return;
            string bName = pr.StringResult;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(bName))
                {
                    ed.WriteMessage("\nCan't find '{0}' block.", bName);
                    return;
                }

                PromptPointResult ppr = ed.GetPoint("\nSpecify insertion point: ");
                if (ppr.Status != PromptStatus.OK)
                    return;

                // Get the current UCS Z axis (extrusion direction)
                Matrix3d ucsMat = ed.CurrentUserCoordinateSystem;
                CoordinateSystem3d ucs = ucsMat.CoordinateSystem3d;
                Vector3d zdir = ucsMat.CoordinateSystem3d.Zaxis;

                // Get the OCS corresponding to UCS Z axis
                Matrix3d ocsMat = MakeOcs(zdir);

                // Transform the input point from UCS to OCS
                Point3d pt = ppr.Value.TransformBy(ucsMat.PreMultiplyBy(ocsMat));

                // Get the X axis of the OCS
                Vector3d ocsXdir = ocsMat.CoordinateSystem3d.Xaxis;

                // Get the UCS rotation (angle between the OCS X axis and the UCS X axis)
                double rot = ocsXdir.GetAngleTo(ucs.Xaxis, zdir);

                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                BlockReference br = new BlockReference(pt, bt[bName]);
                br.Position = pt;
                br.Rotation = rot;
                br.Normal = zdir;



                // br.sc

                //Check Br is Dynamic
                //if (br.IsDynamicBlock)
                //{
                var props = br.DynamicBlockReferencePropertyCollection;

                foreach (DynamicBlockReferenceProperty prop in props)
                {
                    if (prop.PropertyName.Equals("d1"))
                    {
                        try
                        {
                            prop.Value = 200.0;
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n{ex.Message}");
                        }
                        break;
                    }
                }

                // br attributs

                foreach (ObjectId attId in br.AttributeCollection)
                {
                    var acAtt = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (acAtt == null) continue;

                    if (!acAtt.Tag.Equals("d1", StringComparison.CurrentCultureIgnoreCase)) continue;

                    acAtt.UpgradeOpen();
                    acAtt.TextString = "200";
                }


                //}


                btr.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);
                tr.Commit();
            }
        }

        // Return an OCS Matrix3d using the 'Arbitrary Axis Algoriythm'
        private Matrix3d MakeOcs(Vector3d zdir)
        {
            double d = 1.0 / 64.0;
            zdir = zdir.GetNormal();
            Vector3d xdir = Math.Abs(zdir.X) < d && Math.Abs(zdir.Y) < d ?
                Vector3d.YAxis.CrossProduct(zdir).GetNormal() :
                Vector3d.ZAxis.CrossProduct(zdir).GetNormal();
            Vector3d ydir = zdir.CrossProduct(xdir).GetNormal();
            return new Matrix3d(new double[16]{
                xdir.X, xdir.Y, xdir.Z, 0.0,
                ydir.X, ydir.Y, ydir.Z, 0.0,
                zdir.X, zdir.Y, zdir.Z, 0.0,
                0.0, 0.0, 0.0, 1.0});
        }


        //List Attribute
        [CommandMethod("LISTATT")]

        public void ListAttributes()
        {
            Editor ed =
              Application.DocumentManager.MdiActiveDocument.Editor;
            Database db =
              HostApplicationServices.WorkingDatabase;
            Transaction tr =
              db.TransactionManager.StartTransaction();

            // Start the transaction
            try
            {
                // Build a filter list so that only
                // block references are selected
                TypedValue[] filList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                SelectionFilter filter = new SelectionFilter(filList);
                PromptSelectionOptions opts = new PromptSelectionOptions();

                opts.MessageForAdding = "Select block references: ";
                PromptSelectionResult res = ed.GetSelection(opts, filter);


                // Do nothing if selection is unsuccessful
                if (res.Status != PromptStatus.OK)
                    return;

                SelectionSet selSet = res.Value;
                ObjectId[] idArray = selSet.GetObjectIds();

                foreach (ObjectId blkId in idArray)
                {
                    BlockReference blkRef =
                      (BlockReference)tr.GetObject(blkId,
                        OpenMode.ForRead);
                    BlockTableRecord btr =
                      (BlockTableRecord)tr.GetObject(
                        blkRef.BlockTableRecord,
                        OpenMode.ForRead
                      );

                    ed.WriteMessage("\nBlock: " + btr.Name);

                    btr.Dispose();

                    AttributeCollection attCol =

                      blkRef.AttributeCollection;

                    foreach (ObjectId attId in attCol)
                    {
                        AttributeReference attRef = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);


                        string str = ("\n  Attribute Tag: "
                            + attRef.Tag
                            + "\n    Attribute String: "
                            + attRef.TextString
                          );
                        ed.WriteMessage(str);
                    }
                }

                tr.Commit();
            }

            catch (Exception ex)
            {
                ed.WriteMessage(("Exception: " + ex.Message));
            }
            finally
            {
                tr.Dispose();
            }
        }


        // 선택된 BlcokReference의 기준 Block 내용 편집
        [CommandMethod("K2")]
        public void cmd_ChangeBlock()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = HostApplicationServices.WorkingDatabase;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Get block object from selection of user
                PromptEntityOptions peo = new PromptEntityOptions("\nPlease select block\n");
                peo.SetRejectMessage("Only block");
                peo.AddAllowedClass(typeof(BlockReference), false);

                var per = ed.GetEntity(peo);

                // Check object is suitable ?
                if (per.Status != PromptStatus.OK)
                    return;

                var blockref = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);

                var block = (BlockTableRecord)tr.GetObject(blockref.BlockTableRecord, OpenMode.ForWrite);

                // Check all objects in block (blocktablerecord)
                foreach (ObjectId id in block)
                {
                    DBObject ent_obj = tr.GetObject(id, OpenMode.ForWrite);
                    var ent = ent_obj as Entity;
                    ent.LineWeight = LineWeight.ByBlock;
                    //if (ent_obj is Line)
                    //{
                    //    var ln = ent_obj as Line;
                    //    ln.StartPoint = new Point3d(0, -3, 0);
                    //    ln.ColorIndex = 4;

                    //    ed.Regen(); // Update block after modified
                    //}
                }
                tr.Commit();
            }
        }

        // Block Count
        [CommandMethod("BB_Count")]
        static public void BlockTableCounter()
        {
            const double rowHeight = 1000, colWidth = 2000.0;
            const double textHeight = rowHeight * 0.25;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var db = doc.Database;
            var ed = doc.Editor;

            var pr = ed.GetPoint("\nEnter table insertion point");
            if (pr.Status != PromptStatus.OK)

                return;

            using (var tr = doc.TransactionManager.StartTransaction())
            {

                var blockRefs = JEntity.GetEntityAllByTpye<BlockReference>(JEntity.MakeSelFilter("INSERT"));
                if (blockRefs == null) return;

                var brGrps = blockRefs.GroupBy(x => JBlock.GetBtrFromBr(x).Name); // 종류별로 가져와서 이름 Sort


                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // Create the table, set its style and default row/column size
                var tb = new Table();
                tb.TableStyle = db.Tablestyle;
                tb.SetRowHeight(rowHeight);
                tb.SetColumnWidth(colWidth);
                tb.Position = pr.Value;

                // Set the header cell
                var head = tb.Cells[0, 0];
                head.Value = "Blocks";
                head.Alignment = CellAlignment.MiddleCenter;
                head.TextHeight = textHeight;


                // Insert an additional column
                tb.InsertColumns(1, colWidth + 50, 2);
                //tb.InsertColumns(0, colWidth, 2);

                // Loop through the blocks in the drawing, creating rows
                //foreach (var id in bt)
                //{
                foreach (var brg in brGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }

                    // Add a row
                    tb.InsertRows(tb.Rows.Count, rowHeight, 1);
                    var rowIdx = tb.Rows.Count - 1;

                    // The first cell will hold the block name                
                    var c1 = tb.Cells[rowIdx, 0];
                    c1.BlockTableRecordId = btr.Id;
                    // The 2nd  will contain a thumbnail of the block
                    var c2 = tb.Cells[rowIdx, 1];
                    c2.Value = btr.Name;
                    c2.Alignment = CellAlignment.MiddleCenter;
                    c2.TextHeight = textHeight;

                    // The 3nd cell will hold the block name
                    var c3 = tb.Cells[rowIdx, 2];
                    c3.Value = brg.Count();
                    c3.Alignment = CellAlignment.MiddleCenter;
                    c3.TextHeight = textHeight;


                }

                // Now we add the table to the current space

                var sp = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                sp.AppendEntity(tb);


                // And to the transaction, which we then commit
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();

            }

        }

        // Selected Block Count
        [CommandMethod("BB_Count1")]

        static public void SleBlockCounter()
        {
            const double rowHeight = 1000, colWidth = 2000.0;
            const double textHeight = rowHeight * 0.25;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var db = doc.Database;
            var ed = doc.Editor;



            using (var tr = doc.TransactionManager.StartTransaction())
            {

                //var blockRefs = JEntity.GetEntityAllByTpye<BlockReference>(JEntity.MakeSelFilter("INSERT"));
                List<BlockReference> blockRefs = JEntity.GetEntityByTpye<BlockReference>("대상을 선택 하세요?", JSelFilter.MakeFilterTypes("INSERT"));

                if (blockRefs == null) return;

                var pr = ed.GetPoint("\nEnter table insertion point");
                if (pr.Status != PromptStatus.OK)
                    return;

                var brGrps = blockRefs.GroupBy(x => JBlock.GetBtrFromBr(x).Name); // 종류별로 가져와서 이름 Sort


                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                // Create the table, set its style and default row/column size
                var tb = new Table();
                tb.TableStyle = db.Tablestyle;
                tb.SetRowHeight(rowHeight);
                tb.SetColumnWidth(colWidth);
                tb.Position = pr.Value;

                // Set the header cell
                var head = tb.Cells[0, 0];
                head.Value = "Blocks";
                head.Alignment = CellAlignment.MiddleCenter;
                head.TextHeight = textHeight;


                // Insert an additional column
                tb.InsertColumns(1, colWidth + 50, 2);
                //tb.InsertColumns(0, colWidth, 2);

                // Loop through the blocks in the drawing, creating rows
                //foreach (var id in bt)
                //{
                foreach (var brg in brGrps)
                {
                    ObjectId btrId = new ObjectId();
                    BlockTableRecord btr = new BlockTableRecord();
                    if (brg.First().IsDynamicBlock)
                    {
                        btrId = brg.First().DynamicBlockTableRecord;
                        btr = tr.GetObject(brg.First().DynamicBlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }
                    else
                    {
                        btrId = brg.First().BlockTableRecord;
                        btr = tr.GetObject(brg.First().BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    }

                    // Add a row
                    tb.InsertRows(tb.Rows.Count, rowHeight, 1);
                    var rowIdx = tb.Rows.Count - 1;

                    // The first cell will hold the block name                
                    var c1 = tb.Cells[rowIdx, 0];
                    c1.BlockTableRecordId = btr.Id;
                    // The 2nd  will contain a thumbnail of the block
                    var c2 = tb.Cells[rowIdx, 1];
                    c2.Value = btr.Name;
                    c2.Alignment = CellAlignment.MiddleCenter;
                    c2.TextHeight = textHeight;

                    // The 3nd cell will hold the block name
                    var c3 = tb.Cells[rowIdx, 2];
                    c3.Value = brg.Count();
                    c3.Alignment = CellAlignment.MiddleCenter;
                    c3.TextHeight = textHeight;


                }

                // Now we add the table to the current space

                var sp = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                sp.AppendEntity(tb);


                // And to the transaction, which we then commit
                tr.AddNewlyCreatedDBObject(tb, true);
                tr.Commit();

            }

        }



        // block Explode based by Table
        [CommandMethod("BB_Explode")]
        public static void ExplodeBlockFromTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = doc.TransactionManager.StartTransaction())
            {
                try
                {
                    // Step 1: 사용자가 Table의 셀을 직접 클릭하도록 함
                    var clickOptions = new PromptPointOptions("\nClick on table cell to explode blocks: ");
                    var clickResult = ed.GetPoint(clickOptions);
                    if (clickResult.Status != PromptStatus.OK)
                        return;

                    var clickPoint = clickResult.Value;

                    // Step 2: 클릭한 지점에서 Table 찾기 (Model Space만 검사)
                    Table table = null;
                    // 기존 코드: TableHitTestInfo hitInfo = null;
                    // 수정 코드: TableHitTestInfo는 struct이므로 null 할당 불가, 대신 default 값으로 초기화
                    TableHitTestInfo hitInfo = default;

                    // Model Space에서 Table 찾기
                    var modelSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId entityId in modelSpace)
                    {
                        var entity = tr.GetObject(entityId, OpenMode.ForRead);
                        if (entity is Table tableEntity)
                        {
                            try
                            {
                                // 이 Table에서 HitTest 실행
                                var testHit = tableEntity.HitTest(clickPoint, Vector3d.ZAxis);
                                //if (testHit != null)
                                //{
                                    table = tableEntity;
                                    hitInfo = testHit;
                                    break;
                                //}
                            }
                            catch
                            {
                                // HitTest 실패시 다음 Table 검사
                                continue;
                            }
                        }
                    }

                    // Step 3: Table이 클릭되었는지 확인
                    if (table == null)
                    {
                        ed.WriteMessage("\nNo table cell found at the clicked point. Please click on a table cell.");
                        return;
                    }

                    int row = hitInfo.Row;
                    int column = hitInfo.Column;

                    //// Step 4: 클릭한 row의 column 1에서 Block 이름 가져오기
                    //int row1 = hitInfo.Row;
                    //int column1 = hitInfo.Column;

                    ed.WriteMessage($"\nClicked: Row {row}, Column {column}");

                    // Block 이름은 항상 column 1에 있음
                    if (table.Columns.Count <= 1)
                    {
                        ed.WriteMessage("\nTable does not have enough columns.");
                        return;
                    }

                    string blockName = "";

                    try
                    {
                        var nameCell = table.Cells[row, 1]; // column 1에서 Block 이름 가져오기

                        if (nameCell.Value != null && nameCell.Value is string cellText)
                        {
                            blockName = cellText.Trim();
                        }

                        if (string.IsNullOrEmpty(blockName))
                        {
                            ed.WriteMessage($"\nNo block name found in row {row}, column 1.");
                            return;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nError reading block name from row {row}, column 1: {ex.Message}");
                        return;
                    }

                    ed.WriteMessage($"\nBlock name found: {blockName}");

                    // Step 5: 해당 Block들을 찾아서 Explode 실행
                    int explodedCount = ExplodeBlocksByName(tr, db, blockName);

                    if (explodedCount > 0)
                    {
                        ed.WriteMessage($"\n{explodedCount} blocks of type '{blockName}' were exploded.");
                        tr.Commit();
                    }
                    else
                    {
                        ed.WriteMessage($"\nNo blocks of type '{blockName}' found in the drawing.");
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 지정된 이름의 모든 Block Reference를 Model Space에서 찾아서 Explode 실행
        /// </summary>
        /// <param name="tr">Transaction</param>
        /// <param name="db">Database</param>
        /// <param name="blockName">Block 이름</param>
        /// <returns>Explode된 Block의 개수</returns>
        private static int ExplodeBlocksByName(Transaction tr, Database db, string blockName)
        {
            int explodedCount = 0;

            // Block Table에서 해당 이름의 Block이 있는지 확인
            var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            if (!bt.Has(blockName))
            {
                return 0; // Block이 존재하지 않음
            }

            var btrId = bt[blockName];

            // Model Space에서 해당 Block Reference들을 찾기
            var modelSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

            // 모든 Block Reference를 수집 (Explode 중에 컬렉션이 변경되는 것을 방지)
            var blockRefsToExplode = new System.Collections.Generic.List<ObjectId>();

            foreach (ObjectId id in modelSpace)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead);
                if (ent is BlockReference br)
                {
                    string refBlockName = "";

                    // Dynamic Block인지 확인
                    if (br.IsDynamicBlock)
                    {
                        var dynamicBtr = tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (dynamicBtr != null)
                        {
                            refBlockName = dynamicBtr.Name;
                        }
                    }
                    else
                    {
                        var normalBtr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (normalBtr != null)
                        {
                            refBlockName = normalBtr.Name;
                        }
                    }

                    // Block 이름이 일치하는 경우 리스트에 추가
                    if (string.Equals(refBlockName, blockName, StringComparison.OrdinalIgnoreCase))
                    {
                        blockRefsToExplode.Add(id);
                    }
                }
            }

            // 수집된 Block Reference들을 Explode
            foreach (var brId in blockRefsToExplode)
            {
                try
                {
                    var blockRef = tr.GetObject(brId, OpenMode.ForWrite) as BlockReference;
                    if (blockRef != null)
                    {
                        // Explode 실행
                        var explodedEntities = new DBObjectCollection();
                        blockRef.Explode(explodedEntities);

                        // Explode된 Entity들을 현재 공간에 추가
                        foreach (Entity explodedEnt in explodedEntities)
                        {
                            modelSpace.AppendEntity(explodedEnt);
                            tr.AddNewlyCreatedDBObject(explodedEnt, true);
                        }

                        // 원래 Block Reference 삭제
                        blockRef.Erase();
                        explodedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    // 개별 Block Explode 실패 시 계속 진행
                    var ed = Application.DocumentManager.MdiActiveDocument.Editor;
                    ed.WriteMessage($"\nFailed to explode block {brId}: {ex.Message}");
                }
            }

            return explodedCount;
        }

        /// <summary>
        /// Block Reference의 이름을 가져옵니다 (Dynamic Block 고려)
        /// </summary>
        /// <param name="tr">Transaction</param>
        /// <param name="br">Block Reference</param>
        /// <returns>Block 이름</returns>
        private static string GetBlockReferenceName(Transaction tr, BlockReference br)
        {
            try
            {
                if (br.IsDynamicBlock)
                {
                    var dynamicBtr = tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    return dynamicBtr?.Name ?? "";
                }
                else
                {
                    var normalBtr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    return normalBtr?.Name ?? "";
                }
            }
            catch
            {
                return "";
            }
        }
    }

    //
    public static class BlockUtilities
    {
        #region Block Size Functions

        /// <summary>
        /// BlockReference의 크기를 반환합니다 (변환이 적용된 실제 크기)
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>크기 정보 (Width, Height, Depth)</returns>
        public static BlockSize GetBlockSize(BlockReference blockRef, Transaction transaction)
        {
            if (blockRef == null || transaction == null)
                throw new ArgumentNullException();

            try
            {
                // BlockReference의 Bounds 사용 (변환이 적용된 실제 크기)
                if (blockRef.Bounds.HasValue)
                {
                    var bounds = blockRef.Bounds.Value;
                    return new BlockSize
                    {
                        Width = bounds.MaxPoint.X - bounds.MinPoint.X,
                        Height = bounds.MaxPoint.Y - bounds.MinPoint.Y,
                        Depth = bounds.MaxPoint.Z - bounds.MinPoint.Z,
                        MinPoint = bounds.MinPoint,
                        MaxPoint = bounds.MaxPoint
                    };
                }

                // Bounds가 없는 경우 BlockTableRecord에서 계산
                return GetBlockDefinitionSize(blockRef.BlockTableRecord, transaction);
            }
            catch (Exception)
            {
                return new BlockSize();
            }
        }

        /// <summary>
        /// BlockTableRecord의 정의 크기를 반환합니다 (원본 크기)
        /// </summary>
        /// <param name="blockTableRecordId">BlockTableRecord ObjectId</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>크기 정보</returns>
        public static BlockSize GetBlockDefinitionSize(ObjectId blockTableRecordId, Transaction transaction)
        {
            if (blockTableRecordId.IsNull || transaction == null)
                throw new ArgumentNullException();

            try
            {
                var btr = transaction.GetObject(blockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return new BlockSize();

                if (btr.Bounds.HasValue)
                {
                    var bounds = btr.Bounds.Value;
                    return new BlockSize
                    {
                        Width = bounds.MaxPoint.X - bounds.MinPoint.X,
                        Height = bounds.MaxPoint.Y - bounds.MinPoint.Y,
                        Depth = bounds.MaxPoint.Z - bounds.MinPoint.Z,
                        MinPoint = bounds.MinPoint,
                        MaxPoint = bounds.MaxPoint
                    };
                }

                return new BlockSize();
            }
            catch (Exception)
            {
                return new BlockSize();
            }
        }

        /// <summary>
        /// Explode 작업 결과를 담는 클래스
        /// </summary>
        public class ExplodeResults
        {
            public List<ObjectId> SuccessfulBlocks { get; set; }
            public List<ObjectId> FailedBlocks { get; set; }
            public List<ObjectId> CreatedEntities { get; set; }

            public ExplodeResults()
            {
                SuccessfulBlocks = new List<ObjectId>();
                FailedBlocks = new List<ObjectId>();
                CreatedEntities = new List<ObjectId>();
            }

            public int SuccessCount => SuccessfulBlocks.Count;
            public int FailureCount => FailedBlocks.Count;
            public int CreatedEntityCount => CreatedEntities.Count;

            public override string ToString()
            {
                return $"성공: {SuccessCount}, 실패: {FailureCount}, 생성된 Entity: {CreatedEntityCount}";
            }
        }

        #endregion

        #region Entity Count Functions

        /// <summary>
        /// Block에 포함된 전체 Entity 개수를 반환합니다
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>Entity 개수</returns>
        public static int GetEntityCount(BlockReference blockRef, Transaction transaction)
        {
            if (blockRef == null || transaction == null)
                return 0;

            return GetEntityCount(blockRef.BlockTableRecord, transaction);
        }

        /// <summary>
        /// BlockTableRecord에 포함된 Entity 개수를 반환합니다
        /// </summary>
        /// <param name="blockTableRecordId">BlockTableRecord ObjectId</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>Entity 개수</returns>
        public static int GetEntityCount(ObjectId blockTableRecordId, Transaction transaction)
        {
            if (blockTableRecordId.IsNull || transaction == null)
                return 0;

            try
            {
                var btr = transaction.GetObject(blockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return 0;

                // LINQ를 사용하여 Entity 개수 계산
                return btr.Cast<ObjectId>()
                         .Count(id => IsEntity(id));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Entity 타입별 개수를 반환합니다
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>타입별 개수 딕셔너리</returns>
        public static Dictionary<string, int> GetEntityCountByType(BlockReference blockRef, Transaction transaction)
        {
            if (blockRef == null || transaction == null)
                return new Dictionary<string, int>();

            return GetEntityCountByType(blockRef.BlockTableRecord, transaction);
        }

        /// <summary>
        /// BlockTableRecord의 Entity 타입별 개수를 반환합니다
        /// </summary>
        /// <param name="blockTableRecordId">BlockTableRecord ObjectId</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>타입별 개수 딕셔너리</returns>
        public static Dictionary<string, int> GetEntityCountByType(ObjectId blockTableRecordId, Transaction transaction)
        {
            var result = new Dictionary<string, int>();

            if (blockTableRecordId.IsNull || transaction == null)
                return result;

            try
            {
                var btr = transaction.GetObject(blockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return result;

                // LINQ를 사용하여 타입별 개수 계산
                var typeGroups = btr.Cast<ObjectId>()
                                   .Where(id => IsEntity(id))
                                   .GroupBy(id => id.ObjectClass.Name);

                foreach (var group in typeGroups)
                {
                    result[group.Key] = group.Count();
                }

                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        #endregion

        #region Entity List Functions

        /// <summary>
        /// Block에 포함된 모든 Entity 목록을 반환합니다
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>Entity 목록</returns>
        public static List<Entity> GetEntityList(BlockReference blockRef, Transaction transaction)
        {
            if (blockRef == null || transaction == null)
                return new List<Entity>();

            return GetEntityList(blockRef.BlockTableRecord, transaction);
        }

        /// <summary>
        /// BlockTableRecord에 포함된 모든 Entity 목록을 반환합니다
        /// </summary>
        /// <param name="blockTableRecordId">BlockTableRecord ObjectId</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>Entity 목록</returns>
        public static List<Entity> GetEntityList(ObjectId blockTableRecordId, Transaction transaction)
        {
            var entities = new List<Entity>();

            if (blockTableRecordId.IsNull || transaction == null)
                return entities;

            try
            {
                var btr = transaction.GetObject(blockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return entities;

                foreach (ObjectId id in btr)
                {
                    if (IsEntity(id))
                    {
                        try
                        {
                            var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;
                            if (entity != null)
                            {
                                entities.Add(entity);
                            }
                        }
                        catch (Exception)
                        {
                            // 개별 Entity 로드 실패 시 계속 진행
                            continue;
                        }
                    }
                }

                return entities;
            }
            catch (Exception)
            {
                return entities;
            }
        }

        /// <summary>
        /// 특정 타입의 Entity 목록을 반환합니다
        /// </summary>
        /// <typeparam name="T">Entity 타입</typeparam>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <returns>지정된 타입의 Entity 목록</returns>
        public static List<T> GetEntityList<T>(BlockReference blockRef, Transaction transaction) where T : Entity
        {
            var entities = new List<T>();

            if (blockRef == null || transaction == null)
                return entities;

            try
            {
                var allEntities = GetEntityList(blockRef, transaction);
                entities.AddRange(allEntities.OfType<T>());
                return entities;
            }
            catch (Exception)
            {
                return entities;
            }
        }

        #endregion

        #region Recursive Block Explode Functions

        /// <summary>
        /// Block을 재귀적으로 explode하여 모든 기본 Entity를 반환합니다
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="transformMatrix">변환 매트릭스 (기본값: Identity)</param>
        /// <returns>변환이 적용된 모든 기본 Entity 목록</returns>
        public static List<Entity> ExplodeBlockRecursively(BlockReference blockRef, Transaction transaction, Matrix3d? transformMatrix = null)
        {
            var result = new List<Entity>();

            if (blockRef == null || transaction == null)
                return result;

            try
            {
                // BlockReference의 변환 매트릭스 계산
                var currentTransform = transformMatrix ?? Matrix3d.Identity;
                var blockTransform = blockRef.BlockTransform;
                var combinedTransform = blockTransform * currentTransform;

                // Block 내부의 모든 Entity 처리
                var entities = GetEntityList(blockRef, transaction);

                foreach (var entity in entities)
                {
                    if (entity is BlockReference nestedBlockRef)
                    {
                        // 중첩된 Block을 재귀적으로 처리
                        var explodedEntities = ExplodeBlockRecursively(nestedBlockRef, transaction, combinedTransform);
                        result.AddRange(explodedEntities);
                    }
                    else
                    {
                        // 기본 Entity는 변환을 적용하여 결과에 추가
                        var transformedEntity = ApplyTransformToEntity(entity, combinedTransform);
                        if (transformedEntity != null)
                        {
                            result.Add(transformedEntity);
                        }
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        /// <summary>
        /// Block을 재귀적으로 explode하여 모든 Entity 정보를 반환합니다 (변환 정보 포함)
        /// </summary>
        /// <param name="blockRef">분석할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="transformMatrix">변환 매트릭스</param>
        /// <returns>Entity 정보 목록</returns>
        public static List<ExplodedEntityInfo> ExplodeBlockWithInfo(BlockReference blockRef, Transaction transaction, Matrix3d? transformMatrix = null)
        {
            var result = new List<ExplodedEntityInfo>();

            if (blockRef == null || transaction == null)
                return result;

            try
            {
                var currentTransform = transformMatrix ?? Matrix3d.Identity;
                var blockTransform = blockRef.BlockTransform;
                var combinedTransform = blockTransform * currentTransform;

                var entities = GetEntityList(blockRef, transaction);

                foreach (var entity in entities)
                {
                    if (entity is BlockReference nestedBlockRef)
                    {
                        var explodedEntities = ExplodeBlockWithInfo(nestedBlockRef, transaction, combinedTransform);
                        result.AddRange(explodedEntities);
                    }
                    else
                    {
                        result.Add(new ExplodedEntityInfo
                        {
                            OriginalEntity = entity,
                            TransformMatrix = combinedTransform,
                            EntityType = entity.GetType().Name,
                            Layer = entity.Layer,
                            ObjectId = entity.ObjectId
                        });
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        #endregion

        #region Actual Explode Functions

        /// <summary>
        /// BlockReference를 실제로 explode합니다 (1단계만)
        /// </summary>
        /// <param name="blockRef">explode할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="deleteOriginal">원본 Block 삭제 여부</param>
        /// <returns>생성된 Entity들의 ObjectId 목록</returns>
        public static List<ObjectId> ExplodeBlock(BlockReference blockRef, Transaction transaction, bool deleteOriginal = true)
        {
            var result = new List<ObjectId>();

            if (blockRef == null || transaction == null)
                return result;

            try
            {
                // Explode할 Entity들을 수집
                var explodeCollection = new DBObjectCollection();
                blockRef.Explode(explodeCollection);

                if (explodeCollection.Count == 0)
                    return result;

                // Model Space 또는 현재 공간 가져오기
                var currentSpaceId = GetCurrentSpaceId(blockRef.Database);
                var currentSpace = transaction.GetObject(currentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                if (currentSpace == null)
                    return result;

                // Explode된 Entity들을 도면에 추가
                foreach (Entity entity in explodeCollection)
                {
                    if (entity != null)
                    {
                        currentSpace.AppendEntity(entity);
                        transaction.AddNewlyCreatedDBObject(entity, true);
                        result.Add(entity.ObjectId);
                    }
                }

                // 원본 BlockReference 삭제
                if (deleteOriginal)
                {
                    blockRef.UpgradeOpen();
                    blockRef.Erase();
                }

                return result;
            }
            catch (Exception)
            {
                // 오류 발생 시 생성된 Entity들 정리
                foreach (var objId in result)
                {
                    try
                    {
                        if (!objId.IsNull && !objId.IsErased)
                        {
                            var obj = transaction.GetObject(objId, OpenMode.ForWrite);
                            obj.Erase();
                        }
                    }
                    catch { }
                }
                return new List<ObjectId>();
            }
        }

        /// <summary>
        /// BlockReference를 재귀적으로 완전히 explode합니다
        /// </summary>
        /// <param name="blockRef">explode할 BlockReference</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="deleteOriginal">원본 Block 삭제 여부</param>
        /// <returns>최종적으로 생성된 기본 Entity들의 ObjectId 목록</returns>
        public static List<ObjectId> ExplodeBlockCompletely(BlockReference blockRef, Transaction transaction, bool deleteOriginal = true)
        {
            var finalResult = new List<ObjectId>();

            if (blockRef == null || transaction == null)
                return finalResult;

            try
            {
                var toProcess = new Queue<ObjectId>();

                // 첫 번째 explode 수행
                var firstLevelEntities = ExplodeBlock(blockRef, transaction, deleteOriginal);

                foreach (var entityId in firstLevelEntities)
                {
                    toProcess.Enqueue(entityId);
                }

                // 재귀적으로 모든 BlockReference를 explode
                while (toProcess.Count > 0)
                {
                    var currentEntityId = toProcess.Dequeue();

                    try
                    {
                        if (currentEntityId.IsNull || currentEntityId.IsErased)
                            continue;

                        var currentEntity = transaction.GetObject(currentEntityId, OpenMode.ForRead);

                        if (currentEntity is BlockReference nestedBlockRef)
                        {
                            // 중첩된 BlockReference를 explode
                            var explodedEntities = ExplodeBlock(nestedBlockRef, transaction, true);

                            // explode된 Entity들을 처리 대기열에 추가
                            foreach (var explodedId in explodedEntities)
                            {
                                toProcess.Enqueue(explodedId);
                            }
                        }
                        else
                        {
                            // 기본 Entity는 최종 결과에 추가
                            finalResult.Add(currentEntityId);
                        }
                    }
                    catch (Exception)
                    {
                        // 개별 Entity 처리 실패 시 계속 진행
                        continue;
                    }
                }

                return finalResult;
            }
            catch (Exception)
            {
                return finalResult;
            }
        }

        /// <summary>
        /// 선택된 여러 BlockReference들을 일괄 explode합니다
        /// </summary>
        /// <param name="blockRefIds">explode할 BlockReference ObjectId 목록</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="recursive">재귀적 explode 여부</param>
        /// <param name="deleteOriginals">원본 Block들 삭제 여부</param>
        /// <returns>처리 결과 정보</returns>
        public static ExplodeResults ExplodeMultipleBlocks(List<ObjectId> blockRefIds, Transaction transaction, bool recursive = false, bool deleteOriginals = true)
        {
            var results = new ExplodeResults();

            if (blockRefIds == null || transaction == null)
                return results;

            foreach (var blockRefId in blockRefIds)
            {
                try
                {
                    if (blockRefId.IsNull || blockRefId.IsErased)
                    {
                        results.FailedBlocks.Add(blockRefId);
                        continue;
                    }

                    var blockRef = transaction.GetObject(blockRefId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null)
                    {
                        results.FailedBlocks.Add(blockRefId);
                        continue;
                    }

                    List<ObjectId> explodedEntities;

                    if (recursive)
                    {
                        explodedEntities = ExplodeBlockCompletely(blockRef, transaction, deleteOriginals);
                    }
                    else
                    {
                        explodedEntities = ExplodeBlock(blockRef, transaction, deleteOriginals);
                    }

                    results.SuccessfulBlocks.Add(blockRefId);
                    results.CreatedEntities.AddRange(explodedEntities);
                }
                catch (Exception)
                {
                    results.FailedBlocks.Add(blockRefId);
                }
            }

            return results;
        }

        /// <summary>
        /// 현재 공간의 모든 BlockReference를 찾아서 explode합니다
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="transaction">활성 Transaction</param>
        /// <param name="recursive">재귀적 explode 여부</param>
        /// <param name="blockNamePattern">Block 이름 패턴 (null이면 모든 Block)</param>
        /// <returns>처리 결과 정보</returns>
        public static ExplodeResults ExplodeAllBlocks(Database database, Transaction transaction, bool recursive = false, string blockNamePattern = null)
        {
            var results = new ExplodeResults();

            if (database == null || transaction == null)
                return results;

            try
            {
                var currentSpaceId = GetCurrentSpaceId(database);
                var currentSpace = transaction.GetObject(currentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                if (currentSpace == null)
                    return results;

                // BlockReference들을 찾기
                var blockRefs = new List<ObjectId>();
                var blockRefClass = RXObject.GetClass(typeof(BlockReference));

                foreach (ObjectId id in currentSpace)
                {
                    if (id.ObjectClass == blockRefClass)
                    {
                        try
                        {
                            var blockRef = transaction.GetObject(id, OpenMode.ForRead) as BlockReference;
                            if (blockRef != null)
                            {
                                // 패턴 검사
                                if (string.IsNullOrEmpty(blockNamePattern) ||
                                    blockRef.Name.Contains(blockNamePattern))
                                {
                                    blockRefs.Add(id);
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 찾은 BlockReference들을 explode
                results = ExplodeMultipleBlocks(blockRefs, transaction, recursive, true);

                return results;
            }
            catch (Exception)
            {
                return results;
            }
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// ObjectId가 Entity인지 확인합니다
        /// </summary>
        /// <param name="id">확인할 ObjectId</param>
        /// <returns>Entity 여부</returns>
        private static bool IsEntity(ObjectId id)
        {
            if (id.IsNull || id.IsErased || id.IsEffectivelyErased)
                return false;

            try
            {
                var entityClass = RXObject.GetClass(typeof(Entity));
                return id.ObjectClass.IsDerivedFrom(entityClass);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Entity에 변환 매트릭스를 적용하여 새로운 Entity를 생성합니다
        /// </summary>
        /// <param name="originalEntity">원본 Entity</param>
        /// <param name="transformMatrix">변환 매트릭스</param>
        /// <returns>변환된 Entity (복사본)</returns>
        private static Entity ApplyTransformToEntity(Entity originalEntity, Matrix3d transformMatrix)
        {
            try
            {
                var clonedEntity = originalEntity.Clone() as Entity;
                if (clonedEntity != null)
                {
                    clonedEntity.TransformBy(transformMatrix);
                    return clonedEntity;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 현재 공간의 ObjectId를 반환합니다 (Model Space 또는 현재 Paper Space)
        /// </summary>
        /// <param name="database">Database</param>
        /// <returns>현재 공간의 ObjectId</returns>
        private static ObjectId GetCurrentSpaceId(Database database)
        {
            if (database.TileMode)
            {
                // Model Space
                return SymbolUtilityServices.GetBlockModelSpaceId(database);
            }
            else
            {
                // Paper Space
                return SymbolUtilityServices.GetBlockPaperSpaceId(database);
            }
        }

        #endregion
    }

    #region Data Structure Classes

    /// <summary>
    /// Block 크기 정보를 담는 클래스
    /// </summary>
    public class BlockSize
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Depth { get; set; }
        /// <summary>
        /// 면적 (제곱미터) - Width × Height로 자동 계산
        /// </summary>
        public double Area { get { return Width/1000 * Height/1000; } }

        public Point3d MinPoint { get; set; }
        public Point3d MaxPoint { get; set; }

        public BlockSize()
        {
            Width = 0;
            Height = 0;
            Depth = 0;
            MinPoint = Point3d.Origin;
            MaxPoint = Point3d.Origin;
        }

        public override string ToString()
        {
            return $"Width: {Width:F2}, Height: {Height:F2}, Depth: {Depth:F2}, Area: {Area:F2}";
        }
    }

    /// <summary>
    /// Exploded Entity 정보를 담는 클래스
    /// </summary>
    public class ExplodedEntityInfo
    {
        public Entity? OriginalEntity { get; set; }
        public Matrix3d TransformMatrix { get; set; }
        public string? EntityType { get; set; }
        public string? Layer { get; set; }
        public ObjectId ObjectId { get; set; }

        public override string ToString()
        {
            return $"Type: {EntityType}, Layer: {Layer}";
        }
    }

    #endregion

    #region Command Examples

    public class BlockAnalysisCommands
    {
        [CommandMethod("BLOCKINFO")]
        public void GetBlockInfo()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Block 선택
                    var peo = new PromptEntityOptions("\nBlock을 선택하세요: ");
                    peo.SetRejectMessage("\nBlockReference만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;

                    var blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null)
                        return;

                    // Block 정보 출력
                    var size = BlockUtilities.GetBlockSize(blockRef, tr);
                    var entityCount = BlockUtilities.GetEntityCount(blockRef, tr);
                    var entityCountByType = BlockUtilities.GetEntityCountByType(blockRef, tr);

                    ed.WriteMessage($"\n=== Block 정보 ===");
                    ed.WriteMessage($"\n크기: {size}");
                    ed.WriteMessage($"\n전체 Entity 개수: {entityCount}");
                    ed.WriteMessage($"\n타입별 Entity 개수:");
                    foreach (var kvp in entityCountByType)
                    {
                        ed.WriteMessage($"\n  {kvp.Key}: {kvp.Value}개");
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류: {ex.Message}");
                }
            }
        }

        [CommandMethod("EXPLODEBLOCKACTUAL")]
        public void ExplodeBlockActual()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var peo = new PromptEntityOptions("\n실제로 explode할 Block을 선택하세요: ");
                    peo.SetRejectMessage("\nBlockReference만 선택할 수 있습니다.");
                    peo.AddAllowedClass(typeof(BlockReference), true);

                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;

                    var blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null)
                        return;

                    // 재귀적 explode 여부 확인
                    var pko = new PromptKeywordOptions("\n재귀적으로 explode하시겠습니까?");
                    pko.Keywords.Add("Yes");
                    pko.Keywords.Add("No");
                    pko.Keywords.Default = "Yes";

                    var pkr = ed.GetKeywords(pko);
                    bool recursive = (pkr.Status == PromptStatus.OK && pkr.StringResult == "Yes");

                    List<ObjectId> createdEntities;

                    if (recursive)
                    {
                        createdEntities = BlockUtilities.ExplodeBlockCompletely(blockRef, tr, true);
                        ed.WriteMessage($"\n재귀적 Explode 완료: {createdEntities.Count}개의 Entity가 생성되었습니다.");
                    }
                    else
                    {
                        createdEntities = BlockUtilities.ExplodeBlock(blockRef, tr, true);
                        ed.WriteMessage($"\n1단계 Explode 완료: {createdEntities.Count}개의 Entity가 생성되었습니다.");
                    }

                    // 생성된 Entity 타입별 개수 출력
                    if (createdEntities.Count > 0)
                    {
                        var typeGroups = createdEntities
                            .Where(id => !id.IsNull && !id.IsErased)
                            .Select(id =>
                            {
                                try
                                {
                                    return tr.GetObject(id, OpenMode.ForRead)?.GetType().Name ?? "Unknown";
                                }
                                catch
                                {
                                    return "Unknown";
                                }
                            })
                            .GroupBy(x => x);

                        ed.WriteMessage($"\n생성된 Entity 타입:");
                        foreach (var group in typeGroups)
                        {
                            ed.WriteMessage($"\n  {group.Key}: {group.Count()}개");
                        }
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류: {ex.Message}");
                    tr.Abort();
                }
            }
        }

        [CommandMethod("EXPLODEALLBLOCKS")]
        public void ExplodeAllBlocks()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 재귀적 explode 여부 확인
                    var pko1 = new PromptKeywordOptions("\n재귀적으로 explode하시겠습니까?");
                    pko1.Keywords.Add("Yes");
                    pko1.Keywords.Add("No");
                    pko1.Keywords.Default = "Yes";

                    var pkr1 = ed.GetKeywords(pko1);
                    if (pkr1.Status != PromptStatus.OK) return;

                    bool recursive = (pkr1.StringResult == "Yes");

                    // Block 이름 패턴 입력
                    var pso = new PromptStringOptions("\nBlock 이름 패턴을 입력하세요 (Enter=모든 Block): ");
                    pso.AllowSpaces = true;
                    var psr = ed.GetString(pso);

                    string pattern = null;
                    if (psr.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(psr.StringResult))
                    {
                        pattern = psr.StringResult;
                    }

                    // 확인 메시지
                    var pko2 = new PromptKeywordOptions($"\n현재 공간의 {(string.IsNullOrEmpty(pattern) ? "모든" : pattern + " 패턴의")} Block을 {(recursive ? "재귀적으로" : "")} explode하시겠습니까?");
                    pko2.Keywords.Add("Yes");
                    pko2.Keywords.Add("No");
                    pko2.Keywords.Default = "No";

                    var pkr2 = ed.GetKeywords(pko2);
                    if (pkr2.Status != PromptStatus.OK || pkr2.StringResult != "Yes")
                        return;

                    // 모든 Block explode 수행
                    var results = BlockUtilities.ExplodeAllBlocks(db, tr, recursive, pattern);

                    ed.WriteMessage($"\n=== Explode 결과 ===");
                    ed.WriteMessage($"\n{results}");

                    if (results.FailureCount > 0)
                    {
                        ed.WriteMessage($"\n실패한 Block들이 있습니다. 로그를 확인하세요.");
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n오류: {ex.Message}");
                    tr.Abort();
                }
            }
        }
    }
    #endregion


    // <summary>
    /// AutoCAD 2025 (.NET 8.0) 호환 블록 내부 엔티티 복사 클래스
    /// 블록을 선택했을 때 선택 지점에서 가장 가까운 내부 Entity를 찾아
    /// 현재 레이어에 복사하는 기능을 제공합니다.
    /// 중첩된 블록도 재귀적으로 검색합니다.
    /// </summary>
    public class BlockEntityCopier
    {
        /// <summary>
        /// 블록 내부의 가장 가까운 엔티티를 현재 레이어에 복사하는 명령어
        /// 사용법: AutoCAD 명령줄에서 COPYBLOCKENTITY 입력
        /// </summary>
        [CommandMethod("COPYBLOCKENTITY")]
        public void CopyClosestBlockEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;
            Database db = doc.Database;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    editor.WriteMessage("\n=== Block Entity Copier (AutoCAD 2025 / .NET 8.0) ===");

                    // Step 1: 블록 선택
                    PromptEntityOptions blockSelectionOptions = new PromptEntityOptions("\nSelect a block reference: ");
                    blockSelectionOptions.SetRejectMessage("\nMust be a block reference!");
                    blockSelectionOptions.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult blockResult = editor.GetEntity(blockSelectionOptions);
                    if (blockResult.Status != PromptStatus.OK)
                    {
                        editor.WriteMessage("\nBlock selection cancelled.");
                        return;
                    }

                    // Step 2: 선택점 가져오기
                    Point3d selectionPoint = blockResult.PickedPoint;
                    editor.WriteMessage($"\nSelection point: X={selectionPoint.X:F3}, Y={selectionPoint.Y:F3}, Z={selectionPoint.Z:F3}");

                    // Step 3: 블록 참조 객체 가져오기
                    BlockReference blockRef = trans.GetObject(blockResult.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null)
                    {
                        editor.WriteMessage("\nInvalid block reference.");
                        return;
                    }

                    // Step 4: 현재 레이어 정보 가져오기
                    LayerTableRecord currentLayerRecord = trans.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayer = currentLayerRecord?.Name ?? "0";
                    editor.WriteMessage($"\nTarget layer: {currentLayer}");

                    // Step 5: 블록 내부에서 가장 가까운 엔티티 찾기 (재귀적)
                    editor.WriteMessage("\nSearching for closest entity in block (including nested blocks)...");
                    Entity closestEntity = FindClosestEntityInBlock(trans, blockRef, selectionPoint);

                    if (closestEntity != null)
                    {
                        // Step 6: 엔티티를 현재 레이어에 복사
                        CopyEntityToCurrentLayer(trans, db, closestEntity, currentLayer);
                        editor.WriteMessage($"\n✓ Entity successfully copied to layer: {currentLayer}");
                    }
                    else
                    {
                        editor.WriteMessage("\n✗ No entity found in the selected block.");
                    }

                    trans.Commit();
                    editor.WriteMessage("\n=== Operation completed ===");
                }
            }
            catch (Exception ex)
            {
                editor.WriteMessage($"\n✗ Error: {ex.Message}");
                editor.WriteMessage($"\nStack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 블록 내부의 가장 가까운 엔티티를 재귀적으로 찾기 (개선된 버전)
        /// 선택 지점 주변의 작은 영역만 검사하여 성능 향상
        /// 중첩 블록의 경계 검사 개선
        /// </summary>
        private Entity FindClosestEntityInBlock(Transaction trans, BlockReference blockRef, Point3d selectionPoint)
        {
            Entity closestEntity = null;
            double minDistance = double.MaxValue;

            try
            {
                // 블록의 변환 행렬 가져오기
                Matrix3d blockTransform = blockRef.BlockTransform;

                // 선택점을 블록의 로컬 좌표계로 변환
                Point3d localSelectionPoint = selectionPoint.TransformBy(blockTransform.Inverse());

                // 블록 테이블 레코드 열기
                BlockTableRecord blockDef = trans.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (blockDef == null) return null;

                // 성능 최적화: 선택 지점 주변의 검색 반경 설정 (사용자 단위 기준)
                double searchRadius = GetOptimalSearchRadius(trans, blockRef);

                Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
                editor.WriteMessage($"\nOptimization: Using search radius {searchRadius:F2} units around selection point");

                // 블록 내부의 엔티티를 공간적으로 필터링하여 검사
                int totalEntities = 0;
                int checkedEntities = 0;

                foreach (ObjectId entityId in blockDef)
                {
                    totalEntities++;
                    if (entityId.IsErased || !entityId.IsValid) continue;

                    try
                    {
                        DBObject dbObj = trans.GetObject(entityId, OpenMode.ForRead);

                        // 중첩된 블록 참조인 경우 - 개선된 검사 로직
                        if (dbObj is BlockReference nestedBlockRef)
                        {
                            // 개선: 선택점이 블록 경계 내에 있는지 확인
                            if (IsPointInsideNestedBlock(trans, nestedBlockRef, localSelectionPoint))
                            {
                                checkedEntities++;
                                Entity nestedResult = FindClosestEntityInNestedBlock(trans, nestedBlockRef, localSelectionPoint, blockTransform);
                                if (nestedResult != null)
                                {
                                    closestEntity?.Dispose();
                                    closestEntity = nestedResult;                               
                                    //double distance = CalculateEntityDistance(nestedResult, selectionPoint);
                                    //if (distance < minDistance)
                                    //{
                                    //    minDistance = distance;
                                    //    closestEntity?.Dispose();
                                    //    closestEntity = nestedResult;
                                    //}
                                    //else
                                    //{
                                    //    nestedResult.Dispose();
                                    //}
                                }
                            }
                        }
                        // 일반 엔티티인 경우
                        else if (dbObj is Entity entity)
                        {
                            // 엔티티가 검색 영역 내에 있는지 먼저 확인 (최적화)
                            if (!IsEntityInSearchArea(entity, localSelectionPoint, searchRadius))
                                continue; // 검색 영역 밖의 엔티티는 건너뛰기

                            checkedEntities++;
                            // 엔티티의 월드 좌표를 계산
                            Entity transformedEntity = entity.Clone() as Entity;
                            if (transformedEntity != null)
                            {
                                transformedEntity.TransformBy(blockTransform);

                                closestEntity?.Dispose();
                                closestEntity = transformedEntity;

                                //double distance = CalculateEntityDistance(transformedEntity, selectionPoint);
                                //if (distance < minDistance)
                                //{
                                //    minDistance = distance;
                                //    closestEntity?.Dispose();
                                //    closestEntity = transformedEntity;
                                //}
                                //else
                                //{
                                //    transformedEntity.Dispose();
                                //}
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        editor.WriteMessage($"\nWarning processing entity {entityId}: {ex.Message}");
                        continue;
                    }
                }

                editor.WriteMessage($"\nPerformance: Checked {checkedEntities}/{totalEntities} entities ({(double)checkedEntities / totalEntities * 100:F1}% efficiency)");
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError in FindClosestEntityInBlock: {ex.Message}");
                return null;
            }

            return closestEntity;
        }

        /// <summary>
        /// 선택점이 중첩 블록의 경계 내에 있는지 확인하는 개선된 메서드
        /// </summary>
        private bool IsPointInsideNestedBlock(Transaction trans, BlockReference nestedBlockRef, Point3d localSelectionPoint)
        {
            try
            {
                // 1. 먼저 중첩 블록의 바운딩 박스를 구해서 선택점이 내부에 있는지 확인
                try
                {
                    Extents3d blockBounds = nestedBlockRef.GeometricExtents;

                    // 선택점이 블록의 바운딩 박스 내부에 있는지 확인
                    if (IsPointInsideBounds(localSelectionPoint, blockBounds))
                    {
                        return true; // 선택점이 블록 내부에 있으면 무조건 검사 대상
                    }
                }
                catch
                {
                    // GeometricExtents를 구할 수 없는 경우, 블록 정의에서 바운딩 박스 계산
                }

                // 2. GeometricExtents를 구할 수 없는 경우, 블록 정의에서 바운딩 박스 직접 계산
                try
                {
                    Extents3d calculatedBounds = CalculateNestedBlockBounds(trans, nestedBlockRef);
                    if (calculatedBounds.MinPoint != calculatedBounds.MaxPoint) // 유효한 바운딩 박스인지 확인
                    {
                        if (IsPointInsideBounds(localSelectionPoint, calculatedBounds))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // 바운딩 박스 계산도 실패한 경우
                }

                // 3. 바운딩 박스 방법이 모두 실패한 경우, 검사하지 않음 (안전하게 제외)
                return false;
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning in nested block check: {ex.Message}");
                // 오류 발생시 안전하게 제외 (선택점이 내부에 있는지 확실하지 않으므로)
                return false;
            }
        }

        /// <summary>
        /// 점이 바운딩 박스 내부에 있는지 확인
        /// </summary>
        private bool IsPointInsideBounds(Point3d point, Extents3d bounds)
        {
            return point.X >= bounds.MinPoint.X && point.X <= bounds.MaxPoint.X &&
                   point.Y >= bounds.MinPoint.Y && point.Y <= bounds.MaxPoint.Y;
                   //&&
                   //point.Z >= bounds.MinPoint.Z && point.Z <= bounds.MaxPoint.Z;
        }

        /// <summary>
        /// 중첩 블록의 바운딩 박스를 블록 정의에서 직접 계산
        /// </summary>
        private Extents3d CalculateNestedBlockBounds(Transaction trans, BlockReference nestedBlockRef)
        {
            try
            {
                // 블록 정의 가져오기
                BlockTableRecord nestedBlockDef = trans.GetObject(nestedBlockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (nestedBlockDef == null)
                    throw new InvalidOperationException("Cannot get block definition");

                Extents3d? combinedBounds = null;

                // 블록 정의 내 모든 엔티티의 바운딩 박스 합산
                foreach (ObjectId entityId in nestedBlockDef)
                {
                    if (entityId.IsErased || !entityId.IsValid) continue;

                    try
                    {
                        Entity entity = trans.GetObject(entityId, OpenMode.ForRead) as Entity;
                        if (entity != null)
                        {
                            Extents3d entityBounds = entity.GeometricExtents;

                            if (combinedBounds == null)
                            {
                                combinedBounds = entityBounds;
                            }
                            else
                            {
                                // 바운딩 박스 확장
                                Point3d minPt = combinedBounds.Value.MinPoint;
                                Point3d maxPt = combinedBounds.Value.MaxPoint;

                                double minX = Math.Min(minPt.X, entityBounds.MinPoint.X);
                                double minY = Math.Min(minPt.Y, entityBounds.MinPoint.Y);
                                double minZ = Math.Min(minPt.Z, entityBounds.MinPoint.Z);

                                double maxX = Math.Max(maxPt.X, entityBounds.MaxPoint.X);
                                double maxY = Math.Max(maxPt.Y, entityBounds.MaxPoint.Y);
                                double maxZ = Math.Max(maxPt.Z, entityBounds.MaxPoint.Z);

                                combinedBounds = new Extents3d(
                                    new Point3d(minX, minY, minZ),
                                    new Point3d(maxX, maxY, maxZ)
                                );
                            }
                        }
                    }
                    catch
                    {
                        // 개별 엔티티 처리 실패시 무시하고 계속
                        continue;
                    }
                }

                if (combinedBounds.HasValue)
                {
                    // 블록의 변환 행렬 적용
                    Extents3d transformedBounds = TransformBounds(combinedBounds.Value, nestedBlockRef.BlockTransform);
                    return transformedBounds;
                }
                else
                {
                    // 엔티티가 없거나 바운딩 박스를 구할 수 없는 경우
                    // 삽입점 기준의 작은 바운딩 박스 반환
                    Point3d insertPt = nestedBlockRef.Position;
                    double smallOffset = 0.1; // 매우 작은 오프셋
                    return new Extents3d(
                        new Point3d(insertPt.X - smallOffset, insertPt.Y - smallOffset, insertPt.Z - smallOffset),
                        new Point3d(insertPt.X + smallOffset, insertPt.Y + smallOffset, insertPt.Z + smallOffset)
                    );
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nError calculating nested block bounds: {ex.Message}");
                // 기본값으로 삽입점 기준의 작은 바운딩 박스 반환
                Point3d insertPt = nestedBlockRef.Position;
                double smallOffset = 1.0;
                return new Extents3d(
                    new Point3d(insertPt.X - smallOffset, insertPt.Y - smallOffset, insertPt.Z - smallOffset),
                    new Point3d(insertPt.X + smallOffset, insertPt.Y + smallOffset, insertPt.Z + smallOffset)
                );
            }
        }

        /// <summary>
        /// 바운딩 박스에 변환 행렬 적용
        /// </summary>
        private Extents3d TransformBounds(Extents3d bounds, Matrix3d transform)
        {
            try
            {
                // 바운딩 박스의 8개 모서리 점을 모두 변환
                Point3d[] corners = new Point3d[]
                {
                bounds.MinPoint,
                new Point3d(bounds.MaxPoint.X, bounds.MinPoint.Y, bounds.MinPoint.Z),
                new Point3d(bounds.MinPoint.X, bounds.MaxPoint.Y, bounds.MinPoint.Z),
                new Point3d(bounds.MinPoint.X, bounds.MinPoint.Y, bounds.MaxPoint.Z),
                new Point3d(bounds.MaxPoint.X, bounds.MaxPoint.Y, bounds.MinPoint.Z),
                new Point3d(bounds.MaxPoint.X, bounds.MinPoint.Y, bounds.MaxPoint.Z),
                new Point3d(bounds.MinPoint.X, bounds.MaxPoint.Y, bounds.MaxPoint.Z),
                bounds.MaxPoint
                };

                // 모든 모서리 점을 변환
                Point3d[] transformedCorners = new Point3d[8];
                for (int i = 0; i < 8; i++)
                {
                    transformedCorners[i] = corners[i].TransformBy(transform);
                }

                // 변환된 점들로부터 새로운 바운딩 박스 계산
                double minX = transformedCorners[0].X;
                double minY = transformedCorners[0].Y;
                double minZ = transformedCorners[0].Z;
                double maxX = transformedCorners[0].X;
                double maxY = transformedCorners[0].Y;
                double maxZ = transformedCorners[0].Z;

                for (int i = 1; i < 8; i++)
                {
                    Point3d pt = transformedCorners[i];
                    minX = Math.Min(minX, pt.X);
                    minY = Math.Min(minY, pt.Y);
                    minZ = Math.Min(minZ, pt.Z);
                    maxX = Math.Max(maxX, pt.X);
                    maxY = Math.Max(maxY, pt.Y);
                    maxZ = Math.Max(maxZ, pt.Z);
                }

                return new Extents3d(
                    new Point3d(minX, minY, minZ),
                    new Point3d(maxX, maxY, maxZ)
                );
            }
            catch
            {
                // 변환 실패시 원본 반환
                return bounds;
            }
        }

        /// <summary>
        /// 최적의 검색 반경을 동적으로 계산
        /// 뷰포트 크기와 블록 크기를 고려하여 적응적으로 설정
        /// </summary>
        private double GetOptimalSearchRadius(Transaction trans, BlockReference blockRef)
        {
            try
            {
                // 기본 반경
                double baseRadius = 10.0;

                // 현재 뷰포트 크기 확인
                Point3d minPt = (Point3d)Application.GetSystemVariable("VIEWMIN");
                Point3d maxPt = (Point3d)Application.GetSystemVariable("VIEWMAX");
                double viewWidth = maxPt.X - minPt.X;
                double viewHeight = maxPt.Y - minPt.Y;
                double viewSize = Math.Min(viewWidth, viewHeight);

                // 뷰포트 크기의 5%를 검색 반경으로 설정 (최소 1, 최대 100)
                double adaptiveRadius = Math.Max(1.0, Math.Min(100.0, viewSize * 0.05));

                // 블록의 스케일 팩터 고려
                Matrix3d transform = blockRef.BlockTransform;
                Vector3d xVector = transform.CoordinateSystem3d.Xaxis;
                Vector3d yVector = transform.CoordinateSystem3d.Yaxis;
                double scaleX = xVector.Length;
                double scaleY = yVector.Length;
                double avgScale = (scaleX + scaleY) / 2.0;

                // 최종 반경 계산
                double finalRadius = Math.Max(baseRadius, adaptiveRadius / avgScale);

                return finalRadius;
            }
            catch
            {
                return 50.0; // 기본값
            }
        }

        /// <summary>
        /// 점이 검색 반경 내에 있는지 확인 (단순 거리 체크)
        /// </summary>
        private bool IsWithinSearchRadius(Point3d entityPoint, Point3d searchPoint, double radius)
        {
            double distance = entityPoint.DistanceTo(searchPoint);
            return distance <= radius;
        }

        /// <summary>
        /// 엔티티가 검색 영역 내에 있는지 확인 (효율적인 공간 필터링)
        /// 엔티티 타입별로 최적화된 검사 방법 사용
        /// </summary>
        private bool IsEntityInSearchArea(Entity entity, Point3d searchPoint, double radius)
        {
            try
            {
                //// 1. 특정 엔티티 타입에 대한 빠른 검사
                //if (entity is BlockReference blockRef)
                //{
                //    return IsWithinSearchRadius(blockRef.Position, searchPoint, radius);
                //}
                //else if (entity is DBText dbText)
                //{
                //    return IsWithinSearchRadius(dbText.Position, searchPoint, radius);
                //}
                //else if (entity is MText mText)
                //{
                //    return IsWithinSearchRadius(mText.Location, searchPoint, radius);
                //}
                //else if (entity is Dimension dim)
                //{
                //    return IsWithinSearchRadius(dim.TextPosition, searchPoint, radius);
                //}

                // 2. 바운딩 박스 기반 검사 (일반적인 기하학적 엔티티)
                try
                {
                    //Extents3d bounds = entity.GeometricExtents;

                    //// 바운딩 박스의 중심점
                    //Point3d center = new Point3d(
                    //    (bounds.MinPoint.X + bounds.MaxPoint.X) / 2,
                    //    (bounds.MinPoint.Y + bounds.MaxPoint.Y) / 2,
                    //    (bounds.MinPoint.Z + bounds.MaxPoint.Z) / 2
                    //);

                    //// 바운딩 박스의 최대 반경 (대각선의 절반)
                    //double boundingRadius = bounds.MinPoint.DistanceTo(bounds.MaxPoint) / 2;

                    //// 검색 원과 바운딩 박스가 겹치는지 빠른 체크
                    //double centerDistance = center.DistanceTo(searchPoint);

                    //// 바운딩 박스가 검색 반경과 전혀 겹치지 않으면 제외
                    //if (centerDistance > radius + boundingRadius)
                    //    return false;

                    //// 바운딩 박스가 완전히 검색 반경 내에 있으면 포함
                    //if (centerDistance + boundingRadius <= radius)
                    //    return true;

                    // 3. Curve 타입에 대한 정확한 검사 (필요한 경우에만)
                    if (entity is Curve curve)
                    {
                        try
                        {
                            Point3d closestPoint = curve.GetClosestPointTo(searchPoint, false);
                            return closestPoint.DistanceTo(searchPoint) <= radius;
                        }
                        catch
                        {
                            //// GetClosestPointTo 실패 시 바운딩 박스 기준으로 판단
                            //return centerDistance <= radius + boundingRadius;
                        }
                    }
                    else
                    {
                        //// Curve가 아닌 경우 바운딩 박스 중심 거리로 판단
                        //return centerDistance <= radius + boundingRadius;
                        return false;
                    }
                }
                catch (System.Exception ex)
                {
                    // GeometricExtents를 구할 수 없는 경우
                    Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning: Could not get bounds for entity type {entity.GetType().Name}: {ex.Message}");
                    // 안전하게 포함시킴 (false positive는 허용, false negative는 방지)
                    return true;
                }
                return false;
            }
            catch
            {
                // 모든 검사 실패 시 안전하게 포함시킴
                return true;
            }
        }

        /// <summary>
        /// 사용자가 검색 반경을 수동으로 설정할 수 있는 명령어
        /// </summary>
        [CommandMethod("COPYBLOCKENTITY_CUSTOM")]
        public void CopyClosestBlockEntityWithCustomRadius()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor editor = doc.Editor;
            Database db = doc.Database;

            try
            {
                // 검색 반경 입력 받기
                PromptDoubleOptions radiusOptions = new PromptDoubleOptions("\nEnter search radius (current units): ");
                radiusOptions.DefaultValue = 50.0;
                radiusOptions.AllowNegative = false;
                radiusOptions.AllowZero = false;

                PromptDoubleResult radiusResult = editor.GetDouble(radiusOptions);
                if (radiusResult.Status != PromptStatus.OK) return;

                double customRadius = radiusResult.Value;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    editor.WriteMessage($"\n=== Block Entity Copier (Custom Radius: {customRadius:F2}) ===");

                    // 블록 선택
                    PromptEntityOptions blockOptions = new PromptEntityOptions("\nSelect a block reference: ");
                    blockOptions.SetRejectMessage("\nMust be a block reference!");
                    blockOptions.AddAllowedClass(typeof(BlockReference), true);

                    PromptEntityResult blockResult = editor.GetEntity(blockOptions);
                    if (blockResult.Status != PromptStatus.OK) return;

                    Point3d selectionPoint = blockResult.PickedPoint;
                    BlockReference blockRef = trans.GetObject(blockResult.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blockRef == null) return;

                    // 현재 레이어 정보 가져오기
                    LayerTableRecord currentLayerRecord = trans.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
                    string currentLayer = currentLayerRecord?.Name ?? "0";

                    // 커스텀 반경으로 검색
                    Entity closestEntity = FindClosestEntityInBlockWithRadius(trans, blockRef, selectionPoint, customRadius);

                    if (closestEntity != null)
                    {
                        CopyEntityToCurrentLayer(trans, db, closestEntity, currentLayer);
                        editor.WriteMessage($"\n✓ Entity found and copied to layer: {currentLayer}");
                    }
                    else
                    {
                        editor.WriteMessage($"\n✗ No entity found within {customRadius:F2} units of selection point.");
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                editor.WriteMessage($"\n✗ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 지정된 반경으로 가장 가까운 엔티티 찾기
        /// </summary>
        private Entity FindClosestEntityInBlockWithRadius(Transaction trans, BlockReference blockRef, Point3d selectionPoint, double searchRadius)
        {
            Entity closestEntity = null;
            double minDistance = double.MaxValue;

            try
            {
                Matrix3d blockTransform = blockRef.BlockTransform;
                Point3d localSelectionPoint = selectionPoint.TransformBy(blockTransform.Inverse());

                BlockTableRecord blockDef = trans.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (blockDef == null) return null;

                Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
                int totalEntities = 0;
                int checkedEntities = 0;

                foreach (ObjectId entityId in blockDef)
                {
                    totalEntities++;
                    if (entityId.IsErased || !entityId.IsValid) continue;

                    try
                    {
                        DBObject dbObj = trans.GetObject(entityId, OpenMode.ForRead);

                        if (dbObj is BlockReference nestedBlockRef)
                        {
                            if (IsPointInsideNestedBlock(trans, nestedBlockRef, localSelectionPoint))
                            {
                                checkedEntities++;
                                Entity nestedResult = FindClosestEntityInNestedBlock(trans, nestedBlockRef, localSelectionPoint, blockTransform);
                                if (nestedResult != null)
                                {
                                    double distance = CalculateEntityDistance(nestedResult, selectionPoint);
                                    if (distance <= searchRadius && distance < minDistance)
                                    {
                                        minDistance = distance;
                                        closestEntity?.Dispose();
                                        closestEntity = nestedResult;
                                    }
                                    else
                                    {
                                        nestedResult.Dispose();
                                    }
                                }
                            }
                        }
                        else if (dbObj is Entity entity)
                        {
                            if (IsEntityInSearchArea(entity, localSelectionPoint, searchRadius))
                            {
                                checkedEntities++;
                                Entity transformedEntity = entity.Clone() as Entity;
                                if (transformedEntity != null)
                                {
                                    transformedEntity.TransformBy(blockTransform);

                                    double distance = CalculateEntityDistance(transformedEntity, selectionPoint);
                                    if (distance <= searchRadius && distance < minDistance)
                                    {
                                        minDistance = distance;
                                        closestEntity?.Dispose();
                                        closestEntity = transformedEntity;
                                    }
                                    else
                                    {
                                        transformedEntity.Dispose();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        editor.WriteMessage($"\nWarning processing entity {entityId}: {ex.Message}");
                        continue;
                    }
                }

                editor.WriteMessage($"\nEfficiency: {checkedEntities}/{totalEntities} entities checked ({(double)checkedEntities / totalEntities * 100:F1}%)");
                if (closestEntity != null)
                {
                    editor.WriteMessage($"\nClosest entity distance: {minDistance:F3} units");
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError: {ex.Message}");
                return null;
            }

            return closestEntity;
        }

        /// <summary>
        /// 중첩된 블록 내부의 엔티티를 찾기
        /// </summary>
        private Entity FindClosestEntityInNestedBlock(Transaction trans, BlockReference nestedBlockRef, Point3d localPoint, Matrix3d parentTransform)
        {
            try
            {
                // 중첩 블록의 변환 행렬 계산
                Matrix3d nestedTransform = nestedBlockRef.BlockTransform;
                Matrix3d combinedTransform = nestedTransform.PreMultiplyBy(parentTransform);

                // 임시 블록 참조 생성하여 재귀 호출
                using (BlockReference tempBlockRef = new BlockReference(nestedBlockRef.Position, nestedBlockRef.BlockTableRecord))
                {
                    tempBlockRef.BlockTransform = combinedTransform;
                    Point3d transformedPoint = localPoint.TransformBy(parentTransform);
                    return FindClosestEntityInBlock(trans, tempBlockRef, transformedPoint);
                }
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError in nested block processing: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 엔티티와 점 사이의 거리 계산 - AutoCAD 2025 API 호환
        /// Entity 타입에 따라 적절한 거리 계산 방법 사용
        /// </summary>
        private double CalculateEntityDistance(Entity entity, Point3d point)
        {
            try
            {
                // Curve 타입인 경우에만 GetClosestPointTo 사용
                if (entity is Curve curve)
                {
                    Point3d closestPoint = curve.GetClosestPointTo(point, false);
                    return point.DistanceTo(closestPoint);
                }
                // BlockReference의 경우 삽입점과의 거리
                else if (entity is BlockReference blockRef)
                {
                    return point.DistanceTo(blockRef.Position);
                }
                // DBText, MText의 경우 삽입점과의 거리
                else if (entity is DBText dbText)
                {
                    return point.DistanceTo(dbText.Position);
                }
                else if (entity is MText mText)
                {
                    return point.DistanceTo(mText.Location);
                }
                // Hatch의 경우 중심점과의 거리
                else if (entity is Hatch hatch)
                {
                    try
                    {
                        Extents3d bounds = hatch.GeometricExtents;
                        Point3d center = new Point3d(
                            (bounds.MinPoint.X + bounds.MaxPoint.X) / 2,
                            (bounds.MinPoint.Y + bounds.MaxPoint.Y) / 2,
                            (bounds.MinPoint.Z + bounds.MaxPoint.Z) / 2
                        );
                        return point.DistanceTo(center);
                    }
                    catch
                    {
                        // Hatch가 비어있는 경우 등
                        return double.MaxValue;
                    }
                }
                // Dimension의 경우
                else if (entity is Dimension dim)
                {
                    return point.DistanceTo(dim.TextPosition);
                }
                // 일반적인 기하학적 엔티티의 경우 바운딩 박스 중심과의 거리
                else
                {
                    try
                    {
                        Extents3d bounds = entity.GeometricExtents;
                        Point3d center = new Point3d(
                            (bounds.MinPoint.X + bounds.MaxPoint.X) / 2,
                            (bounds.MinPoint.Y + bounds.MaxPoint.Y) / 2,
                            (bounds.MinPoint.Z + bounds.MaxPoint.Z) / 2
                        );
                        return point.DistanceTo(center);
                    }
                    catch
                    {
                        // GeometricExtents를 구할 수 없는 경우
                        return double.MaxValue;
                    }
                }
            }
            catch (Exception ex)
            {
                // 모든 방법이 실패한 경우
                Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\nWarning: Could not calculate distance for entity type {entity.GetType().Name}: {ex.Message}");
                return double.MaxValue;
            }
        }

        /// <summary>
        /// 엔티티를 현재 레이어에 복사
        /// </summary>
        private void CopyEntityToCurrentLayer(Transaction trans, Database db, Entity entity, string layerName)
        {
            try
            {
                // 엔티티 복제
                Entity copiedEntity = entity.Clone() as Entity;
                if (copiedEntity == null) return;

                // 레이어 설정
                copiedEntity.Layer = layerName;

                // 현재 스페이스에 추가 (모델 스페이스 또는 페이퍼 스페이스)
                BlockTableRecord currentSpace = GetCurrentSpace(trans, db);
                if (currentSpace == null)
                {
                    copiedEntity.Dispose();
                    return;
                }

                // 엔티티를 현재 스페이스에 추가
                currentSpace.UpgradeOpen();
                ObjectId newEntityId = currentSpace.AppendEntity(copiedEntity);
                trans.AddNewlyCreatedDBObject(copiedEntity, true);

                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nEntity copied successfully with ObjectId: {newEntityId}");
            }
            catch (Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError copying entity: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 활성 스페이스 가져오기 - AutoCAD 2025 호환
        /// </summary>
        private BlockTableRecord GetCurrentSpace(Transaction trans, Database db)
        {
            try
            {
                BlockTable blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (blockTable == null) return null;

                // 현재 스페이스 확인 (TILEMODE 변수로 확인)
                object tileModeObj = Application.GetSystemVariable("TILEMODE");
                bool isModelSpace = Convert.ToInt32(tileModeObj) != 0;

                ObjectId spaceId;
                if (isModelSpace)
                {
                    spaceId = blockTable[BlockTableRecord.ModelSpace];
                }
                else
                {
                    spaceId = blockTable[BlockTableRecord.PaperSpace];
                }

                return trans.GetObject(spaceId, OpenMode.ForWrite) as BlockTableRecord;
            }
            catch
            {
                // 기본값으로 모델 스페이스 반환
                try
                {
                    BlockTable blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    ObjectId modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                    return trans.GetObject(modelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                }
                catch
                {
                    return null;
                }
            }
        }
    }



}
