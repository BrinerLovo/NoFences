using NoFences.Model;
using NoFences.Routing;
using System;
using System.Collections.Generic;

namespace NoFences.Transfer
{
    public sealed class NoFencesLayoutPackage
    {
        public int FormatVersion { get; set; } = 1;
        public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;
        public GlobalSettingsSnapshot GlobalSettings { get; set; } = new GlobalSettingsSnapshot();
        public List<FenceInfo> Fences { get; set; } = new List<FenceInfo>();
        public List<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
    }

    public sealed class GlobalSettingsSnapshot
    {
        public bool HideDesktopIcons { get; set; }
        public int WindowOpacity { get; set; }
        public bool ShowContainerFolder { get; set; }
        public int TitleSize { get; set; }
        public bool Snapping { get; set; }
        public bool AutoMinify { get; set; }
        public int SnapSize { get; set; }
        public int HeaderColorArgb { get; set; }
        public int HeaderAlpha { get; set; }
        public int WindowColorArgb { get; set; }
        public double OverallOpacity { get; set; }
        public bool ConfirmFenceDeletion { get; set; }
        public bool EnableFileWatchers { get; set; }
        public bool ReduceAnimations { get; set; }
    }
}
