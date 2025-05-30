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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucWorkingTime : DevExpress.XtraEditors.XtraUserControl
    {
        private ToolStripMenuItem mergeMenuItem;
        private ToolStripMenuItem unmergeMenuItem;
        public bool HasUnsavedChanges => _modifiedDataList.Count > 0;

        private readonly ProductionData_DAL productionData_DAL = new ProductionData_DAL();

        private ProductionDataListManager _listManager;
        private readonly List<ProductionData> _modifiedDataList = new List<ProductionData>();
        private List<ExcelRowData> _excelPreviewData;
        public ucWorkingTime()
        {
            InitializeComponent();

            dgvWorkingTime.CustomRowFilter += dgvWorkingTime_CustomRowFilter;
            this.Load += ucWorkingTime_Load;
            dgvWorkingTime.MouseUp += dgvWorkingTime_MouseUp;
            dgvWorkingTime.RowCellStyle += dgvWorkingTime_RowCellStyle;
            dgvWorkingTime.KeyDown += dgvWorkingTime_KeyDown;
            cbxProcess.SelectedIndexChanged += cbxProcess_SelectedIndexChanged;
            dgvWorkingTime.CellMerge += dgvWorkingTime_CellMerge;
            dgvWorkingTime.CellValueChanged += dgvWorkingTime_CellValueChanged;

            mergeMenuItem = new ToolStripMenuItem("Merge");
            mergeMenuItem.Click += mergeToolStripMenuItem_Click;

            unmergeMenuItem = new ToolStripMenuItem("Unmerge");
            unmergeMenuItem.Click += unmergeToolStripMenuItem_Click;

            contextMenuMerge.Items.Add(mergeMenuItem);
            contextMenuMerge.Items.Add(unmergeMenuItem);
        }

        private void btnPreviewCancel_Click(object sender, EventArgs e)
        {
            layoutPreview.Visible = false;
        }
        private void btnImportExcel_Click(object sender, EventArgs e)
        {
        }
        private void btnPreviewSave_Click(object sender, EventArgs e)
        {
            if (_excelPreviewData == null)
                return;

            var productionList = _listManager?.MergedList.ToList();
            if (productionList == null || productionList.Count == 0)
            {
                XtraMessageBox.Show("Dữ liệu sản xuất chưa được tải.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                XtraMessageBox.Show($"{updatedCount} dòng đã được cập nhật.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                gridControl1.RefreshDataSource();
                dgvWorkingTime.RefreshData();
            }
            else
            {
                XtraMessageBox.Show("Không có dòng nào được cập nhật.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    previewView.Columns["TotalWorker"].Caption = "Person";
                if (previewView.Columns["WorkingHours"] != null)
                    previewView.Columns["WorkingHours"].Caption = "Working Hours";

                layoutPreview.Visible = true;
                layoutPreview.BringToFront();
            }
            else
            {
                XtraMessageBox.Show("Không có dữ liệu trong file Excel.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private int MergeExcelDataToProductionList(List<ExcelRowData> excelList, List<ProductionData> productionList)
        {
            int updatedCount = 0;

            foreach (var excelRow in excelList)
            {
                var matchedRows = productionList.Where(p =>
                    string.Equals(p.LineName, excelRow.LineName, StringComparison.OrdinalIgnoreCase) &&
                    p.ScanDate == excelRow.WorkingDate).ToList();

                if (matchedRows.Any())
                {
                    foreach (var matched in matchedRows)
                    {
                        bool isUpdated = false;

                        if (excelRow.TotalWorker.HasValue && matched.TotalWorker != excelRow.TotalWorker)
                        {
                            matched.TotalWorker = excelRow.TotalWorker.Value;
                            isUpdated = true;
                        }

                        if (excelRow.WorkingHours.HasValue && matched.WorkingTime != excelRow.WorkingHours)
                        {
                            matched.WorkingTime = excelRow.WorkingHours.Value;
                            isUpdated = true;
                        }

                        if (isUpdated)
                            updatedCount++;
                    }
                }
            }

            return updatedCount;
        }


        private void ConfigureGridAfterDataBinding()
        {
            var editableCols = new List<string> { "WorkingTime", "TotalWorker" };
            GridViewHelper.ApplyDefaultFormatting(dgvWorkingTime, editableCols);
            GridViewHelper.ApplyRowStyleAlternateColors(dgvWorkingTime, Color.AliceBlue, Color.White);

            dgvWorkingTime.OptionsSelection.MultiSelect = true;
            dgvWorkingTime.OptionsSelection.MultiSelectMode = GridMultiSelectMode.CheckBoxRowSelect;

            GridViewHelper.EnableWordWrapForGridView(dgvWorkingTime);

            GridViewHelper.HideColumns(dgvWorkingTime,
                "ArticleID", "DepartmentCode", "ProductionID",
                "Rate", "", "IsVisible", "TotalWorkingHours",
                "MergeGroupID", "IsSlides", "PPHRateValue", "PPHFallsBelowReasons",
                "Process", "ActualPPH", "PPHRate", "LargestOutput"
            );

            GridViewHelper.SetColumnCaptions(dgvWorkingTime, new Dictionary<string, string>
    {
        { "IEPPH", "IE PPH" },
        { "TypeName", "Stage" }
    });

            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvWorkingTime);
            GridViewHelper.EnableCopyFunctionality(dgvWorkingTime);
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

            _modifiedDataList.Clear();
        }
        private void LoadDataToGrid(ProductionDataListManager manager)
        {
            _listManager = manager;
            gridControl1.DataSource = _listManager.MergedList;

            PopulateProcessComboBox(_listManager.MergedList.ToList());
            ApplyFilter();
        }

        private async void ucWorkingTime_Load(object sender, EventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                data =>
                {
                    LoadDataToGrid(data);
                    ConfigureGridAfterDataBinding();
                });
        }
        public async Task SaveModifiedData()
        {
            if (_modifiedDataList == null || !_modifiedDataList.Any())
            {
                XtraMessageBox.Show("There is no modified data to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    SaveAllDataToDatabase,
                    (result) => { },
                                    
                "Loading"
                );

                XtraMessageBox.Show("Data saved successfully!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _modifiedDataList.Clear();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Error while saving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveAllDataToDatabase()
        {
            var dal = new ProductionData_DAL();
            var dataToSave = _modifiedDataList.ToList();

            foreach (var item in dataToSave)
            {
                try
                {
                    await Task.Run(() => dal.UpdateProductionData(item));
                }
                catch (Exception ex)
                {
                    // Nếu bạn muốn handle lỗi trùng tại đây, có thể ghi log hoặc ghi flag, 
                    // nhưng không gọi MessageBox.
                    // Ví dụ:
                    if (ex.Message.ToLower().Contains("duplicate") || ex.Message.ToLower().Contains("trùng"))
                    {
                        // Có thể log lỗi hoặc thêm xử lý khác
                        throw new Exception("Duplicate data detected, please check your input.");
                    }
                    else
                    {
                        throw; 
                    }
                }
            }
        }

        private void dgvWorkingTime_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            var gridView = sender as GridView;
            var data = gridView.GetRow(e.RowHandle) as ProductionData;
            if (data == null)
                return;

            string fieldName = e.Column.FieldName;  // Ví dụ: "TotalWorker"
            object newValue = e.Value;              // Giá trị người dùng vừa nhập

            Debug.WriteLine($"ProductionID: {data.ProductionID} | Field: {fieldName} | NewValue: {newValue}");

            // Cập nhật _modifiedDataList để lưu các thay đổi tạm thời
            var modified = _modifiedDataList.FirstOrDefault(x => x.ProductionID == data.ProductionID);
            if (modified != null)
            {
                typeof(ProductionData).GetProperty(fieldName)?.SetValue(modified, newValue);
            }
            else
            {
                var clone = new ProductionData
                {
                    ProductionID = data.ProductionID,
                    TotalWorker = data.TotalWorker,
                    WorkingTime = data.WorkingTime,
                    Quantity = data.Quantity,
                    IEPPH = data.IEPPH,
                    // Copy thêm các trường khác nếu cần
                };
                typeof(ProductionData).GetProperty(fieldName)?.SetValue(clone, newValue);
                _modifiedDataList.Add(clone);
            }

            // Cập nhật trực tiếp vào RawData (BindingList<ProductionData>)
            var rawItem = _listManager.RawData.FirstOrDefault(r => r.ProductionID == data.ProductionID);
            if (rawItem != null)
            {
                typeof(ProductionData).GetProperty(fieldName)?.SetValue(rawItem, newValue);

                // Optional: Nếu có cần tính toán lại Target/Rate thì gọi Recalculate()
                if (fieldName == nameof(ProductionData.TotalWorker) || fieldName == nameof(ProductionData.WorkingTime) || fieldName == nameof(ProductionData.IEPPH))
                {
                    rawItem.Recalculate();
                }
            }
        }

        private void toolTipController1_GetActiveObjectInfo(object sender, DevExpress.Utils.ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
            if (e.SelectedControl is DevExpress.XtraGrid.GridControl grid)
            {
                DevExpress.XtraGrid.Views.Grid.GridView view = grid.FocusedView as DevExpress.XtraGrid.Views.Grid.GridView;
                var pt = view.GridControl.PointToClient(System.Windows.Forms.Control.MousePosition);
                var hitInfo = view.CalcHitInfo(pt);

                if (hitInfo.InRowCell)
                {
                    string fieldName = hitInfo.Column.FieldName;
                    string toolTip = null;

                    if (fieldName == "TotalWorker")
                        toolTip = "Số người là số nguyên dương (ví dụ: 20)";
                    else if (fieldName == "WorkingTime")
                        toolTip = "Số giờ làm việc là số thực dương, có thể có dấu chấm (ví dụ: 9.5)";

                    if (!string.IsNullOrEmpty(toolTip))
                    {
                        string key = $"{hitInfo.RowHandle}_{fieldName}";
                        e.Info = new DevExpress.Utils.ToolTipControlInfo(key, toolTip);
                    }
                }
            }
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
                    e.ErrorText = "Chỉ được nhập số nguyên dương hoặc để trống.";
                }
            }
            else if (field == "WorkingTime")
            {
                if (!double.TryParse(valueStr, out double wt) || wt <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Chỉ được nhập số thực dương hoặc để trống.";
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
        private ProductionDataListManager FetchData()
        {
            var data = productionData_DAL.GetAllData();
            return new ProductionDataListManager(data);
        }

        private void unmergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedProductionData();

            // Lấy dòng đầu tiên được chọn mà đang ở trạng thái Merged
            var selectedItem = selectedItems.FirstOrDefault(x => x.IsMerged && x.MergeGroupID.HasValue);

            if (selectedItem == null)
            {
                ShowMessage("Dòng đã chọn chưa được gộp.\nHãy thử lại!");
                return;
            }

            int groupId = selectedItem.MergeGroupID.Value;

            // 🔥 Lấy toàn bộ ProductionID của groupId (không chỉ dòng được chọn)
            var selectedIDs = _listManager.RawData
                .Where(x => x.MergeGroupID == groupId)
                .Select(x => x.ProductionID)
                .ToList();

            if (selectedIDs.Count == 0)
            {
                ShowMessage("Không tìm thấy dòng nào thuộc nhóm đã chọn để hủy gộp.");
                return;
            }

            productionData_DAL.SetUnmergeInfo(groupId);
            _listManager.UnmergeItems(groupId, selectedIDs);

            dgvWorkingTime.ClearSelection();
        }

        private void dgvWorkingTime_CustomRowFilter(object sender, DevExpress.XtraGrid.Views.Base.RowFilterEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            if (view == null) return;

            var data = view.GetRow(e.ListSourceRow) as ProductionData;
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
        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedProductionData();

            if (selectedItems.Count < 2)
            {
                ShowMessage("Vui lòng chọn ít nhất 2 dòng để gộp.");
                return;
            }

            if (!CanMergeItems(selectedItems))
            {
                dgvWorkingTime.ClearSelection();
                return;
            }

            _listManager.MergeItems(selectedItems);

            foreach (var item in selectedItems)
            {
                productionData_DAL.SetMergeInfo(item.ProductionID, item.MergeGroupID ?? 0);
            }

            dgvWorkingTime.ClearSelection();
            dgvWorkingTime.RefreshData();
        }
        private void ShowMessage(string message)
        {
            XtraMessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool CanMergeItems(List<ProductionData> selectedItems)
        {
            if (!HasSameScanDate(selectedItems))
            {
                ShowMessage("Các dòng có ngày làm việc khác nhau, không thể gộp.");
                return false;
            }

            if (!HasSameDepartmentCode(selectedItems))
            {
                ShowMessage("Không thể gộp dữ liệu của các chuyền khác nhau.");
                return false;
            }

            if (HasMergedItems(selectedItems))
            {
                ShowMessage("Có dòng đã gộp trước đó, hãy tách trước khi thực hiện gộp.");
                return false;
            }

            return true;
        }

        // Hàm kiểm tra xem các dòng có cùng ScanDate không
        private bool HasSameScanDate(List<ProductionData> selectedItems)
        {
            var distinctDates = selectedItems
                .Where(x => x.ScanDate.HasValue)
                .Select(x => x.ScanDate.Value.Date)
                .Distinct()
                .ToList();
            return distinctDates.Count == 1;
        }

        // Hàm kiểm tra xem các dòng có cùng DepartmentCode không
        private bool HasSameDepartmentCode(List<ProductionData> selectedItems)
        {
            var distinctDepartments = selectedItems.Select(x => x.DepartmentCode).Distinct().ToList();
            return distinctDepartments.Count == 1;
        }

        // Hàm kiểm tra xem có dòng nào đã thuộc nhóm merge không
        private bool HasMergedItems(List<ProductionData> selectedItems)
        {
            return selectedItems.Any(x => x.MergeGroupID != null);
        }

        // Hàm lấy dữ liệu sản xuất đã chọn
        private List<ProductionData> GetSelectedProductionData()
        {
            return dgvWorkingTime.GetSelectedRows()
                .Select(i => dgvWorkingTime.GetRow(i) as ProductionData)
                .Where(x => x != null)
                .ToList();
        }
        private void dgvWorkingTime_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            var row = view.GetRow(e.RowHandle) as ProductionData;

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

            // Gộp nếu:
            // - Đang ở cột ArticleName và giá trị giống nhau
            // - Hoặc các cột khác, nhưng ArticleName giống nhau
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
