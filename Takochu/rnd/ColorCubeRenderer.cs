using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
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

        // 調整用のスケール値
        private const float m_RenderScale = 100.0f;

        // 整の値から右回転。
        private static readonly List<Vector4> m_RenderVertexs = new List<Vector4> {
            new Vector4(1.0f *m_RenderScale, 1.0f*m_RenderScale, 1.0f*m_RenderScale,1.0f),
            new Vector4(1.0f * m_RenderScale, 1.0f*m_RenderScale, -1.0f*m_RenderScale,1.0f),
            new Vector4(-1.0f*m_RenderScale, 1.0f*m_RenderScale, -1.0f*m_RenderScale,1.0f),
            new Vector4(-1.0f*m_RenderScale, 1.0f*m_RenderScale, 1.0f*m_RenderScale, 1.0f),
            new Vector4(1.0f*m_RenderScale, -1.0f*m_RenderScale, 1.0f*m_RenderScale, 1.0f),
            new Vector4(1.0f*m_RenderScale, -1.0f*m_RenderScale, -1.0f*m_RenderScale,1.0f),
            new Vector4(-1.0f*m_RenderScale, -1.0f*m_RenderScale, -1.0f*m_RenderScale, 1.0f),
            new Vector4(-1.0f*m_RenderScale, -1.0f*m_RenderScale, 1.0f*m_RenderScale, 1.0f)
        };
        // 上下左右前後
        private static readonly BMDInfo.BMDTriangleData m_RenderTriangleFace = new BMDInfo.BMDTriangleData(
            new List<BMDInfo.BMDTriangleData.TrianglesPosition> { 
            // 0123
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[0], m_RenderVertexs[1], m_RenderVertexs[2] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[2], m_RenderVertexs[3], m_RenderVertexs[0] }),
            // 7654                                                                                                                    
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[7], m_RenderVertexs[6], m_RenderVertexs[5] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[5], m_RenderVertexs[4], m_RenderVertexs[7] }),
            // 0374                                                                                                                    
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[0], m_RenderVertexs[3], m_RenderVertexs[7] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[7], m_RenderVertexs[4], m_RenderVertexs[0] }),
            // 1562                                                                                                                    
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[1], m_RenderVertexs[5], m_RenderVertexs[6] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[6], m_RenderVertexs[2], m_RenderVertexs[1] }),
            // 0415                                                                                                                    
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[0], m_RenderVertexs[4], m_RenderVertexs[1] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[1], m_RenderVertexs[5], m_RenderVertexs[0] }),
            // 2673                                                                                                                    
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[2], m_RenderVertexs[6], m_RenderVertexs[7] }),
            new BMDInfo.BMDTriangleData.TrianglesPosition(new Vector4[3] { m_RenderVertexs[7], m_RenderVertexs[3], m_RenderVertexs[2] })
            }
        );

        public override BMDInfo.BMDTriangleData GetTriangles()
        {
            if (m_ShowAxes)
            {
                return m_RenderTriangleFace;
            }
            return new BMDInfo.BMDTriangleData();
        }

        private float m_Size;
        private Vector4 m_BorderColor, m_FillColor;
        private bool m_ShowAxes;
    }
}