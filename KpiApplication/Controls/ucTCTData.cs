using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Models;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucTCTData : XtraUserControl
    {
        private List<TCTData_Model> tctData_Models = new List<TCTData_Model>();
        private List<TCTData_Pivoted> pivotedDataOriginal;

        public ucTCTData()
        {
            InitializeComponent();
        }

        private void LoadDataToGrid(List<TCTData_Pivoted> data)
        {
            gridControl.DataSource = data;

            GridViewHelper.ApplyDefaultFormatting(dgvTCT);
            GridViewHelper.EnableWordWrapForGridView(dgvTCT);
            GridViewHelper.AdjustGridColumnWidthsAndRowHeight(dgvTCT);
            GridViewHelper.EnableCopyFunctionality(dgvTCT);

            dgvTCT.BestFitColumns();
        }
        private async Task LoadDataAsync()
        {
            await AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () => TCT_DAL.GetAllTCTData(),
                result =>
                {
                    tctData_Models = result;
                    var pivotedData = PivoteHelper.PivotTCTData(tctData_Models);
                    pivotedDataOriginal = pivotedData.Select(x => x.Clone()).ToList();
                    LoadDataToGrid(pivotedData);
                    SetupColumnComboBox("Type", new[] {
    "Production Trial",
    "First Production",
    "Mass Production"
});

                    ConfigureGridAfterDataBinding();
                },
                "Loading..."
            );
        }
        private void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
                saveDialog.Title = "Xuất TCT Data ra Excel";
                saveDialog.FileName = $"TCTData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 1. Lưu dữ liệu hiện tại
                        var originalData = gridControl.DataSource as List<TCTData_Pivoted>;

                        // 2. Lọc dữ liệu không hợp lệ
                        var filteredData = originalData
                            .Where(x => !IsAllFieldsNull(x))
                            .ToList();

                        // 3. Gán dữ liệu đã lọc vào lưới
                        gridControl.DataSource = filteredData;

                        // 4. Xuất file Excel
                        dgvTCT.ExportToXlsx(saveDialog.FileName);

                        // 5. Khôi phục lại dữ liệu gốc sau khi xuất
                        gridControl.DataSource = originalData;

                        XtraMessageBox.Show("✅ Xuất Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show($"❌ Lỗi khi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool IsAllFieldsNull(TCTData_Pivoted item)
        {
            return string.IsNullOrWhiteSpace(item.Type)
                && item.Cutting == null
                && item.Stitching == null
                && item.Assembly == null
                && item.StockFitting == null;
        }

        private async void ucTCTData_Load(object sender, EventArgs e)
        {
            await LoadDataAsync();
        }
        private void SetupColumnComboBox(string columnName, string[] items)
        {
            var col = dgvTCT.Columns[columnName];
            if (col == null) return;

            var combo = new RepositoryItemComboBox
            {
                TextEditStyle = TextEditStyles.DisableTextEditor
            };
            combo.Items.Clear();
            combo.Items.AddRange(items);

            gridControl.RepositoryItems.Add(combo);
            col.ColumnEdit = combo;

            col.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.True;
            col.OptionsEditForm.UseEditorColRowSpan = false;
        }

        private void ConfigureGridAfterDataBinding()
        {
            this.dgvTCT.BestFitColumns();
            ColumnsReadOnlyInEditForm();
            GridViewHelper.EnableCopyFunctionality((GridView)this.dgvTCT);
        }
        private void ColumnsReadOnlyInEditForm()
        {
            string[] readOnlyColumns = { "TotalTCT", "TotalTCT", "LastUpdatedAt" };

            foreach (var colName in readOnlyColumns)
            {
                var column = dgvTCT.Columns[colName];
                if (column != null)
                {
                    column.OptionsColumn.AllowEdit = false;
                    column.OptionsColumn.ReadOnly = true;
                    column.ColumnEdit = null;
                }
            }
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
                        () => Task.Run(() =>
                        {
                            List<string> errors;
                            var tctItems = ExcelImporter.ReadTCTItemsFromExcel(filePath, out errors);

                            var unpivotedList = new List<TCTImport_Model>();

                            foreach (var item in tctItems)
                            {
                                if (item.Cutting.HasValue)
                                    unpivotedList.Add(new TCTImport_Model { ModelName = item.ModelName, Type = item.Type, Process = "Cutting", TCT = item.Cutting });

                                if (item.Stitching.HasValue)
                                    unpivotedList.Add(new TCTImport_Model { ModelName = item.ModelName, Type = item.Type, Process = "Stitching", TCT = item.Stitching });

                                if (item.Assembly.HasValue)
                                    unpivotedList.Add(new TCTImport_Model { ModelName = item.ModelName, Type = item.Type, Process = "Assembly", TCT = item.Assembly });

                                if (item.Stockfitting.HasValue)
                                    unpivotedList.Add(new TCTImport_Model { ModelName = item.ModelName, Type = item.Type, Process = "Stock Fitting", TCT = item.Stockfitting });
                            }

                            TCT_DAL.SaveTCTImportList(unpivotedList);

                            Invoke((Action)(() =>
                            {
                                string message = "Lưu dữ liệu thành công!";
                                if (errors.Count > 0)
                                    message += $"\n⚠️ Có {errors.Count} lỗi dữ liệu bị bỏ qua.";

                                XtraMessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }),
                        "Importing..."
                    );
                }
            }
        }

        private void dgvTCT_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            if (e.Row is TCTData_Pivoted updatedRow)
            {
                var view = sender as GridView;
                if (view == null) return;

                var oldRow = pivotedDataOriginal.FirstOrDefault(x =>
                    x.ModelName == updatedRow.ModelName && x.Type == updatedRow.Type);

                if (oldRow == null) return;
                string updatedBy = Common.Global.CurrentEmployee.Username;
                var updatedModels = new List<TCTData_Model>();

                void AddIfChanged(string processName, double? oldValue, double? newValue)
                {
                    if (oldValue != newValue)
                    {
                        updatedModels.Add(new TCTData_Model
                        {
                            ModelName = updatedRow.ModelName,
                            Type = updatedRow.Type,
                            Process = processName,
                            TCTValue = newValue,
                            LastUpdatedAt = DateTime.Now,
                            Notes = updatedRow.Notes
                        });
                    }
                }

                AddIfChanged("Cutting", oldRow.Cutting, updatedRow.Cutting);
                AddIfChanged("Stitching", oldRow.Stitching, updatedRow.Stitching);
                AddIfChanged("Assembly", oldRow.Assembly, updatedRow.Assembly);
                AddIfChanged("Stock Fitting", oldRow.StockFitting, updatedRow.StockFitting);

                if ((updatedRow.Notes ?? "") != (oldRow.Notes ?? "") && updatedModels.Count == 0)
                {
                    updatedModels.Add(new TCTData_Model
                    {
                        ModelName = updatedRow.ModelName,
                        Type = updatedRow.Type,
                        Process = "Cutting", 
                        TCTValue = oldRow.Cutting,
                        LastUpdatedAt = DateTime.Now,
                        Notes = updatedRow.Notes
                    });
                }

                if (updatedModels.Count == 0) return;

                Task.Run(() =>
                {
                    try
                    {
                        foreach (var item in updatedModels)
                        {
                            TCT_DAL.InsertOrUpdateTCT(item, updatedBy);
                        }

                        Invoke(new Action(() =>
                        {
                            updatedRow.LastUpdatedAt = updatedModels.Max(x => x.LastUpdatedAt);
                            dgvTCT.RefreshData();

                            var original = pivotedDataOriginal.First(x =>
                                x.ModelName == updatedRow.ModelName && x.Type == updatedRow.Type);

                            original.Cutting = updatedRow.Cutting;
                            original.Stitching = updatedRow.Stitching;
                            original.Assembly = updatedRow.Assembly;
                            original.StockFitting = updatedRow.StockFitting;
                            original.Notes = updatedRow.Notes;
                            original.LastUpdatedAt = updatedRow.LastUpdatedAt;

                            XtraMessageBox.Show("Cập nhật TCT thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            XtraMessageBox.Show("Lỗi khi cập nhật TCT: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                });
            }
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync();
        }

        private void dgvTCT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                GridView view = sender as GridView;
                int rowHandle = view.FocusedRowHandle;

                if (rowHandle >= 0 && !view.IsNewItemRow(rowHandle))
                {
                    var confirm = XtraMessageBox.Show("Bạn có chắc muốn xóa dòng này?", "Xác nhận", MessageBoxButtons.YesNo);
                    if (confirm == DialogResult.Yes)
                    {
                        view.DeleteRow(rowHandle);
                    }
                }
            }
        }
        private void dgvTCT_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e)
        {
            try
            {
                if (e.Row is TCTData_Pivoted deletedRow)
                {
                    TCT_DAL.DeleteTCT(deletedRow.ModelName, deletedRow.Type);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("❌ Lỗi khi xóa dòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
