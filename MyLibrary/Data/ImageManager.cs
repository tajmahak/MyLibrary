using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MyLibrary.Data
{
    public static class ImageManager
    {
        /// <summary>
        /// Изменение размера изображения
        /// </summary>
        /// <param name="image">Исходное изображение</param>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <param name="highQuality">Высококачественное изменение размера изображение (работает медленнее)</param>
        /// <returns></returns>
        public static Image GetResizeImage(Image image, int width, int height, bool highQuality)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                if (highQuality)
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                }
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        /// <summary>
        /// Накладывание изображения поверх другого
        /// </summary>
        /// <param name="image">Исходное изображение для наложения на фоновое изображение</param>
        /// <param name="backgroundImage">Фоновое изображение</param>
        /// <returns></returns>
        public static Image GetOverlayImage(Image image, Image backgroundImage)
        {
            backgroundImage = new Bitmap(backgroundImage);
            var destRect = new Rectangle(0, 0, backgroundImage.Width, backgroundImage.Height);
            using (var graphics = Graphics.FromImage(backgroundImage))
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            return backgroundImage;
        }
        /// <summary>
        /// Изображение в круглом фоне
        /// </summary>
        /// <param name="image">Исходное изображение</param>
        /// <param name="backgrColor">Цвет круга</param>
        /// <returns></returns>
        public static Image GetColorCircleImage(Image image, Color backgrColor)
        {
            int scalePixel = image.Width / 10;
            var newImg = new Bitmap(image.Width, image.Height);
            var destRect = new Rectangle(scalePixel, scalePixel, image.Width - scalePixel, image.Height - scalePixel);
            using (var graphics = Graphics.FromImage(newImg))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.FillEllipse(new SolidBrush(backgrColor), new Rectangle(0, 0, newImg.Width - 1, newImg.Height - 1));
                graphics.DrawImage(image, destRect, 0, 0, image.Width + scalePixel, image.Height + scalePixel, GraphicsUnit.Pixel);
            }
            return newImg;
        }
        /// <summary>
        /// Изображение с водяным знаком в правом нижнем углу (водяной знак не масштабируется)
        /// </summary>
        /// <param name="image">Исходное изображение</param>
        /// <param name="watermarkImage">Изображение водяного знака</param>
        /// <returns></returns>
        public static Image GetWatermarkImage(Image image, Image watermarkImage)
        {
            watermarkImage = new Bitmap(watermarkImage);
            var destRect = new Rectangle(0, 0, image.Width, image.Height);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.DrawImage(watermarkImage, destRect, -2, -2, image.Width, image.Height, GraphicsUnit.Pixel);
            }
            return image;
        }
    }
}
