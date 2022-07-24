using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;

namespace Takochu.smg
{
    class NewObjectDB 
    {
        public const string Xml_PathString = "res/New_objectdb.xml";
        public const string URL_LinkString = "https://raw.githubusercontent.com/SunakazeKun/galaxydatabase/main/objectdb.xml";

        public static Dictionary<ushort, Dictionary<string, Object>> Categories;
        public static Dictionary<string, Object> Objects;

        public static TreeNode[] ObjectNodes;

        public static void GenDB()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(URL_LinkString, Xml_PathString);
                }
                catch (WebException webEx)
                {
                    if (webEx.Status == WebExceptionStatus.NameResolutionFailure)
                    {
                        File.WriteAllText(Xml_PathString, Properties.Resources.objectdb);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void Generate_WhenNotfound()
        {
            var AppCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(AppCurrentDirectory + Xml_PathString) == false)
                GenDB();
        }

        public static void Load()
        {
            Generate_WhenNotfound();

            
            //Actors = new Dictionary<string, Actor>();
            Objects = new Dictionary<string, Object>();

            XmlDocument xmlDB_Doc = new XmlDocument();
            xmlDB_Doc.Load(Xml_PathString);

            var dbNodesCount = xmlDB_Doc.DocumentElement.ChildNodes.Count;

            Categories = new Dictionary<ushort, Dictionary<string, Object>>();

            

            for (int dbNodeNo = 0; dbNodeNo < dbNodesCount; dbNodeNo++) 
            {
                //カテゴリーノードの読み取り
                if (dbNodeNo == 0) 
                {
                    XmlNode CategoriesNode = xmlDB_Doc.DocumentElement.ChildNodes[0];
                    Categories = new Dictionary<ushort, Dictionary<string, Object>>();

                    var categoryNodeCount = CategoriesNode.ChildNodes.Count;
                    ObjectNodes = new TreeNode[categoryNodeCount];
                    for (ushort i = 0; i < categoryNodeCount; i++)
                    {
                        XmlNode categoryXmlNode = CategoriesNode.ChildNodes[i];
                        ushort categoryID = Convert.ToUInt16(categoryXmlNode.Attributes["id"].Value);
                        Categories[i] = new Dictionary<string, Object>();
                        //Categories.Add(categoryID,default);
                        ObjectNodes[i] = new TreeNode
                        {
                            Text = categoryXmlNode.InnerText,
                            Tag = categoryID
                        };
                    }
                    continue;
                }

                //オブジェクトデータ読み取り
                XmlNode objectNode = xmlDB_Doc.DocumentElement.ChildNodes[dbNodeNo];
                XmlNode flagsNode = objectNode["flags"];
                FlagData flags = new FlagData(flagsNode);
                Object objectData = new Object(objectNode,flags);
                Categories[objectData.CategoryID].Add(objectNode.Attributes["id"].Value, objectData);
                ObjectNodes[objectData.CategoryID].Nodes.Add(objectData.DisplayName/*objectNode.Attributes["id"].Value*/);
                ObjectNodes[objectData.CategoryID].Nodes[ObjectNodes[objectData.CategoryID].Nodes.Count -1].Tag = objectData;
            }
            
            //ObjectNodes = new TreeNode[Objects.Count];
            //for (int i = 0; i < Objects.Count; i++)
            //{
            //    ObjectNodes[i] = new TreeNode(Objects.ElementAt(i).Key)
            //    {
            //        Tag = Objects.ElementAt(i).Value
            //    };
            //}
        }

        public class Object 
        {
            public string FileName;
            public string DisplayName;
            public FlagData Flags;
            public ushort CategoryID;
            public string TypeName;
            public string Notes;

            public Object(XmlNode objectNode,FlagData flags) 
            {
                FileName = objectNode.Attributes["id"].Value;
                DisplayName = objectNode["name"].InnerText;
                Flags = flags;
                CategoryID = Convert.ToUInt16(objectNode["category"].Attributes["id"].Value);
                TypeName = objectNode["preferredfile"].Attributes["name"].Value;
                Notes = objectNode["notes"].InnerText;
            }
        }

        public struct FlagData 
        {
            public short Games;
            public short Known;
            public short Complete;
            public short NeedsPaths;

            public FlagData(XmlNode flagsNode) 
            {
                Games        = Convert.ToInt16  (flagsNode.Attributes["games"].Value);
                Known        = Convert.ToInt16  (flagsNode.Attributes["known"].Value);
                Complete     = Convert.ToInt16  (flagsNode.Attributes["complete"].Value);
                NeedsPaths   = Convert.ToInt16  (flagsNode.Attributes["needsPaths"].Value);
            }
        }
    }

    class ObjectDB
    {
        public const string Xml_PathString = "res/objectdb.xml";
        public const string URL_LinkString = "http://shibboleet.us.to/new_db/generate.php";

        public static Dictionary<string, Actor> Actors;
        public static Dictionary<string, Object> Objects;

        public static TreeNode[] ObjectNodes;

        //public static TreeView ObjectTreeView = new TreeView();

        //public static TreeNodeCollection ObjectTreeNodeCollection { get; private set; }

