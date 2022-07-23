using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.smg.obj;

namespace Takochu.util.GameVer
{

    public class SMG2:IGameVersion
    {
        public List<string> AddObjectList => new List<string>()
        {
            "Normal",
            "Spawn",
            "Gravity",
            "Area",
            "Camera",
            "MapPart",
            "Cutscene",
            "Position",
            "Changer",
            "Debug",
            "Path",
            "PathPoint"
        };

        //public Dictionary<string, AbstractObj> AddObjectDictionary => new Dictionary<string, AbstractObj>() 
        //{
        //    { "Normal", new AbstractObj()},
        //    "Spawn",
        //    "Gravity",
        //    "Area",
        //    "Camera",
        //    "MapPart",
        //    "Cutscene",
        //    "Position",
        //    "Changer",
        //    "Debug",
        //    "Path",
        //    "PathPoint"
        //};

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}
