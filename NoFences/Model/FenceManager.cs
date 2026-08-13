using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Model
{
    public sealed class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private const string MetaFileName = "__fence_metadata.xml";
        private const string MetadataDirectoryName = "Metadata";
        private static readonly XmlSerializer FenceSerializer = new XmlSerializer(typeof(FenceInfo));

        private readonly object persistenceLock = new object();
        private readonly string basePath;
        private readonly string metadataRootPath;

        public List<FenceWindow> Fences { get; } = new List<FenceWindow>();

        public string DataDirectoryPath => basePath;

        private FenceManager()
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NoFences");
            metadataRootPath = Path.Combine(basePath, MetadataDirectoryName);

            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(metadataRootPath);
        }

        public void LoadFences()
        {
            try
            {
                MigrateLegacyMetadata();
            }
            catch (Exception ex) when (IsFileSystemException(ex))
            {
                AppLogger.Error("Unable to migrate legacy fence metadata.", ex);
            }

            var loadedIds = new HashSet<Guid>();
            string[] metadataDirectories;
            try
            {
                metadataDirectories = Directory.GetDirectories(metadataRootPath);
            }
            catch (Exception ex) when (IsFileSystemException(ex))
            {
                AppLogger.Error("Unable to enumerate fence metadata.", ex);
                return;
            }

            foreach (string metadataDirectory in metadataDirectories)
            {
                FenceInfo fenceInfo = LoadFenceMetadata(metadataDirectory);
                if (fenceInfo == null || fenceInfo.Id == Guid.Empty || !loadedIds.Add(fenceInfo.Id))
                    continue;

                try
                {
                    SettingsValidator.NormalizeFence(fenceInfo);
                    var window = new FenceWindow(fenceInfo);
                    Fences.Add(window);
                    window.Show();
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Unable to load fence '{fenceInfo.Name}'.", ex);
                }
            }
        }

        public void AddNewFence(FenceWindow fence)
        {
            if (fence != null && !Fences.Contains(fence))
                Fences.Add(fence);
        }

        public void RemoveFence(FenceWindow fence)
        {
            if (fence != null)
                Fences.Remove(fence);
        }

        public void CreateFence(string name)
        {
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = name,
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };
            SettingsValidator.NormalizeFence(fenceInfo);

            UpdateFence(fenceInfo);
            var window = new FenceWindow(fenceInfo);
            AddNewFence(window);
            window.Show();
        }

        public void RemoveFence(FenceInfo info, FenceWindow window)
        {
            if (info == null)
                return;

            // Fence removal deletes configuration only. Linked folders and their
            // contents always remain untouched.
            string metadataDirectory = GetMetadataDirectoryPath(info.Id);
            DeleteIfExists(Path.Combine(metadataDirectory, MetaFileName));
            DeleteIfExists(Path.Combine(metadataDirectory, MetaFileName + ".bak"));
            DeleteIfExists(Path.Combine(metadataDirectory, MetaFileName + ".tmp"));

            try
            {
                if (Directory.Exists(metadataDirectory)
                    && Directory.GetFileSystemEntries(metadataDirectory).Length == 0)
                {
                    Directory.Delete(metadataDirectory, false);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Unable to remove empty metadata directory: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Unable to remove empty metadata directory: {ex.Message}");
            }

            RemoveFence(window);
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));
            if (fenceInfo.Id == Guid.Empty)
                throw new InvalidOperationException("Fence metadata must have a stable identifier.");

            SettingsValidator.NormalizeFence(fenceInfo);

            lock (persistenceLock)
            {
                string metadataDirectory = GetMetadataDirectoryPath(fenceInfo.Id);
                Directory.CreateDirectory(metadataDirectory);
                WriteMetadataAtomically(fenceInfo, metadataDirectory);
                CleanupLegacyMetadataFiles(GetContentFolderPath(fenceInfo));
            }
        }

        public string GetContentFolderPath(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            return !string.IsNullOrWhiteSpace(fenceInfo.CustomFolderPath)
                ? Path.GetFullPath(fenceInfo.CustomFolderPath)
                : Path.Combine(basePath, fenceInfo.Id.ToString("D"));
        }

        public string GetDefaultContentFolderPath(Guid fenceId)
        {
            if (fenceId == Guid.Empty)
                throw new ArgumentException("A fence identifier is required.", nameof(fenceId));
            return Path.Combine(basePath, fenceId.ToString("D"));
        }

        private string GetMetadataDirectoryPath(Guid fenceId)
        {
            return Path.Combine(metadataRootPath, fenceId.ToString("D"));
        }

        internal static FenceInfo LoadFenceMetadata(string metadataDirectory)
        {
            string primaryPath = Path.Combine(metadataDirectory, MetaFileName);
            FenceInfo fenceInfo = DeserializeFence(primaryPath);
            if (fenceInfo != null)
                return fenceInfo;

            string backupPath = primaryPath + ".bak";
            fenceInfo = DeserializeFence(backupPath);
            if (fenceInfo != null)
            {
                AppLogger.Info($"Recovered fence metadata from backup: {backupPath}");
                TryRestorePrimaryMetadata(backupPath, primaryPath);
            }

            return fenceInfo;
        }

        private static void TryRestorePrimaryMetadata(string backupPath, string primaryPath)
        {
            try
            {
                File.Copy(backupPath, primaryPath, true);
            }
            catch (Exception ex) when (IsFileSystemException(ex))
            {
                AppLogger.Error("Unable to repair primary fence metadata from backup.", ex);
            }
        }

        private static FenceInfo DeserializeFence(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return FenceSerializer.Deserialize(stream) as FenceInfo;
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidOperationException)
            {
                Debug.WriteLine($"Unable to read fence metadata '{path}': {ex.Message}");
                return null;
            }
        }

        private static void WriteMetadataAtomically(FenceInfo fenceInfo, string metadataDirectory)
        {
            string metadataPath = Path.Combine(metadataDirectory, MetaFileName);
            string temporaryPath = metadataPath + ".tmp";
            string backupPath = metadataPath + ".bak";

            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                {
                    FenceSerializer.Serialize(stream, fenceInfo);
                    stream.Flush(true);
                }

                if (File.Exists(metadataPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, metadataPath, backupPath, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceMetadataByCopy(temporaryPath, metadataPath, backupPath);
                    }
                    catch (IOException)
                    {
                        ReplaceMetadataByCopy(temporaryPath, metadataPath, backupPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, metadataPath);
                }
            }
            finally
            {
                DeleteIfExists(temporaryPath);
            }
        }

        private static void ReplaceMetadataByCopy(string temporaryPath, string metadataPath, string backupPath)
        {
            File.Copy(metadataPath, backupPath, true);
            File.Copy(temporaryPath, metadataPath, true);
            File.Delete(temporaryPath);
        }

        private void MigrateLegacyMetadata()
        {
            foreach (string directory in Directory.EnumerateDirectories(basePath))
            {
                if (string.Equals(directory, metadataRootPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                string legacyMetadataPath = Path.Combine(directory, MetaFileName);
                FenceInfo fenceInfo = DeserializeFence(legacyMetadataPath);
                if (fenceInfo == null || fenceInfo.Id == Guid.Empty)
                    continue;

                string newMetadataPath = Path.Combine(GetMetadataDirectoryPath(fenceInfo.Id), MetaFileName);
                if (!File.Exists(newMetadataPath))
                    UpdateFence(fenceInfo);

                DeleteIfExists(legacyMetadataPath);
                DeleteIfExists(legacyMetadataPath + ".bak");
            }
        }

        private static void CleanupLegacyMetadataFiles(string contentFolderPath)
        {
            if (string.IsNullOrWhiteSpace(contentFolderPath))
                return;

            DeleteIfExists(Path.Combine(contentFolderPath, MetaFileName));
            DeleteIfExists(Path.Combine(contentFolderPath, MetaFileName + ".bak"));
            DeleteIfExists(Path.Combine(contentFolderPath, MetaFileName + ".tmp"));
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Unable to delete metadata file '{path}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Unable to delete metadata file '{path}': {ex.Message}");
            }
        }

        private static bool IsFileSystemException(Exception exception)
        {
            return exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException
                || exception is NotSupportedException;
        }
    }
}
