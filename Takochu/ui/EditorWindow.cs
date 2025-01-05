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

namespace Takochu.ui
{
    public partial class EditorWindow : Form
    {
        private readonly string _galaxyName;
        private int mCurrentScenario;
        private GalaxyScenario _galaxyScenario;
        private List<AbstractObj> mObjects = new List<AbstractObj>();
        private List<PathObj> mPaths = new List<PathObj>();

        Dictionary<string, List<StageObj>> mStages = new Dictionary<string, List<StageObj>>();
        List<string> mZonesUsed = new List<string>();

        Dictionary<string, int> mZoneMasks = new Dictionary<string, int>();

        Dictionary<int, Dictionary<int, int>> mDispLists = new Dictionary<int, Dictionary<int, int>>();

        AbstractObj mSelectedObject;

        public readonly IGameVersion GameVersion;

        private void AddObject_ToolStripItem_ObjectNameClick(object sender, EventArgs e)
        {
            if (scenarioTreeView.SelectedNode == null)
            {
                Console.WriteLine("シナリオが選択されていません。");
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

        public EditorWindow(string galaxyName)
        {
            InitializeComponent();
            _galaxyName = galaxyName;

            GameVersion = GameUtil.IsSMG1() ? new SMG2() : new SMG2();

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
            mDispLists.Add(0, new Dictionary<int, int>());
            mDispLists.Add(1, new Dictionary<int, int>());
            mDispLists.Add(2, new Dictionary<int, int>());
        }

        private void ClearGLDisplayLists()
        {
            foreach (KeyValuePair<int, Dictionary<int, int>> disp in mDispLists)
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
            m_PickingFrameBuffer = new uint[9];
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
            m_AreChanges = false;
            mStages.Clear();
            mZonesUsed.Clear();
            mZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            zonesListTreeView.Nodes.Clear();
            lightsTreeView.Nodes.Clear();
            cameraListTreeView.Nodes.Clear();

            mPaths.Clear();

            mObjects.Clear();

            ClearGLDisplayLists();
            mDispLists.Clear();
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
            mZonesUsed.Add(mainGalaxyName);
            mZonesUsed.AddRange(mainGalaxyZone.GetZonesUsedOnLayers(layers));

            Dictionary<string, List<Camera>> cameras = new Dictionary<string, List<Camera>>();
            List<Light> lights = new List<Light>();

            List<string> currentLayers = new List<string>();

            mObjects.AddRange(mainGalaxyZone.GetAllObjectsFromLayers(layers));

            foreach (string zoneName in mZonesUsed)
            {
                mZoneMasks.Add(zoneName, _galaxyScenario.GetMaskUsedInZoneOnCurrentScenario(zoneName));

                TreeNode zoneNode = new TreeNode()
                {
                    Text = zoneName,
                    Name = zoneName
                };

                AssignNodesToZoneNode(ref zoneNode);
                currentLayers.AddRange(GameUtil.GetGalaxyLayers(mZoneMasks[zoneName]));

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

                mPaths.AddRange(zone.mPaths);

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

                    mObjects.AddRange(zone.GetAllObjectsFromLayers(currentScenarioZoneLayerNames));
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

                        mStages.Add(layer, stgs);
                    }
                }
            }

            foreach (string zone in mZonesUsed)
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

        private void scenarioTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }

        private void scenarioTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (scenarioTreeView.SelectedNode != null)
            {
                mCurrentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);

                applyGalaxyNameBtn.Enabled = true;
                LoadScenario(mCurrentScenario);

                if (_galaxyScenario.GetMainGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{mCurrentScenario}.canm"))
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
            if (EditorWindowSys.DataGridViewEdit.IsChanged || m_AreChanges)
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
            StageInfoEditor stageInfo = new StageInfoEditor(ref _galaxyScenario, mCurrentScenario);
            stageInfo.ShowDialog();
        }

        private void applyGalaxyNameBtn_Click(object sender, EventArgs e)
        {
            string galaxy_lbl = $"GalaxyName_{_galaxyScenario.mName}";
            string scenario_lbl = $"ScenarioName_{_galaxyScenario.mName}{mCurrentScenario}";

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
                mCurrentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);
                applyGalaxyNameBtn.Enabled = true;
                LoadScenario(mCurrentScenario);

