using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;
using NoFences.Routing;
using NoFences.Transfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinFormsControl = System.Windows.Forms.Control;

namespace NoFences
{
    public partial class SettingsWindow : Form
    {
        private static readonly Color WindowBackground = Color.FromArgb(31, 31, 31);
        private static readonly Color NavigationBackground = Color.FromArgb(36, 36, 36);
        private static readonly Color SurfaceBackground = Color.FromArgb(42, 42, 42);
        private static readonly Color HoverBackground = Color.FromArgb(54, 54, 54);
        private static readonly Color BorderColor = Color.FromArgb(74, 74, 74);
        private static readonly Color PrimaryText = Color.FromArgb(242, 242, 242);
        private static readonly Color SecondaryText = Color.FromArgb(175, 175, 175);
        private static readonly Color AccentColor = Color.FromArgb(80, 120, 170);

        private readonly Timer settingsSaveTimer;
        private readonly Timer settingsApplyTimer;
        private readonly Dictionary<int, WinFormsControl> pages = new Dictionary<int, WinFormsControl>();
        private readonly ToolTip toolTip = new ToolTip();

        private ListBox navigationList;
        private Panel contentHost;
        private Label contentTitle;
        private Label contentDescription;
        private Button resetButton;
        private Button closeButton;

        private CheckBox startupCheckBox;
        private CheckBox hideDesktopCheckBox;
        private CheckBox showContainerCheckBox;
        private CheckBox confirmDeletionCheckBox;
        private CheckBox enableWatchersCheckBox;
        private CheckBox snappingCheckBox;
        private CheckBox autoMinifyCheckBox;
        private CheckBox reduceAnimationsCheckBox;
        private NumericUpDown snapSizeInput;
        private NumericUpDown titleHeightInput;
        private TrackBar overallOpacitySlider;
        private TrackBar windowOpacitySlider;
        private TrackBar headerOpacitySlider;
        private Label overallOpacityValue;
        private Label windowOpacityValue;
        private Label headerOpacityValue;
        private Button headerColorButton;
        private Button windowColorButton;
        private bool initialized;

        public Action OnSettingsChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            BuildInterface();

            settingsSaveTimer = new Timer { Interval = 300 };
            settingsSaveTimer.Tick += SettingsSaveTimer_Tick;
            settingsApplyTimer = new Timer { Interval = 50 };
            settingsApplyTimer.Tick += SettingsApplyTimer_Tick;

            SettingsValidator.NormalizeGlobalSettings();
            LoadControlValues();
            BindEvents();
            initialized = true;
            navigationList.SelectedIndex = 0;
        }

