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
    }
}
