using NoFences.Layout;
using NoFences.Model;
using NoFences.Util;
using Peter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace NoFences.Rendering
{
    internal sealed class FenceRenderer : IDisposable
    {
        private const float TextShadowDistance = 1F;
        private readonly ThumbnailProvider thumbnailProvider;
        private readonly Dictionary<string, FenceEntry> entries =
            new Dictionary<string, FenceEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> details =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly StringFormat titleFormat = new StringFormat { Alignment = StringAlignment.Center };
        private readonly StringFormat itemFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        private Font titleFont;
        private Font itemFont;
        private SolidBrush titleBrush;
        private SolidBrush headerBrush;
        private SolidBrush windowBrush;
        private SolidBrush scrollBarBrush;
        private SolidBrush textBrush;
        private SolidBrush textShadowBrush;
        private SolidBrush selectedHoverBrush;
        private SolidBrush selectedBrush;
        private SolidBrush hoverBrush;
        private Pen itemOutlinePen;

        public FenceRenderer(ThumbnailProvider thumbnailProvider)
        {
            this.thumbnailProvider = thumbnailProvider ?? throw new ArgumentNullException(nameof(thumbnailProvider));
        }

        public void UpdateAppearance(
            int logicalTitleHeight,
            Color headerColor,
            int headerAlpha,
            Color windowColor,
            int windowAlpha)
        {
            DisposeAppearanceResources();
            titleFont = new Font("Segoe UI", (int)Math.Floor(logicalTitleHeight / 2D));
            itemFont = new Font("Segoe UI", 9F);
            titleBrush = new SolidBrush(Color.White);
            headerBrush = new SolidBrush(Color.FromArgb(headerAlpha, headerColor));
            windowBrush = new SolidBrush(Color.FromArgb(windowAlpha, windowColor));
            scrollBarBrush = new SolidBrush(Color.FromArgb(150, Color.Black));
            textBrush = new SolidBrush(Color.FromArgb(225, Color.White));
            textShadowBrush = new SolidBrush(Color.FromArgb(100, 15, 15, 15));
            selectedHoverBrush = new SolidBrush(Color.FromArgb(100, SystemColors.GradientActiveCaption));
            selectedBrush = new SolidBrush(Color.FromArgb(80, SystemColors.GradientInactiveCaption));
            hoverBrush = new SolidBrush(Color.FromArgb(80, SystemColors.ActiveCaption));
            itemOutlinePen = new Pen(Color.FromArgb(120, SystemColors.ActiveBorder));
        }

        public void Render(
            Graphics graphics,
            Rectangle clientRectangle,
            string title,
            int titleHeight,
            FenceLayoutSnapshot layout,
            ISet<string> selectedPaths,
            string hoveredPath,
            bool minified,
            bool animating)
        {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(windowBrush, clientRectangle);
            graphics.FillRectangle(headerBrush, new RectangleF(0, 0, clientRectangle.Width, titleHeight));
            graphics.DrawString(title, titleFont, titleBrush, new PointF(clientRectangle.Width / 2F, 1F), titleFormat);
            if (minified || animating)
                return;

            GraphicsState state = graphics.Save();
            int viewableHeight = Math.Max(0, clientRectangle.Height - titleHeight);
            graphics.SetClip(new Rectangle(0, titleHeight, clientRectangle.Width, viewableHeight));
            for (int index = 0; index < layout.Items.Count; index++)
            {
                FenceLayoutItem item = layout.Items[index];
                if (item.Bounds.Bottom < titleHeight || item.Bounds.Top > clientRectangle.Height)
                    continue;
                RenderItem(graphics, item, layout.DisplayMode, selectedPaths.Contains(item.Path),
                    string.Equals(hoveredPath, item.Path, StringComparison.OrdinalIgnoreCase));
            }

            if (layout.ScrollHeight > 0 && viewableHeight > 0)
            {
                int scrollBarHeight = Math.Max(
                    10,
                    (int)((float)viewableHeight / (viewableHeight + layout.ScrollHeight) * viewableHeight));
                int maximumOffset = Math.Max(1, layout.ScrollHeight);
                int currentOffset = layout.Items.Count == 0
                    ? 0
                    : Math.Max(0, titleHeight + FenceLayout.ItemPadding - layout.Items[0].Bounds.Top);
                int scrollBarPosition = titleHeight
                    + (int)((float)currentOffset / maximumOffset * (viewableHeight - scrollBarHeight));
                graphics.FillRectangle(
                    scrollBarBrush,
                    new Rectangle(clientRectangle.Width - 5, scrollBarPosition, 5, scrollBarHeight));
            }
            graphics.Restore(state);
        }

        private void RenderItem(
            Graphics graphics,
            FenceLayoutItem item,
            FenceDisplayMode displayMode,
            bool selected,
            bool hovered)
        {
            FenceEntry entry = GetEntry(item.Path);
            if (entry == null)
                return;

            Icon icon = entry.ExtractIcon(thumbnailProvider);
            if (icon == null)
                return;

            if (displayMode != FenceDisplayMode.Icons)
            {
                RenderListItem(graphics, item, entry, icon, displayMode, selected, hovered);
                return;
            }
            int iconX = item.Bounds.Left + FenceLayout.ItemWidth / 2 - icon.Width / 2;
            var textPosition = new PointF(item.Bounds.Left, item.Bounds.Top + icon.Height + 5);
            var textSize = new SizeF(FenceLayout.ItemWidth, FenceLayout.TextHeight);
            var outline = new Rectangle(
                item.Bounds.Left - 2,
                item.Bounds.Top - 2,
                FenceLayout.ItemWidth + 2,
                FenceLayout.ItemHeight + 2);

            if (selected)
            {
                graphics.DrawRectangle(itemOutlinePen, outline.Shrink(1));
                graphics.FillRectangle(hovered ? selectedHoverBrush : selectedBrush, outline);
            }
            else if (hovered)
            {
                graphics.DrawRectangle(itemOutlinePen, outline.Shrink(1));
                graphics.FillRectangle(hoverBrush, outline);
            }

            graphics.DrawIcon(icon, iconX, item.Bounds.Top);
            graphics.DrawString(
                entry.Name,
                itemFont,
                textShadowBrush,
                new RectangleF(textPosition.Move(TextShadowDistance, TextShadowDistance), textSize),
                itemFormat);
            graphics.DrawString(entry.Name, itemFont, textBrush, new RectangleF(textPosition, textSize), itemFormat);
        }

        private void RenderListItem(
            Graphics graphics,
            FenceLayoutItem item,
            FenceEntry entry,
            Icon icon,
            FenceDisplayMode displayMode,
            bool selected,
            bool hovered)
        {
            Rectangle outline = item.Bounds;
            if (selected)
            {
                graphics.FillRectangle(hovered ? selectedHoverBrush : selectedBrush, outline);
                graphics.DrawRectangle(itemOutlinePen, outline.Shrink(1));
            }
            else if (hovered)
            {
                graphics.FillRectangle(hoverBrush, outline);
                graphics.DrawRectangle(itemOutlinePen, outline.Shrink(1));
            }

            int iconSize = displayMode == FenceDisplayMode.CompactList ? 20 : 28;
            var iconBounds = new Rectangle(
                outline.Left + 5,
                outline.Top + Math.Max(0, (outline.Height - iconSize) / 2),
                iconSize,
                iconSize);
            graphics.DrawIcon(icon, iconBounds);

            int textLeft = iconBounds.Right + 8;
            if (displayMode == FenceDisplayMode.CompactList)
            {
                TextRenderer.DrawText(
                    graphics,
                    entry.Name,
                    itemFont,
                    new Rectangle(textLeft, outline.Top, Math.Max(1, outline.Right - textLeft - 5), outline.Height),
                    textBrush.Color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            TextRenderer.DrawText(
                graphics,
                entry.Name,
                itemFont,
                new Rectangle(textLeft, outline.Top + 3, Math.Max(1, outline.Right - textLeft - 5), 20),
                textBrush.Color,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(
                graphics,
                GetDetails(entry),
                itemFont,
                new Rectangle(textLeft, outline.Top + 22, Math.Max(1, outline.Right - textLeft - 5), 18),
                Color.FromArgb(170, Color.White),
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private string GetDetails(FenceEntry entry)
        {
            if (details.TryGetValue(entry.Path, out string cachedDetails))
                return cachedDetails;

            string value;
            try
            {
                if (entry.Type == EntryType.Folder)
                    value = "Folder  ·  " + Directory.GetLastWriteTime(entry.Path).ToString("g", CultureInfo.CurrentCulture);
                else
                {
                    var file = new FileInfo(entry.Path);
                    value = FormatSize(file.Length) + "  ·  " + file.LastWriteTime.ToString("g", CultureInfo.CurrentCulture);
                }

            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                value = entry.Type == EntryType.Folder ? "Folder" : Path.GetExtension(entry.Path).TrimStart('.').ToUpperInvariant();
            }
            details[entry.Path] = value;
            return value;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1024L * 1024L)
                return (bytes / 1024D).ToString("0.#", CultureInfo.CurrentCulture) + " KB";
            if (bytes < 1024L * 1024L * 1024L)
                return (bytes / (1024D * 1024D)).ToString("0.#", CultureInfo.CurrentCulture) + " MB";
            return (bytes / (1024D * 1024D * 1024D)).ToString("0.#", CultureInfo.CurrentCulture) + " GB";
        }

        private FenceEntry GetEntry(string path)
        {
            if (entries.TryGetValue(path, out FenceEntry entry))
                return entry;
            entry = FenceEntry.FromPath(path);
            entries[path] = entry;
            return entry;
        }

        public void InvalidateEntries()
        {
            entries.Clear();
            details.Clear();
        }

        public void Dispose()
        {
            DisposeAppearanceResources();
            titleFormat.Dispose();
            itemFormat.Dispose();
            entries.Clear();
            details.Clear();
        }

        private void DisposeAppearanceResources()
        {
            titleFont?.Dispose();
            itemFont?.Dispose();
            titleBrush?.Dispose();
            headerBrush?.Dispose();
            windowBrush?.Dispose();
            scrollBarBrush?.Dispose();
            textBrush?.Dispose();
            textShadowBrush?.Dispose();
            selectedHoverBrush?.Dispose();
            selectedBrush?.Dispose();
            hoverBrush?.Dispose();
            itemOutlinePen?.Dispose();
        }
    }
}
