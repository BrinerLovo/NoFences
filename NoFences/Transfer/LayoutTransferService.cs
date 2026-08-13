using NoFences.Model;
using NoFences.Routing;
using NoFences.Util;
using NoFences.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace NoFences.Transfer
{
    internal static class LayoutTransferService
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(NoFencesLayoutPackage));

        public static void Export(string filePath)
        {
            var package = new NoFencesLayoutPackage
            {
                GlobalSettings = CaptureGlobalSettings(),
                Fences = new List<FenceInfo>(FenceManager.Instance.GetSavedFenceInfos()),
                RoutingRules = new List<RoutingRule>(RoutingRuleManager.Instance.Rules)
            };
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                Serializer.Serialize(stream, package);
        }

        public static NoFencesLayoutPackage Read(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var package = Serializer.Deserialize(stream) as NoFencesLayoutPackage;
                if (package == null || package.FormatVersion != 1)
                    throw new InvalidDataException("This layout package version is not supported.");
                package.Fences = package.Fences ?? new List<FenceInfo>();
                package.RoutingRules = package.RoutingRules ?? new List<RoutingRule>();
                return package;
            }
        }

        public static void Import(NoFencesLayoutPackage package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            ApplyGlobalSettings(package.GlobalSettings ?? new GlobalSettingsSnapshot());
            FenceManager.Instance.ReplaceFences(package.Fences);
            RoutingRuleManager.Instance.ReplaceRules(package.RoutingRules);
            DesktopUtil.TrySetDesktopIconsVisible(!Properties.Settings.Default.hide_desktop_icons, out _);
            FenceManager.Instance.RefreshAllSettings();
        }

        private static GlobalSettingsSnapshot CaptureGlobalSettings()
        {
            var settings = Properties.Settings.Default;
            return new GlobalSettingsSnapshot
            {
                HideDesktopIcons = settings.hide_desktop_icons,
                WindowOpacity = settings.opacity,
                ShowContainerFolder = settings.show_container_folder,
                TitleSize = settings.title_size,
                Snapping = settings.snapping,
                AutoMinify = settings.autoMinify,
                SnapSize = settings.snapSize,
                HeaderColorArgb = settings.headerColor.ToArgb(),
                HeaderAlpha = settings.headerAlpha,
                WindowColorArgb = settings.windowColor.ToArgb(),
                OverallOpacity = settings.overallOpacity,
                ConfirmFenceDeletion = settings.confirmFenceDeletion,
                EnableFileWatchers = settings.enableFileWatchers,
                ReduceAnimations = settings.reduceAnimations
            };
        }

        private static void ApplyGlobalSettings(GlobalSettingsSnapshot snapshot)
        {
            var settings = Properties.Settings.Default;
            settings.hide_desktop_icons = snapshot.HideDesktopIcons;
            settings.opacity = snapshot.WindowOpacity;
            settings.show_container_folder = snapshot.ShowContainerFolder;
            settings.title_size = snapshot.TitleSize;
            settings.snapping = snapshot.Snapping;
            settings.autoMinify = snapshot.AutoMinify;
            settings.snapSize = snapshot.SnapSize;
            settings.headerColor = Color.FromArgb(snapshot.HeaderColorArgb);
            settings.headerAlpha = snapshot.HeaderAlpha;
            settings.windowColor = Color.FromArgb(snapshot.WindowColorArgb);
            settings.overallOpacity = snapshot.OverallOpacity;
            settings.confirmFenceDeletion = snapshot.ConfirmFenceDeletion;
            settings.enableFileWatchers = snapshot.EnableFileWatchers;
            settings.reduceAnimations = snapshot.ReduceAnimations;
            SettingsValidator.NormalizeGlobalSettings();
            settings.Save();
        }
    }
}
