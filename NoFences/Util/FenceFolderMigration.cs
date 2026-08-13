using System;
using System.Collections.Generic;
using System.IO;

namespace NoFences.Util
{
    internal sealed class FenceFolderMigrationResult
    {
        public Dictionary<string, string> MovedPaths { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    internal static class FenceFolderMigration
    {
        public static bool TryMoveContents(
            string sourceDirectory,
            string destinationDirectory,
            out FenceFolderMigrationResult result,
            out string errorMessage)
        {
            result = new FenceFolderMigrationResult();
            errorMessage = null;

            try
            {
                string source = PathUtil.NormalizeDirectoryPath(sourceDirectory);
                string destination = PathUtil.NormalizeDirectoryPath(destinationDirectory);

                if (PathUtil.IsSamePath(source, destination))
                    return true;
                if (PathUtil.IsPathWithinDirectory(destination, source))
                {
                    errorMessage = "The destination folder cannot be inside the current fence folder.";
                    return false;
                }
                if (!Directory.Exists(source))
                {
                    Directory.CreateDirectory(destination);
                    return true;
                }

                string[] entries = Directory.GetFileSystemEntries(source, "*", SearchOption.TopDirectoryOnly);
                Directory.CreateDirectory(destination);
                for (int index = 0; index < entries.Length; index++)
                {
                    string entry = entries[index];
                    if (FenceFolderSynchronizer.IsMetadataPath(entry))
                        continue;

                    bool isDirectory = Directory.Exists(entry);
                    string movedPath = PathUtil.GetUniqueDestinationPath(entry, destination, isDirectory);
                    if (isDirectory)
                        Directory.Move(entry, movedPath);
                    else
                        File.Move(entry, movedPath);
                    result.MovedPaths.Add(entry, movedPath);
                }

                return true;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException)
            {
                errorMessage = ex.Message;
                RollBack(result.MovedPaths);
                result.MovedPaths.Clear();
                AppLogger.Error("Unable to migrate fence folder contents.", ex);
                return false;
            }
        }

        private static void RollBack(Dictionary<string, string> movedPaths)
        {
            var moves = new List<KeyValuePair<string, string>>(movedPaths);
            for (int index = moves.Count - 1; index >= 0; index--)
            {
                try
                {
                    string originalPath = moves[index].Key;
                    string movedPath = moves[index].Value;
                    if (Directory.Exists(movedPath))
                        Directory.Move(movedPath, originalPath);
                    else if (File.Exists(movedPath))
                        File.Move(movedPath, originalPath);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Unable to roll back a fence folder move.", ex);
                }
            }
        }
    }
}
