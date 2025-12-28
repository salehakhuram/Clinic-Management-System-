using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ClinicManagement
{
    public static class ImageHelper
    {
        private static string UploadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", "Images");

        static ImageHelper()
        {
            if (!Directory.Exists(UploadsFolder))
            {
                Directory.CreateDirectory(UploadsFolder);
            }
        }

        public static string SaveImage(Image img, int maxWidth = 300, int maxHeight = 300)
        {
            if (img == null) return null;

            try
            {
                string fileName = Guid.NewGuid().ToString() + ".jpg";
                string fullPath = Path.Combine(UploadsFolder, fileName);

                using (Image resized = ResizeImage(img, maxWidth, maxHeight))
                {
                    resized.Save(fullPath, ImageFormat.Jpeg);
                }

                return Path.Combine("Uploads", "Images", fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static Image LoadImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return GetDefaultAvatar();

            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (File.Exists(fullPath))
                {
                    // Use memory stream to avoid locking the file
                    using (var ms = new MemoryStream(File.ReadAllBytes(fullPath)))
                    {
                        return Image.FromStream(ms);
                    }
                }
            }
            catch
            {
                // Fallback to default if error
            }

            return GetDefaultAvatar();
        }

        public static void DeleteImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Ignore delete errors
            }
        }

        private static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private static Image GetDefaultAvatar()
        {
            // Create a simple default avatar if no resource is found
            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(240, 240, 240));
                using (Pen p = new Pen(Color.Gray, 2))
                {
                    g.DrawEllipse(p, 10, 10, 80, 80);
                    g.DrawEllipse(p, 30, 30, 15, 15);
                    g.DrawEllipse(p, 55, 30, 15, 15);
                    g.DrawArc(p, 30, 50, 40, 30, 0, 180);
                }
            }
            return bmp;
        }
    }
}
