using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using CharCommon;

namespace ChatClientCommon
{
    public class EmojiInfo
    {
        private BitmapImage _image;
        private bool _imageLoaded;
        private readonly object _imageLock = new object();

        public string Emoji { get; set; }
        public string Name { get; set; }

        public BitmapImage Image
        {
            get
            {
                if (!_imageLoaded)
                {
                    lock (_imageLock)
                    {
                        if (!_imageLoaded)
                        {
                            try
                            {
                                _image = EmojiRenderer.RenderEmojiToBitmapImage(Emoji, 32);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(typeof(EmojiInfo), ex);
                            }
                            finally
                            {
                                _imageLoaded = true;
                            }
                        }
                    }
                }
                return _image;
            }
            set
            {
                lock (_imageLock)
                {
                    _image = value;
                    _imageLoaded = true;
                }
            }
        }
    }

    public static class EmojiRenderer
    {
        private static readonly object _renderLock = new object();

        public static string RenderEmoji(string emoji, int size = 32)
        {
            if (string.IsNullOrEmpty(emoji))
            {
                return null;
            }

            lock (_renderLock)
            {
                Bitmap bitmap = null;
                Graphics graphics = null;
                Font font = null;
                MemoryStream ms = null;

                try
                {
                    bitmap = new Bitmap(size, size);
                    graphics = Graphics.FromImage(bitmap);

                    // Set high quality rendering
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.Clear(Color.Transparent);

                    // Draw emoji
                    font = new Font("Segoe UI Emoji", size * 0.7f, FontStyle.Regular, GraphicsUnit.Pixel);
                    StringFormat stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    graphics.DrawString(emoji, font, Brushes.Black,
                        new RectangleF(0, 0, size, size), stringFormat);

                    ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    return ByteStringConverter.ToZ85String(imageBytes);
                }
                catch (Exception ex)
                {
                    Logger.Error(typeof(EmojiRenderer), ex);
                    return null;
                }
                finally
                {
                    font?.Dispose();
                    graphics?.Dispose();
                    bitmap?.Dispose();
                    ms?.Dispose();
                }
            }
        }

        public static BitmapImage RenderEmojiToBitmapImage(string emoji, int size = 32)
        {
            if (string.IsNullOrEmpty(emoji))
            {
                return null;
            }

            lock (_renderLock)
            {
                Bitmap bitmap = null;
                Graphics graphics = null;
                Font font = null;
                MemoryStream ms = null;

                try
                {
                    bitmap = new Bitmap(size, size);
                    graphics = Graphics.FromImage(bitmap);

                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.Clear(Color.Transparent);

                    font = new Font("Segoe UI Emoji", size * 0.7f, FontStyle.Regular, GraphicsUnit.Pixel);
                    StringFormat stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    graphics.DrawString(emoji, font, Brushes.Black,
                        new RectangleF(0, 0, size, size), stringFormat);

                    ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
                catch (Exception ex)
                {
                    Logger.Error(typeof(EmojiRenderer), ex);
                    return null;
                }
                finally
                {
                    font?.Dispose();
                    graphics?.Dispose();
                    bitmap?.Dispose();
                    ms?.Dispose();
                }
            }
        }

        public static EmojiInfo CreateEmojiInfo(string emoji, string name)
        {
            return new EmojiInfo
            {
                Emoji = emoji,
                Name = name
                // Image is NOT set here - it will be lazily loaded when accessed
            };
        }
    }
}