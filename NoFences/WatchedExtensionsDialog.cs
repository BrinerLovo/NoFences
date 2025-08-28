using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NoFences
{
    public partial class WatchedExtensionsDialog : Form
    {
        public List<string> WatchedExtensions { get; private set; }

        public WatchedExtensionsDialog(List<string> currentExtensions)
        {
            InitializeComponent();
            WatchedExtensions = new List<string>(currentExtensions ?? new List<string>());
            LoadExtensions();
        }

        private void LoadExtensions()
        {
            listBoxExtensions.Items.Clear();
            foreach (var ext in WatchedExtensions)
            {
                listBoxExtensions.Items.Add(ext);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string extension = textBoxNewExtension.Text.Trim();
            
            // Validate extension format
            if (string.IsNullOrEmpty(extension))
            {
                MessageBox.Show("Please enter an extension.", "Invalid Extension", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure extension starts with a dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            // Check if extension already exists
            if (WatchedExtensions.Contains(extension))
            {
                MessageBox.Show("This extension is already in the list.", "Duplicate Extension", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            WatchedExtensions.Add(extension);
            listBoxExtensions.Items.Add(extension);
            textBoxNewExtension.Clear();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (listBoxExtensions.SelectedIndex >= 0)
            {
                string selectedExtension = listBoxExtensions.SelectedItem.ToString();
                WatchedExtensions.Remove(selectedExtension);
                listBoxExtensions.Items.RemoveAt(listBoxExtensions.SelectedIndex);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void textBoxNewExtension_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnAdd_Click(sender, e);
                e.Handled = true;
            }
        }

        private void listBoxExtensions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                btnRemove_Click(sender, e);
            }
        }
    }
}