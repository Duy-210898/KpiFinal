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
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace KpiApplication.Controls
{
    public partial class ucViewPPHData : DevExpress.XtraEditors.XtraUserControl
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

        public ucViewPPHData()
        {
            InitializeComponent();
            InitZoomOverlay();
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
        private async void ucViewPPHData_Load(object sender, EventArgs e)
        {
            await LoadDataAsync("Loading...");
        }

        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            await LoadDataAsync("Refreshing data...");
        }

        // Hàm Load dữ liệu dùng chung
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
                XtraMessageBox.Show($"Đã xảy ra lỗi khi tải dữ liệu:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string[] readOnlyColumns = { "ArticleName"};

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
                ShowError("❌ Dữ liệu dòng hiện tại không hợp lệ.");
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
                ShowError($"❌ Lỗi khi lưu dữ liệu.\nChi tiết: {ex.Message}");
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
                throw new Exception("❌ Không thể thêm mới do thiếu dữ liệu liên quan.");

            EnsureTCTInsertedOrUpdated(item);

            Debug.WriteLine($"✅ Đã thêm mới ArticleID = {item.ArticleID}.");
        }
        private void HandleUpdatedRecord(IETotal_Model current, IETotal_Model original)
        {
            string updatedBy = Common.Global.CurrentEmployee.Username;
            DateTime updatedAt = DateTime.UtcNow;

            var changedProps = current.GetChangedProperties(original);

            if (changedProps.Count == 0)
            {
                Debug.WriteLine($"ℹ️ Không có thay đổi cho ArticleID = {current.ArticleID}.");
                return;
            }

            if (changedProps.Contains("ModelName"))
            {
                if (!IEPPHData_DAL.Update_ArticleModelName(current.ArticleID, current.ModelName))
                {
                    throw new Exception($"Không thể cập nhật ModelName cho ArticleID = {current.ArticleID}.");
                }
                Debug.WriteLine($"✏️ Đã cập nhật ModelName của ArticleID = {current.ArticleID} thành '{current.ModelName}'.");
            }


            Debug.WriteLine("🔄 Các thuộc tính thay đổi: " + string.Join(", ", changedProps));

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
            $"Không thể cập nhật ArticleProcessTypeData cho ArticleID = {current.ArticleID}."
        ),
        (
            iePPHData_DAL.Update_ArticlePCIncharge,
            new[] { "PersonIncharge", "PCSend" },
            $"Không thể cập nhật Article_PCIncharge cho ArticleID = {current.ArticleID}."
        ),
        (
            iePPHData_DAL.Update_ArticleOutsourcing,
            new[] { "OutsourcingStitchingBool", "OutsourcingAssemblingBool", "OutsourcingStockFittingBool", "NoteForPC", "Status" },
            $"Không thể cập nhật Article_Outsourcing cho ArticleID = {current.ArticleID}."
        ),
        (
            item =>
            {
                EnsureTCTInsertedOrUpdated(item);
                return true;
            },
            new[] { "TCTValue", "ModelName", "TypeName", "Process" },
            $"Không thể cập nhật TCT cho ArticleID = {current.ArticleID}."
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

            Debug.WriteLine($"✅ Đã cập nhật ArticleID = {current.ArticleID}.");
        }
        private void EnsureTCTInsertedOrUpdated(IETotal_Model item)
        {
            if (string.IsNullOrWhiteSpace(item.ModelName) ||
                string.IsNullOrWhiteSpace(item.TypeName) ||
                string.IsNullOrWhiteSpace(item.Process))
            {
                Debug.WriteLine("⚠️ Thiếu thông tin để thêm/cập nhật TCT.");
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
                Debug.WriteLine($"➕ Đã thêm TCT: Model = {item.ModelName}, Process = {item.Process}");
            }
            else
            {
                iePPHData_DAL.Update_TCTData(item.ModelName, item.TypeName, item.Process, tctValue, updatedBy);
                Debug.WriteLine($"🔁 Đã cập nhật TCT: Model = {item.ModelName}, Process = {item.Process}");
            }
        }

        private bool EnsureArticleDependenciesInserted(IETotal_Model item)
        {
            bool inserted = false;

            if (item.ArticleID <= 0)
            {
                ShowError("❌ ArticleID không hợp lệ.");
                return false;
            }

            if (!iePPHData_DAL.Exists_ArticlePCIncharge(item.ArticleID))
            {
                iePPHData_DAL.Insert_ArticlePCIncharge(item);
                inserted = true;
            }

            if (!iePPHData_DAL.Exists_ArticleOutsourcing(item.ArticleID))
            {
                iePPHData_DAL.Insert_ArticleOutsourcing(item);
                inserted = true;
            }

            if (item.ProcessID.HasValue && item.TypeID.HasValue)
            {
                if (!iePPHData_DAL.Exists_ArticleProcessType(item.ArticleID, item.ProcessID.Value, item.TypeID.Value))
                {
                    iePPHData_DAL.Insert_ArticleProcessTypeData(item);
                    inserted = true;
                }
            }
            else
            {
                Debug.WriteLine("⚠️ Thiếu ProcessID hoặc TypeID.");
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
                    throw new Exception($"Không tìm thấy TypeID cho TypeName = {item.TypeName}.");
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
                    throw new Exception($"Không tìm thấy ProcessID cho Process = {item.Process}.");
                item.ProcessID = processID.Value;
            }
            else
            {
                item.ProcessID = null;
            }
        }

        private void ShowError(string message)
        {
            XtraMessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        // Chỉ cho phép xóa khi đang chọn ô từ cột "DataStatus" trở về sau
                        if (focusedIndex >= statusIndex)
                        {
                            var confirm = MessageBox.Show("Bạn có chắc muốn xóa dòng này?", "Xác nhận", MessageBoxButtons.YesNo);
                            if (confirm == DialogResult.Yes)
                            {
                                view.DeleteRow(rowHandle);
                            }
                        }
                        else
                        {
                            MessageBox.Show("❌ Không được phép xóa dòng từ cột này.\nVui lòng chọn ô từ cột 'DataStatus' trở về sau.", "Cảnh báo");
                        }
                    }
                }
                if (e.Control && e.KeyCode == Keys.F)
                {
                    dgvIEPPH.ShowFindPanel();
                    e.Handled = true;
                }
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
                MessageBox.Show("❌ Lỗi khi xóa dòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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