        private void BuildInterface()
        {
            BackColor = WindowBackground;
            ForeColor = PrimaryText;

            var navigationPanel = new Panel
            {
                BackColor = NavigationBackground,
                Dock = DockStyle.Left,
                Width = 176,
                Padding = new Padding(16, 20, 12, 16)
            };
            var appTitle = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 16F),
                Height = 34,
                Text = "NoFences"
            };
            var appSubtitle = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                ForeColor = SecondaryText,
                Height = 42,
                Text = "Desktop organization"
            };
            navigationList = new ListBox
            {
                BackColor = NavigationBackground,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ForeColor = PrimaryText,
                ItemHeight = 38,
                IntegralHeight = false
            };
            navigationList.Items.AddRange(new object[] { "General", "Behavior", "Appearance", "About" });
            navigationList.DrawItem += NavigationList_DrawItem;
            navigationList.SelectedIndexChanged += NavigationList_SelectedIndexChanged;
            navigationPanel.Controls.Add(navigationList);
            navigationPanel.Controls.Add(appSubtitle);
            navigationPanel.Controls.Add(appTitle);

            var footer = new Panel
            {
                BackColor = NavigationBackground,
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(20, 14, 20, 14)
            };
            resetButton = CreateButton("Restore defaults", 122);
            resetButton.Dock = DockStyle.Left;
            resetButton.Click += ResetButton_Click;
            closeButton = CreateButton("Close", 92);
            closeButton.Dock = DockStyle.Right;
            closeButton.Click += (sender, args) => Close();
            footer.Controls.Add(resetButton);
            footer.Controls.Add(closeButton);

            var header = new Panel
            {
                BackColor = WindowBackground,
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(28, 20, 24, 8)
            };
            contentTitle = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI Semibold", 17F),
                Height = 34
            };
            contentDescription = new Label
            {
                Dock = DockStyle.Top,
                ForeColor = SecondaryText,
                Height = 28
            };
            header.Controls.Add(contentDescription);
            header.Controls.Add(contentTitle);

            contentHost = new Panel
            {
                BackColor = WindowBackground,
                Dock = DockStyle.Fill
            };

            Controls.Add(contentHost);
            Controls.Add(header);
            Controls.Add(footer);
            Controls.Add(navigationPanel);

            pages.Add(0, BuildGeneralPage());
            pages.Add(1, BuildBehaviorPage());
            pages.Add(2, BuildAppearancePage());
            pages.Add(3, BuildAboutPage());
        }

        private WinFormsControl BuildGeneralPage()
        {
            startupCheckBox = CreateCheckBox(
                "Start NoFences with Windows",
                "Launch NoFences automatically after you sign in.");
            hideDesktopCheckBox = CreateCheckBox(
                "Hide standard desktop icons",
                "Hide Windows desktop icons while keeping fence windows visible.");
            showContainerCheckBox = CreateCheckBox(
                "Show the NoFences desktop container",
                "Make the compatibility container folder visible on the desktop.");
            confirmDeletionCheckBox = CreateCheckBox(
                "Confirm before removing a fence",
                "Fence removal keeps linked files, but confirmation prevents accidental removal.");

            Button openDataButton = CreateButton("Open data folder", 132);
            openDataButton.Click += (sender, args) => OpenFolder(FenceManager.Instance.DataDirectoryPath);
            Button openLogsButton = CreateButton("Open logs", 100);
            openLogsButton.Click += (sender, args) => OpenFolder(AppLogger.DirectoryPath);
            var actionRow = CreateActionRow(openDataButton, openLogsButton);

            Button routingButton = CreateButton("Routing rules", 120);
            routingButton.Click += (sender, args) =>
            {
                using (var window = new RoutingRulesWindow())
                    window.ShowDialog(this);
            };
            Button exportButton = CreateButton("Export layout", 116);
            exportButton.Click += ExportLayout_Click;
            Button importButton = CreateButton("Import layout", 116);
            importButton.Click += ImportLayout_Click;
            var organizationRow = CreateActionRow(routingButton, exportButton, importButton);

            return CreatePage(
                CreateSection("Startup and desktop", "Control how NoFences integrates with Windows.", startupCheckBox, hideDesktopCheckBox, showContainerCheckBox),
                CreateSection("Organization", "Automate file routing or move your complete setup between installations.", organizationRow),
                CreateSection("Safety and diagnostics", "Keep destructive actions explicit and make troubleshooting accessible.", confirmDeletionCheckBox, actionRow));
        }

        private void ExportLayout_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "nofences",
                Filter = "NoFences layout (*.nofences)|*.nofences|XML files (*.xml)|*.xml",
                FileName = "NoFences layout.nofences",
                Title = "Export NoFences layout"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    LayoutTransferService.Export(dialog.FileName);
                    MessageBox.Show(this, "The fence layout, settings, and routing rules were exported.", "Export layout", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Unable to export the NoFences layout.", ex);
                    MessageBox.Show(this, ex.Message, "Export layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ImportLayout_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "NoFences layout (*.nofences;*.xml)|*.nofences;*.xml",
                Title = "Import NoFences layout"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    NoFencesLayoutPackage package = LayoutTransferService.Read(dialog.FileName);
                    if (MessageBox.Show(
                            this,
                            $"Replace the current layout with {package.Fences.Count} imported fence(s)?\n\nLinked files and folders are never deleted.",
                            "Import layout",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    LayoutTransferService.Import(package);
                    LoadControlValues();
                    MessageBox.Show(this, "The layout and settings were imported successfully.", "Import layout", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Unable to import the NoFences layout.", ex);
                    MessageBox.Show(this, ex.Message, "Import layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private WinFormsControl BuildBehaviorPage()
        {
            enableWatchersCheckBox = CreateCheckBox(
                "Monitor linked folders and watched extensions",
                "Disable this to stop all automatic filesystem monitoring.");
            snappingCheckBox = CreateCheckBox(
                "Snap fences while moving",
                "Align fence windows to a consistent desktop grid.");
            autoMinifyCheckBox = CreateCheckBox(
                "Auto-minify fences by default",
                "Used by fences that inherit the global auto-minify setting.");
            reduceAnimationsCheckBox = CreateCheckBox(
                "Reduce animations",
                "Use immediate resizing and opacity changes for accessibility and lower overhead.");
            snapSizeInput = CreateNumberInput(2, 300);

            return CreatePage(
                CreateSection("File monitoring", "Automatic synchronization can be overridden per fence.", enableWatchersCheckBox),
                CreateSection("Window behavior", "Set consistent movement and interaction defaults.", snappingCheckBox, CreateLabeledRow("Snap distance", snapSizeInput, "pixels"), autoMinifyCheckBox, reduceAnimationsCheckBox));
        }

        private WinFormsControl BuildAppearancePage()
        {
            titleHeightInput = CreateNumberInput(SettingsValidator.MinimumTitleHeight, SettingsValidator.MaximumTitleHeight);
            overallOpacitySlider = CreateSlider(5, 100);
            windowOpacitySlider = CreateSlider(0, 255);
            headerOpacitySlider = CreateSlider(0, 255);
            overallOpacityValue = CreateValueLabel();
            windowOpacityValue = CreateValueLabel();
            headerOpacityValue = CreateValueLabel();
            headerColorButton = CreateColorButton("Header color");
            windowColorButton = CreateColorButton("Fence color");

            return CreatePage(
                CreateSection("Typography", "Applied to fences that inherit global appearance.", CreateLabeledRow("Title height", titleHeightInput, "pixels")),
                CreateSection(
                    "Transparency",
                    "Keep content readable while controlling how much of the desktop remains visible.",
                    CreateSliderRow("Overall opacity", overallOpacitySlider, overallOpacityValue),
                    CreateSliderRow("Fence fill", windowOpacitySlider, windowOpacityValue),
                    CreateSliderRow("Header fill", headerOpacitySlider, headerOpacityValue)),
                CreateSection("Colors", "Choose restrained solid colors for fence surfaces.", CreateActionRow(headerColorButton, windowColorButton)));
        }

        private WinFormsControl BuildAboutPage()
        {
            var version = new Label
            {
                AutoSize = false,
                ForeColor = PrimaryText,
                Height = 28,
                Text = "Version " + Program.GetAppVersion()
            };
            var description = new Label
            {
                AutoSize = false,
                ForeColor = SecondaryText,
                Height = 64,
                Text = "A lightweight, open desktop organization tool. Configuration and linked folder content remain local to this computer."
            };
            return CreatePage(CreateSection("About NoFences", "Desktop organization without visual clutter.", version, description));
        }

        private FlowLayoutPanel CreatePage(params WinFormsControl[] sections)
        {
            var page = new FlowLayoutPanel
            {
                AutoScroll = true,
                BackColor = WindowBackground,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(24, 4, 18, 20),
                WrapContents = false
            };
            page.Controls.AddRange(sections);
            return page;
        }

        private Panel CreateSection(string title, string description, params WinFormsControl[] controls)
        {
            int bodyHeight = 0;
            for (int index = 0; index < controls.Length; index++)
                bodyHeight += controls[index].Height + 8;

            var section = new Panel
            {
                BackColor = SurfaceBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16),
                Size = new Size(510, 76 + bodyHeight)
            };
            var titleLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI Semibold", 10F),
                Location = new Point(16, 14),
                Size = new Size(470, 22),
                Text = title
            };
            var descriptionLabel = new Label
            {
                AutoSize = false,
                ForeColor = SecondaryText,
                Location = new Point(16, 37),
                Size = new Size(470, 28),
                Text = description
            };
            section.Controls.Add(titleLabel);
            section.Controls.Add(descriptionLabel);

            int top = 68;
            foreach (WinFormsControl control in controls)
            {
                control.Location = new Point(16, top);
                control.Width = 470;
                section.Controls.Add(control);
                top += control.Height + 8;
            }

            return section;
        }

        private CheckBox CreateCheckBox(string text, string description)
        {
            var checkBox = new CheckBox
            {
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                ForeColor = PrimaryText,
                Height = 28,
                Text = text
            };
            toolTip.SetToolTip(checkBox, description);
            checkBox.AccessibleDescription = description;
            return checkBox;
        }

        private static NumericUpDown CreateNumberInput(int minimum, int maximum)
        {
            return new NumericUpDown
            {
                BackColor = NavigationBackground,
                BorderStyle = BorderStyle.FixedSingle,
                ForeColor = PrimaryText,
                Maximum = maximum,
                Minimum = minimum,
                Size = new Size(90, 28)
            };
        }

        private static TrackBar CreateSlider(int minimum, int maximum)
        {
            return new TrackBar
            {
                AutoSize = false,
                Height = 30,
                Maximum = maximum,
                Minimum = minimum,
                TickStyle = TickStyle.None
            };
        }

        private static Label CreateValueLabel()
        {
            return new Label
            {
                AutoSize = false,
                ForeColor = SecondaryText,
                TextAlign = ContentAlignment.MiddleRight,
                Width = 56
            };
        }

        private Button CreateButton(string text, int width)
        {
            var button = new Button
            {
                BackColor = SurfaceBackground,
                FlatStyle = FlatStyle.Flat,
                ForeColor = PrimaryText,
                Height = 34,
                Text = text,
                Width = width
            };
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = HoverBackground;
            return button;
        }

        private Button CreateColorButton(string text)
        {
            Button button = CreateButton(text, 150);
            button.TextAlign = ContentAlignment.MiddleRight;
            return button;
        }

        private static Panel CreateActionRow(params WinFormsControl[] controls)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 38,
                WrapContents = false
            };
            row.Controls.AddRange(controls);
            return row;
        }

        private static TableLayoutPanel CreateLabeledRow(string label, WinFormsControl control, string suffix)
        {
            var row = new TableLayoutPanel { ColumnCount = 3, Height = 32, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            row.Controls.Add(control, 1, 0);
            row.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = SecondaryText, Text = suffix, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            return row;
        }

        private static TableLayoutPanel CreateSliderRow(string label, TrackBar slider, Label value)
        {
            var row = new TableLayoutPanel { ColumnCount = 3, Height = 34, RowCount = 1 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            row.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            slider.Dock = DockStyle.Fill;
            value.Dock = DockStyle.Fill;
            row.Controls.Add(slider, 1, 0);
            row.Controls.Add(value, 2, 0);
            return row;
        }

        private void BindEvents()
        {
            startupCheckBox.CheckedChanged += StartupCheckBox_CheckedChanged;
            hideDesktopCheckBox.CheckedChanged += HideDesktopCheckBox_CheckedChanged;
            showContainerCheckBox.CheckedChanged += ShowContainerCheckBox_CheckedChanged;
            confirmDeletionCheckBox.CheckedChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.confirmFenceDeletion = confirmDeletionCheckBox.Checked, false);
            enableWatchersCheckBox.CheckedChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.enableFileWatchers = enableWatchersCheckBox.Checked, true);
            snappingCheckBox.CheckedChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.snapping = snappingCheckBox.Checked, true);
            autoMinifyCheckBox.CheckedChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.autoMinify = autoMinifyCheckBox.Checked, true);
            reduceAnimationsCheckBox.CheckedChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.reduceAnimations = reduceAnimationsCheckBox.Checked, true);
            snapSizeInput.ValueChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.snapSize = (int)snapSizeInput.Value, true);
            titleHeightInput.ValueChanged += (sender, args) => UpdateSetting(() => Properties.Settings.Default.title_size = (int)titleHeightInput.Value, true);
            overallOpacitySlider.ValueChanged += (sender, args) =>
            {
                overallOpacityValue.Text = overallOpacitySlider.Value + "%";
                UpdateSetting(() => Properties.Settings.Default.overallOpacity = overallOpacitySlider.Value / 100d, true);
            };
            windowOpacitySlider.ValueChanged += (sender, args) =>
            {
                windowOpacityValue.Text = ToPercent(windowOpacitySlider.Value) + "%";
                UpdateSetting(() => Properties.Settings.Default.opacity = windowOpacitySlider.Value, true);
            };
            headerOpacitySlider.ValueChanged += (sender, args) =>
            {
                headerOpacityValue.Text = ToPercent(headerOpacitySlider.Value) + "%";
                UpdateSetting(() => Properties.Settings.Default.headerAlpha = headerOpacitySlider.Value, true);
            };
            headerColorButton.Click += (sender, args) => SelectColor(true);
            windowColorButton.Click += (sender, args) => SelectColor(false);
        }

        private void LoadControlValues()
        {
            var settings = Properties.Settings.Default;
            startupCheckBox.Checked = StartupManager.IsStartupEnabled();
            hideDesktopCheckBox.Checked = settings.hide_desktop_icons;
            showContainerCheckBox.Checked = settings.show_container_folder;
            confirmDeletionCheckBox.Checked = settings.confirmFenceDeletion;
            enableWatchersCheckBox.Checked = settings.enableFileWatchers;
            snappingCheckBox.Checked = settings.snapping;
            autoMinifyCheckBox.Checked = settings.autoMinify;
            reduceAnimationsCheckBox.Checked = settings.reduceAnimations;
            snapSizeInput.Value = settings.snapSize;
            titleHeightInput.Value = settings.title_size;
            overallOpacitySlider.Value = (int)Math.Round(settings.overallOpacity * 100d);
            windowOpacitySlider.Value = settings.opacity;
            headerOpacitySlider.Value = settings.headerAlpha;
            headerColorButton.BackColor = settings.headerColor;
            windowColorButton.BackColor = settings.windowColor;
            overallOpacityValue.Text = overallOpacitySlider.Value + "%";
            windowOpacityValue.Text = ToPercent(windowOpacitySlider.Value) + "%";
            headerOpacityValue.Text = ToPercent(headerOpacitySlider.Value) + "%";
            snapSizeInput.Enabled = snappingCheckBox.Checked;
        }

        private void UpdateSetting(Action update, bool applyLive)
        {
            if (!initialized)
                return;

            update();
            snapSizeInput.Enabled = snappingCheckBox.Checked;
            ScheduleSettingsSave();
            if (applyLive)
                ScheduleSettingsApply();
        }

        private void StartupCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized)
                return;

            if (StartupManager.TrySetStartup(Application.ExecutablePath, startupCheckBox.Checked, out string error))
                return;

            initialized = false;
            startupCheckBox.Checked = !startupCheckBox.Checked;
            initialized = true;
            MessageBox.Show(this, error, "Startup setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void HideDesktopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized)
                return;

            if (!DesktopUtil.TrySetDesktopIconsVisible(!hideDesktopCheckBox.Checked, out string error))
            {
                initialized = false;
                hideDesktopCheckBox.Checked = !hideDesktopCheckBox.Checked;
                initialized = true;
                MessageBox.Show(this, error, "Desktop icons", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateSetting(() => Properties.Settings.Default.hide_desktop_icons = hideDesktopCheckBox.Checked, false);
        }

        private void ShowContainerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized)
                return;

            try
            {
                Directory.CreateDirectory(FenceWindow.HiddenDesktopPath);
                File.SetAttributes(
                    FenceWindow.HiddenDesktopPath,
                    showContainerCheckBox.Checked ? FileAttributes.Normal : FileAttributes.Hidden);
                UpdateSetting(() => Properties.Settings.Default.show_container_folder = showContainerCheckBox.Checked, false);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unable to update the container folder visibility.", ex);
                MessageBox.Show(this, ex.Message, "Container folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectColor(bool header)
        {
            using (var dialog = new ColorDialog
            {
                Color = header ? Properties.Settings.Default.headerColor : Properties.Settings.Default.windowColor,
                FullOpen = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                if (header)
                {
                    headerColorButton.BackColor = dialog.Color;
                    Properties.Settings.Default.headerColor = dialog.Color;
                }
                else
                {
                    windowColorButton.BackColor = dialog.Color;
                    Properties.Settings.Default.windowColor = dialog.Color;
                }
                ScheduleSettingsSave();
                ScheduleSettingsApply();
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    this,
                    "Restore all global settings to their defaults? Per-fence settings are not changed.",
                    "Restore defaults",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            initialized = false;
            Properties.Settings.Default.Reset();
            SettingsValidator.NormalizeGlobalSettings();
            LoadControlValues();
            initialized = true;
            DesktopUtil.TrySetDesktopIconsVisible(!Properties.Settings.Default.hide_desktop_icons, out _);
            ScheduleSettingsSave();
            ScheduleSettingsApply();
        }

        private void NavigationList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!pages.TryGetValue(navigationList.SelectedIndex, out WinFormsControl page))
                return;

            string[] titles = { "General", "Behavior", "Appearance", "About" };
            string[] descriptions =
            {
                "Startup, desktop integration, and safety.",
                "File monitoring and fence interaction defaults.",
                "Global colors, sizing, and transparency.",
                "Version and application information."
            };
            contentTitle.Text = titles[navigationList.SelectedIndex];
            contentDescription.Text = descriptions[navigationList.SelectedIndex];
            contentHost.Controls.Clear();
            contentHost.Controls.Add(page);
            page.Dock = DockStyle.Fill;
        }

        private void NavigationList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var background = new SolidBrush(selected ? SurfaceBackground : NavigationBackground))
                e.Graphics.FillRectangle(background, e.Bounds);
            if (selected)
            {
                using (var accent = new SolidBrush(AccentColor))
                    e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top + 7, 3, e.Bounds.Height - 14);
            }
            TextRenderer.DrawText(
                e.Graphics,
                navigationList.Items[e.Index].ToString(),
                Font,
                new Rectangle(e.Bounds.Left + 14, e.Bounds.Top, e.Bounds.Width - 16, e.Bounds.Height),
                PrimaryText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private static int ToPercent(int alpha)
        {
            return (int)Math.Round(alpha / 255d * 100d);
        }

        private static void OpenFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Unable to open folder '{path}'.", ex);
                MessageBox.Show(ex.Message, "Open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScheduleSettingsSave()
        {
            settingsSaveTimer.Stop();
            settingsSaveTimer.Start();
        }

        private void ScheduleSettingsApply()
        {
            settingsApplyTimer.Stop();
            settingsApplyTimer.Start();
        }

        private void SettingsApplyTimer_Tick(object sender, EventArgs e)
        {
            settingsApplyTimer.Stop();
            OnSettingsChanged?.Invoke();
        }

        private void SettingsSaveTimer_Tick(object sender, EventArgs e)
        {
            FlushSettingsSave();
        }

        private void FlushSettingsSave()
        {
            if (!settingsSaveTimer.Enabled)
                return;

            settingsSaveTimer.Stop();
            try
            {
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unable to save global settings.", ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (settingsApplyTimer.Enabled)
            {
                settingsApplyTimer.Stop();
                OnSettingsChanged?.Invoke();
            }
            FlushSettingsSave();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            settingsSaveTimer.Tick -= SettingsSaveTimer_Tick;
            settingsSaveTimer.Dispose();
            settingsApplyTimer.Tick -= SettingsApplyTimer_Tick;
            settingsApplyTimer.Dispose();
            toolTip.Dispose();
            base.OnFormClosed(e);
        }
    }
}
