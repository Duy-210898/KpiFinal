using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        private ProductionDataListManager _listManager;

        public ucViewData()
        {
            InitializeComponent();
            AddItemsToBarMenu();
            dgvViewProductionData.RowCellStyle += dgvViewProductionData_RowCellStyle;
        }
        private ProductionDataListManager FetchData()
        {
            var data = productionData_DAL.GetAllData();
            return new ProductionDataListManager(data);
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


        private void LoadDataToGrid(ProductionDataListManager manager)
        {
            _listManager = manager;
            gridControl1.DataSource = _listManager.MergedList;
            PopulateProcessComboBox(_listManager.MergedList.ToList());
            ApplyFilter();
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

            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnReportExport));
            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnShoesKPIExport));
            btnExport.LinksPersistInfo.Add(new LinkPersistInfo(btnSlidesKPIExport));

            btnReportExport.ItemClick += btnReportExport_ItemClick;
            btnShoesKPIExport.ItemClick += btnShoesKPIExport_ItemClick;
            btnSlidesKPIExport.ItemClick += btnSlidesKPIExport_ItemClick;
            
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

        private async void btnReportExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            var view = dgvViewProductionData as GridView;
            if (view == null || view.RowCount == 0)
            {
                XtraMessageBox.Show("No data available to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);

            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                XtraMessageBox.Show("Unable to export data, please try reordering the data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                XtraMessageBox.Show("ScanDate column not found in the data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string folderPath = @"D:\Report";
            string filePath = Path.Combine(folderPath, "PPH_Report.xlsx");

            try
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"An error occurred while creating the report folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    XtraMessageBox.Show($"An error occurred while creating the new file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (IsFileLocked(filePath))
            {
                XtraMessageBox.Show("The file is currently open or in use by another application. Please close the file and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Sử dụng AsyncLoaderHelper để xử lý async và show progress tự động
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var distinctScanDates = dataTable.AsEnumerable()
                            .Select(row =>
                            {
                                var val = row["ScanDate"];
                                if (val == null || val == DBNull.Value) return (DateTime?)null;

                                if (val is DateTime dt) return dt;

                                if (DateTime.TryParseExact(val.ToString(), "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exact))
                                    return exact;

                                if (DateTime.TryParse(val.ToString(), out DateTime parsed))
                                    return parsed;

                                return (DateTime?)null;
                            })
                            .Where(x => x.HasValue)
                            .Select(x => x.Value.Date)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        foreach (var scanDate in distinctScanDates)
                        {
                            // Gọi ExportDataForProcess trên background
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
                    });
                },
                "Exporting...",
                null,
                true);

                XtraMessageBox.Show("Report data exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"An error occurred while exporting report data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            Debug.WriteLine("Danh sách tên cột trong DataTable:");
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
                XtraMessageBox.Show("No data available to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);
            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                XtraMessageBox.Show("Unable to export data, please try reordering the data.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                XtraMessageBox.Show("ScanDate column not found in the data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                XtraMessageBox.Show($"An error occurred while retrieving the date: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var filtered = dataTable.AsEnumerable()
                .Where(row => processFilter.Contains(row.Field<string>("Process")) &&
                              row.Field<string>("DepartmentCode") != "4001SX" &&
                              IsSlideRow(row) == includeIsSlides);

            if (!filtered.Any())
            {
                XtraMessageBox.Show("No matching data available to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        XtraMessageBox.Show($"An error occurred while creating the report folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        XtraMessageBox.Show("Report data exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show($"An error occurred while exporting report data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void PopulateProcessComboBox(List<ProductionData> productionDataList)
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

                // Chèn "All Process" lên đầu
                processList.Insert(0, "-- All Process --");

                // Cập nhật ComboBox
                cbxProcess.Properties.Items.Clear();
                cbxProcess.Properties.Items.AddRange(processList);

                // Đảm bảo có thể set SelectedIndex
                if (cbxProcess.Properties.Items.Count > 0)
                    cbxProcess.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Đã xảy ra lỗi khi tải danh sách Process:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ConfigureGridAfterDataBinding()
        {
            // Cấu hình hiển thị & chỉnh sửa
            var editableCols = new List<string> { "IEPPH" };
            GridViewHelper.ApplyDefaultFormatting(dgvViewProductionData, editableCols);
            GridViewHelper.EnableWordWrapForGridView(dgvViewProductionData);
            GridViewHelper.ApplyRowStyleAlternateColors(dgvViewProductionData, Color.AliceBlue, Color.White);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvViewProductionData);

            // Cấu hình chọn dòng
            dgvViewProductionData.OptionsSelection.MultiSelect = false;
            dgvViewProductionData.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;
            gridControl1.UseEmbeddedNavigator = true;

            // Ẩn các cột không cần thiết
            GridViewHelper.HideColumns(dgvViewProductionData,
                "ArticleID", "DepartmentCode", "OutputRate",
                "Process", "PPHRateValue", "IsSlides",
                "ProductionID", "Rate", "IsMerged", "PPHFallsBelowReasons",
                "IsVisible", "MergeGroupID", "LargestOutput");

            // Đặt tiêu đề cột
            GridViewHelper.SetColumnCaptions(dgvViewProductionData, new Dictionary<string, string>
    {
        { "IEPPH", "IE PPH" },
        { "TypeName", "Stage" }
    });

            // Cho phép copy giá trị ô
            GridViewHelper.EnableCopyFunctionality(dgvViewProductionData);
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
            var row = view.GetRow(e.RowHandle) as ProductionData;

            if (row?.IsMerged == true)
            {
                e.Appearance.BackColor = Color.LightYellow;
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
    }
}
