using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.io;
using Takochu.fmt;

namespace Takochu.fmt.SectionData
{
    /*
     このセクションでは、マテリアルやジョイントの階層を取得したり、その他のモデルの情報も取得します。
     要するにモデルをレンダリングするうえで必要な情報が格納されています。
     */
    public class INF1
    {
        /// <summary>
        /// モデルの階層を示したShort型のデータ
        /// </summary>
        public enum NodeTypes : ushort
        {
            FinishNode   = 0x0000,
            NewNode      = 0x0001,
            EndNode      = 0x0002,
            AddJoint     = 0x0010,
            AddMaterial  = 0x0011,
            AddShape     = 0x0012
        }

        //ヘッダ情報
        public uint SectionSize { get; private set; }
        public ushort UnknownSetting { get; private set; }
        public uint MatrixGroupCount { get; private set; }
        public uint VertexCount { get; private set; }
        public uint HierarchyDataOffset { get; private set; }

        //データ情報
        //public List<BMD.SceneGraphNode> SceneGraph { get; private set; }
        public Stack<INF1NodeData> INF1Nodes { get; private set; }

        private Stack<ushort> _matstack;
        private Stack<int> _nodestack;
        private long _pos_StartAddress;

        public INF1() 
        {
            
        }

        public void Read(FileBase fileBase)
        {
            Initialize();
            SetStackEndData();
            SetHeader(fileBase);
            SetHierarchyAndInfo(fileBase);
            fileBase.Seek((int)(_pos_StartAddress + SectionSize));
        }

        private void Initialize() 
        {
            //SceneGraph = new List<BMD.SceneGraphNode>();
            _matstack = new Stack<ushort>();
            _nodestack = new Stack<int>();

            
        }

        private void SetStackEndData() 
        {
            //後にスタックの末尾を判別するために代入？
            _matstack.Push(0xFFFF);
            _nodestack.Push(-1);
        }

        private void SetHeader(FileBase fileBase) 
        {
            //ヘッダー情報取得
            _pos_StartAddress = fileBase.Position() - 4;

            SectionSize = fileBase.ReadUInt32();
            UnknownSetting = fileBase.ReadUInt16();

            fileBase.Skip(6);   //パディングをスキップ(ushort + uint 6byte)
            MatrixGroupCount = fileBase.ReadUInt32();
            VertexCount = fileBase.ReadUInt32();
            //fileBase.Skip((int)(datastart - 0x18));
            HierarchyDataOffset = fileBase.ReadUInt32();
        }

        private void SetHierarchyAndInfo(FileBase fileBase) 
        {
            //階層構造や階層に格納されたモデル情報(Joint,Vertex,Geometry)を取得します
            ushort ReadNodeType_ushortNum;
            ushort HierarchyID = 0;
            while ((ReadNodeType_ushortNum = fileBase.ReadUInt16()) != 0)
            {
                ushort NodeTypeTagNo = fileBase.ReadUInt16();

                switch (ReadNodeType_ushortNum)
                {
                    case 0x0001:
                        HierarchyID++;
                        break;
                    case 0x0002:
                        HierarchyID--;
                        break;
                    case 0x0010:
                    case 0x0011:
                    case 0x0012:
                        INF1Nodes.Push(new INF1NodeData(HierarchyID, (NodeTypes)ReadNodeType_ushortNum, NodeTypeTagNo));
                        break;
                }
            }

            //EndNodeの処理
            INF1Nodes.Push(new INF1NodeData(0, (NodeTypes)0x0000, 0x0000));
        }

        /// <summary>
        /// A structure that stores the data of the INF1 section.<br/>
        /// INF1セクションのデータを格納する構造体
        /// </summary>
        public struct INF1NodeData
        {
            public ushort HierarchyID;
            public NodeTypes NodeType;
            public ushort NodeTypeTagNo;
            public INF1NodeData(ushort hierarchyID ,NodeTypes nodeType ,ushort nodeTypeTagNo)
            {
                HierarchyID = hierarchyID;
                NodeType = nodeType;
                NodeTypeTagNo = nodeTypeTagNo;
            }
        }

        
            
        
    }
}
