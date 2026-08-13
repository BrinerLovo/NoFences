using System;
using System.IO;

namespace NoFences.Util
{
    internal static class FenceFileMover
    {
        public static bool TryMove(
            string sourcePath,
            string destinationDirectory,
            out string destinationPath,
            out string errorMessage)
        {
            destinationPath = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(sourcePath)
                || string.IsNullOrWhiteSpace(destinationDirectory))
            {
                errorMessage = "A source path and destination folder are required.";
                return false;
            }

            try
            {
                string source = Path.GetFullPath(sourcePath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destinationFolder = Path.GetFullPath(destinationDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                bool isDirectory = Directory.Exists(source);

                if (!isDirectory && !File.Exists(source))
                {
                    errorMessage = "The source item no longer exists.";
                    return false;
                }

                if (PathUtil.IsSamePath(source, destinationFolder)
                    || PathUtil.IsPathWithinDirectory(destinationFolder, source))
                {
                    errorMessage = "A folder cannot be moved into itself or one of its descendants.";
                    return false;
                }

                if (PathUtil.IsSamePath(Path.GetDirectoryName(source), destinationFolder))
                {
                    destinationPath = source;
                    return true;
                }

                Directory.CreateDirectory(destinationFolder);
                destinationPath = PathUtil.GetUniqueDestinationPath(source, destinationFolder, isDirectory);

                if (isDirectory)
                    Directory.Move(source, destinationPath);
                else
                    File.Move(source, destinationPath);

                return true;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException)
            {
                destinationPath = null;
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
