using System;
using System.Data;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.OtherForm;
using ForecastingModule.Utilities;

namespace ForecastingModule.Controller
{
    internal class SettingsController : BaseController, ISettingConntroller
    {
        private SqlDataAdapter adapter;
        private DataTable dataTable;

        private DataRow newRow; // To track new row

        private readonly string selectedTableQuery;
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
            // Add a new row to the DataTable
            newRow = dataTable.NewRow();
            //set default values
            newRow["OPS_RecordID"] = Guid.NewGuid(); 
            newRow["OPS_Tab"] = ""; 
            newRow["OPS_NbrMonths"] = 18; 
            newRow["OPS_NbrDays"] = 60; 
            newRow["OPS_ActiveFlag"] = 1; 
            newRow["OPS_DisplayOrder"] = 0; 
            newRow["OPS_Comments"] = ""; 

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
