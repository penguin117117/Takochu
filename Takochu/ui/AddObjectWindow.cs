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

namespace Takochu.ui
{
    public partial class AddObjectWindow : Form
    {
        private readonly IGameVersion _gameVer;
        public List<AbstractObj> Objects { get; private set; }

        public AddObjectWindow(IGameVersion gameVer,Dictionary<string,Zone> usedZones)
        {
            InitializeComponent();

            _gameVer = gameVer;

            //"Map", "placement", "objinfo"
            usedZones["BigGalaxy"].AddObject("Map" , "Placement" ,"ObjInfo");
            Objects = usedZones["BigGalaxy"].mObjects["Map"]["Common"];
            //usedZones["BigGalaxy"].mObjects["Map"]["Common"].Add(new AbstractObj("Kuribo", usedZones["BigGalaxy"]));
        }

        private void AddObjectButton_Click(object sender, EventArgs e)
        {

        }
    }
}
