using NoFences.Model;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace NoFences.Layout
{
    internal sealed class FenceLayoutItem
    {
        public FenceLayoutItem(string path, Rectangle bounds)
        {
            Path = path;
            Bounds = bounds;
        }

        public string Path { get; }
        public Rectangle Bounds { get; }
    }

    internal sealed class FenceLayoutSnapshot
    {
        public FenceLayoutSnapshot(
            IReadOnlyList<string> orderedPaths,
            IReadOnlyList<FenceLayoutItem> items,
            FenceDisplayMode displayMode,
            int scrollHeight,
            int totalHeight)
        {
            OrderedPaths = orderedPaths;
            Items = items;
            DisplayMode = displayMode;
            ScrollHeight = scrollHeight;
            TotalHeight = totalHeight;
        }

        public IReadOnlyList<string> OrderedPaths { get; }
        public IReadOnlyList<FenceLayoutItem> Items { get; }
        public FenceDisplayMode DisplayMode { get; }
        public int ScrollHeight { get; }
        public int TotalHeight { get; }

        public string HitTest(Point point)
        {
            for (int index = 0; index < Items.Count; index++)
            {
                if (Items[index].Bounds.Contains(point))
                    return Items[index].Path;
            }
            return null;
        }

        public int GetInsertionIndex(Point point)
        {
            for (int index = 0; index < Items.Count; index++)
            {
                Rectangle bounds = Items[index].Bounds;
                if (DisplayMode != FenceDisplayMode.Icons)
                {
                    if (point.Y < bounds.Top + bounds.Height / 2)
                        return index;
                    continue;
                }

                if (point.Y < bounds.Top + bounds.Height / 2
                    || (point.Y < bounds.Bottom && point.X < bounds.Left + bounds.Width / 2))
                {
                    return index;
                }
            }
            return Items.Count;
        }
    }

    internal sealed class FenceLayout
    {
        public const int ItemWidth = 75;
        public const int IconHeight = 32;
        public const int TextHeight = 35;
        public const int ItemPadding = 15;
        public const int ItemHeight = IconHeight + ItemPadding + TextHeight;
        public const int ListPadding = 8;
        public const int CompactRowHeight = 32;
        public const int DetailsRowHeight = 44;

        private readonly List<string> sourcePaths = new List<string>();
        private FenceLayoutSnapshot cachedSnapshot;
        private FenceSortMode cachedSortMode;
        private bool cachedDescending;
        private int cachedWidth;
        private int cachedHeight;
        private int cachedTitleHeight;
        private int cachedScrollOffset;
        private FenceDisplayMode cachedDisplayMode;

        public FenceLayoutSnapshot CreateSnapshot(
            IReadOnlyList<string> paths,
            FenceSortMode sortMode,
            bool descending,
            FenceDisplayMode displayMode,
            int width,
            int height,
            int titleHeight,
            int scrollOffset)
        {
            if (cachedSnapshot != null
                && cachedSortMode == sortMode
                && cachedDescending == descending
                && cachedDisplayMode == displayMode
                && cachedWidth == width
                && cachedHeight == height
                && cachedTitleHeight == titleHeight
                && cachedScrollOffset == scrollOffset
                && PathsEqual(paths))
            {
                return cachedSnapshot;
            }

            sourcePaths.Clear();
            for (int index = 0; index < paths.Count; index++)
                sourcePaths.Add(paths[index]);

            var renderablePaths = new List<string>(sourcePaths.Count);
            for (int index = 0; index < sourcePaths.Count; index++)
            {
                string path = sourcePaths[index];
                if (File.Exists(path) || Directory.Exists(path))
                    renderablePaths.Add(path);
            }

            List<string> orderedPaths = GetOrderedPaths(renderablePaths, sortMode, descending);
            var items = new List<FenceLayoutItem>(orderedPaths.Count);
            int x = displayMode == FenceDisplayMode.Icons ? ItemPadding : ListPadding;
            int y = x;
            int contentBottom = 0;
            for (int index = 0; index < orderedPaths.Count; index++)
            {
                int itemWidth = displayMode == FenceDisplayMode.Icons
                    ? ItemWidth
                    : Math.Max(1, width - (ListPadding * 2));
                int itemHeight = GetItemHeight(displayMode);
                var bounds = new Rectangle(x, y + titleHeight - scrollOffset, itemWidth, itemHeight);
                items.Add(new FenceLayoutItem(orderedPaths[index], bounds));
                contentBottom = Math.Max(contentBottom, y + itemHeight);

                if (displayMode == FenceDisplayMode.Icons)
                {
                    x += ItemWidth + ItemPadding;
                    if (x + ItemWidth > width)
                    {
                        x = ItemPadding;
                        y += ItemHeight + ItemPadding;
                    }
                }
                else
                {
                    y += itemHeight + 4;
                }
            }

            int viewableHeight = Math.Max(0, height - titleHeight);
            int scrollHeight = Math.Max(0, contentBottom - viewableHeight);
            cachedSnapshot = new FenceLayoutSnapshot(
                orderedPaths,
                items,
                displayMode,
                scrollHeight,
                contentBottom + titleHeight);
            cachedSortMode = sortMode;
            cachedDescending = descending;
            cachedDisplayMode = displayMode;
            cachedWidth = width;
            cachedHeight = height;
            cachedTitleHeight = titleHeight;
            cachedScrollOffset = scrollOffset;
            return cachedSnapshot;
        }

        public void Invalidate()
        {
            cachedSnapshot = null;
        }

        private static int GetItemHeight(FenceDisplayMode displayMode)
        {
            switch (displayMode)
            {
                case FenceDisplayMode.CompactList:
                    return CompactRowHeight;
                case FenceDisplayMode.Details:
                    return DetailsRowHeight;
                default:
                    return ItemHeight;
            }
        }

        internal static List<string> GetOrderedPaths(
            IEnumerable<string> paths,
            FenceSortMode sortMode,
            bool descending)
        {
            var result = new List<string>(paths ?? Enumerable.Empty<string>());
            if (sortMode == FenceSortMode.Custom)
                return result;

            Comparison<string> comparison;
            switch (sortMode)
            {
                case FenceSortMode.Type:
                    comparison = CompareByType;
                    break;
                case FenceSortMode.Date:
                    comparison = CompareByDate;
                    break;
                default:
                    comparison = CompareByName;
                    break;
            }

            result.Sort((left, right) => descending
                ? comparison(right, left)
                : comparison(left, right));
            return result;
        }

        private bool PathsEqual(IReadOnlyList<string> paths)
        {
            if (sourcePaths.Count != paths.Count)
                return false;
            for (int index = 0; index < paths.Count; index++)
            {
                if (!string.Equals(sourcePaths[index], paths[index], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private static int CompareByName(string left, string right)
        {
            return StringComparer.CurrentCultureIgnoreCase.Compare(
                Path.GetFileName(left),
                Path.GetFileName(right));
        }

        private static int CompareByType(string left, string right)
        {
            bool leftFolder = Directory.Exists(left);
            bool rightFolder = Directory.Exists(right);
            if (leftFolder != rightFolder)
                return leftFolder ? -1 : 1;

            int typeComparison = StringComparer.CurrentCultureIgnoreCase.Compare(
                leftFolder ? string.Empty : Path.GetExtension(left),
                rightFolder ? string.Empty : Path.GetExtension(right));
            return typeComparison != 0 ? typeComparison : CompareByName(left, right);
        }

        private static int CompareByDate(string left, string right)
        {
            int dateComparison = GetLastWriteTimeUtc(left).CompareTo(GetLastWriteTimeUtc(right));
            return dateComparison != 0 ? dateComparison : CompareByName(left, right);
        }

        private static DateTime GetLastWriteTimeUtc(string path)
        {
            try
            {
                if (File.Exists(path))
                    return File.GetLastWriteTimeUtc(path);
                if (Directory.Exists(path))
                    return Directory.GetLastWriteTimeUtc(path);
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException
                || ex is NotSupportedException)
            {
                AppLogger.Error($"Unable to read the item date for sorting: {path}", ex);
            }
            return DateTime.MinValue;
        }
    }
}
