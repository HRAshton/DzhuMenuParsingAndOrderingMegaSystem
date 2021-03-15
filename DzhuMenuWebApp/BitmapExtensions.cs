using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DzhuMenuWebApp
{
	internal static class BitmapExtensions
	{
		public static bool IsWhite(this Bitmap bmp, int x, int y)
		{
			return bmp.GetPixel(x, y).GetBrightness() > 0.5;
		}

		public static byte[] ToByteArray(this Image image, ImageFormat format)
		{
			using var ms = new MemoryStream();
			image.Save(ms, format);
			return ms.ToArray();
		}
	}
}