using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Excel;
using KpiApplication.Forms;
using KpiApplication.Models;
using KpiApplication.Services;
using KpiApplication.Utils;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucTCTData : XtraUserControl, ISupportLoadAsync
    {
        private BindingList<TCTData_Model> tctData_Models = new BindingList<TCTData_Model>();
        private BindingList<TCTData_Pivoted> pivotedDataOriginal;

        private List<TCTData_Pivoted> pivotedDataSnapshot;

        private Dictionary<string, TCTData_Pivoted> snapshotLookup;
        private HashSet<string> modelTypeKeySet = new HashSet<string>();

        public ucTCTData()
        {
            InitializeComponent();
            ApplyLocalizedText();
        }
        private void ApplyLocalizedText()
        {
            btnExport.Caption = Lang.Export;
            btnImport.Caption = Lang.Import;
        }

        private void LoadDataToGrid(BindingList<TCTData_Pivoted> data, bool editable)
        {
            gridControl.DataSource = data;
            TCTGridHelper.ApplyGridSettings(dgvTCT, editable);
            TCTGridHelper.SetCaptions(dgvTCT);
            TCTGridHelper.AutoAdjustColumnWidths(dgvTCT);
        }
        public async Task LoadDataAsync()
        {
            try
            {
                UseWaitCursor = true;

                var result = await Task.Run(() => TCTService.GetAllTCTData());

                tctData_Models = new BindingList<TCTData_Model>(result);

                var pivotedData = TCTService.Pivot(result);
                (pivotedDataOriginal, pivotedDataSnapshot) = TCTService.CreatePivotSnapshot(pivotedData);

                modelTypeKeySet = TCTService.BuildModelTypeKeySet(pivotedDataSnapshot);
                snapshotLookup = TCTService.BuildSnapshotLookup(pivotedDataSnapshot);

                LoadDataToGrid(pivotedDataOriginal, true);

                SetupColumnComboBox("Type", new[]
                {
            "Production Trial", "First Production", "Mass Production"
        });

                ConfigureGridAfterDataBinding();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"{Lang.LoadDataFailed}", ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
        private void dgvTCT_RowUpdated(object sender, RowObjectEventArgs e)
        {
            if (!(e.Row is TCTData_Pivoted updatedRow)) return;

            if (!IsRowValid(updatedRow))
            {
                MessageBoxHelper.ShowWarning("ModelName và Type là bắt buộc.");
                return;
            }

            HandleInsertOrUpdate(updatedRow);
        }

        private bool IsRowValid(TCTData_Pivoted row)
        {
            return !(string.IsNullOrWhiteSpace(row.ModelName) || string.IsNullOrWhiteSpace(row.Type));
        }

        private void HandleInsertOrUpdate(TCTData_Pivoted updatedRow)
        {
            string updatedBy = Global.CurrentEmployee.Username;
            string newKey = TCTService.MakeKey(updatedRow.ModelName, updatedRow.Type);
            string oldKey = TCTService.MakeKey(updatedRow.OriginalModelName, updatedRow.OriginalType);

            bool isNewRow = !modelTypeKeySet.Contains(newKey);
            var oldRow = TCTService.GetSnapshotRow(snapshotLookup, updatedRow.OriginalModelName, updatedRow.OriginalType);

            if (isNewRow || oldRow == null)
            {
                InsertRowAsync(updatedRow, updatedBy);
                modelTypeKeySet.Add(newKey);
            }
            else if (TCTService.HasTCTChanged(oldRow, updatedRow))
            {
                UpdateRowAsync(updatedRow, oldRow, updatedBy);
                TCTService.UpdateSnapshotRow(snapshotLookup, pivotedDataSnapshot, updatedRow);

                if (oldKey != newKey)
                {
                    modelTypeKeySet.Remove(oldKey);
                    modelTypeKeySet.Add(newKey);
                }
            }

            updatedRow.OriginalModelName = updatedRow.ModelName;
            updatedRow.OriginalType = updatedRow.Type;
        }
        private void InsertRowAsync(TCTData_Pivoted row, string updatedBy)
        {
            Task.Run(() =>
            {
                try
                {
                    TCTService.Insert(row, updatedBy);
                    Invoke((MethodInvoker)(() =>
                    {
                        if (!pivotedDataOriginal.Any(x => x.ModelName == row.ModelName && x.Type == row.Type))
                            pivotedDataOriginal.Add(row.Clone());

                        dgvTCT.RefreshData();
                        MessageBoxHelper.ShowInfo(Lang.Inserted);
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((MethodInvoker)(() => MessageBoxHelper.ShowError(Lang.InsertArticleFailed, ex)));
                }
            });
        }
        private void UpdateRowAsync(TCTData_Pivoted newRow, TCTData_Pivoted oldRow, string updatedBy)
        {
            Task.Run(() =>
            {
                try
                {
                    TCTService.Update(newRow, oldRow, updatedBy, out bool changed);
                    if (!changed) return;

                    Invoke((MethodInvoker)(() =>
                    {
                        dgvTCT.RefreshData();
                        MessageBoxHelper.ShowInfo(Lang.UpdateSuccess);
                    }));
                }
                catch (Exception ex)
                {
                    Invoke((MethodInvoker)(() => MessageBoxHelper.ShowError(Lang.UpdateFailed, ex)));
                }
            });
        }

        private void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
                saveDialog.Title = Lang.ExportTCTTitle;
                saveDialog.FileName = $"TCTData_{DateTime.Now:yyyyMMdd}.xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var originalData = (gridControl.DataSource as IEnumerable<TCTData_Pivoted>)?.ToList();

                        var filteredData = originalData
                            .Where(x => !TCTService.IsAllFieldsNull(x))
                            .ToList();

                        gridControl.DataSource = filteredData;

                        dgvTCT.ExportToXlsx(saveDialog.FileName);

                        gridControl.DataSource = originalData;

                        MessageBoxHelper.ShowInfo(Lang.ExportSuccess);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError(Lang.ExportFailed, ex);
                    }
                }
            }
        }

        private void SetupColumnComboBox(string columnName, string[] items)
        {
            var col = dgvTCT.Columns[columnName];
            if (col == null) return;

            string comboName = $"combo_{columnName}";
            var existing = gridControl.RepositoryItems.Cast<RepositoryItem>()
                .FirstOrDefault(r => r.Name == comboName);

            if (existing != null)
            {
                col.ColumnEdit = existing;
                return;
            }

            var combo = new RepositoryItemComboBox
            {
                Name = comboName,
                TextEditStyle = TextEditStyles.DisableTextEditor
            };
            combo.Items.AddRange(items);

            gridControl.RepositoryItems.Add(combo);
            col.ColumnEdit = combo;

            col.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.True;
            col.OptionsEditForm.UseEditorColRowSpan = false;
        }

        private void ConfigureGridAfterDataBinding()
        {
            ColumnsReadOnlyInEditForm();
            GridViewHelper.EnableCopyFunctionality((GridView)this.dgvTCT);
            GridViewHelper.HideColumns(dgvTCT,
                "OriginalModelName", "OriginalType");

            GridViewHelper.SetColumnCaptions(dgvTCT, new Dictionary<string, string>
            {
                ["Type"] = Lang.Type,
                ["Cutting"] = Lang.Cutting,
                ["Stitching"] = Lang.Stitching,
                ["Assembly"] = Lang.Assembly,
                ["StockFitting"] = Lang.StockFitting,
                ["TotalTCT"] = Lang.TotalTCT,
                ["LastUpdatedAt"] = Lang.LastUpdatedAt,
                ["Notes"] = Lang.Notes
            });


            foreach (GridColumn column in dgvTCT.Columns)
            {
                if (column.FieldName != "Notes")
                    column.BestFit();
            }

            // Tính tổng chiều rộng
            int totalColumnWidth = dgvTCT.Columns
                .Where(c => c.Visible)
                .Sum(c => c.Width);

            int viewWidth = dgvTCT.ViewRect.Width - SystemInformation.VerticalScrollBarWidth;
            int remaining = viewWidth - totalColumnWidth;

            if (remaining > 0)
            {
                var stretchColumn = dgvTCT.Columns["Notes"];
                if (stretchColumn != null)
                    stretchColumn.Width += remaining;
            }
        }
        private void ColumnsReadOnlyInEditForm()
        {
            string[] readOnlyColumns = { "TotalTCT", "LastUpdatedAt" };

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
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openDialog.ShowDialog() != DialogResult.OK)
                    return;

                string filePath = openDialog.FileName;
                List<string> errors = new List<string>();
                List<TCTImport_Model> unpivotedList = new List<TCTImport_Model>();

                try
                {
                    // Lấy danh sách sheet
                    List<string> sheetNames = null;
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        sheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                    }

                    // Hiển thị form chọn sheet
                    string selectedSheet = null;
                    using (var form = new SheetSelectionForm(sheetNames))
                    {
                        if (form.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(form.SelectedSheet))
                            return;

                        selectedSheet = form.SelectedSheet;
                    }

                    int updated = 0, inserted = 0;

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        () => Task.Run(() =>
                        {
                            var tctItems = ExcelImporter.ReadTCTItemsFromExcel(filePath, selectedSheet, out errors);

                            foreach (var item in tctItems)
                            {
                                if (item == null || string.IsNullOrWhiteSpace(item.ModelName))
                                    continue;

                                TCTService.AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Cutting", item.Cutting);
                                TCTService.AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Stitching", item.Stitching);
                                TCTService.AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Assembly", item.Assembly);
                                TCTService.AddIfHasValue(unpivotedList, item.ModelName, item.Type, "Stock Fitting", item.Stockfitting);
                            }

                            if (unpivotedList.Count == 0)
                                throw new Exception(Lang.NoValidTCTDataFound);

                            string currentUser = Common.Global.CurrentEmployee.Username;
                            var result = TCT_DAL.SaveTCTImportList(unpivotedList, currentUser);
                            updated = result.updated;
                            inserted = result.inserted;
                        }),
                        Lang.Importing
                    );

                    // Thông báo kết quả
                    var sb = new StringBuilder();
                    sb.AppendLine($"✅ {Lang.ImportSuccess}");
                    sb.AppendLine($"📌 {Lang.Updated}: {updated} {Lang.Rows}.");
                    sb.AppendLine($"🆕 {Lang.Inserted}: {inserted} {Lang.Rows}.");

                    if (errors.Count > 0)
                    {
                        sb.AppendLine($"\n⚠️ {Lang.SkippedRows}: {errors.Count}");
                        sb.AppendLine($"\n🔍 {Lang.ErrorDetails}:");
                        sb.AppendLine(string.Join("\n• ", errors.Take(10)));
                    }
                    MessageBoxHelper.ShowInfo(sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError(Lang.ImportFailed, ex);
                }
            }
        }

        private void dgvTCT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                GridView view = sender as GridView;
                int rowHandle = view.FocusedRowHandle;

                if (view.IsNewItemRow(rowHandle)) return;

                var confirm = MessageBoxHelper.ShowConfirm(Lang.ConfirmDelete);
                if (confirm == DialogResult.Yes)
                {
                    view.DeleteRow(rowHandle);
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
                XtraMessageBox.Show($"{Lang.Error} {Lang.DeleteFailed}: {ex.Message}", Lang.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
