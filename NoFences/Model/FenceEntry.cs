using NoFences.Util;
using NoFences.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace NoFences.Model
{
    public class FenceEntry
    {
        public string Path { get; }

        public EntryType Type { get; }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        private FenceEntry(string path, EntryType type)
        {
            Path = path;
            Type = type;
        }

        public static FenceEntry FromPath(string path)
        {
            if (File.Exists(path))
                return new FenceEntry(path, EntryType.File);
            else if (Directory.Exists(path))
                return new FenceEntry(path, EntryType.Folder);
            else 
            {
                Console.WriteLine($"FenceEntry.FromPath: Path does not exist: {path}");
                return null;
            }
        }

        public Icon ExtractIcon(ThumbnailProvider thumbnailProvider)
        {
            if (Type == EntryType.File)
            {
                try
                {
                    string localPath = ConvertToLocalPath(Path);

                    // Check if a thumbnail is supported
                    if (thumbnailProvider.IsSupported(localPath))
                        return thumbnailProvider.GenerateThumbnail(localPath);

                    // Try extracting associated icon
                    Icon icon = Icon.ExtractAssociatedIcon(localPath);
                    if (icon != null)
                        return icon;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error extracting icon: {ex.Message}");
                }

                // Return generic/unknown file icon if extraction fails
                return IconUtil.UnknownFile;
            }
            else
            {
                return IconUtil.FolderLarge;
            }
        }

        /// <summary>
        /// In case the file is an UNC file path, convert it to a local path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string ConvertToLocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (path.StartsWith(@"\\"))
            {
                try
                {
                    return new Uri(path).LocalPath; // Convert UNC to local path
                }
                catch
                {
                    // Return original path if conversion fails
                    return path;
                }
            }
            return path;
        }

        public void Open()
        {
            Task.Run(() =>
            {
                // start asynchronously
                try
                {
                    if (Type == EntryType.File)
                        Process.Start(Path);
                    else if (Type == EntryType.Folder)
                        Process.Start("explorer.exe", Path);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to start: {e}");
                }
            });
        }
    }
}
