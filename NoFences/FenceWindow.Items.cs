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

        private string HandleDraggedItem(string filePath)
        {
            if (!filePath.StartsWith(DesktopPath, StringComparison.OrdinalIgnoreCase) && !filePath.StartsWith(PublicDesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Non desktop: {filePath}");
                return filePath; // Ignore non-Desktop files
            }

            if (Directory.Exists(filePath))
            {
                return MoveFolderToFenceFolder(filePath);
            }
            else if (File.Exists(filePath))
            {
                return MoveFileToFenceFolder(filePath);
            }

            return filePath;
        }

        private string MoveFileToFenceFolder(string filePath)
        {
            // Ensure the fence folder exists
            if (!Directory.Exists(fenceFolderPath))
            {
                Directory.CreateDirectory(fenceFolderPath);
            }

            string fileName = Path.GetFileName(filePath);
            string newFilePath = Path.Combine(fenceFolderPath, fileName);

            // Handle name conflicts
            int counter = 1;
            string originalNewFilePath = newFilePath;
            while (File.Exists(newFilePath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(originalNewFilePath);
                string ext = Path.GetExtension(originalNewFilePath);
                string dir = Path.GetDirectoryName(originalNewFilePath);
                newFilePath = Path.Combine(dir, $"{nameWithoutExt}_{counter}{ext}");
                counter++;
            }

            try
            {
                File.Move(filePath, newFilePath);
                Console.WriteLine($"Moved file to fence folder: {filePath} → {newFilePath}");
                return newFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move file to fence folder: {ex.Message}");
                return filePath;
            }
        }

        private string MoveFolderToFenceFolder(string folderPath)
        {
            // Ensure the fence folder exists
            if (!Directory.Exists(fenceFolderPath))
            {
                Directory.CreateDirectory(fenceFolderPath);
            }

            string folderName = Path.GetFileName(folderPath);
            string newFolderPath = Path.Combine(fenceFolderPath, folderName);

            // Handle name conflicts
            int counter = 1;
            string originalNewFolderPath = newFolderPath;
            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(fenceFolderPath, $"{folderName}_{counter}");
                counter++;
            }

            try
            {
                Directory.Move(folderPath, newFolderPath);
                Console.WriteLine($"Moved folder to fence folder: {folderPath} → {newFolderPath}");
                return newFolderPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move folder to fence folder: {ex.Message}");
                return folderPath;
            }
        }

        // Keep the old static methods for backward compatibility when moving to global hidden desktop
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

        static string MoveFolderToHiddenDesktop(string folderPath)
        {
            if (!Directory.Exists(HiddenDesktopPath))
            {
                Directory.CreateDirectory(HiddenDesktopPath);
                File.SetAttributes(HiddenDesktopPath, FileAttributes.Hidden); // Hide the folder
            }

            string folderName = Path.GetFileName(folderPath);
            string newFolderPath = Path.Combine(HiddenDesktopPath, folderName);

            // Handle name conflicts
            int counter = 1;
            string originalNewFolderPath = newFolderPath;
            while (Directory.Exists(newFolderPath))
            {
                newFolderPath = Path.Combine(HiddenDesktopPath, $"{folderName}_{counter}");
                counter++;
            }

            try
            {
                Directory.Move(folderPath, newFolderPath);
                Console.WriteLine($"Moved folder: {folderPath} → {newFolderPath}");
                return newFolderPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move folder: {ex.Message}");
                return folderPath;
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

        public void MoveItemToDesktop(string path)
        {
            // Check if the file is in this fence's folder path or the global hidden desktop
            if (path.StartsWith(fenceFolderPath, StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith(HiddenDesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                {
                    string newFilePath = Path.Combine(DesktopPath, Path.GetFileName(path));
                    
                    // Handle name conflicts
                    int counter = 1;
                    string originalNewFilePath = newFilePath;
                    while (File.Exists(newFilePath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(originalNewFilePath);
                        string ext = Path.GetExtension(originalNewFilePath);
                        newFilePath = Path.Combine(DesktopPath, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }
                    
                    try
                    {
                        File.Move(path, newFilePath);
                        Console.WriteLine($"Moved file back to desktop: {path} → {newFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to move file back to desktop: {ex.Message}");
                    }
                }
                else if (Directory.Exists(path))
                {
                    string newFolderPath = Path.Combine(DesktopPath, Path.GetFileName(path));
                    
                    // Handle name conflicts
                    int counter = 1;
                    string originalNewFolderPath = newFolderPath;
                    while (Directory.Exists(newFolderPath))
                    {
                        string folderName = Path.GetFileName(originalNewFolderPath);
                        newFolderPath = Path.Combine(DesktopPath, $"{folderName}_{counter}");
                        counter++;
                    }
                    
                    try
                    {
                        Directory.Move(path, newFolderPath);
                        Console.WriteLine($"Moved folder back to desktop: {path} → {newFolderPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to move folder back to desktop: {ex.Message}");
                    }
                }
            }
        }

        public static void MoveFileToDesktop(string path)
        {
            // check if the file is in the hidden desktop
            if (path.StartsWith(HiddenDesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                {
                    string newFilePath = Path.Combine(DesktopPath, Path.GetFileName(path));
                    try
                    {
                        File.Move(path, newFilePath);
                        Console.WriteLine($"Moved file back to desktop: {path} → {newFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to move file back to desktop: {ex.Message}");
                    }
                }
                else if (Directory.Exists(path))
                {
                    string newFolderPath = Path.Combine(DesktopPath, Path.GetFileName(path));
                    try
                    {
                        Directory.Move(path, newFolderPath);
                        Console.WriteLine($"Moved folder back to desktop: {path} → {newFolderPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to move folder back to desktop: {ex.Message}");
                    }
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
