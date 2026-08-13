using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;

namespace NoFences
{
    internal sealed class FenceSettingsWindow : Form
    {
        private static readonly Color WindowBackground = Color.FromArgb(31, 31, 31);
        private static readonly Color SurfaceBackground = Color.FromArgb(42, 42, 42);
        private static readonly Color BorderColor = Color.FromArgb(74, 74, 74);
        private static readonly Color PrimaryText = Color.FromArgb(242, 242, 242);
        private static readonly Color SecondaryText = Color.FromArgb(175, 175, 175);

        private readonly TextBox nameInput;
        private readonly TextBox folderInput;
        private readonly TextBox extensionsInput;
        private readonly CheckBox moveContentsCheckBox;
        private readonly CheckBox lockedCheckBox;
        private readonly CheckBox autoSyncCheckBox;
        private readonly CheckBox inheritMinifyCheckBox;
        private readonly CheckBox autoMinifyCheckBox;
        private readonly CheckBox inheritTitleCheckBox;
        private readonly NumericUpDown titleHeightInput;
        private readonly ComboBox sortModeInput;
        private readonly CheckBox sortDescendingCheckBox;

        public FenceSettingsWindow(FenceInfo fenceInfo, string effectiveFolderPath)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            Text = "Fence settings — " + fenceInfo.Name;
            BackColor = WindowBackground;
            ForeColor = PrimaryText;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(650, 650);

