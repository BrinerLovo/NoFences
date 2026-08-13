using NoFences.Layout;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NoFences
{
    public partial class FenceWindow
    {
        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (lockedToolStripMenuItem.Checked)
                return;

            if (dragDropController.IsOwnDrag(e.Data) && fenceInfo.SortMode != FenceSortMode.Custom)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            if (dragDropController.IsOwnDrag(e.Data))
            {
                dragDropController.InternalDropHandled = true;
                if (fenceInfo.SortMode != FenceSortMode.Custom)
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                e.Effect = DragDropEffects.Move;
                Point dropPoint = PointToClient(new Point(e.X, e.Y));
                FenceLayoutSnapshot layout = GetLayoutSnapshot();
                if (dragDropController.TryReorder(
                    fenceInfo.Files,
                    layout.OrderedPaths,
                    layout.GetInsertionIndex(dropPoint),
                    out string[] previousOrder))
                {
                    RecordUndo("reorder items", previousOrder);
                    Save();
                }

                draggedItem = null;
                isDragging = false;
                InvalidateFenceContent();
                return;
            }

            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped == null || dropped.Length == 0)
                return;

            var addedPaths = new List<string>(dropped.Length);
            var handledSourcePaths = new List<string>(dropped.Length);
            var moveErrors = new List<string>();
            int failedMoveCount = 0;
            foreach (string item in dropped)
            {
                if (fenceInfo.Files.Contains(item, StringComparer.OrdinalIgnoreCase) || !ItemExists(item))
                    continue;

                if (TryMoveItemToFenceFolder(item, out string itemPath, out string errorMessage))
                {
                    if (!fenceInfo.Files.Contains(itemPath, StringComparer.OrdinalIgnoreCase))
                    {
                        fenceInfo.Files.Add(itemPath);
                        addedPaths.Add(itemPath);
                        handledSourcePaths.Add(item);
                    }
                    else
                    {
                        handledSourcePaths.Add(item);
                    }
                }
                else
                {
                    failedMoveCount++;
                    moveErrors.Add(Path.GetFileName(item) + ": " + (string.IsNullOrWhiteSpace(errorMessage)
                        ? "Windows rejected the move."
                        : errorMessage));
                }
            }

            if (handledSourcePaths.Count == 0)
            {
                e.Effect = DragDropEffects.None;
                if (failedMoveCount > 0)
                {
                    MessageBox.Show(
                        "The item could not be moved into the fence folder.\n\n" + moveErrors[0],
                        "Move to fence",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return;
            }

            e.Effect = DragDropEffects.Move;
            dragDropController.MarkHandledByFence(e.Data, handledSourcePaths);
            if (addedPaths.Count > 0)
            {
                Save();
                BeginInvoke((Action)InvalidateFenceContent);
            }

            if (failedMoveCount > 0)
            {
                MessageBox.Show(
                    $"Moved {handledSourcePaths.Count} item(s), but {failedMoveCount} item(s) could not be moved.\n\n"
                    + string.Join("\n", moveErrors.Take(3)),
                    "Move to fence",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void FenceWindow_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (e.Action != DragAction.Cancel)
                return;

            Cursor = Cursors.Default;
            draggedItem = null;
            dragDropController.InternalDropHandled = false;
            Invalidate();
        }

        private void FenceWindow_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            Cursor = Cursors.Default;
            e.UseDefaultCursors = true;
        }
    }
}
