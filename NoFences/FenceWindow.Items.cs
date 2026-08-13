using NoFences.Model;
using NoFences.Util;
using System;
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
            string[] previousItems = fenceInfo.Files.ToArray();
            int removed = dragDropController.RemoveSelected(
                fenceInfo.Files,
                GetLayoutSnapshot().OrderedPaths);
            if (removed == 0)
                return;

            RecordUndo(removed == 1 ? "remove item" : $"remove {removed} items", previousItems);
            hoveringItem = null;
            Save();
            InvalidateFenceContent();
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

        private bool ItemExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }
    }
}
