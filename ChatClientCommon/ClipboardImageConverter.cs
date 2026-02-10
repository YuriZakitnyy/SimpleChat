using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CharCommon;

namespace ChatClientCommon
{
    /// <summary>
    /// Helper to read an image from the clipboard, encode it as PNG and convert to a compact string
    /// using <see cref="ByteStringConverter.ToZ85String(byte[])"/>.
    /// </summary>
    public static class ClipboardImageConverter
    {
        /// <summary>
        /// Reads the image from the clipboard (if any), converts it to PNG and returns the encoded string.
        /// Returns null when no image is available.
        /// Note: Clipboard access must occur on an STA thread (UI thread in WPF).
        /// </summary>
        public static string GetPngStringFromClipboard()
        {
            if (!Clipboard.ContainsImage())
            {
                return null;
            }

            var bitmap = Clipboard.GetImage();
            if (bitmap == null)
            {
                return null;
            }

            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(ms);

                var bytes = ms.ToArray();
                return ByteStringConverter.ToZ85String(bytes);
            }
        }
    }
}