using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Model
{
    internal sealed class FenceRepository
    {
        private const string MetadataFileName = "__fence_metadata.xml";
        private const string MetadataDirectoryName = "Metadata";
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(FenceInfo));

        private readonly object persistenceLock = new object();
        private readonly string basePath;
        private readonly string metadataRootPath;

        public FenceRepository()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NoFences"))
        {
        }

        internal FenceRepository(string dataDirectoryPath)
        {
            basePath = Path.GetFullPath(dataDirectoryPath);
            metadataRootPath = Path.Combine(basePath, MetadataDirectoryName);
            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(metadataRootPath);
        }

        public string DataDirectoryPath => basePath;

        public IReadOnlyList<FenceInfo> LoadAll()
        {
            TryMigrateLegacyMetadata();
            string[] metadataDirectories;
            try
            {
                metadataDirectories = Directory.GetDirectories(metadataRootPath);
            }
            catch (Exception ex) when (IsFileSystemException(ex))
            {
                AppLogger.Error("Unable to enumerate fence metadata.", ex);
                return Array.Empty<FenceInfo>();
            }

            var fences = new List<FenceInfo>(metadataDirectories.Length);
            var loadedIds = new HashSet<Guid>();
            for (int index = 0; index < metadataDirectories.Length; index++)
            {
                FenceInfo fenceInfo = LoadFenceMetadata(metadataDirectories[index]);
                if (fenceInfo == null || fenceInfo.Id == Guid.Empty || !loadedIds.Add(fenceInfo.Id))
                    continue;

                SettingsValidator.NormalizeFence(fenceInfo);
                fences.Add(fenceInfo);
            }

            return fences;
        }

        public void Save(FenceInfo fenceInfo)
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

        public void Delete(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                return;

            lock (persistenceLock)
            {
                string metadataDirectory = GetMetadataDirectoryPath(fenceInfo.Id);
                DeleteIfExists(Path.Combine(metadataDirectory, MetadataFileName));
                DeleteIfExists(Path.Combine(metadataDirectory, MetadataFileName + ".bak"));
                DeleteIfExists(Path.Combine(metadataDirectory, MetadataFileName + ".tmp"));
                try
                {
                    if (Directory.Exists(metadataDirectory)
                        && Directory.GetFileSystemEntries(metadataDirectory).Length == 0)
                    {
                        Directory.Delete(metadataDirectory, false);
                    }
                }
                catch (Exception ex) when (IsFileSystemException(ex))
                {
                    AppLogger.Error("Unable to remove an empty fence metadata directory.", ex);
                }
            }
        }

        public string GetContentFolderPath(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            return !string.IsNullOrWhiteSpace(fenceInfo.CustomFolderPath)
                ? Path.GetFullPath(fenceInfo.CustomFolderPath)
                : GetDefaultContentFolderPath(fenceInfo.Id);
        }

        public string GetDefaultContentFolderPath(Guid fenceId)
        {
            if (fenceId == Guid.Empty)
                throw new ArgumentException("A fence identifier is required.", nameof(fenceId));
            return Path.Combine(basePath, fenceId.ToString("D"));
        }

        internal static FenceInfo LoadFenceMetadata(string metadataDirectory)
        {
            string primaryPath = Path.Combine(metadataDirectory, MetadataFileName);
            FenceInfo fenceInfo = DeserializeFence(primaryPath);
            if (fenceInfo != null)
                return fenceInfo;

            string backupPath = primaryPath + ".bak";
            fenceInfo = DeserializeFence(backupPath);
            if (fenceInfo == null)
                return null;

            AppLogger.Info($"Recovered fence metadata from backup: {backupPath}");
            TryRestorePrimaryMetadata(backupPath, primaryPath);
            return fenceInfo;
        }

        private string GetMetadataDirectoryPath(Guid fenceId)
        {
            return Path.Combine(metadataRootPath, fenceId.ToString("D"));
        }

        private void TryMigrateLegacyMetadata()
        {
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(basePath))
                {
                    if (string.Equals(directory, metadataRootPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string legacyMetadataPath = Path.Combine(directory, MetadataFileName);
                    FenceInfo fenceInfo = DeserializeFence(legacyMetadataPath);
                    if (fenceInfo == null || fenceInfo.Id == Guid.Empty)
                        continue;

                    string newMetadataPath = Path.Combine(GetMetadataDirectoryPath(fenceInfo.Id), MetadataFileName);
                    if (!File.Exists(newMetadataPath))
                        Save(fenceInfo);

                    DeleteIfExists(legacyMetadataPath);
                    DeleteIfExists(legacyMetadataPath + ".bak");
                }
            }
            catch (Exception ex) when (IsFileSystemException(ex))
            {
                AppLogger.Error("Unable to migrate legacy fence metadata.", ex);
            }
        }

        private static FenceInfo DeserializeFence(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return Serializer.Deserialize(stream) as FenceInfo;
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
            string metadataPath = Path.Combine(metadataDirectory, MetadataFileName);
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
                    Serializer.Serialize(stream, fenceInfo);
                    stream.Flush(true);
                }

                if (!File.Exists(metadataPath))
                {
                    File.Move(temporaryPath, metadataPath);
                    return;
                }

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

        private static void CleanupLegacyMetadataFiles(string contentFolderPath)
        {
            if (string.IsNullOrWhiteSpace(contentFolderPath))
                return;

            DeleteIfExists(Path.Combine(contentFolderPath, MetadataFileName));
            DeleteIfExists(Path.Combine(contentFolderPath, MetadataFileName + ".bak"));
            DeleteIfExists(Path.Combine(contentFolderPath, MetadataFileName + ".tmp"));
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
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
