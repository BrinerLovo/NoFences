using NoFences.Model;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NoFences.Util
{
    internal static class FencePlacement
    {
        public static void EnsureVisible(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            var bounds = new Rectangle(
                fenceInfo.PosX,
                fenceInfo.PosY,
                Math.Max(SettingsValidator.MinimumFenceWidth, fenceInfo.Width),
                Math.Max(SettingsValidator.MinimumFenceHeight, fenceInfo.Height));
            bool visible = Screen.AllScreens.Any(screen =>
            {
                Rectangle intersection = Rectangle.Intersect(bounds, screen.WorkingArea);
                return intersection.Width >= 48 && intersection.Height >= 32;
            });
            if (visible)
                return;

            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea
                ?? new Rectangle(0, 0, 1920, 1080);
            fenceInfo.PosX = workingArea.Left + Math.Min(48, Math.Max(0, workingArea.Width - bounds.Width));
            fenceInfo.PosY = workingArea.Top + Math.Min(48, Math.Max(0, workingArea.Height - bounds.Height));
        }
    }
}
