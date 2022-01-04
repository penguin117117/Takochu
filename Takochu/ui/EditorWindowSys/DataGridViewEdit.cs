using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.smg.obj;
using Takochu.smg;
using ObjDB = Takochu.smg.ObjectDB;


namespace Takochu.ui.EditorWindowSys
{
    /*
    ********************************************************************************************************************
    In progress. 2021/12～
    Create a class to display the properties of an object in the data grid view.
    Specifically, you can generate a data table to put into the data source of the data grid view in the editor window.

    [Implemented features]
    1. Viewing Object Properties

    [ToDo]
    1. Implement a function to translate data names.
    2. Reflect the changed data in the actual object.(Only partially works. rot,pos,scale)
    3. このクラスは機能が増えすぎて汚くなってきているのでリファクタリングをする必要がある。

    By penguin117117
    ********************************************************************************************************************
    */

    public class DataGridViewEdit
    {
        private static bool _isChanged = false;
        public static bool IsChanged { get => _isChanged;  }

        public List<string> Obj_argNotes { get => _obj_argNote; }

        public Dictionary<string,string> Obj_argDisplayNames { get => _obj_argDisplayNames; }

        private Dictionary<string,string> _obj_argDisplayNames;
        private List<string> _obj_argNote;
        private DataColumn cLeft, cRight;
        private AbstractObj _abstObj;
        private readonly DataGridView _dataGridView;
        private DataTable _dt;

        /// <summary>
        /// Set "DataGridView" to display the properties of the object displayed in the editor window.
        /// </summary>
        /// <param name="dataGridView">Specify the target "DataGridView".</param>
        /// <param name="abstObj">Specify the "AbstractObj" class or the XXXXObj class that inherits from the "AbstractObj" class.</param>
        public DataGridViewEdit(DataGridView dataGridView, AbstractObj abstObj)
        {
            _dataGridView = dataGridView;
            _abstObj = abstObj;
            _obj_argNote = new List<string>();
            _obj_argDisplayNames = new Dictionary<string, string>();
        }

        public static void IsChangedClear() 
        {
            _isChanged = false;
        }

        /// <summary>
        /// Create and retrieve a data table.
        /// </summary>
        /// <returns><see cref="DataTable"/></returns>
        public DataTable GetDataTable()
        {
            Initialize();
            NullCheck();
            
            DataTable dt = new DataTable();

            SetColumn(ref dt);
            SetRow(ref dt);

            _dt = dt;

            return dt;
        }

        private void Initialize()
        {
            if (_dataGridView.DataSource != null)
                _dataGridView.DataSource = null;

            _dataGridView.AllowUserToAddRows = false;
            _dataGridView.AllowUserToResizeRows = false;
            _dataGridView.AllowUserToResizeColumns = false;
        }

        private void NullCheck()
        {
            if (_abstObj == null)
                throw new Exception("GalaxyObject is null");
        }

        private void SetColumn(ref DataTable dt)
        {
            cLeft = dt.Columns.Add("Name");
            {
                cLeft.DataType = Type.GetType("System.String");
                cLeft.ColumnName = "Info";
                cLeft.ReadOnly = true;
                cLeft.Unique = true;
                cLeft.AutoIncrement = false;
            }


            cRight = dt.Columns.Add("Value");
            {
                cRight.DataType = Type.GetType("System.Object");
                cRight.ColumnName = "Value";
                cRight.ReadOnly = false;
                cRight.Unique = false;
                cRight.AutoIncrement = false;
            }
        }

        private void SetRow(ref DataTable dt)
        {
            //Console.WriteLine(ObjDB.GetActorFromObjectName(_abstObj.mName).ActorName);
            
            foreach (var ObjEntry in _abstObj.mEntry)
            {
                var oldDisplayName = BCSV.HashToFieldName(ObjEntry.Key);

                var newDisplayName = Set_ObjargNameFromObjectDB(oldDisplayName);
                

                var row = dt.NewRow();
                {
                    row.SetField(cLeft, newDisplayName); row.SetField(cRight, ObjEntry.Value);
                }
                dt.Rows.Add(row);
                _obj_argDisplayNames.Add(newDisplayName,oldDisplayName);
            }
        }

