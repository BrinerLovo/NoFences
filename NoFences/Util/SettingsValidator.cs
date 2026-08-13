using NoFences.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoFences.Util
{
    internal static class SettingsValidator
    {
        public const int MinimumFenceWidth = 140;
        public const int MinimumFenceHeight = 80;
        public const int MinimumTitleHeight = 16;
        public const int MaximumTitleHeight = 72;

        public static void NormalizeGlobalSettings()
        {
            var settings = Properties.Settings.Default;
            settings.opacity = Clamp(settings.opacity, 0, 255);
            settings.headerAlpha = Clamp(settings.headerAlpha, 0, 255);
            settings.title_size = Clamp(settings.title_size, MinimumTitleHeight, MaximumTitleHeight);
            settings.snapSize = Clamp(settings.snapSize, 2, 300);
            settings.overallOpacity = Math.Max(0.05d, Math.Min(1d, settings.overallOpacity));
        }

        public static void NormalizeFence(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            fenceInfo.Name = string.IsNullOrWhiteSpace(fenceInfo.Name)
                ? "Fence"
                : fenceInfo.Name.Trim();
            fenceInfo.Width = Math.Max(MinimumFenceWidth, fenceInfo.Width);
            fenceInfo.Height = Math.Max(MinimumFenceHeight, fenceInfo.Height);
            fenceInfo.TitleHeight = Clamp(
                fenceInfo.TitleHeight <= 0 ? Properties.Settings.Default.title_size : fenceInfo.TitleHeight,
                MinimumTitleHeight,
                MaximumTitleHeight);
            fenceInfo.Files = NormalizePaths(fenceInfo.Files);
            fenceInfo.WatchedExtensions = NormalizeExtensions(fenceInfo.WatchedExtensions);
            if (!Enum.IsDefined(typeof(FenceSortMode), fenceInfo.SortMode))
                fenceInfo.SortMode = FenceSortMode.Custom;
            if (fenceInfo.SortMode == FenceSortMode.Custom)
                fenceInfo.SortDescending = false;
            if (!Enum.IsDefined(typeof(FenceDisplayMode), fenceInfo.DisplayMode))
                fenceInfo.DisplayMode = FenceDisplayMode.Icons;

            if (!string.IsNullOrWhiteSpace(fenceInfo.CustomFolderPath))
                fenceInfo.CustomFolderPath = fenceInfo.CustomFolderPath.Trim();
        }

        private static List<string> NormalizePaths(IEnumerable<string> paths)
        {
            if (paths == null)
                return new List<string>();

            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> NormalizeExtensions(IEnumerable<string> extensions)
        {
            if (extensions == null)
                return new List<string>();

            return extensions
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.Trim().TrimStart('*'))
                .Select(extension => extension.StartsWith(".", StringComparison.Ordinal)
                    ? extension
                    : "." + extension)
                .Where(extension => extension.Length > 1
                    && extension.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
