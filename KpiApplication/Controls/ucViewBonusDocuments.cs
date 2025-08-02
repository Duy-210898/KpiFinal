using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using KpiApplication.Common;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace KpiApplication.Controls
{
    public partial class ucViewBonusDocuments : XtraUserControl, ISupportLoadAsync
    {
        private readonly DocumentServices _docService = new DocumentServices();
        private BonusDocument_Model currentViewingDoc;
        private MemoryStream currentStream;
        private string previousModel;

        public ucViewBonusDocuments()
        {
            InitializeComponent();
            ApplyLocalizedText();
            ConfigureControls();
        }

        private void ApplyLocalizedText()
        {
            btnExportFile.Text = Lang.ExportFile;
            lookUpModelName.Properties.NullText = Lang.SelectModel;
            layoutControlItem1.Text = Lang.ModelName;
        }

        private void ConfigureControls()
        {
            pictureViewer.Properties.SizeMode = PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;
            btnExportFile.Enabled = false;
            pdfViewer.Visible = false;
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
        private void ReloadCurrentDocument()
        {
            var data = _docService.GetDocumentBytesWithCache(currentViewingDoc.Id);
            if (data == null || data.Length == 0) return;

            ResetViewer();
            DisplaySelectedDocument(currentViewingDoc, data);
        }

        private void SetUiStateDuringLoading(bool enable)
        {
            btnExportFile.Enabled = enable;
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

        private void ResetViewer()
        {
            if (pdfViewer == null || pictureViewer == null || lblFileName == null) return;

            // Ẩn control để tránh chớp
            pdfViewer.Visible = false;
            pictureViewer.Visible = false;

            ViewerResetHelper.ResetViewer(pdfViewer, pictureViewer, lblFileName, ref currentStream, ref currentViewingDoc);
            btnExportFile.Enabled = false;
        }

        private bool RefreshDocumentList(string model = null)
        {
            model = model ?? lookUpModelName.EditValue?.ToString();
            if (string.IsNullOrWhiteSpace(model)) return false;

            try
            {
                ResetViewer();

                if (!model.Equals(previousModel, StringComparison.OrdinalIgnoreCase))
                {
                    _docService.ClearCache();
                }

                // Ngắt kết nối sự kiện trước khi gán
                listBoxDocuments.SelectedIndexChanged -= ListBoxDocuments_SelectedIndexChanged;

                listBoxDocuments.DataSource = new BindingList<BonusDocument_Model>(_docService.GetDocumentsByModel(model));
                listBoxDocuments.DisplayMember = "FileName";
                listBoxDocuments.SelectedIndex = -1; // Rõ ràng hơn

                listBoxArticles.DataSource = _docService.GetArticlesByModel(model);
                listBoxArticles.DisplayMember = "ArticleName";

                // Gắn lại sự kiện sau khi hoàn tất gán DataSource
                listBoxDocuments.SelectedIndexChanged += ListBoxDocuments_SelectedIndexChanged;

                previousModel = model;
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.LoadDataFailed, ex);
                return false;
            }
        }
        private void SetupTooltipController()
        {
            if (listBoxDocuments.ToolTipController != null) return;

            listBoxDocuments.ToolTipController = toolTipController1;
            listBoxDocuments.ToolTipController.GetActiveObjectInfo += (_, ea) =>
            {
                var point = listBoxDocuments.PointToClient(Cursor.Position);
                int index = listBoxDocuments.IndexFromPoint(point);

                if (index >= 0 && index < listBoxDocuments.ItemCount && listBoxDocuments.GetItem(index) is BonusDocument_Model item)
                {
                    string tooltip = DocumentServices.GetDocumentTooltip(item);
                    ea.Info = new DevExpress.Utils.ToolTipControlInfo(item, tooltip);
                }
            };
        }
        public async Task LoadDataAsync()
        {
            await LoadModelNamesToLookupAsync();
        }


        private async Task LoadModelNamesToLookupAsync()
        {
            try
            {
                UseWaitCursor = true;

                var modelNames = await Task.Run(() => _docService.GetModelNamesHavingDocuments());

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

        private void lookUpModelName_EditValueChanged(object sender, EventArgs e)
        {
            RefreshDocumentList();
        }

        private async Task<bool> LoadAndDisplayDocument(BonusDocument_Model doc)
        {
            var data = await Task.Run(() => _docService.GetDocumentBytesWithCache(doc.Id));
            if (data == null || data.Length == 0) return false;

            ResetViewer();
            return DisplaySelectedDocument(doc, data);
        }

        private async void ListBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedObj = listBoxDocuments.SelectedItem;
            if (listBoxDocuments.SelectedIndex < 0 || !(selectedObj is BonusDocument_Model selectedDoc))
            {
                ResetViewer();
                return;
            }

            SetUiStateDuringLoading(false);

            try
            {
                bool success = await LoadAndDisplayDocument(selectedDoc);
                if (success)
                {
                    currentViewingDoc = selectedDoc;
                    btnExportFile.Enabled = true;
                }
                else
                {
                    currentViewingDoc = null;
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Lang.UnexpectedError, ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
        public async void PerformRefresh()
        {
            try
            {
                UseWaitCursor = true;

                var modelNames = await Task.Run(() => _docService.GetModelNamesHavingDocuments());

                lookUpModelName.EditValue = null;

                ResetViewer();
                listBoxDocuments.DataSource = null;
                listBoxArticles.DataSource = null;

                _docService.ClearCache();

                lookUpModelName.Properties.DataSource = modelNames;
                lookUpModelName.Properties.NullText = Lang.SelectModel;

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
        private void ExportCurrentFile()
        {
            if (currentViewingDoc == null || currentStream == null)
            {
                MessageBoxHelper.ShowWarning(Lang.NoFileSelected);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                string ext = Path.GetExtension(currentViewingDoc.FileName)?.ToLower() ?? string.Empty;
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

        private void btnExportFile_Click(object sender, EventArgs e) => ExportCurrentFile();
    }
}
