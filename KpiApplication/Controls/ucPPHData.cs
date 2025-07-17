using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
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
    public partial class ucPPHData : DevExpress.XtraEditors.XtraUserControl
    {
        private BindingList<IETotal_Model> ieTotalList;
        private List<IETotal_Model> ieTotalListOriginalClone;
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

        public ucPPHData()
        {
            InitializeComponent();
            InitZoomOverlay();
            this.Load += ucViewPPHData_Load;

            dgvIEPPH.RowUpdated += dgvIEPPH_RowUpdated;
            dgvIEPPH.CellMerge += dgvIEPPH_CellMerge;
            dgvIEPPH.MouseWheel += dgvIEPPH_MouseWheel;
            dgvIEPPH.ShowingPopupEditForm += dgvIEPPH_ShowingPopupEditForm;

            mergeByArticleCols = new HashSet<string> {
        "ArticleName", "ModelName", "PCSend", "PersonIncharge", "OutsourcingAssemblingBool",
        "NoteForPC", "OutsourcingStitchingBool", "OutsourcingStockFittingBool", "Status"
    };

        }
        private async void ucViewPPHData_Load(object sender, EventArgs e)
        {
            await LoadDataAsync("Loading...");
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync("Refreshing data...");
        }

        // Common data loading method
        private async Task LoadDataAsync(string splashMessage)
        {
            try
            {
                await AsyncLoaderHelper.LoadDataWithSplashAsync(
                    this,
                    FetchData,
                    data =>
                    {
                        LoadDataToGrid(data);

                        SetupMemoEditColumn("NoteForPC");
                        SetupColumnComboBox("PersonIncharge", x => x.PersonIncharge.Trim());
                        SetupColumnComboBox("Status", x => x.Status);
                        SetupColumnComboBox("TypeName", new[] {
                    "Production Trial",
                    "First Production",
                    "Mass Production"
                        });
                        SetupColumnComboBox("IsSigned", new[] { "Signed", "Not Sign Yet" });
                        SetupColumnComboBox("Process", iePPHData_DAL.GetProcessList());

                        SetColumnOrder();
                        ApplyColumnAlignment();
                        SetColumnCaptions();

                        ConfigureGridAfterDataBinding();
                    },
                    splashMessage
                );
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("An error occurred while loading data", ex);
            }
        }
        private void SetupColumnComboBox(string columnName, List<string> items)
        {
            SetupColumnComboBox(columnName, items.ToArray());
        }

        private void SetupColumnComboBox(string columnName, Func<IETotal_Model, string> selector)
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

        private void SetupColumnComboBox(string columnName, string[] items)
        {
            var col = dgvIEPPH.Columns[columnName];
            if (col == null) return;

            var combo = new RepositoryItemComboBox
            {
                TextEditStyle = TextEditStyles.DisableTextEditor
            };
            combo.Items.Clear();
            combo.Items.AddRange(items);

            gridControl1.RepositoryItems.Add(combo);
            col.ColumnEdit = combo;

            col.OptionsEditForm.Visible = DevExpress.Utils.DefaultBoolean.True;
            col.OptionsEditForm.UseEditorColRowSpan = false;
        }
        private void ConfigureGridAfterDataBinding()
        {
            ColumnsReadOnlyInEditForm();
            ConfigureGridView();
            GridViewHelper.EnableCopyFunctionality((GridView)this.dgvIEPPH);
            GridViewHelper.ApplyDefaultFormatting(this.dgvIEPPH);
        }

        private BindingList<IETotal_Model> FetchData()
        {
            var data = iePPHData_DAL.GetIEPPHData();
            ieTotalListOriginalClone = data.Select(IETotal_Model.Clone).ToList();
            return data;
        }

        private void LoadDataToGrid(BindingList<IETotal_Model> data)
        {
            ieTotalList = data;
            gridControl1.DataSource = ieTotalList;
        }

        private async void btnExport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (ieTotalList == null || ieTotalList.Count == 0)
            {
                MessageBoxHelper.ShowInfo("No data to export.");
                return;
            }

            var result = MessageBoxHelper.ShowConfirm("Do you want to include the TCT column in the export?", "Excel Export Options");
            bool includeTCT = (result == DialogResult.Yes);

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.FileName = "PPHData.xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    string originalFilePath = saveDialog.FileName;
                    string filePath = originalFilePath;

                    int count = 1;
                    string dir = Path.GetDirectoryName(originalFilePath);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilePath);
                    string ext = Path.GetExtension(originalFilePath);

                    while (File.Exists(filePath))
                    {
                        filePath = Path.Combine(dir, $"{fileNameWithoutExt}_{count}{ext}");
                        count++;
                    }

                    await AsyncLoaderHelper.LoadDataWithSplashAsync(
                        this,
                        () =>
                        {
                            ExcelExporter.ExportIETotalPivoted(ieTotalList.ToList(), filePath, includeTCT);
                            return true;
                        },
                        _ => { },
                        "Exporting..."
                    );

                    MessageBoxHelper.ShowInfo("Excel export completed successfully!");

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
            string[] readOnlyColumns = { "ArticleName" };

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
        private void SetColumnCaptions()
        {
            GridViewHelper.SetColumnCaptions(dgvIEPPH, new Dictionary<string, string>()
            {
                ["IEPPHValue"] = "IE PPH",
                ["IsSigned"] = "Production Sign",
                ["OutsourcingAssemblingBool"] = "Outsourcing\nAssembling",
                ["OutsourcingStitchingBool"] = "Outsourcing\nStitching",
                ["OutsourcingStockFittingBool"] = "Outsourcing\nStockFitting",
                ["AdjustOperatorNo"] = "Operator",
                ["TargetOutputPC"] = "Target\nOutput",
                ["ProductionSign"] = "Production\nSign",
                ["THTValue"] = "THT",
                ["ReferenceModel"] = "Reference\nModel",
                ["OperatorAdjust"] = "Operator\nAdjust",
                ["ReferenceOperator"] = "Reference\nOperator",
                ["FinalOperator"] = "Final\nOperator",
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
            foreach (GridColumn col in dgvIEPPH.Columns)
            {
                if (col.FieldName != "NoteForPC" && col.FieldName != "ModelName")
                {
                    col.BestFit();
                }
                else
                {
                    col.Width = 220;
                    col.OptionsColumn.FixedWidth = true;
                }
            }
        }
        private async void dgvIEPPH_RowUpdated(object sender, RowObjectEventArgs e)
        {
            var current = e.Row as IETotal_Model;
            if (current == null)
            {
                MessageBoxHelper.ShowError("❌ The current row data is invalid.");
                return;
            }

            try
            {
                ProcessRowUpdate(current);

                dgvIEPPH.RefreshData();

                await LoadDataAsync("Refreshing data...");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError($"❌ Error while saving data.\nDetails: {ex.Message}");
                Debug.WriteLine($"❌ Exception: {ex}");
            }
        }
        private void ProcessRowUpdate(IETotal_Model current)
        {
            var original = FindOriginal(current);

            SetProcessID(current);
            SetTypeID(current);

            if (original == null)
            {
                HandleNewRecord(current);
            }
            else
            {
                HandleUpdatedRecord(current, original);
            }

            UpdateOriginalClone(current);
        }
        private void HandleNewRecord(IETotal_Model item)
        {
            if (!EnsureArticleDependenciesInserted(item))
                throw new Exception("❌ Unable to insert new record due to missing dependent data.");

            EnsureTCTInsertedOrUpdated(item);

            Debug.WriteLine($"✅ New record inserted: ArticleID = {item.ArticleID}.");
        }
        private void HandleUpdatedRecord(IETotal_Model current, IETotal_Model original)
        {
            string updatedBy = Common.Global.CurrentEmployee.Username;
            DateTime updatedAt = DateTime.UtcNow;

            var changedProps = current.GetChangedProperties(original);

            if (changedProps.Count == 0)
            {
                Debug.WriteLine($"ℹ️ No changes detected for ArticleID = {current.ArticleID}.");
                return;
            }

            if (changedProps.Contains("ModelName"))
            {
                if (!IEPPHData_DAL.Update_ArticleModelName(current.ArticleID, current.ModelName))
                {
                    throw new Exception($"Failed to update ModelName for ArticleID = {current.ArticleID}.");
                }
                Debug.WriteLine($"✏️ ModelName updated for ArticleID = {current.ArticleID} to '{current.ModelName}'.");
            }

            Debug.WriteLine("🔄 Modified properties: " + string.Join(", ", changedProps));

            if (changedProps.Contains("TypeName")) SetTypeID(current);
            if (changedProps.Contains("Process")) SetProcessID(current);

            var updateActions = new (Func<IETotal_Model, bool> updateFunc, string[] relatedProps, string errorMessage)[]
            {
        (
            item => iePPHData_DAL.Update_ArticleProcessTypeData(item, updatedBy, updatedAt),
            new[] {
                "TargetOutputPC", "AdjustOperatorNo", "TypeName", "TCTValue", "IsSigned", "Process",
                "ReferenceModel", "OperatorAdjust", "ReferenceOperator", "FinalOperator", "Notes"
            },
            $"Failed to update ArticleProcessTypeData for ArticleID = {current.ArticleID}."
        ),
        (
            iePPHData_DAL.Update_ArticlePCIncharge,
            new[] { "PersonIncharge", "PCSend" },
            $"Failed to update Article_PCIncharge for ArticleID = {current.ArticleID}."
        ),
        (
            iePPHData_DAL.Update_ArticleOutsourcing,
            new[] { "OutsourcingStitchingBool", "OutsourcingAssemblingBool", "OutsourcingStockFittingBool", "NoteForPC", "Status" },
            $"Failed to update Article_Outsourcing for ArticleID = {current.ArticleID}."
        ),
        (
            item =>
            {
                EnsureTCTInsertedOrUpdated(item);
                return true;
            },
            new[] { "TCTValue", "ModelName", "TypeName", "Process" },
            $"Failed to update TCT for ArticleID = {current.ArticleID}."
        )
            };

            foreach (var (updateFunc, props, errorMessage) in updateActions)
            {
                if (changedProps.Intersect(props).Any())
                {
                    if (!updateFunc(current))
                        throw new Exception(errorMessage);
                }
            }

            Debug.WriteLine($"✅ Record updated successfully: ArticleID = {current.ArticleID}.");
        }
        private void EnsureTCTInsertedOrUpdated(IETotal_Model item)
        {
            if (string.IsNullOrWhiteSpace(item.ModelName) ||
                string.IsNullOrWhiteSpace(item.TypeName) ||
                string.IsNullOrWhiteSpace(item.Process))
            {
                Debug.WriteLine("⚠️ Missing information: unable to insert or update TCT.");
                return;
            }

            string normModel = Normalize(item.ModelName);
            string normType = Normalize(item.TypeName);
            string normProcess = Normalize(item.Process);
            string updatedBy = Common.Global.CurrentEmployee.Username;

            double? tctValue = item.TCTValue;

            if (!iePPHData_DAL.Exists_TCTData(normModel, normType, normProcess))
            {
                iePPHData_DAL.Insert_TCTData(item.ModelName, item.TypeName, item.Process, tctValue);
                Debug.WriteLine($"➕ Inserted TCT: Model = {item.ModelName}, Process = {item.Process}");
            }
            else
            {
                iePPHData_DAL.Update_TCTData(item.ModelName, item.TypeName, item.Process, tctValue, updatedBy);
                Debug.WriteLine($"🔁 Updated TCT: Model = {item.ModelName}, Process = {item.Process}");
            }
        }
        private bool EnsureArticleDependenciesInserted(IETotal_Model item)
        {
            bool inserted = false;

            if (item.ArticleID <= 0)
            {
                MessageBoxHelper.ShowError("❌ Invalid ArticleID.");
                return false;
            }

            if (!iePPHData_DAL.Exists_ArticlePCIncharge(item.ArticleID))
            {
                iePPHData_DAL.Insert_ArticlePCIncharge(item);
                inserted = true;
                Debug.WriteLine($"➕ Inserted Article_PCIncharge for ArticleID = {item.ArticleID}.");
            }

            if (!iePPHData_DAL.Exists_ArticleOutsourcing(item.ArticleID))
            {
                iePPHData_DAL.Insert_ArticleOutsourcing(item);
                inserted = true;
                Debug.WriteLine($"➕ Inserted Article_Outsourcing for ArticleID = {item.ArticleID}.");
            }

            if (item.ProcessID.HasValue && item.TypeID.HasValue)
            {
                if (!iePPHData_DAL.Exists_ArticleProcessType(item.ArticleID, item.ProcessID.Value, item.TypeID.Value))
                {
                    iePPHData_DAL.Insert_ArticleProcessTypeData(item);
                    inserted = true;
                    Debug.WriteLine($"➕ Inserted ArticleProcessType for ArticleID = {item.ArticleID}, ProcessID = {item.ProcessID}, TypeID = {item.TypeID}.");
                }
            }
            else
            {
                Debug.WriteLine($"⚠️ Missing ProcessID or TypeID for ArticleID = {item.ArticleID}.");
            }

            return inserted;
        }

        private void UpdateOriginalClone(IETotal_Model item)
        {
            var index = ieTotalListOriginalClone.FindIndex(x =>
                x.ArticleID == item.ArticleID &&
                x.ProcessID == item.ProcessID &&
                x.TypeID == item.TypeID);

            if (index >= 0)
            {
                ieTotalListOriginalClone[index] = IETotal_Model.Clone(item);
            }
            else
            {
                ieTotalListOriginalClone.Add(IETotal_Model.Clone(item));
            }
        }
        private IETotal_Model FindOriginal(IETotal_Model item)
        {
            return ieTotalListOriginalClone.FirstOrDefault(x =>
                x.ArticleID == item.ArticleID &&
                x.ProcessID == item.ProcessID &&
                x.TypeID == item.TypeID);
        }


        private string Normalize(string value) => value?.Trim().ToUpper() ?? string.Empty;


        private void SetTypeID(IETotal_Model item)
        {
            if (!string.IsNullOrWhiteSpace(item.TypeName))
            {
                var typeID = iePPHData_DAL.GetTypeID(item.TypeName);
                if (typeID == null)
                    throw new Exception($"TypeID not found for TypeName = {item.TypeName}.");

                item.TypeID = typeID.Value;
            }
            else
            {
                item.TypeID = null;
            }
        }

        private void SetProcessID(IETotal_Model item)
        {
            if (!string.IsNullOrWhiteSpace(item.Process))
            {
                var processID = iePPHData_DAL.GetProcessID(item.Process);
                if (processID == null)
                    throw new Exception($"ProcessID not found for Process = {item.Process}.");

                item.ProcessID = processID.Value;
            }
            else
            {
                item.ProcessID = null;
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
        private Label zoomOverlayLabel;
        private Timer zoomOverlayTimer;
        private void InitZoomOverlay()
        {
            zoomOverlayLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(180, Color.LightGray),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14),
                Visible = false
            };

            zoomOverlayLabel.BringToFront();
            this.Controls.Add(zoomOverlayLabel);

            zoomOverlayTimer = new Timer { Interval = 1000 };
            zoomOverlayTimer.Tick += (s, e) =>
            {
                zoomOverlayLabel.Visible = false;
                zoomOverlayTimer.Stop();
            };
        }
        private void ApplyZoom(float zoom)
        {
            var oldFont = dgvIEPPH.Appearance.Row.Font;
            float newFontSize = oldFont.Size * zoom / currentZoomFactor;

            if (Math.Abs(newFontSize - oldFont.Size) < 0.1f)
                return;

            Font zoomedFont = new Font(oldFont.FontFamily, newFontSize, oldFont.Style);

            dgvIEPPH.BeginUpdate();
            try
            {
                dgvIEPPH.Appearance.Row.Font = zoomedFont;
                dgvIEPPH.Appearance.HeaderPanel.Font = zoomedFont;
                dgvIEPPH.Appearance.FooterPanel.Font = zoomedFont;
                dgvIEPPH.Appearance.GroupRow.Font = zoomedFont;
                dgvIEPPH.Appearance.GroupFooter.Font = zoomedFont;
                dgvIEPPH.Appearance.Preview.Font = zoomedFont;

                // Thay đổi chiều cao dòng (tùy theo font size)
                dgvIEPPH.RowHeight = (int)(22 * zoom);
            }
            finally
            {
                dgvIEPPH.EndUpdate();
            }

            dgvIEPPH.Invalidate();
        }

        private void dgvIEPPH_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                float zoomStep = e.Delta > 0 ? 1.05f : 0.95f;
                float newZoom = currentZoomFactor * zoomStep;
                newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));

                if (Math.Abs(newZoom - currentZoomFactor) > 0.01f)
                {
                    ApplyZoom(newZoom);
                    ShowZoomOverlay((int)(newZoom * 100));
                    currentZoomFactor = newZoom;
                }
            }
        }
        private void ShowZoomOverlay(int zoomPercent)
        {
            if (zoomOverlayLabel == null) return;

            zoomOverlayLabel.Text = $"Zoom: {zoomPercent}%";
            zoomOverlayLabel.Size = new Size(200, 60);

            // Canh giữa form
            zoomOverlayLabel.Location = new Point(
                (this.Width - zoomOverlayLabel.Width) / 2,
                (this.Height - zoomOverlayLabel.Height) / 2
            );

            zoomOverlayLabel.Visible = true;
            zoomOverlayLabel.BringToFront();
            zoomOverlayTimer.Stop();
            zoomOverlayTimer.Start();
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

        private void dgvIEPPH_KeyDown(object sender, KeyEventArgs e)
        {
            bool isDelete = e.KeyCode == Keys.Delete;
            bool isCtrlMinus = e.Control && e.KeyCode == Keys.OemMinus;

            if (isDelete || isCtrlMinus)
            {
                GridView view = sender as GridView;
                int rowHandle = view.FocusedRowHandle;

                if (rowHandle >= 0 && !view.IsNewItemRow(rowHandle))
                {
                    string focusedColumn = view.FocusedColumn?.FieldName;
                    if (focusedColumn != null)
                    {
                        int focusedIndex = Array.IndexOf(desiredColumnOrder, focusedColumn);
                        int statusIndex = Array.IndexOf(desiredColumnOrder, "DataStatus");

                        if (focusedIndex >= statusIndex)
                        {
                            var confirm = MessageBoxHelper.ShowConfirm(
                                "Are you sure you want to delete this row?",
                                "Confirmation"
                            );

                            if (confirm == DialogResult.Yes)
                            {
                                view.DeleteRow(rowHandle);
                            }
                        }
                        else
                        {
                            MessageBoxHelper.ShowWarning(
                                "❌ You are not allowed to delete from this column.\nPlease select a cell from 'DataStatus' column or later."
                            );
                        }
                    }
                }
            }

            if (e.Control && e.KeyCode == Keys.F)
            {
                dgvIEPPH.ShowFindPanel();
                e.Handled = true;
            }
        }

        private void dgvIEPPH_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e)
        {
            try
            {
                if (e.Row is IETotal_Model deletedRow)
                {
                    IEPPHData_DAL.DeletePPH(deletedRow.ArticleID, deletedRow.ProcessID, deletedRow.TypeID);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("❌ Error while deleting row", ex);
            }
        }
        public void ShowFind()
        {
            dgvIEPPH.ShowFindPanel();
            dgvIEPPH.Focus();
        }


        private void ucViewPPHData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                dgvIEPPH.ShowFindPanel();
                dgvIEPPH.Focus();
                e.Handled = true;
            }
        }
    }
}


