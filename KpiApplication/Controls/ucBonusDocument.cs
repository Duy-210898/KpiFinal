using DevExpress.XtraEditors;
using KpiApplication.Common;
using KpiApplication.DataAccess;
using KpiApplication.Forms;
using KpiApplication.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KpiApplication.Controls
{
    public partial class ucBonusDocument : XtraUserControl
    {
        private readonly BonusDocument_DAL _docDal = new BonusDocument_DAL();
        private readonly Article_DAL _articleDal = new Article_DAL();
        private readonly TextEdit txtInPlaceRename = new TextEdit() { Visible = false };
        private readonly LruCache<int, byte[]> _docCache = new LruCache<int, byte[]>(20, TimeSpan.FromMinutes(10));
        private readonly LruCache<int, Image> _imageCache = new LruCache<int, Image>(10, TimeSpan.FromMinutes(10));

        private MemoryStream currentStream;

        private string selectedFileName;
        private byte[] selectedPdfData;
        private BonusDocument_Model currentViewingDoc;

        public ucBonusDocument()
        {
            InitializeComponent();
            pictureViewer.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;
            listBoxArticles.SelectionMode = SelectionMode.None;

            btnDelete.Enabled = false;
            btnExportFile.Enabled = false;
            btnAddNew.Enabled = false;
            InitControls();
            LoadModelNamesToLookup();
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible && !IsDisposed && currentViewingDoc != null)
            {
                var data = GetDocumentBytes(currentViewingDoc.Id);
                if (data != null)
                {
                    ViewerResetHelper.ResetViewer(
                        pdfViewer, pictureViewer, lblFileName,
                        ref currentStream, ref currentViewingDoc);

                    DocumentViewerHelper.DisplayDocument(
                        this,
                        pdfViewer,
                        pictureViewer,
                        lblFileName,
                        currentViewingDoc,
                        data,
                        ref currentStream,
                        _imageCache,
                        out _);
                }
            }
        }


        private byte[] GetDocumentBytes(int docId)
        {
            if (_docCache.TryGetValue(docId, out var data))
                return data;

            var doc = _docDal.GetById(docId);
            if (doc?.PdfData != null && doc.PdfData.Length > 0)
            {
                _docCache.AddOrUpdate(docId, doc.PdfData);
                return doc.PdfData;
            }

            return null;
        }

        private void InitControls()
        {
            txtInPlaceRename.KeyDown += TxtInPlaceRename_KeyDown;
            Controls.Add(txtInPlaceRename);

            listBoxDocuments.DoubleClick += (s, e) => BeginRenameSelectedItem();
        }

        private void ListBoxDocuments_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F2:
                    BeginRenameSelectedItem();
                    e.Handled = true;
                    break;
                case Keys.Delete:
                    DeleteSelectedDocument();
                    e.Handled = true;
                    break;
            }
        }

        private void TxtInPlaceRename_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitRename();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                txtInPlaceRename.Visible = false;
            }
        }

        private static bool IsValidFileName(string name, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "File name cannot be empty.";
                return false;
            }

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "File name contains invalid characters: \\ / : * ? \" < > |";
                return false;
            }

            return true;
        }

        private void RefreshDocumentList(string selectedModel = null)
        {
            if (string.IsNullOrWhiteSpace(selectedModel))
                selectedModel = lookUpModelName.EditValue?.ToString();

            if (string.IsNullOrWhiteSpace(selectedModel)) return;

            try
            {
                ViewerResetHelper.ResetViewer(
                    pdfViewer, pictureViewer, lblFileName,
                    ref currentStream, ref currentViewingDoc);

                btnAddNew.Enabled = true;
                _docCache.Clear();

                listBoxDocuments.DataSource = new BindingList<BonusDocument_Model>(
                    _docDal.GetMetadataByModelName(selectedModel));

                listBoxDocuments.DisplayMember = "FileName";

                listBoxArticles.DataSource = _articleDal.GetByModelName(selectedModel);
                listBoxArticles.DisplayMember = "ArticleName";
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Failed to load data", ex);
            }
        }
        private void CommitRename()
        {
            txtInPlaceRename.Visible = false;

            var selectedDoc = txtInPlaceRename.Tag as BonusDocument_Model;
            if (selectedDoc == null) return;

            string inputName = txtInPlaceRename.Text.Trim();
            string extension = Path.GetExtension(selectedDoc.FileName);
            string newFileName = inputName + extension;

            string error;
            if (!IsValidFileName(inputName, out error))
            {
                MessageBoxHelper.ShowWarning(error);
                return;
            }

            if (newFileName.Equals(selectedDoc.FileName, StringComparison.OrdinalIgnoreCase)) return;

            if (_docDal.Exists(selectedDoc.ModelName, newFileName))
            {
                MessageBoxHelper.ShowWarning("File name already exists. Please choose a different name.");
                return;
            }

            try
            {
                _docDal.RenameFileNameById(selectedDoc.Id, newFileName, DateTime.Now, Global.CurrentEmployee?.UserID ?? 1);
                selectedDoc.FileName = newFileName;
                RefreshDocumentList(selectedDoc.ModelName);
                MessageBoxHelper.ShowInfo("File renamed successfully.");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Rename failed", ex);
            }
        }

        private void BeginRenameSelectedItem()
        {
            var selectedDoc = listBoxDocuments.SelectedItem as BonusDocument_Model;
            if (selectedDoc == null) return;

            var bounds = listBoxDocuments.GetItemRectangle(listBoxDocuments.SelectedIndex);

            txtInPlaceRename.SetBounds(
                listBoxDocuments.Left + bounds.Left,
                listBoxDocuments.Top + bounds.Top,
                bounds.Width,
                bounds.Height);

            txtInPlaceRename.Text = Path.GetFileNameWithoutExtension(selectedDoc.FileName);
            txtInPlaceRename.Tag = selectedDoc;
            txtInPlaceRename.Visible = true;
            txtInPlaceRename.BringToFront();
            txtInPlaceRename.Focus();
            txtInPlaceRename.SelectAll();
        }

        private void LoadModelNamesToLookup()
        {
            _ = AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () => _articleDal.GetDistinctModelNames(),
                modelNames =>
                {
                    lookUpModelName.Properties.DataSource = modelNames;
                    lookUpModelName.Properties.NullText = "-- Select Model --";

                    if (listBoxDocuments.ToolTipController == null)
                    {
                        listBoxDocuments.ToolTipController = toolTipController1;
                        listBoxDocuments.ToolTipController.GetActiveObjectInfo += (s, ea) =>
                        {
                            var point = listBoxDocuments.PointToClient(Cursor.Position);
                            int index = listBoxDocuments.IndexFromPoint(point);

                            if (index >= 0 && index < listBoxDocuments.ItemCount &&
                                listBoxDocuments.GetItem(index) is BonusDocument_Model item)
                            {
                                string tooltip = $"📄 File name: {item.FileName}\n🕒 Created at: {item.CreatedAt:yyyy-MM-dd}\n👤 Created by: {item.CreatedByName}";

                                if (item.UpdatedAt.HasValue && !string.IsNullOrWhiteSpace(item.UpdatedByName))
                                {
                                    tooltip += $"\n✏️ Updated at: {item.UpdatedAt:yyyy-MM-dd}\n👤 Updated by: {item.UpdatedByName}";
                                }

                                ea.Info = new DevExpress.Utils.ToolTipControlInfo(item, tooltip);
                            }
                        };
                    }
                },
                caption: "Loading...",
                description: "Loading model names...");
        }

        private void lookUpModelName_EditValueChanged(object sender, EventArgs e)
        {
            string model = lookUpModelName.EditValue?.ToString();
            if (!string.IsNullOrEmpty(model))
            {
                RefreshDocumentList(model);
            }
        }

        private async void ListBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxDocuments.SelectedItem is BonusDocument_Model selectedDoc)
            {
                btnExportFile.Enabled = false;
                btnDelete.Enabled = false;

                UseWaitCursor = true;

                await Task.Run(() =>
                {
                    var data = GetDocumentBytes(selectedDoc.Id);

                    string errorMessage;
                    var success = DocumentViewerHelper.DisplayDocument(
                        this,
                        pdfViewer,
                        pictureViewer,
                        lblFileName,
                        selectedDoc,
                        data,
                        ref currentStream,
                        _imageCache,
                        out errorMessage);

                    Invoke(new Action(() =>
                    {
                        UseWaitCursor = false;

                        if (success)
                        {
                            currentViewingDoc = selectedDoc;
                            btnExportFile.Enabled = true;
                            btnDelete.Enabled = true;
                        }
                        else if (!string.IsNullOrEmpty(errorMessage))
                        {
                            MessageBoxHelper.ShowWarning(errorMessage);
                        }
                    }));
                });
            }
            else
            {
                btnExportFile.Enabled = false;
                btnDelete.Enabled = false;
            }
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            string modelName = lookUpModelName.EditValue?.ToString();

            if (string.IsNullOrWhiteSpace(modelName))
            {
                MessageBoxHelper.ShowWarning("Please select a model before choosing a file.");
                return;
            }

            using (var ofd = new OpenFileDialog
            {
                Filter = "Supported Files|*.pdf;*.jpg;*.jpeg;*.png;*.bmp"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    selectedPdfData = File.ReadAllBytes(ofd.FileName);
                    selectedFileName = Path.GetFileName(ofd.FileName);

                    using (var preview = new PreviewSaveForm(selectedPdfData, selectedFileName, modelName))
                    {
                        preview.ShowDialog();

                        if (preview.IsConfirmed && !string.IsNullOrWhiteSpace(preview.FileName))
                        {
                            selectedPdfData = preview.FinalFileData;  
                            selectedFileName = preview.FileName;
                            SaveDocument(modelName, selectedFileName, selectedPdfData);
                        }
                        else if (string.IsNullOrWhiteSpace(preview.FileName))
                        {
                            MessageBoxHelper.ShowWarning("File name cannot be empty.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError("Failed to open file", ex);
                }
            }
        }
        private void SaveDocument(string modelName, string fileName, byte[] pdfData)
        {
            try
            {
                var doc = new BonusDocument_Model
                {
                    ModelName = modelName,
                    FileName = fileName,
                    PdfData = pdfData,
                    CreatedBy = Global.CurrentEmployee?.UserID ?? 1
                };

                if (_docDal.Exists(modelName, fileName))
                {
                    var existing = _docDal.GetMetadataByModelName(modelName)?.FirstOrDefault(x => x.FileName == fileName);
                    if (existing != null)
                    {
                        doc.Id = existing.Id;
                        doc.UpdatedAt = DateTime.Now;
                        doc.UpdatedBy = Global.CurrentEmployee?.UserID ?? 1;
                        _docDal.Update(doc);
                        MessageBoxHelper.ShowInfo("File updated successfully.");
                    }
                }
                else
                {
                    _docDal.Insert(doc);
                    MessageBoxHelper.ShowInfo("File saved successfully.");
                }

                selectedPdfData = null;
                selectedFileName = null;

                RefreshDocumentList(modelName);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Error while saving document", ex);
            }
        }
        public void PerformRefresh()
        {
            _ = AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () =>
                {
                    return _articleDal.GetDistinctModelNames();
                },
                modelNames =>
                {
                    lookUpModelName.EditValue = null;

                    ViewerResetHelper.ResetViewer(
                        pdfViewer, pictureViewer, lblFileName,
                        ref currentStream, ref currentViewingDoc);

                    listBoxDocuments.DataSource = null;
                    listBoxArticles.DataSource = null;

                    _docCache.Clear();
                    _imageCache.Clear();

                    btnAddNew.Enabled = false;
                    btnDelete.Enabled = false;
                    btnExportFile.Enabled = false;

                    lookUpModelName.Properties.DataSource = modelNames;
                    lookUpModelName.Properties.NullText = "-- Select Model --";

                    if (listBoxDocuments.ToolTipController == null)
                    {
                        listBoxDocuments.ToolTipController = toolTipController1;
                        listBoxDocuments.ToolTipController.GetActiveObjectInfo += (s, ea) =>
                        {
                            var point = listBoxDocuments.PointToClient(Cursor.Position);
                            int index = listBoxDocuments.IndexFromPoint(point);

                            if (index >= 0 && index < listBoxDocuments.ItemCount &&
                                listBoxDocuments.GetItem(index) is BonusDocument_Model item)
                            {
                                string tooltip = $"📄 File name: {item.FileName}\n🕒 Created at: {item.CreatedAt:yyyy-MM-dd}\n👤 Created by: {item.CreatedByName}";

                                if (item.UpdatedAt.HasValue && !string.IsNullOrWhiteSpace(item.UpdatedByName))
                                {
                                    tooltip += $"\n✏️ Updated at: {item.UpdatedAt:yyyy-MM-dd}\n👤 Updated by: {item.UpdatedByName}";
                                }

                                ea.Info = new DevExpress.Utils.ToolTipControlInfo(item, tooltip);
                            }
                        };
                    }
                },
                caption: "Refreshing...",
                description: "Refreshing bonus document data...");
        }

        private void DeleteSelectedDocument()
        {
            var selectedDoc = listBoxDocuments.SelectedItem as BonusDocument_Model;
            if (selectedDoc == null) return;

            var confirm = MessageBoxHelper.ShowConfirm(
                $"Are you sure you want to delete file '{selectedDoc.FileName}' from model '{selectedDoc.ModelName}'?",
                "Confirm Delete");

            if (confirm != DialogResult.Yes) return;

            try
            {
                _docDal.Delete(selectedDoc.Id);
                _docCache.Remove(selectedDoc.Id);

                MessageBoxHelper.ShowInfo("File deleted successfully.");

                if (currentViewingDoc != null && currentViewingDoc.Id == selectedDoc.Id)
                {
                    ViewerResetHelper.ResetViewer(
                        pdfViewer, pictureViewer, lblFileName,
                        ref currentStream, ref currentViewingDoc);
                }

                RefreshDocumentList(selectedDoc.ModelName);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError("Failed to delete file", ex);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e) => DeleteSelectedDocument();

        private void btnExportFile_Click(object sender, EventArgs e)
        {
            ExportCurrentFile();
        }
        private void ExportCurrentFile()
        {
            if (currentViewingDoc == null || currentStream == null)
            {
                MessageBoxHelper.ShowWarning("No file is currently selected.");
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string ext = Path.GetExtension(currentViewingDoc.FileName)?.ToLower() ?? ".pdf";
                string filter;

                switch (ext)
                {
                    case ".pdf":
                        filter = "PDF File (*.pdf)|*.pdf";
                        break;
                    case ".jpg":
                        filter = "JPEG Image (*.jpg)|*.jpg";
                        break;
                    case ".jpeg":
                        filter = "JPEG Image (*.jpeg)|*.jpeg";
                        break;
                    case ".png":
                        filter = "PNG Image (*.png)|*.png";
                        break;
                    case ".bmp":
                        filter = "Bitmap Image (*.bmp)|*.bmp";
                        break;
                    default:
                        filter = "All Files (*.*)|*.*";
                        break;
                }

                sfd.Filter = filter;
                sfd.FileName = currentViewingDoc.FileName;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, currentStream.ToArray());
                        MessageBoxHelper.ShowInfo("File exported successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowError("Failed to save file", ex);
                    }
                }
            }
        }
    }
}