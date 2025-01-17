﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.fmt;
using OpenTK;
using System.Windows.Forms;
using System.Security.Policy;
using System.Security.Cryptography;
using Takochu.ui;
using static Takochu.smg.ObjectDB;
using Takochu.util;
using Takochu.io;
using OpenTK.Graphics.OpenGL;
using Takochu.rnd;
using System.Drawing;
using Takochu.calc;
using System.Diagnostics;

namespace Takochu.smg.obj
{
    public class LevelObj : AbstractObj
    {
        //Note:
        //I am considering whether to use this process to handle type specification by "ObjectDB" in "objarg".
        //Please do not use it as is, as I plan to set up a separate class for its actual use.
        //public dynamic dytest
        //{
        //    get
        //    {
        //        //Type t = Type.GetType("System.Int32");
        //        //t.GetField("MaxValue");
        //        return Convert.ChangeType(1, TypeCode.Int32);
        //    }
        //}


        //private ISMG2_SwitchID _smg2_SwitchID;
        //This dictionary type will be used temporarily until
        //the implementation of the object database is completed.
        // TODO: オブジェクト(BMDInfo)でarcロードが失敗する問題を解決するために公開します。
        // 極力このファイル以外での参照はするべきではありません。
        public static readonly Dictionary<string, (string,string)> SP_ObjectName = new Dictionary<string, (string,string)>() 
        {
            { "BenefitItemOneUp" , ("KinokoOneUp","None") },
            { "PlantA" , ("PlantA00","None") },
            { "PlantB" , ("PlantB00","None") },
            { "PlantC" , ("PlantC00","None") },
            { "PlantD" , ("PlantD01","None") },
            { "SplashPieceBlock" , ("CoinBlock","None") },
            { "GreenStar" , ("PowerStar","None") }
        };

        public LevelObj(string objectName,Zone parentZone,string path):base(objectName,parentZone) 
        {
            //string[] content = path.Split('/');
            //mDirectory = content[0];
            //mLayer = content[1];
            //mFile = content[2];

            Set_DirectoryAndLayerAndFileString(path);

            mEntry.Add("name", mName = objectName);
            mEntry.Add("l_id", mID = 0);

            mObjArgs = new int[8];

            for (int i = 0; i < 8; i++)
                mEntry.Add($"Obj_arg{i}", mObjArgs[i] = -1);

            mType = "Obj";

            mEntry.Add("CameraSetId", mCameraSetID = -1);
            mEntry.Add("SW_APPEAR", mSwitchAppear = -1);
            mEntry.Add("SW_DEAD", mSwitchDead = -1);
            mEntry.Add("SW_A", mSwitchActivate = -1);
            mEntry.Add("SW_B", mSwitchDeactivate = -1);
            if (GameUtil.IsSMG2())
            {
                mEntry.Add("SW_AWAKE", mSwitchAwake = -1);
                mEntry.Add("SW_PARAM", mSwitchParameter = -1);
                mEntry.Add("ParamScale", mParamScale = 1);
                mEntry.Add("Obj_ID", mObjID = -1);
                mEntry.Add("GeneratorID", mGeneratorID = -1);
            }
            mEntry.Add("MessageId", mMessageID = -1);

            mTruePosition = Vector3.Zero;
            mEntry.Add("pos_x", mTruePosition.X);
            mEntry.Add("pos_y", mTruePosition.Y);
            mEntry.Add("pos_z", mTruePosition.Z);

            mTrueRotation = Vector3.Zero;
            mEntry.Add("dir_x", mTrueRotation.X);
            mEntry.Add("dir_y", mTrueRotation.Y);
            mEntry.Add("dir_z", mTrueRotation.Z);

            mScale = Vector3.One;
            mEntry.Add("scale_x", mScale.X);
            mEntry.Add("scale_y", mScale.Y);
            mEntry.Add("scale_z", mScale.Z);

            mEntry.Add("CastId", mCastID = -1);
            mEntry.Add("ViewGroupId", mViewGroupID = -1);
            mEntry.Add("ShapeModelNo", mShapeModelNo = -1);
            mEntry.Add("CommonPath_ID", mPathID = -1);
            mEntry.Add("ClippingGroupId", mClippingGroupID = -1);
            mEntry.Add("GroupId", mGroupID = -1);
            mEntry.Add("DemoGroupId", mDemoGroupID = -1);
            mEntry.Add("MapParts_ID", mMapPartsID = -1);

            BMD_Rendering();
        }

