using KpiApplication.Forms;
using KpiApplication.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KpiApplication.Excel
{
    public class ExcelImporter
    {
        public static List<TCTRaw> ReadTCTItemsFromExcel(string filePath, out List<string> errors)
        {
            var result = new List<TCTRaw>(); 
            errors = new List<string>();

            var modelNamesSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 4; row <= rowCount; row++)
                {
                    string modelName = worksheet.Cells[row, 1].Text?.Trim();
                    string type = worksheet.Cells[row, 2].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(modelName))
                        continue;

                    if (modelNamesSeen.Contains(modelName))
                        continue; 

                    modelNamesSeen.Add(modelName);

                    var item = new TCTRaw  
                    {
                        ModelName = modelName,
                        Type = type,
                        Cutting = TryParseDoubleOrLog(worksheet.Cells[row, 3].Text, row, "Cutting", errors),
                        Stitching = TryParseDoubleOrLog(worksheet.Cells[row, 4].Text, row, "Stitching", errors),
                        Assembly = TryParseDoubleOrLog(worksheet.Cells[row, 5].Text, row, "Assembly", errors),
                        Stockfitting = TryParseDoubleOrLog(worksheet.Cells[row, 6].Text, row, "Stockfitting", errors),
                    };

                    result.Add(item);
                }
            }

            return result;
        }

        private static double? TryParseDoubleOrLog(string text, int row, string columnName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            errors.Add($"Dòng {row}, cột {columnName}: không thể chuyển '{text}' thành số.");
            return null;
        }

        public static List<WeeklyPlanData_Model> ImportWeeklyPlanFromExcel(string filePath, string sheetName)
        {
            var list = new List<WeeklyPlanData_Model>();

            // Lấy tên file không có phần mở rộng, ví dụ: "KẾ HOẠCH SẢN XUẤT TUẦN 4 THÁNG 5"
            string fileName = Path.GetFileNameWithoutExtension(filePath).ToUpper();

            int weekNumber = 0;
            int monthNumber = 0;
            int yearNumber = DateTime.Now.Year;

            // Tìm tuần
            var weekMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"TUẦN\s*(\d+)");
            if (weekMatch.Success && int.TryParse(weekMatch.Groups[1].Value, out int w))
                weekNumber = w;

            // Tìm tháng
            var monthMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"THÁNG\s*(\d+)");
            if (monthMatch.Success && int.TryParse(monthMatch.Groups[1].Value, out int m))
                monthNumber = m;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                    throw new Exception($"Không tìm thấy sheet '{sheetName}'.");
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var data = new WeeklyPlanData_Model
                    {
                        ModelName = worksheet.Cells[row, 1].Text.Trim(),
                        ArticleName = worksheet.Cells[row, 2].Text.Trim(),
                        Stitching = worksheet.Cells[row, 3].Text.Trim(),
                        Assembling = worksheet.Cells[row, 4].Text.Trim(),
                        StockFitting = worksheet.Cells[row, 5].Text.Trim(),
                        BPFC = worksheet.Cells[row, 6].Text.Trim(),

                        Week = weekNumber,
                        Month = monthNumber,
                        Year = yearNumber,
                    };

                    list.Add(data);
                }
            }

            return list;
        }
        public static List<ExcelRowData_Model> LoadExcelData(string filePath)
        {
            var dataList = new List<ExcelRowData_Model>();

            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                List<string> sheetNames = GetSheetNames(filePath);
                Debug.WriteLine($"Found sheets: {string.Join(", ", sheetNames)}");

                bool isMegaFile = filePath.IndexOf("MEGA", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isTeraFile = filePath.IndexOf("TERA", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isMegaFile || isTeraFile)
                {
                    string selectedSheet = ShowSheetSelectionDialog(sheetNames);
                    Debug.WriteLine($"Selected sheet: {selectedSheet}");
                    if (selectedSheet == null) return dataList;

                    DateTime workingDate = NormalizeSheetNameAsDate(selectedSheet);
                    ProcessWorksheet(package, selectedSheet, dataList, isMegaFile, isTeraFile, workingDate);
                }
                else
                {
                    int[] sheetIndexes = { 5, 6, 7, 8, 9, 10, 11 };
                    Debug.WriteLine($"Total worksheets count: {package.Workbook.Worksheets.Count}");
                    foreach (int index in sheetIndexes)
                    {
                        if (index <= package.Workbook.Worksheets.Count)
                        {
                            string sheetName = package.Workbook.Worksheets[index - 1].Name;
                            var worksheet = package.Workbook.Worksheets[sheetName];
                            string workingDate = worksheet.Cells["L2"].Text.Trim();
                            Debug.WriteLine($"Sheet {sheetName} L2 = {workingDate}");
                            string dateOnlyStr = ExtractDateFromText(workingDate);
                            if (DateTime.TryParse(dateOnlyStr, out DateTime dateOnly))
                            {
                                ProcessWorksheet(package, sheetName, dataList, isMegaFile, isTeraFile, dateOnly);
                            }
                            else
                            {
                                Debug.WriteLine($"Cannot parse date from sheet {sheetName} L2");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Sheet index {index} exceeds worksheet count");
                        }
                    }
                }
            }

            Debug.WriteLine($"Total rows loaded: {dataList.Count}");
            return dataList;
        }
        private static DateTime NormalizeSheetNameAsDate(string sheetName)
        {
            DateTime date;

            if (DateTime.TryParseExact(sheetName, "d.M.yyyy", null, System.Globalization.DateTimeStyles.None, out date) ||
                DateTime.TryParseExact(sheetName, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }

            if (DateTime.TryParseExact(sheetName, "M.d", null, System.Globalization.DateTimeStyles.None, out date) ||
                DateTime.TryParseExact(sheetName, "MM.dd", null, System.Globalization.DateTimeStyles.None, out date))
            {
                var today = DateTime.Today;
                var parsed = new DateTime(today.Year, date.Month, date.Day);

                if (parsed > today)
                    parsed = parsed.AddYears(-1);

                return parsed;
            }

            // fallback nếu không parse được
            return DateTime.MinValue;
        }

        private static string ExtractDateFromText(string input)
        {
            var parts = input.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                string datePart = parts[1];

                string[] dateFormats = {
            "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "MM-dd-yyyy",
            "dd.MM.yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "yyyy.MM.dd",
            "d/M/yyyy", "M/d/yyyy", "d-M-yyyy", "M-d-yyyy"
        };

                if (DateTime.TryParseExact(datePart, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return parsedDate.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);
                }

                if (DateTime.TryParse(datePart, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
                {
                    return parsedDate.ToString("dd-MMM-yy", CultureInfo.CurrentCulture);
                }
            }

            return string.Empty;
        }

        private static void ProcessWorksheet(ExcelPackage package, string sheetName, List<ExcelRowData_Model> dataList, bool isMegaFile, bool isTeraFile, DateTime workingDate)
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet?.Dimension == null) return;

            int startRow = isMegaFile || isTeraFile ? 4 : 5;
            for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
            {
                string colBValue, colJValue, colCValue;
                if (isMegaFile)
                {
                    colBValue = worksheet.Cells[row, 3].Text.Trim();
                    colJValue = worksheet.Cells[row, 4].Text.Trim();
                    colCValue = worksheet.Cells[row, 5].Text.Trim();
                }
                else if (isTeraFile)
                {
                    colBValue = worksheet.Cells[row, 3].Text.Trim();
                    colJValue = worksheet.Cells[row, 5].Text.Trim();
                    colCValue = worksheet.Cells[row, 6].Text.Trim();
                }
                else
                {
                    colBValue = worksheet.Cells[row, 2].Text.Trim();
                    colJValue = worksheet.Cells[row, 10].Text.Trim();
                    colCValue = null;
                }

                if (string.IsNullOrEmpty(colBValue) || colBValue.Equals("Total", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!colBValue.StartsWith("VPX", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessRow(colBValue, colJValue, colCValue, dataList, isMegaFile, isTeraFile, workingDate);
                }
            }
        }

        private static List<string> GetSheetNames(string filePath)
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                return package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
            }
        }

        private static string ShowSheetSelectionDialog(List<string> sheetNames)
        {
            using (SheetSelectionForm form = new SheetSelectionForm(sheetNames))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.SelectedSheet;
                }
            }
            return null;
        }

        private static void ProcessRow(string colBValue, string colJValue, string colCValue, List<ExcelRowData_Model> dataList, bool isMegaFile, bool isTeraFile, DateTime workingDate)
        {
            colBValue = NormalizeBValue(colBValue);

            if (colBValue.StartsWith("AS") || colBValue.StartsWith("BS") || colBValue.StartsWith("CS") ||
                colBValue.StartsWith("DS") || colBValue.StartsWith("ES") || colBValue.StartsWith("FS") ||
                colBValue.StartsWith("GS") || colBValue.StartsWith("HS") || colBValue.StartsWith("IS") ||
                colBValue.StartsWith("JS"))
            {
                colBValue = FormatMegaOrTeraValue(colBValue, isMegaFile, isTeraFile);
            }

            int? parsedJ = int.TryParse(colJValue, out int j) ? j : (int?)null;
            float? parsedC = float.TryParse(colCValue, out float c) ? c : (float?)null;

            if (colBValue.Contains("AL1+ PAL2"))
            {
                dataList.Add(new ExcelRowData_Model { LineName = "AL01", TotalWorker = parsedJ, WorkingHours = parsedC, WorkingDate = workingDate });
                dataList.Add(new ExcelRowData_Model { LineName = "AL02", TotalWorker = null, WorkingHours = parsedC, WorkingDate = workingDate });
            }
            else if (colBValue.Contains("AL3+ PAL5"))
            {
                dataList.Add(new ExcelRowData_Model { LineName = "AL03", TotalWorker = null, WorkingHours = parsedC, WorkingDate = workingDate });
                dataList.Add(new ExcelRowData_Model { LineName = "AL05", TotalWorker = parsedJ, WorkingHours = parsedC, WorkingDate = workingDate });
            }
            else
            {
                colBValue = NormalizeKey(colBValue);
                dataList.Add(new ExcelRowData_Model { LineName = colBValue, TotalWorker = parsedJ, WorkingHours = parsedC, WorkingDate = workingDate });
            }
        }

        private static string FormatMegaOrTeraValue(string colBValue, bool isMegaFile, bool isTeraFile)
        {
            string prefix = isMegaFile ? "MEGA - " : isTeraFile ? "TERA - " : string.Empty;
            return colBValue.Length == 3
                ? prefix + colBValue.Substring(0, 2) + "0" + colBValue[2]
                : prefix + colBValue;
        }

        private static string NormalizeBValue(string colBValue)
        {
            string[] prefixes = { "PAS", "PCS", "PDS", "POS", "PAL", "PCL", "PDL", "PEL", "PFL", "PIL", "POL" };
            foreach (var prefix in prefixes)
            {
                if (colBValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string numberPart = colBValue.Substring(prefix.Length).PadLeft(2, '0');
                    return prefix.Substring(1, 2) + numberPart;
                }
            }
            return colBValue;
        }

        private static string NormalizeKey(string key)
        {
            if (key.StartsWith("PAC", StringComparison.OrdinalIgnoreCase)) return "APC";
            if (key.StartsWith("PCC", StringComparison.OrdinalIgnoreCase)) return "CPC";
            if (key.StartsWith("PDC", StringComparison.OrdinalIgnoreCase)) return "DPC";
            if (key.StartsWith("POC", StringComparison.OrdinalIgnoreCase)) return "OPC";
            if (key.StartsWith("PET6", StringComparison.OrdinalIgnoreCase)) return "ET06";
            return key;
        }
    }
}