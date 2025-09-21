using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using CADExtension;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;

namespace Acadv25JArch
{

    public class TableCommands
    {
        [CommandMethod("TABLEROWINFO")]
        public void GetTableRowInfo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 테이블 선택 프롬프트
                PromptEntityOptions peo = new PromptEntityOptions("\n테이블의 셀을 클릭하세요: ");
                peo.SetRejectMessage("\n테이블 객체만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Table), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 선택한 테이블 가져오기
                    Table table = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Table;

                    if (table == null)
                    {
                        ed.WriteMessage("\n테이블을 찾을 수 없습니다.");
                        return;
                    }

                    // 클릭한 포인트 가져오기
                    Point3d pickPoint = per.PickedPoint;

                    // 테이블 좌표계로 변환
                    Point3d transformedPoint = pickPoint.TransformBy(table.BlockTransform.Inverse());

                    // 클릭한 셀의 행과 열 인덱스 찾기
                    TableHitTestInfo hitInfo = table.HitTest(transformedPoint, Vector3d.ZAxis);

                    if (hitInfo.Type == TableHitTestType.Cell)
                    {
                        int rowIndex = hitInfo.Row;
                        int colIndex = hitInfo.Column;

                        // 행 정보 수집
                        StringBuilder rowInfo = new StringBuilder();
                        rowInfo.AppendLine("\n========== 테이블 행 정보 ==========");
                        rowInfo.AppendLine($"선택한 셀: 행 {rowIndex + 1}, 열 {colIndex + 1}");
                        rowInfo.AppendLine($"행 높이: {table.Rows[rowIndex].Height:F2}");
                        rowInfo.AppendLine("\n--- 해당 행의 모든 셀 정보 ---");

                        // 해당 행의 모든 셀 정보 가져오기
                        int numColumns = table.Columns.Count;

                        for (int col = 0; col < numColumns; col++)
                        {
                            try
                            {
                                Cell cell = table.Cells[rowIndex, col];

                                // 셀이 병합되었는지 확인
                                CellRange mergeRange;
                                bool isMerged = table.IsMergedCell(rowIndex, col, out mergeRange);

                                if (isMerged)
                                {
                                    if (mergeRange.TopRow == rowIndex && mergeRange.LeftColumn == col)
                                    {
                                        // 병합된 셀의 첫 번째 셀인 경우
                                        string cellValue = cell.TextString;
                                        rowInfo.AppendLine($"  열 {col + 1}: {cellValue} [병합된 셀]");
                                        rowInfo.AppendLine($"    - 병합 범위: 행 {mergeRange.TopRow + 1}-{mergeRange.BottomRow + 1}, 열 {mergeRange.LeftColumn + 1}-{mergeRange.RightColumn + 1}");
                                    }
                                    else
                                    {
                                        rowInfo.AppendLine($"  열 {col + 1}: [병합된 셀의 일부]");
                                    }
                                }
                                else
                                {
                                    // 일반 셀
                                    string cellValue = cell.TextString;
                                    rowInfo.AppendLine($"  열 {col + 1}: {cellValue}");

                                    // 정렬 정보 (nullable 처리)
                                    if (cell.Alignment.HasValue)
                                    {
                                        CellAlignment alignment = cell.Alignment.Value;
                                        rowInfo.AppendLine($"    - 정렬: {alignment}");
                                    }

                                    // 셀 스타일 정보
                                    string cellStyle = cell.Style;
                                    if (!string.IsNullOrEmpty(cellStyle))
                                    {
                                        rowInfo.AppendLine($"    - 스타일: {cellStyle}");
                                    }

                                    // 배경색 정보
                                    Autodesk.AutoCAD.Colors.Color bgColor = cell.BackgroundColor;
                                    if (bgColor != null && bgColor.ColorIndex != 0)
                                    {
                                        rowInfo.AppendLine($"    - 배경색 인덱스: {bgColor.ColorIndex}");
                                    }

                                    // 테두리 정보 (nullable 처리)
                                    if (cell.Borders != null && cell.Borders.Top != null)
                                    {
                                        LineWeight? topBorderWeight = cell.Borders.Top.LineWeight;
                                        if (topBorderWeight.HasValue)
                                        {
                                            // LineWeight enum 값을 double로 변환
                                            double borderWidth = GetLineWeightValue(topBorderWeight.Value);
                                            if (borderWidth > 0)
                                            {
                                                rowInfo.AppendLine($"    - 테두리 두께: {borderWidth:F2}mm");
                                            }
                                        }
                                    }

                                    // 텍스트 높이
                                    double textHeight = (double)cell.TextHeight;
                                    if (textHeight > 0)
                                    {
                                        rowInfo.AppendLine($"    - 텍스트 높이: {textHeight:F2}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                rowInfo.AppendLine($"  열 {col + 1}: [읽기 오류: {ex.Message}]");
                            }
                        }

                        // 셀 타입 정보
                        TableCellType cellType = table.Cells[rowIndex, 0].CellType;
                        rowInfo.AppendLine($"\n첫 번째 셀 타입: {GetCellTypeDescription(cellType)}");

                        // 결과 출력
                        ed.WriteMessage(rowInfo.ToString());

                        // 선택한 행 하이라이트 (선택적)
                        HighlightTableRow(table, rowIndex, ed);
                    }
                    else
                    {
                        ed.WriteMessage("\n테이블 셀을 정확히 클릭해주세요.");
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// LineWeight enum 값을 mm 단위로 변환
        /// </summary>
        private double GetLineWeightValue(LineWeight lineWeight)
        {
            switch (lineWeight)
            {
                case LineWeight.LineWeight000: return 0.00;
                case LineWeight.LineWeight005: return 0.05;
                case LineWeight.LineWeight009: return 0.09;
                case LineWeight.LineWeight013: return 0.13;
                case LineWeight.LineWeight015: return 0.15;
                case LineWeight.LineWeight018: return 0.18;
                case LineWeight.LineWeight020: return 0.20;
                case LineWeight.LineWeight025: return 0.25;
                case LineWeight.LineWeight030: return 0.30;
                case LineWeight.LineWeight035: return 0.35;
                case LineWeight.LineWeight040: return 0.40;
                case LineWeight.LineWeight050: return 0.50;
                case LineWeight.LineWeight053: return 0.53;
                case LineWeight.LineWeight060: return 0.60;
                case LineWeight.LineWeight070: return 0.70;
                case LineWeight.LineWeight080: return 0.80;
                case LineWeight.LineWeight090: return 0.90;
                case LineWeight.LineWeight100: return 1.00;
                case LineWeight.LineWeight106: return 1.06;
                case LineWeight.LineWeight120: return 1.20;
                case LineWeight.LineWeight140: return 1.40;
                case LineWeight.LineWeight158: return 1.58;
                case LineWeight.LineWeight200: return 2.00;
                case LineWeight.LineWeight211: return 2.11;
                case LineWeight.ByLayer: return -1.0; // ByLayer
                case LineWeight.ByBlock: return -2.0; // ByBlock
                case LineWeight.ByLineWeightDefault: return -3.0; // Default
                default: return 0.0;
            }
        }

        /// <summary>
        /// 테이블 행 하이라이트 (시각적 피드백)
        /// </summary>
        private void HighlightTableRow(Table table, int rowIndex, Editor ed)
        {
            try
            {
                // 임시로 행의 모든 셀 선택 표시
                int numColumns = table.Columns.Count;
                StringBuilder cellPositions = new StringBuilder();
                cellPositions.Append("선택된 행의 셀 위치: ");

                for (int col = 0; col < numColumns; col++)
                {
                    CellRange mergeRange;
                    bool isMerged = table.IsMergedCell(rowIndex, col, out mergeRange);

                    if (!isMerged ||
                        (isMerged && mergeRange.TopRow == rowIndex && mergeRange.LeftColumn == col))
                    {
                        cellPositions.Append($"[{rowIndex},{col}] ");
                    }
                }

                ed.WriteMessage($"\n{cellPositions}");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n하이라이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 셀 타입 설명 반환
        /// </summary>
        private string GetCellTypeDescription(TableCellType cellType)
        {
            switch (cellType)
            {
                //case TableCellType.Title:
                //    return "제목 셀";
                //case TableCellType.Header:
                //    return "헤더 셀";
                //case TableCellType.Data:
                //    return "데이터 셀";
                default:
                    return "알 수 없음";
            }
        }

        /// <summary>
        /// 테이블의 모든 데이터를 CSV로 내보내기
        /// </summary>
        [CommandMethod("EXPORTTABLEDATA")]
        public void ExportTableData()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n내보낼 테이블을 선택하세요: ");
                peo.SetRejectMessage("\n테이블 객체만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Table), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Table table = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Table;

                    if (table == null)
                    {
                        ed.WriteMessage("\n테이블을 찾을 수 없습니다.");
                        return;
                    }

                    StringBuilder csvData = new StringBuilder();
                    int numRows = table.Rows.Count;
                    int numCols = table.Columns.Count;

                    // 모든 행과 열 순회
                    for (int row = 0; row < numRows; row++)
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            try
                            {
                                Cell cell = table.Cells[row, col];
                                string cellValue = cell.TextString;

                                // CSV 형식으로 처리 (쉼표와 따옴표 처리)
                                if (cellValue.Contains(",") || cellValue.Contains("\""))
                                {
                                    cellValue = "\"" + cellValue.Replace("\"", "\"\"") + "\"";
                                }
                                csvData.Append(cellValue);
                            }
                            catch
                            {
                                csvData.Append("");
                            }

                            if (col < numCols - 1)
                                csvData.Append(",");
                        }
                        csvData.AppendLine();
                    }

                    // 파일로 저장
                    string fileName = $"TableExport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    string filePath = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                        fileName
                    );

                    System.IO.File.WriteAllText(filePath, csvData.ToString(), System.Text.Encoding.UTF8);
                    ed.WriteMessage($"\n테이블 데이터가 다음 위치에 저장되었습니다: {filePath}");

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 실시간으로 테이블 셀 모니터링
        /// </summary>
        [CommandMethod("MONITORTABLE")]
        public void MonitorTableSelection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n테이블 모니터링을 시작합니다. ESC를 눌러 종료하세요.");

            while (true)
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n테이블 셀을 클릭하세요 (ESC로 종료): ");
                peo.SetRejectMessage("\n테이블만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Table), true);
                peo.AllowNone = true;

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status == PromptStatus.Cancel || per.Status == PromptStatus.None)
                {
                    ed.WriteMessage("\n테이블 모니터링을 종료합니다.");
                    break;
                }

                if (per.Status == PromptStatus.OK)
                {
                    GetTableRowInfo();
                }
            }
        }

        /// <summary>
        /// 테이블 정보 요약 표시
        /// </summary>
        [CommandMethod("TABLESUMMARY")]
        public void ShowTableSummary()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                PromptEntityOptions peo = new PromptEntityOptions("\n테이블을 선택하세요: ");
                peo.SetRejectMessage("\n테이블 객체만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Table), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Table table = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Table;

                    if (table == null)
                    {
                        ed.WriteMessage("\n테이블을 찾을 수 없습니다.");
                        return;
                    }

                    StringBuilder summary = new StringBuilder();
                    summary.AppendLine("\n========== 테이블 요약 정보 ==========");
                    summary.AppendLine($"행 수: {table.Rows.Count}");
                    summary.AppendLine($"열 수: {table.Columns.Count}");
                    summary.AppendLine($"총 셀 수: {table.Rows.Count * table.Columns.Count}");

                    // 전체 테이블 크기
                    double totalWidth = 0;
                    double totalHeight = 0;

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        totalWidth += table.Columns[i].Width;
                    }

                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        totalHeight += table.Rows[i].Height;
                    }

                    summary.AppendLine($"테이블 너비: {totalWidth:F2}");
                    summary.AppendLine($"테이블 높이: {totalHeight:F2}");

                    // 병합된 셀 개수 계산
                    int mergedCellCount = 0;
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        for (int col = 0; col < table.Columns.Count; col++)
                        {
                            CellRange mergeRange;
                            if (table.IsMergedCell(row, col, out mergeRange))
                            {
                                if (mergeRange.TopRow == row && mergeRange.LeftColumn == col)
                                {
                                    mergedCellCount++;
                                }
                            }
                        }
                    }

                    summary.AppendLine($"병합된 셀 그룹 수: {mergedCellCount}");

                    // 테이블 스타일 정보
                    if (table.TableStyle != ObjectId.Null)
                    {
                        TableStyle tableStyle = tr.GetObject(table.TableStyle, OpenMode.ForRead) as TableStyle;
                        if (tableStyle != null)
                        {
                            summary.AppendLine($"테이블 스타일: {tableStyle.Name}");
                        }
                    }

                    ed.WriteMessage(summary.ToString());

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 텍스트를 포함한 셀 찾기
        /// </summary>
        [CommandMethod("FINDINCELLS")]
        public void FindTextInCells()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 검색할 텍스트 입력
                PromptStringOptions pso = new PromptStringOptions("\n찾을 텍스트를 입력하세요: ");
                pso.AllowSpaces = true;
                PromptResult pr = ed.GetString(pso);

                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                    return;

                string searchText = pr.StringResult.ToLower();

                // 테이블 선택
                PromptEntityOptions peo = new PromptEntityOptions("\n테이블을 선택하세요: ");
                peo.SetRejectMessage("\n테이블 객체만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Table), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Table table = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Table;

                    if (table == null)
                    {
                        ed.WriteMessage("\n테이블을 찾을 수 없습니다.");
                        return;
                    }

                    StringBuilder results = new StringBuilder();
                    results.AppendLine($"\n========== '{pr.StringResult}' 검색 결과 ==========");
                    int foundCount = 0;

                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        for (int col = 0; col < table.Columns.Count; col++)
                        {
                            try
                            {
                                Cell cell = table.Cells[row, col];
                                string cellText = cell.TextString;

                                if (!string.IsNullOrEmpty(cellText) &&
                                    cellText.ToLower().Contains(searchText))
                                {
                                    foundCount++;
                                    results.AppendLine($"행 {row + 1}, 열 {col + 1}: {cellText}");
                                }
                            }
                            catch
                            {
                                // 병합된 셀 또는 읽기 오류 무시
                            }
                        }
                    }

                    results.AppendLine($"\n총 {foundCount}개의 셀에서 찾았습니다.");
                    ed.WriteMessage(results.ToString());

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }



}
