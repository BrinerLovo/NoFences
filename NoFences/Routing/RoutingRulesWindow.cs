using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;

namespace NoFences.Routing
{
    internal sealed class RoutingRulesWindow : Form
    {
        private readonly List<RoutingRule> rules;
        private readonly ListView listView;

        public RoutingRulesWindow()
        {
            Text = "Routing rules";
            BackColor = Color.FromArgb(31, 31, 31);
            ForeColor = Color.FromArgb(242, 242, 242);
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(780, 460);
            MinimizeBox = false;

            rules = RoutingRuleManager.Instance.Rules.Select(Clone).ToList();
            listView = new ListView
            {
                BackColor = Color.FromArgb(36, 36, 36),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                ForeColor = ForeColor,
                FullRowSelect = true,
                HideSelection = false,
                View = View.Details
            };
            listView.Columns.Add("Rule", 150);
            listView.Columns.Add("Source folder", 260);
            listView.Columns.Add("Extensions", 130);
            listView.Columns.Add("Destination", 150);
            listView.DoubleClick += (sender, args) => EditSelected();

            var header = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(20, 14, 20, 8) };
            header.Controls.Add(new Label { Dock = DockStyle.Top, Font = new Font("Segoe UI Semibold", 16F), Height = 30, Text = "Routing rules" });
            header.Controls.Add(new Label { Dock = DockStyle.Bottom, ForeColor = Color.FromArgb(175, 175, 175), Height = 24, Text = "Move newly created files from a source folder into a destination fence." });

