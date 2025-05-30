using NoFences.Misc;
using NoFences.Model;
using NoFences.Util;
using System;
using System.Drawing;
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

        private Fader opacityFader;
        private bool suspendContentDraw = false;

        public void RefreshBrushes()
        {
            darkBrush = new SolidBrush(Color.Black);
            lightBrush = new SolidBrush(Color.White);
            headerBrush = new SolidBrush(Color.FromArgb(headerAlpha, headerColor));
            windowBrush = new SolidBrush(Color.FromArgb(windowAlpha, windowColor));
            scrollBarBrush = new SolidBrush(Color.FromArgb(150, Color.Black));
            textBrush = new SolidBrush(Color.FromArgb(225, Color.White));
            textShadowBrush = new SolidBrush(Color.FromArgb(100, 15, 15, 15));
            this.Opacity = overallOpacity;
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clip = new Region(ClientRectangle);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            g.FillRectangle(windowBrush, ClientRectangle);

            // Title
            g.DrawString(Text, titleFont, lightBrush, new PointF(Width / 2, titleOffset), new StringFormat { Alignment = StringAlignment.Center });
            g.FillRectangle(headerBrush, new RectangleF(0, 0, Width, titleHeight));

            if (IsMinified)
            {
                return;
            }

            // Items
            var x = itemPadding;
            var y = itemPadding;
            scrollHeight = 0;
            totalHeight = 0;

            // Items Clipping Area
            int viewableHeight = Height - titleHeight; // The height available for content
            g.Clip = new Region(new Rectangle(0, titleHeight, Width, viewableHeight));

            var files = fenceInfo.Files.ToArray();
            int index = -1;
            foreach (var file in files)
            {
                index++;
                var entry = FenceEntry.FromPath(file);
                if (entry == null) continue;

                RenderEntry(g, entry, x, y + titleHeight - scrollOffset, index);

                var itemBottom = y + itemHeight;
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

            // Calculate Scroll Height (Total content height minus the visible area)
            scrollHeight = Math.Max(0, scrollHeight - viewableHeight);

            // Ensure scrollOffset is within bounds
            scrollOffset = Math.Min(scrollOffset, scrollHeight);

            // Scroll bars
            if (scrollHeight > 0)
            {
                // Scrollbar height should be proportional to the visible vs. total content height
                int scrollbarHeight = Math.Max(10, (int)((float)viewableHeight / (viewableHeight + scrollHeight) * viewableHeight));

                int scrollbarPosition = titleHeight + (int)((float)scrollOffset / scrollHeight * (viewableHeight - scrollbarHeight));

                e.Graphics.FillRectangle(scrollBarBrush, new Rectangle(Width - 5, scrollbarPosition, 5, scrollbarHeight));
            }

            // Click handlers
            if (shouldUpdateSelection && !hasSelectionUpdated)
                selectedItem = null;

            if (!hasHoverUpdated)
                hoveringItem = null;

            shouldRunDoubleClick = false;
            shouldUpdateSelection = false;
            hasSelectionUpdated = false;
            hasHoverUpdated = false;
        }

        private void RenderEntry(Graphics g, FenceEntry entry, int x, int y, int index)
        {
            if (isAnimating || suspendContentDraw) return; // avoid lagging

            var icon = entry.ExtractIcon(thumbnailProvider);
            var name = entry.Name;

            var textPosition = new PointF(x, y + icon.Height + 5);
            var textMaxSize = new SizeF(itemWidth, textHeight);

            var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            var textSize = g.MeasureString(name, iconFont, textMaxSize, stringFormat);
            var outlineRect = new Rectangle(x - 2, y - 2, itemWidth + 2, icon.Height + (int)textSize.Height + 5 + 2);
            var outlineRectInner = outlineRect.Shrink(1);

            var mousePos = PointToClient(MousePosition);
            var mouseOver = mousePos.X >= x && mousePos.Y >= y && mousePos.X < x + outlineRect.Width && mousePos.Y < y + outlineRect.Height;

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

                if (mouseOver && MouseButtons == MouseButtons.Left && selectedItem != null)
                {
                    if (Math.Abs(mousePos.X - x) > 8 || Math.Abs(mousePos.Y - y) > 8) // Prevent accidental drag
                    {
                        draggedItemIndex = index;
                        draggedItem = selectedItem;
                        DoDragDrop(entry.Path, DragDropEffects.Move);
                    }
                }

                if (selectedItem == entry.Path)
                {
                    if (mouseOver)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, SystemColors.GradientActiveCaption)), outlineRect);
                    }
                    else
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(80, SystemColors.GradientInactiveCaption)), outlineRect);
                    }
                }
                else
                {
                    if (mouseOver)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(80, SystemColors.ActiveCaption)), outlineRect);
                    }
                }
            }

            g.DrawIcon(icon, x + itemWidtHalf - (icon.Width / 2), y);
            g.DrawString(name, iconFont, textShadowBrush, new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize), stringFormat);
            g.DrawString(name, iconFont, textBrush, new RectangleF(textPosition, textMaxSize), stringFormat);
        }

        public void SetOverallOpacity(double opacity)
        {
            if (opacityFader == null)
            {
                opacityFader = new Fader(0.22f).OnFinish(() =>
                {
                    suspendContentDraw = false;
                });
            }

            suspendContentDraw = true;
            opacityFader.StartFade(this.Opacity, opacity, value =>
            {
                Opacity = value;
                Refresh();
            });
        }
    }
}