            var header = new Panel { Dock = DockStyle.Top, Height = 82, Padding = new Padding(24, 18, 24, 8) };
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 17F),
                Height = 34,
                Text = "Fence settings"
            });
            header.Controls.Add(new Label
            {
                Dock = DockStyle.Bottom,
                ForeColor = SecondaryText,
                Height = 25,
                Text = "Configure this fence without changing global defaults."
            });

            var content = new FlowLayoutPanel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(22, 4, 16, 18),
                WrapContents = false
            };

            nameInput = CreateTextBox(fenceInfo.Name);
            folderInput = CreateTextBox(effectiveFolderPath);
            Button browseButton = CreateButton("Browse…", 88);
            browseButton.Click += BrowseButton_Click;
            moveContentsCheckBox = CreateCheckBox(
                "Move current fence items when the linked folder changes",
                true);
            extensionsInput = CreateTextBox(string.Join(", ", fenceInfo.WatchedExtensions ?? new List<string>()));
            lockedCheckBox = CreateCheckBox("Lock position and size", fenceInfo.Locked);
            autoSyncCheckBox = CreateCheckBox("Automatically sync changes from the linked folder", fenceInfo.AutoSyncFolder);
            sortModeInput = new ComboBox
            {
                BackColor = Color.FromArgb(36, 36, 36),
                DrawMode = DrawMode.OwnerDrawFixed,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                ForeColor = PrimaryText,
                Height = 30,
                ItemHeight = 24
            };
            sortModeInput.Items.AddRange(new object[]
            {
                "Custom order",
                "Name",
                "Type",
                "Date modified"
            });
            sortModeInput.SelectedIndex = (int)fenceInfo.SortMode;
            sortModeInput.DrawItem += SortModeInput_DrawItem;
            sortDescendingCheckBox = CreateCheckBox("Reverse sort order", fenceInfo.SortDescending);
            inheritMinifyCheckBox = CreateCheckBox("Use global auto-minify setting", fenceInfo.UseGlobalAutoMinify);
            autoMinifyCheckBox = CreateCheckBox("Auto-minify this fence", fenceInfo.CanMinify);
            inheritTitleCheckBox = CreateCheckBox("Use global title height", fenceInfo.UseGlobalTitleHeight);
            titleHeightInput = new NumericUpDown
            {
                BackColor = SurfaceBackground,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = PrimaryText,
                Minimum = SettingsValidator.MinimumTitleHeight,
                Maximum = SettingsValidator.MaximumTitleHeight,
                Value = Math.Max(
                    SettingsValidator.MinimumTitleHeight,
                    Math.Min(SettingsValidator.MaximumTitleHeight, fenceInfo.TitleHeight)),
                Width = 90
            };
            inheritMinifyCheckBox.CheckedChanged += (sender, args) => autoMinifyCheckBox.Enabled = !inheritMinifyCheckBox.Checked;
            inheritTitleCheckBox.CheckedChanged += (sender, args) => titleHeightInput.Enabled = !inheritTitleCheckBox.Checked;
            sortModeInput.SelectedIndexChanged += (sender, args) =>
                sortDescendingCheckBox.Enabled = sortModeInput.SelectedIndex != (int)FenceSortMode.Custom;
            autoMinifyCheckBox.Enabled = !inheritMinifyCheckBox.Checked;
            titleHeightInput.Enabled = !inheritTitleCheckBox.Checked;
            sortDescendingCheckBox.Enabled = fenceInfo.SortMode != FenceSortMode.Custom;

            content.Controls.Add(CreateSection(
                "Identity",
                "Give the fence a clear desktop label.",
                CreateLabeledRow("Name", nameInput)));
            content.Controls.Add(CreateSection(
                "Folder and synchronization",
                "Items dropped into this fence are physically stored in its linked folder.",
                CreateFolderRow(folderInput, browseButton),
                moveContentsCheckBox,
                autoSyncCheckBox,
                CreateLabeledRow("Watched extensions", extensionsInput),
                CreateHint("Comma-separated extensions such as .png, .pdf, .zip. Leave empty to disable desktop imports.")));
            content.Controls.Add(CreateSection(
                "Behavior",
                "Per-fence interaction overrides.",
                lockedCheckBox,
                inheritMinifyCheckBox,
                autoMinifyCheckBox,
                CreateLabeledRow("Item order", sortModeInput),
                sortDescendingCheckBox,
                CreateHint("Manual drag-and-drop reordering is available only with Custom order.")));
            content.Controls.Add(CreateSection(
                "Appearance",
                "Keep the global title size or use a local value.",
                inheritTitleCheckBox,
                CreateLabeledRow("Title height", titleHeightInput)));

            var footer = new Panel
            {
                BackColor = Color.FromArgb(36, 36, 36),
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(20, 14, 20, 14)
            };
            Button cancelButton = CreateButton("Cancel", 92);
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Dock = DockStyle.Right;
            Button applyButton = CreateButton("Apply", 92);
            applyButton.BackColor = Color.FromArgb(62, 92, 128);
            applyButton.DialogResult = DialogResult.OK;
            applyButton.Dock = DockStyle.Right;
            applyButton.Margin = new Padding(0, 0, 10, 0);
            applyButton.Click += ApplyButton_Click;
            footer.Controls.Add(cancelButton);
            footer.Controls.Add(applyButton);

            AcceptButton = applyButton;
            CancelButton = cancelButton;
            Controls.Add(content);
            Controls.Add(header);
            Controls.Add(footer);
        }

        public string FenceName => nameInput.Text.Trim();
        public string FolderPath => folderInput.Text.Trim();
        public bool MoveContents => moveContentsCheckBox.Checked;
        public bool Locked => lockedCheckBox.Checked;
        public bool AutoSyncFolder => autoSyncCheckBox.Checked;
        public bool UseGlobalAutoMinify => inheritMinifyCheckBox.Checked;
        public bool AutoMinify => autoMinifyCheckBox.Checked;
        public bool UseGlobalTitleHeight => inheritTitleCheckBox.Checked;
        public int TitleHeight => (int)titleHeightInput.Value;
        public FenceSortMode SortMode => (FenceSortMode)Math.Max(0, sortModeInput.SelectedIndex);
        public bool SortDescending => sortDescendingCheckBox.Checked;
        public List<string> WatchedExtensions => SettingsValidator.NormalizeExtensions(
            extensionsInput.Text.Split(
                new[] { ',', ';', ' ', '\r', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries));

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FenceName))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Enter a fence name.", "Fence settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nameInput.Focus();
                return;
            }

            try
            {
                string folder = System.IO.Path.GetFullPath(FolderPath);
                if (string.IsNullOrWhiteSpace(folder))
                    throw new ArgumentException("Select a linked folder.");
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, ex.Message, "Fence folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                folderInput.Focus();
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Choose the folder linked to this fence",
                SelectedPath = System.IO.Directory.Exists(folderInput.Text) ? folderInput.Text : string.Empty,
                ShowNewFolderButton = true
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    folderInput.Text = dialog.SelectedPath;
            }
        }

        private static void SortModeInput_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || !(sender is ComboBox comboBox))
                return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var background = new SolidBrush(selected ? Color.FromArgb(65, 65, 65) : Color.FromArgb(36, 36, 36)))
                e.Graphics.FillRectangle(background, e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                comboBox.Items[e.Index].ToString(),
                comboBox.Font,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                PrimaryText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static Panel CreateSection(string title, string description, params WinFormsControl[] controls)
        {
            int height = 76 + controls.Sum(control => control.Height + 8);
            var panel = new Panel
            {
                BackColor = SurfaceBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16),
                Size = new Size(585, height)
            };
            panel.Controls.Add(new Label
            {
                Font = new Font("Segoe UI Semibold", 10F),
                Location = new Point(16, 14),
                Size = new Size(545, 22),
                Text = title
            });
            panel.Controls.Add(new Label
            {
                ForeColor = SecondaryText,
                Location = new Point(16, 37),
                Size = new Size(545, 28),
                Text = description
            });
            int top = 68;
            foreach (WinFormsControl control in controls)
            {
                control.Location = new Point(16, top);
                control.Width = 545;
                panel.Controls.Add(control);
                top += control.Height + 8;
            }
            return panel;
        }

        private static TextBox CreateTextBox(string value)
        {
            return new TextBox
            {
                BackColor = Color.FromArgb(36, 36, 36),
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = PrimaryText,
                Height = 28,
                Text = value
            };
        }

        private static CheckBox CreateCheckBox(string text, bool value)
        {
            return new CheckBox
            {
                Checked = value,
                FlatStyle = FlatStyle.Flat,
                ForeColor = PrimaryText,
                Height = 28,
                Text = text
            };
        }

        private static Button CreateButton(string text, int width)
        {
            var button = new Button
            {
                BackColor = SurfaceBackground,
                FlatStyle = FlatStyle.Flat,
                ForeColor = PrimaryText,
                Height = 34,
                Text = text,
                Width = width
            };
            button.FlatAppearance.BorderColor = BorderColor;
            return button;
        }

        private static TableLayoutPanel CreateLabeledRow(string label, WinFormsControl control)
        {
            var row = new TableLayoutPanel { ColumnCount = 2, Height = 32, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            control.Dock = DockStyle.Fill;
            row.Controls.Add(control, 1, 0);
            return row;
        }

        private static TableLayoutPanel CreateFolderRow(WinFormsControl folder, WinFormsControl browse)
        {
            var row = new TableLayoutPanel { ColumnCount = 2, Height = 34, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            folder.Dock = DockStyle.Fill;
            browse.Dock = DockStyle.Fill;
            row.Controls.Add(folder, 0, 0);
            row.Controls.Add(browse, 1, 0);
            return row;
        }

        private static Label CreateHint(string text)
        {
            return new Label { ForeColor = SecondaryText, Height = 34, Text = text };
        }
    }
}
