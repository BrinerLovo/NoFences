using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NoFences.Interaction
{
    internal sealed class FenceDragDropController
    {
        public const string InternalDragFormat = "NoFences.InternalItemsDrag";
        private const string HandledFencePathsFormat = "NoFences.HandledFencePaths";

        private readonly string sourceId = Guid.NewGuid().ToString("N");
        private readonly HashSet<string> selectedPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string selectionAnchor;

        public ISet<string> SelectedPaths => selectedPaths;
        public bool InternalDropHandled { get; set; }

        public bool IsSelected(string path)
        {
            return !string.IsNullOrEmpty(path) && selectedPaths.Contains(path);
        }

        public void Select(string path, IReadOnlyList<string> orderedPaths, bool toggle, bool range)
        {
            if (string.IsNullOrEmpty(path))
            {
                if (!toggle && !range)
                    ClearSelection();
                return;
            }

            if (range && !string.IsNullOrEmpty(selectionAnchor))
            {
                int anchorIndex = IndexOf(orderedPaths, selectionAnchor);
                int targetIndex = IndexOf(orderedPaths, path);
                if (anchorIndex >= 0 && targetIndex >= 0)
                {
                    if (!toggle)
                        selectedPaths.Clear();
                    int start = Math.Min(anchorIndex, targetIndex);
                    int end = Math.Max(anchorIndex, targetIndex);
                    for (int index = start; index <= end; index++)
                        selectedPaths.Add(orderedPaths[index]);
                    return;
                }
            }

            if (toggle)
            {
                if (!selectedPaths.Add(path))
                    selectedPaths.Remove(path);
            }
            else
            {
                selectedPaths.Clear();
                selectedPaths.Add(path);
            }
            selectionAnchor = path;
        }

        public void SelectAll(IReadOnlyList<string> orderedPaths)
        {
            selectedPaths.Clear();
            for (int index = 0; index < orderedPaths.Count; index++)
                selectedPaths.Add(orderedPaths[index]);
            selectionAnchor = orderedPaths.Count > 0 ? orderedPaths[orderedPaths.Count - 1] : null;
        }

        public void ClearSelection()
        {
            selectedPaths.Clear();
            selectionAnchor = null;
        }

        public string[] GetSelectedInDisplayOrder(IReadOnlyList<string> orderedPaths, string fallbackPath = null)
        {
            if (!string.IsNullOrEmpty(fallbackPath) && !selectedPaths.Contains(fallbackPath))
            {
                selectedPaths.Clear();
                selectedPaths.Add(fallbackPath);
                selectionAnchor = fallbackPath;
            }
            return orderedPaths.Where(selectedPaths.Contains).ToArray();
        }

        public DataObject CreateDragData(IReadOnlyList<string> orderedPaths, string fallbackPath)
        {
            string[] paths = GetSelectedInDisplayOrder(orderedPaths, fallbackPath);
            var data = new DataObject();
            data.SetData(InternalDragFormat, sourceId);
            data.SetData(DataFormats.FileDrop, paths);
            InternalDropHandled = false;
            return data;
        }

        public bool IsOwnDrag(IDataObject data)
        {
            return data != null
                && data.GetDataPresent(InternalDragFormat)
                && string.Equals(data.GetData(InternalDragFormat) as string, sourceId, StringComparison.Ordinal);
        }

        public void MarkHandledByFence(IDataObject data, IReadOnlyList<string> sourcePaths)
        {
            if (!(data is DataObject mutableData) || sourcePaths == null || sourcePaths.Count == 0)
                return;

            var paths = new string[sourcePaths.Count];
            for (int index = 0; index < sourcePaths.Count; index++)
                paths[index] = sourcePaths[index];
            mutableData.SetData(HandledFencePathsFormat, paths);
        }

        public string[] GetPathsHandledByFence(IDataObject data)
        {
            return data != null && data.GetDataPresent(HandledFencePathsFormat)
                ? data.GetData(HandledFencePathsFormat) as string[] ?? Array.Empty<string>()
                : Array.Empty<string>();
        }

        public bool TryReorder(
            List<string> customOrder,
            IReadOnlyList<string> displayedOrder,
            int targetIndex,
            out string[] previousOrder)
        {
            previousOrder = customOrder.ToArray();
            string[] movingPaths = GetSelectedInDisplayOrder(displayedOrder);
            if (movingPaths.Length == 0
                || !FenceItemOrder.TryMoveMany(customOrder, displayedOrder, movingPaths, targetIndex))
            {
                previousOrder = null;
                return false;
            }
            return true;
        }

        public int RemoveSelected(List<string> paths, IReadOnlyList<string> displayedOrder)
        {
            string[] selected = GetSelectedInDisplayOrder(displayedOrder);
            int removed = 0;
            for (int index = 0; index < selected.Length; index++)
                removed += paths.RemoveAll(path => string.Equals(path, selected[index], StringComparison.OrdinalIgnoreCase));
            ClearSelection();
            return removed;
        }

        public void RetainExisting(IReadOnlyCollection<string> paths)
        {
            selectedPaths.RemoveWhere(selected => !paths.Contains(selected, StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(selectionAnchor) && !selectedPaths.Contains(selectionAnchor))
                selectionAnchor = null;
        }

        private static int IndexOf(IReadOnlyList<string> paths, string path)
        {
            for (int index = 0; index < paths.Count; index++)
            {
                if (string.Equals(paths[index], path, StringComparison.OrdinalIgnoreCase))
                    return index;
            }
            return -1;
        }
    }
}
