using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.smg;
using Takochu.smg.obj;
using Takochu.util.GameVer;
using ObjDB = Takochu.smg.ObjectDB;

namespace Takochu.ui
{
    public partial class AddObjectWindow : Form
    {
        private readonly IGameVersion _gameVer;
        public static List<AbstractObj> Objects { get; private set; }
        private Dictionary<string, Zone> _usedZones;
        public static bool IsChanged { get; private set; } = false;

        public static AbstractObj AddTargetObject = null;

        //public static bool IsAddObject;

        public AddObjectWindow(IGameVersion gameVer,Dictionary<string,Zone> usedZones)
        {
            InitializeComponent();

            _gameVer = gameVer;

            _usedZones = usedZones;

            foreach (var usedZoneName in usedZones) 
            {
                ZoneComboBox.Items.Add(usedZoneName.Value.ZoneName);
            }
                

            ZoneComboBox.SelectedIndex = 0;

            //if(!ObjectDataTreeView.Created)
            //var a = NewObjectDB.ObjectNodes;
            ObjectDataTreeView.Nodes.Clear();

            //Console.WriteLine(ObjectDataTreeView.Nodes);
            //if(ObjectDataTreeView.Nodes.Count < 1)

            TreeNode[] a = NewObjectDB.ObjectNodes;

            ObjectDataTreeView.Nodes.AddRange(a);

            


        }

        private void AddObjectButton_Click(object sender, EventArgs e)
        {
            if (ObjectDataTreeView.SelectedNode == null) 
            {
                MessageBox.Show("Select the object to be added.", "Error");
                return;
            }
            if (ObjectDataTreeView.SelectedNode.Tag is NewObjectDB.Object == false) return;

            string targetZoneName = ZoneComboBox.Text;
            string targetLyerName = LayerComboBox.Text;
            string targetLayerAndObjectType = $"Placement/{targetLyerName}/ObjInfo";


            AddTargetObject = new LevelObj((ObjectDataTreeView.SelectedNode.Tag as NewObjectDB.Object).FileName, _usedZones[targetZoneName], targetLayerAndObjectType);
            _usedZones[targetZoneName].mObjects["Map"][targetLyerName].Add(AddTargetObject);

            Objects = _usedZones[targetZoneName].mObjects["Map"][targetLyerName];

            IsChanged = true;

            

            Close();
        }

        private void ZoneComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var layers = _usedZones[ZoneComboBox.Text].GetLayersUsedOnZoneForCurrentScenario();

            if (LayerComboBox.Items.Count > 0)
                LayerComboBox.Items.Clear();

            foreach (var layer in layers) 
            {
                LayerComboBox.Items.Add(layer);
            }

            LayerComboBox.SelectedIndex = 0;
        }

        private void ObjectDataTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (ObjectDataTreeView.Nodes == null) return;
            if (ObjectDataTreeView.SelectedNode.Parent == null) return;
            if (ObjectDataTreeView.SelectedNode.Tag is NewObjectDB.Object == false) return;

            var objectNodeTag = ObjectDataTreeView.SelectedNode.Tag as NewObjectDB.Object;
            var objectInfo = NewObjectDB.Categories[(ushort)ObjectDataTreeView.SelectedNode.Parent.Index][objectNodeTag.FileName];

            textBox1.Text = objectInfo.Notes;
        }

        private void ObjectDataTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

        private void AddObjectWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            //このツリーノードの削除は必須なので消さないこと
            //消してしまうと二度目にこのウィンドウを開く際にエラーが発生します。
            ObjectDataTreeView.Nodes.Clear();
        }
    }
}
