using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.OtherForm;
using ForecastingModule.Utilities;

namespace ForecastingModule.Controller
{
    internal class SettingsController : BaseController, ISettingConntroller
    {
        private readonly Dictionary<string, Func<object>> TYPE_VALUE_MAP = new Dictionary<string, Func<object>> {
                { "uniqueidentifier", () => Guid.NewGuid() },
                { "nvarchar", () => "" },
                { "int", () => 0 },
                { "bit", () => 1 }
            };

        private SqlDataAdapter adapter;
        private DataTable dataTable;

        private DataRow newRow; // To track new row

        private readonly string selectedTableQuery;
        private static readonly Logger log = Logger.Instance;
        public SettingsController(object model, IView view, string selectedTableQuery) : base(model, view)
        {
            this.selectedTableQuery = selectedTableQuery;
            ((SettingsView)view).OnLoad += Load;
            ((SettingsView)view).OnInsert += Insert;
            ((SettingsView)view).OnDelete += Delete;
            ((SettingsView)view).OnSave += Save;
        }

        public override object Load()
        {
            dataTable = new DataTable();
            if (this.selectedTableQuery == null)
            {
                Logger.Instance.LogInfo("SettingsController");
                return dataTable;
            }
            string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);

            adapter = new SqlDataAdapter(this.selectedTableQuery, connectionString);
            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

            // Fill DataTable
            adapter.Fill(dataTable);
            return dataTable;
        }

        public void Insert()
        {
            newRow = dataTable.NewRow();

            Tuple<string, List<Tuple<string, string>>> locaModel = ((Tuple<string, List<Tuple<string, string>>>)(this.model));
            List<Tuple<string, string>> nameTypeList = locaModel.Item2;//return name - type tuple 
            foreach (Tuple<string, string> item in nameTypeList)//iterate by item1 - columnName, item2 - column type
            {
                Func<object> value;
                TYPE_VALUE_MAP.TryGetValue(item.Item2, out value);//serach be type name 
                if(value == null)
                {
                    log.LogWarning($"Table {locaModel.Item1} - column {item.Item1} with type {item.Item2} does not find in typeValueMap.");
                    continue;
                }
                newRow[item.Item1]  = value.Invoke();//call method from typeValueMap
            }

            // Add the new row to the DataTable
            dataTable.Rows.Add(newRow);
        }

        public void Delete(int rowIndex)
        {
            dataTable.Rows[rowIndex].Delete();
        }

        public void Save()
        {
            SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);
            // Update the database with changes from the DataTable
            adapter.Update(dataTable);
        }
    }
}