                if (_galaxyScenario.GetMainGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{mCurrentScenario}.canm"))
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
            foreach (AbstractObj o in mObjects)
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
            foreach (PathObj o in mPaths)
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

        private const float k_FOV = (float)((70f * Math.PI) / 180f);
        private float m_FOV = (float)(70f * Math.PI) / 180f;
        private const float k_zNear = 0.01f;
        private const float k_zFar = 1000f;
        private float m_zFar = 1000f;

        private bool m_GLLoaded;
        private float m_AspectRatio;
        private Vector2 m_CamRotation; //X is vertical?, Y is Horizontal?.
        private Vector3 m_CamPosition;
        private Vector3 m_CamTarget;
        private float m_CamDistance;
        private bool m_UpsideDown;
        private Matrix4 m_CamMatrix, m_SkyboxMatrix, m_ProjMatrix;
        private RenderInfo m_RenderInfo;

        private MouseButtons m_MouseDown;
        private Point m_LastMouseMove, m_LastMouseClick;
        private Point m_MouseCoords;
        private float m_PixelFactorX, m_PixelFactorY;

        private uint[] m_PickingFrameBuffer;
        private float m_PickingDepth;

        private bool m_OrthView = false;
        private float m_OrthZoom = 20f;

        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit;
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Cameras;
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Zones;
        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit_Lights;

        private bool m_AreChanges;
        private void RenderObjectLists(RenderMode mode)
        {
            int renderModeNo = (int)mode;

            List<StageObj> stageObjLayers = _galaxyScenario.GetMainGalaxyZone().GetAllStageDataFromLayers(_galaxyScenario.GetMainGalaxyZone().GetLayersUsedOnZoneForCurrentScenario());

            foreach (StageObj stageObj in stageObjLayers)
            {
                List<AbstractObj> objsInStage = mObjects.FindAll(o => o.mParentZone.ZoneName == stageObj.mName);
                List<PathObj> pathsInStage = mPaths.FindAll(p => p.mParentZone.ZoneName == stageObj.mName);

                foreach (AbstractObj o in objsInStage)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[renderModeNo].ContainsKey(o.mUnique))
                        continue;

                    keyValuePairs.Add(o.mUnique, GL.GenLists(1));
                    mDispLists[renderModeNo].Add(o.mUnique, GL.GenLists(1));

                    if (o.mType == "AreaObj" && AreaToolStripMenuItem.Checked == false && mode != RenderMode.Picking)
                        continue;

                    GL.NewList(mDispLists[renderModeNo][o.mUnique], ListMode.Compile);

                    GL.PushMatrix();
                    {
                        GL.Translate(stageObj.mPosition);
                        GL.Rotate(stageObj.mRotation.Z, 0f, 0f, 1f);
                        GL.Rotate(stageObj.mRotation.Y, 0f, 1f, 0f);
                        GL.Rotate(stageObj.mRotation.X, 1f, 0f, 0f);
                    }

                    if (mode == RenderMode.Picking)
                    {
                        GL.Color4((byte)o.mPicking.R, (byte)o.mPicking.G, (byte)o.mPicking.B, (byte)0xFF);
                    }

                    o.Render(mode);
                    GL.PopMatrix();

                    GL.EndList();
                }

                foreach (PathObj p in pathsInStage)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[renderModeNo].ContainsKey(p.mUnique))
                        continue;

                    keyValuePairs.Add(p.mUnique, GL.GenLists(1));
                    mDispLists[renderModeNo].Add(p.mUnique, GL.GenLists(1));

                    if (pathsToolStripMenuItem.Checked == false && mode != RenderMode.Picking)
                        continue;

                    GL.NewList(mDispLists[renderModeNo][p.mUnique], ListMode.Compile);

                    GL.PushMatrix();

                    GL.Translate(stageObj.mPosition);
                    GL.Rotate(stageObj.mRotation.Z, 0f, 0f, 1f);
                    GL.Rotate(stageObj.mRotation.Y, 0f, 1f, 0f);
                    GL.Rotate(stageObj.mRotation.X, 1f, 0f, 0f);
                    p.Render(mode);
                    GL.PopMatrix();

                    GL.EndList();
                }
                // and now we just do the regular stage
                List<AbstractObj> regularObjs = mObjects.FindAll(o => o.mParentZone.ZoneName == _galaxyScenario.mName);
                List<PathObj> regularPaths = mPaths.FindAll(p => p.mParentZone.ZoneName == _galaxyScenario.mName);

                foreach (AbstractObj o in regularObjs)
                {
                    //LevelObj level = o as LevelObj;
                    //Console.WriteLine("test "+ o.mName);
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[renderModeNo].ContainsKey(o.mUnique))
                        continue;

                    keyValuePairs.Add(o.mUnique, GL.GenLists(1));
                    mDispLists[renderModeNo].Add(o.mUnique, GL.GenLists(1));

                    if (o.mType == "AreaObj" && AreaToolStripMenuItem.Checked == false && mode != RenderMode.Picking)
                        continue;

