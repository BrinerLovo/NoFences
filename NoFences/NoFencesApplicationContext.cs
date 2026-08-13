using NoFences.Model;
using NoFences.Routing;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NoFences
{
    internal sealed class NoFencesApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly ToolStripMenuItem fencesMenu;
        private bool exiting;

        public NoFencesApplicationContext()
        {
            RoutingRuleManager.Instance.Start();
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show all fences", null, (sender, args) => FenceManager.Instance.SetAllVisible(true));
            trayMenu.Items.Add("Hide all fences", null, (sender, args) => FenceManager.Instance.SetAllVisible(false));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Lock all fences", null, (sender, args) => FenceManager.Instance.SetAllLocked(true));
            trayMenu.Items.Add("Unlock all fences", null, (sender, args) => FenceManager.Instance.SetAllLocked(false));
            trayMenu.Items.Add("Sync all fences", null, (sender, args) => FenceManager.Instance.SyncAll());
            fencesMenu = new ToolStripMenuItem("Fences");
            trayMenu.Items.Add(fencesMenu);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("New fence", null, (sender, args) => FenceManager.Instance.CreateFence("New fence"));
            trayMenu.Items.Add("Settings", null, Settings_Click);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit NoFences", null, Exit_Click);
            trayMenu.Opening += (sender, args) => RebuildFenceMenu();

            trayIcon = new NotifyIcon
            {
                ContextMenuStrip = trayMenu,
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
                Text = "NoFences",
                Visible = true
            };
            trayIcon.DoubleClick += (sender, args) => FenceManager.Instance.SetAllVisible(true);
        }

        private void RebuildFenceMenu()
        {
            fencesMenu.DropDownItems.Clear();
            FenceWindow[] fences = FenceManager.Instance.Fences.ToArray();
            if (fences.Length == 0)
            {
                fencesMenu.DropDownItems.Add(new ToolStripMenuItem("No fences") { Enabled = false });
                return;
            }

            foreach (FenceWindow fence in fences.OrderBy(window => window.FenceName, StringComparer.CurrentCultureIgnoreCase))
            {
                var fenceItem = new ToolStripMenuItem(fence.FenceName);
                var visibleItem = new ToolStripMenuItem("Visible") { Checked = fence.IsFenceVisible, CheckOnClick = true };
                visibleItem.Click += (sender, args) => fence.SetFenceVisible(visibleItem.Checked);
                var lockedItem = new ToolStripMenuItem("Locked") { Checked = fence.IsFenceLocked, CheckOnClick = true };
                lockedItem.Click += (sender, args) => fence.SetFenceLocked(lockedItem.Checked);
                fenceItem.DropDownItems.Add(visibleItem);
                fenceItem.DropDownItems.Add(lockedItem);
                fenceItem.DropDownItems.Add("Sync", null, (sender, args) => fence.SyncFromTray());
                fencesMenu.DropDownItems.Add(fenceItem);
            }
        }

        private static void Settings_Click(object sender, EventArgs e)
        {
            using (var settings = new SettingsWindow())
            {
                settings.OnSettingsChanged += FenceManager.Instance.RefreshAllSettings;
                settings.ShowDialog();
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            if (exiting)
                return;
            exiting = true;
            trayIcon.Visible = false;
            FenceManager.Instance.CloseAll();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trayIcon.Dispose();
                trayMenu.Dispose();
                RoutingRuleManager.Instance.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
