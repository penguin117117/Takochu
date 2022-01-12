﻿using System;
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


namespace Takochu.ui
{
    public partial class EditorWindow : Form
    {
        public EditorWindow(string galaxyName)
        {
            InitializeComponent();
            mGalaxyName = galaxyName;

            if (GameUtil.IsSMG1())
                saveGalaxyBtn.Enabled = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            //base.OnLoad(e);

            mGalaxy = Program.sGame.OpenGalaxy(mGalaxyName);
            galaxyNameTxtBox.Text = mGalaxy.mGalaxyName;

            foreach(KeyValuePair<int, Scenario> scenarios in mGalaxy.mScenarios)
            {
                Scenario s = scenarios.Value;
                TreeNode n = new TreeNode($"[{s.mScenarioNo}] {s.mScenarioName}")
                {
                    Tag = s.mScenarioNo
                };

                scenarioTreeView.Nodes.Add(n);
            }

            if (!BGMInfo.HasBGMInfo(mGalaxy.mName))
                stageInformationBtn.Enabled = false;
        }

        private string mGalaxyName;
        public int mCurrentScenario;

        private Galaxy mGalaxy;
        private List<AbstractObj> mObjects = new List<AbstractObj>();
        private List<PathObj> mPaths = new List<PathObj>();
        Dictionary<string, List<StageObj>> mStages = new Dictionary<string, List<StageObj>>();
        List<string> mZonesUsed = new List<string>();

        Dictionary<string, int> mZoneMasks = new Dictionary<string, int>();

