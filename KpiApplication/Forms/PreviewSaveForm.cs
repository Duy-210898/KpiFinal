using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using KpiApplication.DataAccess;
using KpiApplication.Services;
using KpiApplication.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KpiApplication.Forms
{
    public partial class PreviewSaveForm : XtraForm
    {
        private readonly byte[] fileData;
        private readonly string fileExtension;
        public bool IsConfirmed { get; private set; } = false;
        public string FileName => Path.GetFileName(txtFileName.Text?.Trim());
        public byte[] FinalFileData { get; private set; }
        public string DocumentType => cbxDocumentType.SelectedItem?.ToString();

        private readonly DocumentServices _docService = new DocumentServices();

        private readonly string modelName;
        private readonly string documentType;

        public PreviewSaveForm(byte[] fileData, string fileName, string modelName)
        {
            InitializeComponent();
            pictureViewer.Properties.SizeMode = PictureSizeMode.Squeeze;
            pictureViewer.Properties.ShowMenu = false;
            pictureViewer.Properties.ZoomAccelerationFactor = 1;
            pictureViewer.Properties.AllowScrollViaMouseDrag = false;

            this.fileData = fileData;
            this.modelName = modelName;

            txtFileName.Text = fileName;
            txtModelName.Text = modelName;
            txtModelName.Properties.ReadOnly = true;

            fileExtension = Path.GetExtension(fileName)?.ToLower();

            DisplayPreview();
        }

        // Hiển thị file PDF hoặc hình ảnh
        private void DisplayPreview()
        {
            pdfViewer.Visible = false;
            pictureViewer.Visible = false;

            if (fileExtension == ".pdf")
            {
                var stream = new MemoryStream(fileData);
                pdfViewer.LoadDocument(stream);
                pdfViewer.Visible = true;
            }
            else if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png" || fileExtension == ".bmp")
            {
                try
                {
                    using (var stream = new MemoryStream(fileData))
                    using (var rawImage = Image.FromStream(stream))
                    {
                        pictureViewer.Image = FixImageRotation((Image)rawImage.Clone());
                    }
                    pictureViewer.Visible = true;
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Failed to load image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                XtraMessageBox.Show("Unsupported file type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Sự kiện lưu file
        private void btnSave_Click(object sender, EventArgs e)
        {
            string inputName = Path.GetFileNameWithoutExtension(txtFileName.Text.Trim());
            if (string.IsNullOrWhiteSpace(inputName))
            {
                XtraMessageBox.Show("File name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cbxDocumentType.SelectedItem == null)
            {
                XtraMessageBox.Show("Please select a Document Type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedDocumentType = cbxDocumentType.SelectedItem.ToString();

            string newExtension = fileExtension;
            byte[] outputData = fileData;

            if (IsImage(fileExtension))
            {
                var result = XtraMessageBox.Show(
                    "Do you want to convert this image to PDF before saving?",
                    "Convert to PDF?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        using (var ms = new MemoryStream(fileData))
                        using (var image = Image.FromStream(ms))
                        {
                            var fixedImage = FixImageRotation((Image)image.Clone());
                            var portraitImage = AutoRotateToPortrait(fixedImage);
                            var resizedImage = ResizeImage(portraitImage, 1200);

                            outputData = ImageToPdfConverter.ConvertImageToPdf(resizedImage);

                            resizedImage.Dispose();
                            portraitImage.Dispose();
                            fixedImage.Dispose();
                        }

                        newExtension = ".pdf";
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show("Failed to convert image to PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            string newFileName = inputName + newExtension;
            txtFileName.Text = newFileName;
            FinalFileData = outputData;

            if (_docService.DocumentExists(modelName, newFileName))
            {
                var overwrite = XtraMessageBox.Show(
                    $"A document named '{newFileName}' already exists under model '{modelName}'.\nDo you want to overwrite it?",
                    "Confirm Overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (overwrite != DialogResult.Yes)
                    return;
            }

            IsConfirmed = true;
            this.Close();
        }

        private static bool IsImage(string ext) =>
            ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";

        // Sự kiện hủy
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Xoay hình ảnh sang chiều dọc nếu cần
        public static Image AutoRotateToPortrait(Image img)
        {
            if (img.Width > img.Height)
            {
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }
            return img;
        }

        // Resize hình theo chiều rộng tối đa
        public static Image ResizeImage(Image image, int maxWidth)
        {
            if (image.Width <= maxWidth)
                return (Image)image.Clone();

            int newWidth = maxWidth;
            int newHeight = (int)(image.Height * ((float)newWidth / image.Width));
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

        // Sửa hướng ảnh nếu có thông tin Exif
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
    }
}