                    GL.NewList(mDispLists[renderModeNo][o.mUnique], ListMode.Compile);

                    GL.PushMatrix();

                    if (mode == RenderMode.Picking)
                    {
                        GL.Color4((byte)o.mPicking.R, (byte)o.mPicking.G, (byte)o.mPicking.B, (byte)0xFF);
                    }

                    o.Render(mode);
                    GL.PopMatrix();

                    GL.EndList();
                }

                foreach (PathObj path in regularPaths)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[renderModeNo].ContainsKey(path.mUnique))
                        continue;

                    keyValuePairs.Add(path.mUnique, GL.GenLists(1));
                    mDispLists[renderModeNo].Add(path.mUnique, GL.GenLists(1));

                    if (pathsToolStripMenuItem.Checked == false && mode != RenderMode.Picking)
                        continue;

                    GL.NewList(mDispLists[renderModeNo][path.mUnique], ListMode.Compile);

                    GL.PushMatrix();

                    path.Render(mode);

                    GL.PopMatrix();

                    GL.EndList();
                }
            }
        }

        private void UpdateViewport()
        {
            GL.Viewport(glLevelView.ClientRectangle);

            m_AspectRatio = (float)glLevelView.Width / (float)glLevelView.Height;
            GL.MatrixMode(MatrixMode.Projection);
            m_ProjMatrix = Matrix4.CreatePerspectiveFieldOfView(k_FOV, m_AspectRatio, k_zNear, k_zFar);
            GL.LoadMatrix(ref m_ProjMatrix);

            m_PixelFactorX = ((2f * (float)Math.Tan(k_FOV / 2f) * m_AspectRatio) / (float)(glLevelView.Width));
            m_PixelFactorY = ((2f * (float)Math.Tan(k_FOV / 2f)) / (float)(glLevelView.Height));
        }

        private void UpdateCamera()
        {
            Vector3 up;

            if (Math.Cos(m_CamRotation.Y) < 0)
            {
                m_UpsideDown = true;
                up = new Vector3(0.0f, -1.0f, 0.0f);
            }
            else
            {
                m_UpsideDown = false;
                up = new Vector3(0.0f, 1.0f, 0.0f);
            }

            m_CamPosition.X = m_CamDistance * (float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            m_CamPosition.Y = m_CamDistance * (float)Math.Sin(m_CamRotation.Y);
            m_CamPosition.Z = m_CamDistance * (float)Math.Sin(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);

            Vector3 skybox_target;
            skybox_target.X = -(float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            skybox_target.Y = -(float)Math.Sin(m_CamRotation.Y);
            skybox_target.Z = -(float)Math.Sin(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);

            Vector3.Add(ref m_CamPosition, ref m_CamTarget, out m_CamPosition);

            m_CamMatrix = Matrix4.LookAt(m_CamPosition, m_CamTarget, up);
            m_SkyboxMatrix = Matrix4.LookAt(Vector3.Zero, skybox_target, up);
            m_CamMatrix = Matrix4.Mult(Matrix4.CreateScale(0.0001f), m_CamMatrix);
        }

        private void UpdateCamera(Vector3 v3)
        {
            Vector3 up;
            m_CamRotation = new Vector2(v3.X, v3.Y);
            if (Math.Cos(m_CamRotation.Y) < 0)
            {
                m_UpsideDown = true;
                up = new Vector3(0.0f, -1.0f, 0.0f);
            }
            else
            {
                m_UpsideDown = false;
                up = new Vector3(0.0f, 1.0f, 0.0f);
            }

            m_CamPosition.X = m_CamDistance * (float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            m_CamPosition.Y = m_CamDistance * (float)Math.Sin(m_CamRotation.Y);
            m_CamPosition.Z = m_CamDistance * (float)Math.Sin(v3.Z) * (float)Math.Cos(m_CamRotation.Y);

            Vector3 skybox_target = v3;
            //skybox_target.X = -(float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            //skybox_target.Y = -(float)Math.Sin(m_CamRotation.Y);
            //skybox_target.Z = -(float)Math.Sin(v3.Z) * (float)Math.Cos(m_CamRotation.Y);

            Vector3.Add(ref m_CamPosition, ref m_CamTarget, out m_CamPosition);

            m_CamMatrix = Matrix4.LookAt(m_CamPosition, m_CamTarget, up);
            m_SkyboxMatrix = Matrix4.LookAt(Vector3.One, skybox_target, up);
            m_CamMatrix = Matrix4.Mult(Matrix4.CreateScale(0.0001f), m_CamMatrix);
        }

        [Obsolete]
        private void glLevelView_Paint(object sender, PaintEventArgs e)
        {
            if (!m_GLLoaded) return;
            glLevelView.MakeCurrent();

            /* step one -- fakecolor rendering */
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.ClearDepth(1f);
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref m_CamMatrix);

            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ColorLogicOp);
            GL.Disable(EnableCap.Dither);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.PolygonSmooth);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Lighting);

            foreach (KeyValuePair<int, Dictionary<int, int>> disp in mDispLists)
            {
                if (disp.Key != 2)
                    continue;

                foreach (KeyValuePair<int, int> actualList in disp.Value)
                    GL.CallList(actualList.Value);
            }

            GL.DepthMask(true);
            GL.Flush();

            GL.ReadPixels(m_MouseCoords.X - 1, glLevelView.Height - m_MouseCoords.Y + 1, 3, 3, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, m_PickingFrameBuffer);
            GL.ReadPixels(m_MouseCoords.X, glLevelView.Height - m_MouseCoords.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref m_PickingDepth);
            m_PickingDepth = -(m_zFar * k_zNear / (m_PickingDepth * (m_zFar - k_zNear) - m_zFar));

            /* actual rendering */
            GL.DepthMask(true);
            GL.ClearColor(0.0f, 0.0f, 0.125f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.Enable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Dither);
            GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.PolygonSmooth);

            GL.LoadMatrix(ref m_CamMatrix);

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

            mDispLists[0].Values.ToList().ForEach(l => GL.CallList(l));
            mDispLists[1].Values.ToList().ForEach(l => GL.CallList(l));

            glLevelView.SwapBuffers();
        }

        private void glLevelView_MouseMove(object sender, MouseEventArgs e)
        {
            float xdelta = (float)(e.X - m_LastMouseMove.X);
            float ydelta = (float)(e.Y - m_LastMouseMove.Y);

            //Console.WriteLine($"{m_PickingFrameBuffer[0]}");

            m_MouseCoords = e.Location;
            m_LastMouseMove = e.Location;

            if (m_MouseDown != MouseButtons.None)
            {
                if (m_MouseDown == MouseButtons.Right)
                {
                    if (m_UpsideDown)
                        xdelta = -xdelta;

                    m_CamRotation.X -= xdelta * 0.002f;
                    m_CamRotation.Y -= ydelta * 0.002f;
                }
                else if (m_MouseDown == MouseButtons.Left)
                {
                    xdelta *= 0.005f;
                    ydelta *= 0.005f;

                    m_CamTarget.X -= xdelta * (float)Math.Sin(m_CamRotation.X);
                    m_CamTarget.X -= ydelta * (float)Math.Cos(m_CamRotation.X) * (float)Math.Sin(m_CamRotation.Y);
                    m_CamTarget.Y += ydelta * (float)Math.Cos(m_CamRotation.Y);
                    m_CamTarget.Z += xdelta * (float)Math.Cos(m_CamRotation.X);
                    m_CamTarget.Z -= ydelta * (float)Math.Sin(m_CamRotation.X) * (float)Math.Sin(m_CamRotation.Y);
                }

                UpdateCamera();
            }

            glLevelView.Refresh();
        }

        private void glLevelView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != m_MouseDown) return;
            var rayTest1 = ScreenToRay(e.Location);
            // this checks to make sure that we are just clicking, and not coming off of a drag while left clicking
            if ((Math.Abs(e.X - m_LastMouseClick.X) < 3) && (Math.Abs(e.Y - m_LastMouseClick.Y) < 3) &&
                (m_PickingFrameBuffer[4] == m_PickingFrameBuffer[1]) &&
                (m_PickingFrameBuffer[4] == m_PickingFrameBuffer[3]) &&
                (m_PickingFrameBuffer[4] == m_PickingFrameBuffer[5]) &&
                (m_PickingFrameBuffer[4] == m_PickingFrameBuffer[7]))
            {
                uint color = m_PickingFrameBuffer[4];
                Color clr = EditorUtil.UIntToColor(color);

                int id = EditorUtil.ColorHolder.IDFromColor(clr);

                foreach (string z in mZonesUsed)
                {
                    Zone zone = _galaxyScenario.GetZone(z);
                    AbstractObj obj = zone.GetObjFromUniqueID(id);

                    if (obj == null)
                    {
                        continue;
                    }

                    // if the current seleted object is the same, we deselect
                    if (obj == mSelectedObject)
                    {
                        mSelectedObject = null;
                        return;
                    }

                    mSelectedObject = obj;

                    if (!(obj is LevelObj)) 
                    {
                        return;
                    } 

                    //選択したオブジェクトのBMDファイル内の三角面情報の一覧を取得する。
                    BMDInfo.BMDTriangleData bmdTriangleData = BMDInfo.GetTriangles(obj);
                    glLevelView.SwapBuffers();

                    //カメラのRayと三角面の位置情報から交差判定を行う。

                    float nearest_hitpoint_distance = float.MaxValue; // 最も近い交差点とカメラとの距離を記録する
                    Vector3? nearest_hitpoint_position = null; // 最も近い交差点の交差位置を記録する



                    // クリックしたオブジェクトが含む三角面の数だけ繰り返す

                    float t, u, v;
                    foreach (var triangle in bmdTriangleData.TriangleDataList)
                    {
                        var objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                        var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
                        var positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mTruePosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

                        Vector3 v0 = Vector3.Add(positionWithZoneRotation,triangle.V0.Xyz)*10000f;
                        Vector3 v1 = Vector3.Add(positionWithZoneRotation, triangle.V1.Xyz) * 10000f;
                        Vector3 v2 = Vector3.Add(positionWithZoneRotation, triangle.V2.Xyz) * 10000f;
                        Vector3 normal_vector = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

                        // 交差点の位置ベクトルは Ray.org + t * Ray.dir = v0 + u(v1-v0) + v(v2-v0)
                        t = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Origin - v0, v1 - v0), v2 - v0);
                        u = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Direction, v2 - v0), rayTest1.Origin - v0);
                        v = (1.0f / Vector3.Dot(Vector3.Cross(rayTest1.Direction, (v2 - v0)), (v1 - v0))) * Vector3.Dot(Vector3.Cross(rayTest1.Origin - v0, v1 - v0), rayTest1.Direction);

                        // この条件に引っかかればその三角面とは交差していない
                        // 三角面を含む平面について，レイと平面の交点は三角面の外側 もしくは レイが三角面の裏面から入射
                        if (((u < 0.0f || v < 0.0f) || (u + v > 1.0f)) || (Vector3.Dot(rayTest1.Direction, normal_vector) >= 0.0f))
                        {
                            //Debug.WriteLine($"DEBUG: the position you clicked is t:{t} u:{u} v:{v}");
                            continue;
                        }
                            

                        // 三角面とマウスクリックした点におけるカメラの視線は交差している

                        // これまでの交差点において最も近い交差点を確認する．
                        // この条件に引っかかればこれまでの任意の交差点よりも近くの交差点である．
                        if(t < nearest_hitpoint_distance)
                        {
                            nearest_hitpoint_distance = t;

                            //元のコード
                            //nearest_hitpoint_position = rayTest1.Origin + t * rayTest1.Direction;
                            nearest_hitpoint_position = new Vector3(Vector3.Add(rayTest1.Origin,Vector3.Multiply(rayTest1.Direction,t))) ;
                        }
                    }

                    // nearest_hitpoint_position =: クリックした3次元座標

                    // if条件: どの三角面とも交差していない場合にif内部に入る(何もしない)
                    if (nearest_hitpoint_distance == float.MaxValue)
                    {
                        

                        MessageBox.Show("交点なし");

                        Debug.WriteLine("DEBUG★: the position you clicked is " + nearest_hitpoint_position.ToString());
                        //return;
                    }
                    else 
                    {
                        textBox1.Text = "☆DEBUG☆: the position you clicked is " + nearest_hitpoint_position.ToString();
                        Debug.WriteLine("☆DEBUG☆: the position you clicked is " + nearest_hitpoint_position.ToString());
                    } 



                    if (AddObjectWindow.AddTargetObject != null && nearest_hitpoint_position !=null)
                    {
                        glLevelView.SwapBuffers();

                        var raytest2 = ScreenToRay(e.Location);
                        //Console.WriteLine($"RayTest::{raytest2.Origin}{raytest2.Direction}");

                        var objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                        var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
                        var positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mTruePosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

                        //カメラ位置とオブジェクト原点の距離の中間の座標にオブジェクトをセットします。
                        //レイの方向の制御は行っていません

                        var rayPos = Vector3.Multiply(raytest2.Origin, 10000f);
                        var selectedObjGlobalPosition = objTruePos + positionWithZoneRotation;

                        //距離の計算
                        var ToCameraFromObj3d = new Vector3(selectedObjGlobalPosition - rayPos);

                        ToCameraFromObj3d = new Vector3((float)Math.Pow(Math.Abs(ToCameraFromObj3d.X), 2), (float)Math.Pow(Math.Abs(ToCameraFromObj3d.Y), 2), (float)Math.Pow(Math.Abs(ToCameraFromObj3d.Z), 2));

                        var ToCameraFromObj = ToCameraFromObj3d.X + ToCameraFromObj3d.Y + ToCameraFromObj3d.Z;

                        ToCameraFromObj = (float)Math.Sqrt(ToCameraFromObj);

                        //AddObjectWindow.AddTargetObject.SetPosition((rayPos + Vector3.Multiply(raytest2.Direction/*obj.mTruePosition*/, ToCameraFromObj)) / 2);
                        AddObjectWindow.AddTargetObject.SetPosition(new Vector3((float)nearest_hitpoint_position.Value.X, (float)nearest_hitpoint_position.Value.Y,(float)nearest_hitpoint_position.Value.Z/*Vector3.Multiply(raytest2.Direction, ToCameraFromObj) + rayPos*/));

                        if (AddObjectWindow.IsChanged)
                        {
                            var objCount = AddObjectWindow.Objects.Count();
                            mObjects.Add(AddObjectWindow.Objects[objCount - 1]);


                           
                            Scenario_ReLoad();
                            SelectTreeNodeWithUnique(AddObjectWindow.AddTargetObject.mUnique);
                            ChangeToNode(objectsListTreeView.SelectedNode, false);

                            AddObjectWindow.AddTargetObject = null;
                        }
                        break;
                    }
                    if (obj is PathPointObj)
                    {
                        SelectTreeNodeWithUnique(id);
                        break;
                    }
                    else
                    {
                        SelectTreeNodeWithUnique(obj.mUnique);
                        break;
                    }


                }
            }

            m_MouseDown = MouseButtons.None;
            m_LastMouseMove = e.Location;
        }

        private void glLevelView_MouseDown(object sender, MouseEventArgs e)
        {
            var raytest2 = ScreenToRay(e.Location);

            //textBox1.Text = raytest2.Direction.ToString();

            
            GL.PushMatrix();
            GL.LineWidth(3.0f);
            GL.Begin(BeginMode.Lines);
            GL.Color3(1f, 0f, 1f);
            GL.Vertex3(raytest2.Origin * 10000.0f);
            GL.Vertex3(raytest2.Origin * 10000.0f + (1000000f * raytest2.Direction));
            GL.End();
            GL.PopMatrix();
            glLevelView.SwapBuffers();

            if (m_MouseDown != MouseButtons.None) return;

            m_MouseDown = e.Button;
            m_LastMouseMove = m_LastMouseClick = e.Location;

            

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
                dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, stageObj);
                dataGridView1 = dataGridViewEdit.GetDataTable();
                mSelectedObject = stageObj;
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

                    m_CamDistance = 0.200f;
                    m_CamTarget = Pos_ZoneOffset / 10000f + CorrectPos_Object / 10000;
                    m_CamPosition = CorrectPos_Object / 10000;
                    m_CamRotation.Y = (float)Math.PI / 8f;
                    m_CamRotation.X = (-(abstractObj.mTrueRotation.Y + Pos_ZoneOffset.Y) / 180f) * (float)Math.PI;
                }

                //objects PropertyGrideSetting
                //Display the property grid for setting the currently selected object.
                //Note: These processes are not related to the camera's processing.

                dataGridView1.DataSource = null;
                dataGridViewEdit = null;
                mSelectedObject = abstractObj;

                switch (node.Parent.Text)
                {
                    case "Objects":
                        if (!(abstractObj is LevelObj))
                            throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(LevelObj) }」 .");
                        LevelObj obj = abstractObj as LevelObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, obj);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Areas":
                        //AreaObj area = abstractObj as AreaObj;
                        //dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, area);
                        //dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                        if (!(abstractObj is AreaObj))
                            throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(AreaObj) }」 .");
                        AreaObj areaobj = abstractObj as AreaObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, areaobj);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Gravity":
                        PlanetObj planetObj = abstractObj as PlanetObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, planetObj);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Camera Areas":
                        CameraObj cameraObj = abstractObj as CameraObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, cameraObj);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Debug Movement":
                        DebugMoveObj debug = abstractObj as DebugMoveObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, debug);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Map Parts":
                        MapPartsObj mapparts = abstractObj as MapPartsObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, mapparts);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Demos":
                        DemoObj demo = abstractObj as DemoObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, demo);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Starting Points":
                        StartObj start = abstractObj as StartObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, start);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
                        break;
                    case "Paths":
                        PathObj path = abstractObj as PathObj;
                        dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, path);
                        dataGridView1 = dataGridViewEdit.GetDataTable();
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
                    dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, pathPoint);
                    dataGridView1 = dataGridViewEdit.GetDataTable();
                }

                if (pathsToolStripMenuItem.Checked == false)
                {
                    if (abstractObj.CanUsePath())
                    {
                        Zone z = abstractObj.mParentZone;
                        // we need to delete any rendered paths
                        List<int> ids = z.GetAllUniqueIDsFromObjectsOfType("PathObj");
                        ids.ForEach(i => GL.DeleteLists(mDispLists[0][i], 1));
                        PathObj path = z.GetPathFromID(abstractObj.mEntry.Get<short>("CommonPath_ID"));

                        if (path != null)
                        {
                            // now we render only this path
                            var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(path.mParentZone.ZoneName);
                            var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(path.mParentZone.ZoneName);

                            GL.DeleteLists(mDispLists[0][path.mUnique], 1);
                            GL.NewList(mDispLists[0][path.mUnique], ListMode.Compile);

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

            dataGridViewEdit.ChangeValue(e.RowIndex, cell_value);

            if (mSelectedObject.GetType() == typeof(PathPointObj))
            {
                PathPointObj obj = mSelectedObject as PathPointObj;
                PathObj path = obj.mParent;

                var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(mSelectedObject.mParentZone.ZoneName);
                var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(mSelectedObject.mParentZone.ZoneName);

                GL.DeleteLists(mDispLists[0][path.mUnique], 1);
                GL.NewList(mDispLists[0][path.mUnique], ListMode.Compile);

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
            else if (mSelectedObject.GetType() == typeof(StageObj))
            {
                StageObj stageObj = mSelectedObject as StageObj;
                Zone z = _galaxyScenario.GetZone(stageObj.mName);
                List<int> ids = z.GetAllUniqueIDsFromZoneOnCurrentScenario();

                foreach (int id in ids)
                {
                    GL.DeleteLists(mDispLists[0][id], 1);
                    GL.NewList(mDispLists[0][id], ListMode.Compile);

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
                var Pos_ZoneOffset = _galaxyScenario.Get_Pos_GlobalOffset(mSelectedObject.mParentZone.ZoneName);
                var Rot_ZoneOffset = _galaxyScenario.Get_Rot_GlobalOffset(mSelectedObject.mParentZone.ZoneName);

               GL.DeleteLists(mDispLists[0][mSelectedObject.mUnique], 1);
                GL.NewList(mDispLists[0][mSelectedObject.mUnique], ListMode.Compile);

                GL.PushMatrix();
                {
                    GL.Translate(Pos_ZoneOffset);
                    GL.Rotate(Rot_ZoneOffset.Z, 0f, 0f, 1f);
                    GL.Rotate(Rot_ZoneOffset.Y, 0f, 1f, 0f);
                    GL.Rotate(Rot_ZoneOffset.X, 1f, 0f, 0f);
                }

                mSelectedObject.Render(RenderMode.Opaque);
                GL.PopMatrix();
                GL.EndList();
            }

            m_AreChanges = true;
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
                        GL.DeleteLists(mDispLists[0][id], 1);
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

                        GL.DeleteLists(mDispLists[0][area.mUnique], 1);
                        GL.NewList(mDispLists[0][area.mUnique], ListMode.Compile);

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
            m_AreChanges = false;
            OpenSaveStatusLabel.Text = "Changes Saved : SaveTime : " + DateTime.Now;
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mStages.Clear();
            mZonesUsed.Clear();
            mZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            mPaths.Clear();
            mObjects.Clear();
            mDispLists.Clear();
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
            if (dataGridView1.CurrentCellAddress.X == 0 && dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
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

        private void glLevelView_MouseClick(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right) return;
            var ray = ScreenToRay(e.Location);
            System.Console.WriteLine("Ray rad.(X,Y,Z)");
            System.Console.WriteLine(ray.Direction.X);
            System.Console.WriteLine(ray.Direction.Y);
            System.Console.WriteLine(ray.Direction.Z);
            GL.Begin(BeginMode.LineStrip);
            GL.Color3(1f, 0f, 0f);
            GL.Vertex3(ray.Origin);
            GL.Vertex3(ray.Origin + 100000f * ray.Direction);
            GL.End();
            Debug.WriteLine("camera ray direction: " + ray.Direction);
        }

        private Ray ScreenToRay(Point mousePos)//カメラZ軸非対応(Incompatible :: cam Z rotate.)
        {
            //namespace System(cppLang & memo)
            //GL系の座標で計算してSMG系の座標に変換しています。（vector2のX座標がSMGのZ座標のため）

            Vector2 mousePosRayRad = new Vector2();
            float k_FOV_RadPerDot = k_FOV / glLevelView.Height;
            float k_FOV_harf = k_FOV / 2;
            //(mouse move)y rad
            mousePosRayRad.Y = k_FOV_harf - (k_FOV_RadPerDot * mousePos.Y);
            //x rad
            mousePosRayRad.X = ((k_FOV_harf * m_AspectRatio) - ((k_FOV_RadPerDot) * mousePos.X));

            //convert.
            Vector3 rotateRad = new Vector3(-m_CamRotation.Y, -m_CamRotation.X, 0f);

            //x rotate only.(view y rotate)
            rotateRad += new Vector3(mousePosRayRad.Y,
                                     0f,
                                     0f
                                     );

            //yz rotate add.(view x rotate)
            Vector3 ray = new Vector3((float)((Math.Sin(rotateRad.Y) * Math.Cos(rotateRad.X)) + (Math.Cos(rotateRad.Y) * Math.Sin(mousePosRayRad.X))),
                                      (float)((Math.Sin(rotateRad.X) * (Math.Cos(mousePosRayRad.X)))),
                                      (float)((Math.Cos(rotateRad.Y) * Math.Cos(rotateRad.X)) + (Math.Sin(-rotateRad.Y) * Math.Sin(mousePosRayRad.X)))
                                      );

            ray = new Vector3(-ray.Z, ray.Y, ray.X);

            ray = Vector3.Normalize(ray);


            return new Ray(m_CamTarget, ray);
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
                        GL.DeleteLists(mDispLists[0][id], 1);
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

                        GL.DeleteLists(mDispLists[0][path.mUnique], 1);
                        GL.NewList(mDispLists[0][path.mUnique], ListMode.Compile);

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
                            GL.DeleteLists(mDispLists[0][uniqueID], 1);
                            objectsListTreeView.Nodes.Remove(node);
                            m_AreChanges = true;
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
                GL.DeleteLists(mDispLists[0][uniqueID], 1);
                objectsListTreeView.Nodes.Remove(node);
                m_AreChanges = true;
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

                GL.DeleteLists(mDispLists[0][parentPath.mUnique], 1);
                GL.NewList(mDispLists[0][parentPath.mUnique], ListMode.Compile);

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
                m_AreChanges = true;
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
                        if (mDispLists[0].ContainsKey(id))
                        {
                            GL.DeleteLists(mDispLists[0][id], 1);
                        }
                    }

                    objectsListTreeView.Nodes.Remove(node);

                    m_AreChanges = true;
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

                    GL.DeleteLists(mDispLists[0][obj.mUnique], 1);
                    GL.NewList(mDispLists[0][obj.mUnique], ListMode.Compile);

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

                m_AreChanges = true;
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

                    GL.DeleteLists(mDispLists[0][obj.mUnique], 1);
                    GL.NewList(mDispLists[0][obj.mUnique], ListMode.Compile);

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

                m_AreChanges = true;
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
            dataGridViewEdit_Cameras = new EditorWindowSys.DataGridViewEdit(camerasDataGridView, camera);
            camerasDataGridView = dataGridViewEdit_Cameras.GetDataTable(camera);
        }

        private void glLevelView_Resize(object sender, EventArgs e)
        {
            if (!m_GLLoaded) return;
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

            m_CamRotation = new Vector2(0.0f, 0.0f);
            m_CamTarget = new Vector3(0.0f, 0.0f, 0.0f);
            m_CamDistance = 1f;// 700.0f;

            m_RenderInfo = new RenderInfo();

            UpdateViewport();
            Vector3 CameraDefaultVector3 = new Vector3(0f, 0f, 0f);
            m_GLLoaded = true;
            m_CamDistance = 0.200f;
            m_CamTarget = CameraDefaultVector3;
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
                    if (e.Delta > 0) m_CamRotation.Y += ((float)Math.PI / 180) * 90;
                    if (e.Delta < 0) m_CamRotation.Y += ((float)Math.PI / 180) * -90;
                }
                else
                {
                    if (e.Delta > 0) m_CamRotation.X += ((float)Math.PI / 180) * -90;
                    if (e.Delta < 0) m_CamRotation.X += ((float)Math.PI / 180) * 90;
                }
                Console.WriteLine(e.Delta);
            }
            else
            {
                float delta = -((e.Delta / 120f) * 0.1f);
                m_CamTarget.X += delta * (float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
                m_CamTarget.Y += delta * (float)Math.Sin(m_CamRotation.Y);
                m_CamTarget.Z += delta * (float)Math.Sin(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            }
            UpdateCamera();

            glLevelView.Refresh();
        }

        #endregion
    }
}
