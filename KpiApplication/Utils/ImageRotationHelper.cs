using System;
using System.Drawing;
using System.Linq;

namespace KpiApplication.Utils
{
    public class ImageRotationHelper
    {
        public static Image FixImageRotation(Image img)
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
