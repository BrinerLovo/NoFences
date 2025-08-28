using NoFences.Model;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private double overallOpacity = 0.5f;
        private bool snapping = false;
        private int snapSize = 30;
        private Color headerColor = Color.Black;
        private Color windowColor = Color.Black;
        private int headerAlpha = 50;
        private int windowAlpha = 50;

        private void LoadSettings()
        {
            snapping = Properties.Settings.Default.snapping;
            snapSize = Properties.Settings.Default.snapSize;
            headerColor = Properties.Settings.Default.headerColor;
            headerAlpha = Properties.Settings.Default.headerAlpha;
            windowColor = Properties.Settings.Default.windowColor;
            windowAlpha = Properties.Settings.Default.opacity;
            overallOpacity = Properties.Settings.Default.overallOpacity;
        }

        /// <summary>
        /// this is called when an setting in the settings window is changed
        /// </summary>
        private void OnSettingsChanged()
        {
            var allFences = FenceManager.Instance.Fences;
            foreach (var fence in allFences)
            {
                fence.RefreshSettings();
            }
        }

        public void RefreshSettings()
        {
            logicalTitleHeight = Properties.Settings.Default.title_size;
            fenceInfo.TitleHeight = Properties.Settings.Default.title_size;
            fenceInfo.CanMinify = Properties.Settings.Default.autoMinify;
            LoadSettings();
            RefreshBrushes();

            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            ReloadFonts();
            Minify();
            if (IsMinified)
            {
                Height = titleHeight;
            }

            // Reinitialize file watchers in case watched extensions changed
            ReinitializeFileWatchers();

            Refresh();
        }

        /// <summary>
        /// Reinitializes the file watchers with current watched extensions settings
        /// </summary>
        public void ReinitializeFileWatchers()
        {
            // Dispose existing watchers
            fenceWatcher?.Dispose();
            desktopWatcher?.Dispose();
            fenceWatcher = null;
            desktopWatcher = null;

            // Reinitialize with current settings
            InitFileWatchers();
        }

        private void settingsMenuItem_Click(object sender, EventArgs e)
        {
            using (SettingsWindow settings = new SettingsWindow())
            {
                settings.OnSettingsChanged += OnSettingsChanged;
                settings.ShowDialog(); // Opens the settings as a modal popup
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new EditDialog(Text);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Text = dialog.NewName;
                fenceInfo.Name = Text;
                Refresh();
                Save();
            }
        }

        private void newFenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("New fence");
        }

        private void lockedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.Locked = lockedToolStripMenuItem.Checked;
            Save();
        }

        private void minifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.CanMinify = minifyToolStripMenuItem.Checked;
            Save();
        }

        private void watchedExtensionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new WatchedExtensionsDialog(fenceInfo.WatchedExtensions))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    fenceInfo.WatchedExtensions = dialog.WatchedExtensions;
                    Save();
                    ReinitializeFileWatchers();
                }
            }
        }

        private void customFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select custom folder path for this fence";
                folderDialog.ShowNewFolderButton = true;
                
                // Set current path if it exists
                if (!string.IsNullOrEmpty(fenceInfo.CustomFolderPath) && System.IO.Directory.Exists(fenceInfo.CustomFolderPath))
                {
                    folderDialog.SelectedPath = fenceInfo.CustomFolderPath;
                }
                
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                {
                    string oldPath = fenceFolderPath;
                    fenceInfo.CustomFolderPath = folderDialog.SelectedPath;
                    
                    // Ask user if they want to move existing files to the new location
                    if (System.IO.Directory.Exists(oldPath) && System.IO.Directory.GetFiles(oldPath).Length > 0)
                    {
                        var result = MessageBox.Show(
                            "Would you like to move existing files from the old location to the new folder?",
                            "Move Files",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                            
                        if (result == DialogResult.Yes)
                        {
                            MoveFilesToNewLocation(oldPath, fenceFolderPath);
                        }
                    }
                    
                    Save();
                    ReinitializeFileWatchers();
                    Refresh();
                }
            }
        }

        private void clearCustomFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(fenceInfo.CustomFolderPath))
            {
                var result = MessageBox.Show(
                    "This will reset the fence to use the default folder location. Continue?",
                    "Reset Folder Path",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    string oldPath = fenceFolderPath;
                    fenceInfo.CustomFolderPath = null;
                    
                    // Ask if they want to move files to the default location
                    if (System.IO.Directory.Exists(oldPath) && System.IO.Directory.GetFiles(oldPath).Length > 0)
                    {
                        var moveResult = MessageBox.Show(
                            "Would you like to move existing files to the default location?",
                            "Move Files",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                            
                        if (moveResult == DialogResult.Yes)
                        {
                            MoveFilesToNewLocation(oldPath, fenceFolderPath);
                        }
                    }
                    
                    Save();
                    ReinitializeFileWatchers();
                    Refresh();
                }
            }
        }

        private void MoveFilesToNewLocation(string sourcePath, string destinationPath)
        {
            try
            {
                // Ensure destination directory exists
                if (!System.IO.Directory.Exists(destinationPath))
                {
                    System.IO.Directory.CreateDirectory(destinationPath);
                }

                // Move all files from source to destination
                foreach (string file in System.IO.Directory.GetFiles(sourcePath))
                {
                    string fileName = System.IO.Path.GetFileName(file);
                    string destFile = System.IO.Path.Combine(destinationPath, fileName);
                    
                    // Handle duplicates
                    int counter = 1;
                    string originalDestFile = destFile;
                    while (System.IO.File.Exists(destFile))
                    {
                        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(originalDestFile);
                        string ext = System.IO.Path.GetExtension(originalDestFile);
                        destFile = System.IO.Path.Combine(destinationPath, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }
                    
                    System.IO.File.Move(file, destFile);
                    
                    // Update the file path in our fence list
                    int index = fenceInfo.Files.IndexOf(file);
                    if (index >= 0)
                    {
                        fenceInfo.Files[index] = destFile;
                    }
                }
                
                Console.WriteLine($"Successfully moved files from {sourcePath} to {destinationPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ...existing code...
    }
}
