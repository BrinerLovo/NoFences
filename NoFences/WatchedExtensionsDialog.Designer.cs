namespace NoFences
{
    partial class WatchedExtensionsDialog
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
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelDescription = new System.Windows.Forms.Label();
            this.listBoxExtensions = new System.Windows.Forms.ListBox();
            this.labelAddExtension = new System.Windows.Forms.Label();
            this.textBoxNewExtension = new System.Windows.Forms.TextBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.Location = new System.Drawing.Point(12, 9);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(199, 15);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "Configure Watched Extensions";
            // 
            // labelDescription
            // 
            this.labelDescription.Location = new System.Drawing.Point(12, 30);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(360, 40);
            this.labelDescription.TabIndex = 1;
            this.labelDescription.Text = "Files with these extensions will be automatically moved from the desktop to this " +
    "fence when created. Leave empty to disable file watching.";
            // 
            // listBoxExtensions
            // 
            this.listBoxExtensions.FormattingEnabled = true;
            this.listBoxExtensions.Location = new System.Drawing.Point(15, 80);
            this.listBoxExtensions.Name = "listBoxExtensions";
            this.listBoxExtensions.Size = new System.Drawing.Size(200, 160);
            this.listBoxExtensions.TabIndex = 2;
            this.listBoxExtensions.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listBoxExtensions_KeyDown);
            // 
            // labelAddExtension
            // 
            this.labelAddExtension.AutoSize = true;
            this.labelAddExtension.Location = new System.Drawing.Point(230, 80);
            this.labelAddExtension.Name = "labelAddExtension";
            this.labelAddExtension.Size = new System.Drawing.Size(83, 13);
            this.labelAddExtension.TabIndex = 3;
            this.labelAddExtension.Text = "Add Extension:";
            // 
            // textBoxNewExtension
            // 
            this.textBoxNewExtension.Location = new System.Drawing.Point(230, 100);
            this.textBoxNewExtension.Name = "textBoxNewExtension";
            this.textBoxNewExtension.Size = new System.Drawing.Size(100, 20);
            this.textBoxNewExtension.TabIndex = 4;
            this.textBoxNewExtension.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxNewExtension_KeyPress);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(230, 130);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(60, 23);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(230, 160);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(60, 23);
            this.btnRemove.TabIndex = 6;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(217, 260);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(298, 260);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // WatchedExtensionsDialog
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 295);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.textBoxNewExtension);
            this.Controls.Add(this.labelAddExtension);
            this.Controls.Add(this.listBoxExtensions);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.labelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WatchedExtensionsDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Watched Extensions";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.ListBox listBoxExtensions;
        private System.Windows.Forms.Label labelAddExtension;
        private System.Windows.Forms.TextBox textBoxNewExtension;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}