        //public static string[] ObjectNames;

        public static void GenDB()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(URL_LinkString, Xml_PathString);
                }
                catch (WebException webEx)
                {
                    if (webEx.Status == WebExceptionStatus.NameResolutionFailure) 
                    {
                        File.WriteAllText(Xml_PathString, Properties.Resources.objectdb);
                    }
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void Generate_WhenNotfound() 
        {
            var AppCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(AppCurrentDirectory + Xml_PathString) == false)
                GenDB();
        }

        public static void Load()
        {
            Generate_WhenNotfound();


            Actors = new Dictionary<string, Actor>();
            Objects = new Dictionary<string, Object>();

            XmlDocument db = new XmlDocument();
            db.Load(Xml_PathString);

            XmlNode actorsNode = db.DocumentElement.ChildNodes[0];

            foreach(XmlNode actorNode in actorsNode.ChildNodes)
            {
                Actor actorData = new Actor();

                actorData.ActorName = actorNode.Attributes["id"].Value;

                XmlNode generalFlags = actorNode["flags"];
                actorData.IsKnown = Convert.ToInt32(generalFlags.Attributes["known"].Value);
                actorData.IsComplete = Convert.ToInt32(generalFlags.Attributes["complete"].Value);
                actorData.Fields = new List<ActorField>();
                XmlNode fields = actorNode["fields"];

                foreach(XmlNode field in fields.ChildNodes)
                {
                    ActorField f = new ActorField();
                    string arg = field.Attributes["id"].Value;
                    f.Arg = Int32.Parse(arg[arg.Length - 1].ToString());
                    f.Name = field.Attributes["name"].Value;
                    f.Type = field.Attributes["type"].Value;
                    f.Value = field.Attributes["values"].Value;
                    f.Notes = field.Attributes["notes"].Value;
                    actorData.Fields.Add(f);
                }

                Actors.Add(actorData.ActorName, actorData);
            }

            XmlNode objects = db.DocumentElement.ChildNodes[1];

            foreach(XmlNode objs in objects.ChildNodes)
            {
                Object obj = new Object();

                obj.InternalName = objs.Attributes["id"].Value;

                XmlNode generalFlags = objs["attributes"];

                obj.Name = generalFlags["name"].InnerText;
                obj.Actor = generalFlags["actor"].InnerText;
                obj.Notes = generalFlags["notes"].InnerText;
                obj.Game = Int32.Parse(generalFlags["flags"].Attributes["games"].Value);

                Objects.Add(obj.InternalName, obj);
            }

            ObjectNodes = new TreeNode[Objects.Count];
            for(int i = 0; i<Objects.Count; i++) 
            {
                
                ObjectNodes[i] = new TreeNode(Objects.ElementAt(i).Key);
                ObjectNodes[i].Tag = Objects.ElementAt(i).Value;
                //ObjectNodes[i].Text = Objects.ElementAt(i).Key;
                //ObjectNodes[i].Name = Objects.ElementAt(i).Key;
            }

            //ObjectTreeNodeCollection = new TreeNodeCollection(ObjectNodes);
            //ObjectTreeNodeCollection.AddRange(ObjectNodes);

            //ObjectTreeView = new TreeView();
            //ObjectTreeView.Nodes.AddRange(ObjectNodes);
        }

        public static string GetFriendlyObjNameFromObj(string objName)
        {
            if (!Objects.ContainsKey(objName))
            {
                return objName;
            }

            string name = Objects[objName].Name;

            if (name == "")
            {
                return objName;
            }

            return name;
        }

        public static Actor GetActorFromObjectName(string objName)
        {
            if (!Objects.ContainsKey(objName))
                return null;

            Object obj = Objects[objName];

            if (obj.Actor == "")
                return null;

            return Actors[obj.Actor];
        }

        public static ActorField GetFieldFromActor(Actor actor, int fieldNo)
        {
            foreach(ActorField field in actor.Fields)
            {
                if (field.Arg == fieldNo)
                    return field;
            }

            return null;
        }

        public static string[] GetFieldAsList(ActorField field)
        {
            string[] elements = field.Value.Split(',');
            return elements;
        }

        public static int IndexOfSelectedListField(ActorField field, int value)
        {
            string[] list = GetFieldAsList(field);
            int index = -1;

            foreach(string element in list)
            {
                index++;

                string[] split = element.Split('=');

                if (Int32.Parse(split[0]) == value)
                    return index;
            }

            return -1;
        }

        public static bool UsesObjArg(string objName, int argNo)
        {
            bool ret = false;

            Actor actor = GetActorFromObjectName(objName);

            if (actor == null)
                return ret;

            actor.Fields.ForEach(f =>
            {
                if (f.Arg == argNo)
                    ret = true;
            });

            return ret;
        }

        public class Actor
        {
            public string ActorName;
            public int IsKnown;
            public int IsComplete;
            public List<ActorField> Fields;
        }

        public class ActorField
        {
            public int Arg;
            public string Type;
            public string Name;
            public string Value;
            public string Notes;
        }

        public class Object
        {
            public string InternalName;
            public string Name;
            public string Actor;
            public int Game;
            public string Notes;
        }

       
    }
}
