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

            Refresh();
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

        // We do not use this anymore, we use a global setting for this.
        private void titleSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new HeightDialog(fenceInfo.TitleHeight);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                fenceInfo.TitleHeight = dialog.TitleHeight;
                logicalTitleHeight = dialog.TitleHeight;
                titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
                ReloadFonts();
                Minify();
                if (IsMinified)
                {
                    Height = titleHeight;
                }
                Refresh();
                Save();
            }
        }

        private void minifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsMinified)
            {
                Height = fenceInfo.Height;
                state = FenceState.Normal;
            }
            fenceInfo.CanMinify = minifyToolStripMenuItem.Checked;
            Save();

        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedItem();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            deleteItemToolStripMenuItem.Visible = hoveringItem != null;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Really remove this fence?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                FenceManager.Instance.RemoveFence(fenceInfo, this);
                Close();
            }
        }

        private void closeAppMenuItem_Click(object sender, EventArgs e)
        {
            // close the application
            Application.Exit();
        }

        private bool IsNearLeftEdge(Point location)
        {
            int edgeThreshold = 5; // Pixels from the edge to start resizing
            return location.X <= edgeThreshold;
        }

        private bool IsNearRightEdge(Point location)
        {
            int edgeThreshold = 5;
            return location.X >= this.ClientSize.Width - edgeThreshold;
        }

        private bool IsNearBottomEdge(Point location)
        {
            int edgeThreshold = 5;
            return location.Y >= this.ClientSize.Height - edgeThreshold;
        }
    }
}
