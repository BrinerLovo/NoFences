using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NoFences.Model
{
    public sealed class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private readonly FenceRepository repository = new FenceRepository();

        public List<FenceWindow> Fences { get; } = new List<FenceWindow>();
        public string DataDirectoryPath => repository.DataDirectoryPath;

        private FenceManager()
        {
        }

        public void LoadFences()
        {
            IReadOnlyList<FenceInfo> savedFences = repository.LoadAll();
            for (int index = 0; index < savedFences.Count; index++)
            {
                FenceInfo fenceInfo = savedFences[index];
                try
                {
                    var window = new FenceWindow(fenceInfo);
                    Fences.Add(window);
                    if (!fenceInfo.Hidden)
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
            repository.Save(fenceInfo);

            var window = new FenceWindow(fenceInfo);
            AddNewFence(window);
            window.Show();
        }

        public void SetAllVisible(bool visible)
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].SetFenceVisible(visible);
        }

        public void SetAllLocked(bool locked)
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].SetFenceLocked(locked);
        }

        public void SyncAll()
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].SyncFromTray(showResult: false);
        }

        public void RefreshAllSettings()
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].RefreshSettings();
        }

        public void CloseAll()
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].Close();
        }

        public FenceWindow FindFence(Guid fenceId)
        {
            return Fences.Find(window => window.FenceId == fenceId);
        }

        public IReadOnlyList<FenceInfo> GetSavedFenceInfos()
        {
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].FlushPendingSaves();
            return repository.LoadAll();
        }

        public void ReplaceFences(IEnumerable<FenceInfo> importedFences)
        {
            IReadOnlyList<FenceInfo> existing = repository.LoadAll();
            FenceWindow[] windows = Fences.ToArray();
            for (int index = 0; index < windows.Length; index++)
                windows[index].Close();
            for (int index = 0; index < existing.Count; index++)
                repository.Delete(existing[index]);

            var importedIds = new HashSet<Guid>();
            foreach (FenceInfo info in importedFences ?? Array.Empty<FenceInfo>())
            {
                if (info == null)
                    continue;
                if (info.Id == Guid.Empty)
                    info.Id = Guid.NewGuid();
                if (!importedIds.Add(info.Id))
                    continue;
                SettingsValidator.NormalizeFence(info);
                repository.Save(info);
                var window = new FenceWindow(info);
                Fences.Add(window);
                if (!info.Hidden)
                    window.Show();
            }
        }

        public void RemoveFence(FenceInfo info, FenceWindow window)
        {
            repository.Delete(info);
            RemoveFence(window);
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            repository.Save(fenceInfo);
        }

        public string GetContentFolderPath(FenceInfo fenceInfo)
        {
            return repository.GetContentFolderPath(fenceInfo);
        }

        public string GetDefaultContentFolderPath(Guid fenceId)
        {
            return repository.GetDefaultContentFolderPath(fenceId);
        }

        internal static FenceInfo LoadFenceMetadata(string metadataDirectory)
        {
            return FenceRepository.LoadFenceMetadata(metadataDirectory);
        }
    }
}
