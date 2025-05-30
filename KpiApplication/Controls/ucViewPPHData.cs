using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucViewPPHData : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<IETotal> ieTotalList;
        private List<IETotal> ieTotalListOriginalClone;
        private readonly HashSet<string> mergeByArticleCols;
        private IEPPHData_DAL iePPHData_DAL = new IEPPHData_DAL();

        // Thứ tự cột hiển thị
        private readonly string[] desiredColumnOrder = {
            "ArticleName", "ModelName", "PCSend", "PersonIncharge", "NoteForPC",
            "OutsourcingAssembling", "OutsourcingStitching", "OutsourcingStockFitting",
            "DataStatus", "StageName", "TypeName", "TargetOutputPC", "AdjustOperatorNo",
            "IEPPHValue", "TCTValue", "THTValue", "IsSigned",
            "SectionName", "ReferenceModel", "OperatorAdjust", "ReferenceOperator",
            "FinalOperator", "Notes"
        };

        public ucViewPPHData()
        {
            InitializeComponent();
            this.Load += ucViewPPHData_Load; 
            dgvIEPPH.RowUpdated += dgvIEPPH_RowUpdated;
            dgvIEPPH.CellMerge += dgvIEPPH_CellMerge;
            dgvIEPPH.MouseWheel += dgvIEPPH_MouseWheel;
            dgvIEPPH.CustomDrawCell += dgvIEPPH_CustomDrawCell;
            dgvIEPPH.ShowingPopupEditForm += dgvIEPPH_ShowingPopupEditForm;

            mergeByArticleCols = new HashSet<string> {
        "ArticleName", "ModelName", "PCSend", "PersonIncharge", "OutsourcingAssemblingBool",
        "NoteForPC", "OutsourcingStitchingBool", "OutsourcingStockFittingBool", "Status"
    };

        }

        private BindingList<IETotal> FetchData()
        {
            var data = iePPHData_DAL.GetIEPPHData();
            ieTotalListOriginalClone = data.Select(IETotal.Clone).ToList();
            return data;
        }

        private void LoadDataToGrid(BindingList<IETotal> data)
        {
            ieTotalList = data;
            gridControl1.DataSource = ieTotalList;
        }
        private async void btnImport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openDialog.FileName;

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        async () =>
                        {
                            await Task.Run(() =>
                            {
                                List<string> errors;
                                var tctItems = ExcelImporter.ReadTCTItemsFromExcel(filePath, out errors);

                                // --- Bắt đầu xoay bảng ---
                                var unpivotedList = new List<TCTImport>();

                                foreach (var item in tctItems)
                                {
                                    if (item.Cutting.HasValue)
                                        unpivotedList.Add(new TCTImport { ModelName = item.ModelName, Type = item.Type, Process = "Cutting", TCT = item.Cutting });

                                    if (item.Stitching.HasValue)
                                        unpivotedList.Add(new TCTImport { ModelName = item.ModelName, Type = item.Type, Process = "Stitching", TCT = item.Stitching });

                                    if (item.Assembly.HasValue)
                                        unpivotedList.Add(new TCTImport { ModelName = item.ModelName, Type = item.Type, Process = "Assembly", TCT = item.Assembly });

                                    if (item.Stockfitting.HasValue)
                                        unpivotedList.Add(new TCTImport { ModelName = item.ModelName, Type = item.Type, Process = "Stock Fitting", TCT = item.Stockfitting });
                                }

                                TCT_DAL.SaveTCTImportList(unpivotedList);

                                this.Invoke((Action)(() =>
                                {
                                    string message = $"Lưu dữ liệu thành công!";

                                    if (errors.Count > 0)
                                    {
                                        message += $"\n⚠️ Có {errors.Count} lỗi dữ liệu bị bỏ qua.";
                                    }

                                    XtraMessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }));
                            });
                        },
                        "Importing..." 
                    );
                }
            }
        }

        private async void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (ieTotalList == null || ieTotalList.Count == 0)
            {
                XtraMessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = XtraMessageBox.Show("Bạn có muốn xuất kèm cột TCT không?", "Tùy chọn xuất Excel",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            bool includeTCT = (result == DialogResult.Yes);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.FileName = "PPHData.xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string originalFilePath = saveDialog.FileName;
                    string filePath = originalFilePath;

                    // Nếu file tồn tại, thêm hậu tố _1, _2, ... cho đến khi không trùng
                    int count = 1;
                    string dir = Path.GetDirectoryName(originalFilePath);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilePath);
                    string ext = Path.GetExtension(originalFilePath);

                    while (File.Exists(filePath))
                    {
                        filePath = Path.Combine(dir, $"{fileNameWithoutExt}_{count}{ext}");
                        count++;
                    }

                    // Sử dụng LoadDataWithSplashAsync để hiện SplashScreen khi export
                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        () =>
                        {
                            ExcelExporter.ExportIETotalPivoted(ieTotalList.ToList(), filePath, includeTCT);
                            return true; // chỉ để phù hợp delegate Func<T>
                        },
                        _ => { 
                        }, 
                        "Loading"
                    );

                    XtraMessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
        }
        private void SetColumnOrder()
        {
            for (int i = 0; i < desiredColumnOrder.Length; i++)
            {
                var col = dgvIEPPH.Columns[desiredColumnOrder[i]];
                if (col != null)
                    col.VisibleIndex = i;
            }
        }

        private void ApplyColumnAlignment()
        {
            foreach (var colName in mergeByArticleCols)
            {
                var col = dgvIEPPH.Columns[colName];
                if (col != null)
                {
                    col.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
                    col.AppearanceCell.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
                }
            }
        }
        private void SetupMemoEditColumn(string columnName)
        {
            var memoEdit = new DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit();
            memoEdit.WordWrap = true;
            memoEdit.ScrollBars = ScrollBars.Vertical;
            gridControl1.RepositoryItems.Add(memoEdit);

            if (dgvIEPPH.Columns[columnName] != null)
            {
                dgvIEPPH.Columns[columnName].ColumnEdit = memoEdit;
                dgvIEPPH.Columns[columnName].AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            }

            dgvIEPPH.OptionsView.RowAutoHeight = true;
        }

        private void ColumnsReadOnlyInEditForm()
        {
            string[] readOnlyColumns = { "ArticleName", "ModelName", "Process" };

            foreach (var colName in readOnlyColumns)
            {
                var column = dgvIEPPH.Columns[colName];
                if (column != null)
                {
                    column.OptionsColumn.AllowEdit = false;
                    column.OptionsColumn.ReadOnly = true;
                    column.ColumnEdit = null;
                }
            }
        }
        private void SetupColumnComboBox(string columnName, Func<IETotal, string> selector)
        {
            var list = ieTotalList.Select(selector)
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .Distinct()
                                  .ToList();

            var combo = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            combo.Items.AddRange(list);
            combo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            gridControl1.RepositoryItems.Add(combo);

            if (dgvIEPPH.Columns[columnName] != null)
                dgvIEPPH.Columns[columnName].ColumnEdit = combo;
        }

        private void SetupColumnComboBox(string columnName, params string[] items)
        {
            var combo = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            combo.Items.AddRange(items);
            combo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            gridControl1.RepositoryItems.Add(combo);

            if (dgvIEPPH.Columns[columnName] != null)
                dgvIEPPH.Columns[columnName].ColumnEdit = combo;
        }
        private async void ucViewPPHData_Load(object sender, EventArgs e)
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                FetchData,
                (Action<BindingList<IETotal>>)(data =>
                {
                    LoadDataToGrid(data);

                    SetupMemoEditColumn("NoteForPC");
                    SetupColumnComboBox("PersonIncharge", x => x.PersonIncharge);
                    SetupColumnComboBox("Status", x => x.Status);
                    SetupColumnComboBox("TypeName", x => x.TypeName);
                    SetupColumnComboBox("IsSigned", new[] { "Signed", "Not Sign Yet" });

                    SetColumnOrder();
                    ApplyColumnAlignment();
                    ConfigureGridView();
                    SetColumnCaptions();
                    this.dgvIEPPH.BestFitColumns();
                    ColumnsReadOnlyInEditForm();
                    GridViewHelper.EnableCopyFunctionality((GridView)this.dgvIEPPH);
                }),
                "Loading..."
            );
        }
        private void SetColumnCaptions()
        {
            GridViewHelper.SetColumnCaptions(dgvIEPPH, new Dictionary<string, string>()
            {
                ["IEPPHValue"] = "IE PPH",
                ["IsSigned"] = "Production Sign",
                ["OutsourcingAssemblingBool"] = "Outsourcing\nAssembling",
                ["OutsourcingStitchingBool"] = "Outsourcing\nStitching",
                ["OutsourcingStockFittingBool"] = "Outsourcing\nStockFitting",
                ["AdjustOperatorNo"] = "Adjust\nOperator",
                ["TargetOutputPC"] = "Target\nOutput",
                ["THTValue"] = "THT",
                ["TypeName"] = "Type",
                ["PersonIncharge"] = "Person\nIncharge"
            });
        }

        private void ConfigureGridView()
        {
            GridViewHelper.ApplyDefaultFormatting(dgvIEPPH);

            dgvIEPPH.OptionsView.AllowCellMerge = true;
            dgvIEPPH.OptionsView.RowAutoHeight = true;
            dgvIEPPH.OptionsView.ColumnAutoWidth = false;
            dgvIEPPH.OptionsSelection.MultiSelect = true;
            dgvIEPPH.OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;
            gridControl1.UseEmbeddedNavigator = false;

            GridViewHelper.EnableWordWrapForGridView(dgvIEPPH);
            GridViewHelper.FixColumns(dgvIEPPH, "ArticleName", "ModelName");
            GridViewHelper.HideColumns(dgvIEPPH, "TypeID", "IEID", "ProcessID", "StageID", "ArticleID");
        }
        private void dgvIEPPH_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            var currentItem = e.Row as IETotal;
            if (currentItem == null)
            {
                XtraMessageBox.Show("❌ Dữ liệu dòng hiện tại không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var originalItem = ieTotalListOriginalClone.FirstOrDefault(x => x.IEID == currentItem.IEID);
            if (originalItem == null)
            {
                XtraMessageBox.Show($"❌ Không tìm thấy dữ liệu gốc để so sánh cho IEID = {currentItem.IEID}.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var changedProps = currentItem.GetChangedProperties(originalItem);
            if (changedProps.Count == 0)
            {
                Debug.WriteLine($"ℹ️ Không có thay đổi nào đối với IEID = {currentItem.IEID}.");
                return;
            }

            bool updateIEPPHData = false;
            bool updateProductionStages = false;
            bool updateArticles = false;

            var iePPHColumns = new[] {
        "PersonIncharge", "NoteForPC", "PCSend",
        "OutsourcingAssemblingBool", "OutsourcingStitchingBool", "OutsourcingStockFittingBool",
        "Status"
    };

            foreach (var prop in changedProps)
            {
                if (iePPHColumns.Contains(prop))
                    updateIEPPHData = true;
                if (new[] { "TargetOutputPC", "AdjustOperatorNo", "TypeName", "TCTValue", "IsSigned", "ReferenceModel", "OperatorAdjust", "ReferenceOperator", "FinalOperator", "Notes" }.Contains(prop))
                    updateProductionStages = true;
                if (new[] { "ArticleName", "ModelName" }.Contains(prop))
                    updateArticles = true;
            }

            try
            {
                if (updateIEPPHData)
                {
                    if (!iePPHData_DAL.UpdateIEPPHData_IE_PPH_Data_Part(currentItem))
                    {
                        throw new Exception($"Không thể cập nhật dữ liệu IE_PPH_Data cho IEID = {currentItem.IEID}.");
                    }
                }

                if (updateProductionStages)
                {
                    if (changedProps.Contains("TypeName"))
                    {
                        var typeID = iePPHData_DAL.GetTypeIDByTypeName(currentItem.TypeName);
                        if (typeID == null)
                        {
                            throw new Exception($"Không tìm thấy TypeID cho TypeName = {currentItem.TypeName}.");
                        }
                        currentItem.TypeID = typeID.Value;
                    }

                    if (currentItem.ProcessID == 0)
                    {
                        var processID = iePPHData_DAL.GetProcessID(currentItem.Process);
                        if (processID == null)
                        {
                            throw new Exception($"Không tìm thấy ProcessID cho Process = {currentItem.Process}.");
                        }
                        currentItem.ProcessID = processID.Value;
                    }

                    if (!iePPHData_DAL.UpdateIEPPHData_Production_Stages_Part(currentItem))
                    {
                        throw new Exception($"Không thể cập nhật dữ liệu Production_Stages cho IEID = {currentItem.IEID}.");
                    }
                }

                if (updateArticles)
                {
                    if (!iePPHData_DAL.UpdateIEPPHData_Articles_Part(currentItem))
                    {
                        throw new Exception($"Không thể cập nhật dữ liệu Articles cho IEID = {currentItem.IEID}.");
                    }
                }

                // Cập nhật bản gốc
                var index = ieTotalListOriginalClone.FindIndex(x => x.IEID == currentItem.IEID);
                if (index >= 0)
                {
                    ieTotalListOriginalClone[index] = IETotal.Clone(currentItem);
                }

                // Cập nhật danh sách chính
                if (updateIEPPHData)
                {
                    foreach (var item in ieTotalList.Where(x => x.IEID == currentItem.IEID))
                    {
                        if (changedProps.Contains("PersonIncharge"))
                            item.PersonIncharge = currentItem.PersonIncharge;
                        if (changedProps.Contains("NoteForPC"))
                            item.NoteForPC = currentItem.NoteForPC;
                        if (changedProps.Contains("PCSend"))
                            item.PCSend = currentItem.PCSend;
                        if (changedProps.Contains("OutsourcingAssemblingBool"))
                            item.OutsourcingAssemblingBool = currentItem.OutsourcingAssemblingBool;
                        if (changedProps.Contains("OutsourcingStitchingBool"))
                            item.OutsourcingStitchingBool = currentItem.OutsourcingStitchingBool;
                        if (changedProps.Contains("OutsourcingStockFittingBool"))
                            item.OutsourcingStockFittingBool = currentItem.OutsourcingStockFittingBool;
                        if (changedProps.Contains("Status"))
                            item.Status = currentItem.Status;
                    }
                }

                dgvIEPPH.RefreshData();

                Debug.WriteLine($"✅ Đã cập nhật thành công IEID = {currentItem.IEID}, các trường thay đổi: {string.Join(", ", changedProps)}");
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"❌ Lỗi khi cập nhật dữ liệu IEID = {currentItem?.IEID}.\nChi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine("❌ Exception khi cập nhật: " + ex.ToString());
            }
        }
        private void dgvIEPPH_CellMerge(object sender, CellMergeEventArgs e)
        {
            if (mergeByArticleCols.Contains(e.Column.FieldName))
            {
                var val1 = dgvIEPPH.GetRowCellValue(e.RowHandle1, "ArticleName")?.ToString();
                var val2 = dgvIEPPH.GetRowCellValue(e.RowHandle2, "ArticleName")?.ToString();
                e.Merge = val1 == val2;
            }
            else
            {
                e.Merge = false;
            }

            e.Handled = true;
        }

        private float currentZoomFactor = 1.0f;
        private const float MinZoom = 0.6f;
        private const float MaxZoom = 2.0f;

        private void dgvIEPPH_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                float zoomStep = e.Delta > 0 ? 1.1f : 0.9f;
                currentZoomFactor *= zoomStep;

                currentZoomFactor = Math.Max(MinZoom, Math.Min(MaxZoom, currentZoomFactor));
                GridViewHelper.ZoomGrid(dgvIEPPH, currentZoomFactor);
            }
        }
        private void dgvIEPPH_CustomDrawCell(object sender, RowCellCustomDrawEventArgs e)
        {
            if (e.RowHandle < 0 || e.Column.VisibleIndex < 0)
                return;

            if (e.Column.VisibleIndex >= 9)
            {
                bool isEvenRow = e.RowHandle % 2 == 0;

                Color backColor = isEvenRow ? Color.White : Color.AliceBlue;

                e.Appearance.BackColor = backColor;
            }
        }

        private void dgvIEPPH_ShowingPopupEditForm(object sender, ShowingPopupEditFormEventArgs e)
        {
            // Lấy form popup edit
            Form editForm = e.EditForm;
            if (editForm == null) return;

            editForm.StartPosition = FormStartPosition.Manual;

            // Tính toán vị trí giữa màn hình
            Rectangle screenBounds = Screen.FromControl(editForm).Bounds;
            int x = screenBounds.Left + (screenBounds.Width - editForm.Width) / 2;
            int y = screenBounds.Top + (screenBounds.Height - editForm.Height) / 2;

            // Cập nhật vị trí
            editForm.Location = new Point(x, y);
        }
    }
}


