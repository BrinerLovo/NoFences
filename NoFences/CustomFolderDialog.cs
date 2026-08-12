using System;
using System.IO;
using System.Windows.Forms;

namespace NoFences
{
    public partial class CustomFolderDialog : Form
    {
        public CustomFolderDialog(string currentPath)
        {
            InitializeComponent();
            NoFences.Control.UiTheme.Apply(this);
            tbPath.Text = currentPath ?? "";
        }

        public string CustomFolderPath => tbPath.Text;

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Validate the path
            string path = tbPath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                DialogResult = DialogResult.OK;
                return;
            }

            try
            {
                // Try to create the directory if it doesn't exist
                if (!Directory.Exists(path))
                {
                    var result = MessageBox.Show(
                        $"The folder '{path}' does not exist. Do you want to create it?",
                        "Create Folder",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        return; // Don't close dialog
                    }
                }

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid folder path: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select custom folder path for this fence";
                folderDialog.ShowNewFolderButton = true;
                
                if (!string.IsNullOrEmpty(tbPath.Text) && Directory.Exists(tbPath.Text))
                {
                    folderDialog.SelectedPath = tbPath.Text;
                }
                
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                {
                    tbPath.Text = folderDialog.SelectedPath;
                }
            }
        }
    }
}
