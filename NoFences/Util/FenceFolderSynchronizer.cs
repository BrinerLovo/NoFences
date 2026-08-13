using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoFences.Util
{
    internal sealed class FenceFolderSyncResult
    {
        public List<string> AddedPaths { get; } = new List<string>();
        public List<string> RemovedPaths { get; } = new List<string>();
        public List<int> RemovedIndices { get; } = new List<int>();
        public bool Changed => AddedPaths.Count > 0 || RemovedPaths.Count > 0;
    }

    internal static class FenceFolderSynchronizer
    {
        private const string MetadataFileName = "__fence_metadata.xml";

        public static FenceFolderSyncResult Synchronize(List<string> displayedPaths, string folderPath)
        {
            if (displayedPaths == null)
                throw new ArgumentNullException(nameof(displayedPaths));
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("A linked folder path is required.", nameof(folderPath));

            Directory.CreateDirectory(folderPath);

            string[] folderItems = Directory
                .EnumerateFileSystemEntries(folderPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !IsMetadataPath(path))
                .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var folderItemSet = new HashSet<string>(folderItems, StringComparer.OrdinalIgnoreCase);
            var result = new FenceFolderSyncResult();

            for (int index = 0; index < displayedPaths.Count; index++)
            {
                string displayedPath = displayedPaths[index];
                if (IsDirectChild(displayedPath, folderPath)
                    && (IsMetadataPath(displayedPath) || !folderItemSet.Contains(displayedPath)))
                {
                    result.RemovedPaths.Add(displayedPath);
                    result.RemovedIndices.Add(index);
                }
            }

            for (int index = result.RemovedIndices.Count - 1; index >= 0; index--)
                displayedPaths.RemoveAt(result.RemovedIndices[index]);

            var displayedItemSet = new HashSet<string>(displayedPaths, StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < folderItems.Length; index++)
            {
                string folderItem = folderItems[index];
                if (displayedItemSet.Add(folderItem))
                {
                    displayedPaths.Add(folderItem);
                    result.AddedPaths.Add(folderItem);
                }
            }

            return result;
        }

        internal static bool IsMetadataPath(string path)
        {
            string fileName = Path.GetFileName(path);
            return string.Equals(fileName, MetadataFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, MetadataFileName + ".bak", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, MetadataFileName + ".tmp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectChild(string path, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return PathUtil.IsSamePath(Path.GetDirectoryName(trimmedPath), folderPath);
            }
            catch (Exception ex) when (
                ex is ArgumentException
                || ex is NotSupportedException
                || ex is PathTooLongException)
            {
                return false;
            }
        }
    }
}
