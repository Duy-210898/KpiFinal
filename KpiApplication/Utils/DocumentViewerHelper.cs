using DevExpress.XtraEditors;
using DevExpress.XtraPdfViewer;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DevExpress.Utils.Extensions;

namespace KpiApplication.Utils
{
    public class DocumentViewerHelper
    {
        public static bool DisplayDocument(
            Control control,
            PdfViewer pdfViewer,
            PictureEdit pictureViewer,
            LabelControl lblFileName,
            BonusDocument_Model doc,
            byte[] data,
            ref MemoryStream currentStream,
            LruCache<int, Image> imageCache,
            out string errorMessage)
        {
            errorMessage = null;

            if (doc == null || data == null || data.Length == 0)
            {
                errorMessage = "This document has no data.";
                return false;
            }

            control.InvokeSafe(() =>
            {
                pdfViewer.Visible = false;
                pictureViewer.Visible = false;
            });

            string ext = Path.GetExtension(doc.FileName)?.ToLowerInvariant();
            var stream = new MemoryStream(data);

            try
            {
                if (ext == ".pdf")
                {
                    control.InvokeSafe(() =>
                    {
                        pdfViewer.CloseDocument();
                        pdfViewer.LoadDocument(stream);
                        pdfViewer.Visible = true;
                    });
                }
                else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                {
                    if (imageCache != null && imageCache.TryGetValue(doc.Id, out var cachedImage))
                    {
                        control.InvokeSafe(() =>
                        {
                            pictureViewer.Image = cachedImage;
                            pictureViewer.Visible = true;
                        });
                        stream.Dispose();
                    }
                    else
                    {
                        control.InvokeSafe(() =>
                        {
                            using (var img = Image.FromStream(stream))
                            {
                                var fixedImg = ImageRotationHelper.FixImageRotation((Image)img.Clone());
                                pictureViewer.Image = fixedImg;
                                pictureViewer.Visible = true;
                                imageCache?.AddOrUpdate(doc.Id, fixedImg);
                            }
                        });
                    }
                }
                else
                {
                    stream.Dispose();
                    errorMessage = "Unsupported file type.";
                    return false;
                }

                control.InvokeSafe(() =>
                {
                    lblFileName.Text = "\uD83D\uDCC4 " + doc.FileName;
                });

                currentStream?.Dispose();
                currentStream = stream;

                return true;
            }
            catch (Exception ex)
            {
                stream.Dispose();
                errorMessage = "Failed to load document: " + ex.Message;
                return false;
            }
        }
    }
}
