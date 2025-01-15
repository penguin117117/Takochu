using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.io;
using Takochu.smg;
using Takochu.smg.obj;
using Takochu.util;
using OpenTK;
using Takochu.smg.msg;
using Takochu.rnd;
using OpenTK.Graphics.OpenGL;
using static Takochu.util.RenderUtil;
using System.Runtime.InteropServices;
using Takochu.util.GameVer;
using System.Diagnostics;
using Takochu.smg.obj.ObjectSubData;
using System.CodeDom;
using Fasterflect;
using Octokit;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace Takochu.ui
{
    public partial class EditorWindow : Form
    {
        private readonly string _galaxyName;
        private int _currentScenario;
        private GalaxyScenario _galaxyScenario;
        private List<AbstractObj> _objects = new List<AbstractObj>();
        private List<PathObj> _paths = new List<PathObj>();

        private Dictionary<string, List<StageObj>> _galaxyZones = new Dictionary<string, List<StageObj>>();
        private List<string> _zonesUsed = new List<string>();

        private Dictionary<string, int> _ZoneMasks = new Dictionary<string, int>();

        private Dictionary<int, Dictionary<int, int>> _dispLists = new Dictionary<int, Dictionary<int, int>>();

        private AbstractObj _selectedObject;

        public readonly IGameVersion GameVersion;

        public EditorWindow(string galaxyName)
        {
            InitializeComponent();
            _galaxyName = galaxyName;

            GameVersion = GameUtil.IsSMG1() ? throw new Exception("現バージョンではSMG1をサポートしていません") : new SMG2();

            //SMG1 data cannot be saved in the current version.
            //現段階ではギャラクシー1のデータは保存できません。
            if (GameUtil.IsSMG1())
                SaveToolStripMenuItem.Enabled = false;


            foreach (string addObjectName in GameVersion.AddObjectList)
            {
                var toolStripMenuItem = new ToolStripMenuItem(addObjectName);
                toolsToolStripMenuItem.Name = addObjectName + toolsToolStripMenuItem.AccessibleName;
                toolsToolStripMenuItem.Text = addObjectName;
                AddObjectToolStripDropDownButton.DropDownItems.Add(toolStripMenuItem);
                toolStripMenuItem.Click += new EventHandler(AddObject_ToolStripItem_ObjectNameClick);
            }

            if (GameUtil.IsSMG2())
            {
                //foreach (SMG2_ObjectType item in Enum.GetValues(typeof(SMG2_ObjectType)))
                //{
                //    var toolStripMenuItem = new ToolStripMenuItem(item.ToString());
                //    toolsToolStripMenuItem.Name = item.ToString()+toolsToolStripMenuItem.AccessibleName;
                //    toolsToolStripMenuItem.Text = item.ToString();
                //    AddObjectToolStripDropDownButton.DropDownItems.Add(toolStripMenuItem);
                //    toolStripMenuItem.Click += new EventHandler(AddObject_ToolStripItem_ObjectNameClick);
                //}
            }

        }

        private void InitializeDispList()
        {
            _dispLists.Add(0, new Dictionary<int, int>());
            _dispLists.Add(1, new Dictionary<int, int>());
            _dispLists.Add(2, new Dictionary<int, int>());
        }

        private void ClearGLDisplayLists()
        {
            foreach (KeyValuePair<int, Dictionary<int, int>> disp in _dispLists)
            {
                foreach (KeyValuePair<int, int> actualList in disp.Value)
                {
                    GL.DeleteLists(actualList.Value, 1);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            //base.OnLoad(e);



            _galaxyScenario = Program.sGame.OpenGalaxy(_galaxyName);

            GalaxyNameTxtBox.Text = _galaxyScenario.mHolderName;
            AreaToolStripMenuItem.Checked = Properties.Settings.Default.EditorWindowDisplayArea;
            pathsToolStripMenuItem.Checked = Properties.Settings.Default.EditorWindowDisplayPath;
            _pickingFrameBuffer = new uint[9];
            InitializeDispList();

            foreach (KeyValuePair<int, ScenarioEntry> scenarioBCSV_Entry in _galaxyScenario.ScenarioARC.ScenarioDataBCSV)
            {
                ScenarioEntry senario = scenarioBCSV_Entry.Value;
                TreeNode treeNode = new TreeNode($"[{senario.ScenarioNo}] {senario.ScenarioName}")
                {
                    Tag = senario.ScenarioNo
                };

                scenarioTreeView.Nodes.Add(treeNode);
            }

            //if (!BGMInfo.HasBGMInfo(mGalaxy.mName))
            //    stageInformationBtn.Enabled = false;
        }

        public void LoadScenario(int scenarioNo)
        {
            _areChanges = false;
            _galaxyZones.Clear();
            _zonesUsed.Clear();
            _ZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            zonesListTreeView.Nodes.Clear();
            lightsTreeView.Nodes.Clear();
            cameraListTreeView.Nodes.Clear();

            _paths.Clear();

            _objects.Clear();

            ClearGLDisplayLists();
            _dispLists.Clear();
            InitializeDispList();

            _galaxyScenario.SetScenario(scenarioNo);
            scenarioNameTxtBox.Text = _galaxyScenario.mCurScenarioName;

            string mainGalaxyName = _galaxyScenario.mName;

            // first we need to get the proper layers that the galaxy itself uses
            // ギャラクシーの対象のシナリオで使用される全てのレイヤーを取得する。
            int layerMaskBitPatternNo = _galaxyScenario.GetMaskUsedInZoneOnCurrentScenario(mainGalaxyName);
            List<string> layers = GameUtil.GetGalaxyLayers(layerMaskBitPatternNo);

            layers.ForEach(layerName => layerViewerDropDown.DropDownItems.Add(layerName));

            Zone mainGalaxyZone = _galaxyScenario.GetZone(mainGalaxyName);

            // now we get the zones used on these layers
            // add our galaxy name itself so we can properly add it to a scene list with the other zones
            _zonesUsed.Add(mainGalaxyName);
            _zonesUsed.AddRange(mainGalaxyZone.GetZonesUsedOnLayers(layers));

            Dictionary<string, List<Camera>> cameras = new Dictionary<string, List<Camera>>();
            List<Light> lights = new List<Light>();

            List<string> currentLayers = new List<string>();

            _objects.AddRange(mainGalaxyZone.GetAllObjectsFromLayers(layers));

            foreach (string zoneName in _zonesUsed)
            {
                _ZoneMasks.Add(zoneName, _galaxyScenario.GetMaskUsedInZoneOnCurrentScenario(zoneName));

                TreeNode zoneNode = new TreeNode()
                {
                    Text = zoneName,
                    Name = zoneName
                };

                AssignNodesToZoneNode(ref zoneNode);
                currentLayers.AddRange(GameUtil.GetGalaxyLayers(_ZoneMasks[zoneName]));

                objectsListTreeView.Nodes.Add(zoneNode);

                Zone zone = _galaxyScenario.GetZone(zoneName);

                if (zone.mLights != null)
                {
                    TreeNode lightZoneNode = new TreeNode(zone.ZoneName);

                    foreach (Light light in zone.mLights)
                    {
                        TreeNode lightNode = new TreeNode(light.mName);
                        lightNode.Tag = light;
                        lightZoneNode.Nodes.Add(lightNode);
                    }

                    lightsTreeView.Nodes.Add(lightZoneNode);
                }


                ZoneAttributes attrs = _galaxyScenario.GetZone(zoneName).mAttributes;

                if (attrs != null)
                {
                    //tabControl1.TabPages[1].Enabled = true;
                    TreeNode zoneInfoNode = new TreeNode(zoneName);
                    zonesListTreeView.Nodes.Add(zoneInfoNode);
                    zoneInfoNode.Nodes.Add("Shadow Parameters");
                    zoneInfoNode.Nodes.Add("Water Parameters");
                    zoneInfoNode.Nodes.Add("Flags");

                    foreach (ZoneAttributes.ShadowParam prm in attrs.mShadowParams)
                    {
                        TreeNode n = new TreeNode(prm.ToString());
                        n.Tag = prm;
                        zoneInfoNode.Nodes[0].Nodes.Add(n);
                    }

                    foreach (ZoneAttributes.WaterCameraParam prm in attrs.mWaterParams)
                    {
                        TreeNode n = new TreeNode(prm.ToString());
                        n.Tag = prm;
                        zoneInfoNode.Nodes[1].Nodes.Add(n);
                    }

                    foreach (ZoneAttributes.FlagNameTable prm in attrs.mFlagTable)
                    {
                        TreeNode n = new TreeNode(prm.ToString());
                        n.Tag = prm;
                        zoneInfoNode.Nodes[2].Nodes.Add(n);
                    }

                    //zonesListTreeView.Nodes.Add(zoneInfoNode);
                }
                else
                {
                    //tabControl1.TabPages[1].Enabled = false;
                }


                if (GameUtil.IsSMG1())
                    currentLayers = currentLayers.ConvertAll(l => l.ToLower());

                List<AbstractObj> objs = zone.GetAllObjectsFromLayers(currentLayers);

                _paths.AddRange(zone.mPaths);

                cameras.Add(zoneName, zone.mCameras);

                if (zone.mLights != null)
                    lights.AddRange(zone.mLights);

                //mCurrentLayers = GameUtil.GetGalaxyLayers(zoneMasks[mGalaxy.mName]);

                // the main galaxy is always loaded before we get into this block
                // so we can do all of our offsetting here
                // このブロックに入る前に、メイン銀河は常にロードされています。
                // オフセットはすべてここで行うことができます。

                var currentScenarioZoneLayerNames = zone.GetLayersUsedOnZoneForCurrentScenario();
                if (!zone.mIsMainGalaxy)
                {
                    if (GameUtil.IsSMG1())
                        currentLayers = currentLayers.ConvertAll(l => l.ToLower());

                    _objects.AddRange(zone.GetAllObjectsFromLayers(currentScenarioZoneLayerNames));
                    //mObjects.AddRange(z.GetAllObjectsFromLayers(currentLayers));
                    //mGalaxy
                    //var layername = z.GetLayersUsedOnZoneForCurrentScenario()[z.mGalaxy.mScenarioNo];
                    //mObjects.AddRange(z.GetObjectsFromLayer("Map",layername)) ;
                    //Console.WriteLine(layername);
                }
                else
                {
                    foreach (string layer in currentScenarioZoneLayerNames)
                    {
                        List<StageObj> stgs;

                        if (mainGalaxyZone.mHasStageObjList.ContainsKey(layer))
                        {
                            stgs = mainGalaxyZone.mHasStageObjList[layer];
                        }
                        else if (mainGalaxyZone.mHasStageObjList.ContainsKey(layer.ToLower()))
                        {
                            stgs = mainGalaxyZone.mHasStageObjList[layer.ToLower()];
                        }
                        else
                        {
                            throw new Exception("EditorWindow::LoadScenario -- Invalid layers");
                        }

                        _galaxyZones.Add(layer, stgs);
                    }
                }
            }

            foreach (string zone in _zonesUsed)
            {
                TreeNode cameraZoneNode = new TreeNode(zone);
                PopulateCameraTreeNode(ref cameraZoneNode);

                cameras[zone].ForEach(camera =>
                {
                    if (Enum.IsDefined(typeof(Camera.CameraType), camera.GetCameraType()))
                    {
                        TreeNode nd = new TreeNode(camera.mName)
                        {
                            Tag = camera
                        };
                        cameraZoneNode.Nodes[(int)camera.GetCameraType()].Nodes.Add(nd);
                    }
                });

                cameraListTreeView.Nodes.Add(cameraZoneNode);
            }

            PopulateTreeView();

            RenderObjectLists(RenderMode.Picking);
            RenderObjectLists(RenderMode.Opaque);

            attrFinderToolStripMenuItem.Enabled = true;
        }

        private void PopulateCameraTreeNode(ref TreeNode node)
        {
            node.Nodes.Add("Cube Cameras");
            node.Nodes.Add("Group Cameras");
            node.Nodes.Add("Event Cameras");
            node.Nodes.Add("Start Cameras");
            node.Nodes.Add("Other Cameras");
        }

        private void AddObject_ToolStripItem_ObjectNameClick(object sender, EventArgs e)
        {
            if (scenarioTreeView.SelectedNode == null)
            {
                Translate.GetMessageBox.Show(MessageBoxText.ScenarioNotSelected, MessageBoxCaption.Info);
                return;
            }

            var toolStripMenuItem = sender as ToolStripMenuItem;
            Console.WriteLine(toolStripMenuItem.Text);

            AddObjectWindow addObjectWindow = new AddObjectWindow(GameVersion, _galaxyScenario.GetZones());
            addObjectWindow.ShowDialog();

            //オブジェクトが追加されていない場合はこの処理を実行しないようにする。
            //if (AddObjectWindow.IsChanged)
            //{
            //    var objCount = addObjectWindow.Objects.Count();
            //    mObjects.Add(addObjectWindow.Objects[objCount - 1]);
            //    Scenario_ReLoad();
            //}

            //この下にオブジェクト追加後に
            //フォームを閉じるボタンを押したときに警告が出るようにするためのコードを書く必要がある

        }

        private void scenarioTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }

        private void scenarioTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (scenarioTreeView.SelectedNode != null)
            {
                _currentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);

                applyGalaxyNameBtn.Enabled = true;
                LoadScenario(_currentScenario);

                if (_galaxyScenario.GetMainGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{_currentScenario}.canm"))
                    introCameraEditorBtn.Enabled = true;
                else
                    introCameraEditorBtn.Enabled = false;
            }

            _galaxyScenario.GetMainGalaxyZone().LoadCameras();

            UpdateCamera();
            glLevelView.Refresh();
        }

        private void EditorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {

            //オブジェクトのプロパティに変更がある場合警告を表示します
            //Display a warning when there are changes to the object's properties
            DialogResult dr;
            if (EditorWindowSys.DataGridViewEdit.IsChanged || _areChanges)
            {
                dr = Translate.GetMessageBox.Show(MessageBoxText.ChangesNotSaved, MessageBoxCaption.Error, MessageBoxButtons.YesNo);
                if ((dr == DialogResult.No) || (dr == DialogResult.Cancel))
                {
                    e.Cancel = true;
                    return;
                }
            }

            _galaxyScenario.Close();
        }

        private void EditorWindow_Load(object sender, EventArgs e)
        {

        }

        private void galaxyViewControl_Load(object sender, EventArgs e)
        {

        }
        private void openMsgEditorButton_Click(object sender, EventArgs e)
        {
            MessageEditor editor = new MessageEditor(ref _galaxyScenario);
            editor.Show();
        }

        private void saveGalaxyBtn_Click(object sender, EventArgs e)
        {

        }


        private void closeEditorBtn_Click(object sender, EventArgs e)
        {



        }

        private void stageInformationBtn_Click(object sender, EventArgs e)
        {
            StageInfoEditor stageInfo = new StageInfoEditor(ref _galaxyScenario, _currentScenario);
            stageInfo.ShowDialog();
        }

        private void applyGalaxyNameBtn_Click(object sender, EventArgs e)
        {
            string galaxy_lbl = $"GalaxyName_{_galaxyScenario.mName}";
            string scenario_lbl = $"ScenarioName_{_galaxyScenario.mName}{_currentScenario}";

            NameHolder.AssignToGalaxy(galaxy_lbl, GalaxyNameTxtBox.Text);
            NameHolder.AssignToScenario(scenario_lbl, scenarioNameTxtBox.Text);
        }

        private void introCameraEditorBtn_Click(object sender, EventArgs e)
        {
            IntroEditor intro = new IntroEditor(ref _galaxyScenario);
            intro.Show();
        }

        private void Scenario_ReLoad()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (scenarioTreeView.SelectedNode != null)
            {
                _currentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);
                applyGalaxyNameBtn.Enabled = true;
                LoadScenario(_currentScenario);

                if (_galaxyScenario.GetMainGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{_currentScenario}.canm"))
                    introCameraEditorBtn.Enabled = true;
                else
                    introCameraEditorBtn.Enabled = false;
            }
            _galaxyScenario.GetMainGalaxyZone().LoadCameras();
            UpdateCamera();
            glLevelView.Refresh();
            sw.Stop();
            Console.WriteLine("ScenarioReLoad: " + $"{sw.Elapsed}");
        }

        private int GetIndexOfZoneNode(string name)
        {
            return objectsListTreeView.Nodes.IndexOf(objectsListTreeView.Nodes[name]);
        }

        private void AssignNodesToZoneNode(ref TreeNode node)
        {
            node.Nodes.Add("Areas");
            node.Nodes.Add("Camera Areas");
            node.Nodes.Add("Objects");
            node.Nodes.Add("Gravity");
            node.Nodes.Add("Debug Movement");
            node.Nodes.Add("Positions");
            node.Nodes.Add("Demos");
            node.Nodes.Add("Starting Points");
            node.Nodes.Add("Map Parts");
            node.Nodes.Add("Paths");
        }

        private int GetNodeIndexOfObject(string type)
        {
            switch (type)
            {
                case "AreaObj":
                    return 0;
                case "CameraObj":
                    return 1;
                case "Obj":
                    return 2;
                case "PlanetObj":
                    return 3;
                case "DebugMoveObj":
                    return 4;
                case "GeneralPosObj":
                    return 5;
                case "DemoObj":
                    return 6;
                case "StartObj":
                    return 7;
                case "MapPartsObj":
                    return 8;
            }

            return -1;
        }

        private void PopulateTreeView()
        {
            foreach (AbstractObj o in _objects)
            {
                string zone = o.mParentZone.ZoneName;
                int idx = GetIndexOfZoneNode(zone);
                TreeNode zoneNode = objectsListTreeView.Nodes[idx];

                if (!o.mParentZone.ZoneName.EndsWith("Galaxy") && zoneNode.Tag == null)
                {
                    var layers = _galaxyScenario.GetMainGalaxyZone().GetAllStageDataFromLayers(_galaxyScenario.GetMainGalaxyZone().GetLayersUsedOnZoneForCurrentScenario());
                    zoneNode.Tag = layers.Find(stage => stage.mName == zoneNode.Text) as StageObj;
                }

                TreeNode objNode = new TreeNode()
                {
                    Text = o.ToString(),
                    Tag = o
                };

                /* indicies of nodes
                 * 0 = Areas
                 * 1 = Camera Areas
                 * 2 = Objects
                 * 3 = Gravity
                 * 4 = Debug Movement
                 * 5 = General Position
                 * 6 = Demos
                 * 7 = Starting Points
                 * 8 = Map Parts
                 * 9 = Paths
                 */

                int nodeIdx = GetNodeIndexOfObject(o.mType);
                //Console.WriteLine("zone  " + zone + "_______" + idx+"______"+nodeIdx);
                zoneNode.Nodes[nodeIdx].Nodes.Add(objNode);
            }

            // path nodes are a little different, so
            foreach (PathObj o in _paths)
            {
                string zone = o.mParentZone.ZoneName;
                int idx = GetIndexOfZoneNode(zone);
                TreeNode zoneNode = objectsListTreeView.Nodes[idx];

                TreeNode pathNode = new TreeNode()
                {
                    Text = o.ToString(),
                    Tag = o
                };

                int curIdx = 0;

                foreach (PathPointObj pobj in o.mPathPointObjs)
                {
                    TreeNode ppNode = new TreeNode()
                    {
                        Text = $"Point {curIdx}",
                        Tag = pobj
                    };

                    pathNode.Nodes.Add(ppNode);
                    curIdx++;
                }

                zoneNode.Nodes[9].Nodes.Add(pathNode);
            }
        }

        #region rendering code

        private const float FOV = (float)((70f * Math.PI) / 180f);
        private const float Z_NEAR = 0.01f;
        private const float Z_FAR = 1000f;

        private bool _glLoaded;
        private float _aspectRatio;
        private Vector2 _camRotation; //X is vertical?, Y is Horizontal?.
        private Vector3 _camPosition;
        private Vector3 _camTarget;
        private float _camDistance;
        private bool _upsideDown;

        //_skyboxMatrix は代入されているが使用されていない
        //ToDo:不要な計算の場合は削除する必要がある。
        private Matrix4 _camMatrix, _skyboxMatrix, _projMatrix;

        //下記の値は代入されているが使用されていない
        //ToDo:不要な計算の場合は削除する必要がある。
        private RenderInfo _renderInfo;

        private MouseButtons _mouseDown;
        private Point _lastMouseMove, _lastMouseClick;
        private Point _mouseCoords;

        //下記2つの値は代入されているが使用されていない
        //ToDo:不要な計算の場合は削除する必要がある。
        private float _pixelFactorX, _pixelFactorY;

        private uint[] _pickingFrameBuffer;
        private float _pickingDepth;

        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Objects;

        /// <summary>
        /// XXXXGalaxyMap.arc/Stage/camera/CameraParam.bcamを編集するためのデータグリッドビュー
        /// </summary>
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_CameraParam;
        /// <summary>
        /// XXXXGalaxyScenario.arc/XXXXGalaxyScenario/ZoneList.bcsvを編集するためのデータグリッドビュー
        /// </summary>
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Zones;
        /// <summary>
        /// XXXXGalaxyLight.arc/Stage/csv/XXXXGalaxyLight.bcsvを編集するためのデータグリッドビュー
        /// </summary>
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Lights;

        /// <summary>
        /// エディターウィンドウでオブジェクトを変更したかを判別します。<br/>
        /// この値は主に保存が必要かの判別に使用されます。
        /// </summary>
        private bool _areChanges;

        // befor name is "reguler stage"
        private void RenderGalaxyObjectLists(RenderMode renderMode)
        {
            List<AbstractObj> regularObjs = _objects.FindAll(o => o.mParentZone.ZoneName == _galaxyScenario.mName);
            List<PathObj> regularPaths = _paths.FindAll(p => p.mParentZone.ZoneName == _galaxyScenario.mName);

            foreach (AbstractObj abstractObj in regularObjs)
            {
                //LevelObj level = o as LevelObj;
                //Console.WriteLine("test "+ o.mName);
                Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                if (_dispLists[(int)renderMode].ContainsKey(abstractObj.mUnique))
                    continue;

                keyValuePairs.Add(abstractObj.mUnique, GL.GenLists(1));
                _dispLists[(int)renderMode].Add(abstractObj.mUnique, GL.GenLists(1));

                if (abstractObj.mType == "AreaObj" && AreaToolStripMenuItem.Checked == false && renderMode != RenderMode.Picking)
                    continue;

                GL.NewList(_dispLists[(int)renderMode][abstractObj.mUnique], ListMode.Compile);

                GL.PushMatrix();

                if (renderMode == RenderMode.Picking)
                {
                    GL.Color4((byte)abstractObj.mPicking.R, (byte)abstractObj.mPicking.G, (byte)abstractObj.mPicking.B, (byte)0xFF);
                }

                abstractObj.Render(renderMode);
                GL.PopMatrix();

                GL.EndList();
            }

            foreach (PathObj pathObj in regularPaths)
            {
                Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                if (_dispLists[(int)renderMode].ContainsKey(pathObj.mUnique))
                    continue;

                keyValuePairs.Add(pathObj.mUnique, GL.GenLists(1));
                _dispLists[(int)renderMode].Add(pathObj.mUnique, GL.GenLists(1));

                if (pathsToolStripMenuItem.Checked == false && renderMode != RenderMode.Picking)
                    continue;

                GL.NewList(_dispLists[(int)renderMode][pathObj.mUnique], ListMode.Compile);

                GL.PushMatrix();

                pathObj.Render(renderMode);

                GL.PopMatrix();

                GL.EndList();
            }

        }
        private void RenderZoneObjectLists(RenderMode renderMode, List<StageObj> stageObjLayers)
        {// Zone processing.
            foreach (StageObj stageObj in stageObjLayers)
            {
                // Lambda
                List<AbstractObj> objsInStage = _objects.FindAll(o => o.mParentZone.ZoneName == stageObj.mName);
                List<PathObj> pathsInStage = _paths.FindAll(p => p.mParentZone.ZoneName == stageObj.mName);

                foreach (AbstractObj abstractObj in objsInStage)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (_dispLists[(int)renderMode].ContainsKey(abstractObj.mUnique))
                        continue;

                    keyValuePairs.Add(abstractObj.mUnique, GL.GenLists(1));
                    _dispLists[(int)renderMode].Add(abstractObj.mUnique, GL.GenLists(1));

                    if (abstractObj.mType == "AreaObj" && AreaToolStripMenuItem.Checked == false && renderMode != RenderMode.Picking)
                        continue;

                    GL.NewList(_dispLists[(int)renderMode][abstractObj.mUnique], ListMode.Compile);

                    GL.PushMatrix();

                    {
                        // Zone to Global
                        // x: 0 0 1
                        // y: 0 1 0
                        // z: 1 0 0
                        GL.Translate(stageObj.mPosition);
                        GL.Rotate(stageObj.mRotation.Z, 0f, 0f, 1f);
                        GL.Rotate(stageObj.mRotation.Y, 0f, 1f, 0f);
                        GL.Rotate(stageObj.mRotation.X, 1f, 0f, 0f);
                    }

                    if (renderMode == RenderMode.Picking)
                    {
                        GL.Color4((byte)abstractObj.mPicking.R, (byte)abstractObj.mPicking.G, (byte)abstractObj.mPicking.B, (byte)0xFF);
                    }

                    abstractObj.Render(renderMode);
                    GL.PopMatrix();

                    GL.EndList();
                }

                foreach (PathObj pathObj in pathsInStage)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (_dispLists[(int)renderMode].ContainsKey(pathObj.mUnique))
                        continue;

                    keyValuePairs.Add(pathObj.mUnique, GL.GenLists(1));
                    _dispLists[(int)renderMode].Add(pathObj.mUnique, GL.GenLists(1));

                    if (pathsToolStripMenuItem.Checked == false && renderMode != RenderMode.Picking)
                        continue;

                    GL.NewList(_dispLists[(int)renderMode][pathObj.mUnique], ListMode.Compile);

                    GL.PushMatrix();
                    {
                        // Zone to Global
                        // x: 0 0 1
                        // y: 0 1 0
                        // z: 1 0 0
                        GL.Translate(stageObj.mPosition);
                        GL.Rotate(stageObj.mRotation.Z, 0f, 0f, 1f);
                        GL.Rotate(stageObj.mRotation.Y, 0f, 1f, 0f);
                        GL.Rotate(stageObj.mRotation.X, 1f, 0f, 0f);
                    }
                    pathObj.Render(renderMode);
                    GL.PopMatrix();

                    GL.EndList();
                }
            }
        }
        private void RenderObjectLists(RenderMode renderMode)
        {
            // Get layers that is used "galaxy".
            var ScenarioLayers = _galaxyScenario.GetMainGalaxyZone().GetLayersUsedOnZoneForCurrentScenario();
            // get zone that is used scenario.
            List<StageObj> stageObjLayers = _galaxyScenario.GetMainGalaxyZone().GetAllStageDataFromLayers(ScenarioLayers);

            // TODO: If mistake rendering then try it to flip.
            // before: Zone -> Galaxy(Regular stage).
            // refactored: Galaxy(Reguler stage) ->Zone.
            RenderGalaxyObjectLists(renderMode);
            RenderZoneObjectLists(renderMode, stageObjLayers);
        }

        private void UpdateViewport()
        {
            GL.Viewport(glLevelView.ClientRectangle);

            _aspectRatio = (float)glLevelView.Width / (float)glLevelView.Height;
            GL.MatrixMode(MatrixMode.Projection);
            _projMatrix = Matrix4.CreatePerspectiveFieldOfView(FOV, _aspectRatio, Z_NEAR, Z_FAR);
            GL.LoadMatrix(ref _projMatrix);


            _pixelFactorX = ((2f * (float)Math.Tan(FOV / 2f) * _aspectRatio) / (float)(glLevelView.Width));
            _pixelFactorY = ((2f * (float)Math.Tan(FOV / 2f)) / (float)(glLevelView.Height));
        }

        private void UpdateCamera()
        {
            Vector3 up;

            if (Math.Cos(_camRotation.Y) < 0)
            {
                _upsideDown = true;
                up = new Vector3(0.0f, -1.0f, 0.0f);
            }
            else
            {
                _upsideDown = false;
                up = new Vector3(0.0f, 1.0f, 0.0f);
            }

            _camPosition.X = _camDistance * (float)Math.Cos(_camRotation.X) * (float)Math.Cos(_camRotation.Y);
            _camPosition.Y = _camDistance * (float)Math.Sin(_camRotation.Y);
            _camPosition.Z = _camDistance * (float)Math.Sin(_camRotation.X) * (float)Math.Cos(_camRotation.Y);

            Vector3 skybox_target;
            skybox_target.X = -(float)Math.Cos(_camRotation.X) * (float)Math.Cos(_camRotation.Y);
            skybox_target.Y = -(float)Math.Sin(_camRotation.Y);
            skybox_target.Z = -(float)Math.Sin(_camRotation.X) * (float)Math.Cos(_camRotation.Y);

            Vector3.Add(ref _camPosition, ref _camTarget, out _camPosition);

            _camMatrix = Matrix4.LookAt(_camPosition, _camTarget, up);
            _skyboxMatrix = Matrix4.LookAt(Vector3.Zero, skybox_target, up);
            _camMatrix = Matrix4.Mult(Matrix4.CreateScale(0.0001f), _camMatrix);
        }

        private void UpdateCamera(Vector3 v3)
        {
            Vector3 up;
            _camRotation = new Vector2(v3.X, v3.Y);
            if (Math.Cos(_camRotation.Y) < 0)
            {
                _upsideDown = true;
                up = new Vector3(0.0f, -1.0f, 0.0f);
            }
            else
            {
                _upsideDown = false;
                up = new Vector3(0.0f, 1.0f, 0.0f);
            }

            _camPosition.X = _camDistance * (float)Math.Cos(_camRotation.X) * (float)Math.Cos(_camRotation.Y);
            _camPosition.Y = _camDistance * (float)Math.Sin(_camRotation.Y);
            _camPosition.Z = _camDistance * (float)Math.Sin(v3.Z) * (float)Math.Cos(_camRotation.Y);

            Vector3 skybox_target = v3;
            //skybox_target.X = -(float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            //skybox_target.Y = -(float)Math.Sin(m_CamRotation.Y);
            //skybox_target.Z = -(float)Math.Sin(v3.Z) * (float)Math.Cos(m_CamRotation.Y);

            Vector3.Add(ref _camPosition, ref _camTarget, out _camPosition);

            _camMatrix = Matrix4.LookAt(_camPosition, _camTarget, up);
            _skyboxMatrix = Matrix4.LookAt(Vector3.One, skybox_target, up);
            _camMatrix = Matrix4.Mult(Matrix4.CreateScale(0.0001f), _camMatrix);
        }

        [Obsolete]
        private void glLevelView_Paint(object sender, PaintEventArgs e)
        {
            if (!_glLoaded) return;
            glLevelView.MakeCurrent();

            /* step one -- fakecolor rendering */
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.ClearDepth(1f);
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref _camMatrix);

            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ColorLogicOp);
            GL.Disable(EnableCap.Dither);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.PolygonSmooth);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Lighting);

            foreach (KeyValuePair<int, Dictionary<int, int>> disp in _dispLists)
            {
                if (disp.Key != 2)
                    continue;

                foreach (KeyValuePair<int, int> actualList in disp.Value)
                    GL.CallList(actualList.Value);
            }

            GL.DepthMask(true);
            GL.Flush();

            GL.ReadPixels(_mouseCoords.X - 1, glLevelView.Height - _mouseCoords.Y + 1, 3, 3, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, _pickingFrameBuffer);
            GL.ReadPixels(_mouseCoords.X, glLevelView.Height - _mouseCoords.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref _pickingDepth);
            _pickingDepth = -(Z_FAR * Z_NEAR / (_pickingDepth * (Z_FAR - Z_NEAR) - Z_FAR));

            /* actual rendering */
            GL.DepthMask(true);
            GL.ClearColor(0.0f, 0.0f, 0.125f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Dither);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.PolygonSmooth);

            GL.LoadMatrix(ref _camMatrix);

            GL.Begin(BeginMode.Lines);
            GL.Color4(1f, 0f, 0f, 1f);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(100000f, 0f, 0f);
            GL.Color4(0f, 1f, 0f, 1f);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0, 100000f, 0f);
            GL.Color4(0f, 0f, 1f, 1f);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, 0f, 100000f);
            GL.End();

            GL.Color4(1f, 1f, 1f, 1f);

            _dispLists[0].Values.ToList().ForEach(l => GL.CallList(l));
            _dispLists[1].Values.ToList().ForEach(l => GL.CallList(l));

            glLevelView.SwapBuffers();
        }

        private void glLevelView_MouseMove(object sender, MouseEventArgs e)
        {
            float xdelta = (float)(e.X - _lastMouseMove.X);
            float ydelta = (float)(e.Y - _lastMouseMove.Y);

            //Console.WriteLine($"{m_PickingFrameBuffer[0]}");

            _mouseCoords = e.Location;
            _lastMouseMove = e.Location;

            if (_mouseDown != MouseButtons.None)
            {
                if (_mouseDown == MouseButtons.Right)
                {
                    if (_upsideDown)
                        xdelta = -xdelta;

                    _camRotation.X -= xdelta * 0.002f;
                    _camRotation.Y -= ydelta * 0.002f;
                }
                else if (_mouseDown == MouseButtons.Left)
                {
                    xdelta *= 0.005f;
                    ydelta *= 0.005f;

                    _camTarget.X -= xdelta * (float)Math.Sin(_camRotation.X);
                    _camTarget.X -= ydelta * (float)Math.Cos(_camRotation.X) * (float)Math.Sin(_camRotation.Y);
                    _camTarget.Y += ydelta * (float)Math.Cos(_camRotation.Y);
                    _camTarget.Z += xdelta * (float)Math.Cos(_camRotation.X);
                    _camTarget.Z -= ydelta * (float)Math.Sin(_camRotation.X) * (float)Math.Sin(_camRotation.Y);
                }

                UpdateCamera();
            }

            glLevelView.Refresh();
        }

        /// <summary>
        /// レイの交点情報とオブジェクトを保存したクラス。
        /// </summary>
        class CollisionInfo {
            public Vector3? nearestHitpointPosition;
            public float nearestHitpointDistance;
            public AbstractObj abstructObj;

            public CollisionInfo() {
                nearestHitpointPosition = null;
                // 最大値から小なり比較による計算のためMaxValue。
                nearestHitpointDistance = float.MaxValue;
                abstructObj = null;
            }
            public static bool operator <(CollisionInfo a, CollisionInfo b)
            {
                if (a.nearestHitpointDistance < b.nearestHitpointDistance)
                    return true;
                return false;
            }
            public static bool operator >(CollisionInfo a, CollisionInfo b)
            {
                if (a.nearestHitpointDistance > b.nearestHitpointDistance)
                    return true;
                return false;
            }
        };

        /// <summary>
        /// オブジェクトとレイの交点とそのオブジェクトを計算します。
        /// </summary>
        /// <param name="collisionInfo"></param>
        /// <param name="ray"></param>
        /// <param name="objList"></param>
        /// <param name="zonePos"></param>
        /// <param name="zoneRotMatrix"></param>
        /// <returns></returns>
        private CollisionInfo ObjectCollision(CollisionInfo collisionInfo, Ray ray, IReadOnlyCollection<AbstractObj> objList, Vector3? zonePos, Matrix3? zoneRotMatrix)
        {
            // 三角面の取得の際、順番または三角面化していない箇所があるため現状では選択できないことがあります。
            // テストの際はちゃんとテストで出力している三角面で、描画されている面を選択して下さい。
            // TODO: ゾーン回転軸の情報が不完全であるため、ゾーンオブジェクトは選択できません。

            // ポリゴン描画によるテストをする際はこの関数のコメントを外して下さい。
            // glLevelView.SwapBuffers();

            //カメラのRayと三角面の位置情報から交差判定を行う。
            // クリックしたオブジェクトが含む三角面の数だけ繰り返す
            float t, u, v;
            foreach (AbstractObj obj in objList)
            {
                // 特殊処理。
                Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                // モデルがなければスキップ
                if (obj.mRenderer == null)
                {
                    continue;
                }
                BMDInfo.BMDTriangleData bmdTriangleData = obj.mRenderer.GetTriangles();
                //// 要素がなければ処理をスキップ。
                //if (!(obj.mRenderer is BmdRenderer))
                //{
                //    continue;
                //}

                // obj.mTruePositionは100倍された値となっている模様。
                var globalPos = obj.mTruePosition;
                var globalRotMat = new Matrix3();
                var scaleMat = new Matrix3(
                    new Vector3(obj.mScale.X, 0.0f, 0.0f),
                    new Vector3(0.0f, obj.mScale.Y, 0.0f),
                    new Vector3(0.0f, 0.0f, obj.mScale.Z));

                if (zonePos == null)
                {
                    globalRotMat = GetRotMatrix3SmgCoordObject((obj.mRotation * (float)Math.PI) / 180.0f);
                }
                else
                {
                    globalPos = (globalPos * (Matrix3)zoneRotMatrix) + (Vector3)zonePos;
                    globalRotMat = GetRotMatrix3SmgZoneCoordObject((obj.mRotation * (float)Math.PI) / 180.0f) * (Matrix3)zoneRotMatrix;
                }

                foreach (var triangle in bmdTriangleData.TriangleDataList)
                {
                    // 三角面は座標軸回転、位置ともに計算されていないため、ここで計算します。
                    Vector3 v0 = Vector3.Add(globalPos, triangle.V0.Xyz * scaleMat * globalRotMat);
                    Vector3 v1 = Vector3.Add(globalPos, triangle.V1.Xyz * scaleMat * globalRotMat);
                    Vector3 v2 = Vector3.Add(globalPos, triangle.V2.Xyz * scaleMat * globalRotMat);
                    var v0Tov1 = v1 - v0;
                    var v0Tov2 = v2 - v0;
                    Vector3 normal_vector = Vector3.Normalize(Vector3.Cross(v0Tov1, v0Tov2));
                    // マルチスレッド処理ではレンダリングできません。
//#if DEBUG
//                    {
//                        // drow test polygon.
//                        GL.PushMatrix();
//                        GL.Begin(BeginMode.Polygon);
//                        GL.Vertex3(v0);
//                        GL.Vertex3(v1);
//                        GL.Vertex3(v2);
//                        GL.End();
//                        GL.PopMatrix();
//                    }
//#endif
                    // 交差点の位置ベクトルは Ray.org + t * Ray.dir = v0 + u(v1-v0) + v(v2-v0)
                    var camOriginToBasePos = ray.Origin - v0;
                    var common = 1.0f / Vector3.Dot(Vector3.Cross(ray.Direction, v0Tov2), v0Tov1);
                    t = common * Vector3.Dot(Vector3.Cross(camOriginToBasePos, v0Tov1), v0Tov2);
                    u = common * Vector3.Dot(Vector3.Cross(ray.Direction, v0Tov2), camOriginToBasePos);
                    v = common * Vector3.Dot(Vector3.Cross(camOriginToBasePos, v0Tov1), ray.Direction);

                    // この条件に引っかかればその三角面とは交差していない
                    // 三角面を含む平面について，レイと平面の交点は三角面の外側 もしくは レイが三角面の裏面から入射
                    if (((u < 0.0f || v < 0.0f) || (u + v > 1.0f)) || (Vector3.Dot(ray.Direction, normal_vector) >= 0.0f))
                    {
                        //Debug.WriteLine($"DEBUG: the position you clicked is t:{t} u:{u} v:{v}");
                        continue;
                    }


                    // 三角面とマウスクリックした点におけるカメラの視線は交差している

                    // これまでの交差点において最も近い交差点を確認する．
                    // この条件に引っかかればこれまでの任意の交差点よりも近くの交差点である．
                    if (t < collisionInfo.nearestHitpointDistance)
                    {
                        collisionInfo.nearestHitpointDistance = t;
                        //元のコード
                        //nearest_hitpoint_position = rayTest1.Origin + t * rayTest1.Direction;
                        collisionInfo.nearestHitpointPosition = Vector3.Add(ray.Origin, Vector3.Multiply(ray.Direction, t));
                        collisionInfo.abstructObj = obj;
                    }
                }
            }
            return collisionInfo;
        }

        /// <summary>
        /// レイ交点判定用の引数クラス
        /// </summary>
        private class ObjectCollisionArgPackage
        {
            public Vector3? zonePos = new Vector3?();
            public Matrix3? zoneRotMat = new Matrix3?();
            public List<AbstractObj> zoneObjs = new List<AbstractObj>();

            public ObjectCollisionArgPackage(Vector3? a, Matrix3? b, List<AbstractObj> c) {
                zonePos = a;
                zoneRotMat = b;
                zoneObjs = c;
            }
        };
        /// <summary>
        /// 割り当てられたリソースで最短の交点とオブジェクトを計算します。
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="objCollArgPack"></param>
        /// <returns></returns>
        private CollisionInfo ObjectCollisionMin(Ray ray, List<ObjectCollisionArgPackage> objCollArgPack)
        {
            CollisionInfo collisionInfo = new CollisionInfo();
            foreach (var objColl in objCollArgPack)
            {
                collisionInfo= ObjectCollision(collisionInfo, ray, objColl.zoneObjs, objColl.zonePos,objColl.zoneRotMat);
            }
            return collisionInfo;
        }

        /// <summary>
        /// この関数はray交点が存在するときのみ実行する。
        /// </summary>
        /// <param name="collisionInfo"></param>
        private void ObjectCollisionSuccess_process(CollisionInfo collisionInfo)
        {
            if (AddObjectWindow.AddTargetObject != null)
            {
                // mPositionでゾーンかギャラクシーかを判別
                if (AddObjectWindow.AddTargetObject.mPosition.X == 0.0f &&
                    AddObjectWindow.AddTargetObject.mPosition.Y == 0.0f &&
                    AddObjectWindow.AddTargetObject.mPosition.Z == 0.0f)
                {
                    AddObjectWindow.AddTargetObject.SetPosition((Vector3)collisionInfo.nearestHitpointPosition);
                }
                else
                {
                    // XXX: Zoen軸のXY反転。回転では[X,Y,Z]*-1。
                    AddObjectWindow.AddTargetObject.SetPosition(
                        Vector3.Cross((Vector3)collisionInfo.nearestHitpointPosition, new Vector3(-1.0f,-1.0f,1.0f)) -
                        AddObjectWindow.AddTargetObject.mPosition);
                }
                if (AddObjectWindow.IsChanged)
                {
                    var objCount = AddObjectWindow.Objects.Count();
                    _objects.Add(AddObjectWindow.Objects[objCount - 1]);

                    Scenario_ReLoad();
                    SelectTreeNodeWithUnique(AddObjectWindow.AddTargetObject.mUnique);
                    ChangeToNode(objectsListTreeView.SelectedNode, false);

                    AddObjectWindow.AddTargetObject = null;
                }
            }
            else
            {
                SelectTreeNodeWithUnique(collisionInfo.abstructObj.mUnique);
            }
        }

        private void glLevelView_MouseUp(object sender, MouseEventArgs e)
        {
            // 現状ではここの関数でマウスボタンのステータスを変更しているため、
            // returnを使用しないで下さい。

            if (e.Button != _mouseDown) return;
            var rayTest1 = ScreenToRay(e.Location);
            // this checks to make sure that we are just clicking, and not coming off of a drag while left clicking
            if ((Math.Abs(e.X - _lastMouseClick.X) < 3) && (Math.Abs(e.Y - _lastMouseClick.Y) < 3) &&
                (_pickingFrameBuffer[4] == _pickingFrameBuffer[1]) &&
                (_pickingFrameBuffer[4] == _pickingFrameBuffer[3]) &&
                (_pickingFrameBuffer[4] == _pickingFrameBuffer[5]) &&
                (_pickingFrameBuffer[4] == _pickingFrameBuffer[7]))
            {
                CollisionInfo collisionInfo = new CollisionInfo();

                // シナリオが選択されているかどうか。
                if (_currentScenario != 0) {

//                    // シングルスレッド処理
//                    {
//#if DEBUG
//                        var sw = new System.Diagnostics.Stopwatch(); // 時間測定
//                        sw.Start(); // 時間測定
//#endif
//                        // Galaxy
//                        List<AbstractObj> galaxyObjs = _objects.FindAll(o => o.mParentZone.ZoneName == _galaxyScenario.mName);
//                        collisionInfo = objectCollision(collisionInfo, rayTest1, galaxyObjs, null, null);
//                        // Zone
//                        // ギャラクシーで使用されているゾーンの取得。
//                        var ScenarioLayers = _galaxyScenario.GetMainGalaxyZone().GetLayersUsedOnZoneForCurrentScenario();
//                        // シナリオで使用されているゾーンの取得。
//                        List<StageObj> stageObjLayers = _galaxyScenario.GetMainGalaxyZone().GetAllStageDataFromLayers(ScenarioLayers);
//                        foreach (StageObj stageObj in stageObjLayers)
//                        {
//                            var zonePos = stageObj.mPosition;
//                            var zoneRotMat = GetRotMatrix3SmgCoordZone((stageObj.mRotation * (float)Math.PI) / 180.0f);

//                            List<AbstractObj> zoneObjs = _objects.FindAll(o => o.mParentZone.ZoneName == stageObj.mName);
//                            collisionInfo = objectCollision(collisionInfo, rayTest1, zoneObjs, zonePos, zoneRotMat);
//                        }
//#if DEBUG
//                        sw.Stop(); // 時間測定
//                        TimeSpan ts = sw.Elapsed; // 時間測定
//                        Debug.WriteLine("SelectObjectByRaySingle"); // 時間測定
//                        Debug.WriteLine($"　{ts}"); // 時間測定
//                        Debug.WriteLine($"　{ts.Hours}時間 {ts.Minutes}分 {ts.Seconds}秒 {ts.Milliseconds}ミリ秒"); // 時間測定
//                        Debug.WriteLine($"　{sw.ElapsedMilliseconds}ミリ秒"); // 時間測定
//#endif
//                    }

                    // マルチスレッド処理
                    {
#if DEBUG
                        var sw = new System.Diagnostics.Stopwatch(); // 時間測定
                        sw.Start(); // 時間測定
#endif
                        // メインギャラクシーもゾーンとして振り分けます。
                        // ゾーン数と同じ数のスレッドでレイ計算をします。
                        // 計算時間 = 最も遅いゾーン + (最単オブジェクトの判別 * ゾーン数)
                        // ただし、論理コア数以上のスレッドは作成しません。
                        List<Task<CollisionInfo>> taskList = new List<Task<CollisionInfo>>();
                        List<List<ObjectCollisionArgPackage>> objCollPackList = new List<List<ObjectCollisionArgPackage>> {
                        new List<ObjectCollisionArgPackage>()};
                        // Galaxy
                        objCollPackList[0].Add(new ObjectCollisionArgPackage(
                            null,
                            null,
                            _objects.FindAll(o => o.mParentZone.ZoneName == _galaxyScenario.mName)));
                        //List<PathObj> galaxyPaths = _paths.FindAll(p => p.mParentZone.ZoneName == _galaxyScenario.mName);
                        //Zone
                        // ギャラクシーで使用されているゾーンの取得。
                        var ScenarioLayers = _galaxyScenario.GetMainGalaxyZone().GetLayersUsedOnZoneForCurrentScenario();
                        // シナリオで使用されているゾーンの取得。
                        List<StageObj> stageObjLayers = _galaxyScenario.GetMainGalaxyZone().GetAllStageDataFromLayers(ScenarioLayers);
                        // スレッド数分の引数バッファを確保
                        foreach (var i in Enumerable.Range(0, stageObjLayers.Count % Environment.ProcessorCount))
                        {
                            objCollPackList.Add(new List<ObjectCollisionArgPackage>());
                        }
                        // バッファに値を書き込む。
                        foreach (var i in Enumerable.Range(0, stageObjLayers.Count))
                        {
                            objCollPackList[i % Environment.ProcessorCount].Add(new ObjectCollisionArgPackage(
                                stageObjLayers[i].mPosition,
                                GetRotMatrix3SmgCoordZone((stageObjLayers[i].mRotation * (float)Math.PI) / 180.0f),
                                _objects.FindAll(o => o.mParentZone.ZoneName == stageObjLayers[i].mName)));
                        }
                        // バッファの数のスレッドを作成。
                        foreach (var objCollPack in objCollPackList)
                        {
                            taskList.Add(System.Threading.Tasks.Task.Run(() => ObjectCollisionMin(rayTest1, objCollPack)));
                        }
                        // スレッドごとの最短オブジェクトの比較。
                        foreach (var task in taskList)
                        {
                            task.Wait();
                            CollisionInfo buf = task.Result;
                            if (buf < collisionInfo)
                            {
                                collisionInfo = buf;
                            }
                        }
#if DEBUG
                        sw.Stop(); // 時間測定
                        TimeSpan ts = sw.Elapsed; // 時間測定
                        Debug.WriteLine("SelectObjectByRayMulti"); // 時間測定
                        Debug.WriteLine($"　{ts}"); // 時間測定
                        Debug.WriteLine($"　{ts.Hours}時間 {ts.Minutes}分 {ts.Seconds}秒 {ts.Milliseconds}ミリ秒"); // 時間測定
                        Debug.WriteLine($"　{sw.ElapsedMilliseconds}ミリ秒"); // 時間測定
#endif
                    }

                    if (collisionInfo.nearestHitpointPosition != null)
                    {
                        ObjectCollisionSuccess_process(collisionInfo);
                    }
                }

                /// このコメントアウトの中にreturnが含まれているため、マウス動作に影響が出ます。
                //uint color = _pickingFrameBuffer[4];
                //Color clr = EditorUtil.UIntToColor(color);

                //int id = EditorUtil.ColorHolder.IDFromColor(clr);

                //foreach (string z in _zonesUsed)
                //{
                //    Zone zone = _galaxyScenario.GetZone(z);
                //    AbstractObj obj = zone.GetObjFromUniqueID(id);

                //    if (obj == null)
                //    {
                //        continue;
                //    }

                //    // if the current seleted object is the same, we deselect
                //    if (obj == _selectedObject)
                //    {
                //        _selectedObject = null;
                //        return;
                //    }

                //    _selectedObject = obj;

                //    if (!(obj is LevelObj))
                //    {
                //        return;
                //    }

                //    //選択したオブジェクトのBMDファイル内の三角面情報の一覧を取得する。
                //    BMDInfo.BMDTriangleData bmdTriangleData = BMDInfo.GetTriangles(obj);
                //    glLevelView.SwapBuffers();

                //    //カメラのRayと三角面の位置情報から交差判定を行う。

                //    float nearest_hitpoint_distance = float.MaxValue; // 最も近い交差点とカメラとの距離を記録する
                //    Vector3? nearest_hitpoint_position = null; // 最も近い交差点の交差位置を記録する



                //    // クリックしたオブジェクトが含む三角面の数だけ繰り返す

                //    float t, u, v;
                //    foreach (var triangle in bmdTriangleData.TriangleDataList)
                //    {
                //        var objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                //        var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
                //        var positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mTruePosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

                //        Vector3 v0 = Vector3.Add(positionWithZoneRotation, triangle.V0.Xyz) * 10000f;
                //        Vector3 v1 = Vector3.Add(positionWithZoneRotation, triangle.V1.Xyz) * 10000f;
                //        Vector3 v2 = Vector3.Add(positionWithZoneRotation, triangle.V2.Xyz) * 10000f;
                //        Vector3 normal_vector = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

                //        // 交差点の位置ベクトルは Ray.org + t * Ray.dir = v0 + u(v1-v0) + v(v2-v0)
                //        t = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Origin - v0, v1 - v0), v2 - v0);
                //        u = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Direction, v2 - v0), rayTest1.Origin - v0);
                //        v = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Origin - v0, v1 - v0), rayTest1.Direction);

                //        // この条件に引っかかればその三角面とは交差していない
                //        // 三角面を含む平面について，レイと平面の交点は三角面の外側 もしくは レイが三角面の裏面から入射
                //        if (((u < 0.0f || v < 0.0f) || (u + v > 1.0f)) || (Vector3.Dot(rayTest1.Direction, normal_vector) >= 0.0f))
                //        {
                //            //Debug.WriteLine($"DEBUG: the position you clicked is t:{t} u:{u} v:{v}");
                //            continue;
                //        }


                //        // 三角面とマウスクリックした点におけるカメラの視線は交差している

                //        // これまでの交差点において最も近い交差点を確認する．
                //        // この条件に引っかかればこれまでの任意の交差点よりも近くの交差点である．
                //        if(t < nearest_hitpoint_distance)
                //        {
                //            nearest_hitpoint_distance = t;

                //            //元のコード
                //            //nearest_hitpoint_position = rayTest1.Origin + t * rayTest1.Direction;
                //            nearest_hitpoint_position = new Vector3(Vector3.Add(rayTest1.Origin,Vector3.Multiply(rayTest1.Direction,t))) ;
                //        }
                //    }

                //    // nearest_hitpoint_position =: クリックした3次元座標

                //    // if条件: どの三角面とも交差していない場合にif内部に入る(何もしない)
                //    if (nearest_hitpoint_distance == float.MaxValue)
                //    {
                //        MessageBox.Show("交点なし");

                //        Debug.WriteLine("DEBUG★: the position you clicked is " + nearest_hitpoint_position.ToString());
                //        //return;
                //    }
                //    else
                //    {
                //        MessageBox.Show("交点あり");

                //        textBox1.Text = "☆DEBUG☆: the position you clicked is " + nearest_hitpoint_position.ToString();
                //        Debug.WriteLine("☆DEBUG☆: the position you clicked is " + nearest_hitpoint_position.ToString());
                //    }



                //    if (AddObjectWindow.AddTargetObject != null && nearest_hitpoint_position !=null)
                //    {
                //        glLevelView.SwapBuffers();

                //        var raytest2 = ScreenToRay(e.Location);
                //        //Console.WriteLine($"RayTest::{raytest2.Origin}{raytest2.Direction}");

                //        var objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                //        var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
                //        var positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mTruePosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

                //        //カメラ位置とオブジェクト原点の距離の中間の座標にオブジェクトをセットします。
                //        //レイの方向の制御は行っていません

                //        var rayPos = Vector3.Multiply(raytest2.Origin, 10000f);
                //        var selectedObjGlobalPosition = objTruePos + positionWithZoneRotation;

                //        //距離の計算
                //        var ToCameraFromObj3d = new Vector3(selectedObjGlobalPosition - rayPos);

                //        ToCameraFromObj3d = new Vector3((float)Math.Pow(Math.Abs(ToCameraFromObj3d.X), 2), (float)Math.Pow(Math.Abs(ToCameraFromObj3d.Y), 2), (float)Math.Pow(Math.Abs(ToCameraFromObj3d.Z), 2));

                //        var ToCameraFromObj = ToCameraFromObj3d.X + ToCameraFromObj3d.Y + ToCameraFromObj3d.Z;

                //        ToCameraFromObj = (float)Math.Sqrt(ToCameraFromObj);

                //        //AddObjectWindow.AddTargetObject.SetPosition((rayPos + Vector3.Multiply(raytest2.Direction/*obj.mTruePosition*/, ToCameraFromObj)) / 2);
                //        AddObjectWindow.AddTargetObject.SetPosition(new Vector3((float)nearest_hitpoint_position.Value.X, (float)nearest_hitpoint_position.Value.Y,(float)nearest_hitpoint_position.Value.Z/*Vector3.Multiply(raytest2.Direction, ToCameraFromObj) + rayPos*/));

                //        if (AddObjectWindow.IsChanged)
                //        {
                //            var objCount = AddObjectWindow.Objects.Count();
                //            _objects.Add(AddObjectWindow.Objects[objCount - 1]);



                //            Scenario_ReLoad();
                //            SelectTreeNodeWithUnique(AddObjectWindow.AddTargetObject.mUnique);
                //            ChangeToNode(objectsListTreeView.SelectedNode, false);

                //            AddObjectWindow.AddTargetObject = null;
                //        }
                //        break;
                //    }
                //    if (obj is PathPointObj)
                //    {
                //        SelectTreeNodeWithUnique(id);
                //        break;
                //    }
                //    else
                //    {
                //        SelectTreeNodeWithUnique(obj.mUnique);
                //        break;
                //    }


                //}
            }

            _mouseDown = MouseButtons.None;
            _lastMouseMove = e.Location;
        }

        private void glLevelView_MouseDown(object sender, MouseEventArgs e)
        {
#if DEBUG
            var raytest2 = ScreenToRay(e.Location);

            //textBox1.Text = raytest2.Direction.ToString();

            // rayの終点がマウスの座標になるように修正。
            GL.PushMatrix();
            GL.LineWidth(3.0f);
            GL.Begin(BeginMode.Lines);
            GL.Color3(1f, 0f, 1f);
            GL.Vertex3(_camTarget * 10000.0f);
            GL.Vertex3(raytest2.Origin + (1000000f * raytest2.Direction));
            GL.End();
            GL.PopMatrix();
            glLevelView.SwapBuffers();
#endif
            // マウスステータスのアップデート。
            if (_mouseDown != MouseButtons.None) return;
            _mouseDown = e.Button;
            _lastMouseMove = _lastMouseClick = e.Location;
        }

        private void ChangeToNode(TreeNode node, bool changeCamera = false)
        {
            AbstractObj abstractObj = node.Tag as AbstractObj;

            if (abstractObj == null) return;

            node.EnsureVisible();
            objectsListTreeView.Select();
            objectsListTreeView.SelectedNode = node;

            if (node.Parent == null && node.Text.EndsWith("Zone"))
            {
                StageObj stageObj = abstractObj as StageObj;
                dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, stageObj);
                ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                _selectedObject = stageObj;
            }
            else
            {
                if (changeCamera)
                {
                    //objects Camera Setting
                    //The following process moves the camera to the object.
                    var ZoneName = abstractObj.mParentZone.ZoneName;
                    var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(ZoneName);
                    var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(ZoneName);

                    var PosObj = abstractObj.mTruePosition;

                    //Move the camera to the position of Point0, index number 0 in the PathPoint list.
                    if (abstractObj is PathObj)
                    {
                        if (abstractObj == null) return;
                        var pathObj = abstractObj as PathObj;
                        PosObj = pathObj.mPathPointObjs[0].mPoint0;
                    }

                    //The camera when PathPointObj is selected.
                    //The selection of point 1 and point 2 of PathPointObj is not supported.
                    if (abstractObj is PathPointObj)
                    {
                        if (abstractObj == null) return;
                        var pathPointObj = abstractObj as PathPointObj;
                        PosObj = pathPointObj.mPoint0;
                    }

                    var CorrectPos_Object = calc.RotateTransAffine.GetPositionAfterRotation(PosObj, Rot_ZoneOffset, calc.RotateTransAffine.TargetVector.All);

                    _camDistance = 0.200f;
                    _camTarget = Pos_ZoneOffset / 10000f + CorrectPos_Object / 10000;
                    _camPosition = CorrectPos_Object / 10000;
                    _camRotation.Y = (float)Math.PI / 8f;
                    _camRotation.X = (-(abstractObj.mTrueRotation.Y + Pos_ZoneOffset.Y) / 180f) * (float)Math.PI;
                }

                //objects PropertyGrideSetting
                //Display the property grid for setting the currently selected object.
                //Note: These processes are not related to the camera's processing.

                ObjectPropertyDataGridView.DataSource = null;
                dataGridViewEdit_Objects = null;
                _selectedObject = abstractObj;

                //選択されたAbstractObjの親ノードのテキストが
                //「Objects」、「Areas」などの場合に「AbstractObj」を適切な型に変更して
                //データグリッドビューに表示します。
                //TODO: 言語設定を追加した際に柔軟に対応できる設計にしたほうがよい
                switch (node.Parent.Text)
                {
                    case "Objects":
                        if (!(abstractObj is LevelObj))
                            throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(LevelObj) }」 .");
                        LevelObj obj = abstractObj as LevelObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, obj);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Areas":
                        //AreaObj area = abstractObj as AreaObj;
                        //dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, area);
                        //dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                        if (!(abstractObj is AreaObj))
                            throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(AreaObj) }」 .");
                        AreaObj areaobj = abstractObj as AreaObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, areaobj);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Gravity":
                        PlanetObj planetObj = abstractObj as PlanetObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, planetObj);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Camera Areas":
                        CameraObj cameraObj = abstractObj as CameraObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, cameraObj);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Debug Movement":
                        DebugMoveObj debug = abstractObj as DebugMoveObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, debug);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Map Parts":
                        MapPartsObj mapparts = abstractObj as MapPartsObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, mapparts);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Demos":
                        DemoObj demo = abstractObj as DemoObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, demo);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Starting Points":
                        StartObj start = abstractObj as StartObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, start);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    case "Paths":
                        PathObj path = abstractObj as PathObj;
                        dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, path);
                        ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                        break;
                    default:
                        //dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, abstractObj);
                        //dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                        break;
                }

                // we have a path point
                if (node.Parent.Parent != null && node.Parent.Parent.Text == "Paths")
                {
                    PathPointObj pathPoint = abstractObj as PathPointObj;
                    dataGridViewEdit_Objects = new EditorWindowSys.DataGridViewEdit(ObjectPropertyDataGridView, pathPoint);
                    ObjectPropertyDataGridView = dataGridViewEdit_Objects.GetDataTable();
                }

                if (pathsToolStripMenuItem.Checked == false)
                {
                    if (abstractObj.CanUsePath())
                    {
                        Zone z = abstractObj.mParentZone;
                        // we need to delete any rendered paths
                        List<int> ids = z.GetAllUniqueIDsFromObjectsOfType("PathObj");
                        ids.ForEach(i => GL.DeleteLists(_dispLists[0][i], 1));
                        PathObj path = z.GetPathFromID(abstractObj.mEntry.Get<short>("CommonPath_ID"));

                        if (path != null)
                        {
                            // now we render only this path
                            var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(path.mParentZone.ZoneName);
                            var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(path.mParentZone.ZoneName);

                            GL.DeleteLists(_dispLists[0][path.mUnique], 1);
                            GL.NewList(_dispLists[0][path.mUnique], ListMode.Compile);

                            GL.PushMatrix();
                            {
                                GL.Translate(Pos_ZoneOffset);
                                GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                                GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                                GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                            }

                            path.Render(RenderMode.Opaque);
                            GL.PopMatrix();
                            GL.EndList();
                        }
                    }
                }
            }

            UpdateCamera();
            glLevelView.Refresh();
        }

        private void objectsListTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ChangeToNode(e.Node, (Control.ModifierKeys == Keys.Shift));
        }

        private void objectsListTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }

        private void toolStripLabel3_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {

        }

        IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;

                foreach (var child in Collect(node.Nodes))
                    yield return child;
            }
        }

        private void SelectTreeNodeWithUnique(int id)
        {
            foreach (var node in Collect(objectsListTreeView.Nodes))
            {
                AbstractObj obj = node.Tag as AbstractObj;

                if (obj == null)
                    continue;

                if (obj.mUnique == id)
                {
                    tabControl1.SelectedIndex = 2;
                    ExpandAllParents(node);
                    ChangeToNode(node, (Control.ModifierKeys == Keys.Shift));
                    return;
                }
                else if (obj is PathPointObj)
                {
                    var ppObj = obj as PathPointObj;

                    for (int i = 0; i < 3; i++)
                    {
                        if (ppObj.mPointIDs[i] == id)
                        {
                            tabControl1.SelectedIndex = 2;
                            ExpandAllParents(node);
                            ChangeToNode(node, (Control.ModifierKeys == Keys.Shift));
                            return;
                        }
                    }
                }
            }
        }

        private void ExpandAllParents(TreeNode node)
        {
            node.Expand();

            if (node.Parent != null)
            {
                ExpandAllParents(node.Parent);
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DataGridView)sender;
            var cell_value = dgv[e.ColumnIndex, e.RowIndex].Value;

            //dgv[e.ColumnIndex, e.RowIndex].AccessibilityObject.
            if (dgv[e.ColumnIndex, e.RowIndex] is DataGridViewComboBoxCell)
            {
                var tes = dgv[e.ColumnIndex, e.RowIndex] as DataGridViewComboBoxCell;
                cell_value = tes.Items.IndexOf(cell_value) - 1;
            }

            dataGridViewEdit_Objects.ChangeValue(e.RowIndex, cell_value);

            if (_selectedObject.GetType() == typeof(PathPointObj))
            {
                PathPointObj obj = _selectedObject as PathPointObj;
                PathObj path = obj.mParent;

                var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(_selectedObject.mParentZone.ZoneName);
                var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(_selectedObject.mParentZone.ZoneName);

                GL.DeleteLists(_dispLists[0][path.mUnique], 1);
                GL.NewList(_dispLists[0][path.mUnique], ListMode.Compile);

                GL.PushMatrix();
                {
                    GL.Translate(Pos_ZoneOffset);
                    GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                    GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                    GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                }

                path.Render(RenderMode.Opaque);
                GL.PopMatrix();
                GL.EndList();
            }
            else if (_selectedObject.GetType() == typeof(StageObj))
            {
                StageObj stageObj = _selectedObject as StageObj;
                Zone z = _galaxyScenario.GetZone(stageObj.mName);
                List<int> ids = z.GetAllUniqueIDsFromZoneOnCurrentScenario();

                foreach (int id in ids)
                {
                    GL.DeleteLists(_dispLists[0][id], 1);
                    GL.NewList(_dispLists[0][id], ListMode.Compile);

                    GL.PushMatrix();
                    {
                        GL.Translate(stageObj.mTruePosition);
                        GL.Rotate(stageObj.mTrueRotation.Z, 0f, 0f, 1f);
                        GL.Rotate(stageObj.mTrueRotation.Y, 0f, 1f, 0f);
                        GL.Rotate(stageObj.mTrueRotation.X, 1f, 0f, 0f);
                    }

                    z.RenderObjFromUnique(id, RenderMode.Opaque, true);
                    GL.PopMatrix();
                    GL.EndList();
                }
            }
            else
            {
                var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(_selectedObject.mParentZone.ZoneName);
                var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(_selectedObject.mParentZone.ZoneName);

               GL.DeleteLists(_dispLists[0][_selectedObject.mUnique], 1);
                GL.NewList(_dispLists[0][_selectedObject.mUnique], ListMode.Compile);

                GL.PushMatrix();
                {
                    GL.Translate(Pos_ZoneOffset);
                    GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                    GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                    GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                }

                _selectedObject.Render(RenderMode.Opaque);
                GL.PopMatrix();
                GL.EndList();
            }

            _areChanges = true;
            undoToolStripMenuItem.Enabled = true;
            glLevelView.Refresh();

            // if redoing was possible at this point, it is no longer possible since we have introduced a more recent redo
            if (EditorUtil.EditorActionHolder.CanRedo())
            {
                EditorUtil.EditorActionHolder.ClearActionsAfterCurrent();
                redoToolStripMenuItem.Enabled = false;
            }

        }

        private void AreaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> zones = _galaxyScenario.GetZonesUsedOnCurrentScenario();
            Dictionary<string, List<int>> ids = new Dictionary<string, List<int>>
            {
                { _galaxyScenario.mName, _galaxyScenario.GetMainGalaxyZone().GetAllUniqueIDsFromObjectsOfType("AreaObj") }
            };

            foreach (string z in zones)
            {
                Zone zone = _galaxyScenario.GetZone(z);
                ids.Add(z, zone.GetAllUniqueIDsFromObjectsOfType("AreaObj"));
            }

            if (AreaToolStripMenuItem.Checked)
            {
                // disable areas
                AreaToolStripMenuItem.Checked = false;

                foreach (KeyValuePair<string, List<int>> kvp in ids)
                {
                    List<int> id_list = kvp.Value;

                    foreach (int id in id_list)
                    {
                        GL.DeleteLists(_dispLists[0][id], 1);
                    }
                }
            }
            else
            {
                // enable areas
                AreaToolStripMenuItem.Checked = true;

                foreach (KeyValuePair<string, List<int>> kvp in ids)
                {
                    string zoneName = kvp.Key;
                    List<int> id_list = kvp.Value;

                    foreach (int id in id_list)
                    {
                        var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(zoneName);
                        var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(zoneName);

                        AreaObj area = _galaxyScenario.GetZone(zoneName).GetObjFromUniqueID(id) as AreaObj;

                        GL.DeleteLists(_dispLists[0][area.mUnique], 1);
                        GL.NewList(_dispLists[0][area.mUnique], ListMode.Compile);

                        GL.PushMatrix();
                        {
                            GL.Translate(Pos_ZoneOffset);
                            GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                            GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                            GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                        }

                        area.Render(RenderMode.Opaque);
                        GL.PopMatrix();
                        GL.EndList();
                    }
                }
            }

            glLevelView.Refresh();
            Properties.Settings.Default.EditorWindowDisplayArea = AreaToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameUtil.IsSMG1())
            {
                Translate.GetMessageBox.Show(MessageBoxText.UnimplementedFeatures, MessageBoxCaption.Info);
                return;
            }
            _galaxyScenario.Save();
            EditorWindowSys.DataGridViewEdit.IsChangedClear();
            _areChanges = false;
            OpenSaveStatusLabel.Text = "Changes Saved : SaveTime : " + DateTime.Now;
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _galaxyZones.Clear();
            _zonesUsed.Clear();
            _ZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            _paths.Clear();
            _objects.Clear();
            _dispLists.Clear();
            glLevelView.Dispose();
            _galaxyScenario.Close();
        }

        /// <summary>
        /// データグリッドビューセルに紐づけされたコントロールの変更を検知します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (ObjectPropertyDataGridView.CurrentCellAddress.X == 0 && ObjectPropertyDataGridView.IsCurrentCellDirty)
            {
                ObjectPropertyDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            ObjectPropertyDataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void zonesListTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Parent == null)
                return;

            AbstractObj param;
            switch (e.Node.Parent.Text)
            {
                case "Shadow Parameters":
                    param = e.Node.Tag as ZoneAttributes.ShadowParam;
                    zonesDataGridView.DataSource = null;
                    dataGridViewEdit_Zones = new EditorWindowSys.DataGridViewEdit(zonesDataGridView, param);
                    zonesDataGridView = dataGridViewEdit_Zones.GetDataTable();
                    break;
                case "Water Parameters":
                    param = e.Node.Tag as ZoneAttributes.WaterCameraParam;
                    zonesDataGridView.DataSource = null;
                    dataGridViewEdit_Zones = new EditorWindowSys.DataGridViewEdit(zonesDataGridView, param);
                    zonesDataGridView = dataGridViewEdit_Zones.GetDataTable();
                    break;
                case "Flags":
                    param = e.Node.Tag as ZoneAttributes.FlagNameTable;
                    zonesDataGridView.DataSource = null;
                    dataGridViewEdit_Zones = new EditorWindowSys.DataGridViewEdit(zonesDataGridView, param);
                    zonesDataGridView = dataGridViewEdit_Zones.GetDataTable();
                    break;
            }
        }

        private void lightsTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Light lght = e.Node.Tag as Light;

            if (lght == null) return;

            lightsDataGridView.DataSource = null;
            dataGridViewEdit_Lights = new EditorWindowSys.DataGridViewEdit(lightsDataGridView, lght);
            lightsDataGridView = dataGridViewEdit_Lights.GetDataTable();
        }

        /// <summary>
        /// SMG座標系での回転行列の取得。
        /// </summary>
        /// <param name="rad">軸回転</param>
        /// <returns>回転行列(3次元)</returns>
        private Matrix3 GetRotMatrix3SmgCoordX(Vector3 rad)
        {
            return new Matrix3(
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, (float)Math.Cos(rad.X), -(float)Math.Sin(rad.X)),
                new Vector3(0.0f, (float)Math.Sin(rad.X), (float)Math.Cos(rad.X)));
        }
        /// <summary>
        /// SMG座標系での回転行列の取得。
        /// </summary>
        /// <param name="rad">軸回転</param>
        /// <returns>回転行列(3次元)</returns>
        private Matrix3 GetRotMatrix3SmgCoordY(Vector3 rad)
        {
            return new Matrix3(
                new Vector3((float)Math.Cos(rad.Y), 0.0f, (float)Math.Sin(rad.Y)),
                new Vector3(0f, 1f, 0f),
                new Vector3(-(float)Math.Sin(rad.Y), 0.0f, (float)Math.Cos(rad.Y)));
        }
        /// <summary>
        /// SMG座標系での回転行列の取得。
        /// </summary>
        /// <param name="rad">軸回転</param>
        /// <returns>回転行列(3次元)</returns>
        private Matrix3 GetRotMatrix3SmgCoordZ(Vector3 rad)
        {
            return new Matrix3(
                new Vector3((float)Math.Cos(rad.Z), -(float)Math.Sin(rad.Z), 0.0f),
                new Vector3((float)Math.Sin(rad.Z), (float)Math.Cos(rad.Z), 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f));
        }
        /// <summary>
        /// SMG座標系かつ、カメラ回転軸を主とした回転行列。
        /// </summary>
        /// <param name="rad"></param>
        /// <returns>回転行列(3次元)</returns>
        private Matrix3 GetRotMatrix3SmgCoordZY(Vector3 rad)
        {
            // Vector2でのカメラ回転の優先度をSMG座標系で計算するため、このようになる。
            return GetRotMatrix3SmgCoordZ(rad) * GetRotMatrix3SmgCoordY(rad);
        }
        /// <summary>
        /// SMGギャラクシー座標系回転行列。
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private Matrix3 GetRotMatrix3SmgCoordGlobal(Vector3 rad)
        {
            return GetRotMatrix3SmgCoordX(rad) * GetRotMatrix3SmgCoordY(rad) * GetRotMatrix3SmgCoordZ(rad);
        }
        /// <summary>
        /// SMG座標系オブジェクト用回転行列
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private Matrix3 GetRotMatrix3SmgCoordObject(Vector3 rad)
        {
            rad *= -1;
            return GetRotMatrix3SmgCoordGlobal(rad);
        }
        /// <summary>
        /// SMG座標系ゾーン用回転行列
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private Matrix3 GetRotMatrix3SmgCoordZone(Vector3 rad)
        {
            // Zone座標軸からの変換。
            rad *= -1;
            return GetRotMatrix3SmgCoordGlobal(rad);
        }
        /// <summary>
        /// SMGゾーン座標系オブジェクト用回転行列
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        private Matrix3 GetRotMatrix3SmgZoneCoordObject(Vector3 rad)
        {
            // Zone座標軸からの変換。
            rad *= -1;
            return GetRotMatrix3SmgCoordZ(rad) * GetRotMatrix3SmgCoordY(rad) * GetRotMatrix3SmgCoordX(rad);
        }

        /// <summary>
        /// SMG座標軸でのカメラのグローバル回転行列の取得。
        /// </summary>
        /// <returns></returns>
        private Matrix3 GetCamRotMatrix3SMGCoord() {
            // SMG座標での回転。
            Vector3 rotateRad = new Vector3(0f, _camRotation.X, -_camRotation.Y);
            return GetRotMatrix3SmgCoordZY(rotateRad);
        }

        /// <summary>
        /// カメラのローカル座標でのレイの方向を計算します。
        /// </summary>
        /// <param name="mousePos"></param>
        /// <returns></returns>
        private Vector3 CalculateVectorFromCursorPos(Point mousePos) {
            // このプロジェクトのGL.Vertex3()座標はSMG座標互換です。
            // そのため、右手 上y、 手前xでレンダリングされます。
            // また、マウス座標は左上が原点であり、FOVはrad(70度)VFovです。
            // 軸は、下記のようになっているようです。
            //  X Y Z :エディタ座標
            //  _ X-Y :内部カメラ回転

            // SMG座標に変換後計算をします。
            // 画面中央を基準値にとした座標の割合を計算し、回転軸に関連付ける。
            // dotOffsetはフォームのサイズでは縁のピクセル数が含まれるため。
            // テスト段階では0で問題なし。
            int dotOffset = 0;
            var editorHeight = glLevelView.Size.Height - dotOffset;
            var editorWidth = glLevelView.Size.Width - dotOffset;
            double aspectRatio = (double)editorWidth / (double)editorHeight;
            double hWidth = editorWidth / 2d;
            double hHeight = editorHeight / 2d;
            Vector3d mouseToRotateD = new Vector3d(
                0d,
                -((hWidth - (double)mousePos.X) / hWidth),
                (hHeight - (double)mousePos.Y) / hHeight);
            // ドット値をラジアンに変換 + 画面座標の補正をします。
            double vHarfFov = (double)FOV / 2d;
            double vHarfFovTan = Math.Tan(vHarfFov);
            double hHarfFovTan = Math.Tan(vHarfFov) * aspectRatio;
            double hHarfFov = Math.Atan(hHarfFovTan);

            mouseToRotateD.Y = Math.Atan(mouseToRotateD.Y * hHarfFovTan);
            // Y軸の回転分倍率補正を掛ける。
            mouseToRotateD.Z = Math.Atan(mouseToRotateD.Z * vHarfFovTan * Math.Abs(Math.Cos(mouseToRotateD.Y)));

            // 基準となる正面方向のベクトル。
            Vector3 ray = new Vector3(-1f, 0f, 0f);

            ray *= GetRotMatrix3SmgCoordZY((Vector3)mouseToRotateD);

            return Vector3.Normalize(ray);
        }

        /// <summary>
        /// フォーム上のマウス座標より、収束点とそこからの方角を計算します。
        /// </summary>
        /// <param name="mousePos"></param>
        /// <returns></returns>
        private Ray ScreenToRay(Point mousePos)
        {
            var toGlobal = GetCamRotMatrix3SMGCoord();
            // SMGグローバル座標でのカメラの収束点の計算
            Vector3 camConvergence = new Vector3(_camDistance, 0.0f, 0.0f) * toGlobal;
            return new Ray((_camTarget + camConvergence) * 10000.0f, CalculateVectorFromCursorPos(mousePos) * toGlobal);
        }

        private void lightsTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Parent == null) return;

            Light en = e.Node.Tag as Light;
            LightAttribEditor editor = new LightAttribEditor(en.mName);
            editor.Show();
        }



        private void TabPage_SizeChanged(object sender, EventArgs e)
        {
            if (sender == null)
            {
                Console.WriteLine("null");
                return;
            }

            var tabpage = (TabPage)sender;
            foreach (Control con in tabpage.Controls)
            {
                if (con is DataGridView)
                {
                    var dgv = con as DataGridView;
                    if (dgv.Columns.Count < 1) return;
                    //dgv.Anchor = AnchorStyles.Top & AnchorStyles.Left;
                    var dgvHeight = tabpage.ClientRectangle.Height - dgv.Location.Y;

                    //Console.WriteLine($"{tabpage.Height} : {dgv.Location.Y}");

                    dgv.MaximumSize = new Size(dgv.MaximumSize.Width, dgvHeight);

                    var rowTotalHeght = ((dgv.Rows.Count + 1) * dgv.RowTemplate.Height);
                    if (rowTotalHeght < dgv.MaximumSize.Height)
                    {
                        dgv.Height = rowTotalHeght;
                    }
                    else
                    {
                        dgv.Height = dgvHeight;
                    }

                    //Console.WriteLine($"{dgv.Name} : {dgvHeight}");
                }
            }
        }

        private void objectsListTreeView_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void objectsListTreeView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                deleteObjNode(objectsListTreeView.SelectedNode);
            }
            else
            {
                ChangeToNode(objectsListTreeView.SelectedNode);
            }

        }

        private void pathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> zones = _galaxyScenario.GetZonesUsedOnCurrentScenario();
            Dictionary<string, List<int>> ids = new Dictionary<string, List<int>>
            {
                { _galaxyScenario.mName, _galaxyScenario.GetMainGalaxyZone().GetAllUniqueIDsFromObjectsOfType("PathObj") }
            };

            foreach (string z in zones)
            {
                Zone zone = _galaxyScenario.GetZone(z);
                ids.Add(z, zone.GetAllUniqueIDsFromObjectsOfType("PathObj"));
            }

            if (pathsToolStripMenuItem.Checked)
            {
                // disable paths
                pathsToolStripMenuItem.Checked = false;

                foreach (KeyValuePair<string, List<int>> kvp in ids)
                {
                    List<int> id_list = kvp.Value;

                    foreach (int id in id_list)
                    {
                        GL.DeleteLists(_dispLists[0][id], 1);
                    }
                }
            }
            else
            {
                // enable paths
                pathsToolStripMenuItem.Checked = true;

                foreach (KeyValuePair<string, List<int>> kvp in ids)
                {
                    string zoneName = kvp.Key;
                    List<int> id_list = kvp.Value;

                    foreach (int id in id_list)
                    {
                        var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(zoneName);
                        var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(zoneName);

                        PathObj path = _galaxyScenario.GetZone(zoneName).GetObjFromUniqueID(id) as PathObj;

                        GL.DeleteLists(_dispLists[0][path.mUnique], 1);
                        GL.NewList(_dispLists[0][path.mUnique], ListMode.Compile);

                        GL.PushMatrix();
                        {
                            GL.Translate(Pos_ZoneOffset);
                            GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                            GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                            GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                        }

                        path.Render(RenderMode.Opaque);
                        GL.PopMatrix();
                        GL.EndList();
                    }
                }
            }

            glLevelView.Refresh();
            Properties.Settings.Default.EditorWindowDisplayPath = pathsToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void deleteObjNode(TreeNode node)
        {
            AbstractObj selectedObject = node.Tag as AbstractObj;

            if (node.Text == _galaxyScenario.mName)
            {
                MessageBox.Show("You cannot delete the main galaxy!");
                return;
            }

            // paths and stages require additional logic to delete
            if (selectedObject.mType != "StageObj" && selectedObject.mType != "PathPointObj")
            {
                int uniqueID = selectedObject.mUnique;
                Zone zone = selectedObject.mParentZone;

                if (selectedObject.mType == "PathObj")
                {
                    PathObj path_obj = selectedObject as PathObj;
                    AbstractObj out_obj;

                    if (zone.DoesAnyObjUsePathID(path_obj.Get<short>("no"), out out_obj))
                    {
                        DialogResult res = MessageBox.Show($"You are about to delete a path that an object still uses! Are you sure you want to delete it?\n{out_obj.ToString()}", "Path Deletion", MessageBoxButtons.YesNo);

                        if (res == DialogResult.Yes)
                        {
                            zone.DeleteObjectWithUniqueID(uniqueID);
                            GL.DeleteLists(_dispLists[0][uniqueID], 1);
                            objectsListTreeView.Nodes.Remove(node);
                            _areChanges = true;
                            glLevelView.Refresh();
                            return;
                        }
                        else
                        {
                            // we do nothing
                            return;
                        }
                    }
                }

                zone.DeleteObjectWithUniqueID(uniqueID);
                GL.DeleteLists(_dispLists[0][uniqueID], 1);
                objectsListTreeView.Nodes.Remove(node);
                _areChanges = true;
                glLevelView.Refresh();

            }
            else if (selectedObject.mType == "PathPointObj")
            {
                PathPointObj pobj = node.Tag as PathPointObj;
                PathObj parentPath = pobj.mParent;
                Zone z = pobj.mParentZone;
                z.DeletePathPointFromPath(parentPath.mUnique, node.Parent.Nodes.IndexOf(node));

                var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(z.ZoneName);
                var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(z.ZoneName);

                GL.DeleteLists(_dispLists[0][parentPath.mUnique], 1);
                GL.NewList(_dispLists[0][parentPath.mUnique], ListMode.Compile);

                GL.PushMatrix();
                {
                    GL.Translate(Pos_ZoneOffset);
                    GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                    GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                    GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                }

                parentPath.Render(RenderMode.Opaque);
                GL.PopMatrix();
                GL.EndList();

                objectsListTreeView.Nodes.Remove(node);
                _areChanges = true;
                glLevelView.Refresh();
            }
            else if (selectedObject.mType == "StageObj")
            {
                DialogResult res = MessageBox.Show("You are about to delete an entire zone. Are you sure?", "Zone Deletion", MessageBoxButtons.YesNo);

                if (res == DialogResult.Yes)
                {
                    List<int> ids = _galaxyScenario.GetZone(selectedObject.mName).GetAllUniqueIDS();
                    _galaxyScenario.RemoveZone(selectedObject.mName);

                    foreach (int id in ids)
                    {
                        // only delete the display lists for the objects that are currently in the scene
                        // the RemoveZone functions removes the ones not in it
                        if (_dispLists[0].ContainsKey(id))
                        {
                            GL.DeleteLists(_dispLists[0][id], 1);
                        }
                    }

                    objectsListTreeView.Nodes.Remove(node);

                    _areChanges = true;
                    glLevelView.Refresh();
                }
            }
        }

        private void deleteObjButton_Click(object sender, EventArgs e)
        {
            if (objectsListTreeView.SelectedNode != null)
            {
                deleteObjNode(objectsListTreeView.SelectedNode);
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorUtil.EditorAction to;
            EditorUtil.EditorAction from;
            EditorUtil.EditorActionHolder.DoUndo(out from, out to);

            if (from.mActionType == EditorUtil.EditorActionHolder.ActionType.ActionType_EditObject)
            {
                EditorUtil.ObjectEditAction objAction = from as EditorUtil.ObjectEditAction;
                AbstractObj obj = objAction.mEditedObject;
                Console.WriteLine($"in a perfect would, we would set {objAction.mFieldName} to {objAction.mValue}");

                // this code is only here because there seems to be some issues when it comes to converting types...
                // forcing these types (as defined in a file) seems to fix a lot of casting issues
                if (BCSV.sFieldTypeTable.ContainsKey(objAction.mFieldName))
                {
                    string fieldtype = BCSV.sFieldTypeTable[objAction.mFieldName];

                    switch (fieldtype)
                    {
                        case "float":
                            obj.mEntry.Set(objAction.mFieldName, (float)objAction.mValue);
                            break;
                    }
                }
                else
                {
                    obj.mEntry.Set(objAction.mFieldName, objAction.mValue);
                }

                if (objAction.mFieldName.StartsWith("pos_"))
                {
                    obj.SetPosition(new Vector3(obj.mEntry.Get<float>("pos_x"), obj.mEntry.Get<float>("pos_y"), obj.mEntry.Get<float>("pos_z")));

                    var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                    var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);

                    GL.DeleteLists(_dispLists[0][obj.mUnique], 1);
                    GL.NewList(_dispLists[0][obj.mUnique], ListMode.Compile);

                    GL.PushMatrix();
                    {
                        GL.Translate(Pos_ZoneOffset);
                        GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                        GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                        GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                    }

                    obj.Render(RenderMode.Opaque);
                    GL.PopMatrix();
                    GL.EndList();
                    glLevelView.Refresh();
                }

                _areChanges = true;
            }

            undoToolStripMenuItem.Enabled = EditorUtil.EditorActionHolder.CanUndo();
            // undoing causes the ability to redo!
            redoToolStripMenuItem.Enabled = true;
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditorUtil.EditorAction from;
            EditorUtil.EditorAction to;
            EditorUtil.EditorActionHolder.DoRedo(out from, out to);

            if (from.mActionType == EditorUtil.EditorActionHolder.ActionType.ActionType_EditObject)
            {
                EditorUtil.ObjectEditAction objAction = to as EditorUtil.ObjectEditAction;
                AbstractObj obj = objAction.mEditedObject;
                Console.WriteLine($"in a perfect would, we would set {objAction.mFieldName} to {objAction.mValue}");

                if (BCSV.sFieldTypeTable.ContainsKey(objAction.mFieldName))
                {
                    string fieldtype = BCSV.sFieldTypeTable[objAction.mFieldName];

                    switch (fieldtype)
                    {
                        case "float":
                            obj.mEntry.Set(objAction.mFieldName, (float)objAction.mValue);
                            break;
                    }
                }
                else
                {
                    obj.mEntry.Set(objAction.mFieldName, objAction.mValue);
                }

                if (objAction.mFieldName.StartsWith("pos_"))
                {
                    obj.SetPosition(new Vector3(obj.mEntry.Get<float>("pos_x"), obj.mEntry.Get<float>("pos_y"), obj.mEntry.Get<float>("pos_z")));

                    var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                    var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);

                    GL.DeleteLists(_dispLists[0][obj.mUnique], 1);
                    GL.NewList(_dispLists[0][obj.mUnique], ListMode.Compile);

                    GL.PushMatrix();
                    {
                        GL.Translate(Pos_ZoneOffset);
                        GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                        GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                        GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                    }

                    obj.Render(RenderMode.Opaque);
                    GL.PopMatrix();
                    GL.EndList();
                    glLevelView.Refresh();
                }

                _areChanges = true;
            }

            redoToolStripMenuItem.Enabled = EditorUtil.EditorActionHolder.CanRedo();
            undoToolStripMenuItem.Enabled = EditorUtil.EditorActionHolder.CanUndo();
        }

        private void attrFinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> zones = _galaxyScenario.GetZonesUsedOnCurrentScenario();
            zones.Add(_galaxyScenario.mName);
            List<AbstractObj> resObjs = new List<AbstractObj>();

            TextDialog dlg = new TextDialog();
            dlg.ShowDialog();
            string field = dlg.GetField();

            if (dlg.IsCanceled())
            {
                return;
            }

            StageObjectAttrFinder finder = new StageObjectAttrFinder(field);

            foreach (string zone in zones)
            {
                Zone z = _galaxyScenario.GetZone(zone);
                resObjs.AddRange(z.GetAllObjectsWithAttributeNonZero(field));
            }

            if (resObjs.Count == 0)
            {
                MessageBox.Show($"No objects were found that contain the field {field}.");
                return;
            }

            foreach (AbstractObj obj in resObjs)
            {
                if (obj.mEntry.ContainsKey("l_id"))
                {
                    finder.AddRow(obj.Get<int>("l_id"), obj.mName, obj.mParentZone.ZoneName, obj.mEntry.Get(field));
                }
                else
                {
                    finder.AddRow(-1, obj.mName, obj.mParentZone.ZoneName, obj.mEntry.Get(field));
                }
            }

            finder.Show();
        }


        private void cameraListTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Camera camera = e.Node.Tag as Camera;

            if (camera == null) return;

            camerasDataGridView.DataSource = null;
            dataGridViewEdit_CameraParam = new EditorWindowSys.DataGridViewEdit(camerasDataGridView, camera);
            camerasDataGridView = dataGridViewEdit_CameraParam.GetDataTable(camera);
        }

        private void glLevelView_Resize(object sender, EventArgs e)
        {
            if (!_glLoaded) return;
            glLevelView.MakeCurrent();

            UpdateViewport();
        }

        private void glLevelView_Load(object sender, EventArgs e)
        {
            glLevelView.MakeCurrent();

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(1f);

            GL.FrontFace(FrontFaceDirection.Cw);

            _camRotation = new Vector2(0.0f, 0.0f);
            _camTarget = new Vector3(0.0f, 0.0f, 0.0f);
            _camDistance = 1f;// 700.0f;

            _renderInfo = new RenderInfo();

            UpdateViewport();
            Vector3 CameraDefaultVector3 = new Vector3(0f, 0f, 0f);
            _glLoaded = true;
            _camDistance = 0.200f;
            _camTarget = CameraDefaultVector3;
            UpdateCamera();
            glLevelView.Refresh();
        }

        private void glLevelView_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                //回転方向は右ねじの法則とSMG座標系に合わせてます。
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (e.Delta > 0) _camRotation.Y += ((float)Math.PI / 180) * 90;
                    if (e.Delta < 0) _camRotation.Y += ((float)Math.PI / 180) * -90;
                }
                else
                {
                    if (e.Delta > 0) _camRotation.X += ((float)Math.PI / 180) * -90;
                    if (e.Delta < 0) _camRotation.X += ((float)Math.PI / 180) * 90;
                }
                Console.WriteLine(e.Delta);
            }
            else
            {
                float delta = -((e.Delta / 120f) * 0.1f);
                _camTarget.X += delta * (float)Math.Cos(_camRotation.X) * (float)Math.Cos(_camRotation.Y);
                _camTarget.Y += delta * (float)Math.Sin(_camRotation.Y);
                _camTarget.Z += delta * (float)Math.Sin(_camRotation.X) * (float)Math.Cos(_camRotation.Y);
            }
            UpdateCamera();

            glLevelView.Refresh();
        }

#endregion
    }
}
