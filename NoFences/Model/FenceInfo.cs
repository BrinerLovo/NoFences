using System;
using System.Collections.Generic;

namespace NoFences.Model
{
    public class FenceInfo
    {
        /* 
         * DO NOT RENAME PROPERTIES. Used for XML serialization.
         */

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window height.
        /// </summary>
        public int Height { get; set; }

        public bool Locked { get; set; }

        public bool Folded { get; set; }

        public bool CanMinify { get; set; }

        /// <summary>
        /// Gets or sets whether this fence inherits the global auto-minify setting.
        /// </summary>
        public bool UseGlobalAutoMinify { get; set; } = true;

        /// <summary>
        /// Gets or sets the logical window title height.
        /// </summary>
        public int TitleHeight { get; set; } = 35;

        /// <summary>
        /// Gets or sets whether this fence inherits the global title height.
        /// </summary>
        public bool UseGlobalTitleHeight { get; set; } = true;

        /// <summary>
        /// Gets or sets whether changes in the linked folder are reflected automatically.
        /// </summary>
        public bool AutoSyncFolder { get; set; } = true;

        /// <summary>
        /// Gets or sets how items are ordered. Manual drag reordering is available only for Custom.
        /// </summary>
        public FenceSortMode SortMode { get; set; } = FenceSortMode.Custom;

        /// <summary>
        /// Gets or sets whether automatic sorting uses descending order.
        /// </summary>
        public bool SortDescending { get; set; }

        /// <summary>
        /// Gets or sets how fence items are presented.
        /// </summary>
        public FenceDisplayMode DisplayMode { get; set; } = FenceDisplayMode.Icons;

        /// <summary>
        /// Gets or sets whether the fence is hidden while the application remains active.
        /// </summary>
        public bool Hidden { get; set; }

        public List<string> Files { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of file extensions to watch for automatic addition.
        /// When files with these extensions are created on the desktop, they will be automatically 
        /// moved to this fence's folder. Leave empty to disable watching.
        /// </summary>
        public List<string> WatchedExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the custom folder path for this fence. 
        /// If null or empty, the default path will be used (LocalApplicationData/NoFences/{Id}).
        /// </summary>
        public string CustomFolderPath { get; set; }

        public FenceInfo()
        {

        }

        public FenceInfo(Guid id)
        {
            Id = id;
        }
    }
}
