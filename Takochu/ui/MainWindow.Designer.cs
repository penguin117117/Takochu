namespace Takochu
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.mainToolStrip = new System.Windows.Forms.ToolStrip();
            this.selectGameFolderBtn = new System.Windows.Forms.ToolStripButton();
            this.bcsvEditorBtn = new System.Windows.Forms.ToolStripButton();
            this.rarcExplorer_Btn = new System.Windows.Forms.ToolStripButton();
            this.hashCalcBtn = new System.Windows.Forms.ToolStripButton();
            this.showMessageEditorBtn = new System.Windows.Forms.ToolStripButton();
            this.settingsBtn = new System.Windows.Forms.ToolStripButton();
            this.galaxyTreeView = new System.Windows.Forms.TreeView();
            this.glControl1 = new OpenTK.GLControl();
            this.mainToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainToolStrip
            // 
            this.mainToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.mainToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectGameFolderBtn,
            this.bcsvEditorBtn,
            this.rarcExplorer_Btn,
            this.hashCalcBtn,
            this.showMessageEditorBtn,
            this.settingsBtn});
            this.mainToolStrip.Location = new System.Drawing.Point(0, 0);
            this.mainToolStrip.Name = "mainToolStrip";
            this.mainToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.mainToolStrip.Size = new System.Drawing.Size(1333, 34);
            this.mainToolStrip.Stretch = true;
            this.mainToolStrip.TabIndex = 0;
            this.mainToolStrip.Text = "toolStrip1";
            // 
            // selectGameFolderBtn
            // 
            this.selectGameFolderBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.selectGameFolderBtn.Image = ((System.Drawing.Image)(resources.GetObject("selectGameFolderBtn.Image")));
            this.selectGameFolderBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.selectGameFolderBtn.Name = "selectGameFolderBtn";
            this.selectGameFolderBtn.Size = new System.Drawing.Size(167, 29);
            this.selectGameFolderBtn.Text = "Select Game Folder";
            this.selectGameFolderBtn.Click += new System.EventHandler(this.selectGameFolderBtn_Click);
            // 
            // bcsvEditorBtn
            // 
            this.bcsvEditorBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bcsvEditorBtn.Enabled = false;
            this.bcsvEditorBtn.Image = ((System.Drawing.Image)(resources.GetObject("bcsvEditorBtn.Image")));
            this.bcsvEditorBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bcsvEditorBtn.Name = "bcsvEditorBtn";
            this.bcsvEditorBtn.Size = new System.Drawing.Size(110, 29);
            this.bcsvEditorBtn.Text = "BCSV Editor";
            this.bcsvEditorBtn.Click += new System.EventHandler(this.BcsvEditorBtn_Click);
            // 
            // rarcExplorer_Btn
            // 
            this.rarcExplorer_Btn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.rarcExplorer_Btn.Image = ((System.Drawing.Image)(resources.GetObject("rarcExplorer_Btn.Image")));
            this.rarcExplorer_Btn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.rarcExplorer_Btn.Name = "rarcExplorer_Btn";
            this.rarcExplorer_Btn.Size = new System.Drawing.Size(130, 29);
            this.rarcExplorer_Btn.Text = "RARC Explorer";
            this.rarcExplorer_Btn.Click += new System.EventHandler(this.rarcExplorer_Btn_Click);
            // 
            // hashCalcBtn
            // 
            this.hashCalcBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.hashCalcBtn.Image = ((System.Drawing.Image)(resources.GetObject("hashCalcBtn.Image")));
            this.hashCalcBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.hashCalcBtn.Name = "hashCalcBtn";
            this.hashCalcBtn.Size = new System.Drawing.Size(139, 29);
            this.hashCalcBtn.Text = "Hash Calculator";
            this.hashCalcBtn.Click += new System.EventHandler(this.hashCalcBtn_Click);
            // 
            // showMessageEditorBtn
            // 
            this.showMessageEditorBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.showMessageEditorBtn.Image = ((System.Drawing.Image)(resources.GetObject("showMessageEditorBtn.Image")));
            this.showMessageEditorBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.showMessageEditorBtn.Name = "showMessageEditorBtn";
            this.showMessageEditorBtn.Size = new System.Drawing.Size(138, 29);
            this.showMessageEditorBtn.Text = "Message Editor";
            this.showMessageEditorBtn.Click += new System.EventHandler(this.showMessageEditorBtn_Click);
            // 
            // settingsBtn
            // 
            this.settingsBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.settingsBtn.Image = ((System.Drawing.Image)(resources.GetObject("settingsBtn.Image")));
            this.settingsBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.settingsBtn.Name = "settingsBtn";
            this.settingsBtn.Size = new System.Drawing.Size(80, 29);
            this.settingsBtn.Text = "Settings";
            this.settingsBtn.Click += new System.EventHandler(this.settingsBtn_Click);
            // 
            // galaxyTreeView
            // 
            this.galaxyTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.galaxyTreeView.Location = new System.Drawing.Point(0, 34);
            this.galaxyTreeView.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.galaxyTreeView.Name = "galaxyTreeView";
            this.galaxyTreeView.Size = new System.Drawing.Size(1333, 589);
            this.galaxyTreeView.TabIndex = 1;
            this.galaxyTreeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.galaxyTreeView_NodeMouseDoubleClick);
            this.galaxyTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.galaxyTreeView_KeyDown);
            // 
            // glControl1
            // 
            this.glControl1.BackColor = System.Drawing.Color.Black;
            this.glControl1.Location = new System.Drawing.Point(0, 0);
            this.glControl1.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.glControl1.Name = "glControl1";
            this.glControl1.Size = new System.Drawing.Size(0, 0);
            this.glControl1.TabIndex = 2;
            this.glControl1.VSync = false;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1333, 623);
            this.Controls.Add(this.glControl1);
            this.Controls.Add(this.galaxyTreeView);
            this.Controls.Add(this.mainToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.Name = "MainWindow";
            this.Text = "Takochu";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.mainToolStrip.ResumeLayout(false);
            this.mainToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip mainToolStrip;
        private System.Windows.Forms.ToolStripButton selectGameFolderBtn;
        private System.Windows.Forms.ToolStripButton bcsvEditorBtn;
        private System.Windows.Forms.TreeView galaxyTreeView;
        private System.Windows.Forms.ToolStripButton rarcExplorer_Btn;
        private OpenTK.GLControl glControl1;
        private System.Windows.Forms.ToolStripButton showMessageEditorBtn;
        private System.Windows.Forms.ToolStripButton settingsBtn;
        private System.Windows.Forms.ToolStripButton hashCalcBtn;
    }
}

