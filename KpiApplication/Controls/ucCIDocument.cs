using DevExpress.Utils;
using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.Forms;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucCIDocument : DevExpress.XtraEditors.XtraUserControl, ISupportLoadAsync
    {
        // -----------------------------
        // Instance fields
        // -----------------------------
        private readonly DocumentServices _docService = new DocumentServices();
        private readonly TextEdit txtInPlaceRename = new TextEdit() { Visible = false };
        private MemoryStream currentStream;
        private BonusDocument_Model currentViewingDoc;
        private string previousModel;

        // -----------------------------
        // Constructor
        // -----------------------------
        public ucCIDocument()
        {
            InitializeComponent();
            InitializeControls();
            ApplyLocalizedText();
        }

        // -----------------------------
        // Public methods
        // -----------------------------
        public void PerformRefresh() => _ = LoadModels(reset: true);

        // -----------------------------
        // Lifecycle overrides
        // -----------------------------
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

        // -----------------------------
        // Initialization methods
        // -----------------------------
        private void InitializeControls()
        {
            pictureViewer.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;

            btnDelete.Enabled = false;
            btnExportFile.Enabled = false;
            btnAddNew.Enabled = false;

            txtInPlaceRename.KeyDown += TxtInPlaceRename_KeyDown;
            Controls.Add(txtInPlaceRename);

            listBoxMachineList.DoubleClick += (s, e) => BeginRenameSelectedItem();
            listBoxLayoutFile.DoubleClick += (s, e) => BeginRenameSelectedItem();
        }

        private void ApplyLocalizedText()
        {
            btnExportFile.Text = Lang.Export;
            btnDelete.Text = Lang.Delete;
            btnAddNew.Text = Lang.AddNewFile;
            lookUpModelName.Properties.NullText = Lang.SelectModel;
            layoutControlItem1.Text = Lang.ModelName;
            layoutControlItem2.Text = Lang.ArticleList;
            layoutControlItem3.Text = Lang.BonusFileList;
        }

        private void SetupTooltipController()
        {
            if (listBoxLayoutFile.ToolTipController != null && listBoxMachineList.ToolTipController != null)
                return;

            // Setup cho listBoxLayoutFile
            if (listBoxLayoutFile.ToolTipController == null)
            {
                listBoxLayoutFile.ToolTipController = toolTipController1;
                listBoxLayoutFile.ToolTipController.GetActiveObjectInfo += (_, ea) =>
                {
                    var point = listBoxLayoutFile.PointToClient(Cursor.Position);
                    int index = listBoxLayoutFile.IndexFromPoint(point);

                    if (index >= 0 && index < listBoxLayoutFile.ItemCount && listBoxLayoutFile.GetItem(index) is BonusDocument_Model item)
                    {
                        string tooltip = DocumentServices.GetDocumentTooltip(item);
                        ea.Info = new DevExpress.Utils.ToolTipControlInfo(item, tooltip);
                    }
                };
            }

            // Setup cho listBoxMachineList
            if (listBoxMachineList.ToolTipController == null)
            {
                listBoxMachineList.ToolTipController = toolTipController1;
                listBoxMachineList.ToolTipController.GetActiveObjectInfo += (_, ea) =>
                {
                    var point = listBoxMachineList.PointToClient(Cursor.Position);
                    int index = listBoxMachineList.IndexFromPoint(point);

                    if (index >= 0 && index < listBoxMachineList.ItemCount && listBoxMachineList.GetItem(index) is BonusDocument_Model item)
                    {
                        string tooltip = DocumentServices.GetDocumentTooltip(item);
                        ea.Info = new DevExpress.Utils.ToolTipControlInfo(item, tooltip);
                    }
                };
            }
        }
        // -----------------------------
        // Data loading and refresh
        // -----------------------------
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
                    listBoxMachineList.DataSource = null;
                    listBoxLayoutFile.DataSource = null;
                    _docService.ClearCache();
                    btnAddNew.Enabled = btnDelete.Enabled = btnExportFile.Enabled = false;
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
        private bool RefreshDocumentList(string model = null)
        {
            if (model == null) model = lookUpModelName.EditValue?.ToString();
            if (string.IsNullOrWhiteSpace(model)) return false;

            try
            {
                ResetViewer();
                btnAddNew.Enabled = true;
                if (!model.Equals(previousModel, StringComparison.OrdinalIgnoreCase))
                {
                    _docService.ClearCache();
                }
                listBoxLayoutFile.SelectedIndexChanged -= ListBoxDocuments_SelectedIndexChanged;
                listBoxMachineList.SelectedIndexChanged -= ListBoxDocuments_SelectedIndexChanged;

                //listBoxDocuments.DataSource = new BindingList<BonusDocument_Model>(_docService.GetDocumentsByModel(model));
                //listBoxDocuments.DisplayMember = "FileName";
                //listBoxDocuments.SelectedIndex = -1;

                //listBoxArticles.DataSource = _docService.GetArticlesByModel(model);
                //listBoxArticles.DisplayMember = "ArticleName";

                listBoxMachineList.SelectedIndexChanged += ListBoxDocuments_SelectedIndexChanged;
                listBoxLayoutFile.SelectedIndexChanged += ListBoxDocuments_SelectedIndexChanged;
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataFailed, ex);
                return false;
            }
        }
        private void ResetViewer()
        {
            if (pdfViewer == null || pictureViewer == null || lblFileName == null) return;
            ViewerResetHelper.ResetViewer(pdfViewer, pictureViewer, lblFileName, ref currentStream, ref currentViewingDoc);
        }
        // -----------------------------
        // Rename logic
        // -----------------------------
        private void BeginRenameSelectedItem()
        {
            var doc = listBoxLayoutFile.SelectedItem as BonusDocument_Model;
            if (doc == null) return;
            var bounds = listBoxLayoutFile.GetItemRectangle(listBoxLayoutFile.SelectedIndex);
            txtInPlaceRename.SetBounds(listBoxLayoutFile.Left + bounds.Left, listBoxLayoutFile.Top + bounds.Top, bounds.Width, bounds.Height);
            txtInPlaceRename.Text = Path.GetFileNameWithoutExtension(doc.FileName);
            txtInPlaceRename.Tag = doc;
            txtInPlaceRename.Visible = true;
            txtInPlaceRename.BringToFront();
            txtInPlaceRename.Focus();
            txtInPlaceRename.SelectAll();
        }

        private void CommitRename()
        {
            txtInPlaceRename.Visible = false;
            var selectedDoc = txtInPlaceRename.Tag as BonusDocument_Model;
            if (selectedDoc == null) return;

            string inputName = txtInPlaceRename.Text.Trim();
            if (!DocumentServices.IsValidFileName(inputName, out var error))
            {
                MessageBoxHelper.ShowWarning(error);
                return;
            }

            string extension = Path.GetExtension(selectedDoc.FileName);
            string newFileName = inputName + extension;

            if (newFileName.Equals(selectedDoc.FileName, StringComparison.OrdinalIgnoreCase)) return;
            if (_docService.DocumentExists(selectedDoc.ModelName, newFileName))
            {
                MessageBoxHelper.ShowWarning(Lang.FileNameExists);
                return;
            }

            try
            {
                _docService.RenameDocument(selectedDoc, newFileName, Global.CurrentEmployee?.UserID ?? 1);
                selectedDoc.FileName = newFileName;
                RefreshDocumentList(selectedDoc.ModelName);
                MessageBoxHelper.ShowInfo(Lang.RenameSuccess);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.RenameFailed, ex);
            }
        }

        // -----------------------------
        // File operations
        // -----------------------------
        private bool TrySelectFile(out string fileName, out byte[] data)
        {
            fileName = null;
            data = null;

            using (var ofd = new OpenFileDialog
            {
                Filter = $"{Lang.SupportedFiles}|*.pdf;*.jpg;*.jpeg;*.png;*.bmp",
                Title = Lang.SelectFile,
                CheckFileExists = true,
                Multiselect = false
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK)
                    return false;

                try
                {
                    string selectedPath = ofd.FileName;
                    if (!File.Exists(selectedPath))
                    {
                        MessageBoxHelper.ShowWarning(Lang.FileNotFound);
                        return false;
                    }

                    data = File.ReadAllBytes(selectedPath);
                    fileName = Path.GetFileName(selectedPath);
                    return true;
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

                return false;
            }
        }

        private void SaveDocument(string modelName, string fileName, string documentType, byte[] pdfData)
        {
            try
            {
                _docService.SaveOrUpdateDocument(modelName, fileName, documentType, pdfData, Global.CurrentEmployee?.UserID ?? 1);
                RefreshDocumentList(modelName);
                MessageBoxHelper.ShowInfo(Lang.FileSavedSuccessfully);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.ErrorWhileSavingDocument, ex);
            }
        }

        private void DeleteSelectedDocument()
        {
            var selectedDoc = listBoxLayoutFile.SelectedItem as BonusDocument_Model;
            if (selectedDoc == null) return;

            var confirm = MessageBoxHelper.ShowConfirm(
                string.Format(Lang.ConfirmDelete_Message, selectedDoc.FileName, selectedDoc.ModelName),
                Lang.ConfirmDelete_Title);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _docService.DeleteDocument(selectedDoc.Id);
                _docService.RemoveDocumentFromCache(selectedDoc.Id);

                if (currentViewingDoc?.Id == selectedDoc.Id)
                {
                    ResetViewer();
                    currentViewingDoc = null;
                }

                MessageBoxHelper.ShowInfo(Lang.DeletedSuccess);
                RefreshDocumentList(selectedDoc.ModelName);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.DeleteFailed, ex);
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
        /// <summary>
        /// Other Methods
        /// </summary>
        /// 
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
        private void SetUiStateDuringLoading(bool enable)
        {
            btnExportFile.Enabled = enable;
            btnDelete.Enabled = enable;
            UseWaitCursor = !enable;
        }
        private bool DisplaySelectedDocument(BonusDocument_Model doc, byte[] data)
        {
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



        // -----------------------------
        // Event handlers
        // -----------------------------
        private void ListBoxDocuments_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F2: BeginRenameSelectedItem(); e.Handled = true; break;
                case Keys.Delete: DeleteSelectedDocument(); e.Handled = true; break;
            }
        }

        private void TxtInPlaceRename_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { CommitRename(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) txtInPlaceRename.Visible = false;
        }

        private async void ListBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedDoc = listBoxLayoutFile.SelectedItem as BonusDocument_Model;
            if (selectedDoc == null || currentViewingDoc?.Id == selectedDoc.Id)
                return;

            SetUiStateDuringLoading(false);

            try
            {
                var data = await LoadDocumentDataAsync(selectedDoc.Id);

                if (DisplaySelectedDocument(selectedDoc, data))
                {
                    currentViewingDoc = selectedDoc;
                    SetUiStateDuringLoading(true);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Lỗi khi tải tài liệu", ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
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

            if (!TrySelectFile(out string fileName, out byte[] data)) return;

            using (var preview = new PreviewSaveForm(data, fileName, modelName))
            {
                preview.ShowDialog();

                if (preview.IsConfirmed &&
                    !string.IsNullOrWhiteSpace(preview.FileName) &&
                    !string.IsNullOrWhiteSpace(preview.DocumentType))
                {
                    SaveDocument(
                        modelName,
                        preview.FileName,
                        preview.DocumentType,
                        preview.FinalFileData
                    );
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e) => DeleteSelectedDocument();
        private void btnExportFile_Click(object sender, EventArgs e) => ExportCurrentFile();
    }
}
