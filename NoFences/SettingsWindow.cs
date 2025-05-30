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

        public SettingsWindow()
        {
            InitializeComponent();
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
            Properties.Settings.Default.Save(); // Saves the setting immediately
            OnSettingsChanged?.Invoke();
        }

        private void opacityBar_ValueChanged(object sender, System.EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.opacity = opacityBar.Value;
            Properties.Settings.Default.Save(); // Saves the setting immediately
            UpdateText();
            windowColorPanel.BackColor = Color.FromArgb(opacityBar.Value, Properties.Settings.Default.windowColor);
            OnSettingsChanged?.Invoke();
        }

        private void showContainerFolToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.show_container_folder = showContainerFolToggle.Checked;
            Properties.Settings.Default.Save(); // Saves the setting immediately

            if (showContainerFolToggle.Checked)
            {
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
            Properties.Settings.Default.Save();
            UpdateText();
            OnSettingsChanged?.Invoke();
        }

        private void snappingCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.snapping = snappingCheckbox.Checked;
            Properties.Settings.Default.Save();
            OnSettingsChanged?.Invoke();
        }

        private void autoMinifyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.autoMinify = autoMinifyCheckbox.Checked;
            Properties.Settings.Default.Save();
            OnSettingsChanged?.Invoke();
        }

        private void snapSizeSlider_Scroll(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.snapSize = snapSizeSlider.Value;
            Properties.Settings.Default.Save();
            UpdateText();
            OnSettingsChanged?.Invoke();
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
                    Properties.Settings.Default.Save();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        private void headerAlphaSlider_Scroll(object sender, EventArgs e)
        {
            if (!initialized) return;

            Properties.Settings.Default.headerAlpha = headerAlphaSlider.Value;
            Properties.Settings.Default.Save();
            headerColorPreview.BackColor = Color.FromArgb(headerAlphaSlider.Value, Properties.Settings.Default.headerColor);
            OnSettingsChanged?.Invoke();
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
                    Properties.Settings.Default.Save();
                    OnSettingsChanged?.Invoke();
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
            Properties.Settings.Default.Save();
            OnSettingsChanged?.Invoke();
            UpdateText();
        }
    }
}
