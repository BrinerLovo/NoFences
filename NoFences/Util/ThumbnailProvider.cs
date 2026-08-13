using NoFences.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NoFences.Util
{
    public sealed class ThumbnailProvider : IDisposable
    {
        private static readonly HashSet<string> SupportedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tiff", ".tif"
            };

        private const int MaxThumbnailSize = 32;
        private readonly object cacheLock = new object();
        private readonly Dictionary<string, Icon> iconCache =
            new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Icon> retiredIcons = new List<Icon>();
        private readonly HashSet<string> pendingThumbnails =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(4);
        private readonly SynchronizationContext synchronizationContext;
        private bool disposed;

        public ThumbnailProvider()
        {
            synchronizationContext = SynchronizationContext.Current;
        }

        public event EventHandler IconThumbnailLoaded;

        public bool IsSupported(string path)
        {
            return SupportedExtensions.Contains(Path.GetExtension(path));
        }

        public Icon GetIcon(string path, bool isFolder)
        {
            if (isFolder)
                return IconUtil.FolderLarge;

            lock (cacheLock)
            {
                if (iconCache.TryGetValue(path, out Icon cachedIcon))
                    return cachedIcon;
            }

            Icon initialIcon = ExtractAssociatedIcon(path);
            bool startThumbnail;
            lock (cacheLock)
            {
                if (iconCache.TryGetValue(path, out Icon existingIcon))
                {
                    initialIcon.Dispose();
                    return existingIcon;
                }

                iconCache.Add(path, initialIcon);
                startThumbnail = IsSupported(path) && pendingThumbnails.Add(path);
            }

            if (startThumbnail)
                Task.Run(() => GenerateThumbnailAsync(path));

            return initialIcon;
        }

        public Icon GenerateThumbnail(string path)
        {
            return GetIcon(path, isFolder: false);
        }

        private async Task GenerateThumbnailAsync(string path)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (disposed || !File.Exists(path))
                    return;

                Icon thumbnail = CreateThumbnail(path);
                if (thumbnail == null)
                    return;

                lock (cacheLock)
                {
                    if (disposed)
                    {
                        thumbnail.Dispose();
                        return;
                    }

                    if (iconCache.TryGetValue(path, out Icon oldIcon))
                        retiredIcons.Add(oldIcon);
                    iconCache[path] = thumbnail;
                }

                RaiseThumbnailLoaded();
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is ExternalException)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to generate thumbnail for '{path}': {ex.Message}");
            }
            finally
            {
                lock (cacheLock)
                    pendingThumbnails.Remove(path);
                semaphore.Release();
            }
        }

        private static Icon CreateThumbnail(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (Image image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false))
            using (var canvas = new Bitmap(MaxThumbnailSize, MaxThumbnailSize))
            using (Graphics graphics = Graphics.FromImage(canvas))
            {
                Size scaledSize = GetScaledSize(
                    image.Width,
                    image.Height,
                    MaxThumbnailSize,
                    MaxThumbnailSize);

                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(
                    image,
                    (MaxThumbnailSize - scaledSize.Width) / 2,
                    (MaxThumbnailSize - scaledSize.Height) / 2,
                    scaledSize.Width,
                    scaledSize.Height);

                IntPtr iconHandle = canvas.GetHicon();
                try
                {
                    using (Icon temporaryIcon = Icon.FromHandle(iconHandle))
                        return (Icon)temporaryIcon.Clone();
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        private static Icon ExtractAssociatedIcon(string path)
        {
            Icon icon = null;
            try
            {
                icon = Icon.ExtractAssociatedIcon(path);
                return icon != null ? (Icon)icon.Clone() : (Icon)IconUtil.UnknownFile.Clone();
            }
            catch
            {
                return (Icon)IconUtil.UnknownFile.Clone();
            }
            finally
            {
                icon?.Dispose();
            }
        }

        private void RaiseThumbnailLoaded()
        {
            EventHandler handler = IconThumbnailLoaded;
            if (handler == null || disposed)
                return;

            if (synchronizationContext != null)
                synchronizationContext.Post(_ => handler(this, EventArgs.Empty), null);
            else
                handler(this, EventArgs.Empty);
        }

        public static Size GetScaledSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            if (originalWidth <= 0 || originalHeight <= 0 || maxWidth <= 0 || maxHeight <= 0)
                return Size.Empty;

            float ratio = Math.Min((float)maxWidth / originalWidth, (float)maxHeight / originalHeight);
            return new Size(
                Math.Max(1, (int)Math.Round(originalWidth * ratio)),
                Math.Max(1, (int)Math.Round(originalHeight * ratio)));
        }

        public void Dispose()
        {
            lock (cacheLock)
            {
                if (disposed)
                    return;

                disposed = true;
                foreach (Icon icon in iconCache.Values)
                    icon.Dispose();
                for (int index = 0; index < retiredIcons.Count; index++)
                    retiredIcons[index].Dispose();
                iconCache.Clear();
                retiredIcons.Clear();
                pendingThumbnails.Clear();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
