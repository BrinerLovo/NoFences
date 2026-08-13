using System;
using System.Collections.Generic;

namespace NoFences.Util
{
    internal static class FenceItemOrder
    {
        public static bool TryMove(List<string> paths, string path, int targetIndex, out int originalIndex)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            originalIndex = paths.FindIndex(item =>
                string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
            if (originalIndex < 0 || targetIndex < 0)
                return false;

            string itemToMove = paths[originalIndex];
            paths.RemoveAt(originalIndex);
            int insertionIndex = Math.Max(0, Math.Min(targetIndex, paths.Count));
            paths.Insert(insertionIndex, itemToMove);
            return insertionIndex != originalIndex;
        }

        public static bool TryMoveMany(List<string> items, IReadOnlyList<string> paths, int targetIndex)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (paths == null || paths.Count == 0)
                return false;

            var selected = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            var moving = new List<string>(paths.Count);
            int selectedBeforeTarget = 0;
            for (int index = 0; index < items.Count; index++)
            {
                if (!selected.Contains(items[index]))
                    continue;
                moving.Add(items[index]);
                if (index < targetIndex)
                    selectedBeforeTarget++;
            }
            if (moving.Count == 0)
                return false;

            string[] previous = items.ToArray();
            items.RemoveAll(selected.Contains);
            int insertionIndex = Math.Max(0, Math.Min(targetIndex - selectedBeforeTarget, items.Count));
            items.InsertRange(insertionIndex, moving);
            for (int index = 0; index < previous.Length; index++)
            {
                if (!string.Equals(previous[index], items[index], StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static bool TryMoveMany(
            List<string> items,
            IReadOnlyList<string> displayedOrder,
            IReadOnlyList<string> paths,
            int targetIndex)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (displayedOrder == null)
                throw new ArgumentNullException(nameof(displayedOrder));

            var reorderedVisibleItems = new List<string>(displayedOrder.Count);
            for (int index = 0; index < displayedOrder.Count; index++)
                reorderedVisibleItems.Add(displayedOrder[index]);
            if (!TryMoveMany(reorderedVisibleItems, paths, targetIndex))
                return false;

            var visiblePaths = new HashSet<string>(displayedOrder, StringComparer.OrdinalIgnoreCase);
            int visibleIndex = 0;
            for (int index = 0; index < items.Count && visibleIndex < reorderedVisibleItems.Count; index++)
            {
                if (visiblePaths.Contains(items[index]))
                    items[index] = reorderedVisibleItems[visibleIndex++];
            }
            return true;
        }
    }
}