        public LevelObj(BCSV.Entry entry, Zone parentZone, string path) : base(entry)
        {
            
            mParentZone = parentZone;
            Set_DirectoryAndLayerAndFileString(path);

            //string[] content = path.Split('/');
            //mDirectory = content[0];
            //mLayer = content[1];
            //mFile = content[2];

            mType = "Obj";

            
            mTruePosition = new Vector3(Get<float>("pos_x"), Get<float>("pos_y"), Get<float>("pos_z"));
            mTrueRotation = new Vector3(Get<float>("dir_x"), Get<float>("dir_y"), Get<float>("dir_z"));

            mPosition = new Vector3(Get<float>("pos_x") / 100, Get<float>("pos_y") / 100, Get<float>("pos_z") / 100);
            mRotation = new Vector3(Get<float>("dir_x"), Get<float>("dir_y"), Get<float>("dir_z"));
            mScale = new Vector3(Get<float>("scale_x"), Get<float>("scale_y"), Get<float>("scale_z"));
            mID = Get<int>("l_id");
            mObjArgs = new int[8];

            for (int i = 0; i < 8; i++)
                mObjArgs[i] = Get<int>($"Obj_arg{i}");

            mCameraSetID = Get<int>("CameraSetId");
            mSwitchAppear = Get<int>("SW_APPEAR");
            mSwitchDead = Get<int>("SW_DEAD");
            mSwitchActivate = Get<int>("SW_A");
            mSwitchDeactivate = Get<int>("SW_B");
           
            mMessageID = Get<int>("MessageId");

            if (GameUtil.IsSMG2())
            {
                mSwitchAwake = Get<int>("SW_AWAKE");
                mSwitchParameter = Get<int>("SW_PARAM");
                mParamScale = Get<float>("ParamScale");
                mObjID = Get<short>("Obj_ID");
                mGeneratorID = Get<short>("GeneratorID");
            }

            mCastID = Get<int>("CastId");
            mViewGroupID = Get<int>("ViewGroupId");
            mShapeModelNo = Get<short>("ShapeModelNo");
            mPathID = Get<short>("CommonPath_ID");
            mClippingGroupID = Get<short>("ClippingGroupId");
            mGroupID = Get<short>("GroupId");
            mDemoGroupID = Get<short>("DemoGroupId");
            mMapPartsID = Get<short>("MapParts_ID");

            BMD_Rendering();
        }

        private void Set_DirectoryAndLayerAndFileString(string path) 
        {
            string[] content = path.Split('/');
            mDirectory = content[0];
            mLayer = content[1];
            mFile = content[2];
        }

