using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace NoFences.Routing
{
    internal sealed class RoutingRuleManager : IDisposable
    {
        public static RoutingRuleManager Instance { get; } = new RoutingRuleManager();

        private readonly RoutingRuleRepository repository =
            new RoutingRuleRepository(FenceManager.Instance.DataDirectoryPath);
        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private readonly Dictionary<string, int> pendingPaths =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 650 };
        private SynchronizationContext synchronizationContext;

        private RoutingRuleManager()
        {
            Rules = repository.Load();
            NormalizeRules(Rules);
            timer.Tick += Timer_Tick;
        }

        public List<RoutingRule> Rules { get; private set; }

        public void Start()
        {
            synchronizationContext = SynchronizationContext.Current;
            RestartWatchers();
        }

        public void ReplaceRules(IEnumerable<RoutingRule> rules)
        {
            Rules = rules?.ToList() ?? new List<RoutingRule>();
            NormalizeRules(Rules);
            repository.Save(Rules);
            RestartWatchers();
        }

        public void SaveAndRestart()
        {
            NormalizeRules(Rules);
            repository.Save(Rules);
            RestartWatchers();
        }

        private void RestartWatchers()
        {
            DisposeWatchers();
            foreach (string folder in Rules
                .Where(rule => rule.Enabled && Directory.Exists(rule.SourceFolder))
                .Select(rule => Path.GetFullPath(rule.SourceFolder))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var watcher = new FileSystemWatcher(folder)
                    {
                        Filter = "*",
                        IncludeSubdirectories = false,
                        NotifyFilter = NotifyFilters.FileName,
                        InternalBufferSize = 16 * 1024
                    };
                    watcher.Created += ItemCreated;
                    watcher.Renamed += ItemCreated;
                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
                {
                    AppLogger.Error($"Unable to monitor routing folder '{folder}'.", ex);
                }
            }
        }

        private void ItemCreated(object sender, FileSystemEventArgs e)
        {
            synchronizationContext?.Post(_ =>
            {
                pendingPaths[e.FullPath] = 0;
                timer.Stop();
                timer.Start();
            }, null);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            KeyValuePair<string, int>[] paths = pendingPaths.ToArray();
            pendingPaths.Clear();
            for (int index = 0; index < paths.Length; index++)
            {
                string path = paths[index].Key;
                int attempt = paths[index].Value;
                if (!TryRoute(path)
                    && attempt < 5
                    && (File.Exists(path) || Directory.Exists(path))
                    && HasMatchingRule(path))
                {
                    pendingPaths[path] = attempt + 1;
                }
            }
            if (pendingPaths.Count > 0)
                timer.Start();
        }

        internal bool TryRoute(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;

            string sourceFolder = Path.GetDirectoryName(Path.GetFullPath(path));
            string extension = Directory.Exists(path) ? string.Empty : Path.GetExtension(path);
            RoutingRule rule = Rules.FirstOrDefault(candidate =>
                candidate.Enabled
                && PathUtil.IsSamePath(candidate.SourceFolder, sourceFolder)
                && candidate.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
            if (rule == null)
                return false;

            FenceWindow destination = FenceManager.Instance.FindFence(rule.DestinationFenceId);
            return destination != null && destination.AcceptRoutedItem(path);
        }

        public bool HasMatchingRule(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                return false;
            }

            string sourceFolder = Path.GetDirectoryName(fullPath);
            string extension = Path.GetExtension(fullPath);
            return Rules.Any(rule =>
                rule.Enabled
                && FenceManager.Instance.FindFence(rule.DestinationFenceId) != null
                && PathUtil.IsSamePath(rule.SourceFolder, sourceFolder)
                && rule.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        private static void NormalizeRules(List<RoutingRule> rules)
        {
            rules.RemoveAll(rule => rule == null);
            for (int index = 0; index < rules.Count; index++)
            {
                RoutingRule rule = rules[index];
                if (rule.Id == Guid.Empty)
                    rule.Id = Guid.NewGuid();
                rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? "Routing rule" : rule.Name.Trim();
                rule.SourceFolder = string.IsNullOrWhiteSpace(rule.SourceFolder) ? string.Empty : rule.SourceFolder.Trim();
                rule.Extensions = SettingsValidator.NormalizeExtensions(rule.Extensions);
            }
        }

        private void DisposeWatchers()
        {
            for (int index = 0; index < watchers.Count; index++)
                watchers[index].Dispose();
            watchers.Clear();
        }

        public void Dispose()
        {
            timer.Dispose();
            DisposeWatchers();
        }
    }
}
