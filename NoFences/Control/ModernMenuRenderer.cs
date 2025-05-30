using System.Drawing;
using System.Windows.Forms;

namespace NoFences.Control
{
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool isDarkMode = true; // Detect if system is in dark mode

        public ModernMenuRenderer() : base(new ModernColorTable(true))
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = isDarkMode ? Color.White : Color.Black; // Dark mode text color
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen borderPen = new Pen(isDarkMode ? Color.FromArgb(50, 50, 50) : Color.LightGray))
            {
                e.Graphics.DrawRectangle(borderPen, new Rectangle(Point.Empty, e.ToolStrip.Size - new Size(1, 1)));
            }
        }

    }

    public class ModernColorTable : ProfessionalColorTable
    {
        private readonly bool isDarkMode;

        public ModernColorTable(bool darkMode)
        {
            isDarkMode = darkMode;
        }

        public override Color MenuBorder => isDarkMode ? Color.FromArgb(50, 50, 50) : Color.LightGray;
        public override Color MenuItemBorder => isDarkMode ? Color.Gray : Color.DarkGray;
        public override Color MenuItemSelected => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.LightGray;
        public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override Color ToolStripDropDownBackground => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.White; // Darker for true dark mode
        public override Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override Color ImageMarginGradientEnd => ToolStripDropDownBackground;

    }

}
