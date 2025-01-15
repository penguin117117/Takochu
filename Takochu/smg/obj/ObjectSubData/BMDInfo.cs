using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Takochu.fmt;
using Takochu.io;
using Takochu.rnd;

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
            public void AddRangeTriangleDataList(BMDTriangleData data)
            {
                TriangleDataList.AddRange(data.TriangleDataList);
            }
        }

        public static AbstractObj TargetObject { get; private set; }
        private static Vector3 positionWithZoneRotation { get; set; }
        private static Vector3 objTruePos { get; set; }

        //public static BMDTriangleData GetTriangles(AbstractObj obj)
        //{
        //    TargetObject = obj;

        //    BMDTriangleData bmdTriangleData = new BMDTriangleData();
        //    string objName = obj.mName;
        //    if (LevelObj.SP_ObjectName.ContainsKey(objName))
        //    {
        //        objName = LevelObj.SP_ObjectName[objName].Item1;
        //    }
        //    using (RARCFilesystem rarc = new RARCFilesystem(Program.sGame.Filesystem.OpenFile($"/ObjectData/{objName}.arc")))
        //    {
        //        if (rarc.DoesFileExist($"/root/{objName}.bdl"))
        //        {
        //            BMD bmd = new BMD(rarc.OpenFile($"/root/{objName}.bdl"));

        //            objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
        //            var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
        //            positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mPosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

        //            //Batchファイル内のPacketsを取得
        //            foreach (BMD.Batch batch in bmd.Batches)
        //            {
        //                Matrix4[] lastmatrixtable = null;

        //                foreach (BMD.Batch.Packet packet in batch.Packets)
        //                {
        //                    Matrix4[] mtxtable = new Matrix4[packet.MatrixTable.Length];
        //                    int[] mtx_debug = new int[packet.MatrixTable.Length];

        //                    for (int i = 0; i < packet.MatrixTable.Length; i++)
        //                    {
        //                        if (packet.MatrixTable[i] == 0xFFFF)
        //                        {
        //                            mtxtable[i] = lastmatrixtable[i];
        //                            mtx_debug[i] = 2;
        //                        }
        //                        else
        //                        {
        //                            BMD.MatrixType mtxtype = bmd.MatrixTypes[packet.MatrixTable[i]];

        //                            if (mtxtype.IsWeighted)
        //                            {
        //                                mtxtable[i] = Matrix4.Identity;

        //                                mtx_debug[i] = 1;
        //                            }
        //                            else
        //                            {
        //                                mtxtable[i] = bmd.Joints[mtxtype.Index].FinalMatrix;
        //                                mtx_debug[i] = 0;
        //                            }
        //                        }
        //                    }

        //                    lastmatrixtable = mtxtable;




        //                    //ジオメトリ情報にアクセスしていると思われる。
        //                    foreach (BMD.Batch.Packet.Primitive prim in packet.Primitives)
        //                    {
        //                        bmdTriangleData.TriangleDataList.AddRange(GetTriangleFaces(prim, bmd, mtxtable));

        //                    }
        //                }
        //            }
        //        }
        //        // DebugTriangleRendering(bmdTriangleData);
        //        return bmdTriangleData;
        //    };


        //}
        [Obsolete]
        public static BMDTriangleData GetTriangles(AbstractObj obj)
        {
            BMDTriangleData bmdTriangleData = new BMDTriangleData();

            // モデル(BMD)でなければcontinue
            if (obj.mRenderer is BmdRenderer)
            {
                BmdRenderer bmdRenderer = (BmdRenderer)obj.mRenderer;
                BMD bmd = bmdRenderer.getModel();

                objTruePos = obj.mParentZone.mGalaxy.Get_Pos_GlobalOffset(obj.mParentZone.ZoneName);
                var objTrueRot = obj.mParentZone.mGalaxy.Get_Rot_GlobalOffset(obj.mParentZone.ZoneName);
                positionWithZoneRotation = calc.RotateTransAffine.GetPositionAfterRotation(obj.mPosition, objTrueRot, calc.RotateTransAffine.TargetVector.All);

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
                            bmdTriangleData.TriangleDataList.AddRange(GetTriangleFaces(obj ,prim, bmd, mtxtable));

                        }
                    }
                }
            }
            else
            {
                // TODO: dummy boxのポリゴンを返す。
            }
            return bmdTriangleData;
        }
        public static BMDTriangleData GetTriangles(BMD model)
        {
            BMDTriangleData bmdTriangleData = new BMDTriangleData();

                //Batchファイル内のPacketsを取得
                foreach (BMD.Batch batch in model.Batches)
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
                                BMD.MatrixType mtxtype = model.MatrixTypes[packet.MatrixTable[i]];

                                if (mtxtype.IsWeighted)
                                {
                                    mtxtable[i] = Matrix4.Identity;

                                    mtx_debug[i] = 1;
                                }
                                else
                                {
                                    mtxtable[i] = model.Joints[mtxtype.Index].FinalMatrix;
                                    mtx_debug[i] = 0;
                                }
                            }
                        }

                        lastmatrixtable = mtxtable;




                        //ジオメトリ情報にアクセスしていると思われる。
                        foreach (BMD.Batch.Packet.Primitive prim in packet.Primitives)
                        {
                            bmdTriangleData.TriangleDataList.AddRange(GetTriangleFaces(prim, model, mtxtable));

                        }
                    }
                }
            
            return bmdTriangleData;
        }

        private static void DebugTriangleRendering(BMDTriangleData bmdTriangleData)
        {
            GL.PushMatrix();
            foreach (var triangleVec in bmdTriangleData.TriangleDataList)
            {
                GL.Begin(BeginMode.Triangles);
                GL.Vertex3(triangleVec.V0.X, triangleVec.V0.Y, triangleVec.V0.Z);
                GL.Vertex3(triangleVec.V1.X, triangleVec.V1.Y, triangleVec.V1.Z);
                GL.Vertex3(triangleVec.V2.X, triangleVec.V2.Y, triangleVec.V2.Z);
                GL.Translate(TargetObject.mTruePosition);
                GL.Rotate(TargetObject.mTrueRotation.Z, 0f, 0f, 1f);
                GL.Rotate(TargetObject.mTrueRotation.Y, 0f, 1f, 0f);
                GL.Rotate(TargetObject.mTrueRotation.X, 1f, 0f, 0f);
                GL.Scale(TargetObject.mScale.X, TargetObject.mScale.Y, TargetObject.mScale.Z);

                //GL.Rotate(TargetObject.mTrueRotation.Z, 0f, 0f, 1f);
                //GL.Rotate(TargetObject.mTrueRotation.Y, 0f, 1f, 0f);
                //GL.Rotate(TargetObject.mTrueRotation.X, 1f, 0f, 0f);
                //GL.Scale(TargetObject.mScale.X * 2.0f, TargetObject.mScale.Y * 2.0f, TargetObject.mScale.Z * 2.0f);
                GL.End();
            }
            GL.PopMatrix();
            Debug.WriteLine($"TargetObjectPosition : {TargetObject.mTruePosition}");
        }

        private static List<BMDTriangleData.TrianglesPosition> GetTriangleFaces(AbstractObj abstructObj, BMD.Batch.Packet.Primitive prim, BMD bmd, Matrix4[] mtxtable)
        {
            //Debug.WriteLine(beginMode[(prim.PrimitiveType - 0x80) / 8]);

            List<BMDTriangleData.TrianglesPosition> trianglesPositionList = new List<BMDTriangleData.TrianglesPosition>();
            var openglVec = new Vector3(abstructObj.mPosition.X, abstructObj.mPosition.Y, abstructObj.mPosition.Z);
            switch (beginMode[(prim.PrimitiveType - 0x80) / 8])
            {
                case BeginMode.Triangles:
                    for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
                    {
                        //頂点情報

                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 pos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]] + openglVec /*+ objTruePos + positionWithZoneRotation*/, 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref pos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out pos);
                        else
                            Vector4.Transform(ref pos, ref mtxtable[0], out pos);

                        //Debug.WriteLine(pos);
                        Debug.WriteLine("通常△");
                    }
                    break;
                case BeginMode.TriangleStrip:
                    for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
                    {
                        Vector4[] trianglePositionArray = new Vector4[3];

                        //頂点情報
                        if (vertexIndex == 0)
                        {
                            //頂点情報


                            for (int i = 0; i < 2; i++)
                            {
                                //頂点インデックスにあった頂点番号の頂点を順番にセット
                                Vector4 firstpos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]] + openglVec /*+ objTruePos + positionWithZoneRotation*/, 1.0f);

                                //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                                if ((prim.ArrayMask & 1) != 0)
                                    Vector4.Transform(ref firstpos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out firstpos);
                                else
                                    Vector4.Transform(ref firstpos, ref mtxtable[0], out firstpos);

                                //Debug.WriteLine(firstpos);

                                trianglePositionArray[i] = new Vector4(firstpos);

                                vertexIndex++;
                            }

                            continue;
                        }


                        //Debug.WriteLine("-----------------------");
                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 beforePos2 = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex - 2]] + openglVec /*+ objTruePos + positionWithZoneRotation*/, 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref beforePos2, ref mtxtable[prim.PosMatrixIndices[vertexIndex - 2]], out beforePos2);
                        else
                            Vector4.Transform(ref beforePos2, ref mtxtable[0], out beforePos2);

                        trianglePositionArray[0] = beforePos2;

                        //Debug.WriteLine(beforePos2);

                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 beforePos1 = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex - 1]] + openglVec /*+ objTruePos + positionWithZoneRotation*/, 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref beforePos1, ref mtxtable[prim.PosMatrixIndices[vertexIndex - 1]], out beforePos1);
                        else
                            Vector4.Transform(ref beforePos1, ref mtxtable[0], out beforePos1);

                        //Debug.WriteLine(beforePos1);

                        trianglePositionArray[1] = beforePos1;

                        //頂点インデックスにあった頂点番号の頂点を順番にセット
                        Vector4 pos = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]] + openglVec /*+ objTruePos + positionWithZoneRotation*/, 1.0f);

                        //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                        if ((prim.ArrayMask & 1) != 0)
                            Vector4.Transform(ref pos, ref mtxtable[prim.PosMatrixIndices[vertexIndex]], out pos);
                        else
                            Vector4.Transform(ref pos, ref mtxtable[0], out pos);


                        trianglePositionArray[2] = pos;

                        //Debug.WriteLine(pos);

                        trianglesPositionList.Add(new BMDTriangleData.TrianglesPosition(trianglePositionArray));
                    }


                    break;
                case BeginMode.TriangleFan:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");
                default:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");


            }



            return trianglesPositionList;
        }

    private static List<BMDTriangleData.TrianglesPosition> GetTriangleFaces(BMD.Batch.Packet.Primitive prim, BMD bmd, Matrix4[] mtxtable)
        {
            int triangleVertCount = 3;
            //Debug.WriteLine(beginMode[(prim.PrimitiveType - 0x80) / 8]);

            List<BMDTriangleData.TrianglesPosition> trianglesPositionList = new List<BMDTriangleData.TrianglesPosition>();
            switch (beginMode[(prim.PrimitiveType - 0x80) / 8])
            {
                case BeginMode.Triangles:
                    for (int vertexIndex = 0; vertexIndex < prim.NumIndices; vertexIndex++)
                    {
                        Vector4[] trianglePositionArray = new Vector4[3];
                        // Triangleなら3固定？
                        foreach (var triangleVertIndex in Enumerable.Range(0, prim.NumIndices))
                        {
                            //頂点情報
                            //頂点インデックスにあった頂点番号の頂点を順番にセット
                            trianglePositionArray[triangleVertIndex] = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                            //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                            if ((prim.ArrayMask & 1) != 0)
                                Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                    ref mtxtable[prim.PosMatrixIndices[vertexIndex]],
                                    out trianglePositionArray[triangleVertIndex]);
                            else
                                Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                    ref mtxtable[0],
                                    out trianglePositionArray[triangleVertIndex]);

                            // Debug.WriteLine(pos);
                            // Debug.WriteLine("通常△");
                        }
                        trianglesPositionList.Add(new BMDTriangleData.TrianglesPosition(trianglePositionArray));
                    }
                    break;
                case BeginMode.TriangleStrip:
                    for (int vertexIndex = 2; vertexIndex < prim.NumIndices;)
                    {
                        // 一つの線の両端から次の頂点を回転方向を変更することで三角面を取得します。
                        // この処理を線ごとにするため、index初期値は線の頂点数となる。
                        vertexIndex -= 2;
                        Vector4[] trianglePositionArray = new Vector4[3];
                        if ((vertexIndex % 2) != 0) 
                        {
                            // 順方向
                            foreach (var triangleVertIndex in Enumerable.Range(0, triangleVertCount))
                            {
                                //頂点情報
                                //頂点インデックスにあった頂点番号の頂点を順番にセット
                                trianglePositionArray[triangleVertIndex] = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                                //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                                if ((prim.ArrayMask & 1) != 0)
                                    Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                        ref mtxtable[prim.PosMatrixIndices[vertexIndex]],
                                        out trianglePositionArray[triangleVertIndex]);
                                else
                                    Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                        ref mtxtable[0],
                                        out trianglePositionArray[triangleVertIndex]);

                                // Debug.WriteLine(pos);
                                // Debug.WriteLine("通常△");
                                vertexIndex++;
                            }
                        }
                        else
                        {
                            // 逆方向
                            foreach (var triangleVertIndex in Enumerable.Range(0, triangleVertCount).Reverse())
                            {
                                //頂点情報
                                //頂点インデックスにあった頂点番号の頂点を順番にセット
                                trianglePositionArray[triangleVertIndex] = new Vector4(bmd.PositionArray[prim.PositionIndices[vertexIndex]], 1.0f);

                                //モデルの拡大縮小、回転、移動を頂点ごとに適用(モデルビュープロジェクション行列でやるのは適さないから しかし、CPUで計算するので負荷高い)
                                if ((prim.ArrayMask & 1) != 0)
                                    Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                        ref mtxtable[prim.PosMatrixIndices[vertexIndex]],
                                        out trianglePositionArray[triangleVertIndex]);
                                else
                                    Vector4.Transform(ref trianglePositionArray[triangleVertIndex],
                                        ref mtxtable[0],
                                        out trianglePositionArray[triangleVertIndex]);

                                // Debug.WriteLine(pos);
                                // Debug.WriteLine("通常△");
                                vertexIndex++;
                            }
                        }
                        trianglesPositionList.Add(new BMDTriangleData.TrianglesPosition(trianglePositionArray));
                    }


                    break;
                case BeginMode.TriangleFan:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");
                default:
                    throw new NotSupportedException($"{beginMode[(prim.PrimitiveType - 0x80) / 8]}の形状データは未対応です。");


            }



            return trianglesPositionList;
        }
    }
}
