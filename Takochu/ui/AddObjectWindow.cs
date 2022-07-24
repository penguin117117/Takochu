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
        public List<AbstractObj> Objects { get; private set; }
        private Dictionary<string, Zone> _usedZones;
        public static bool IsChanged { get; private set; } = false;
        

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

            //ObjectDataTreeView.BeginUpdate();
            //Console.WriteLine(MainWindow.ObjectDBTreeView.Created);



            //ObjectDataTreeView.EndUpdate();
            ObjectDataTreeView.Nodes.AddRange(NewObjectDB.ObjectNodes);
            //ObjectDataTreeView.BeginUpdate();

            //MessageBox.Show($"{ObjDB.ObjectTreeView.Nodes.Count}");
            //ObjDB.ObjectTreeView = ObjectDataTreeView;


            //ObjectDataTreeView.EndUpdate();
            //ObjectDataTreeView.Refresh();

            //foreach (string obj in ObjDB.Objects.Keys)
            //    ObjectDataTreeView.Nodes.Add(obj);

            //int objcount = ObjDB.Objects.Keys.Count;


            //for (int i = 0; i < objcount; i++) 
            //{

            //    ObjectDataTreeView.Nodes.Add(ObjDB.Objects.ElementAt(i).Key);
            //}


        }

        private void AddObjectButton_Click(object sender, EventArgs e)
        {
            if (ObjectDataTreeView.SelectedNode == null) 
            {
                MessageBox.Show("Select the object to be added.", "Error");
                return;
            }
                

            string targetZoneName = ZoneComboBox.Text;
            string targetLyerName = LayerComboBox.Text;
            string targetLayerAndObjectType = $"Placement/{targetLyerName}/ObjInfo";

            _usedZones[targetZoneName].mObjects["Map"][targetLyerName].Add(new LevelObj(ObjectDataTreeView.SelectedNode.Name, _usedZones[targetZoneName], targetLayerAndObjectType));

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
            //if (ObjectDataTreeView.Nodes == null) return;

            //var objectInfo = ObjectDataTreeView.SelectedNode.Tag as ObjectDB.Object;

            //textBox1.Text = objectInfo.Notes;

            if (ObjectDataTreeView.Nodes == null) return;
            var a = ObjectDataTreeView.SelectedNode.Tag as NewObjectDB.Object;
            var objectInfo = NewObjectDB.Categories[(ushort)ObjectDataTreeView.SelectedNode.Parent.Index][a.FileName];

            textBox1.Text = objectInfo.Notes;
            Console.WriteLine(objectInfo.Notes);
        }

        private void ObjectDataTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }
    }
}
