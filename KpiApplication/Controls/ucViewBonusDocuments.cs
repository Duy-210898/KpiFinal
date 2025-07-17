using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using KpiApplication.DataAccess;
using KpiApplication.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace KpiApplication.Controls
{
    public partial class ucViewBonusDocuments : XtraUserControl
    {
        private readonly BonusDocument_DAL _docDal = new BonusDocument_DAL();
        private readonly Article_DAL _articleDal = new Article_DAL();
        private readonly LruCache<int, byte[]> _docCache = new LruCache<int, byte[]>(20, TimeSpan.FromMinutes(10));
        private readonly LruCache<int, Image> _imageCache = new LruCache<int, Image>(10, TimeSpan.FromMinutes(10));

        private BonusDocument_Model currentViewingDoc;
        private MemoryStream currentStream;

        public ucViewBonusDocuments()
        {
            InitializeComponent();
            ConfigureControls();
            LoadModelNamesToLookup();
        }

        private void ConfigureControls()
        {
            pictureViewer.Properties.SizeMode = PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;
            btnExportFile.Enabled = false;
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

                _docCache.Clear();
                _imageCache.Clear();

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

        private void LoadModelNamesToLookup()
        {
            _ = AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () => _articleDal.GetModelNameExistFile(),
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
            RefreshDocumentList();
        }

        private async void ListBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxDocuments.SelectedItem is BonusDocument_Model selectedDoc)
            {
                btnExportFile.Enabled = false;
                UseWaitCursor = true;

                try
                {
                    var data = await Task.Run(() => GetDocumentBytes(selectedDoc.Id));

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

                    if (success)
                    {
                        currentViewingDoc = selectedDoc;
                        btnExportFile.Enabled = true;
                    }
                    else if (!string.IsNullOrEmpty(errorMessage))
                    {
                        MessageBoxHelper.ShowWarning(errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowError("Unexpected error", ex);
                }
                finally
                {
                    UseWaitCursor = false;
                }
            }
            else
            {
                btnExportFile.Enabled = false;
            }
        }
        public void PerformRefresh()
        {
            _ = AsyncLoaderHelper.LoadDataWithSplashAsync(
                this,
                () =>
                {
                    // Reset trạng thái và lấy lại model list
                    var modelNames = _articleDal.GetModelNameExistFile();

                    return modelNames;
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
                description: "Refreshing bonus documents...");
        }

        private void btnExportFile_Click(object sender, EventArgs e)
        {
            if (currentViewingDoc == null || currentStream == null)
            {
                MessageBoxHelper.ShowWarning("No file is currently selected.");
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string ext = Path.GetExtension(currentViewingDoc.FileName)?.ToLowerInvariant();
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
