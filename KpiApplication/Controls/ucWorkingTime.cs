using DevExpress.XtraBars;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucWorkingTime : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private ToolStripMenuItem mergeMenuItem;
        private ToolStripMenuItem unmergeMenuItem;
        public bool HasUnsavedChanges => _modifiedDataList.Count > 0;
        private ProductionDataService_Model _productionDataService;

        private readonly ProductionData_DAL productionData_DAL = new ProductionData_DAL();

        private readonly HashSet<ProductionData_Model> _modifiedDataList = new HashSet<ProductionData_Model>();
        private List<ExcelRowData_Model> _excelPreviewData;
        public ucWorkingTime()
        {
            InitializeComponent();

            dgvWorkingTime.CustomRowFilter += dgvWorkingTime_CustomRowFilter;
            dgvWorkingTime.MouseUp += dgvWorkingTime_MouseUp;
            dgvWorkingTime.KeyDown += dgvWorkingTime_KeyDown;
            dgvWorkingTime.CellMerge += dgvWorkingTime_CellMerge;
            dgvWorkingTime.CellValueChanged += dgvWorkingTime_CellValueChanged;
            dgvWorkingTime.ValidatingEditor += dgvWorkingTime_ValidatingEditor;

            mergeMenuItem = new ToolStripMenuItem("Merge", null, mergeToolStripMenuItem_Click);
            unmergeMenuItem = new ToolStripMenuItem("Unmerge", null, unmergeToolStripMenuItem_Click);

            contextMenuMerge.Items.Add(mergeMenuItem);
            contextMenuMerge.Items.Add(unmergeMenuItem);
            contextMenuMerge.Items.Add(new ToolStripSeparator());
        }
        private void btnPreviewCancel_Click(object sender, EventArgs e)
        {
            layoutPreview.Visible = false;
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
                MessageBoxHelper.ShowError("Load data failed", ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedProductionData();
            if (selectedItems.Count < 2)
            {
                MessageBoxHelper.ShowWarning("Please select at least 2 rows to merge.");
                return;
            }

            if (!CanMergeItems(selectedItems))
            {
                MessageBoxHelper.ShowWarning("Selected rows are not valid for merging.");
                dgvWorkingTime.ClearSelection();
                return;
            }

            _productionDataService.MergeItems(selectedItems);

            foreach (var item in selectedItems)
            {
                if (item.MergeGroupID.HasValue)
                {
                    productionData_DAL.SetMergeInfo(item.ProductionID, item.MergeGroupID.Value);
                }
            }

            dgvWorkingTime.ClearSelection();
            dgvWorkingTime.RefreshData();
        }
        private void unmergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedProductionData();
            var mergedItem = selectedItems.FirstOrDefault(x => x.IsMerged && x.MergeGroupID.HasValue);
            if (mergedItem == null)
            {
                MessageBoxHelper.ShowWarning("The selected row is not merged.\nPlease try again!");
                return;
            }

            int groupId = mergedItem.MergeGroupID.Value;

            var groupItems = _productionDataService.RawData
                .Where(x => x.MergeGroupID == groupId)
                .ToList();

            if (groupItems.Count == 0)
            {
                MessageBoxHelper.ShowWarning("No rows found for the selected group to unmerge.");
                return;
            }

            productionData_DAL.SetUnmergeInfo(groupId);

            _productionDataService.UnmergeItems(groupId, groupItems.Select(x => x.ProductionID).ToList());

            dgvWorkingTime.ClearSelection();
            dgvWorkingTime.RefreshData();

            MessageBoxHelper.ShowInfo("✅ Unmerge completed successfully.");
        }

        private void btnPreviewSave_Click(object sender, EventArgs e)
        {
            if (_excelPreviewData == null)
                return;

            var productionList = _productionDataService?.MergedList.ToList();
            if (productionList == null || productionList.Count == 0)
            {
                MessageBoxHelper.ShowWarning("Production data has not been loaded yet.");
                return;
            }
            int updatedCount = 0;
            foreach (var excelRow in _excelPreviewData)
            {
                var matchedRows = productionList.Where(p =>
                    string.Equals(p.LineName, excelRow.LineName, StringComparison.OrdinalIgnoreCase) &&
                    p.ScanDate == excelRow.WorkingDate).ToList();

                foreach (var matched in matchedRows)
                {
                    bool hasChanged = false;
                    if (excelRow.TotalWorker.HasValue && matched.TotalWorker != excelRow.TotalWorker)
                    {
                        matched.TotalWorker = excelRow.TotalWorker.Value;
                        hasChanged = true;
                    }

                    if (excelRow.WorkingHours.HasValue && matched.WorkingTime != excelRow.WorkingHours)
                    {
                        matched.WorkingTime = excelRow.WorkingHours.Value;
                        hasChanged = true;
                    }

                    if (hasChanged)
                    {
                        updatedCount++;
                        if (!_modifiedDataList.Any(x => x.ProductionID == matched.ProductionID))
                        {
                            _modifiedDataList.Add(matched);
                        }
                    }
                }
            }

            if (updatedCount > 0)
            {
                MessageBoxHelper.ShowInfo($"{updatedCount} row(s) updated.");
                gridControl1.RefreshDataSource();
                dgvWorkingTime.RefreshData();
            }
            else
            {
                MessageBoxHelper.ShowInfo("No rows were updated.");
            }

            layoutPreview.Visible = false;
        }
        private void btnImportFile_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Excel Files|*.xlsx;*.xls";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ShowExcelPreview(dlg.FileName);
                }
            }
        }


        private void ShowExcelPreview(string excelFilePath)
        {
            _excelPreviewData = ExcelImporter.LoadExcelData(excelFilePath);
            if (_excelPreviewData != null && _excelPreviewData.Count > 0)
            {
                previewGrid.DataSource = _excelPreviewData;

                if (previewView.Columns["LineName"] != null)
                    previewView.Columns["LineName"].Caption = "Line";

                if (previewView.Columns["TotalWorker"] != null)
                    previewView.Columns["TotalWorker"].Caption = "Worker Count";

                if (previewView.Columns["WorkingHours"] != null)
                    previewView.Columns["WorkingHours"].Caption = "Working Hours";

                layoutPreview.Visible = true;
                layoutPreview.BringToFront();
            }
            else
            {
                MessageBoxHelper.ShowWarning("No data found in the Excel file.");
            }
        }


        private void ConfigureGridAfterDataBinding()
        {
            var editableCols = new List<string> { "WorkingTime", "TotalWorker" };
            GridViewHelper.ApplyDefaultFormatting(dgvWorkingTime, editableCols);

            dgvWorkingTime.OptionsSelection.MultiSelect = true;
            dgvWorkingTime.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;

            GridViewHelper.EnableWordWrapForGridView(dgvWorkingTime);

            GridViewHelper.HideColumns(dgvWorkingTime,
                "ArticleID", "DepartmentCode", "ProductionID","TargetOfPC",
                "OutputRateValue", "IsMerged", "IsVisible", "TotalWorkingHours",
                "MergeGroupID", "IsSlides", "PPHRateValue", "PPHFallsBelowReasons",
                "ActualPPH", "PPHRate", "LargestOutput", "OperatorAdjust", "IsModified"
            );

            GridViewHelper.SetColumnCaptions(dgvWorkingTime, new Dictionary<string, string>
    {
        { "IEPPH", "IE PPH" },
        { "TypeName", "Stage" }
    });

            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvWorkingTime);
            GridViewHelper.EnableCopyFunctionality(dgvWorkingTime);
        }

        private void LoadDataToGrid(ProductionDataService_Model manager)
        {
            _productionDataService = manager;
            gridControl1.DataSource = _productionDataService.MergedList;

            PopulateProcessComboBox(_productionDataService.MergedList.ToList());
            ApplyFilter();
        }

        private async Task SendOverLimitAlertEmailAsync(List<ProductionData_Model> overLimitRows)
        {
            if (overLimitRows == null || !overLimitRows.Any())
                return;

            try
            {
                if (_productionDataService == null)
                {
                    Debug.WriteLine("⚠️ _productionDataService chưa có dữ liệu, bỏ qua kiểm tra lịch sử.");
                    return;
                }

                DateTime fromDate = DateTime.Today.AddMonths(-6); // chỉ lấy 6 tháng gần nhất
                var historyData = _productionDataService.MergedList?
                    .Where(x =>
                        x.ScanDate >= fromDate &&
                        x.ScanDate < DateTime.Today &&
                        string.Equals(x.TypeName, "Mass Production", StringComparison.OrdinalIgnoreCase) &&
                        x.PPHRateValue.HasValue &&
                        x.PPHRateValue.Value >= 1.3)
                    .OrderByDescending(x => x.ScanDate)
                    .ToList();

                if (historyData == null || historyData.Count == 0)
                {
                    Debug.WriteLine("📭 Không có dữ liệu lịch sử trong 3 tháng gần nhất, bỏ qua gửi.");
                    return;
                }

                var filteredRows = new List<object>();
                foreach (var current in overLimitRows)
                {
                    var matchedHistory = historyData
                        .Where(prev =>
                            string.Equals(prev.LineName, current.LineName, StringComparison.OrdinalIgnoreCase) &&
                            IsModelSimilar(prev.ModelName, current.ModelName)
                        )
                        .Take(3) // chỉ lấy 3 dòng lịch sử mới nhất
                        .ToList();

                    if (matchedHistory.Any())
                    {
                        Debug.WriteLine($"✅ [MATCH] {current.ScanDate:yyyy-MM-dd} | {current.LineName} | {current.ModelName} " +
                                        $"→ Tìm thấy {matchedHistory.Count} bản ghi lịch sử trong 3 tháng qua.");

                        filteredRows.Add(new
                        {
                            Current = new
                            {
                                current.ScanDate,
                                current.LineName,
                                current.ModelName,
                                current.TypeName,
                                current.Quantity,
                                current.TotalWorker,
                                current.WorkingTime,
                                current.ActualPPH,
                                current.IEPPH,
                                current.PPHRate
                            },
                            History = matchedHistory.Select(h => new
                            {
                                h.ScanDate,
                                h.LineName,
                                h.ModelName,
                                h.Quantity,
                                h.TotalWorker,
                                h.WorkingTime,
                                h.ActualPPH,
                                h.IEPPH,
                                h.PPHRate
                            }).ToList(),
                            PushedAt = DateTime.UtcNow.ToString("o")
                        });
                    }
                    else
                    {
                        Debug.WriteLine($"🔇 [NO MATCH] {current.ScanDate:yyyy-MM-dd} | {current.LineName} | {current.ModelName}");
                    }
                }

                if (!filteredRows.Any())
                {
                    Debug.WriteLine("🔇 Không tìm thấy dòng nào có tiền sử vượt 130%, không gửi mail.");
                    return;
                }

                await MailHelper.PushDailyAlertDataAsync(filteredRows);

                Debug.WriteLine($"📤 Đã push {filteredRows.Count} dòng vượt ngưỡng (kèm lịch sử).");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowWarning($"❌ Không push được dữ liệu cảnh báo: {ex.Message}");
            }
        }

        /// <summary>
        /// Bỏ ngoặc và chuẩn hoá model
        /// </summary>
        private string NormalizeModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return string.Empty;

            string result = model.Trim().ToUpperInvariant();

            // Bỏ toàn bộ nội dung trong ngoặc, nếu có nhiều ngoặc cũng bỏ hết
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\([^)]*\)", "").Trim();

            return result;
        }

        /// <summary>
        /// So sánh model: chỉ cần một chứa model còn lại là coi như giống
        /// </summary>
        private bool IsModelSimilar(string model1, string model2)
        {
            string norm1 = NormalizeModel(model1);
            string norm2 = NormalizeModel(model2);

            if (string.IsNullOrEmpty(norm1) || string.IsNullOrEmpty(norm2))
                return false;

            return norm1.Contains(norm2) || norm2.Contains(norm1);
        }

        public async Task SaveModifiedData()
        {
            if (_modifiedDataList == null || !_modifiedDataList.Any())
            {
                MessageBoxHelper.ShowInfo("There is no modified data to save.");
                return;
            }

            var overLimitRows = _modifiedDataList
                .Where(x =>
                    string.Equals(x.TypeName, "Mass Production", StringComparison.OrdinalIgnoreCase) &&
                    x.PPHRateValue.HasValue &&
                    x.PPHRateValue.Value >= 1.3)
                .ToList();

            await SendOverLimitAlertEmailAsync(overLimitRows);

            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    SaveAllDataToDatabase,
                    result => { },
                    Lang.Saving);

                MessageBoxHelper.ShowInfo("Data saved successfully!");
                _modifiedDataList.Clear();
                dgvWorkingTime.RefreshData();

            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Error while saving data", ex);
            }
        }

        #region === Lưu dữ liệu song song ===
        private async Task SaveAllDataToDatabase()
        {
            var dal = new ProductionData_DAL();
            var tasks = new List<Task>();

            foreach (var item in _modifiedDataList)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        dal.UpdateProductionData(item);
                        item.IsModified = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Lỗi khi lưu ProductionID={item.ProductionID}: {ex.Message}");
                        // Bạn có thể log vào file hoặc hiển thị trong UI nếu cần
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }
        #endregion


        #region === CellValueChanged: Ghi nhận thay đổi ===
        private void dgvWorkingTime_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (!(sender is GridView gridView)) return;
            if (!(gridView.GetRow(e.RowHandle) is ProductionData_Model data)) return;

            string fieldName = e.Column.FieldName;
            object newValue = e.Value;

            Debug.WriteLine($"ProductionID: {data.ProductionID} | Field: {fieldName} | NewValue: {newValue}");

            // Gán trực tiếp vào object (nếu chưa gán bởi grid)
            typeof(ProductionData_Model).GetProperty(fieldName)?.SetValue(data, newValue);

            // Đánh dấu là đã chỉnh sửa
            data.IsModified = true;
            _modifiedDataList.Add(data);

            // Nếu có field cần tính toán lại
            if (fieldName == nameof(ProductionData_Model.TotalWorker) ||
                fieldName == nameof(ProductionData_Model.WorkingTime) ||
                fieldName == nameof(ProductionData_Model.IEPPH))
            {
                data.Recalculate();
            }

            gridView.RefreshRow(e.RowHandle);
        }
        #endregion
        private void toolTipController1_GetActiveObjectInfo(object sender, DevExpress.Utils.ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
            var grid = e.SelectedControl as DevExpress.XtraGrid.GridControl;
            if (grid == null)
                return;

            var view = grid.FocusedView as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null)
                return;

            // Lấy tọa độ dựa trên e.Control, không dùng MousePosition (chính xác hơn)
            Point clientPoint = grid.PointToClient(Control.MousePosition);
            var hitInfo = view.CalcHitInfo(clientPoint);

            if (!hitInfo.InRowCell || hitInfo.Column == null)
                return;

            string fieldName = hitInfo.Column.FieldName;
            string toolTip = null;

            if (fieldName == "TotalWorker")
            {
                toolTip = "Number of workers must be a positive integer (e.g., 20)";
            }
            else if (fieldName == "WorkingTime")
            {
                toolTip = string.Format("Working hours must be a positive decimal number (e.g., 9{0}5)",
                    CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            }

            if (string.IsNullOrEmpty(toolTip))
                return;

            // Key phải unique để tránh tooltip cũ hiện lại
            string key = string.Format("Row{0}_Col{1}", hitInfo.RowHandle, fieldName);
            e.Info = new DevExpress.Utils.ToolTipControlInfo(key, toolTip);
        }


        private void dgvWorkingTime_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            string field = view.FocusedColumn.FieldName;
            string valueStr = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(valueStr))
            {
                e.Valid = true;
                return;
            }


            if (field == "TotalWorker")
            {
                if (!int.TryParse(valueStr, out int tw) || tw <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Only positive integers are allowed or leave it blank.";
                }
            }
            else if (field == "WorkingTime")
            {
                double wt;
                // NumberStyles.Float bao gồm AllowThousands -> cần remove
                const NumberStyles style = NumberStyles.Float & ~NumberStyles.AllowThousands;

                bool parsed = double.TryParse(
                    valueStr,
                    style,
                    CultureInfo.CurrentCulture,
                    out wt);

                if (!parsed)
                {
                    parsed = double.TryParse(
                        valueStr,
                        style,
                        CultureInfo.InvariantCulture,
                        out wt);
                }

                if (!parsed || wt <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = $"Only positive decimal numbers are allowed ({CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator} as separator).";
                }

            }
        }


        private void OnProductionDataListChanged()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(OnProductionDataListChanged));
                return;
            
            }
        }

        private ProductionDataService_Model FetchData()  
        {
            var data = productionData_DAL.GetAllData();     
            return new ProductionDataService_Model(data);
        }


        private void dgvWorkingTime_CustomRowFilter(object sender, DevExpress.XtraGrid.Views.Base.RowFilterEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;

            var data = view.GetRow(e.ListSourceRow) as ProductionData_Model;
            if (data != null && data.IsVisible == false)
            {
                e.Visible = false;
                e.Handled = true;
            }
        }

        private void dgvWorkingTime_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hitInfo = dgvWorkingTime.CalcHitInfo(e.Location);
                if (hitInfo.InRow)
                {
                    var selectedRows = dgvWorkingTime.GetSelectedRows();
                    if (selectedRows != null && selectedRows.Length > 0)
                    {
                        dgvWorkingTime.FocusedRowHandle = hitInfo.RowHandle;

                        Point screenPoint = dgvWorkingTime.GridControl.PointToScreen(e.Location);
                        contextMenuMerge.Show(screenPoint);
                    }
                }
            }
        }
        private bool CanMergeItems(List<ProductionData_Model> selectedItems)
        {
            if (!HasSameScanDate(selectedItems))
            {
                MessageBoxHelper.ShowWarning("The selected rows have different working dates and cannot be merged.");
                return false;
            }

            if (!HasSameDepartmentCode(selectedItems))
            {
                MessageBoxHelper.ShowWarning("Cannot merge data from different lines.");
                return false;
            }

            if (HasMergedItems(selectedItems))
            {
                MessageBoxHelper.ShowWarning("Some rows have already been merged. Please unmerge them first.");
                return false;
            }

            return true;
        }

        // Hàm kiểm tra xem các dòng có cùng ScanDate không
        private bool HasSameScanDate(List<ProductionData_Model> selectedItems)
        {
            var distinctDates = selectedItems
                .Where(x => x.ScanDate.HasValue)
                .Select(x => x.ScanDate.Value.Date)
                .Distinct()
                .ToList();
            return distinctDates.Count == 1;
        }

        // Hàm kiểm tra xem các dòng có cùng DepartmentCode không
        private bool HasSameDepartmentCode(List<ProductionData_Model> selectedItems)
        {
            var distinctDepartments = selectedItems.Select(x => x.LineName).Distinct().ToList();
            return distinctDepartments.Count == 1;
        }

        // Hàm kiểm tra xem có dòng nào đã thuộc nhóm merge không
        private bool HasMergedItems(List<ProductionData_Model> selectedItems)
        {
            return selectedItems.Any(x => x.MergeGroupID != null);
        }

        // Hàm lấy dữ liệu sản xuất đã chọn
        private List<ProductionData_Model> GetSelectedProductionData()
        {
            return dgvWorkingTime.GetSelectedRows()
                .Select(i => dgvWorkingTime.GetRow(i) as ProductionData_Model)
                .Where(x => x != null)
                .ToList();
        }
        private void dgvWorkingTime_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            var row = view.GetRow(e.RowHandle) as ProductionData_Model;

            if (row?.IsMerged == true)
            {
                e.Appearance.BackColor = Color.LightYellow;
            }
        }

        private void ApplyFilter()
        {
            DateTime today = DateTime.Today;  // Lấy ngày hôm nay, bỏ giờ phút giây
            DateTime filterDate;

            if (today.DayOfWeek == DayOfWeek.Monday)
            {
                filterDate = today.AddDays(-2);  // Thứ 7
            }
            else
            {
                filterDate = today.AddDays(-1);  // Hôm trước
            }

            // Định dạng ngày (Culture Invariant)
            string startDate = filterDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            string endDate = filterDate.AddDays(1).ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

            // Áp dụng filter cho GridView
            dgvWorkingTime.ActiveFilterString = $"[ScanDate] >= #{startDate}# AND [ScanDate] < #{endDate}#";
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
                MessageBoxHelper.ShowError("An error occurred while loading the list of processes", ex);
            }
        }

        private void cbxProcess_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilterFromComboBox(cbxProcess, dgvWorkingTime);
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

        private void dgvWorkingTime_KeyDown(object sender, KeyEventArgs e)
        {
            var selectedRows = dgvWorkingTime.GetSelectedRows();

            // Nếu không có dòng nào được chọn thì thoát
            if (selectedRows == null || selectedRows.Length == 0)
                return;

            // M để Merge
            if (e.KeyCode == Keys.M)
            {
                mergeToolStripMenuItem_Click(sender, EventArgs.Empty);
                e.Handled = true;
                Debug.WriteLine("Ctrl + M is pressed");
            }
            // U để Unmerge
            else if (e.KeyCode == Keys.U)
            {
                unmergeToolStripMenuItem_Click(sender, EventArgs.Empty);
                e.Handled = true;
                Debug.WriteLine("Ctrl + U is pressed");
            }
        }
        private void dgvWorkingTime_CellMerge(object sender, CellMergeEventArgs e)
        {
            // Các cột muốn gộp, bao gồm cả ArticleName
            string[] mergeableColumns = { "ScanDate", "Fatory", "Plant" };

            if (!mergeableColumns.Contains(e.Column.FieldName))
            {
                e.Merge = false;
                e.Handled = true;
                return;
            }

            // Lấy giá trị ArticleName để làm điều kiện gộp
            string article1 = dgvWorkingTime.GetRowCellValue(e.RowHandle1, "Plant")?.ToString();
            string article2 = dgvWorkingTime.GetRowCellValue(e.RowHandle2, "Plant")?.ToString();

            if (e.Column.FieldName == "Plant")
            {
                e.Merge = article1 == article2;
            }
            else
            {
                e.Merge = article1 == article2;
            }

            e.Handled = true;
        }
    }
}
