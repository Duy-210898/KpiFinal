using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KpiApplication.Forms
{
    public partial class PreviewSaveForm : XtraForm
    {
        private readonly byte[] _fileData;
        private readonly string _fileExtension;
        private readonly string _modelName;
        private readonly DocumentServices _docService = new DocumentServices();

        public bool IsConfirmed { get; private set; }
        public byte[] FinalFileData { get; private set; }
        public string FinalDocumentType { get; private set; }
        public string FileName => Path.GetFileName(txtFileName.Text?.Trim());

        public PreviewSaveForm(
            byte[] fileData,
            string fileName,
            string modelName,
            bool showDocumentType)
        {
            InitializeComponent();
            ConfigurePictureViewer();

            _fileData = fileData;
            _modelName = modelName;
            _fileExtension = Path.GetExtension(fileName)?.ToLower();

            txtFileName.Text = fileName;
            txtModelName.Text = modelName;
            txtModelName.Properties.ReadOnly = true;

            ConfigureDocumentTypeDropdown(showDocumentType);
            DisplayPreview();
        }

        #region UI Configuration
        private void ConfigurePictureViewer()
        {
            pictureViewer.Properties.SizeMode = PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;
        }

        private void ConfigureDocumentTypeDropdown(bool showDocumentType)
        {
            layoutControlItem6.Visibility = showDocumentType
                ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always
                : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

            if (!showDocumentType) return;

            cbxDocumentType.Properties.Items.Clear();
            var types = new[] { "Layout File", "Machine List" };
            cbxDocumentType.Properties.Items.AddRange(types);

            cbxDocumentType.SelectedIndex = 0;
        }
        #endregion

        #region Preview Handling
        private void DisplayPreview()
        {
            pdfViewer.Visible = false;
            pictureViewer.Visible = false;

            if (_fileExtension == ".pdf")
            {
                using (var stream = new MemoryStream(_fileData))
                {
                    pdfViewer.LoadDocument(stream);
                }
                pdfViewer.Visible = true;
            }
            else if (IsImage(_fileExtension))
            {
                try
                {
                    using (var stream = new MemoryStream(_fileData))
                    {
                        using (var rawImage = Image.FromStream(stream))
                        {
                            pictureViewer.Image = FixImageRotation((Image)rawImage.Clone());
                        }
                    }
                    pictureViewer.Visible = true;
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Failed to load image: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                XtraMessageBox.Show("Unsupported file type.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Save Handling
        private void btnSave_Click(object sender, EventArgs e)
        {
            var baseFileName = Path.GetFileNameWithoutExtension(txtFileName.Text.Trim());

            if (!ValidateInputs(baseFileName))
                return;

            FinalDocumentType = cbxDocumentType.Visible
                ? cbxDocumentType.SelectedItem.ToString()
                : "Bonus Document";

            var (finalData, finalExtension) = ProcessFileBeforeSave();
            var newFileName = baseFileName + finalExtension;

            if (!ConfirmOverwriteIfExists(newFileName))
                return;

            txtFileName.Text = newFileName;
            FinalFileData = finalData;
            IsConfirmed = true;
            Close();
        }

        private bool ValidateInputs(string baseFileName)
        {
            if (cbxDocumentType.Visible && cbxDocumentType.SelectedItem == null)
            {
                XtraMessageBox.Show("Please select a Document Type.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                XtraMessageBox.Show("File name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private (byte[] data, string extension) ProcessFileBeforeSave()
        {
            if (!IsImage(_fileExtension))
                return (_fileData, _fileExtension);

            var result = XtraMessageBox.Show(
                "Do you want to convert this image to PDF before saving?",
                "Convert to PDF?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    return (ConvertImageToPdf(_fileData), ".pdf");
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"Failed to convert image to PDF: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return (_fileData, _fileExtension);
        }

        private bool ConfirmOverwriteIfExists(string fileName)
        {
            if (!_docService.DocumentExists(_modelName, fileName))
                return true;

            var overwrite = XtraMessageBox.Show(
                $"A document named '{fileName}' already exists under model '{_modelName}'.\nDo you want to overwrite it?",
                "Confirm Overwrite",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return overwrite == DialogResult.Yes;
        }

        private static byte[] ConvertImageToPdf(byte[] imageData)
        {
            using (var ms = new MemoryStream(imageData))
            using (var image = Image.FromStream(ms))
            {
                using (var fixedImage = FixImageRotation((Image)image.Clone()))
                using (var portraitImage = AutoRotateToPortrait((Image)fixedImage.Clone()))
                using (var resizedImage = ResizeImage(portraitImage, 1200))
                {
                    return ImageToPdfConverter.ConvertImageToPdf(resizedImage);
                }
            }
        }
        #endregion

        #region Helpers
        private void btnCancel_Click(object sender, EventArgs e) => Close();
        private static bool IsImage(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }
        public static Image AutoRotateToPortrait(Image img)
        {
            if (img.Width > img.Height)
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return img;
        }

        public static Image ResizeImage(Image image, int maxWidth)
        {
            if (image.Width <= maxWidth)
                return (Image)image.Clone();

            int newWidth = maxWidth;
            int newHeight = (int)(image.Height * (float)newWidth / image.Width);
            var resized = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(resized))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return resized;
        }

        private static Image FixImageRotation(Image img)
        {
            const int OrientationId = 0x0112;

            if (!img.PropertyIdList.Contains(OrientationId))
                return img;

            try
            {
                var prop = img.GetPropertyItem(OrientationId);
                int orientationValue = BitConverter.ToUInt16(prop.Value, 0);

                switch (orientationValue)
                {
                    case 2: img.RotateFlip(RotateFlipType.RotateNoneFlipX); break;
                    case 3: img.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                    case 4: img.RotateFlip(RotateFlipType.Rotate180FlipX); break;
                    case 5: img.RotateFlip(RotateFlipType.Rotate90FlipX); break;
                    case 6: img.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                    case 7: img.RotateFlip(RotateFlipType.Rotate270FlipX); break;
                    case 8: img.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                }

                img.RemovePropertyItem(OrientationId);
            }
            catch { }

            return img;
        }
        #endregion
    }
}
