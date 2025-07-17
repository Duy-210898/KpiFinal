using DevExpress.XtraEditors;
using DevExpress.XtraPdfViewer;
using System;
using System.Drawing;
using System.IO;

namespace KpiApplication.Utils
{
    public static class ViewerResetHelper
    {
        public static void ResetViewer(
            PdfViewer pdfViewer,
            PictureEdit pictureViewer,
            LabelControl lblFileName,
            ref MemoryStream currentStream,
            ref BonusDocument_Model currentDoc)
        {
            // Clear reference to document model
            currentDoc = null;

            // Reset label
            lblFileName.Text = string.Empty;

            // Close PDF
            if (pdfViewer != null)
            {
                try
                {
                    pdfViewer.CloseDocument();
                }
                catch { /* Ignore if already closed or invalid */ }

                pdfViewer.Visible = false;
            }

            // Dispose hình ảnh nếu có
            if (pictureViewer != null)
            {
                if (pictureViewer.Image is Image img)
                {
                    try
                    {
                        img.Dispose();
                    }
                    catch { /* Log nếu cần */ }
                }

                pictureViewer.Image = null;
                pictureViewer.Visible = false;
            }

            // Dispose stream nếu còn giữ
            if (currentStream != null)
            {
                try
                {
                    currentStream.Dispose();
                }
                catch { /* Log nếu cần */ }
                currentStream = null;
            }
        }
    }
}
