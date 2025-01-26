using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Takochu.smg.obj.ObjectSubData;

namespace Takochu.rnd
{
    public enum RenderMode : int
    {
        Opaque = 0,
        Translucent,
        Picking
        
    }

    public class RenderInfo
    {
        // rendering mode -- those that saw SM64DSe's renderer will find out
        // that Whitehole's renderer works the same way
        public RenderMode Mode;

        // those display lists are to be called before rendering billboard parts
        // they will be recompiled every time the camera is moved
        // PS: probably not the proper way
        // who cares about billboards anyway
        //public int BillboardDL;
        //public int YBillboardDL;

        public static RenderMode[] Modes = { RenderMode.Picking, RenderMode.Opaque, RenderMode.Translucent };
    }

    public class RendererBase
    {
        public virtual void Close()
        {
        }

        public virtual bool GottaRender(RenderInfo info)
        {
            return false;
        }

        public virtual void Render(RenderInfo info)
        {
        }

        /// <summary>
        /// 三角面情報の計算による取得
        /// </summary>
        /// <returns></returns>
        public virtual BMDInfo.BMDTriangleData GetTriangles()
        {
            return new BMDInfo.BMDTriangleData();
        }

    }
}