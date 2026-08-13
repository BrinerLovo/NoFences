using NoFences.History;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private readonly FenceUndoManager undoManager = new FenceUndoManager();
        private ToolStripMenuItem undoMenuItem;
        private ToolStripMenuItem undoMenuItemDark;

        private void InitializeFenceCommands()
        {
            AddFenceCommands(appContextMenu, out undoMenuItem);
            AddFenceCommands(appContextMenuDark, out undoMenuItemDark);
            appContextMenuDark.Opening += contextMenuStrip1_Opening;
            UpdateUndoCommands();
        }

        private void AddFenceCommands(ContextMenuStrip menu, out ToolStripMenuItem undoItem)
        {
            var openFolderItem = new ToolStripMenuItem("Open folder");
            openFolderItem.Click += openFolderToolStripMenuItem_Click;

            var syncItem = new ToolStripMenuItem("Sync")
            {
                ToolTipText = "Synchronize displayed icons with the linked fence folder"
            };
            syncItem.Click += syncToolStripMenuItem_Click;

            undoItem = new ToolStripMenuItem("Undo")
            {
                ShortcutKeyDisplayString = "Ctrl+Z"
            };
            undoItem.Click += undoToolStripMenuItem_Click;

            var fenceSettingsItem = new ToolStripMenuItem("Fence settings…");
            fenceSettingsItem.Click += fenceSettingsToolStripMenuItem_Click;

            menu.Items.Insert(0, openFolderItem);
            menu.Items.Insert(1, syncItem);
            menu.Items.Insert(2, undoItem);
            menu.Items.Insert(3, fenceSettingsItem);
            menu.Items.Insert(4, new ToolStripSeparator());
        }

        private void RecordUndo(string description, IReadOnlyList<string> pathsBeforeChange)
        {
            undoManager.Record(description, pathsBeforeChange);
            UpdateUndoCommands();
        }

        private void UndoLastFenceChange()
        {
            if (!undoManager.TryUndo(fenceInfo.Files))
                return;

            dragDropController.ClearSelection();
            InvalidateFenceContent();
            Save();
            UpdateUndoCommands();
        }

        private void UpdateUndoCommands()
        {
            string description = undoManager.NextDescription;
            string text = description == null ? "Undo" : "Undo " + description;
            SetUndoCommandState(undoMenuItem, undoManager.CanUndo, text);
            SetUndoCommandState(undoMenuItemDark, undoManager.CanUndo, text);
        }

        private static void SetUndoCommandState(ToolStripMenuItem item, bool enabled, string text)
        {
            if (item == null)
                return;
            item.Enabled = enabled;
            item.Text = text;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoLastFenceChange();
        }

        private void syncToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncFromTray();
        }

        public void SyncFromTray(bool showResult = true)
        {
            try
            {
                FenceFolderSyncResult result = SynchronizeFenceFolder(recordUndo: true);
                if (!result.Changed)
                {
                    if (showResult) MessageBox.Show(
                        "The fence is already synchronized with its folder.",
                        "Sync",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (showResult) MessageBox.Show(
                    $"Sync complete. Added {result.AddedPaths.Count} item(s) and removed {result.RemovedPaths.Count} stale item(s).",
                    "Sync",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (showResult) MessageBox.Show(
                    "Unable to synchronize the fence folder:\n" + ex.Message,
                    "Sync",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private FenceFolderSyncResult SynchronizeFenceFolder(bool recordUndo)
        {
            string[] previousPaths = recordUndo ? fenceInfo.Files.ToArray() : null;
            FenceFolderSyncResult result = FenceFolderSynchronizer.Synchronize(
                fenceInfo.Files,
                fenceFolderPath);
            if (!result.Changed)
                return result;

            if (recordUndo)
                RecordUndo("sync", previousPaths);

            dragDropController.RetainExisting(fenceInfo.Files);
            InvalidateFenceContent();
            Save();
            return result;
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(fenceFolderPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = fenceFolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to open the fence folder:\n" + ex.Message,
                    "Open folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
