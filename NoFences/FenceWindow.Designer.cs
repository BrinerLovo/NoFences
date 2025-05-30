namespace NoFences
{
    partial class FenceWindow
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FenceWindow));
            this.appContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lockedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.newFenceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeAppMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appContextMenuDark = new NoFences.Win32.CustomContextMenu();
            this.lockedTick = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.appContextMenu.SuspendLayout();
            this.appContextMenuDark.SuspendLayout();
            this.SuspendLayout();
            // 
            // appContextMenu
            // 
            this.appContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteItemToolStripMenuItem,
            this.lockedToolStripMenuItem,
            this.minifyToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.settingsMenuItem,
            this.toolStripSeparator1,
            this.newFenceToolStripMenuItem,
            this.exitToolStripMenuItem,
            this.closeAppMenuItem});
            this.appContextMenu.Name = "contextMenuStrip1";
            this.appContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            resources.ApplyResources(this.appContextMenu, "appContextMenu");
            this.appContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // deleteItemToolStripMenuItem
            // 
            resources.ApplyResources(this.deleteItemToolStripMenuItem, "deleteItemToolStripMenuItem");
            this.deleteItemToolStripMenuItem.Name = "deleteItemToolStripMenuItem";
            this.deleteItemToolStripMenuItem.Click += new System.EventHandler(this.deleteItemToolStripMenuItem_Click);
            // 
            // lockedToolStripMenuItem
            // 
            this.lockedToolStripMenuItem.CheckOnClick = true;
            this.lockedToolStripMenuItem.Name = "lockedToolStripMenuItem";
            resources.ApplyResources(this.lockedToolStripMenuItem, "lockedToolStripMenuItem");
            this.lockedToolStripMenuItem.Click += new System.EventHandler(this.lockedToolStripMenuItem_Click);
            // 
            // minifyToolStripMenuItem
            // 
            this.minifyToolStripMenuItem.CheckOnClick = true;
            this.minifyToolStripMenuItem.Name = "minifyToolStripMenuItem";
            resources.ApplyResources(this.minifyToolStripMenuItem, "minifyToolStripMenuItem");
            this.minifyToolStripMenuItem.Click += new System.EventHandler(this.minifyToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            resources.ApplyResources(this.renameToolStripMenuItem, "renameToolStripMenuItem");
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // settingsMenuItem
            // 
            this.settingsMenuItem.Name = "settingsMenuItem";
            resources.ApplyResources(this.settingsMenuItem, "settingsMenuItem");
            this.settingsMenuItem.Click += new System.EventHandler(this.settingsMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // newFenceToolStripMenuItem
            // 
            this.newFenceToolStripMenuItem.Name = "newFenceToolStripMenuItem";
            resources.ApplyResources(this.newFenceToolStripMenuItem, "newFenceToolStripMenuItem");
            this.newFenceToolStripMenuItem.Click += new System.EventHandler(this.newFenceToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            resources.ApplyResources(this.exitToolStripMenuItem, "exitToolStripMenuItem");
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // closeAppMenuItem
            // 
            this.closeAppMenuItem.Name = "closeAppMenuItem";
            resources.ApplyResources(this.closeAppMenuItem, "closeAppMenuItem");
            this.closeAppMenuItem.Click += new System.EventHandler(this.closeAppMenuItem_Click);
            // 
            // appContextMenuDark
            // 
            this.appContextMenuDark.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lockedTick,
            this.toolStripMenuItem2,
            this.toolStripMenuItem5,
            this.toolStripSeparator2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4});
            this.appContextMenuDark.Name = "appContextMenu2";
            resources.ApplyResources(this.appContextMenuDark, "appContextMenuDark");
            // 
            // lockedTick
            // 
            this.lockedTick.CheckOnClick = true;
            this.lockedTick.Name = "lockedTick";
            resources.ApplyResources(this.lockedTick, "lockedTick");
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            this.toolStripMenuItem2.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
            this.toolStripMenuItem5.Click += new System.EventHandler(this.settingsMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            this.toolStripMenuItem3.Click += new System.EventHandler(this.newFenceToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
            this.toolStripMenuItem4.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // FenceWindow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimizeBox = false;
            this.Name = "FenceWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FenceWindow_FormClosed);
            this.Load += new System.EventHandler(this.FenceWindow_Load);
            this.LocationChanged += new System.EventHandler(this.FenceWindow_LocationChanged);
            this.Click += new System.EventHandler(this.FenceWindow_Click);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.FenceWindow_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.FenceWindow_DragEnter);
            this.GiveFeedback += new System.Windows.Forms.GiveFeedbackEventHandler(this.FenceWindow_GiveFeedback);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.FenceWindow_Paint);
            this.QueryContinueDrag += new System.Windows.Forms.QueryContinueDragEventHandler(this.FenceWindow_QueryContinueDrag);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FenceWindow_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.FenceWindow_MouseDoubleClick);
            this.MouseEnter += new System.EventHandler(this.FenceWindow_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.FenceWindow_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FenceWindow_MouseMove);
            this.Resize += new System.EventHandler(this.FenceWindow_Resize);
            this.appContextMenu.ResumeLayout(false);
            this.appContextMenuDark.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip appContextMenu;
        private System.Windows.Forms.ToolStripMenuItem lockedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minifyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteItemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newFenceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeAppMenuItem;
        private Win32.CustomContextMenu appContextMenuDark;
        private System.Windows.Forms.ToolStripMenuItem lockedTick;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
    }
}

