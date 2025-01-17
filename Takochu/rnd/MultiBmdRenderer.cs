﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.fmt;
using Takochu.io;
using Takochu.smg.obj.ObjectSubData;

namespace Takochu.rnd
{
    public class MultiBmdRenderer : RendererBase
    {
        public MultiBmdRenderer()
        {
            mRenderers = new Dictionary<string, BmdRenderer>();
        }

        public MultiBmdRenderer(List<string> names)
        {
            mRenderers = new Dictionary<string, BmdRenderer>();
            AddRenderers(names);
        }

        public void AddRenderer(string name, BmdRenderer rnd)
        {
            if (!mRenderers.ContainsKey(name))
            {
                // let's see if we have it in our model cache
                if (ModelCache.HasRenderer(name))
                {
                    mRenderers.Add(name, ModelCache.GetRenderer(name));
                }
                else
                {
                    // add it to our model cache
                    ModelCache.AddRenderer(name, rnd);
                    mRenderers.Add(name, rnd);
                }
            }
        }

        public void AddRenderers(List<string> names)
        {
            foreach(string name in names)
            {
                if (ModelCache.HasRenderer(name))
                {
                    mRenderers.Add(name, ModelCache.GetRenderer(name));
                }
                else
                {
                    if (Program.sGame.DoesFileExist($"/ObjectData/{name}.arc"))
                    {
                        RARCFilesystem rarc = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{name}.arc"));

                        if (rarc.DoesFileExist($"/root/{name}.bdl"))
                        {
                            BmdRenderer rnd = new BmdRenderer(new BMD(rarc.OpenFile($"/root/{name}.bdl")));
                            ModelCache.AddRenderer(name, rnd);
                            mRenderers.Add(name, rnd);
                        }

                        rarc.Close();
                    }
                }
            }
        }

        public override void Render(RenderInfo info)
        {
            foreach(var renderer in mRenderers)
            {
                renderer.Value.Render(info);
            }
        }

        public override bool GottaRender(RenderInfo info)
        {
            return true;
        }

        public override BMDInfo.BMDTriangleData GetTriangles()
        {
            BMDInfo.BMDTriangleData triangleData = new BMDInfo.BMDTriangleData();
            foreach (var renderer in mRenderers) {
                triangleData.AddRangeTriangleDataList(renderer.Value.GetTriangles());
            }
            return triangleData;
        }

        Dictionary<string, BmdRenderer> mRenderers;
    }
}