            var footer = new FlowLayoutPanel
            {
                BackColor = Color.FromArgb(36, 36, 36),
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 62,
                Padding = new Padding(14)
            };
            Button save = CreateButton("Save");
            save.Click += Save_Click;
            Button cancel = CreateButton("Cancel");
            cancel.Click += (sender, args) => Close();
            Button remove = CreateButton("Remove");
            remove.Click += Remove_Click;
            Button edit = CreateButton("Edit");
            edit.Click += (sender, args) => EditSelected();
            Button add = CreateButton("Add rule");
            add.Click += Add_Click;
            footer.Controls.Add(save);
            footer.Controls.Add(cancel);
            footer.Controls.Add(remove);
            footer.Controls.Add(edit);
            footer.Controls.Add(add);

            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 12) };
            content.Controls.Add(listView);
            Controls.Add(content);
            Controls.Add(header);
            Controls.Add(footer);
            RefreshList();
        }

        private void Add_Click(object sender, EventArgs e)
        {
            using (var editor = new RoutingRuleEditorWindow(null))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    rules.Add(editor.Rule);
                    RefreshList();
                }
            }
        }

        private void EditSelected()
        {
            if (listView.SelectedIndices.Count == 0)
                return;
            int index = listView.SelectedIndices[0];
            using (var editor = new RoutingRuleEditorWindow(rules[index]))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    rules[index] = editor.Rule;
                    RefreshList();
                }
            }
        }

        private void Remove_Click(object sender, EventArgs e)
        {
            if (listView.SelectedIndices.Count == 0)
                return;
            rules.RemoveAt(listView.SelectedIndices[0]);
            RefreshList();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            RoutingRuleManager.Instance.ReplaceRules(rules);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void RefreshList()
        {
            listView.BeginUpdate();
            listView.Items.Clear();
            foreach (RoutingRule rule in rules)
            {
                FenceWindow destination = FenceManager.Instance.FindFence(rule.DestinationFenceId);
                var item = new ListViewItem(rule.Enabled ? rule.Name : rule.Name + " (disabled)");
                item.SubItems.Add(rule.SourceFolder);
                item.SubItems.Add(string.Join(", ", rule.Extensions));
                item.SubItems.Add(destination?.FenceName ?? "Missing fence");
                listView.Items.Add(item);
            }
            listView.EndUpdate();
        }

        private static Button CreateButton(string text)
        {
            return new Button { BackColor = Color.FromArgb(48, 48, 48), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Height = 32, Text = text, Width = 92 };
        }

        private static RoutingRule Clone(RoutingRule rule)
        {
            return new RoutingRule
            {
                Id = rule.Id,
                Name = rule.Name,
                SourceFolder = rule.SourceFolder,
                Extensions = new List<string>(rule.Extensions ?? new List<string>()),
                DestinationFenceId = rule.DestinationFenceId,
                Enabled = rule.Enabled
            };
        }
    }

    internal sealed class RoutingRuleEditorWindow : Form
    {
        private readonly TextBox nameInput;
        private readonly TextBox sourceInput;
        private readonly TextBox extensionsInput;
        private readonly ComboBox destinationInput;
        private readonly CheckBox enabledInput;
        private readonly FenceWindow[] fences;
        private readonly Guid ruleId;

        public RoutingRuleEditorWindow(RoutingRule rule)
        {
            ruleId = rule?.Id ?? Guid.NewGuid();
            fences = FenceManager.Instance.Fences.OrderBy(fence => fence.FenceName).ToArray();
            Text = rule == null ? "Add routing rule" : "Edit routing rule";
            BackColor = Color.FromArgb(31, 31, 31);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 310);
            MaximizeBox = false;
            MinimizeBox = false;

            nameInput = CreateTextBox(rule?.Name ?? "New rule");
            sourceInput = CreateTextBox(rule?.SourceFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            extensionsInput = CreateTextBox(string.Join(", ", rule?.Extensions ?? new List<string> { ".pdf" }));
            destinationInput = new ComboBox { BackColor = Color.FromArgb(36, 36, 36), DropDownStyle = ComboBoxStyle.DropDownList, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            destinationInput.Items.AddRange(fences.Select(fence => fence.FenceName).Cast<object>().ToArray());
            int selectedFence = Array.FindIndex(fences, fence => fence.FenceId == rule?.DestinationFenceId);
            destinationInput.SelectedIndex = selectedFence >= 0 ? selectedFence : (fences.Length > 0 ? 0 : -1);
            enabledInput = new CheckBox { Checked = rule?.Enabled ?? true, FlatStyle = FlatStyle.Flat, Text = "Enabled", Height = 28 };

            Button browse = new Button { Text = "Browse...", Width = 90, Dock = DockStyle.Right };
            browse.Click += Browse_Click;
            var sourcePanel = new Panel { Height = 28 };
            sourceInput.Dock = DockStyle.Fill;
            sourcePanel.Controls.Add(sourceInput);
            sourcePanel.Controls.Add(browse);

            var layout = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Top, Height = 225, Padding = new Padding(18), RowCount = 5 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddRow(layout, 0, "Rule name", nameInput);
            AddRow(layout, 1, "Source folder", sourcePanel);
            AddRow(layout, 2, "Extensions", extensionsInput);
            AddRow(layout, 3, "Destination fence", destinationInput);
            AddRow(layout, 4, string.Empty, enabledInput);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 56, Padding = new Padding(12) };
            var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
            save.Click += Save_Click;
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
            footer.Controls.Add(save);
            footer.Controls.Add(cancel);
            Controls.Add(layout);
            Controls.Add(footer);
            AcceptButton = save;
            CancelButton = cancel;
        }

        public RoutingRule Rule { get; private set; }

        private void Save_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameInput.Text) || !System.IO.Directory.Exists(sourceInput.Text) || destinationInput.SelectedIndex < 0)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Enter a name, choose an existing source folder, and select a destination fence.", "Routing rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            List<string> extensions = SettingsValidator.NormalizeExtensions(extensionsInput.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (extensions.Count == 0)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Enter at least one extension, such as .pdf.", "Routing rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Rule = new RoutingRule
            {
                Id = ruleId,
                Name = nameInput.Text.Trim(),
                SourceFolder = System.IO.Path.GetFullPath(sourceInput.Text),
                Extensions = extensions,
                DestinationFenceId = fences[destinationInput.SelectedIndex].FenceId,
                Enabled = enabledInput.Checked
            };
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = sourceInput.Text, Description = "Choose the folder to monitor" })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    sourceInput.Text = dialog.SelectedPath;
            }
        }

        private static TextBox CreateTextBox(string text) => new TextBox { BackColor = Color.FromArgb(36, 36, 36), BorderStyle = BorderStyle.FixedSingle, ForeColor = Color.White, Text = text };

        private static void AddRow(TableLayoutPanel layout, int row, string label, WinFormsControl control)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            control.Dock = DockStyle.Fill;
            layout.Controls.Add(control, 1, row);
        }
    }
}
