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
        private ProductionDataService_Model _productionDataService;

        private readonly ProductionData_DAL productionData_DAL = new ProductionData_DAL();

        private readonly List<ProductionData_Model> _modifiedDataList = new List<ProductionData_Model>();
        private List<ExcelRowData_Model> _excelPreviewData;
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
        private async void ucWorkingTime_Load(object sender, EventArgs e)
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

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedItems = GetSelectedProductionData();
            if (selectedItems.Count < 2)
            {
                MessageBoxHelper.ShowWarning("Please select at least 2 rows to merge.");
                return;
            }

            // Check if the selected rows are eligible to be merged (e.g., same Process, Line, ScanDate...)
            if (!CanMergeItems(selectedItems))
            {
                MessageBoxHelper.ShowWarning("Selected rows are not valid for merging.");
                dgvWorkingTime.ClearSelection();
                return;
            }

            // Call service to perform merge (update model, recalculate, etc.)
            _productionDataService.MergeItems(selectedItems);

            // Update the database for each ProductionID with the new MergeGroupID
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

            // Get all rows with this MergeGroupID from raw data
            var groupItems = _productionDataService.RawData
                .Where(x => x.MergeGroupID == groupId)
                .ToList();

            if (groupItems.Count == 0)
            {
                MessageBoxHelper.ShowWarning("No rows found for the selected group to unmerge.");
                return;
            }

            // Update DB to unmerge (e.g., set MergeGroupID = null)
            productionData_DAL.SetUnmergeInfo(groupId);

            // Update in-memory model (remove merge)
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
                "Process", "ActualPPH", "PPHRate", "LargestOutput", "OperatorAdjust"
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
        private void LoadDataToGrid(ProductionDataService_Model manager)
        {
            _productionDataService = manager;
            gridControl1.DataSource = _productionDataService.MergedList;

            PopulateProcessComboBox(_productionDataService.MergedList.ToList());
            ApplyFilter();
        }

        public async Task SaveModifiedData()
        {
            if (_modifiedDataList == null || !_modifiedDataList.Any())
            {
                MessageBoxHelper.ShowInfo("There is no modified data to save.");
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

                MessageBoxHelper.ShowInfo("Data saved successfully!");
                _modifiedDataList.Clear();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Error while saving data", ex);
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
                    if (ex.Message.ToLower().Contains("duplicate") || ex.Message.ToLower().Contains("trùng"))
                    {
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
            var data = gridView.GetRow(e.RowHandle) as ProductionData_Model;
            if (data == null)
                return;

            string fieldName = e.Column.FieldName;  // Ví dụ: "TotalWorker"
            object newValue = e.Value;              // Giá trị người dùng vừa nhập

            Debug.WriteLine($"ProductionID: {data.ProductionID} | Field: {fieldName} | NewValue: {newValue}");

            // Cập nhật _modifiedDataList để lưu các thay đổi tạm thời
            var modified = _modifiedDataList.FirstOrDefault(x => x.ProductionID == data.ProductionID);
            if (modified != null)
            {
                typeof(ProductionData_Model).GetProperty(fieldName)?.SetValue(modified, newValue);
            }
            else
            {
                var clone = new ProductionData_Model
                {
                    ProductionID = data.ProductionID,
                    TotalWorker = data.TotalWorker,
                    WorkingTime = data.WorkingTime,
                    Quantity = data.Quantity,
                    IEPPH = data.IEPPH,
                    // Copy thêm các trường khác nếu cần
                };
                typeof(ProductionData_Model).GetProperty(fieldName)?.SetValue(clone, newValue);
                _modifiedDataList.Add(clone);
            }

            // Cập nhật trực tiếp vào RawData (BindingList<ProductionData>)
            var rawItem = _productionDataService.RawData.FirstOrDefault(r => r.ProductionID == data.ProductionID);
            if (rawItem != null)
            {
                typeof(ProductionData_Model).GetProperty(fieldName)?.SetValue(rawItem, newValue);

                // Optional: Nếu có cần tính toán lại Target/Rate thì gọi Recalculate()
                if (fieldName == nameof(ProductionData_Model.TotalWorker) || fieldName == nameof(ProductionData_Model.WorkingTime) || fieldName == nameof(ProductionData_Model.IEPPH))
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
                        toolTip = "Number of workers must be a positive integer (e.g., 20)";
                    else if (fieldName == "WorkingTime")
                        toolTip = "Working hours must be a positive decimal number (e.g., 9.5)";

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
                    e.ErrorText = "Only positive integers are allowed or leave it blank.";
                }
            }
            else if (field == "WorkingTime")
            {
                if (!double.TryParse(valueStr, out double wt) || wt <= 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Only positive decimal numbers are allowed or leave it blank.";
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
