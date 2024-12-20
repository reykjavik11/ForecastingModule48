using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal class OperationsPlanningRepositoryImpl : OperationsPlanningRepository
    {
        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<OperationsPlanningRepositoryImpl> _instance = new Lazy<OperationsPlanningRepositoryImpl>(() => new OperationsPlanningRepositoryImpl());

        public static OperationsPlanningRepositoryImpl Instance => _instance.Value;

        private OperationsPlanningRepositoryImpl()
        {
        }

        public Dictionary<string, object> getOperationsSetting(string equipmentName)
        {
            Dictionary<string, object> operationSettings = new Dictionary<string, object>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand($"select op.OPS_NbrDays, op.OPS_NbrMonths, op.OPS_Comments FROM [WeilerForecasting].[dbo].[OperationsSettings] op where op.OPS_Tab ='{equipmentName}' and op.OPS_ActiveFlag=1", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object value = reader.GetValue(i);

                                    operationSettings.Add(columnName, value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.StackTrace);
                throw ex;
            }
            return operationSettings;
        }

        public SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveExistedOperationsPlanning(string equipmentName)
        {
            SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> result = new SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //must be ordering - return structure is linked depend by ordering elements
                    using (var command = new SqlCommand($"select sc.SC_SalesCode, op.OP_Date, op.OP_Quantity, sc.SC_Model from (select SC_SalesCode, SC_Model, SC_OperationsTab from [WeilerForecasting].[dbo].[SalesCodes] where SC_BaseFlag=1) sc left join [WeilerForecasting].[dbo].[OperationsPlanning] op on op.OP_Model = sc.SC_Model left join [WeilerForecasting].[dbo].[OperationsSettings] os on os.OPS_Tab = sc.SC_OperationsTab where sc.SC_OperationsTab='{equipmentName}' and op.OP_Date between DATEFROMPARTS(YEAR(DATEADD(day, os.OPS_NbrDays, GETDATE())), MONTH(DATEADD(day, os.OPS_NbrDays, GETDATE())), 1) and EOMONTH(DATEADD(month, os.OPS_NbrMonths, GETDATE())) order by sc.SC_SalesCode", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string salesCode = reader.GetString(reader.GetOrdinal("SC_SalesCode"));

                                SyncLinkedDictionary<object, object> syncLinkedDictionary = result.Get(salesCode);
                                if (syncLinkedDictionary == null)//first initialization of subelements
                                {
                                    syncLinkedDictionary = new SyncLinkedDictionary<object, object>();

                                    //constant data here
                                    syncLinkedDictionary.Add("SC_Model", reader.GetString(reader.GetOrdinal("SC_Model")));
                                    syncLinkedDictionary.Add("HIDEN_KEYS", new List<string> { "SC_Model" });

                                    result.Add(salesCode, syncLinkedDictionary);
                                }
                                //else
                                //{   
                                    //set dynamic data here
                                    DateTime dateKey = reader.GetDateTime(reader.GetOrdinal("OP_Date"));
                                    int quantity = reader.GetInt32(reader.GetOrdinal("OP_Quantity"));
                                    syncLinkedDictionary.Add(dateKey, quantity);
                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.StackTrace);
                throw ex;
            }
            return result;
        }
    }
}
