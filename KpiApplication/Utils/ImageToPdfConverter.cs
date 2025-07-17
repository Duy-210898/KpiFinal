using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Linq; // ✅ THÊM DÒNG NÀY

namespace KpiApplication.Utils
{
    public static class ImageToPdfConverter
    {
        public static byte[] ConvertImageToPdf(Image image)
        {
            using (var msImage = new MemoryStream())
            {
                // Lưu ảnh dưới dạng JPEG đã nén
                var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 75L); // nén 75%

                image.Save(msImage, jpegCodec, encoderParams);
                msImage.Position = 0; // Reset để đọc lại

                using (var document = new PdfDocument())
                {
                    var page = document.AddPage();

                    using (var gfx = XGraphics.FromPdfPage(page))
                    using (var img = XImage.FromStream(msImage)) // ✅ Fix lỗi delegate
                    {
                        page.Width = XUnit.FromPoint(image.Width);
                        page.Height = XUnit.FromPoint(image.Height);
                        gfx.DrawImage(img, 0, 0, img.PixelWidth, img.PixelHeight);
                    }

                    using (var msPdf = new MemoryStream())
                    {
                        document.Save(msPdf, false);
                        return msPdf.ToArray();
                    }
                }
            }
        }
    }
}
