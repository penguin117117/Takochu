
namespace Takochu.ui
{
    partial class AddObjectWindow
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
            this.SearchLabel = new System.Windows.Forms.Label();
            this.SearchTextBox = new System.Windows.Forms.TextBox();
            this.ObjectDataTreeView = new System.Windows.Forms.TreeView();
            this.AddObjectButton = new System.Windows.Forms.Button();
            this.ZoneComboBox = new System.Windows.Forms.ComboBox();
            this.LayerComboBox = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.NoteTextBox = new System.Windows.Forms.TextBox();
            this.FilenNameLabel = new System.Windows.Forms.Label();
            this.TargetZoneLabel = new System.Windows.Forms.Label();
            this.TagetLayerLabel = new System.Windows.Forms.Label();
            this.NoteLabel = new System.Windows.Forms.Label();
            this.FileNameTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SearchLabel
            // 
            this.SearchLabel.AutoSize = true;
            this.SearchLabel.Location = new System.Drawing.Point(12, 9);
            this.SearchLabel.Name = "SearchLabel";
            this.SearchLabel.Size = new System.Drawing.Size(58, 12);
            this.SearchLabel.TabIndex = 0;
            this.SearchLabel.Text = "Search：🔎";
            // 
            // SearchTextBox
            // 
            this.SearchTextBox.Location = new System.Drawing.Point(14, 27);
            this.SearchTextBox.Name = "SearchTextBox";
            this.SearchTextBox.Size = new System.Drawing.Size(362, 19);
            this.SearchTextBox.TabIndex = 1;
            this.SearchTextBox.TextChanged += new System.EventHandler(this.SearchTextBox_TextChanged);
            // 
            // ObjectDataTreeView
            // 
            this.ObjectDataTreeView.Location = new System.Drawing.Point(14, 52);
            this.ObjectDataTreeView.Name = "ObjectDataTreeView";
            this.ObjectDataTreeView.Size = new System.Drawing.Size(362, 386);
            this.ObjectDataTreeView.TabIndex = 2;
            this.ObjectDataTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectDataTreeView_AfterSelect);
            this.ObjectDataTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.ObjectDataTreeView_NodeMouseClick);
            // 
            // AddObjectButton
            // 
            this.AddObjectButton.Location = new System.Drawing.Point(297, 357);
            this.AddObjectButton.Name = "AddObjectButton";
            this.AddObjectButton.Size = new System.Drawing.Size(101, 23);
            this.AddObjectButton.TabIndex = 3;
            this.AddObjectButton.Text = "AddObject";
            this.AddObjectButton.UseVisualStyleBackColor = true;
            this.AddObjectButton.Click += new System.EventHandler(this.AddObjectButton_Click);
            // 
            // ZoneComboBox
            // 
            this.ZoneComboBox.FormattingEnabled = true;
            this.ZoneComboBox.Location = new System.Drawing.Point(8, 319);
            this.ZoneComboBox.Name = "ZoneComboBox";
            this.ZoneComboBox.Size = new System.Drawing.Size(285, 20);
            this.ZoneComboBox.TabIndex = 4;
            this.ZoneComboBox.SelectedIndexChanged += new System.EventHandler(this.ZoneComboBox_SelectedIndexChanged);
            // 
            // LayerComboBox
            // 
            this.LayerComboBox.FormattingEnabled = true;
            this.LayerComboBox.Location = new System.Drawing.Point(8, 357);
            this.LayerComboBox.Name = "LayerComboBox";
            this.LayerComboBox.Size = new System.Drawing.Size(285, 20);
            this.LayerComboBox.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.NameTextBox);
            this.groupBox1.Controls.Add(this.NameLabel);
            this.groupBox1.Controls.Add(this.FileNameTextBox);
            this.groupBox1.Controls.Add(this.NoteLabel);
            this.groupBox1.Controls.Add(this.TagetLayerLabel);
            this.groupBox1.Controls.Add(this.TargetZoneLabel);
            this.groupBox1.Controls.Add(this.FilenNameLabel);
            this.groupBox1.Controls.Add(this.AddObjectButton);
            this.groupBox1.Controls.Add(this.LayerComboBox);
            this.groupBox1.Controls.Add(this.NoteTextBox);
            this.groupBox1.Controls.Add(this.ZoneComboBox);
            this.groupBox1.Location = new System.Drawing.Point(384, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(404, 411);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "SelectObjectInfo";
            // 
            // NoteTextBox
            // 
            this.NoteTextBox.Location = new System.Drawing.Point(8, 221);
            this.NoteTextBox.Multiline = true;
            this.NoteTextBox.Name = "NoteTextBox";
            this.NoteTextBox.ReadOnly = true;
            this.NoteTextBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.NoteTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.NoteTextBox.Size = new System.Drawing.Size(392, 80);
            this.NoteTextBox.TabIndex = 0;
            // 
            // FilenNameLabel
            // 
            this.FilenNameLabel.AutoSize = true;
            this.FilenNameLabel.Location = new System.Drawing.Point(6, 169);
            this.FilenNameLabel.Name = "FilenNameLabel";
            this.FilenNameLabel.Size = new System.Drawing.Size(55, 12);
            this.FilenNameLabel.TabIndex = 1;
            this.FilenNameLabel.Text = "FileName:";
            // 
            // TargetZoneLabel
            // 
            this.TargetZoneLabel.AutoSize = true;
            this.TargetZoneLabel.Location = new System.Drawing.Point(6, 304);
            this.TargetZoneLabel.Name = "TargetZoneLabel";
            this.TargetZoneLabel.Size = new System.Drawing.Size(63, 12);
            this.TargetZoneLabel.TabIndex = 6;
            this.TargetZoneLabel.Text = "TargetZone";
            // 
            // TagetLayerLabel
            // 
            this.TagetLayerLabel.AutoSize = true;
            this.TagetLayerLabel.Location = new System.Drawing.Point(6, 342);
            this.TagetLayerLabel.Name = "TagetLayerLabel";
            this.TagetLayerLabel.Size = new System.Drawing.Size(66, 12);
            this.TagetLayerLabel.TabIndex = 7;
            this.TagetLayerLabel.Text = "TargetLayer";
            // 
            // NoteLabel
            // 
            this.NoteLabel.AutoSize = true;
            this.NoteLabel.Location = new System.Drawing.Point(6, 206);
            this.NoteLabel.Name = "NoteLabel";
            this.NoteLabel.Size = new System.Drawing.Size(31, 12);
            this.NoteLabel.TabIndex = 8;
            this.NoteLabel.Text = "Note:";
            // 
            // FileNameTextBox
            // 
            this.FileNameTextBox.Location = new System.Drawing.Point(8, 184);
            this.FileNameTextBox.Name = "FileNameTextBox";
            this.FileNameTextBox.Size = new System.Drawing.Size(392, 19);
            this.FileNameTextBox.TabIndex = 9;
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(6, 15);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(34, 12);
            this.NameLabel.TabIndex = 10;
            this.NameLabel.Text = "Name";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(8, 30);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(392, 19);
            this.NameTextBox.TabIndex = 11;
            // 
            // AddObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.ObjectDataTreeView);
            this.Controls.Add(this.SearchTextBox);
            this.Controls.Add(this.SearchLabel);
            this.Name = "AddObjectWindow";
            this.Text = "AddObjectWindow";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.AddObjectWindow_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label SearchLabel;
        private System.Windows.Forms.TextBox SearchTextBox;
        private System.Windows.Forms.TreeView ObjectDataTreeView;
        private System.Windows.Forms.Button AddObjectButton;
        private System.Windows.Forms.ComboBox ZoneComboBox;
        private System.Windows.Forms.ComboBox LayerComboBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox NoteTextBox;
        private System.Windows.Forms.Label TagetLayerLabel;
        private System.Windows.Forms.Label TargetZoneLabel;
        private System.Windows.Forms.Label FilenNameLabel;
        private System.Windows.Forms.TextBox FileNameTextBox;
        private System.Windows.Forms.Label NoteLabel;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label NameLabel;
    }
}