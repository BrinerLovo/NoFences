using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NoFences.Util
{
    public class ThumbnailProvider
    {
        // Supported .NET images as per https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromfile
        private static readonly string[] SupportedExtensions =
        {
            ".bmp",
            ".gif",
            ".jpg",
            ".jpeg",
            ".png",
            ".tiff",
            ".tif",
        };

        private class ThumbnailState
        {
            public Icon icon;
        }

        // Only allow 4 concurrent images to be decoded to try and prevent OOM errors
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(4);
        private readonly IDictionary<string, ThumbnailState> iconCache = new Dictionary<string, ThumbnailState>();
        public event EventHandler IconThumbnailLoaded;
        private const int MAX_THUMBNAIL_SIZE = 32;

        public bool IsSupported(string path)
        {
            return SupportedExtensions.Any(ext => path.EndsWith(ext));
        }

        public Icon GenerateThumbnail(string path)
        {
            if (!iconCache.ContainsKey(path))
            {
                return SubmitGeneratorTask(path).icon;
            }
            else
            {
                return iconCache[path].icon;
            }
        }

        private ThumbnailState SubmitGeneratorTask(string path)
        {
            var state = new ThumbnailState() { icon = Icon.ExtractAssociatedIcon(path) };
            iconCache[path] = state;

            Task.Run(() =>
            {
                semaphore.Wait();
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
                {
                    using (var img = Image.FromStream(ms))
                    {
                        // Compute scaled size while keeping aspect ratio
                        Size scaledSize = GetScaledSize(img.Width, img.Height, MAX_THUMBNAIL_SIZE, MAX_THUMBNAIL_SIZE);

                        using (Bitmap canvas = new Bitmap(MAX_THUMBNAIL_SIZE, MAX_THUMBNAIL_SIZE)) // Create a 32x32 canvas
                        using (Graphics g = Graphics.FromImage(canvas))
                        {
                            g.Clear(Color.Transparent); // Make background transparent
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(img, (MAX_THUMBNAIL_SIZE - scaledSize.Width) / 2, (MAX_THUMBNAIL_SIZE - scaledSize.Height) / 2, scaledSize.Width, scaledSize.Height);

                            var icon = Icon.FromHandle(canvas.GetHicon());
                            state.icon = icon;
                        }
                    }
                }
                IconThumbnailLoaded?.Invoke(this, new EventArgs());
                semaphore.Release();
            });

            return state;
        }

        public static Size GetScaledSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            float ratio = Math.Min((float)maxWidth / originalWidth, (float)maxHeight / originalHeight);
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);
            return new Size(newWidth, newHeight);
        }

    }
}
