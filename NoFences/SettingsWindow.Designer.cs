namespace NoFences
{
    partial class SettingsWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsWindow));
            this.panel1 = new System.Windows.Forms.Panel();
            this.startUpBtn = new System.Windows.Forms.Button();
            this.autoMinifyCheckbox = new System.Windows.Forms.CheckBox();
            this.snappingCheckbox = new System.Windows.Forms.CheckBox();
            this.showContainerFolToggle = new System.Windows.Forms.CheckBox();
            this.hideDesktopToggle = new System.Windows.Forms.CheckBox();
            this.generalTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.windowColorPanel = new System.Windows.Forms.Panel();
            this.headerAlphaSlider = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.headerColorPreview = new System.Windows.Forms.Panel();
            this.snapSizeText = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.snapSizeSlider = new System.Windows.Forms.TrackBar();
            this.titleHeightText = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBarTitleHeight = new System.Windows.Forms.TrackBar();
            this.opacityValueText = new System.Windows.Forms.Label();
            this.opacityBar = new System.Windows.Forms.TrackBar();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.closeBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.overallOpacityText = new System.Windows.Forms.Label();
            this.overallOpacitySlider = new System.Windows.Forms.TrackBar();
            this.panel1.SuspendLayout();
            this.generalTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.headerAlphaSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.snapSizeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarTitleHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.opacityBar)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overallOpacitySlider)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.startUpBtn);
            this.panel1.Controls.Add(this.autoMinifyCheckbox);
            this.panel1.Controls.Add(this.snappingCheckbox);
            this.panel1.Controls.Add(this.showContainerFolToggle);
            this.panel1.Controls.Add(this.hideDesktopToggle);
            this.panel1.Location = new System.Drawing.Point(6, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(400, 415);
            this.panel1.TabIndex = 0;
            // 
            // startUpBtn
            // 
            this.startUpBtn.Location = new System.Drawing.Point(15, 379);
            this.startUpBtn.Name = "startUpBtn";
            this.startUpBtn.Size = new System.Drawing.Size(108, 23);
            this.startUpBtn.TabIndex = 7;
            this.startUpBtn.Text = "Start Up With PC";
            this.startUpBtn.UseVisualStyleBackColor = true;
            this.startUpBtn.Click += new System.EventHandler(this.startUpBtn_Click);
            // 
            // autoMinifyCheckbox
            // 
            this.autoMinifyCheckbox.AutoSize = true;
            this.autoMinifyCheckbox.Location = new System.Drawing.Point(15, 92);
            this.autoMinifyCheckbox.Name = "autoMinifyCheckbox";
            this.autoMinifyCheckbox.Size = new System.Drawing.Size(78, 17);
            this.autoMinifyCheckbox.TabIndex = 6;
            this.autoMinifyCheckbox.Text = "Auto Minify";
            this.autoMinifyCheckbox.UseVisualStyleBackColor = true;
            this.autoMinifyCheckbox.CheckedChanged += new System.EventHandler(this.autoMinifyCheckbox_CheckedChanged);
            // 
            // snappingCheckbox
            // 
            this.snappingCheckbox.AutoSize = true;
            this.snappingCheckbox.Location = new System.Drawing.Point(15, 69);
            this.snappingCheckbox.Name = "snappingCheckbox";
            this.snappingCheckbox.Size = new System.Drawing.Size(107, 17);
            this.snappingCheckbox.TabIndex = 5;
            this.snappingCheckbox.Text = "Enable Snapping";
            this.snappingCheckbox.UseVisualStyleBackColor = true;
            this.snappingCheckbox.CheckedChanged += new System.EventHandler(this.snappingCheckbox_CheckedChanged);
            // 
            // showContainerFolToggle
            // 
            this.showContainerFolToggle.AutoSize = true;
            this.showContainerFolToggle.Location = new System.Drawing.Point(15, 46);
            this.showContainerFolToggle.Name = "showContainerFolToggle";
            this.showContainerFolToggle.Size = new System.Drawing.Size(133, 17);
            this.showContainerFolToggle.TabIndex = 4;
            this.showContainerFolToggle.Text = "Show Container Folder";
            this.showContainerFolToggle.UseVisualStyleBackColor = true;
            this.showContainerFolToggle.CheckedChanged += new System.EventHandler(this.showContainerFolToggle_CheckedChanged);
            // 
            // hideDesktopToggle
            // 
            this.hideDesktopToggle.AutoSize = true;
            this.hideDesktopToggle.Location = new System.Drawing.Point(15, 22);
            this.hideDesktopToggle.Name = "hideDesktopToggle";
            this.hideDesktopToggle.Size = new System.Drawing.Size(120, 17);
            this.hideDesktopToggle.TabIndex = 0;
            this.hideDesktopToggle.Text = "Hide Desktop Icons";
            this.hideDesktopToggle.UseVisualStyleBackColor = true;
            this.hideDesktopToggle.CheckedChanged += new System.EventHandler(this.hideDesktopToggle_CheckedChanged);
            // 
            // generalTab
            // 
            this.generalTab.Controls.Add(this.tabPage1);
            this.generalTab.Controls.Add(this.tabPage2);
            this.generalTab.Controls.Add(this.tabPage3);
            this.generalTab.Location = new System.Drawing.Point(12, 12);
            this.generalTab.Name = "generalTab";
            this.generalTab.SelectedIndex = 0;
            this.generalTab.Size = new System.Drawing.Size(420, 445);
            this.generalTab.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(412, 419);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "General";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.panel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(412, 419);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Visual";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.overallOpacityText);
            this.panel2.Controls.Add(this.overallOpacitySlider);
            this.panel2.Controls.Add(this.label5);
            this.panel2.Controls.Add(this.windowColorPanel);
            this.panel2.Controls.Add(this.headerAlphaSlider);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.headerColorPreview);
            this.panel2.Controls.Add(this.snapSizeText);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.snapSizeSlider);
            this.panel2.Controls.Add(this.titleHeightText);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.trackBarTitleHeight);
            this.panel2.Controls.Add(this.opacityValueText);
            this.panel2.Controls.Add(this.opacityBar);
            this.panel2.Location = new System.Drawing.Point(6, 6);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(400, 430);
            this.panel2.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(23, 311);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(73, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "Window Color";
            // 
            // windowColorPanel
            // 
            this.windowColorPanel.Location = new System.Drawing.Point(26, 336);
            this.windowColorPanel.Name = "windowColorPanel";
            this.windowColorPanel.Size = new System.Drawing.Size(27, 28);
            this.windowColorPanel.TabIndex = 20;
            this.windowColorPanel.Click += new System.EventHandler(this.windowColorPanel_Click);
            // 
            // headerAlphaSlider
            // 
            this.headerAlphaSlider.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.headerAlphaSlider.Location = new System.Drawing.Point(63, 251);
            this.headerAlphaSlider.Maximum = 255;
            this.headerAlphaSlider.Name = "headerAlphaSlider";
            this.headerAlphaSlider.Size = new System.Drawing.Size(165, 45);
            this.headerAlphaSlider.TabIndex = 19;
            this.headerAlphaSlider.TickFrequency = 10;
            this.headerAlphaSlider.TickStyle = System.Windows.Forms.TickStyle.None;
            this.headerAlphaSlider.Value = 30;
            this.headerAlphaSlider.Scroll += new System.EventHandler(this.headerAlphaSlider_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 235);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Header Color";
            // 
            // headerColorPreview
            // 
            this.headerColorPreview.Location = new System.Drawing.Point(26, 260);
            this.headerColorPreview.Name = "headerColorPreview";
            this.headerColorPreview.Size = new System.Drawing.Size(27, 28);
            this.headerColorPreview.TabIndex = 17;
            this.headerColorPreview.Click += new System.EventHandler(this.headerBtn_Click);
            // 
            // snapSizeText
            // 
            this.snapSizeText.AutoSize = true;
            this.snapSizeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.snapSizeText.Location = new System.Drawing.Point(234, 173);
            this.snapSizeText.Name = "snapSizeText";
            this.snapSizeText.Size = new System.Drawing.Size(21, 16);
            this.snapSizeText.TabIndex = 15;
            this.snapSizeText.Text = "30";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 147);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 13);
            this.label4.TabIndex = 14;
            this.label4.Text = "Snapping Size";
            // 
            // snapSizeSlider
            // 
            this.snapSizeSlider.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.snapSizeSlider.Location = new System.Drawing.Point(12, 173);
            this.snapSizeSlider.Maximum = 300;
            this.snapSizeSlider.Minimum = 2;
            this.snapSizeSlider.Name = "snapSizeSlider";
            this.snapSizeSlider.Size = new System.Drawing.Size(216, 45);
            this.snapSizeSlider.TabIndex = 13;
            this.snapSizeSlider.TickFrequency = 5;
            this.snapSizeSlider.Value = 30;
            this.snapSizeSlider.Scroll += new System.EventHandler(this.snapSizeSlider_Scroll);
            // 
            // titleHeightText
            // 
            this.titleHeightText.AutoSize = true;
            this.titleHeightText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleHeightText.Location = new System.Drawing.Point(234, 95);
            this.titleHeightText.Name = "titleHeightText";
            this.titleHeightText.Size = new System.Drawing.Size(45, 16);
            this.titleHeightText.TabIndex = 12;
            this.titleHeightText.Text = "100 px";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Title Font Size";
            // 
            // trackBarTitleHeight
            // 
            this.trackBarTitleHeight.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.trackBarTitleHeight.Location = new System.Drawing.Point(12, 95);
            this.trackBarTitleHeight.Maximum = 72;
            this.trackBarTitleHeight.Minimum = 16;
            this.trackBarTitleHeight.Name = "trackBarTitleHeight";
            this.trackBarTitleHeight.Size = new System.Drawing.Size(216, 45);
            this.trackBarTitleHeight.TabIndex = 10;
            this.trackBarTitleHeight.TickFrequency = 5;
            this.trackBarTitleHeight.Value = 20;
            this.trackBarTitleHeight.Scroll += new System.EventHandler(this.trackBarTitleHeight_Scroll);
            // 
            // opacityValueText
            // 
            this.opacityValueText.AutoSize = true;
            this.opacityValueText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.opacityValueText.Location = new System.Drawing.Point(234, 348);
            this.opacityValueText.Name = "opacityValueText";
            this.opacityValueText.Size = new System.Drawing.Size(28, 16);
            this.opacityValueText.TabIndex = 5;
            this.opacityValueText.Text = "100";
            // 
            // opacityBar
            // 
            this.opacityBar.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.opacityBar.Cursor = System.Windows.Forms.Cursors.NoMoveHoriz;
            this.opacityBar.Location = new System.Drawing.Point(63, 336);
            this.opacityBar.Maximum = 255;
            this.opacityBar.Name = "opacityBar";
            this.opacityBar.Size = new System.Drawing.Size(165, 45);
            this.opacityBar.TabIndex = 4;
            this.opacityBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.opacityBar.Scroll += new System.EventHandler(this.opacityBar_ValueChanged);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(412, 419);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "About";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Cursor = System.Windows.Forms.Cursors.Default;
            this.richTextBox1.Location = new System.Drawing.Point(3, 3);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(406, 214);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // closeBtn
            // 
            this.closeBtn.Location = new System.Drawing.Point(350, 459);
            this.closeBtn.Name = "closeBtn";
            this.closeBtn.Size = new System.Drawing.Size(75, 23);
            this.closeBtn.TabIndex = 2;
            this.closeBtn.Text = "Close";
            this.closeBtn.UseVisualStyleBackColor = true;
            this.closeBtn.Click += new System.EventHandler(this.closeBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Opacity";
            // 
            // overallOpacityText
            // 
            this.overallOpacityText.AutoSize = true;
            this.overallOpacityText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.overallOpacityText.Location = new System.Drawing.Point(234, 45);
            this.overallOpacityText.Name = "overallOpacityText";
            this.overallOpacityText.Size = new System.Drawing.Size(40, 16);
            this.overallOpacityText.TabIndex = 23;
            this.overallOpacityText.Text = "100%";
            // 
            // overallOpacitySlider
            // 
            this.overallOpacitySlider.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.overallOpacitySlider.Cursor = System.Windows.Forms.Cursors.NoMoveHoriz;
            this.overallOpacitySlider.Location = new System.Drawing.Point(12, 30);
            this.overallOpacitySlider.Maximum = 100;
            this.overallOpacitySlider.Minimum = 5;
            this.overallOpacitySlider.Name = "overallOpacitySlider";
            this.overallOpacitySlider.Size = new System.Drawing.Size(216, 45);
            this.overallOpacitySlider.TabIndex = 22;
            this.overallOpacitySlider.TickFrequency = 10;
            this.overallOpacitySlider.Value = 80;
            this.overallOpacitySlider.ValueChanged += new System.EventHandler(this.overallOpacitySlider_ValueChanged);
            // 
            // SettingsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.ClientSize = new System.Drawing.Size(444, 492);
            this.Controls.Add(this.closeBtn);
            this.Controls.Add(this.generalTab);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SettingsWindow";
            this.Text = "Settings Window";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.generalTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.headerAlphaSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.snapSizeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarTitleHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.opacityBar)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.overallOpacitySlider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox hideDesktopToggle;
        private System.Windows.Forms.CheckBox showContainerFolToggle;
        private System.Windows.Forms.TabControl generalTab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label titleHeightText;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TrackBar trackBarTitleHeight;
        private System.Windows.Forms.Label opacityValueText;
        private System.Windows.Forms.TrackBar opacityBar;
        private System.Windows.Forms.CheckBox snappingCheckbox;
        private System.Windows.Forms.CheckBox autoMinifyCheckbox;
        private System.Windows.Forms.Label snapSizeText;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar snapSizeSlider;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Panel headerColorPreview;
        private System.Windows.Forms.TrackBar headerAlphaSlider;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel windowColorPanel;
        private System.Windows.Forms.Button startUpBtn;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button closeBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label overallOpacityText;
        private System.Windows.Forms.TrackBar overallOpacitySlider;
    }
}