using NoFences.Model;
using NoFences.Util;
using System;
using System.Drawing;
using System.IO;

namespace NoFences
{
    public partial class FenceWindow
    {
        static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        static readonly string PublicDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        public static readonly string HiddenDesktopPath = Path.Combine(DesktopPath, "Desktop"); // Hidden folder

        private void RemoveSelectedItem()
        {
            string itemToRemove = selectedItem ?? hoveringItem;
            if (string.IsNullOrEmpty(itemToRemove))
                return;

            int itemIndex = fenceInfo.Files.FindIndex(
                path => string.Equals(path, itemToRemove, StringComparison.OrdinalIgnoreCase));
            if (itemIndex < 0)
                return;

            RecordRemoveUndo(itemToRemove, itemIndex);
            fenceInfo.Files.RemoveAt(itemIndex);
            selectedItem = null;
            hoveringItem = null;

            Save();
            Invalidate();
        }

        private bool TryMoveItemToFenceFolder(string sourcePath, out string destinationPath)
        {
            bool moved = FenceFileMover.TryMove(
                sourcePath,
                fenceFolderPath,
                out destinationPath,
                out string errorMessage);

            if (!moved && !string.IsNullOrEmpty(errorMessage))
                System.Diagnostics.Debug.WriteLine($"Unable to move item into fence: {errorMessage}");

            return moved;
        }

        private int GetItemIndexAtPosition(Point pos)
        {
            int x = itemPadding, y = itemPadding;
            int index = 0;
            var files = fenceInfo.Files.ToArray();

            foreach (var file in files)
            {
                var entry = FenceEntry.FromPath(file);
                if (entry == null)
                    continue;

                // Define item rectangle (the area occupied by an icon)
                var itemRect = new Rectangle(x, y + titleHeight - scrollOffset, itemWidth, itemHeight);

                // If dropping inside an existing item's space, return its index
                if (itemRect.Contains(pos))
                    return index;

                // Move to next item position
                x += itemWidth + itemPadding;
                if (x + itemWidth > Width)
                {
                    x = itemPadding;
                    y += itemHeight + itemPadding;
                }

                index++;
            }

            // **New Fix: Handle Empty Spaces**
            // If dropping in an empty space, find the nearest valid index
            int estimatedIndex = (pos.Y - titleHeight + scrollOffset) / (itemHeight + itemPadding) * (Width / (itemWidth + itemPadding))
                                 + pos.X / (itemWidth + itemPadding);
            return Math.Min(estimatedIndex, files.Length); // Ensure index is within bounds
        }

        private bool ItemExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }
    }
}
