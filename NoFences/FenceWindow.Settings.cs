using NoFences.Model;
using NoFences.Util;
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
            if (fenceInfo.UseGlobalTitleHeight)
                fenceInfo.TitleHeight = Properties.Settings.Default.title_size;
            if (fenceInfo.UseGlobalAutoMinify)
                fenceInfo.CanMinify = Properties.Settings.Default.autoMinify;

            logicalTitleHeight = fenceInfo.TitleHeight;
            LoadSettings();
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            RefreshBrushes();
            Minify();
            if (IsMinified)
            {
                Height = titleHeight;
            }

            // Reinitialize file watchers in case watched extensions changed
            ReinitializeFileWatchers();
            Save();

            Invalidate();
        }

        /// <summary>
        /// Reinitializes the file watchers with current watched extensions settings
        /// </summary>
        public void ReinitializeFileWatchers()
        {
            InitializeFileWatchersOptimized();
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
                Invalidate();
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
            ShowFenceSettings();
        }

        private void clearCustomFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fenceInfo.CustomFolderPath))
                return;

            DialogResult choice = MessageBox.Show(
                this,
                "Use the default NoFences folder for this fence?\n\nChoose Yes to move the current folder contents, No to change the link without moving files, or Cancel to keep the current folder.",
                "Use default folder",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel)
                return;

            string oldFolderPath = fenceFolderPath;
            string defaultFolderPath = FenceManager.Instance.GetDefaultContentFolderPath(fenceInfo.Id);
            FenceFolderMigrationResult migration = null;
            if (choice == DialogResult.Yes)
            {
                DisposeFileWatchersOptimized();
                if (!FenceFolderMigration.TryMoveContents(
                        oldFolderPath,
                        defaultFolderPath,
                        out migration,
                        out string errorMessage))
                {
                    ReinitializeFileWatchers();
                    MessageBox.Show(this, errorMessage, "Use default folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (migration != null)
            {
                for (int index = 0; index < fenceInfo.Files.Count; index++)
                {
                    if (migration.MovedPaths.TryGetValue(fenceInfo.Files[index], out string movedPath))
                        fenceInfo.Files[index] = movedPath;
                }
            }

            fenceInfo.CustomFolderPath = null;
            RefreshSettings();
        }

        private void fenceSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFenceSettings();
        }

        private void ShowFenceSettings()
        {
            string oldFolderPath = fenceFolderPath;
            using (var dialog = new FenceSettingsWindow(fenceInfo, oldFolderPath))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                ApplyFenceSettings(dialog, oldFolderPath);
            }
        }

        private void ApplyFenceSettings(FenceSettingsWindow dialog, string oldFolderPath)
        {
            string requestedFolderPath = PathUtil.NormalizeDirectoryPath(dialog.FolderPath);
            string defaultFolderPath = FenceManager.Instance.GetDefaultContentFolderPath(fenceInfo.Id);
            string newCustomFolderPath = PathUtil.IsSamePath(requestedFolderPath, defaultFolderPath)
                ? null
                : requestedFolderPath;
            bool folderChanged = !PathUtil.IsSamePath(oldFolderPath, requestedFolderPath);

            FenceFolderMigrationResult migration = null;
            if (folderChanged && dialog.MoveContents)
            {
                DisposeFileWatchersOptimized();
                if (!FenceFolderMigration.TryMoveContents(
                        oldFolderPath,
                        requestedFolderPath,
                        out migration,
                        out string errorMessage))
                {
                    ReinitializeFileWatchers();
                    MessageBox.Show(
                        this,
                        "The linked folder was not changed.\n\n" + errorMessage,
                        "Fence folder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            if (migration != null)
            {
                for (int index = 0; index < fenceInfo.Files.Count; index++)
                {
                    if (migration.MovedPaths.TryGetValue(fenceInfo.Files[index], out string movedPath))
                        fenceInfo.Files[index] = movedPath;
                }
            }

            fenceInfo.Name = dialog.FenceName;
            fenceInfo.CustomFolderPath = newCustomFolderPath;
            fenceInfo.Locked = dialog.Locked;
            fenceInfo.AutoSyncFolder = dialog.AutoSyncFolder;
            fenceInfo.UseGlobalAutoMinify = dialog.UseGlobalAutoMinify;
            fenceInfo.CanMinify = dialog.UseGlobalAutoMinify
                ? Properties.Settings.Default.autoMinify
                : dialog.AutoMinify;
            fenceInfo.UseGlobalTitleHeight = dialog.UseGlobalTitleHeight;
            fenceInfo.TitleHeight = dialog.UseGlobalTitleHeight
                ? Properties.Settings.Default.title_size
                : dialog.TitleHeight;
            fenceInfo.WatchedExtensions = dialog.WatchedExtensions;
            bool sortChanged = fenceInfo.SortMode != dialog.SortMode
                || fenceInfo.SortDescending != dialog.SortDescending;
            fenceInfo.SortMode = dialog.SortMode;
            fenceInfo.SortDescending = dialog.SortMode != FenceSortMode.Custom && dialog.SortDescending;

            if (sortChanged)
                dragDropController.ClearSelection();

            Text = fenceInfo.Name;
            lockedToolStripMenuItem.Checked = fenceInfo.Locked;
            lockedTick.Checked = fenceInfo.Locked;
            minifyToolStripMenuItem.Checked = fenceInfo.CanMinify;
            RefreshSettings();
            InvalidateFenceContent();
        }
    }
}
