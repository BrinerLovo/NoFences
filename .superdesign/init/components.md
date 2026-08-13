# Shared UI components

NoFences is a .NET Framework 4.8 Windows Forms application. It uses custom WinForms primitives rather than a web component library.

## `NoFences/Control/UiTheme.cs` — UiTheme

Applies the shared neutral-charcoal palette and control styling recursively.

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.Control
{
    internal static class UiTheme
    {
        private static readonly Color WindowBackground = Color.FromArgb(31, 31, 31);
        private static readonly Color SurfaceBackground = Color.FromArgb(42, 42, 42);
        private static readonly Color HoverBackground = Color.FromArgb(54, 54, 54);
        private static readonly Color BorderColor = Color.FromArgb(74, 74, 74);
        private static readonly Color PrimaryText = Color.FromArgb(242, 242, 242);

        public static void Apply(Form form)
        {
            form.BackColor = WindowBackground;
            form.ForeColor = PrimaryText;
            ApplyToChildren(form.Controls);
        }

        private static void ApplyToChildren(System.Windows.Forms.Control.ControlCollection controls)
        {
            foreach (System.Windows.Forms.Control control in controls)
            {
                control.ForeColor = PrimaryText;

                if (control is Button button)
                {
                    button.BackColor = SurfaceBackground;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = BorderColor;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.MouseOverBackColor = HoverBackground;
                    button.FlatAppearance.MouseDownBackColor = BorderColor;
                    button.UseVisualStyleBackColor = false;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = SurfaceBackground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = SurfaceBackground;
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.BackColor = WindowBackground;
                    checkBox.FlatStyle = FlatStyle.Flat;
                }
                else if (!(control is TrackBar))
                {
                    control.BackColor = WindowBackground;
                }

                if (control.HasChildren)
                    ApplyToChildren(control.Controls);
            }
        }
    }
}
```

## `NoFences/Control/ModernMenuRenderer.cs` — ModernMenuRenderer

Renders context menus using a compact dark palette.

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.Control
{
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool isDarkMode = true;

        public ModernMenuRenderer() : base(new ModernColorTable(true)) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = isDarkMode ? Color.White : Color.Black;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen borderPen = new Pen(isDarkMode ? Color.FromArgb(50, 50, 50) : Color.LightGray))
                e.Graphics.DrawRectangle(borderPen, new Rectangle(Point.Empty, e.ToolStrip.Size - new Size(1, 1)));
        }
    }

    public class ModernColorTable : ProfessionalColorTable
    {
        private readonly bool isDarkMode;
        public ModernColorTable(bool darkMode) { isDarkMode = darkMode; }
        public override Color MenuBorder => isDarkMode ? Color.FromArgb(50, 50, 50) : Color.LightGray;
        public override Color MenuItemBorder => isDarkMode ? Color.Gray : Color.DarkGray;
        public override Color MenuItemSelected => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.LightGray;
        public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override Color ToolStripDropDownBackground => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.White;
        public override Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override Color ImageMarginGradientEnd => ToolStripDropDownBackground;
    }
}
```
