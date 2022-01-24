﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.fmt;
using Takochu.rnd;
using OpenTK.Graphics;

namespace Takochu.smg.obj
{
    public class AbstractObj
    {
        public readonly Dictionary<string, List<string>> cMultiRenderObjs = new Dictionary<string, List<string>>()
        {
            { "RedBlueTurnBlock", new List<string>() { "RedBlueTurnBlock", "RedBlueTurnBlockBase" } }
        };

        public AbstractObj(BCSV.Entry entry)
        {
            mEntry = entry;
            if (entry.ContainsKey("name"))
                mName = Get<string>("name");
            mUnique = Program.sUniqueID++;
        }
        
        public virtual void Save() { }

        public List<AbstractObj> GetObjsWithSameField(string type, int value)
        {
            List<AbstractObj> ret = new List<AbstractObj>();

            List<string> layers = mParentZone.GetLayersUsedOnZoneForCurrentScenario();
            List<AbstractObj> objs = mParentZone.GetObjectsFromLayers("Map", "Obj", layers);
            objs.AddRange(mParentZone.GetObjectsFromLayers("Map", "AreaObj", layers));

            foreach (AbstractObj o in objs)
            {
                if (!o.mEntry.ContainsKey(type))
                    continue;

                if (o.Get<int>(type) == value)
                {
                    ret.Add(o);
                }
            }

            return ret;
        }

        public virtual void Render(RenderMode mode)
        {

        }

        public virtual void Reload_mValues()
        {
            
        }

        public override string ToString()
        {
            return "AbstractObj";
        }

        public T Get<T>(string key)
        {
            return mEntry.Get<T>(key);
        }

        
        public BCSV.Entry mEntry { get; protected set; }
        public Zone mParentZone { get; protected set; }


        public Vector3 mTruePosition { get; protected set; }
        public Vector3 mTrueRotation { get; protected set; }

        public Vector3 mPosition { get; protected set; }
        public Vector3 mRotation { get; protected set; }
        public Vector3 mScale { get; protected set; }

        public string mDirectory { get; protected set; }
        public string mLayer { get; protected set; }
        public string mFile { get; protected set; }

        public string mName { get; protected set; }
        public string mType { get; protected set; }

        public int mUnique { get; protected set; }
        public RendererBase mRenderer { get; protected set; }
        public RendererBase mRenderer2 { get; protected set; }

        public int[] mObjArgs { get; protected set; }


    }
}