        public int GetObj_argNo(string displayName) 
        {
            int Obj_argNo = -1;
            if (displayName.Length <= "Obj_arg".Length)
                return Obj_argNo;

            if (displayName.Length != "Obj_arg".Length + 1)
                return Obj_argNo;

            var argIndexTest = displayName.IndexOf("Obj_arg");

            if (argIndexTest == -1)
                return Obj_argNo;

            var argNo_CharArray = displayName.Skip("Obj_arg".Length).ToArray();

            Console.WriteLine(argNo_CharArray[0]);

            var success = Int32.TryParse(argNo_CharArray[0].ToString(),out int parseObj_argNo);
            if (success) return parseObj_argNo;
            return Obj_argNo;
        }

        private string Set_ObjargNameFromObjectDB(string displayName) 
        {
            int argIndex = GetObj_argNo(displayName);
            if (argIndex == -1) 
                return displayName;

            string note = string.Empty;

            if (ObjDB.UsesObjArg(_abstObj.mName, argIndex))        
            {
                
                var actorfield = ObjDB.GetFieldFromActor(ObjDB.GetActorFromObjectName(_abstObj.mName), argIndex);
                displayName = actorfield.Name;
                note = actorfield.Notes;
                Console.WriteLine(note);
            }
            _obj_argNote.Add(note);





            return displayName;
        }

        public void ChangeValue(int rowIndex, object value)
        {
            var a = _abstObj.mEntry.Keys;
            var name = BCSV.HashToFieldName(a.ElementAt(rowIndex));
            //foreach (var t in a) 
            //{
            //    var str = BCSV.HashToFieldName(t);
            //    Console.WriteLine(str);
            //    Console.WriteLine(_abstObj.mEntry.Get(str));
            //} 

            //Do not allow the object name to be changed.
            if (name == "name") 
            {
                
                return;
            }
            //_abstObj.mEntry.Set(name, value);
            //_abstObj.Reload_mValue();
            Change_mValues(name, value);
        }

        /*
        [HACK]
        Not smart processing, 
        the object does not move without changing the mValue,
        so I have no choice but to use the switch case.
        If you have a solution, please fix it.
        By penguin117117
         */
        private void Change_mValues(string name, object value)
        {
            Console.WriteLine($"Change_mValues: {name}");
            //float ftmp = GetFloat_And_Limiter(value);
            //Console.WriteLine("mDirectory: "+ _abstObj.mDirectory);
            //Console.WriteLine("mFile: " + _abstObj.mFile);
            //Console.WriteLine("mFile: " + _abstObj.mName);

            //foreach (var t in _abstObj.mEntry) Console.WriteLine(t.Key);

            _abstObj.mEntry.Set(name, value);
            _abstObj.Reload_mValues();
            _isChanged = true;
            //_abstObj.mTruePosition = new OpenTK.Vector3(GetFloat_And_Limiter(_abstObj.mEntry.Get("pos_x")), GetFloat_And_Limiter(_abstObj.mEntry.Get("pos_y")), GetFloat_And_Limiter(_abstObj.mEntry.Get("pos_z")));
            //switch (name)
            //{
            //    case "pos_x":
            //        _abstObj.mTruePosition.X = ftmp;
            //        break;
            //    case "pos_y":
            //        _abstObj.mTruePosition.Y = ftmp;
            //        break;
            //    case "pos_z":
            //        _abstObj.mTruePosition.Z = ftmp;
            //        break;
            //    case "dir_x":
            //        _abstObj.mTrueRotation.X = ftmp;
            //        break;
            //    case "dir_y":
            //        _abstObj.mTrueRotation.Y = ftmp;
            //        break;
            //    case "dir_z":
            //        _abstObj.mTrueRotation.Z = ftmp;
            //        break;
            //    case "scale_x":
            //        _abstObj.mScale.X = ftmp;
            //        break;
            //    case "scale_y":
            //        _abstObj.mScale.Y = ftmp;
            //        break;
            //    case "scale_z":
            //        _abstObj.mScale.Z = ftmp;
            //        break;
            //    default:
            //        //処理設定されている物以外は値の変更を行わない。
            //        //You cannot change any value other than the one that is set.
            //        return;
            //}
            
            //_abstObj.Save();
        }

        private bool IsStringParam(string name) 
        {
            switch (name) 
            {
                case "name":
                    return true;
                default:
                    return false;
            }
        }

        private float GetFloat_And_Limiter(object value)
        {

            if (!float.TryParse(value.ToString(), out float ftmp)) return 0f;
            ftmp = Convert.ToSingle(value);
            if (ftmp > float.MaxValue) ftmp = float.MaxValue;
            if (ftmp < float.MinValue) ftmp = float.MinValue;

            return ftmp;
        }
    }
}
