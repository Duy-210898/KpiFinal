using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using KpiApplication.Common;
using KpiApplication.Forms;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucCIDocument : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        private readonly DocumentServices _docService = new DocumentServices();
        private MemoryStream currentStream;
        private BonusDocument_Model currentViewingDoc;
        private string previousModel;
        private BindingList<BonusDocument_Model> _documentBindingList;

        public ucCIDocument()
        {
            InitializeComponent();
            InitializeControls();
            ApplyLocalizedText();
        }
        public Task LoadDataAsync()
        {
            return LoadModelsAsync(true);
        }

        private async Task LoadModelsAsync(bool reset)
        {
            try
            {
                UseWaitCursor = true;
                List<string> modelNames = await Task.Run(() => _docService.GetModelNames());

                if (reset)
                {
                    lookUpModelName.EditValue = null;
                    ResetViewer();
                    gridControl1.DataSource = null;
                    _docService.ClearCache();
                    btnAddNew.Enabled = false;
                    btnDelete.Enabled = false;
                    btnExportFile.Enabled = false;
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

        private bool RefreshDocumentList(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                model = lookUpModelName.EditValue as string;

            if (string.IsNullOrWhiteSpace(model))
                return false;

            try
            {
                ResetViewer();
                btnAddNew.Enabled = true;

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
            gridControl1.DataSource = _documentBindingList;

            gridView1.PopulateColumns();
            ShowColumns(gridView1, "FileNameWithoutExtension", "FileExtension", "DocumentType");
            ConfigureDocumentGridView(gridView1);

            gridView1.ClearSelection();
            gridView1.FocusedRowHandle = DevExpress.XtraGrid.GridControl.InvalidRowHandle;
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

            btnDelete.Enabled = false;
            btnExportFile.Enabled = false;
            btnAddNew.Enabled = false;
        }

        private void ApplyLocalizedText()
        {
            btnExportFile.Text = Lang.Export;
            btnDelete.Text = Lang.Delete;
            btnAddNew.Text = Lang.AddNewFile;
            lookUpModelName.Properties.NullText = Lang.SelectModel;
            layoutControlItem1.Text = Lang.ModelName;
            layoutControlItem2.Text = Lang.FileList;
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
            btnDelete.Enabled = enable;
            UseWaitCursor = !enable;
        }

        private List<BonusDocument_Model> GetAllDocuments(string model)
        {
            var documentTypes = new List<string> { "Layout File", "Machine List" };
            return _docService.GetDocumentsByModelCached(model, documentTypes);
        }
        private BonusDocument_Model GetSelectedDocument()
        {
            return gridView1.GetFocusedRow() as BonusDocument_Model;
        }

        private async Task DeleteSelectedDocumentAsync()
        {
            var selectedDoc = GetSelectedDocument();
            if (selectedDoc == null) return;

            var confirm = MessageBoxHelper.ShowConfirm(
                string.Format(Lang.ConfirmDelete_Message, selectedDoc.FileName, selectedDoc.ModelName),
                Lang.ConfirmDelete_Title);

            if (confirm != DialogResult.Yes) return;

            try
            {
                await Task.Run(() => _docService.DeleteDocument(selectedDoc.Id));
                _docService.RemoveDocumentFromCache(selectedDoc.Id);

                if (currentViewingDoc?.Id == selectedDoc.Id)
                {
                    ResetViewer();
                    currentViewingDoc = null;
                }

                _documentBindingList?.Remove(selectedDoc);

                MessageBoxHelper.ShowInfo(Lang.DeletedSuccess);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.DeleteFailed, ex);
            }
        }
        private void ShowColumns(GridView view, params string[] visibleColumns)
        {
            foreach (GridColumn col in view.Columns)
                col.Visible = visibleColumns.Contains(col.FieldName);
        }
        private void ConfigureDocumentGridView(GridView gridView)
        {
            gridView.OptionsView.ShowIndicator = false;

            gridView.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.False;
            gridView.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;

            // Cho phép edit nhưng khóa mặc định
            gridView.OptionsBehavior.Editable = true;
            gridView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDownFocused;
            gridView.Columns["FileName"].Caption = Lang.FileName;
            gridView.Columns["DocumentType"].Caption = Lang.DocumentType;
        }

        private void ResetViewer()
        {
            if (pdfViewer == null || pictureViewer == null || lblFileName == null) return;
            ViewerResetHelper.ResetViewer(pdfViewer, pictureViewer, lblFileName, ref currentStream, ref currentViewingDoc);
        }
        private (bool Success, string FileName, byte[] Data) TrySelectFile()
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = $"{Lang.SupportedFiles}|*.pdf;*.jpg;*.jpeg;*.png;*.bmp",
                Title = Lang.SelectFile,
                CheckFileExists = true,
                Multiselect = false
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return (false, null, null);

                try
                {
                    string selectedPath = ofd.FileName;
                    if (!File.Exists(selectedPath))
                    {
                        MessageBoxHelper.ShowWarning(Lang.FileNotFound);
                        return (false, null, null);
                    }

                    byte[] data = File.ReadAllBytes(selectedPath);
                    string fileName = Path.GetFileName(selectedPath);
                    return (true, fileName, data);
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBoxHelper.ShowError(Lang.AccessDeniedToFile, ex);
                }
                catch (IOException ex)
                {
                    MessageBoxHelper.ShowError(Lang.FileReadError, ex);
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError(Lang.FailedToOpenFile, ex);
                }

                return (false, null, null);
            }
        }
        private void SaveDocument(string modelName, string fileName, string documentType, byte[] pdfData)
        {
            try
            {
                // Gọi hàm lưu và lấy về document mới/cập nhật đầy đủ thông tin
                var newDoc = _docService.SaveOrUpdateDocument(modelName, fileName, documentType, pdfData, Global.CurrentEmployee?.UserID ?? 1);
                if (newDoc == null)
                {
                    MessageBoxHelper.ShowWarning("Không thể lấy thông tin tài liệu mới sau khi lưu.");
                    return;
                }

                // Thêm vào BindingList để grid tự động cập nhật
                _documentBindingList?.Add(newDoc);

                MessageBoxHelper.ShowInfo(Lang.FileSavedSuccessfully);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ErrorWhileSavingDocument, ex);
            }
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
        private void btnAddNew_Click(object sender, EventArgs e)
        {
            string modelName = lookUpModelName.EditValue?.ToString();
            if (string.IsNullOrWhiteSpace(modelName))
            {
                MessageBoxHelper.ShowWarning(Lang.SelectModelFirst);
                return;
            }

            var result = TrySelectFile();
            if (!result.Success)
                return;

            using (var preview = new PreviewSaveForm(result.Data, result.FileName, modelName, true))
            {
                preview.ShowDialog();

                if (preview.IsConfirmed &&
                    !string.IsNullOrWhiteSpace(preview.FileName) &&
                    !string.IsNullOrWhiteSpace(preview.FinalDocumentType))
                {
                    SaveDocument(
                        modelName,
                        preview.FileName,
                        preview.FinalDocumentType,
                        preview.FinalFileData
                    );
                    // Đảm bảo gọi lại làm mới UI sau khi lưu
                    RefreshDocumentList(modelName);
                }
            }
        }
        private async void btnDelete_Click(object sender, EventArgs e) => await DeleteSelectedDocumentAsync();
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
        private void gridView1_ValidateRow(object sender, DevExpress.XtraGrid.Views.Base.ValidateRowEventArgs e)
        {
            var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
            var doc = view.GetRow(e.RowHandle) as BonusDocument_Model;
            if (doc == null) return;

            string newFileNameWithoutExt = doc.FileNameWithoutExtension;
            string finalFileName = doc.FileName; // Đã tự động ghép tên + đuôi

            if (string.IsNullOrWhiteSpace(newFileNameWithoutExt))
            {
                e.Valid = false;
                MessageBoxHelper.ShowWarning(Lang.InvalidFileName);
                return;
            }

            if (!DocumentServices.IsValidFileName(newFileNameWithoutExt, out var error))
            {
                e.Valid = false;
                MessageBoxHelper.ShowWarning(error);
                return;
            }

            if (_docService.DocumentExists(doc.ModelName, finalFileName))
            {
                e.Valid = false;
                MessageBoxHelper.ShowWarning(Lang.FileNameExists);
                return;
            }

            try
            {
                _docService.RenameDocument(doc, finalFileName, Global.CurrentEmployee?.UserID ?? 1);
                doc.FileName = finalFileName; // Cập nhật lại
            }
            catch (Exception ex)
            {
                e.Valid = false;
                MessageBoxHelper.ShowError(Lang.RenameFailed, ex);
            }
        }
    }
}
