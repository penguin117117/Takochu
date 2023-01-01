using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.fmt;
using Takochu.io;

namespace Takochu.smg.obj.ObjectSubData
{
    public class BMDInfo
    {
        //描画タイプの配列初期化
        private static BeginMode[] beginMode =
        {
            BeginMode.Quads,
            BeginMode.Points,
            BeginMode.Triangles,
            BeginMode.TriangleStrip,
            BeginMode.TriangleFan,
            BeginMode.Lines,
            BeginMode.LineStrip,
            BeginMode.Points
        };

        public class BMDTriangleData
        {
            public struct TrianglesPosition 
            {
                public Vector4 V0;
                public Vector4 V1;
                public Vector4 V2;

                public TrianglesPosition(Vector4[] trianglePositionArray) 
                {
                    V0 = trianglePositionArray[0];
                    V1 = trianglePositionArray[1];
                    V2 = trianglePositionArray[2];
                }
            }

            public List<TrianglesPosition> TriangleDataList { get; private set; }

            public BMDTriangleData() 
            {
                TriangleDataList = new List<TrianglesPosition>();
            }

            public void AddTriangleDataList(TrianglesPosition trianglesPosition) 
            {
                TriangleDataList.Add(trianglesPosition);
            }

        }
        

        public static BMDTriangleData GetTriangles(AbstractObj obj)
        {
            BMDTriangleData bmdTriangleData = new BMDTriangleData();

            RARCFilesystem rarc = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{obj.mName}.arc"));

            if (rarc.DoesFileExist($"/root/{obj.mName}.bdl"))
            {
                BMD bmd = new BMD(rarc.OpenFile($"/root/{obj.mName}.bdl"));

                //Batchファイル内のPacketsを取得
                foreach (BMD.Batch batch in bmd.Batches)
                {
                    Matrix4[] lastmatrixtable = null;

                    foreach (BMD.Batch.Packet packet in batch.Packets)
                    {
                        Matrix4[] mtxtable = new Matrix4[packet.MatrixTable.Length];
                        int[] mtx_debug = new int[packet.MatrixTable.Length];

                        for (int i = 0; i < packet.MatrixTable.Length; i++)
                        {
                            if (packet.MatrixTable[i] == 0xFFFF)
                            {
                                mtxtable[i] = lastmatrixtable[i];
                                mtx_debug[i] = 2;
                            }
                            else
                            {
                                BMD.MatrixType mtxtype = bmd.MatrixTypes[packet.MatrixTable[i]];

                                if (mtxtype.IsWeighted)
                                {
                                    mtxtable[i] = Matrix4.Identity;

                                    mtx_debug[i] = 1;
                                }
                                else
                                {
                                    mtxtable[i] = bmd.Joints[mtxtype.Index].FinalMatrix;
                                    mtx_debug[i] = 0;
                                }
                            }
                        }

                        lastmatrixtable = mtxtable;


                        

                        //ジオメトリ情報にアクセスしていると思われる。
                        foreach (BMD.Batch.Packet.Primitive prim in packet.Primitives)
                        {
                            bmdTriangleData.TriangleDataList.AddRange(GetTriangleFaces(prim,bmd,mtxtable));

                        }
                    }
                }
            }

            return bmdTriangleData;
        }

        private static List<BMDTriangleData.TrianglesPosition> GetTriangleFaces(BMD.Batch.Packet.Primitive prim,BMD bmd,Matrix4[] mtxtable)
        {
            Debug.WriteLine(beginMode[(prim.PrimitiveType - 0x80) / 8]);

            List<BMDTriangleData.TrianglesPosition> trianglesPositionList = new List<BMDTriangleData.TrianglesPosition>();

            switch (beginMode[(prim.PrimitiveType - 0x80) / 8]) 
            {
                case BeginMode.Triangles:
                    for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
                    {
                        //頂点情報

                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 pos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref pos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out pos);
                        else
                            Vector4.Transform(ref pos, ref mtxtable[0], out pos);

                        Debug.WriteLine(pos);
                        Debug.WriteLine("通常△");
                    }
                    break;
                case BeginMode.TriangleStrip:
                    for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
                    {
                        Vector4[] trianglePositionArray = new Vector4[3];

                        //頂点情報
                        if (vertexIndex <= 2) 
                        {
                            //頂点情報
                            

                            for (int i = 0; i < 2; i++) 
                            {
                                //頂点インデックスにあった頂点番号の頂点を順番にセット
                                Vector4 firstpos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                                //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                                if ((prim.ArrayMask & 1) != 0)
                                    Vector4.Transform(ref firstpos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out firstpos);
                                else
                                    Vector4.Transform(ref firstpos, ref mtxtable[0], out firstpos);

                                Debug.WriteLine(firstpos);

                                trianglePositionArray[i] = firstpos;

                                vertexIndex++;
                            }

                            
                        }

                        if (vertexIndex > 2)
                        {
                            Debug.WriteLine("-----------------------");
                            //頂点インデックスにあった頂点番号の頂点を順番にセット
                            Vector4 beforePos2 = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex-2]], 1.0f);

                            //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                            if ((prim.ArrayMask & 1) != 0)
                                Vector4.Transform(ref beforePos2, ref mtxtable[prim.PosMatrixIndices[vertexIndex-2]], out beforePos2);
                            else
                                Vector4.Transform(ref beforePos2, ref mtxtable[0], out beforePos2);

                            Debug.WriteLine(beforePos2);

                            //頂点インデックスにあった頂点番号の頂点を順番にセット
                            Vector4 beforePos1 = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex - 1]], 1.0f);

                            //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                            if ((prim.ArrayMask & 1) != 0)
                                Vector4.Transform(ref beforePos1, ref mtxtable[prim.PosMatrixIndices[vertexIndex - 1]], out beforePos1);
                            else
                                Vector4.Transform(ref beforePos1, ref mtxtable[0], out beforePos1);

                            Debug.WriteLine(beforePos1);

                        }

                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 pos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref pos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out pos);
                        else
                            Vector4.Transform(ref pos, ref mtxtable[0], out pos);


                        trianglePositionArray[2] = pos;

                        Debug.WriteLine(pos);

                        trianglesPositionList.Add(new BMDTriangleData.TrianglesPosition(trianglePositionArray));
                    }


                    break;
                case BeginMode.TriangleFan:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");
                default:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");

                    
            }

            

            return trianglesPositionList;
            //for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
            //{
            //    //頂点情報

            //    //頂点インデックスにあった頂点番号の頂点を順番にセット
            //    Vector4 pos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

            //    //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
            //    if ((prim.ArrayMask & 1) != 0)
            //        Vector4.Transform(ref pos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out pos);
            //    else
            //        Vector4.Transform(ref pos, ref mtxtable[0], out pos);

            //    Debug.WriteLine(pos);
            //}
        }
    }
}
