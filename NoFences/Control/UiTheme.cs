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
