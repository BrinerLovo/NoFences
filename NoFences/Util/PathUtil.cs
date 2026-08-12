using System;
using System.IO;

namespace NoFences.Util
{
    internal static class PathUtil
    {
        public static bool IsSamePath(string firstPath, string secondPath)
        {
            if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
                return false;

            try
            {
                return string.Equals(Normalize(firstPath), Normalize(secondPath), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (IsPathException(ex))
            {
                return false;
            }
        }

        public static bool IsPathWithinDirectory(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
                return false;

            try
            {
                string normalizedPath = Normalize(path);
                string normalizedDirectory = Normalize(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                return normalizedPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (IsPathException(ex))
            {
                return false;
            }
        }

        public static string GetUniqueDestinationPath(string sourcePath, string destinationDirectory, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("A source path is required.", nameof(sourcePath));
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new ArgumentException("A destination directory is required.", nameof(destinationDirectory));

            string name = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string candidate = Path.Combine(destinationDirectory, name);
            if (!PathExists(candidate))
                return candidate;

            string baseName = isDirectory ? name : Path.GetFileNameWithoutExtension(name);
            string extension = isDirectory ? string.Empty : Path.GetExtension(name);
            for (int suffix = 2; ; suffix++)
            {
                candidate = Path.Combine(destinationDirectory, $"{baseName} ({suffix}){extension}");
                if (!PathExists(candidate))
                    return candidate;
            }
        }

        private static bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        private static string Normalize(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsPathException(Exception exception)
        {
            return exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException;
        }
    }
}
