using NoFences.Misc;
using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private SolidBrush darkBrush;
        private SolidBrush lightBrush;
        private SolidBrush headerBrush;
        private SolidBrush windowBrush;
        private SolidBrush scrollBarBrush;
        private SolidBrush textBrush;
        private SolidBrush textShadowBrush;
        private SolidBrush selectedHoverBrush;
        private SolidBrush selectedBrush;
        private SolidBrush hoverBrush;
        private Pen itemOutlinePen;

        private readonly StringFormat titleStringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center
        };
        private readonly StringFormat itemStringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        private readonly List<FenceEntry> entryCache = new List<FenceEntry>();
        private readonly List<string> entryCachePaths = new List<string>();

        private Fader opacityFader;

        public void RefreshBrushes()
        {
            DisposeBrushes();

            darkBrush = new SolidBrush(Color.Black);
            lightBrush = new SolidBrush(Color.White);
            headerBrush = new SolidBrush(Color.FromArgb(headerAlpha, headerColor));
            windowBrush = new SolidBrush(Color.FromArgb(windowAlpha, windowColor));
            scrollBarBrush = new SolidBrush(Color.FromArgb(150, Color.Black));
            textBrush = new SolidBrush(Color.FromArgb(225, Color.White));
            textShadowBrush = new SolidBrush(Color.FromArgb(100, 15, 15, 15));
            selectedHoverBrush = new SolidBrush(Color.FromArgb(100, SystemColors.GradientActiveCaption));
            selectedBrush = new SolidBrush(Color.FromArgb(80, SystemColors.GradientInactiveCaption));
            hoverBrush = new SolidBrush(Color.FromArgb(80, SystemColors.ActiveCaption));
            itemOutlinePen = new Pen(Color.FromArgb(120, SystemColors.ActiveBorder));
            Opacity = overallOpacity;
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(windowBrush, ClientRectangle);

            // Preserve the established appearance and draw order.
            graphics.DrawString(
                Text,
                titleFont,
                lightBrush,
                new PointF(Width / 2f, titleOffset),
                titleStringFormat);
            graphics.FillRectangle(headerBrush, new RectangleF(0, 0, Width, titleHeight));

            if (IsMinified)
                return;

            EnsureEntryCache();

            int x = itemPadding;
            int y = itemPadding;
            scrollHeight = 0;
            totalHeight = 0;

            int viewableHeight = Math.Max(0, Height - titleHeight);
            Rectangle contentBounds = new Rectangle(0, titleHeight, Width, viewableHeight);
            GraphicsState graphicsState = graphics.Save();
            graphics.SetClip(contentBounds);

            Point mousePosition = PointToClient(MousePosition);
            for (int index = 0; index < entryCache.Count; index++)
            {
                FenceEntry entry = entryCache[index];
                if (entry == null)
                    continue;

                int itemY = y + titleHeight - scrollOffset;

                if (itemY + itemHeight >= titleHeight
                    && itemY <= Height)
                {
                    RenderEntry(graphics, entry, x, itemY, mousePosition);
                }

                int itemBottom = y + itemHeight;
                if (itemBottom > scrollHeight)
                {
                    scrollHeight = itemBottom;
                    totalHeight = itemBottom;
                }

                x += itemWidth + itemPadding;
                if (x + itemWidth > Width)
                {
                    x = itemPadding;
                    y += itemHeight + itemPadding;
                }
            }

            totalHeight += titleHeight;
            scrollHeight = Math.Max(0, scrollHeight - viewableHeight);
            scrollOffset = Math.Min(scrollOffset, scrollHeight);

            if (scrollHeight > 0 && viewableHeight > 0)
            {
                int scrollbarHeight = Math.Max(
                    10,
                    (int)((float)viewableHeight / (viewableHeight + scrollHeight) * viewableHeight));
                int scrollbarPosition = titleHeight
                    + (int)((float)scrollOffset / scrollHeight * (viewableHeight - scrollbarHeight));
                graphics.FillRectangle(
                    scrollBarBrush,
                    new Rectangle(Width - 5, scrollbarPosition, 5, scrollbarHeight));
            }

            graphics.Restore(graphicsState);

            if (shouldUpdateSelection && !hasSelectionUpdated)
                selectedItem = null;
            if (!hasHoverUpdated)
                hoveringItem = null;

            shouldRunDoubleClick = false;
            shouldUpdateSelection = false;
            hasSelectionUpdated = false;
            hasHoverUpdated = false;
        }

        private void EnsureEntryCache()
        {
            bool rebuild = entryCachePaths.Count != fenceInfo.Files.Count;
            if (!rebuild)
            {
                for (int index = 0; index < entryCachePaths.Count; index++)
                {
                    if (!string.Equals(
                        entryCachePaths[index],
                        fenceInfo.Files[index],
                        StringComparison.OrdinalIgnoreCase))
                    {
                        rebuild = true;
                        break;
                    }
                }
            }

            if (!rebuild)
                return;

            entryCache.Clear();
            entryCachePaths.Clear();
            for (int index = 0; index < fenceInfo.Files.Count; index++)
            {
                entryCachePaths.Add(fenceInfo.Files[index]);
                entryCache.Add(FenceEntry.FromPath(fenceInfo.Files[index]));
            }
        }

        private void RenderEntry(Graphics graphics, FenceEntry entry, int x, int y, Point mousePosition)
        {
            if (isAnimating)
                return;

            Icon icon = entry.ExtractIcon(thumbnailProvider);
            string name = entry.Name;
            var textPosition = new PointF(x, y + icon.Height + 5);
            var textMaxSize = new SizeF(itemWidth, textHeight);
            SizeF textSize = graphics.MeasureString(name, iconFont, textMaxSize, itemStringFormat);
            var outlineRect = new Rectangle(
                x - 2,
                y - 2,
                itemWidth + 2,
                icon.Height + (int)textSize.Height + 7);
            Rectangle outlineRectInner = outlineRect.Shrink(1);
            bool mouseOver = mousePosition.X >= x
                && mousePosition.Y >= y
                && mousePosition.X < x + outlineRect.Width
                && mousePosition.Y < y + outlineRect.Height;

            if (draggedItem == null && CanInteractWithContent())
            {
                if (mouseOver)
                {
                    hoveringItem = entry.Path;
                    hasHoverUpdated = true;
                }

                if (mouseOver && shouldUpdateSelection)
                {
                    selectedItem = entry.Path;
                    shouldUpdateSelection = false;
                    hasSelectionUpdated = true;
                }

                if (mouseOver && shouldRunDoubleClick)
                {
                    shouldRunDoubleClick = false;
                    entry.Open();
                }

                if (string.Equals(selectedItem, entry.Path, StringComparison.OrdinalIgnoreCase))
                {
                    graphics.DrawRectangle(itemOutlinePen, outlineRectInner);
                    graphics.FillRectangle(mouseOver ? selectedHoverBrush : selectedBrush, outlineRect);
                }
                else if (mouseOver)
                {
                    graphics.DrawRectangle(itemOutlinePen, outlineRectInner);
                    graphics.FillRectangle(hoverBrush, outlineRect);
                }
            }

            graphics.DrawIcon(icon, x + itemWidtHalf - (icon.Width / 2), y);
            graphics.DrawString(
                name,
                iconFont,
                textShadowBrush,
                new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize),
                itemStringFormat);
            graphics.DrawString(
                name,
                iconFont,
                textBrush,
                new RectangleF(textPosition, textMaxSize),
                itemStringFormat);
        }

        public void SetOverallOpacity(double opacity)
        {
            if (Properties.Settings.Default.reduceAnimations)
            {
                Opacity = opacity;
                return;
            }

            if (opacityFader == null)
                opacityFader = new Fader(0.22f);

            opacityFader.StartFade(Opacity, opacity, value => Opacity = value);
        }

        private void DisposeDrawingResources()
        {
            opacityFader?.Dispose();
            opacityFader = null;
            titleStringFormat.Dispose();
            itemStringFormat.Dispose();
            DisposeBrushes();
            titleFont?.Dispose();
            titleFont = null;
            iconFont?.Dispose();
            iconFont = null;
        }

        private void DisposeBrushes()
        {
            darkBrush?.Dispose();
            lightBrush?.Dispose();
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
