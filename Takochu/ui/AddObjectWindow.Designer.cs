
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SearchLabel
            // 
            this.SearchLabel.AutoSize = true;
            this.SearchLabel.Location = new System.Drawing.Point(12, 9);
            this.SearchLabel.Name = "SearchLabel";
            this.SearchLabel.Size = new System.Drawing.Size(46, 12);
            this.SearchLabel.TabIndex = 0;
            this.SearchLabel.Text = "Search：";
            // 
            // SearchTextBox
            // 
            this.SearchTextBox.Location = new System.Drawing.Point(64, 6);
            this.SearchTextBox.Name = "SearchTextBox";
            this.SearchTextBox.Size = new System.Drawing.Size(202, 19);
            this.SearchTextBox.TabIndex = 1;
            // 
            // ObjectDataTreeView
            // 
            this.ObjectDataTreeView.Location = new System.Drawing.Point(14, 31);
            this.ObjectDataTreeView.Name = "ObjectDataTreeView";
            this.ObjectDataTreeView.Size = new System.Drawing.Size(252, 407);
            this.ObjectDataTreeView.TabIndex = 2;
            this.ObjectDataTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ObjectDataTreeView_AfterSelect);
            this.ObjectDataTreeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.ObjectDataTreeView_NodeMouseClick);
            // 
            // AddObjectButton
            // 
            this.AddObjectButton.Location = new System.Drawing.Point(601, 29);
            this.AddObjectButton.Name = "AddObjectButton";
            this.AddObjectButton.Size = new System.Drawing.Size(75, 23);
            this.AddObjectButton.TabIndex = 3;
            this.AddObjectButton.Text = "AddObject";
            this.AddObjectButton.UseVisualStyleBackColor = true;
            this.AddObjectButton.Click += new System.EventHandler(this.AddObjectButton_Click);
            // 
            // ZoneComboBox
            // 
            this.ZoneComboBox.FormattingEnabled = true;
            this.ZoneComboBox.Location = new System.Drawing.Point(272, 31);
            this.ZoneComboBox.Name = "ZoneComboBox";
            this.ZoneComboBox.Size = new System.Drawing.Size(196, 20);
            this.ZoneComboBox.TabIndex = 4;
            this.ZoneComboBox.SelectedIndexChanged += new System.EventHandler(this.ZoneComboBox_SelectedIndexChanged);
            // 
            // LayerComboBox
            // 
            this.LayerComboBox.FormattingEnabled = true;
            this.LayerComboBox.Location = new System.Drawing.Point(474, 31);
            this.LayerComboBox.Name = "LayerComboBox";
            this.LayerComboBox.Size = new System.Drawing.Size(121, 20);
            this.LayerComboBox.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Location = new System.Drawing.Point(272, 339);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(404, 99);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(6, 18);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(392, 75);
            this.textBox1.TabIndex = 0;
            // 
            // AddObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.LayerComboBox);
            this.Controls.Add(this.ZoneComboBox);
            this.Controls.Add(this.AddObjectButton);
            this.Controls.Add(this.ObjectDataTreeView);
            this.Controls.Add(this.SearchTextBox);
            this.Controls.Add(this.SearchLabel);
            this.Name = "AddObjectWindow";
            this.Text = "AddObjectWindow";
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
        private System.Windows.Forms.TextBox textBox1;
    }
}