        Dictionary<int, Dictionary<int, int>> mDispLists = new Dictionary<int, Dictionary<int, int>>();
        
        
        public void LoadScenario(int scenarioNo)
        {
            mStages.Clear();
            mZonesUsed.Clear();
            mZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            mPaths.Clear();

            mObjects.Clear();

            foreach (KeyValuePair<int, Dictionary<int, int>> disp in mDispLists)
            {
                foreach (KeyValuePair<int, int> actualList in disp.Value)
                    GL.DeleteLists(actualList.Value, 1);
            }

            mDispLists.Clear();
            mDispLists.Add(0, new Dictionary<int, int>());
            mDispLists.Add(1, new Dictionary<int, int>());
            mDispLists.Add(2, new Dictionary<int, int>());

            //for (int i = 0; i < 3; i++)
            //    GL.DeleteLists(mDisplayLists[i], 1);

            // we want to clear out the children of the 5 camera type root nodes
            //for (int i = 0; i < 5; i++)
            //    camerasTree.Nodes[i].Nodes.Clear();

            mGalaxy.SetScenario(scenarioNo);
            scenarioNameTxtBox.Text = mGalaxy.mCurScenarioName;

            // first we need to get the proper layers that the galaxy itself uses
            List<string> layers = GameUtil.GetGalaxyLayers(mGalaxy.GetMaskUsedInZoneOnCurrentScenario(mGalaxyName));

            layers.ForEach(l => layerViewerDropDown.DropDownItems.Add(l));

            // get our main galaxy's zone
            Zone mainZone = mGalaxy.GetZone(mGalaxyName);
            //Console.WriteLine(mGalaxyName);

            // now we get the zones used on these layers
            // add our galaxy name itself so we can properly add it to a scene list with the other zones
            mZonesUsed.Add(mGalaxyName);
            mZonesUsed.AddRange(mainZone.GetZonesUsedOnLayers(layers));

            Dictionary<string, List<Camera>> cameras = new Dictionary<string, List<Camera>>();
            List<Light> lights = new List<Light>();

            List<string> currentLayers = new List<string>();

            Zone galaxyZone = mGalaxy.GetGalaxyZone();
            mObjects.AddRange(galaxyZone.GetAllObjectsFromLayers(layers));
            //Console.WriteLine(mZonesUsed.Count);
            //Console.WriteLine("\n\r\n\r");
            foreach (string zone in mZonesUsed)
            {
                mZoneMasks.Add(zone, mGalaxy.GetMaskUsedInZoneOnCurrentScenario(zone));

                TreeNode zoneNode = new TreeNode()
                {
                    Tag = zone,
                    Text = zone,
                    Name = zone
                };


                AssignNodesToZoneNode(ref zoneNode);

                objectsListTreeView.Nodes.Add(zoneNode);

                Zone z = mGalaxy.GetZone(zone);

                currentLayers.AddRange(GameUtil.GetGalaxyLayers(mZoneMasks[zone]));

                if (GameUtil.IsSMG1())
                    currentLayers = currentLayers.ConvertAll(l => l.ToLower());

                List<AbstractObj> objs = z.GetAllObjectsFromLayers(currentLayers);

                mPaths.AddRange(z.mPaths);

                cameras.Add(zone, z.mCameras);

                if (z.mLights != null)
                    lights.AddRange(z.mLights);

                //mCurrentLayers = GameUtil.GetGalaxyLayers(zoneMasks[mGalaxy.mName]);

                // the main galaxy is always loaded before we get into this block
                // so we can do all of our offsetting here
                var TestLayers = z.GetLayersUsedOnZoneForCurrentScenario();
                if (!z.mIsMainGalaxy)
                {
                    if (GameUtil.IsSMG1())
                        currentLayers = currentLayers.ConvertAll(l => l.ToLower());
                    //Console.WriteLine("____________________addrange");
                    mObjects.AddRange(z.GetAllObjectsFromLayers(TestLayers));
                    //mObjects.AddRange(z.GetAllObjectsFromLayers(currentLayers));
                    //mGalaxy
                    //var layername = z.GetLayersUsedOnZoneForCurrentScenario()[z.mGalaxy.mScenarioNo];
                    //mObjects.AddRange(z.GetObjectsFromLayer("Map",layername)) ;
                    //Console.WriteLine(layername);
                }
                else
                {
                    foreach (string layer in /*currentLayers*/TestLayers)
                    {

                        List<StageObj> stgs = galaxyZone.mZones[layer];

                        mStages.Add(layer, stgs);
                    }
                }
            }
            //Console.WriteLine("_________________________________________?");
            List<Camera> cubeCameras = new List<Camera>();
            List<Camera> groupCameras = new List<Camera>();
            List<Camera> eventCameras = new List<Camera>();
            List<Camera> startCameras = new List<Camera>();
            List<Camera> otherCameras = new List<Camera>();

            foreach (string zone in mZonesUsed)
            {
                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Cube)
                        cubeCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Group)
                        groupCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Event)
                        eventCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Start)
                        startCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Other)
                        otherCameras.Add(c);
                });
            }

            PopulateTreeView();

            //RenderObjectLists(RenderMode.Picking);
            RenderObjectLists(RenderMode.Opaque);
            //RenderObjectLists(RenderMode.Translucent);
        }

        private void RenderZone(List<AbstractObj> objs)
        {
            foreach(AbstractObj obj in objs)
                obj.Render(RenderMode.Opaque);
        }

        private void RenderZone2(AbstractObj objs)
        {
            
                objs.Render(RenderMode.Opaque);
        }

        private void scenarioTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

        private void scenarioTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (scenarioTreeView.SelectedNode != null)
            {
                mCurrentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);
                
                applyGalaxyNameBtn.Enabled = true;
                LoadScenario(mCurrentScenario);

                sw.Stop();
                Console.WriteLine($"LoadScenario: {sw.Elapsed}");
                sw.Reset();
                sw.Start();

                if (mGalaxy.GetGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{mCurrentScenario}.canm"))
                    introCameraEditorBtn.Enabled = true;
                else
                    introCameraEditorBtn.Enabled = false;

                sw.Stop();
                Console.WriteLine($"GetGalaxyZone: {sw.Elapsed}");
                sw.Reset();
                sw.Start();
            }
            sw.Stop();
            mGalaxy.GetGalaxyZone().LoadCameras();
            Console.WriteLine($"LoadCameras: {sw.Elapsed}");
            sw.Reset();
            sw.Start();
            UpdateCamera();
            glLevelView.Refresh();
            sw.Stop();
            Console.WriteLine($"ScenarioTreeViewFinish: {sw.Elapsed}");
        }

        private void EditorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {

            //オブジェクトのプロパティに変更がある場合警告を表示します
            //Display a warning when there are changes to the object's properties
            DialogResult dr;
            if (EditorWindowSys.DataGridViewEdit.IsChanged) 
            {
                dr = Translate.GetMessageBox.Show(MessageBoxText.ChangesNotSaved,MessageBoxCaption.Error,MessageBoxButtons.YesNo);
                if ((dr == DialogResult.No) || (dr == DialogResult.Cancel)) { e.Cancel = true; return; }
            }

            mGalaxy.Close();
            
        }

        private void EditorWindow_Load(object sender, EventArgs e)
        {

        }

        private void galaxyViewControl_Load(object sender, EventArgs e)
        {

        }
        private void openMsgEditorButton_Click(object sender, EventArgs e)
        {
            MessageEditor editor = new MessageEditor(ref mGalaxy);
            editor.Show();
        }

        private void saveGalaxyBtn_Click(object sender, EventArgs e)
        {
            if (GameUtil.IsSMG1()) 
            {
                Translate.GetMessageBox.Show(MessageBoxText.UnimplementedFeatures,MessageBoxCaption.Info);
                return;
            }
            mGalaxy.Save();
            EditorWindowSys.DataGridViewEdit.IsChangedClear();
        }

        
        private void closeEditorBtn_Click(object sender, EventArgs e)
        {
            //Do I need this feature?
            //Erase the return if you need to.
            return;
            mStages.Clear();
            mZonesUsed.Clear();
            mZoneMasks.Clear();
            layerViewerDropDown.DropDownItems.Clear();
            objectsListTreeView.Nodes.Clear();
            mPaths.Clear();

            mObjects.Clear();
            mDispLists.Clear();
            glLevelView.Dispose();
            mGalaxy.Close();


        }

        private void stageInformationBtn_Click(object sender, EventArgs e)
        {
            StageInfoEditor stageInfo = new StageInfoEditor(ref mGalaxy, mCurrentScenario);
            stageInfo.Show();
        }

        private void applyGalaxyNameBtn_Click(object sender, EventArgs e)
        {
            string galaxy_lbl = $"GalaxyName_{mGalaxyName}";
            string scenario_lbl = $"ScenarioName_{mGalaxyName}{mCurrentScenario}";

            NameHolder.AssignToGalaxy(galaxy_lbl, galaxyNameTxtBox.Text);
            NameHolder.AssignToScenario(scenario_lbl, scenarioNameTxtBox.Text);
        }

        private void introCameraEditorBtn_Click(object sender, EventArgs e)
        {
            IntroEditor intro = new IntroEditor(ref mGalaxy);
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

                if (mGalaxy.GetGalaxyZone().mIntroCameras.ContainsKey($"StartScenario{mCurrentScenario}.canm"))
                    introCameraEditorBtn.Enabled = true;
                else
                    introCameraEditorBtn.Enabled = false;
            }
            mGalaxy.GetGalaxyZone().LoadCameras();
            UpdateCamera();
            glLevelView.Refresh();
            sw.Stop();
            Console.WriteLine("ScenarioReLoad: "+ $"{sw.Elapsed}");
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
            switch(type)
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
            foreach(AbstractObj o in mObjects)
            {
                
                string zone = o.mParentZone.mZoneName;
                int idx = GetIndexOfZoneNode(zone);
                TreeNode zoneNode = objectsListTreeView.Nodes[idx];
                
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
            foreach(PathObj o in mPaths)
            {
                string zone = o.mParentZone.mZoneName;
                int idx = GetIndexOfZoneNode(zone);
                TreeNode zoneNode = objectsListTreeView.Nodes[idx];

                TreeNode pathNode = new TreeNode()
                {
                    Text = o.ToString(),
                    Tag = o
                };

                int curIdx = 0;

                foreach(PathPointObj pobj in o.mPathPointObjs)
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
        private Vector2 m_CamRotation;
        private Vector3 m_CamPosition;
        private Vector3 m_CamTarget;
        private float m_CamDistance;
        private bool m_UpsideDown;
        private Matrix4 m_CamMatrix, m_SkyboxMatrix;
        private RenderInfo m_RenderInfo;

        private MouseButtons m_MouseDown;
        private Point m_LastMouseMove, m_LastMouseClick;
        private Point m_MouseCoords;
        private float m_PixelFactorX, m_PixelFactorY;

        private uint[] m_PickingFrameBuffer;
        private float m_PickingDepth;
        private float m_PickingModelDepth;

        private bool m_OrthView = false;
        private float m_OrthZoom = 20f;

        private static EditorWindowSys.DataGridViewEdit dataGridViewEdit;
        /*
         * 0 = Opaque
         * 1 = Translucent
         * 2 = Picking
         */
        private int[] mDisplayLists = new int[3];

        private int[] mObjectDisplayLists;

        private void RenderObjectLists(RenderMode mode)
        {
            int t = 0;

            switch(mode)
            {
                case RenderMode.Opaque:
                    t = 0;
                    break;
                case RenderMode.Translucent:
                    t = 1;
                    break;
                case RenderMode.Picking:
                    t = 2;
                    break;
            }

            /*if (mDisplayLists[t] == 0)
                mDisplayLists[t] = GL.GenLists(1);

            GL.NewList(mDisplayLists[t], ListMode.Compile);*/

            if (mode == RenderMode.Picking)
            {
                foreach(AbstractObj o in mObjects)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    keyValuePairs.Add(o.mUnique, GL.GenLists(1));
                    mDispLists.Add(t, keyValuePairs);

                    GL.NewList(mDispLists[t][o.mUnique], ListMode.Compile);

                    GL.Color4(Color.FromArgb(o.mUnique));
                    o.Render(mode);
                    //Console.WriteLine(o.mName);
                    GL.EndList();
                    
                }
            }
            else
            {
                List<StageObj> stage_layers = mGalaxy.GetGalaxyZone().GetAllStageDataFromLayers(mGalaxy.GetGalaxyZone().GetLayersUsedOnZoneForCurrentScenario());
                
                foreach(StageObj stage in stage_layers)
                {
                    List<AbstractObj> objsInStage = mObjects.FindAll(o => o.mParentZone.mZoneName == stage.mName);
                    List<PathObj> pathsInStage = mPaths.FindAll(p => p.mParentZone.mZoneName == stage.mName);

                    foreach (AbstractObj o in objsInStage)
                    {
                        Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                        if (mDispLists[t].ContainsKey(o.mUnique))
                            continue;

                        keyValuePairs.Add(o.mUnique, GL.GenLists(1));
                        mDispLists[t].Add(o.mUnique, GL.GenLists(1));

                        GL.NewList(mDispLists[t][o.mUnique], ListMode.Compile);

                        GL.PushMatrix();
                        {
                            GL.Translate(stage.mPosition);
                            GL.Rotate(stage.mRotation.Z, 0f, 0f, 1f);
                            GL.Rotate(stage.mRotation.Y, 0f, 1f, 0f);
                            GL.Rotate(stage.mRotation.X, 1f, 0f, 0f);
                        }
                        o.Render(mode);
                        GL.PopMatrix();

                        GL.EndList();
                        //mGalaxy.Get_Pos_GlobalOffset(stage.mName)
                        //Console.WriteLine("                     "+stage.mName);
                    }

                    foreach (PathObj p in pathsInStage)
                    {
                        Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                        if (mDispLists[t].ContainsKey(p.mUnique))
                            continue;

                        keyValuePairs.Add(p.mUnique, GL.GenLists(1));
                        mDispLists[t].Add(p.mUnique, GL.GenLists(1));

                        GL.NewList(mDispLists[t][p.mUnique], ListMode.Compile);

                        GL.PushMatrix();

                        GL.Translate(stage.mPosition);
                        GL.Rotate(stage.mRotation.Z, 0f, 0f, 1f);
                        GL.Rotate(stage.mRotation.Y, 0f, 1f, 0f);
                        GL.Rotate(stage.mRotation.X, 1f, 0f, 0f);
                        p.Render(mode);
                        //Console.WriteLine(p);
                        GL.PopMatrix();

                        GL.EndList();
                        //Console.WriteLine(stage.mName);
                    }                    
                }
                //return;
                // and now we just do the regular stage
                List<AbstractObj> regularObjs = mObjects.FindAll(o => o.mParentZone.mZoneName == mGalaxyName);
                List<PathObj> regularPaths = mPaths.FindAll(p => p.mParentZone.mZoneName == mGalaxyName);

                foreach (AbstractObj o in regularObjs)
                {
                    //LevelObj level = o as LevelObj;
                    //Console.WriteLine("test "+ o.mName);
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[t].ContainsKey(o.mUnique))
                        continue;

                    keyValuePairs.Add(o.mUnique, GL.GenLists(1));
                    mDispLists[t].Add(o.mUnique, GL.GenLists(1));

                    GL.NewList(mDispLists[t][o.mUnique], ListMode.Compile);

                    GL.PushMatrix();
                    //GL.Translate(o.mTruePosition);
                    //GL.Rotate(o.mRotation.Z, 0f, 0f, 1f);
                    //GL.Rotate(o.mRotation.Y, 0f, 1f, 0f);
                    //GL.Rotate(o.mRotation.X, 1f, 0f, 0f);
                    o.Render(mode);
                    GL.PopMatrix();

                    GL.EndList();
                }

                foreach (PathObj path in regularPaths)
                {
                    Dictionary<int, int> keyValuePairs = new Dictionary<int, int>();

                    if (mDispLists[t].ContainsKey(path.mUnique))
                        continue;

                    keyValuePairs.Add(path.mUnique, GL.GenLists(1));
                    mDispLists[t].Add(path.mUnique, GL.GenLists(1));

                    GL.NewList(mDispLists[t][path.mUnique], ListMode.Compile);

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
            Matrix4 projmtx = Matrix4.CreatePerspectiveFieldOfView(k_FOV, m_AspectRatio, k_zNear, k_zFar);
            GL.LoadMatrix(ref projmtx);

            m_PixelFactorX = ((2f * (float)Math.Tan(k_FOV / 2f) * m_AspectRatio) / (float)(glLevelView.Width));
            m_PixelFactorY = ((2f * (float)Math.Tan(k_FOV / 2f)) / (float)(glLevelView.Height));
        }



        private void glLevelView_Paint(object sender, PaintEventArgs e)
        {
            if (!m_GLLoaded) return;
            glLevelView.MakeCurrent();
            //GL.MatrixMode(MatrixMode.Projection);
            //Matrix4 projmtx = (!m_OrthView) ? Matrix4.CreatePerspectiveFieldOfView(m_FOV, m_AspectRatio, k_zNear, m_zFar) :
            //    Matrix4.CreateOrthographic(m_OrthZoom, m_OrthZoom / m_AspectRatio, k_zNear, m_zFar);
            //GL.LoadMatrix(ref projmtx);

            /* step one -- fakecolor rendering */
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref m_CamMatrix);

            GL.Disable(EnableCap.AlphaTest);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Dither);
            GL.Disable(EnableCap.LineSmooth);
            GL.Disable(EnableCap.PolygonSmooth);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Lighting);

            foreach (KeyValuePair<int, Dictionary<int, int>> disp in mDispLists)
            {
                foreach (KeyValuePair<int, int> actualList in disp.Value)
                    GL.CallList(actualList.Value);
            }

            for (int a = 0; a < 3; a++)
            {
                GL.Color4(Color.FromArgb(a));
                GL.CallList(mDisplayLists[a]);
            }

            GL.Flush();
            GL.ReadPixels(m_MouseCoords.X, glLevelView.Height - m_MouseCoords.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref m_PickingModelDepth);
            m_PickingModelDepth = -(m_zFar * k_zNear / (m_PickingModelDepth * (m_zFar - k_zNear) - m_zFar));

            GL.Flush();
            GL.ReadPixels(m_MouseCoords.X - 1, glLevelView.Height - m_MouseCoords.Y + 1, 3, 3, PixelFormat.Bgra, PixelType.UnsignedByte, m_PickingFrameBuffer);

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

            foreach (KeyValuePair<int, Dictionary<int, int>> disp in mDispLists)
            {
                foreach (KeyValuePair<int, int> actualList in disp.Value)
                    GL.CallList(actualList.Value);
            }

            glLevelView.SwapBuffers();
        }

        /// <summary>
        /// カメラ定義関数
        /// </summary>
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



            Vector3.Add(ref m_CamPosition, ref m_CamTarget, out m_CamPosition);

            //ビュー行列生成
            m_CamMatrix = Matrix4.LookAt(m_CamPosition, m_CamTarget, up);

            m_CamMatrix = Matrix4.Mult(Matrix4.Scale(0.0001f), m_CamMatrix);
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
            m_CamMatrix = Matrix4.Mult(Matrix4.Scale(0.0001f), m_CamMatrix);
        }

        private float _rotateSpeed = 0.002f;
        private float _moveSpeed = 0.001f;
        private void glLevelView_MouseMove(object sender, MouseEventArgs e)
        {
            float xdelta = (float)(e.X - m_LastMouseMove.X);
            float ydelta = (float)(e.Y - m_LastMouseMove.Y);


            m_LastMouseMove = e.Location;

            if (m_MouseDown != MouseButtons.None)
            {
                if (m_MouseDown == MouseButtons.Right)
                {
                    if (m_UpsideDown)
                        xdelta = -xdelta;

                    m_CamRotation.X -= xdelta * _rotateSpeed;
                    m_CamRotation.Y -= ydelta * _rotateSpeed;
                }
                else if (m_MouseDown == MouseButtons.Left)
                {
                    xdelta *= _moveSpeed;
                    ydelta *= _moveSpeed;

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

            m_MouseDown = MouseButtons.None;
            m_LastMouseMove = e.Location;
        }

        private void glLevelView_MouseDown(object sender, MouseEventArgs e)
        {
            if (m_MouseDown != MouseButtons.None) return;

            m_MouseDown = e.Button;
            m_LastMouseMove = m_LastMouseClick = e.Location;
        }

        

        private void objectsListTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            AbstractObj abstractObj = e.Node.Tag as AbstractObj;
            
            
            if (abstractObj == null) return;

            //objects Camera Setting
            //The following process moves the camera to the object.
            var ZoneName = abstractObj.mParentZone.mZoneName;
            var Pos_ZoneOffset = mGalaxy.Get_Pos_GlobalOffset(ZoneName);
            var Rot_ZoneOffset = mGalaxy.Get_Rot_GlobalOffset(ZoneName);

            var PosObj = abstractObj.mTruePosition;
            var CorrectPos_Object = calc.RotAfin.GetPositionAfterRotation(PosObj, Rot_ZoneOffset, calc.RotAfin.TargetVector.All);

            m_CamDistance = 0.200f;
            m_CamTarget = Pos_ZoneOffset / 10000f + CorrectPos_Object / 10000;
            m_CamPosition = CorrectPos_Object / 10000;
            m_CamRotation.Y = (float)Math.PI / 8f;
            m_CamRotation.X = (-(abstractObj.mTrueRotation.Y + Pos_ZoneOffset.Y) / 180f) * (float)Math.PI;

            //objects PropertyGrideSetting
            //Display the property grid for setting the currently selected object.
            //Note: These processes are not related to the camera's processing.

            dataGridView1.DataSource = null;
            dataGridViewEdit = null;
            switch (e.Node.Parent.Text)
            {
                case "Objects":
                    if (!(abstractObj is LevelObj))
                        throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(LevelObj) }」 .");
                    LevelObj obj = abstractObj as LevelObj;
                    dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, obj);
                    dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                    break;
                case "Areas":
                    //AreaObj area = abstractObj as AreaObj;
                    //dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, area);
                    //dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                    if (!(abstractObj is AreaObj))
                        throw new Exception($"This 「{ typeof(AbstractObj) }」 is not a 「{ typeof(AreaObj) }」 .");
                    AreaObj areaobj = abstractObj as AreaObj;
                    dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, areaobj);
                    dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                    break;
                case "Map Parts":
                case "MapPartsObj":
                    //MapPartsObj mapparts = abstractObj as MapPartsObj;
                    
                    break;
                default:
                    //dataGridViewEdit = new EditorWindowSys.DataGridViewEdit(dataGridView1, abstractObj);
                    //dataGridView1.DataSource = dataGridViewEdit.GetDataTable();
                    break;
            }

            //Drawing, camera post-processing
            UpdateCamera();
            glLevelView.Refresh();

            //RenderObjectLists(RenderMode.Opaque);



            //RenderZone(mObjects);
            //RenderObjectLists
        }

        private void objectsListTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
        }
        
        private void MoveCameraTo(Vector3 target)
        {
            
        }

        private void propertyGrid1_SelectedObjectsChanged(object sender, EventArgs e)
        {
            //glLevelView_Paint(sender,pa);
            //if (!m_GLLoaded) return;
            //glLevelView.MakeCurrent();

            //UpdateViewport();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Scenario_ReLoad();
        }

        private void toolStripLabel3_Click(object sender, EventArgs e)
        {
            //var a = mGalaxy.GetGalaxyZone().GetObjectsFromLayer("Map",mGalaxy.GetGalaxyZone().GetLayersUsedOnZoneForCurrentScenario()[0]);
            //BCSV.Entry f = new BCSV.Entry();
            //f.Add();
            
            //a.Add(new AbstractObj(BCSV.));
            //mGalaxy.GetGalaxyZone().mObjects["Map"].Add(new LevelObj(Entry, mGalaxy.GetGalaxyZone(), path));
            //mObjects[""][""].Add(new LevelObj(Entry, this, path));
        }

        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            //if (e.RowIndex == -1 && e.ColumnIndex == 0)
            //{
            //    var rec = e.CellBounds;
            //    rec.Width += dataGridView1.Columns[1].Width;
            //    //rec.Height = dataGridView1.Rows[0].Height;
            //    var brush = new SolidBrush(dataGridView1.ColumnHeadersDefaultCellStyle.BackColor);
            //    var pen = new Pen(dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor);
            //    e.Graphics.FillRectangle(brush, rec);
            //    e.Graphics.DrawRectangle(pen,rec);
            //    e.Graphics.DrawLine(pen,new Point(0,rec.Y),new Point(rec.X,0));
            //    //e.Graphics.DrawString("Obj",dataGridView1.Font,new SolidBrush(Color.Black),rec);

            //    //(TextFormatFlags)5 で水平、垂直方向のセンタリングを指定。
            //    TextRenderer.DrawText(e.Graphics, "OBJ", e.CellStyle.Font, rec, e.CellStyle.ForeColor, e.CellStyle.BackColor, (TextFormatFlags)5);
            //    e.Handled = true;
            //}
            //else if (e.RowIndex == 0 && (e.ColumnIndex == 0 || e.ColumnIndex == 1))
            //{
            //    if (e.ColumnIndex == 1) 
            //    {
                    
            //        e.Handled = true;
            //        return;
            //    } 
            //    var rec = e.CellBounds;
            //    rec.Width += dataGridView1.Columns[1].Width;
            //    var brush = new SolidBrush(dataGridView1.ColumnHeadersDefaultCellStyle.BackColor);
            //    var pen = new Pen(dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor);
            //    e.Graphics.FillRectangle(brush, rec);
            //    e.Graphics.DrawRectangle(pen, rec);
            //    e.Graphics.DrawLine(pen, new Point(0, rec.Y), new Point(rec.X, 0));

            //    TextRenderer.DrawText(e.Graphics, "data", e.CellStyle.Font, rec, e.CellStyle.ForeColor, e.CellStyle.BackColor, (TextFormatFlags)5);
            //    e.Handled = true;

            //}
            //else if (e.RowIndex == -1 && e.ColumnIndex == 1)
            //{
            //    e.Handled = true;
            //}
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DataGridView)sender;
            
            var cell_value = dgv[e.ColumnIndex, e.RowIndex].Value;
            dataGridViewEdit.ChangeValue(e.RowIndex,cell_value);
            Scenario_ReLoad();

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

            //UpdateViewport();
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
            float delta = -((e.Delta / 120f) * 0.1f);
            m_CamTarget.X += delta * (float)Math.Cos(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);
            m_CamTarget.Y += delta * (float)Math.Sin(m_CamRotation.Y);
            m_CamTarget.Z += delta * (float)Math.Sin(m_CamRotation.X) * (float)Math.Cos(m_CamRotation.Y);

            UpdateCamera();

            glLevelView.Refresh();
        }

        #endregion
    }
}
