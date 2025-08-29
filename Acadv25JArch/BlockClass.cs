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
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

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
                        blockImage.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
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
        public Entity OriginalEntity { get; set; }
        public Matrix3d TransformMatrix { get; set; }
        public string EntityType { get; set; }
        public string Layer { get; set; }
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


}
