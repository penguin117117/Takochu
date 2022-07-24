using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.io;
using Takochu.smg;
using Takochu.smg.img;
using Takochu.smg.msg;
using Takochu.ui;
using Takochu.util;
using Microsoft.WindowsAPICodePack.Dialogs;
using Takochu.ui.MainWindowSys;

namespace Takochu
{
    public partial class MainWindow : Form
    {
        public GameDirectory GameDirectory { get; private set; }
        public static TreeView ObjectDBTreeView { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.BCSVPaths == null)
            {
                Properties.Settings.Default.BCSVPaths = new List<string>();
            }

            GameDirectory = new GameDirectory();
            SetGalaxyTreeView();

            //MainWindow.ObjectDBTreeView = new TreeView();
            ////MainWindow.ObjectDBTreeView.CreateControl();
            //foreach (var node in ObjectDB.ObjectNodes)
            //{
            //    MainWindow.ObjectDBTreeView.Nodes.Add(node);
            //}

            //var count = ObjectDB.ObjectNodes.Count();

            //for (int i = 0; i < count; i++) 
            //{
            //    MainWindow.ObjectDBTreeView.Nodes.Add(ObjectDB.ObjectNodes[i]);
            //}

            //ObjectDBTreeView.CreateControl();
            //ObjectDBTreeView.Nodes.AddRange(ObjectDB.ObjectNodes);
            //ObjectDBTreeView.Location = new System.Drawing.Point(14, 31);
            //ObjectDBTreeView.Name = "ObjectDataTreeView";
            //ObjectDBTreeView.Size = new System.Drawing.Size(252, 407);
            //ObjectDBTreeView.TabIndex = 2;
        }

        private void SetGalaxyTreeView(bool reSetup = false)
        {
            try
            {
                GameDirectory.OpenDirectory(reSetup);

                bcsvEditorBtn.Enabled = true;
                galaxyTreeView.Nodes.Clear();

                List<string> galaxies = Program.sGame.GetGalaxies();
                Dictionary<string, string> simpleNames = Translate.GetGalaxyNames();

                foreach (string galaxy in galaxies)
                {
                    if (simpleNames.ContainsKey(galaxy))
                    {
                        TreeNode node = new TreeNode(simpleNames[galaxy]);
                        node.ToolTipText = galaxy;
                        node.Tag = galaxy;
                        galaxyTreeView.Nodes.Add(node);
                    }
                    else
                    {
                        TreeNode node = new TreeNode(galaxy);
                        node.ToolTipText = galaxy;
                        node.Tag = galaxy;
                        galaxyTreeView.Nodes.Add(node);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"「{ex.Message}」\n\n" +
                    "An error has occurred in reading the file." +
                    "After initializing the path of the working directory, this program will be killed.\n" +
                    "ファイルの読み込みにエラーが発生しました。" +
                    "作業ディレクトリのパスを初期化した後、このプログラムは終了します。",
                    "Error"
                    );
                GameDirectory.SetDefaultPath();
                KillApplication();
            }

        }

        /// <summary>
        /// アプリケーションを強制終了させます。
        /// </summary>
        private void KillApplication()
        {
            Close();
            Environment.Exit(0);
        }

        private void selectGameFolderBtn_Click(object sender, EventArgs e)
        {
            bool successfulSet = GameDirectory.BrowseSetGameDirectory();
            if (successfulSet) SetGalaxyTreeView(true);
        }

        private void BcsvEditorBtn_Click(object sender, EventArgs e)
        {
            BCSVEditorForm bcsvEditor = new BCSVEditorForm();
            bcsvEditor.Show();
        }

        private void galaxyTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (galaxyTreeView.SelectedNode != null)
            {
                EditorWindow win = new EditorWindow(Convert.ToString(galaxyTreeView.SelectedNode.Tag));
                win.Show();

            }
        }

        private void rarcExplorer_Btn_Click(object sender, EventArgs e)
        {
            RARCExplorer explorer = new RARCExplorer();
            explorer.Show();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // this is our main program getting closed, so we can update our name table if needed
            List<string> fields = new List<string>();

            foreach (KeyValuePair<int, string> kvp in BCSV.sHashTable)
            {
                fields.Add(kvp.Value);
            }

            File.WriteAllLines("res/FieldNames.txt", fields.ToArray());

            NameHolder.Close();
        }

        private void showMessageEditorBtn_Click(object sender, EventArgs e)
        {
            MessageEditor editor = new MessageEditor();
            editor.Show();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            RenderingTest test = new RenderingTest();
            test.Show();
        }

        private void settingsBtn_Click(object sender, EventArgs e)
        {
            SettingsForm settings = new SettingsForm(galaxyTreeView);
            settings.Show();
        }

        private void hashCalcBtn_Click(object sender, EventArgs e)
        {
            HashGenForm hash = new HashGenForm();
            hash.Show();
        }
    }
}
