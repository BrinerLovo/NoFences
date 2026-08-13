using NoFences.Util;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NoFences
{
    public partial class SettingsWindow : Form
    {
        public Action OnSettingsChanged;
        private readonly bool initialized = false;
        private readonly Timer settingsSaveTimer;
        private readonly Timer settingsApplyTimer;

        public SettingsWindow()
        {
            InitializeComponent();
            settingsSaveTimer = new Timer { Interval = 300 };
            settingsSaveTimer.Tick += SettingsSaveTimer_Tick;
            settingsApplyTimer = new Timer { Interval = 50 };
            settingsApplyTimer.Tick += SettingsApplyTimer_Tick;
            hideDesktopToggle.Checked = Properties.Settings.Default.hide_desktop_icons;
            showContainerFolToggle.Checked = Properties.Settings.Default.show_container_folder;
            trackBarTitleHeight.Value = Properties.Settings.Default.title_size;
            snappingCheckbox.Checked = Properties.Settings.Default.snapping;
            autoMinifyCheckbox.Checked = Properties.Settings.Default.autoMinify;
            snapSizeSlider.Value = Properties.Settings.Default.snapSize;
            headerColorPreview.BackColor = Color.FromArgb(Properties.Settings.Default.headerAlpha, Properties.Settings.Default.headerColor);
            headerAlphaSlider.Value = Properties.Settings.Default.headerAlpha;
            windowColorPanel.BackColor = Color.FromArgb(Properties.Settings.Default.opacity, Properties.Settings.Default.windowColor);
            opacityBar.Value = Properties.Settings.Default.opacity;
            overallOpacitySlider.Value = MathUtils.FloorToInt((float)Properties.Settings.Default.overallOpacity * 100f);

            UpdateText();

            initialized = true;
        }

        private void hideDesktopToggle_CheckedChanged(object sender, System.EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.hide_desktop_icons = hideDesktopToggle.Checked;
            ScheduleSettingsSave();
            ScheduleSettingsApply();
        }

        private void opacityBar_ValueChanged(object sender, System.EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.opacity = opacityBar.Value;
            ScheduleSettingsSave();
            UpdateText();
            windowColorPanel.BackColor = Color.FromArgb(opacityBar.Value, Properties.Settings.Default.windowColor);
            ScheduleSettingsApply();
        }

        private void showContainerFolToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.show_container_folder = showContainerFolToggle.Checked;
            ScheduleSettingsSave();

            if (showContainerFolToggle.Checked)
            {
                Directory.CreateDirectory(FenceWindow.HiddenDesktopPath);
                File.SetAttributes(FenceWindow.HiddenDesktopPath, FileAttributes.Normal);
            }
            else
            {
                File.SetAttributes(FenceWindow.HiddenDesktopPath, FileAttributes.Hidden);
            }
        }

        private void trackBarTitleHeight_Scroll(object sender, EventArgs e)
        {
            Properties.Settings.Default.title_size = trackBarTitleHeight.Value;
            ScheduleSettingsSave();
            UpdateText();
            ScheduleSettingsApply();
        }

        private void snappingCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.snapping = snappingCheckbox.Checked;
            ScheduleSettingsSave();
            ScheduleSettingsApply();
        }

        private void autoMinifyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.autoMinify = autoMinifyCheckbox.Checked;
            ScheduleSettingsSave();
            ScheduleSettingsApply();
        }

        private void snapSizeSlider_Scroll(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.snapSize = snapSizeSlider.Value;
            ScheduleSettingsSave();
            UpdateText();
            ScheduleSettingsApply();
        }

        private void UpdateText()
        {
            titleHeightText.Text = trackBarTitleHeight.Value + "px";
            opacityValueText.Text = $"{((float)opacityBar.Value / 255f) * 100:0.0}%";
            snapSizeText.Text = snapSizeSlider.Value.ToString();
            overallOpacityText.Text = $"{overallOpacitySlider.Value}%";
        }

        private void headerBtn_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                // Show color picker dialog
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    Color selectedColor = colorDialog.Color; // Get selected color

                    // Apply color to a panel (or any UI element)
                    headerColorPreview.BackColor = Color.FromArgb(headerAlphaSlider.Value, selectedColor);

                    // Store the color for customization
                    Properties.Settings.Default.headerColor = selectedColor;
                    ScheduleSettingsSave();
                    ScheduleSettingsApply();
                }
            }
        }

        private void headerAlphaSlider_Scroll(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.headerAlpha = headerAlphaSlider.Value;
            ScheduleSettingsSave();
            headerColorPreview.BackColor = Color.FromArgb(headerAlphaSlider.Value, Properties.Settings.Default.headerColor);
            ScheduleSettingsApply();
        }

        private void windowColorPanel_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                // Show color picker dialog
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    Color selectedColor = colorDialog.Color; // Get selected color

                    // Apply color to a panel (or any UI element)
                    windowColorPanel.BackColor = Color.FromArgb(opacityBar.Value, selectedColor);

                    // Store the color for customization
                    Properties.Settings.Default.windowColor = selectedColor;
                    ScheduleSettingsSave();
                    ScheduleSettingsApply();
                }
            }
        }

        private void startUpBtn_Click(object sender, EventArgs e)
        {
            string appName = "NoFence";
            string appPath = Application.ExecutablePath;

            bool isEnabled = StartupManager.IsStartupEnabled(appName);
            StartupManager.SetStartup(appName, appPath, !isEnabled); // Toggle the startup state
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void overallOpacitySlider_ValueChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.overallOpacity = (double)overallOpacitySlider.Value / 100f;
            ScheduleSettingsSave();
            ScheduleSettingsApply();
            UpdateText();
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
            Properties.Settings.Default.Save();
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
            base.OnFormClosed(e);
        }
    }
}
