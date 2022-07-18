using System.Collections.Generic;
using Takochu.smg.obj;

namespace Takochu.util.GameVer
{
    public interface IGameVersion
    {
        List<string> AddObjectList { get; }
        //Dictionary<string, AbstractObj> AddObjectDictionary{get;}
    }
}
