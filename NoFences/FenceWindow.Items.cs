using NoFences.Model;
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
            Console.WriteLine($"RemoveSelectedItem called for item: {hoveringItem}");
            Console.WriteLine($"Files before removal: {string.Join(", ", fenceInfo.Files)}");
            
            fenceInfo.Files.RemoveAll(x => x == hoveringItem);
            hoveringItem = null;
            
            Console.WriteLine($"Files after removal: {string.Join(", ", fenceInfo.Files)}");
            Save();
            Refresh();
        }

        static string HandleDraggedItem(string filePath)
        {
            if (!filePath.StartsWith(DesktopPath, StringComparison.OrdinalIgnoreCase) && !filePath.StartsWith(PublicDesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Non desktop: {filePath}");
                return filePath; // Ignore non-Desktop files
            }

            if (Directory.Exists(filePath))
            {
                return HideFolder(filePath);
            }
            else if (File.Exists(filePath))
            {
                return MoveFileToHiddenDesktop(filePath);
            }

            return filePath;
        }

        static string MoveFileToHiddenDesktop(string filePath)
        {
            if (!Directory.Exists(HiddenDesktopPath))
            {
                Directory.CreateDirectory(HiddenDesktopPath);
                File.SetAttributes(HiddenDesktopPath, FileAttributes.Hidden); // Hide the folder
            }

            string newFilePath = Path.Combine(HiddenDesktopPath, Path.GetFileName(filePath));

            try
            {
                File.Move(filePath, newFilePath);
                Console.WriteLine($"Moved file: {filePath} → {newFilePath}");
                return newFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move file: {ex.Message}");
                return filePath;
            }
        }

        static string HideFolder(string folderPath)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                //  dirInfo.Attributes |= FileAttributes.Hidden;
                Console.WriteLine($"Folder hidden: {folderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to hide folder: {ex.Message}");
            }
            return folderPath;
        }

        public static void MoveFileToDesktop(string path)
        {
            // check if the file is in the hidden desktop
            if (path.StartsWith(HiddenDesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                string newFilePath = Path.Combine(DesktopPath, Path.GetFileName(path));
                try
                {
                    File.Move(path, newFilePath);
                    Console.WriteLine($"Moved file: {path} → {newFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to move file: {ex.Message}");
                }
            }
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
