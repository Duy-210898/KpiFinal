using DevExpress.Mvvm;
using DevExpress.XtraEditors;
using KpiApplication.Models;
using KpiApplication.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KpiApplication.Excel
{
    public class ExcelExporter

    {
        public static void ExportSimpleDataTableToExcel(DataTable table, string filePath, string sheetName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // Load data starting from A1, including headers
                worksheet.Cells["A1"].LoadFromDataTable(table, true);
                worksheet.Cells[worksheet.Dimension.Address].AutoFilter = true;

                int totalRows = table.Rows.Count + 1; // +1 for header
                int totalCols = table.Columns.Count;

                // Format header row (row 1)
                using (var headerRange = worksheet.Cells[1, 1, 1, totalCols])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);

                    headerRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    headerRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    headerRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    headerRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                // Format data rows
                for (int col = 1; col <= totalCols; col++)
                {
                    var columnType = table.Columns[col - 1].DataType;

                    var dataRange = worksheet.Cells[2, col, totalRows, col];

                    if (columnType == typeof(int) || columnType == typeof(double) || columnType == typeof(decimal))
                    {
                        dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        dataRange.Style.Numberformat.Format = "#,##0"; // Format with comma separator
                    }
                    else if (columnType == typeof(DateTime))
                    {
                        dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        dataRange.Style.Numberformat.Format = "dd-MMM-yyyy";
                    }

                    // Border for data
                    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                // Auto-fit all columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Save to file
                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
        }
        public static void ExportIETotalPivoted(List<IETotal_Model> ieTotalList, string filePath, bool includeTCT)
        {
            try
            {
                if (ieTotalList == null || ieTotalList.Count == 0)
                    throw new ArgumentException("Danh sách dữ liệu trống");

                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("Đường dẫn file không hợp lệ.");

                // Lọc dữ liệu loại bỏ các bản ghi thiếu thông tin bắt buộc để tránh lỗi
                ieTotalList = ieTotalList
                    .Where(x => !string.IsNullOrEmpty(x.ModelName)
                             && !string.IsNullOrEmpty(x.ArticleName)
                             && !string.IsNullOrEmpty(x.Process))
                    .ToList();

                if (ieTotalList.Count == 0)
                    throw new ArgumentException("Không có bản ghi hợp lệ để xuất.");

                var fixedColumns = new List<string> { "Model Name\nTên hình thể", "Article" };
                var processOrder = new List<string> {
                "Stitching", "Assembly", "Stock Fitting",
                "Cutting", "Computer Stitching", "Tongue",
                "Outsole buffing stock", "Outsole buffing assembly",
                "Irradiation", "Packing"
            };

                var processesWithIsSigned = new HashSet<string> { "Stitching", "Assembly", "Stock Fitting" };

                int GetColumnCountForProcess(string process)
                {
                    int baseCols = processesWithIsSigned.Contains(process) ? 5 : 4;
                    if (includeTCT) baseCols += 1;
                    return baseCols;
                }

                var allProcessesInData = ieTotalList.Select(x => x.Process).Distinct().ToList();

                var firstProcesses = new List<string> { "Stitching", "Assembly", "Stock Fitting" }
                    .Where(p => allProcessesInData.Contains(p)).ToList();

                var remainingProcesses = allProcessesInData
                    .Except(firstProcesses)
                    .OrderBy(p => processOrder.IndexOf(p) >= 0 ? processOrder.IndexOf(p) : int.MaxValue)
                    .ThenBy(p => p)
                    .ToList();

                var grouped = ieTotalList
                    .GroupBy(x => new { x.ModelName, x.ArticleName, x.NoteForPC })
                    .ToList();

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("IE PPH Total");

                    // Định dạng các dòng header
                    ws.Row(1).Height = 50;
                    ws.Row(2).Height = 60;
                    ws.Row(3).Height = 30;

                    int col = 1;
                    // Cột cố định đầu tiên: Model Name, Article
                    foreach (var c in fixedColumns)
                    {
                        col++;
                    }

                    int noteForPCAfterCol = col;

                    var headerFillColor = Color.LightBlue;

                    // Ghi tiêu đề process
                    foreach (var process in processOrder)
                    {
                        int colsCount = GetColumnCountForProcess(process);
                        int startCol = col;
                        int endCol = startCol + colsCount - 1;

                        ws.Cells[2, startCol, 2, endCol].Merge = true;
                        ws.Cells[2, startCol].Value = process;
                        StyleHeaderCell(ws.Cells[2, startCol, 2, endCol], headerFillColor, true);

                        col += colsCount;

                        if (process == "Stock Fitting")
                            noteForPCAfterCol = col;
                    }

                    // Thêm cột Note For PC và Remark
                    ws.InsertColumn(noteForPCAfterCol, 1);
                    ws.Cells[2, noteForPCAfterCol].Value = "";

                    int remarkColIndex = col + 1;
                    ws.Cells[2, remarkColIndex].Value = "";
                    ws.Cells[3, remarkColIndex].Value = "Remark";
                    StyleHeaderCell(ws.Cells[3, remarkColIndex], headerFillColor, false);

                    // Merge header
                    MergeAndStyleHeader(ws, 1, 1, noteForPCAfterCol,
                        "IE PPH standard for production bonus\n(Chỉ số IE PPH tiêu chuẩn cho tính tiền sản lượng của sản xuất)");

                    MergeAndStyleHeader(ws, 1, noteForPCAfterCol + 1, remarkColIndex,
                        "Not for bonus, data requested by the PC\n(Không sử dụng cho tính tiền sản lượng, dữ liệu được yêu cầu từ sinh quản)");

                    // Header dòng 3
                    col = 1;
                    foreach (var c in fixedColumns)
                    {
                        ws.Cells[3, col].Value = c;
                        StyleHeaderCell(ws.Cells[3, col], headerFillColor, false);
                        col++;
                    }

                    foreach (var process in processOrder)
                    {
                        if (col == noteForPCAfterCol) col++;

                        List<string> currentProcessColumns = new List<string> { "Type\nLoại" };
                        if (processesWithIsSigned.Contains(process))
                            currentProcessColumns.Add("Production Sign\nSản xuất ký tên xác nhận");
                        currentProcessColumns.AddRange(new string[] {
                        "Target Output Of PC\nMục tiêu sản lượng của PC",
                        "Adjust Operator No\nSố người điều chỉnh",
                        "IE PPH"
                    });
                        if (includeTCT)
                            currentProcessColumns.Add("TCT");

                        foreach (var headerText in currentProcessColumns)
                        {
                            ws.Cells[3, col].Value = headerText;
                            StyleHeaderCell(ws.Cells[3, col], headerFillColor, false);
                            col++;
                        }
                    }

                    ws.Cells[3, noteForPCAfterCol].Value = "Note For PC";
                    StyleHeaderCell(ws.Cells[3, noteForPCAfterCol], headerFillColor, false);

                    ws.View.FreezePanes(4, 3);
                    ws.Cells[3, 1, 3, remarkColIndex].AutoFilter = true;

                    int row = 4;
                    foreach (var group in grouped)
                    {
                        col = 1;
                        ws.Cells[row, col++].Value = group.Key.ModelName;
                        ws.Cells[row, col++].Value = group.Key.ArticleName;

                        var dictProcess = group.GroupBy(x => x.Process).ToDictionary(g => g.Key, g => g.First());

                        foreach (var process in processOrder)
                        {
                            if (col == noteForPCAfterCol) col++;

                            if (dictProcess.TryGetValue(process, out var data))
                            {
                                ws.Cells[row, col++].Value = data.TypeName;

                                if (processesWithIsSigned.Contains(process))
                                    ws.Cells[row, col++].Value = data.IsSigned;

                                ws.Cells[row, col++].Value = data.TargetOutputPC;
                                ws.Cells[row, col++].Value = data.AdjustOperatorNo;
                                ws.Cells[row, col++].Value = data.IEPPHValue;
                                if (includeTCT)
                                    ws.Cells[row, col++].Value = data.TCTValue;
                            }
                            else
                            {
                                col += GetColumnCountForProcess(process);
                            }
                        }

                        ws.Cells[row, noteForPCAfterCol].Value = group.Key.NoteForPC;
                        ws.Cells[row, remarkColIndex].Value = "";
                        row++;
                    }

                    int lastRow = row - 1;

                    ApplyExcelStylesVariableColumns(
                        ws,
                        fixedColumns.Count,
                        noteForPCAfterCol,
                        remarkColIndex,
                        2,
                        lastRow,
                        remarkColIndex,
                        processOrder,
                        processesWithIsSigned,
                        includeTCT);

                    if (ws.Dimension != null)
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    File.WriteAllBytes(filePath, package.GetAsByteArray());
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Lỗi xuất Excel", ex);
                throw;
            }
        }
        private static void ApplyExcelStylesVariableColumns(
            ExcelWorksheet ws,
            int fixedColCount,
            int noteColIndex,
            int remarkColIndex,
            int headerStartRow,
            int lastRow,
            int lastCol,
            List<string> processOrder,
            HashSet<string> processesWithIsSigned,
            bool includeTCT)
        {
            // Định nghĩa màu cho từng nhóm
            Color colorGroup1 = Color.FromArgb(217, 225, 242); // Light Blue
            Color colorGroup2 = Color.FromArgb(255, 242, 204); // Light Yellow
            Color colorGroup3 = Color.FromArgb(224, 235, 217); // Light Green

            // Khai báo nhóm
            var group1 = new HashSet<string> { "Stitching", "Cutting", "Tongue", "Computer Stitching" };
            var group2 = new HashSet<string> { "Assembly", "Outsole buffing assembly", "Packing", "Irradiation" };
            var group3 = new HashSet<string> { "Stock Fitting", "Outsole buffing stock" };

            int colStart = fixedColCount + 1;

            int GetColumnCountForProcess(string process)
            {
                int baseCols = processesWithIsSigned.Contains(process) ? 5 : 4;
                if (includeTCT) baseCols += 1;
                return baseCols;
            }

            foreach (var process in processOrder)
            {
                int colsCount = GetColumnCountForProcess(process);
                if (colStart == noteColIndex)
                    colStart++;

                int colEnd = Math.Min(colStart + colsCount - 1, lastCol);

                Color fillColor;
                if (group1.Contains(process)) fillColor = colorGroup1;
                else if (group2.Contains(process)) fillColor = colorGroup2;
                else if (group3.Contains(process)) fillColor = colorGroup3;
                else fillColor = Color.White;

                using (var range = ws.Cells[headerStartRow, colStart, 3, colEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(fillColor);
                }

                colStart += colsCount;
            }

            // Tô màu Note For PC
            using (var noteRange = ws.Cells[headerStartRow, noteColIndex, lastRow, noteColIndex])
            {
                noteRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                noteRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            // Tô màu Remark
            using (var remarkRange = ws.Cells[headerStartRow, remarkColIndex, lastRow, remarkColIndex])
            {
                remarkRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                remarkRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            }

            // Kẻ viền toàn bộ bảng
            using (var range = ws.Cells[headerStartRow, 1, lastRow, lastCol])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }
        // Các hàm MergeAndStyleHeader và StyleHeaderCell giữ nguyên như cũ
        private static void MergeAndStyleHeader(ExcelWorksheet ws, int row, int startCol, int endCol, string text)
        {
            var range = ws.Cells[row, startCol, row, endCol];
            range.Merge = true;
            var cell = ws.Cells[row, startCol];
            cell.Value = text;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.WrapText = true;

            // Kẻ viền ngoài
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }
        private static void StyleHeaderCell(ExcelRange range, Color fillColor, bool fontBold)
        {
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            range.Style.Font.Bold = fontBold;
            range.Style.WrapText = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(fillColor);
        }
        public static void CreateNewExcelFile(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add("Stitching");
                package.Workbook.Worksheets.Add("Assembling");
                package.SaveAs(new FileInfo(filePath));
            }
        }
        public static void ExportVerticalReport(DataTable dataTable, string filePath, string sheetName)
        {
            var columnsToExport = GetColumnsToExport();
            var columnMappings = GetColumnMappings();

            // Ép sắp xếp theo ScanDate tăng dần nếu có cột này
            if (dataTable.Columns.Contains("ScanDate"))
            {
                dataTable = dataTable.AsEnumerable()
                    .OrderBy(r => r.Field<DateTime>("ScanDate"))
                    .CopyToDataTable();
            }

            if (!columnsToExport.Contains("ScanDate"))
                columnsToExport.Insert(0, "ScanDate");

            if (!columnMappings.ContainsKey("ScanDate"))
                columnMappings["ScanDate"] = "ScanDate";

            var fileInfo = new FileInfo(filePath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.Add(sheetName);

                int headerRow = 1;
                int startCol = 1;

                // Kiểm tra xem sheet có dữ liệu chưa
                bool isNewSheet = worksheet.Dimension == null;
                int startRow = worksheet.Dimension?.End.Row + 1 ?? 2;

                var validColumns = columnsToExport.Where(col => dataTable.Columns.Contains(col)).ToList();

                // Ghi tiêu đề nếu là sheet mới
                if (isNewSheet)
                {
                    for (int col = 0; col < validColumns.Count; col++)
                    {
                        string colName = validColumns[col];
                        var cell = worksheet.Cells[headerRow, startCol + col];
                        cell.Value = columnMappings.ContainsKey(colName) ? columnMappings[colName] : colName;
                        cell.Style.Font.Bold = true;
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.WrapText = true;
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.PaleTurquoise);
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                }

                int rowIndex = startRow;
                var numericColumns = new HashSet<string> { "IEPPH", "TotalWorker", "WorkingTime", "Quantity", "ActualPPH", "PPHRate" };

                foreach (DataRow row in dataTable.Rows)
                {
                    bool isEvenRow = rowIndex % 2 == 0;
                    Color defaultColor = isEvenRow ? Color.AliceBlue : Color.White;
                    Color lineNameColor = isEvenRow ? Color.LightYellow : Color.White;
                    double? actualPPH = null;

                    for (int col = 0; col < validColumns.Count; col++)
                    {
                        string colName = validColumns[col];
                        var cell = worksheet.Cells[rowIndex, startCol + col];
                        var value = row[colName];

                        switch (colName)
                        {
                            case "ScanDate":
                                if (DateTime.TryParse(value?.ToString(), out DateTime dateValue))
                                {
                                    cell.Value = dateValue;
                                    cell.Style.Numberformat.Format = "dd-MMM-yyyy";
                                }
                                else
                                {
                                    cell.Value = value;
                                }
                                break;

                            case "PPHRate":
                                string rawValue = value?.ToString()?.Trim() ?? "";
                                rawValue = rawValue.Replace("%", "").Replace(",", ".");

                                if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double pphRate))
                                {
                                    cell.Style.Numberformat.Format = "0.0%";
                                    cell.Value = pphRate / 100.0;

                                    if (pphRate < 90)
                                    {
                                        cell.Style.Font.Color.SetColor(Color.Red);
                                    }
                                }
                                else
                                {
                                    cell.Value = value;
                                }
                                break;

                            case "ActualPPH":
                                if (double.TryParse(value?.ToString(), out double parsed))
                                {
                                    actualPPH = parsed;
                                    cell.Value = parsed;
                                }
                                else
                                {
                                    cell.Value = value;
                                }
                                break;

                            default:
                                cell.Value = value;
                                break;
                        }

                        if (numericColumns.Contains(colName))
                        {
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        var bg = colName == "LineName" ? lineNameColor : defaultColor;
                        ApplyCellStyle(cell, bg);
                    }

                    if (actualPPH.HasValue)
                    {
                        HighlightLowPPH(worksheet, rowIndex, validColumns, startCol, actualPPH.Value);
                    }

                    rowIndex++;
                }

                AutoFitColumns(worksheet, startCol, validColumns.Count);
                package.Save();
            }
        }

        public static void ExportReportDataToExcel(DataTable dataTable, string filePath, string newFilePath, string sheetName, string scanDate)
        {
            var columnsToExport = GetColumnsToExport();
            var columnMappings = GetColumnMappings();

            var fileInfo = new FileInfo(filePath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.Add(sheetName);
                int startColumnIndex = (worksheet.Dimension?.Columns ?? 0) + 1;

                FormatMergedHeaderCell(worksheet, startColumnIndex, columnsToExport.Count, scanDate);
                SetColumnHeaders(worksheet, columnsToExport, columnMappings, startColumnIndex);
                PopulateData(worksheet, dataTable, columnsToExport, startColumnIndex);
                AutoFitColumns(worksheet, startColumnIndex, columnsToExport.Count);

                package.SaveAs(new FileInfo(newFilePath));
            }
        }
        private static List<string> GetColumnsToExport()
        {
            return new List<string>
    {
        "LineName",
        "ModelName",
        "IEPPH",
        "TotalWorker",
        "WorkingTime",
        "Quantity",
        "ActualPPH",
        "PPHRate"
    };
        }
        private static Dictionary<string, string> GetColumnMappings()
        {
            return new Dictionary<string, string>
    {
        { "LineName", "Line\nChuyền" },
        { "ModelName", "Model\nHình thể" },
        { "IEPPH", "IE PPH\nIE PPH\n(Mục tiêu)" },
        { "TotalWorker", "Operator\nSố người" },
        { "WorkingTime", "Working Hours\nThời gian làm việc" },
        { "Quantity", "Output\nSản lượng" },
        { "ActualPPH", "Actual PPH\nPPH thực tế" },
        { "PPHRate", "PPH achievement rate\nTỷ lệ đạt được PPH" }
    };
        }


        private static void PopulateData(ExcelWorksheet worksheet, DataTable dataTable, List<string> columnsToExport, int startColumnIndex)
        {
            int rowIndex = 4;
            var validColumns = columnsToExport.Where(col => dataTable.Columns.Contains(col)).ToList();

            foreach (DataRow row in dataTable.Rows)
            {
                int colIndex = startColumnIndex;
                double? actualPPH = null;
                bool isEvenRow = rowIndex % 2 == 0;
                Color defaultRowColor = isEvenRow ? Color.AliceBlue : Color.White;
                Color lineNameRowColor = isEvenRow ? Color.LightYellow : Color.White;

                foreach (var columnName in validColumns)
                {
                    var cell = worksheet.Cells[rowIndex, colIndex];
                    var cellValue = row[columnName];

                    switch (columnName)
                    {
                        case "PPHRate":
                            string rawValue = cellValue?.ToString()?.Trim() ?? "";

                            rawValue = rawValue.Replace("%", "").Replace(",", ".");

                            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double pphRate))
                            {
                                cell.Style.Numberformat.Format = "0.0%";
                                cell.Value = pphRate / 100.0;

                                if (pphRate < 90)
                                {
                                    cell.Style.Font.Color.SetColor(Color.Red);
                                }
                            }
                            else
                            {
                                cell.Value = cellValue;
                            }
                            break;

                        case "ActualPPH":
                            if (double.TryParse(cellValue.ToString(), out double parsedActualPPH))
                            {
                                cell.Value = parsedActualPPH;
                                actualPPH = parsedActualPPH;
                            }
                            else
                            {
                                cell.Value = cellValue;
                            }
                            break;

                        default:
                            cell.Value = cellValue;
                            break;
                    }

                    var bgColor = columnName == "LineName" ? lineNameRowColor : defaultRowColor;
                    ApplyCellStyle(cell, bgColor);

                    colIndex++;
                }

                if (actualPPH.HasValue)
                {
                    HighlightLowPPH(worksheet, rowIndex, validColumns, startColumnIndex, actualPPH.Value);
                }

                rowIndex++;
            }
        }



        private static void SetColumnHeaders(ExcelWorksheet worksheet, List<string> columnsToExport, Dictionary<string, string> columnMappings, int startColumnIndex)
        {
            int colIndex = startColumnIndex;

            foreach (var columnName in columnsToExport)
            {
                if (columnMappings.ContainsKey(columnName))
                {
                    var cell = worksheet.Cells[3, colIndex];
                    cell.Value = columnMappings[columnName];
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Font.Bold = true;
                    cell.Style.WrapText = true;

                    if (columnName == "LineName")
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                    }
                    else
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.PaleTurquoise);
                    }

                    colIndex++;
                }
            }

            worksheet.Cells[3, startColumnIndex, 3, colIndex - 1].AutoFilter = true;
        }

        private static void HighlightLowPPH(ExcelWorksheet worksheet, int rowIndex, List<string> columnsToExport, int startColumnIndex, double actualPPH)
        {
            var iePphCell = worksheet.Cells[rowIndex, columnsToExport.IndexOf("IEPPH") + startColumnIndex];
            if (double.TryParse(iePphCell.Text, out double iePPH) && actualPPH < iePPH)
            {
                worksheet.Cells[rowIndex, columnsToExport.IndexOf("ActualPPH") + startColumnIndex].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[rowIndex, columnsToExport.IndexOf("ActualPPH") + startColumnIndex].Style.Fill.BackgroundColor.SetColor(Color.Orange);
            }
        }


        private static void ApplyCellStyle(ExcelRange cell, Color rowColor)
        {
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(rowColor);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            cell.Style.WrapText = true;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
        private static void AutoFitColumns(ExcelWorksheet worksheet, int startColumnIndex, int columnCount)
        {
            for (int i = startColumnIndex; i < startColumnIndex + columnCount; i++)
            {
                var column = worksheet.Column(i);
                column.AutoFit();
                if (column.Width < 20) column.Width = 20;
            }
        }

        private static void FormatMergedHeaderCell(ExcelWorksheet worksheet, int startColumnIndex, int columnCount, string scanDate)
        {
            DateTime scanDateValue = DateTime.Parse(scanDate);
            string formattedScanDate = scanDateValue.ToString("dd-MMM");

            var mergedCellRange = worksheet.Cells[2, startColumnIndex, 2, startColumnIndex + columnCount - 1];
            mergedCellRange.Merge = true;

            mergedCellRange.Value = formattedScanDate;
            mergedCellRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            mergedCellRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            mergedCellRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            mergedCellRange.Style.Font.Bold = true;
            mergedCellRange.Style.Font.Size = 12;

            mergedCellRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            mergedCellRange.Style.Fill.BackgroundColor.SetColor(Color.Beige);

            var border = mergedCellRange.Style.Border;
            border.BorderAround(ExcelBorderStyle.Thin);

            worksheet.Row(2).Height = 35;
        }

        public static void ExportKPIDataToExcel(DataTable dataTable, string fileName)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("KPI Data");

                var fixedColumns = new Dictionary<string, string>
        {
            { "ScanDate", "日期\nDate" },
            { "Plant", "車間\nPlant" },
            { "Process", "裁斷/針車/加工\nCutting/Sititching/Assembly" },
            { "LineName", "線\nLine" },
            { "ModelName", "鞋型\nModel" },
            { "LargestOutput", "Model with\nLargest Output" },
            { "Article", "ART" },
            { "TotalWorker", "人数\nLabor" },
            { "WorkingTime", "上班时间\nHours" },
            { "TotalWorkingHours", "投入工時\nTotal working hours" },
            { "Target", "目标產量\nTarget Output" },
            { "Quantity", "實際產量\nActual Output" },
            { "IEPPH", "目標PPH\nTarget PPH" },
            { "ActualPPH", "實際PPH\nActual PPH" },
            { "PPHRate", "IE PPH達成率\nIE PPH Achievement" },
            { "PPHFallsBelowReasons", "PPH未达标原因 \nPPH falls below reasons" }
        };

                var columnWidths = new Dictionary<string, double>
        {
            { "ScanDate", 15 },
            { "Plant", 15 },
            { "Process", 15 },
            { "LineName", 12 },
            { "ModelName", 25 },
            { "LargestOutput", 25 },
            { "Article", 15 },
            { "TotalWorker", 10 },
            { "WorkingTime", 10 },
            { "TotalWorkingHours", 10 },
            { "Target", 10 },
            { "Quantity", 10 },
            { "IEPPH", 10 },
            { "ActualPPH", 10 },
            { "PPHRate", 10 },
            { "PPHFallsBelowReasons", 10 }
        };

                int colIndex = 1;
                foreach (var column in fixedColumns)
                {
                    var cell = worksheet.Cells[1, colIndex];
                    cell.Value = column.Value;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.Font.Bold = true;
                    cell.Style.WrapText = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    colIndex++;
                }

                int rowIndex = 2;
                foreach (DataRow row in dataTable.Rows)
                {
                    colIndex = 1;
                    foreach (var column in fixedColumns)
                    {
                        object value = dataTable.Columns.Contains(column.Key) ? row[column.Key] : null;
                        var cell = worksheet.Cells[rowIndex, colIndex];

                        if (value == DBNull.Value || value == null)
                        {
                            cell.Value = "";
                        }
                        else
                        {
                            try
                            {
                                if (column.Key == "PPHRate")
                                {
                                    string rawValue = value?.ToString()?.Trim() ?? "";
                                    rawValue = rawValue.Replace("%", "").Replace(",", ".");

                                    if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double pphRate))
                                    {
                                        double percentageValue = pphRate / 100.0;
                                        cell.Value = percentageValue;
                                        cell.Style.Numberformat.Format = "0.0%";

                                        // Tô chữ đỏ nếu thấp hơn 90%
                                        if (percentageValue < 0.9)
                                        {
                                            cell.Style.Font.Color.SetColor(Color.Red);
                                        }
                                    }
                                    else
                                    {
                                        cell.Value = value;
                                    }
                                }
                                else if (column.Key == "ScanDate")
                                {
                                    if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
                                    {
                                        cell.Value = dateValue;
                                        cell.Style.Numberformat.Format = "dd/MMM";
                                    }
                                    else
                                    {
                                        cell.Value = value.ToString();
                                    }
                                }
                                else
                                {
                                    cell.Value = value;
                                }
                            }
                            catch
                            {
                                cell.Value = value.ToString();
                            }
                        }

                        // Canh lề ngang
                        if (column.Key == "ModelName" || column.Key == "LargestOutput")
                        {
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        }
                        else
                        {
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        cell.Style.WrapText = true;
                        colIndex++;
                    }
                    rowIndex++;
                }

                // Tô màu xen kẽ và border
                for (int i = 2; i <= dataTable.Rows.Count + 1; i++)
                {
                    var color = (i % 2 == 0) ? Color.AliceBlue : Color.White;
                    for (int j = 1; j <= fixedColumns.Count; j++)
                    {
                        var cell = worksheet.Cells[i, j];
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(color);
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        cell.Style.WrapText = true;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        string key = fixedColumns.Keys.ElementAt(j - 1);
                        if (key == "ModelName" || key == "LargestOutput" || key == "Article")
                        {
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        }
                        else
                        {
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                    }
                }

                // Set chiều rộng cột
                colIndex = 1;
                foreach (var column in fixedColumns)
                {
                    var excelColumn = worksheet.Column(colIndex);
                    if (columnWidths.ContainsKey(column.Key))
                    {
                        excelColumn.Width = columnWidths[column.Key];
                    }
                    else
                    {
                        excelColumn.AutoFit();
                        if (excelColumn.Width < 12) excelColumn.Width = 12;
                    }
                    colIndex++;
                }

                // Tự động filter cho dòng đầu tiên
                worksheet.Cells[1, 1, 1, fixedColumns.Count].AutoFilter = true;

                // Bật wrap toàn vùng dữ liệu (cẩn thận áp dụng cuối để ghi đè)
                worksheet.Cells[1, 1, rowIndex - 1, fixedColumns.Count].Style.WrapText = true;
                worksheet.Cells[1, 1, rowIndex - 1, fixedColumns.Count].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Lưu file
                package.SaveAs(new FileInfo(fileName));
            }
        }
    }
}