        private void BMD_Rendering() 
        {
            /* 
                 * Rendering the proper BMD files can be a little complicated, so let's break this down
                 * If the object has multiple pieces to render, we create the renderer in the first statement
                 * if the model cache already has our model, we take it from there and store it
                 * if the model cache does not have our model, and the file exists, we load the model and store it into our model cache
                 * if an object has a different archive name than the object name, we load that object name instead
                 * if all else fails, we just load a color cube
                 */
            if (cMultiRenderObjs.ContainsKey(mName))
            {
                mRenderer = new MultiBmdRenderer(cMultiRenderObjs[mName]);
            }
            else if (ModelCache.HasRenderer(mName))
            {
                mRenderer = ModelCache.GetRenderer(mName);
            }
            else if (mRenderer == null && Program.sGame.DoesFileExist($"/ObjectData/{mName}.arc"))
            {
                Debug.WriteLine("Read OBJName: " + mName);
                RARCFilesystem rarc = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{mName}.arc"));

                if (rarc.DoesFileExist($"/root/{mName}.bdl"))
                {
                    mRenderer = new BmdRenderer(new BMD(rarc.OpenFile($"/root/{mName}.bdl")));
                    ModelCache.AddRenderer(mName, (BmdRenderer)mRenderer);
                }
                else
                {
                    mRenderer = new ColorCubeRenderer(200f, new Vector4(1f, 1f, 1f, 1f), new Vector4(1f, 0f, 1f, 1f), true);
                }

                if (rarc.DoesFileExist("/root/ColorChange.brk"))
                {
                    //BRK brk = new BRK(rarc.OpenFile("/root/ColorChange.brk"));
                }

                rarc.Close();
            }
            else if (SP_ObjectName.ContainsKey(mName))
            {
                var tmpname = SP_ObjectName[mName];
                RARCFilesystem rarc = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{tmpname.Item1}.arc"));

                if (rarc.DoesFileExist($"/root/{tmpname.Item1}.bdl"))
                {
                    mRenderer = new BmdRenderer(new BMD(rarc.OpenFile($"/root/{tmpname.Item1}.bdl")));
                    ModelCache.AddRenderer(tmpname.Item1, (BmdRenderer)mRenderer);
                }
                else
                {
                    mRenderer = new ColorCubeRenderer(200f, new Vector4(1f, 1f, 1f, 1f), new Vector4(1f, 0f, 1f, 1f), true);
                }

                rarc.Close();

                if (tmpname.Item2 == "None") return;

                RARCFilesystem rarc1 = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{tmpname.Item2}.arc"));

                if (rarc1.DoesFileExist($"/root/{tmpname.Item2}.bdl"))
                {
                    mRenderer2 = new BmdRenderer(new BMD(rarc1.OpenFile($"/root/{tmpname.Item2}.bdl")));
                    ModelCache.AddRenderer(tmpname.Item2, (BmdRenderer)mRenderer2);
                    
                }
                else
                {
                    mRenderer2 = new ColorCubeRenderer(200f, new Vector4(1f, 1f, 1f, 1f), new Vector4(1f, 0f, 1f, 1f), true);
                }

                rarc1.Close();
            }
            else
            {
                mRenderer = new ColorCubeRenderer(150f, new Vector4(1f, 1f, 1f, 1f), new Vector4(1f, 0f, 1f, 1f), true);
            }
        }

        public override void Reload_mValues()
        {
            //string values
            //Currently, it is not linked to ObjectDB, so it cannot be changed temporarily.
            {
                mName = mEntry.Get("name").ToString();
            }
            

            //Int32 ID
            {
                mID = ObjectTypeChange.ToInt32(mEntry.Get("l_id"));
                mCameraSetID = ObjectTypeChange.ToInt32(mEntry.Get("CameraSetId"));
                mMessageID = ObjectTypeChange.ToInt32(mEntry.Get("MessageId"));
                mCastID = ObjectTypeChange.ToInt32(mEntry.Get("CastId"));
                mViewGroupID = ObjectTypeChange.ToInt32(mEntry.Get("ViewGroupId"));
            }

            //Int16 param
            {
                mShapeModelNo = ObjectTypeChange.ToInt16(mEntry.Get("ShapeModelNo"));
                mPathID = ObjectTypeChange.ToInt16(mEntry.Get("CommonPath_ID"));
                mClippingGroupID = ObjectTypeChange.ToInt16(mEntry.Get("ClippingGroupId"));
                mGroupID = ObjectTypeChange.ToInt16(mEntry.Get("GroupId"));
                mDemoGroupID = ObjectTypeChange.ToInt16(mEntry.Get("DemoGroupId"));
                mMapPartsID = ObjectTypeChange.ToInt16(mEntry.Get("MapParts_ID"));
                if (GameUtil.IsSMG2()) 
                {
                    mObjID = ObjectTypeChange.ToInt16(mEntry.Get("Obj_ID"));
                    mGeneratorID = ObjectTypeChange.ToInt16(mEntry.Get("GeneratorID"));
                }
            }

            //Int32 Switch
            { 
                mSwitchAppear = ObjectTypeChange.ToInt32(mEntry.Get("SW_APPEAR"));
                mSwitchDead = ObjectTypeChange.ToInt32(mEntry.Get("SW_DEAD"));
                mSwitchActivate = ObjectTypeChange.ToInt32(mEntry.Get("SW_A"));
                mSwitchDeactivate = ObjectTypeChange.ToInt32(mEntry.Get("SW_B"));
                if (GameUtil.IsSMG2()) 
                {
                    mSwitchAwake = ObjectTypeChange.ToInt32(mEntry.Get("SW_AWAKE"));
                    mSwitchParameter = ObjectTypeChange.ToInt32(mEntry.Get("SW_PARAM"));
                }
                
            }

            //float
            if (GameUtil.IsSMG2())
                mParamScale = ObjectTypeChange.ToFloat(mEntry.Get("ParamScale"));

            //Obj_args
            for (int i = 0; i < mObjArgs.Length; i++) 
                mObjArgs[i] = ObjectTypeChange.ToInt32(mEntry.Get($"Obj_arg{i}")) ;

            //Vector3Values
            {
                mTruePosition = GetVector3_FromEntry("pos_x", "pos_y", "pos_z");
                mTrueRotation = GetVector3_FromEntry("dir_x", "dir_y", "dir_z");
                mScale        = GetVector3_FromEntry("scale_x", "scale_y", "scale_z");
                
                mPosition = new Vector3(mTruePosition) / 100;
                mRotation = new Vector3(mTrueRotation) / 100;
            }
            
            //Console.WriteLine(dytest);
        }

