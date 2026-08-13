using NoFences.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoFences
{
    public partial class FenceWindow
    {
        private readonly ThrottledExecution folderSyncDebouncer =
            new ThrottledExecution(TimeSpan.FromMilliseconds(250));
        private readonly ThrottledExecution desktopImportDebouncer =
            new ThrottledExecution(TimeSpan.FromMilliseconds(500));
        private readonly HashSet<string> pendingDesktopImports =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void InitializeFileWatchersOptimized()
        {
            DisposeFileWatchersOptimized();

            try
            {
                Directory.CreateDirectory(fenceFolderPath);
                fenceWatcher = new FileSystemWatcher(fenceFolderPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "*",
                    IncludeSubdirectories = false
                };
                fenceWatcher.Created += OptimizedFenceFolderChanged;
                fenceWatcher.Renamed += OptimizedFenceFolderChanged;
                fenceWatcher.Deleted += OptimizedFenceFolderChanged;
                fenceWatcher.Error += OptimizedWatcherError;
                fenceWatcher.EnableRaisingEvents = true;

                if (fenceInfo.WatchedExtensions != null && fenceInfo.WatchedExtensions.Count > 0)
                {
                    desktopWatcher = new FileSystemWatcher(DesktopPath)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                        Filter = "*",
                        IncludeSubdirectories = false
                    };
                    desktopWatcher.Created += OptimizedDesktopItemCreated;
                    desktopWatcher.Error += OptimizedWatcherError;
                    desktopWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to initialize file watchers: {ex.Message}");
                DisposeFileWatchersOptimized();
            }
        }

        private void OptimizedFenceFolderChanged(object sender, FileSystemEventArgs e)
        {
            QueueUiAction(() => folderSyncDebouncer.Run(SynchronizeLinkedFolderFromWatcher));
        }

        private void SynchronizeLinkedFolderFromWatcher()
        {
            try
            {
                SynchronizeFenceFolder(recordUndo: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to synchronize linked folder: {ex.Message}");
            }
        }

        private void OptimizedDesktopItemCreated(object sender, FileSystemEventArgs e)
        {
            string extension = Path.GetExtension(e.FullPath);
            bool watchedFile = fenceInfo.WatchedExtensions != null
                && fenceInfo.WatchedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            if (!watchedFile && !Directory.Exists(e.FullPath))
                return;

            QueueUiAction(() =>
            {
                pendingDesktopImports.Add(e.FullPath);
                desktopImportDebouncer.Run(ProcessPendingDesktopImports);
            });
        }

        private void ProcessPendingDesktopImports()
        {
            string[] paths = pendingDesktopImports.ToArray();
            pendingDesktopImports.Clear();
            bool changed = false;

            for (int index = 0; index < paths.Length; index++)
            {
                if (TryMoveItemToFenceFolder(paths[index], out string destinationPath)
                    && !fenceInfo.Files.Contains(destinationPath, StringComparer.OrdinalIgnoreCase))
                {
                    fenceInfo.Files.Add(destinationPath);
                    changed = true;
                }
            }

            if (changed)
            {
                Save();
                Invalidate();
            }
        }

        private void OptimizedWatcherError(object sender, ErrorEventArgs e)
        {
            QueueUiAction(InitializeFileWatchersOptimized);
        }

        private void QueueUiAction(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // The form closed between the state check and BeginInvoke.
            }
        }

        private void DisposeFileWatchersOptimized()
        {
            if (fenceWatcher != null)
            {
                fenceWatcher.EnableRaisingEvents = false;
                fenceWatcher.Created -= OptimizedFenceFolderChanged;
                fenceWatcher.Renamed -= OptimizedFenceFolderChanged;
                fenceWatcher.Deleted -= OptimizedFenceFolderChanged;
                fenceWatcher.Error -= OptimizedWatcherError;
                fenceWatcher.Dispose();
                fenceWatcher = null;
            }

            if (desktopWatcher != null)
            {
                desktopWatcher.EnableRaisingEvents = false;
                desktopWatcher.Created -= OptimizedDesktopItemCreated;
                desktopWatcher.Error -= OptimizedWatcherError;
                desktopWatcher.Dispose();
                desktopWatcher = null;
            }
        }
    }
}
