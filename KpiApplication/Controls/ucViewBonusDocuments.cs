using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.Forms;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucViewBonusDocuments : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private readonly DocumentServices _docService = new DocumentServices();
        private MemoryStream currentStream;
        private BonusDocument_Model currentViewingDoc;
        private string previousModel;
        private BindingList<BonusDocument_Model> _documentBindingList;

        public ucViewBonusDocuments()
        {
            InitializeComponent();
            InitializeControls();
            ApplyLocalizedText();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible || IsDisposed || currentViewingDoc == null || currentStream != null)
                return;

            try
            {
                ReloadCurrentDocument();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Lỗi khi hiển thị tài liệu", ex);
            }
        }

        private void InitializeControls()
        {
            pictureViewer.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;

            btnExportFile.Enabled = false;
        }

        private void ApplyLocalizedText()
        {
            btnExportFile.Text = Lang.Export;
            lookUpModelName.Properties.NullText = Lang.SelectModel;
            layoutControlItem1.Text = Lang.ModelName;
        }

        public async Task LoadDataAsync()
        {
            await LoadModels(reset: true);
        }
        private async Task LoadModels(bool reset = false)
        {
            try
            {
                UseWaitCursor = true;

                var modelNames = await Task.Run(() => _docService.GetModelNames());

                if (reset)
                {
                    lookUpModelName.EditValue = null;
                    ResetViewer();
                    gridControl1.DataSource = null;
                    _docService.ClearCache();
                    previousModel = null;
                }

                lookUpModelName.Properties.DataSource = modelNames;
                SetupTooltipController();
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
        private async void gridView1_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            var selectedDoc = GetSelectedDocument();
            await LoadAndDisplayDocumentAsync(selectedDoc);
        }

        private async Task LoadAndDisplayDocumentAsync(BonusDocument_Model selectedDoc)
        {
            if (selectedDoc == null || currentViewingDoc?.Id == selectedDoc.Id)
                return;

            try
            {
                SetUiStateDuringLoading(false);
                var data = await LoadDocumentDataAsync(selectedDoc.Id);

                if (DisplaySelectedDocument(selectedDoc, data))
                {
                    currentViewingDoc = selectedDoc;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Lỗi khi tải tài liệu", ex);
            }
            finally
            {
                SetUiStateDuringLoading(true);
                UseWaitCursor = false;
            }
        }
        private void ReloadCurrentDocument()
        {
            var data = _docService.GetDocumentBytesWithCache(currentViewingDoc.Id);
            if (data == null || data.Length == 0) return;

            ResetViewer();

            if (!DisplaySelectedDocument(currentViewingDoc, data))
            {
                return;
            }
        }
        private bool DisplaySelectedDocument(BonusDocument_Model doc, byte[] data)
        {
            // Dùng đuôi gốc nếu có, fallback sang FileName
            string extension = !string.IsNullOrWhiteSpace(doc.FileExtension)
                ? doc.FileExtension
                : Path.GetExtension(doc.FileName);

            bool success = DocumentViewerHelper.DisplayDocument(
                this,
                pdfViewer,
                pictureViewer,
                lblFileName,
                doc,
                data,
                ref currentStream,
                _docService.ImageCache,
                out var errorMessage);

            if (!success && !string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBoxHelper.ShowWarning(errorMessage);
            }

            return success;
        }
        private void SetUiStateDuringLoading(bool enable)
        {
            btnExportFile.Enabled = enable;
            UseWaitCursor = !enable;
        }

        private BonusDocument_Model GetSelectedDocument()
        {
            return gridView1.GetFocusedRow() as BonusDocument_Model;
        }

        private void ShowColumns(GridView view, params string[] visibleColumns)
        {
            foreach (GridColumn col in view.Columns)
                col.Visible = visibleColumns.Contains(col.FieldName);
        }


        private Task<byte[]> LoadDocumentDataAsync(int docId)
        {
            return Task.Run(() => _docService.GetDocumentBytesWithCache(docId));
        }

        private void ExportCurrentFile()
        {
            if (currentViewingDoc == null || currentStream == null)
            {
                MessageBoxHelper.ShowWarning(Lang.NoFileSelected);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string ext = Path.GetExtension(currentViewingDoc.FileName)?.ToLower() ?? ".pdf";
                sfd.Filter = DocumentServices.FileFilters.TryGetValue(ext, out var filter) ? filter : "All Files (*.*)|*.*";
                sfd.FileName = currentViewingDoc.FileName;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        DocumentServices.ExportDocumentToFile(currentStream, sfd.FileName);
                        MessageBoxHelper.ShowInfo(Lang.ExportSuccess);
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError(Lang.SaveFileFailed, ex);
                    }
                }
            }
        }
        // -----------------------------
        // Event handlers
        // -----------------------------

        private void lookUpModelName_EditValueChanged(object sender, EventArgs e)
        {
            var model = lookUpModelName.EditValue?.ToString();
            if (string.IsNullOrWhiteSpace(model) || model.Equals(previousModel, StringComparison.OrdinalIgnoreCase))
                return;

            if (RefreshDocumentList(model))
                previousModel = model;
        }
        private bool RefreshDocumentList(string model = null)
        {
            if (model == null) model = lookUpModelName.EditValue?.ToString();
            if (string.IsNullOrWhiteSpace(model)) return false;

            try
            {
                ResetViewer();
                if (!model.Equals(previousModel, StringComparison.OrdinalIgnoreCase))
                    _docService.ClearCache();
                previousModel = model;

                BindDocumentGrid(model);

                return true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataFailed, ex);
                return false;
            }
        }
        private void BindDocumentGrid(string model)
        {
            var documents = GetAllDocuments(model);
            _documentBindingList = new BindingList<BonusDocument_Model>(documents);

            if (documents != null && documents.Any())
            {
                gridControl1.DataSource = _documentBindingList;
                layoutControlItem3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always; // Grid
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always; // Viewer
                layoutControlItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never; // Ẩn label "Không có dữ liệu"

                gridView1.PopulateColumns();
                ShowColumns(gridView1, "FileNameWithoutExtension", "FileExtension", "DocumentType");
                ConfigureDocumentGridView(gridView1);
            }
            else
            {
                gridControl1.DataSource = null;
                gridControl1.MainView = null;
                lblNoData.Text = Lang.NoFileSelected;
                layoutControlItem3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never; // Grid
                layoutControlItem5.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never; // Viewer
                layoutControlItem2.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always; // Hiện label "Không có dữ liệu"

                ResetViewer();
            }
        }
        private List<BonusDocument_Model> GetAllDocuments(string model)
        {
            var documentTypes = new List<string> { "Layout File", "Machine List", "Bonus Document" };
            return _docService.GetDocumentsByModelCached(model, documentTypes);
        }

        private void ConfigureDocumentGridView(GridView gridView)
        {
            gridView.OptionsView.ShowIndicator = false;

            gridView.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;

            // Cho phép edit nhưng khóa mặc định
            gridView.OptionsBehavior.Editable = true;
            gridView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;
            gridView.Columns["FileNameWithoutExtension"].Caption = Lang.FileName;
            gridView.Columns["FileExtension"].Caption = Lang.FileExtension;
            gridView.Columns["DocumentType"].Caption = Lang.DocumentType;
        }

        private void ResetViewer()
        {
            if (pdfViewer == null || pictureViewer == null || lblFileName == null) return;
            ViewerResetHelper.ResetViewer(pdfViewer, pictureViewer, lblFileName, ref currentStream, ref currentViewingDoc);
        }

        private void btnExportFile_Click(object sender, EventArgs e) => ExportCurrentFile();
        private void SetupTooltipController()
        {
            if (gridControl1.ToolTipController == null)
            {
                gridControl1.ToolTipController = toolTipController1;
                gridControl1.ToolTipController.GetActiveObjectInfo += (_, ea) =>
                {
                    var hitInfo = gridView1.CalcHitInfo(gridControl1.PointToClient(Cursor.Position));
                    if (hitInfo.InRowCell)
                    {
                        var doc = gridView1.GetRow(hitInfo.RowHandle) as BonusDocument_Model;
                        if (doc != null)
                        {
                            string tooltip = DocumentServices.GetDocumentTooltip(doc);
                            ea.Info = new DevExpress.Utils.ToolTipControlInfo(doc, tooltip);
                        }
                    }
                };
            }
        }
    }
}