        public override void Render(RenderMode mode)
        {
            RenderInfo inf = new RenderInfo
            {
                Mode = mode
            };

            if (!mRenderer.GottaRender(inf))
                return;
            
            GL.PushMatrix();
            {
                GL.Translate(mTruePosition);
                //"RotateZYX"の順番を変えない事
                //Do not change the order of "RotateZYX"
                GL.Rotate(mTrueRotation.Z, 0f, 0f, 1f);
                GL.Rotate(mTrueRotation.Y, 0f, 1f, 0f);
                GL.Rotate(mTrueRotation.X, 1f, 0f, 0f);
                GL.Scale(mScale.X, mScale.Y, mScale.Z);
            }

            mRenderer.Render(inf);
            
            GL.PopMatrix();
        }
        
        
        public override void Save()
        {
            mEntry.Set("name", mName);
            mEntry.Set("l_id", mID);

            for (int i = 0; i < 8; i++)
                mEntry.Set($"Obj_arg{i}", mObjArgs[i]);

            mEntry.Set("CameraSetId", mCameraSetID);
            mEntry.Set("SW_APPEAR", mSwitchAppear);
            mEntry.Set("SW_DEAD", mSwitchDead);
            mEntry.Set("SW_A", mSwitchActivate);
            mEntry.Set("SW_B", mSwitchDeactivate);
            if (GameUtil.IsSMG2())
            {
                mEntry.Set("SW_AWAKE", mSwitchAwake);
                mEntry.Set("SW_PARAM", mSwitchParameter);
                mEntry.Set("ParamScale", mParamScale);
                mEntry.Set("Obj_ID", mObjID);
                mEntry.Set("GeneratorID", mGeneratorID);
            }
            mEntry.Set("MessageId", mMessageID);

            mEntry.Set("pos_x", mTruePosition.X);
            mEntry.Set("pos_y", mTruePosition.Y);
            mEntry.Set("pos_z", mTruePosition.Z);

            mEntry.Set("dir_x", mTrueRotation.X);
            mEntry.Set("dir_y", mTrueRotation.Y);
            mEntry.Set("dir_z", mTrueRotation.Z);

            mEntry.Set("scale_x", mScale.X);
            mEntry.Set("scale_y", mScale.Y);
            mEntry.Set("scale_z", mScale.Z);

            mEntry.Set("CastId", mCastID);
            mEntry.Set("ViewGroupId", mViewGroupID);
            mEntry.Set("ShapeModelNo", mShapeModelNo);
            mEntry.Set("CommonPath_ID", mPathID);
            mEntry.Set("ClippingGroupId", mClippingGroupID);
            mEntry.Set("GroupId", mGroupID);
            mEntry.Set("DemoGroupId", mDemoGroupID);
            mEntry.Set("MapParts_ID", mMapPartsID);
            
            
        }

        private int mID;
        private int mCameraSetID;
        private int mSwitchAppear;
        private int mSwitchDead;
        private int mSwitchActivate;
        private int mSwitchDeactivate;
        private int mSwitchAwake;
        private int mSwitchParameter;
        private int mMessageID;
        private float mParamScale;
        private int mCastID;
        private int mViewGroupID;
        private short mShapeModelNo;
        public short mPathID;
        private short mClippingGroupID;
        private short mGroupID;
        private short mDemoGroupID;
        private short mMapPartsID;
        private short mObjID;
        private short mGeneratorID;

        public override string ToString()
        {
            return $"[{Get<int>("l_id")}] {ObjectDB.GetFriendlyObjNameFromObj(mName)} [{mLayer}]";
        }
    }
}
