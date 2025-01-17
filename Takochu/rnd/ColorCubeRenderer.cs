using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Takochu.rnd;
using Takochu.smg.obj.ObjectSubData;

namespace Takochu
{
    public class ColorCubeRenderer : RendererBase
    {
        public ColorCubeRenderer(float size, Vector4 border, Vector4 fill, bool axes)
        {
            m_Size = size;
            m_BorderColor = border;
            m_FillColor = fill;
            m_ShowAxes = axes;
        }

        public override bool GottaRender(RenderInfo info)
        {
            return info.Mode != RenderMode.Translucent;
        }

        public override void Render(RenderInfo info)
        {
            if (info.Mode == RenderMode.Translucent) return;

            float s = m_Size / 2f;

            if (info.Mode != RenderMode.Picking)
            {
                for (int i = 0; i < 8; i++)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + i);
                    GL.Disable(EnableCap.Texture2D);
                }

                GL.DepthFunc(DepthFunction.Lequal);
                GL.DepthMask(true);
                GL.Color4(m_FillColor);
                GL.Disable(EnableCap.Lighting);
                GL.Disable(EnableCap.Blend);
                GL.Disable(EnableCap.ColorLogicOp);
                GL.Disable(EnableCap.AlphaTest);
                GL.CullFace(CullFaceMode.Front);
                try { GL.UseProgram(0); } catch { }
            }

            GL.Begin(BeginMode.TriangleStrip);
            // 右
            GL.Vertex3(-s, -s, -s);
            GL.Vertex3(-s, s, -s);
            GL.Vertex3(s, -s, -s);
            GL.Vertex3(s, s, -s);
            // 手前
            GL.Vertex3(s, -s, s);
            GL.Vertex3(s, s, s);
            // 左
            GL.Vertex3(-s, -s, s);
            GL.Vertex3(-s, s, s);
            // 奥
            GL.Vertex3(-s, -s, -s);
            GL.Vertex3(-s, s, -s);
            GL.End();

            GL.Begin(BeginMode.TriangleStrip);
            // 上
            GL.Vertex3(-s, s, -s);
            GL.Vertex3(-s, s, s);
            GL.Vertex3(s, s, -s);
            GL.Vertex3(s, s, s);
            GL.End();

            GL.Begin(BeginMode.TriangleStrip);
            // 下
            GL.Vertex3(-s, -s, -s);
            GL.Vertex3(s, -s, -s);
            GL.Vertex3(-s, -s, s);
            GL.Vertex3(s, -s, s);
            GL.End();

            if (info.Mode != RenderMode.Picking)
            {
                GL.LineWidth(1.5f);
                GL.Color4(m_BorderColor);

                GL.Begin(BeginMode.LineStrip);
                GL.Vertex3(s, s, s);
                GL.Vertex3(-s, s, s);
                GL.Vertex3(-s, s, -s);
                GL.Vertex3(s, s, -s);
                GL.Vertex3(s, s, s);
                GL.Vertex3(s, -s, s);
                GL.Vertex3(-s, -s, s);
                GL.Vertex3(-s, -s, -s);
                GL.Vertex3(s, -s, -s);
                GL.Vertex3(s, -s, s);
                GL.End();

                GL.Begin(BeginMode.Lines);
                GL.Vertex3(-s, s, s);
                GL.Vertex3(-s, -s, s);
                GL.Vertex3(-s, s, -s);
                GL.Vertex3(-s, -s, -s);
                GL.Vertex3(s, s, -s);
                GL.Vertex3(s, -s, -s);
                GL.End();

                if (m_ShowAxes)
                {
                    GL.Begin(BeginMode.Lines);
                    GL.Color3(1.0f, 0.0f, 0.0f);
                    GL.Vertex3(0.0f, 0.0f, 0.0f);
                    GL.Color3(1.0f, 0.0f, 0.0f);
                    GL.Vertex3(s * 2.0f, 0.0f, 0.0f);
                    GL.Color3(0.0f, 1.0f, 0.0f);
                    GL.Vertex3(0.0f, 0.0f, 0.0f);
                    GL.Color3(0.0f, 1.0f, 0.0f);
                    GL.Vertex3(0.0f, s * 2.0f, 0.0f);
                    GL.Color3(0.0f, 0.0f, 1.0f);
                    GL.Vertex3(0.0f, 0.0f, 0.0f);
                    GL.Color3(0.0f, 0.0f, 1.0f);
                    GL.Vertex3(0.0f, 0.0f, s * 2.0f);
                    GL.End();
                }
            }
        }
        // 整の値から右回転。
        private static readonly List<Vector3> m_RenderVertexs = new List<Vector3> {
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, 1.0f, 1.0f),
            new Vector3(1.0f, -1.0f, 1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, 1.0f)
        };
        // 上下左右前後
        private static readonly List<TriangleFace> m_RenderTriangleFace = new List<TriangleFace> {
            // 0123
            new TriangleFace(0, 1, 2),
            new TriangleFace(2, 3, 0),
            // 7654
            new TriangleFace(7, 6, 5),
            new TriangleFace(5, 4, 7),
            // 0374
            new TriangleFace(0, 3, 7),
            new TriangleFace(7, 4, 0),
            // 1562
            new TriangleFace(1, 5, 6),
            new TriangleFace(6, 2, 1),
            // 0415
            new TriangleFace(0, 4, 1),
            new TriangleFace(1, 5, 0),
            // 2673
            new TriangleFace(2, 6, 7),
            new TriangleFace(7, 3, 2)
        };

        public override BMDInfo.BMDTriangleData GetTriangles()
        {
            BMDInfo.BMDTriangleData triangleData = new BMDInfo.BMDTriangleData();
            foreach (var triangleFace in m_RenderTriangleFace)
            {
                var trianglePosition = new BMDInfo.BMDTriangleData.TrianglesPosition(
                    new Vector4[3] { 
                        new Vector4(m_RenderVertexs[triangleFace.v1]),
                        new Vector4(m_RenderVertexs[triangleFace.v1]),
                        new Vector4(m_RenderVertexs[triangleFace.v1])});
                triangleData.TriangleDataList.Add(trianglePosition);
            }
            return triangleData;
        }

        private float m_Size;
        private Vector4 m_BorderColor, m_FillColor;
        private bool m_ShowAxes;
    }
}