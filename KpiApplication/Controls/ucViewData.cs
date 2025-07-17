using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Forms;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using KpiApplication.Common;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucViewData : DevExpress.XtraEditors.XtraUserControl
    {
        private readonly ProductionData_DAL productionData_DAL = new ProductionData_DAL();

        private ProductionDataService_Model _dataService;

        public ucViewData()
        {
            InitializeComponent();
            AddItemsToBarMenu();
            dgvViewProductionData.RowCellStyle += dgvViewProductionData_RowCellStyle;
        }
        private List<ProductionData_Model> FetchData()
        {
            return productionData_DAL.GetAllData();
        }
        private async void ucViewData_Load(object sender, EventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                data =>
                {
                    LoadDataToGrid(data);
                    ConfigureGridAfterDataBinding();
                },
                "Loading"
            );
        }

        private void ConfigureGridAfterDataBinding()
        {
            // Các cột cho phép chỉnh sửa
            var editableCols = new List<string> { "IEPPH" };

            // Cấu hình lưới sử dụng GridViewHelper
            GridViewHelper.ApplyDefaultFormatting(dgvViewProductionData, editableCols);
            GridViewHelper.EnableWordWrapForGridView(dgvViewProductionData);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvViewProductionData);
            GridViewHelper.EnableCopyFunctionality(dgvViewProductionData);

            // Cấu hình hiển thị và chọn dòng
            dgvViewProductionData.OptionsSelection.MultiSelect = false;
            dgvViewProductionData.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
            gridControl1.UseEmbeddedNavigator = true;

            // Ẩn các cột không cần hiển thị
            GridViewHelper.HideColumns(dgvViewProductionData,
                "ArticleID", "DepartmentCode", "OutputRate", "OutputRateValue",
                "Process", "PPHRateValue", "IsSlides", "TargetOfPC",
                "ProductionID", "IsMerged", "PPHFallsBelowReasons",
                "IsVisible", "MergeGroupID", "LargestOutput", "Target", "OperatorAdjust");

            GridViewHelper.SetColumnCaptions(dgvViewProductionData, new Dictionary<string, string>
    {
        { "IEPPH", "IE PPH" },
        { "TypeName", "Stage" }
    });
        }

        private void LoadDataToGrid(List<ProductionData_Model> data)
        {
            _dataService = new ProductionDataService_Model(data);

            gridControl1.DataSource = _dataService.MergedList;

            PopulateProcessComboBox(_dataService.MergedList.ToList());

            ApplyFilter();

            _dataService.MergedListChanged += (s, e) =>
            {
                gridControl1.RefreshDataSource();
            };
        }

        private void AddItemsToBarMenu()
        {
            BarButtonItem btnReportExport = new BarButtonItem();
            btnReportExport.Caption = "Export IE report file";
            btnReportExport.Id = 1;

            BarButtonItem btnShoesKPIExport = new BarButtonItem();
            btnShoesKPIExport.Caption = "Export Shoes KPI report file";
            btnShoesKPIExport.Id = 2;

            BarButtonItem btnSlidesKPIExport = new BarButtonItem();
            btnSlidesKPIExport.Caption = "Export Slide KPI report file";
            btnSlidesKPIExport.Id = 3;

            BarButtonItem btnOutputPerLineExport = new BarButtonItem();
            btnOutputPerLineExport.Caption = "Export Output Per Line report file";
            btnOutputPerLineExport.Id = 4;

            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnReportExport));
            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnShoesKPIExport));
            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnSlidesKPIExport));
            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnOutputPerLineExport));

            btnReportExport.ItemClick += btnReportExport_ItemClick;
            btnShoesKPIExport.ItemClick += btnShoesKPIExport_ItemClick;
            btnSlidesKPIExport.ItemClick += btnSlidesKPIExport_ItemClick;
            btnOutputPerLineExport.ItemClick += btnOutputPerLineExport_ItemClick;

        }

        private ExportOrientation ShowExportDirectionDialog()
        {
            using (var form = new ExportDirectionForm())
            {
                var result = form.ShowDialog();
                return result == DialogResult.OK ? form.SelectedOrientation : ExportOrientation.Cancel;
            }
        }
        private async void btnReportExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            var direction = ShowExportDirectionDialog();
            if (direction == ExportOrientation.Cancel) return;

            var view = dgvViewProductionData as GridView;
            if (view == null || view.RowCount == 0)
            {
                MessageBoxHelper.ShowWarning("No data available to export.");
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);

            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                MessageBoxHelper.ShowWarning("Insufficient data for export. Please review the data layout.");
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                MessageBoxHelper.ShowError("The 'ScanDate' column was not found in the data.");
                return;
            }

            string folderPath = @"D:\Report";
            string directionLabel = direction == ExportOrientation.Horizontal ? "Horizontal" : "Vertical";
            string fileNamePattern = $"PPH_Report_{directionLabel}_*.xlsx";

            // Search for an existing file
            string existingFile = Directory.Exists(folderPath)
                ? Directory.GetFiles(folderPath, fileNamePattern)
                          .OrderByDescending(f => File.GetCreationTime(f))
                          .FirstOrDefault()
                : null;

            string filePath;

            if (existingFile != null)
            {
                var overwrite = XtraMessageBox.Show(
                    $"A file already exists:\n{Path.GetFileName(existingFile)}\nDo you want to overwrite it?",
                    "File Exists",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                filePath = overwrite == DialogResult.Yes
                    ? existingFile
                    : Path.Combine(folderPath, $"PPH_Report_{directionLabel}_1.xlsx");
            }
            else
            {
                filePath = Path.Combine(folderPath, $"PPH_Report_{directionLabel}.xlsx");
            }
            try
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Failed to create the report folder.", ex);
                return;
            }

            if (!File.Exists(filePath))
            {
                try
                {
                    ExcelExporter.CreateNewExcelFile(filePath);
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError("Failed to create a new Excel file.", ex);
                    return;
                }
            }
            else if (IsFileLocked(filePath))
            {
                MessageBoxHelper.ShowError("The file is currently open or locked by another process. Please close it and try again.");
                return;
            }

            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    async () =>
                    {
                        await Task.Run(() =>
                        {
                            if (direction == ExportOrientation.Horizontal)
                            {
                                ExportByHorizontal(dataTable, filePath);
                            }
                            else
                            {
                                ExportByVertical(dataTable, filePath);
                            }
                        });
                    },
                    "Exporting report data...",
                    null,
                    true);

                MessageBoxHelper.ShowInfo($"Report exported successfully.\nFile saved at:\n{filePath}");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("An error occurred during export.", ex);
            }
        }
        private void ExportDataForProcess(DataTable table, string processName, string sheetName, DateTime scanDate, string filePath)
        {
            var rows = table.AsEnumerable()
                .Where(row =>
                    row.Field<string>("Process") == processName &&
                    DateTime.TryParse(row["ScanDate"].ToString(), out var date) &&
                    date.Date == scanDate);

            if (rows.Any())
            {
                var data = rows.CopyToDataTable();
                ExcelExporter.ExportReportDataToExcel(data, filePath, filePath, sheetName, scanDate.ToString("dd-MMM"));
            }
        }
        private void ExportByHorizontal(DataTable dataTable, string filePath)
        {
            var distinctScanDates = dataTable.AsEnumerable()
                .Select(row => ParseScanDate(row["ScanDate"]))
                .Where(x => x.HasValue)
                .Select(x => x.Value.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            foreach (var scanDate in distinctScanDates)
            {
                ExportDataForProcess(dataTable, "Stitching", "Stitching", scanDate, filePath);

                var assemblingRows = dataTable.AsEnumerable()
                    .Where(row =>
                        ((row.Field<string>("Process") == "Assembling" &&
                          DateTime.TryParse(row["ScanDate"].ToString(), out var date) &&
                          date.Date == scanDate) ||
                         row.Field<string>("LineName") == "ET06"))
                    .OrderBy(row => row.Field<string>("LineName") == "ET06" ? 1 : 0);

                if (assemblingRows.Any())
                {
                    var assemblingData = assemblingRows.CopyToDataTable();
                    ExcelExporter.ExportReportDataToExcel(assemblingData, filePath, filePath, "Assembling", scanDate.ToString("dd-MMM"));
                }
            }
        }

        private void ExportByVertical(DataTable dataTable, string filePath)
        {
            var stitchingRows = dataTable.AsEnumerable()
                .Where(row => row.Field<string>("Process") == "Stitching");

            if (stitchingRows.Any())
            {
                var stitchingData = stitchingRows.CopyToDataTable();
                ExcelExporter.ExportVerticalReport(stitchingData, filePath, "Stitching");
            }

            var assemblingRows = dataTable.AsEnumerable()
                .Where(row => row.Field<string>("Process") == "Assembling" || row.Field<string>("LineName") == "ET06");

            if (assemblingRows.Any())
            {
                var assemblingData = assemblingRows.CopyToDataTable();
                ExcelExporter.ExportVerticalReport(assemblingData, filePath, "Assembling");
            }
        }

        private DateTime? ParseScanDate(object val)
        {
            if (val == null || val == DBNull.Value) return null;

            if (val is DateTime dt) return dt;

            if (DateTime.TryParseExact(val.ToString(), "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact;

            if (DateTime.TryParse(val.ToString(), out var parsed))
                return parsed;

            return null;
        }


        private DataTable GetFilteredDataTableFromGridView(GridView view)
        {
            DataTable dt = new DataTable();

            // Thêm cột từ GridView, kể cả unbound columns
            foreach (DevExpress.XtraGrid.Columns.GridColumn column in view.Columns)
            {
                // Dùng typeof(object) để linh hoạt dữ liệu kiểu string, int, DateTime...
                if (!dt.Columns.Contains(column.FieldName))
                    dt.Columns.Add(column.FieldName, typeof(object));
            }
            foreach (DataColumn col in dt.Columns)
            {
                Console.WriteLine(col.ColumnName);
            }

            // Thêm từng dòng dữ liệu hiển thị trên UI
            for (int i = 0; i < view.RowCount; i++)
            {
                DataRow row = dt.NewRow();

                foreach (DevExpress.XtraGrid.Columns.GridColumn column in view.Columns)
                {
                    object value = view.GetRowCellValue(i, column);
                    row[column.FieldName] = value ?? DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
        }
        private async void btnOutputPerLineExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            var view = dgvViewProductionData as GridView;
            if (view == null || view.RowCount == 0)
            {
                MessageBoxHelper.ShowWarning("No data available to export.");
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);
            DataTable exportTable;

            try
            {
                exportTable = BuildOutputPerLineSummary(dataTable);
                if (exportTable == null || exportTable.Rows.Count == 0)
                {
                    MessageBoxHelper.ShowWarning("No valid data found to export.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Error while building export data", ex);
                return;
            }

            string folderPath = @"D:\Report";
            string filePath = Path.Combine(folderPath, $"OutputPerLine_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            try
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Failed to create output folder", ex);
                return;
            }

            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    async () =>
                    {
                        await Task.Run(() =>
                        {
                            ExcelExporter.ExportSimpleDataTableToExcel(exportTable, filePath, "OutputPerLine");
                        });
                    },
                    "Exporting..."
                );

                MessageBoxHelper.ShowInfo("Output Per Line exported successfully!");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Error exporting Output Per Line", ex);
            }
        }


        private DataTable BuildOutputPerLineSummary(DataTable sourceTable)
        {
            if (!sourceTable.Columns.Contains("LineName") || !sourceTable.Columns.Contains("Quantity"))
                throw new InvalidOperationException("Missing required columns: LineName or Quantity.");

            var groupedData = sourceTable.AsEnumerable()
                .Where(row => row["LineName"] != DBNull.Value && row["Quantity"] != DBNull.Value)
                .GroupBy(row => row.Field<string>("LineName"))
                .Select(g => new
                {
                    LineName = g.Key,
                    TotalQuantity = g.Sum(row =>
                    {
                        object val = row["Quantity"];
                        if (val == null || val == DBNull.Value) return 0;
                        return Convert.ToDouble(val);
                    }),
                    TotalDays = g.Select(r =>
                    {
                        if (DateTime.TryParse(r["ScanDate"]?.ToString(), out DateTime dt))
                            return dt.Date;
                        return (DateTime?)null;
                    }).Where(d => d.HasValue).Select(d => d.Value).Distinct().Count()
                })
                .OrderBy(g => g.LineName)
                .ToList();

            if (groupedData.Count == 0)
                return null;

            DataTable exportTable = new DataTable();
            exportTable.Columns.Add("LineName", typeof(string));
            exportTable.Columns.Add("TotalQuantity", typeof(double));
            exportTable.Columns.Add("TotalDays", typeof(int));

            foreach (var item in groupedData)
            {
                exportTable.Rows.Add(item.LineName, item.TotalQuantity, item.TotalDays);
            }

            return exportTable;
        }
        private async void btnShoesKPIExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            await ExportKPIDataAsync("Shoes", new[] { "Cutting", "Stitching", "Assembling", "Stock Fitting" }, false);
        }

        private async void btnSlidesKPIExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            await ExportKPIDataAsync("Slide", new[] { "Cutting", "Stitching", "Assembling", "Stock Fitting" }, true);
        }

        private async Task ExportKPIDataAsync(string reportType, string[] processFilter, bool includeIsSlides)
        {
            var view = dgvViewProductionData as GridView;
            if (view == null || view.RowCount == 0)
            {
                MessageBoxHelper.ShowWarning("No data available to export.");
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);
            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                MessageBoxHelper.ShowWarning("Unable to export data, please try reordering the data.");
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                MessageBoxHelper.ShowError("ScanDate column not found in the data.");
                return;
            }

            DateTime maxDate;
            try
            {
                maxDate = dataTable.AsEnumerable()
                    .Where(row => row["ScanDate"] != DBNull.Value &&
                                  !string.IsNullOrWhiteSpace(row["ScanDate"].ToString()) &&
                                  DateTime.TryParse(row["ScanDate"].ToString(), out _))
                    .Select(row => Convert.ToDateTime(row["ScanDate"]))
                    .Max();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("An error occurred while retrieving the date", ex);
                return;
            }

            var filtered = dataTable.AsEnumerable()
                .Where(row => processFilter.Contains(row.Field<string>("Process")) &&
                              row.Field<string>("DepartmentCode") != "4001SX" &&
                              IsSlideRow(row) == includeIsSlides);

            if (!filtered.Any())
            {
                MessageBoxHelper.ShowWarning("No matching data available to export.");
                return;
            }

            DataTable filteredTable = filtered.CopyToDataTable();

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                string formattedDate = maxDate.ToString("MM-dd");
                string folderPath = @"D:\Report";

                if (!Directory.Exists(folderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError("An error occurred while creating the report folder", ex);
                        return;
                    }
                }

                saveFileDialog.InitialDirectory = folderPath;
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = $"KPI Report-{reportType}-{formattedDate}.xlsx";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = GetUniqueFileName(saveFileDialog.FileName);

                    try
                    {
                        await AsyncLoaderHelper.LoadDataWithSplashAsync(
                            this,
                            async () =>
                            {
                                await Task.Run(() =>
                                {
                                    ExcelExporter.ExportKPIDataToExcel(filteredTable, fileName);
                                });
                            },
                            "Exporting..."
                        );

                        MessageBoxHelper.ShowInfo("Report data exported successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError("An error occurred while exporting report data", ex);
                    }
                }
            }
        }
        private bool IsSlideRow(DataRow row)
        {
            return row.Table.Columns.Contains("IsSlides") &&
                   row["IsSlides"] != DBNull.Value &&
                   Convert.ToBoolean(row["IsSlides"]);
        }
        private string GetUniqueFileName(string fileName)
        {
            int count = 1;
            string fileExtension = System.IO.Path.GetExtension(fileName);
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string directory = System.IO.Path.GetDirectoryName(fileName);
            string newFileName = fileName;

            while (System.IO.File.Exists(newFileName))
            {
                string tempFileName = $"{fileNameWithoutExtension} ({count++}){fileExtension}";
                newFileName = System.IO.Path.Combine(directory, tempFileName);
            }

            return newFileName;
        }

        private void PopulateProcessComboBox(List<ProductionData_Model> productionDataList)
        {
            try
            {
                if (productionDataList == null || productionDataList.Count == 0)
                {
                    cbxProcess.Properties.Items.Clear();
                    cbxProcess.Properties.Items.Add("-- All Process --");
                    cbxProcess.SelectedIndex = 0;
                    return;
                }

                var processList = productionDataList
                    .Select(p => p.Process)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();

                // Insert "All Process" at the top
                processList.Insert(0, "-- All Process --");

                // Update ComboBox items
                cbxProcess.Properties.Items.Clear();
                cbxProcess.Properties.Items.AddRange(processList);

                // Ensure SelectedIndex can be set
                if (cbxProcess.Properties.Items.Count > 0)
                    cbxProcess.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("An error occurred while loading the Process list", ex);
            }
        }

        private void ApplyFilter()
        {
            dgvViewProductionData.BeginSort();
            try
            {
                dgvViewProductionData.ClearSorting();

                // 1️⃣ ScanDate giảm dần
                dgvViewProductionData.Columns["ScanDate"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;

                // Bắt sự kiện CustomColumnSort
                dgvViewProductionData.CustomColumnSort += (sender, e) =>
                {
                    if (e.Column.FieldName == "Factory")
                    {
                        e.Handled = true;
                        int GetOrder(string factory)
                        {
                            if (factory == "Apache") return 1;
                            else if (factory == "Mega") return 2;
                            else if (factory == "Tera") return 3;
                            else return 4;
                        }
                        int x = GetOrder(e.Value1?.ToString() ?? "");
                        int y = GetOrder(e.Value2?.ToString() ?? "");
                        e.Result = x.CompareTo(y);
                    }
                    else if (e.Column.FieldName == "Process")
                    {
                        e.Handled = true;
                        int GetOrder(string process)
                        {
                            if (process == "Stitching") return 1;
                            else if (process == "Assembling") return 2;
                            else if (process == "Stock Fitting") return 3;
                            else return 4;
                        }
                        int x = GetOrder(e.Value1?.ToString() ?? "");
                        int y = GetOrder(e.Value2?.ToString() ?? "");
                        e.Result = x.CompareTo(y);
                    }
                    else if (e.Column.FieldName == "LineName")
                    {
                        e.Handled = true;
                        int ExtractNumber(string line)
                        {
                            if (string.IsNullOrEmpty(line)) return 0;
                            var number = new string(line.SkipWhile(c => !char.IsDigit(c)).ToArray());
                            return int.TryParse(number, out int n) ? n : 0;
                        }
                        int x = ExtractNumber(e.Value1?.ToString());
                        int y = ExtractNumber(e.Value2?.ToString());
                        e.Result = x.CompareTo(y);
                    }
                };

                // 2️⃣ Factory theo Apache, Mega, Tera
                dgvViewProductionData.Columns["Factory"].SortMode = DevExpress.XtraGrid.ColumnSortMode.Custom;
                dgvViewProductionData.Columns["Factory"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;

                // 3️⃣ Process theo S, L, T
                dgvViewProductionData.Columns["Process"].SortMode = DevExpress.XtraGrid.ColumnSortMode.Custom;
                dgvViewProductionData.Columns["Process"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;

                // 4️⃣ Plant tăng dần
                dgvViewProductionData.Columns["Plant"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;

                // 5️⃣ LineName tăng dần (theo số học)
                dgvViewProductionData.Columns["LineName"].SortMode = DevExpress.XtraGrid.ColumnSortMode.Custom;
                dgvViewProductionData.Columns["LineName"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
            }
            finally
            {
                dgvViewProductionData.EndSort();
            }
        }

        private void dgvViewProductionData_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null || e.RowHandle < 0)
                return;

            var row = view.GetRow(e.RowHandle) as ProductionData_Model;

            // Tô nền vàng nếu dòng đã gộp
            if (row?.IsMerged == true)
            {
                e.Appearance.BackColor = Color.LightYellow;
            }

            if (e.Column.FieldName == "PPHRate")
            {
                var pphRate = row?.PPHRateValue;

                if (pphRate.HasValue && pphRate.Value < 0.9)
                {
                    e.Appearance.ForeColor = Color.Red;
                }
            }
        }

        private void cbxProcess_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilterFromComboBox(cbxProcess, dgvViewProductionData);
        }

        private void ApplyFilterFromComboBox(DevExpress.XtraEditors.ComboBoxEdit comboBox, DevExpress.XtraGrid.Views.Grid.GridView gridView)
        {
            if (comboBox.EditValue != null)
            {
                string selectedValue = comboBox.EditValue.ToString();

                if (selectedValue == "-- All Process --")
                {
                    gridView.ActiveFilter.Clear();
                }
                else
                {
                    gridView.ActiveFilterString = $"[Process] = '{selectedValue}'";
                }
            }
        }
        private async void btnRefresh_ItemClick(object sender, ItemClickEventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                data =>
                {
                    LoadDataToGrid(data);
                    ConfigureGridAfterDataBinding();
                },
                "Loading"
            );
        }
    }
}
