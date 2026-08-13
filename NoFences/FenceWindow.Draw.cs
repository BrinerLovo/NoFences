using NoFences.Layout;
using NoFences.Misc;
using NoFences.Rendering;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private readonly FenceLayout fenceLayout = new FenceLayout();
        private FenceLayoutSnapshot currentLayout;
        private FenceRenderer fenceRenderer;
        private Fader opacityFader;

        public void RefreshBrushes()
        {
            EnsureRenderer();
            fenceRenderer.UpdateAppearance(
                logicalTitleHeight,
                headerColor,
                headerAlpha,
                windowColor,
                windowAlpha);
            Opacity = overallOpacity;
        }

        private FenceLayoutSnapshot GetLayoutSnapshot()
        {
            currentLayout = fenceLayout.CreateSnapshot(
                fenceInfo.Files,
                fenceInfo.SortMode,
                fenceInfo.SortDescending,
                Width,
                Height,
                titleHeight,
                scrollOffset);
            if (scrollOffset > currentLayout.ScrollHeight)
            {
                scrollOffset = currentLayout.ScrollHeight;
                fenceLayout.Invalidate();
                currentLayout = fenceLayout.CreateSnapshot(
                    fenceInfo.Files,
                    fenceInfo.SortMode,
                    fenceInfo.SortDescending,
                    Width,
                    Height,
                    titleHeight,
                    scrollOffset);
            }
            scrollHeight = currentLayout.ScrollHeight;
            totalHeight = currentLayout.TotalHeight;
            return currentLayout;
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            FenceLayoutSnapshot layout = GetLayoutSnapshot();
            hoveringItem = CanInteractWithContent() && draggedItem == null
                ? layout.HitTest(PointToClient(MousePosition))
                : null;
            EnsureRenderer();
            fenceRenderer.Render(
                e.Graphics,
                ClientRectangle,
                Text,
                titleHeight,
                layout,
                dragDropController.SelectedPaths,
                hoveringItem,
                IsMinified,
                isAnimating);
        }

        private void InvalidateFenceContent()
        {
            fenceLayout.Invalidate();
            fenceRenderer?.InvalidateEntries();
            Invalidate();
        }

        public void SetOverallOpacity(double opacity)
        {
            if (Properties.Settings.Default.reduceAnimations)
            {
                Opacity = opacity;
                return;
            }

            if (opacityFader == null)
                opacityFader = new Fader(0.22F);
            opacityFader.StartFade(Opacity, opacity, value => Opacity = value);
        }

        private void EnsureRenderer()
        {
            if (fenceRenderer == null)
                fenceRenderer = new FenceRenderer(thumbnailProvider);
        }

        private void DisposeDrawingResources()
        {
            opacityFader?.Dispose();
            opacityFader = null;
            fenceRenderer?.Dispose();
            fenceRenderer = null;
        }
    }
}
