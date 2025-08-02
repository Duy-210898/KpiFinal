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
    public partial class ucViewData : XtraUserControl, ISupportLoadAsync
    {
        private readonly ProductionData_DAL productionData_DAL = new ProductionData_DAL();

        private ProductionDataService_Model _dataService;

        public ucViewData()
        {
            InitializeComponent();
            ApplyLocalizedText();
            AddItemsToBarMenu();
            dgvViewProductionData.RowCellStyle += dgvViewProductionData_RowCellStyle;
        }
        private void ApplyLocalizedText()
        {
            btnExport.Caption = Lang.Export;
            layoutControlItem1.Text = Lang.Process;
        }

        private List<ProductionData_Model> FetchData()
        {
            return productionData_DAL.GetAllData();
        }
        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var data = await Task.Run(() => FetchData());

                LoadDataToGrid(data);
                ConfigureGridAfterDataBinding();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataFailed, ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
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

            GridViewHelper.SetColumnCaptions(dgvViewProductionData, GetCaptions());
        }
        private Dictionary<string, string> GetCaptions()
        {
            return new Dictionary<string, string>
            {
                ["ScanDate"] = Lang.ScanDate,
                ["Factory"] = Lang.Factory,
                ["Plant"] = Lang.Plant,
                ["LineName"] = Lang.LineName,
                ["ModelName"] = Lang.ModelName,
                ["Quantity"] = Lang.Quantity,
                ["TargetOutputPC"] = Lang.TargetOutput,
                ["TotalWorker"] = Lang.TotalWorker,
                ["WorkingTime"] = Lang.WorkingTime,
                ["TotalWorkingHours"] = Lang.TotalWorkingHours,
                ["IEPPH"] = "IE PPH",
                ["Stage"] = Lang.Type, 
                ["PPHRate"] = Lang.PPHRate,
                ["ActualPPH"] = Lang.ActutalPPH
            };
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
            var btnReportExport = new BarButtonItem
            {
                Caption = Lang.ExportIEReportFile,
                Id = 1,
                ImageOptions = { Image = Properties.Resources.export }
            };

            var btnShoesKPIExport = new BarButtonItem
            {
                Caption = Lang.ExportShoesKPIReportFile,
                Id = 2,
                ImageOptions = { Image = Properties.Resources.export }
            };

            var btnSlidesKPIExport = new BarButtonItem
            {
                Caption = Lang.ExportSlidesKPIReportFile,
                Id = 3,
                ImageOptions = { Image = Properties.Resources.export }
            };

            var btnOutputPerLineExport = new BarButtonItem
            {
                Caption = Lang.ExportOutputPerLineReportFile,
                Id = 4,
                ImageOptions = { Image = Properties.Resources.export }
            };

            btnExport.LinksPersistInfo.AddRange(new[]
            {
        new LinkPersistInfo(btnReportExport),
        new LinkPersistInfo(btnShoesKPIExport),
        new LinkPersistInfo(btnSlidesKPIExport),
        new LinkPersistInfo(btnOutputPerLineExport)
    });

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
                MessageBoxHelper.ShowWarning(Lang.NoDataToExport);
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);

            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                MessageBoxHelper.ShowWarning(Lang.ExportInsufficientData);
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                MessageBoxHelper.ShowError(Lang.ScanDateNotFound);
                return;
            }

            string folderPath = @"D:\Report";
            string directionLabel = direction == ExportOrientation.Horizontal ? "Horizontal" : "Vertical";
            string fileNamePattern = $"PPH_Report_{directionLabel}_*.xlsx";

            string existingFile = Directory.Exists(folderPath)
                ? Directory.GetFiles(folderPath, fileNamePattern)
                          .OrderByDescending(f => File.GetCreationTime(f))
                          .FirstOrDefault()
                : null;

            string filePath;
            if (existingFile != null)
            {
                bool overwrite = MessageBoxHelper.ConfirmFileOverwrite(Path.GetFileName(existingFile));
                filePath = overwrite
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
                MessageBoxHelper.ShowError(Lang.FailedToCreateReportFolder, ex);
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
                    MessageBoxHelper.ShowError(Lang.FailedToCreateExcelFile, ex);
                    return;
                }
            }
            else if (IsFileLocked(filePath))
            {
                MessageBoxHelper.ShowError(Lang.FileLockedOrOpened);
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
                                ExportByHorizontal(dataTable, filePath);
                            else
                                ExportByVertical(dataTable, filePath);
                        });
                    },
                    Lang.Export,
                    null,
                    true);

                MessageBoxHelper.ShowInfo(string.Format(Lang.ReportExportSuccess, filePath));
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ExportError, ex);
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
                ExportDataForProcess(dataTable, Lang.Stitching, "Stitching", scanDate, filePath);

                var assemblingRows = dataTable.AsEnumerable()
                    .Where(row =>
                        ((row.Field<string>("Process") == Lang.Assembly &&
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
                .Where(row => row.Field<string>("Process") == Lang.Stitching);

            if (stitchingRows.Any())
            {
                var stitchingData = stitchingRows.CopyToDataTable();
                ExcelExporter.ExportVerticalReport(stitchingData, filePath, "Stitching");
            }

            var assemblingRows = dataTable.AsEnumerable()
                .Where(row => row.Field<string>("Process") == Lang.Assembly || row.Field<string>("LineName") == "ET06");

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
                MessageBoxHelper.ShowWarning(Lang.NoDataToExport);
                return;
            }

            DataTable rawTable = GetFilteredDataTableFromGridView(view);
            DataTable exportTable;

            try
            {
                exportTable = BuildOutputPerLineSummary(rawTable);
                if (exportTable == null || exportTable.Rows.Count == 0)
                {
                    MessageBoxHelper.ShowWarning(Lang.NoValidDataToExport);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.FailedToBuildExportData, ex);
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
                MessageBoxHelper.ShowError(Lang.FailedToCreateReportFolder, ex);
                return;
            }

            if (File.Exists(filePath))
            {
                var result = XtraMessageBox.Show(
                    string.Format(Lang.FileAlreadyExists, filePath),
                    Lang.Confirm,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes) return;
            }

            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    async () =>
                    {
                        await Task.Run(() =>
                        {
                            ExcelExporter.ExportSimpleDataTableToExcel(exportTable, filePath, Lang.OutputPerLineSheetName);
                        });
                    },
                    Lang.Export
                );

                MessageBoxHelper.ShowInfo(string.Format(Lang.ExportSuccess, filePath));
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ExportError, ex);
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
            await ExportKPIDataAsync("Shoes", new[] { Lang.Cutting, Lang.Stitching, Lang.Assembly, Lang.StockFitting }, false);
        }

        private async void btnSlidesKPIExport_ItemClick(object sender, ItemClickEventArgs e)
        {
            await ExportKPIDataAsync("Slide", new[] { Lang.Cutting, Lang.Stitching, Lang.Assembly, Lang.StockFitting }, true);
        }

        private async Task ExportKPIDataAsync(string reportType, string[] processFilter, bool includeIsSlides)
        {
            var view = dgvViewProductionData as GridView;
            if (view == null || view.RowCount == 0)
            {
                MessageBoxHelper.ShowWarning(Lang.NoDataToExport); // ❗ Thêm key này
                return;
            }

            DataTable dataTable = GetFilteredDataTableFromGridView(view);
            if (dataTable == null || dataTable.Columns.Count <= 2)
            {
                MessageBoxHelper.ShowWarning(Lang.Export_UnableToExportTryReorder); // ❗ Thêm key này
                return;
            }

            if (!dataTable.Columns.Contains("ScanDate"))
            {
                MessageBoxHelper.ShowError(Lang.Export_ScanDateColumnMissing); // ❗ Thêm key này
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
                MessageBoxHelper.ShowError(Lang.Export_ErrorGettingDate, ex); // ❗ Thêm key này
                return;
            }

            var filtered = dataTable.AsEnumerable()
                .Where(row => processFilter.Contains(row.Field<string>("Process")) &&
                              row.Field<string>("DepartmentCode") != "4001SX" &&
                              IsSlideRow(row) == includeIsSlides);

            if (!filtered.Any())
            {
                MessageBoxHelper.ShowWarning(Lang.Export_NoMatchingData); // ❗ Thêm key này
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
                        MessageBoxHelper.ShowError(Lang.Export_ErrorCreatingFolder, ex); // ❗ Thêm key này
                        return;
                    }
                }

                saveFileDialog.InitialDirectory = folderPath;
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = $"{Lang.Export_FileNamePrefix}-{reportType}-{formattedDate}.xlsx"; // ❗ Thêm key này

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
                            Lang.Export
                        );

                        MessageBoxHelper.ShowInfo(Lang.ExportSuccess); // ❗ Thêm key này
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError(Lang.ExportError, ex); // ❗ Thêm key này
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
                    cbxProcess.Properties.Items.Add(Lang.AllProcess);
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
                processList.Insert(0, Lang.AllProcess);

                // Update ComboBox items
                cbxProcess.Properties.Items.Clear();
                cbxProcess.Properties.Items.AddRange(processList);

                // Ensure SelectedIndex can be set
                if (cbxProcess.Properties.Items.Count > 0)
                    cbxProcess.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ErrorLoadingProcessList, ex);
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

                if (selectedValue == Lang.AllProcess)
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
                Lang.RefreshingData
            );
        }
    }
}
