using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private const int UndoHistoryCapacity = 20;

        private readonly List<FenceUndoState> undoHistory = new List<FenceUndoState>(UndoHistoryCapacity);
        private ToolStripMenuItem undoMenuItem;
        private ToolStripMenuItem undoMenuItemDark;

        private enum FenceUndoKind
        {
            Reorder,
            Remove,
            Sync
        }

        private sealed class FenceUndoState
        {
            public FenceUndoState(
                FenceUndoKind kind,
                string description,
                string[] paths,
                int originalIndex = -1,
                string[] addedPaths = null,
                int[] originalIndices = null)
            {
                Kind = kind;
                Description = description;
                Paths = paths;
                OriginalIndex = originalIndex;
                AddedPaths = addedPaths;
                OriginalIndices = originalIndices;
            }

            public FenceUndoKind Kind { get; }
            public string Description { get; }
            public string[] Paths { get; }
            public int OriginalIndex { get; }
            public string[] AddedPaths { get; }
            public int[] OriginalIndices { get; }
        }

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

            menu.Items.Insert(0, openFolderItem);
            menu.Items.Insert(1, syncItem);
            menu.Items.Insert(2, undoItem);
            menu.Items.Insert(3, new ToolStripSeparator());
        }

        private void RecordReorderUndo(string path, int originalIndex)
        {
            AddUndoState(new FenceUndoState(FenceUndoKind.Reorder, "reorder", new[] { path }, originalIndex));
        }

        private void RecordRemoveUndo(string path, int originalIndex)
        {
            AddUndoState(new FenceUndoState(FenceUndoKind.Remove, "remove item", new[] { path }, originalIndex));
        }

        private void RecordSyncUndo(List<string> addedPaths, List<string> removedPaths, List<int> removedIndices)
        {
            AddUndoState(new FenceUndoState(
                FenceUndoKind.Sync,
                "sync",
                removedPaths.ToArray(),
                addedPaths: addedPaths.ToArray(),
                originalIndices: removedIndices.ToArray()));
        }

        private void AddUndoState(FenceUndoState state)
        {
            if (undoHistory.Count == UndoHistoryCapacity)
                undoHistory.RemoveAt(0);

            undoHistory.Add(state);
            UpdateUndoCommands();
        }

        private void UndoLastFenceChange()
        {
            if (undoHistory.Count == 0)
                return;

            int lastIndex = undoHistory.Count - 1;
            FenceUndoState state = undoHistory[lastIndex];
            undoHistory.RemoveAt(lastIndex);

            ApplyUndo(state);
            selectedItem = null;
            hoveringItem = null;
            Save();
            Invalidate();
            UpdateUndoCommands();
        }

        private void ApplyUndo(FenceUndoState state)
        {
            switch (state.Kind)
            {
                case FenceUndoKind.Reorder:
                    int currentIndex = FindFenceItemIndex(state.Paths[0]);
                    if (currentIndex < 0)
                        return;

                    string reorderedPath = fenceInfo.Files[currentIndex];
                    fenceInfo.Files.RemoveAt(currentIndex);
                    fenceInfo.Files.Insert(
                        Math.Max(0, Math.Min(state.OriginalIndex, fenceInfo.Files.Count)),
                        reorderedPath);
                    break;

                case FenceUndoKind.Remove:
                    if (FindFenceItemIndex(state.Paths[0]) >= 0)
                        return;

                    fenceInfo.Files.Insert(
                        Math.Max(0, Math.Min(state.OriginalIndex, fenceInfo.Files.Count)),
                        state.Paths[0]);
                    break;

                case FenceUndoKind.Sync:
                    for (int i = 0; i < state.AddedPaths.Length; i++)
                    {
                        string addedPath = state.AddedPaths[i];
                        fenceInfo.Files.RemoveAll(path =>
                            string.Equals(path, addedPath, StringComparison.OrdinalIgnoreCase));
                    }

                    for (int i = 0; i < state.Paths.Length; i++)
                    {
                        if (FindFenceItemIndex(state.Paths[i]) >= 0)
                            continue;

                        int insertionIndex = Math.Max(
                            0,
                            Math.Min(state.OriginalIndices[i], fenceInfo.Files.Count));
                        fenceInfo.Files.Insert(insertionIndex, state.Paths[i]);
                    }
                    break;
            }
        }

        private int FindFenceItemIndex(string path)
        {
            return fenceInfo.Files.FindIndex(item =>
                string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateUndoCommands()
        {
            FenceUndoState state = undoHistory.Count > 0 ? undoHistory[undoHistory.Count - 1] : null;
            string text = state == null ? "Undo" : "Undo " + state.Description;

            SetUndoCommandState(undoMenuItem, state != null, text);
            SetUndoCommandState(undoMenuItemDark, state != null, text);
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
            try
            {
                FenceFolderSyncResult result = SynchronizeFenceFolder(recordUndo: true);

                if (!result.Changed)
                {
                    MessageBox.Show(
                        "The fence is already synchronized with its folder.",
                        "Sync",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                MessageBox.Show(
                    $"Sync complete. Added {result.AddedPaths.Count} item(s) and removed {result.RemovedPaths.Count} stale item(s).",
                    "Sync",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to synchronize the fence folder:\n" + ex.Message,
                    "Sync",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private FenceFolderSyncResult SynchronizeFenceFolder(bool recordUndo)
        {
            FenceFolderSyncResult result = FenceFolderSynchronizer.Synchronize(
                fenceInfo.Files,
                fenceFolderPath);
            if (!result.Changed)
                return result;

            if (recordUndo)
                RecordSyncUndo(result.AddedPaths, result.RemovedPaths, result.RemovedIndices);

            selectedItem = null;
            hoveringItem = null;
            Save();
            Invalidate();
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
