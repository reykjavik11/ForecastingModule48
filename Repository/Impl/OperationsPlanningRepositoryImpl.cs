using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using ForecastingModule.Helper;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal class OperationsPlanningRepositoryImpl : OperationsPlanningRepository, ISqlBaseOperations/*, ISqlAddiotionalOperations*/
    {
        public const string SC_MODEL = "SC_Model";
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

                    using (var command = new SqlCommand($"select IsNull(op.OPS_Tab, '') as OPS_Tab, IsNull(op.OPS_NbrDays, 0) as OPS_NbrDays, IsNull(op.OPS_NbrMonths, 0) as OPS_NbrMonths, IsNull(op.OPS_Comments, '') as OPS_Comments  FROM [WeilerForecasting].[dbo].[OperationsSettings] op  join (SELECT distinct [SC_OperationsTab], [SC_ForecastTab]  FROM [WeilerForecasting].[dbo].[SalesCodes]) sc on sc.SC_OperationsTab=op.OPS_Tab  where sc.SC_ForecastTab ='{equipmentName}' and op.OPS_ActiveFlag=1", connection))
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
                    using (var command = new SqlCommand($"select IsNull(sc.SC_SalesCode, '') as SC_SalesCode, op.OP_Date, IsNull(op.OP_Quantity, 0) as OP_Quantity, IsNull(sc.SC_Model, '') as SC_Model from (select SC_SalesCode, SC_Model, SC_OperationsTab from [WeilerForecasting].[dbo].[SalesCodes] where SC_BaseFlag=1) sc left join [WeilerForecasting].[dbo].[OperationsPlanning] op on op.OP_Model = sc.SC_Model left join [WeilerForecasting].[dbo].[OperationsSettings] os on os.OPS_Tab = sc.SC_OperationsTab where sc.SC_OperationsTab='{equipmentName}' and op.OP_Date between DATEFROMPARTS(YEAR(DATEADD(day, os.OPS_NbrDays, GETDATE())), MONTH(DATEADD(day, os.OPS_NbrDays, GETDATE())), 1) and EOMONTH(DATEADD(month, os.OPS_NbrMonths, GETDATE())) order by SC_SalesCode", connection))
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
                                    syncLinkedDictionary.Add(SC_MODEL, reader.GetString(reader.GetOrdinal("SC_Model")));
                                    syncLinkedDictionary.Add("HIDEN_KEYS", new List<string> { "SC_Model" });

                                    result.Add(salesCode, syncLinkedDictionary);
                                }

                                Nullable<DateTime> nullableDateTime = DataReaderExtensions.GetNullableValue<DateTime>(reader, "OP_Date");
                                if (nullableDateTime.HasValue)
                                {
                                    int quantity = reader.GetInt32(reader.GetOrdinal("OP_Quantity"));
                                    syncLinkedDictionary.Add(nullableDateTime.Value, quantity);
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
            return result;
        }

        public int save(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data)
        {
            int insertedRows = 0;
            if (data == null)
            {
                log.LogWarning($"OperationsPlanningRepositoryImpl -> save: Operation model is null");
                return insertedRows;
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                List<string> salesCodesKeys = data.Keys.ToList();
                HashSet<string> opModelSet = SqlHelper.getModelSet(data, salesCodesKeys);

                List<string> opModelList = new List<string>(opModelSet);
                try
                {
                    // Delete Command
                    StringBuilder deleteQuery = SqlHelper.generateDeleteQuery(opModelList, "OperationsPlanning", "OP_Model");

                    int deletedRows = 0;
                    using (SqlCommand deleteCommand = new SqlCommand(deleteQuery.ToString(), connection, transaction))
                    {
                        // Add parameters for each ID
                        for (int i = 0; i < opModelList.Count; i++)
                        {
                            deleteCommand.Parameters.AddWithValue($"@DeleteId{i}", opModelList[i]);
                        }

                        deletedRows = deleteCommand.ExecuteNonQuery();
                    }
                    //End Delete Command
                    string insertQueries = generateInsertQueries(data, salesCodesKeys);

                    //Insert Command
                    using (SqlCommand insertCommand = new SqlCommand(insertQueries, connection, transaction))
                    {
                        insertCommands(data, salesCodesKeys, insertCommand);
                        insertedRows = insertCommand.ExecuteNonQuery();
                    }
                    transaction.Commit();//TODO uncoment layter
                    log.LogInfo($"Save OperationsPlanningRepositoryImpl - Deleted {deletedRows} rows. Inserted {insertedRows}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    log.LogError(ex.StackTrace);
                    throw ex;
                }
            }
            return insertedRows;
        }

        protected void insertCommands(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys, SqlCommand insertCommand)
        {
            int index = 0;
            foreach (string saleCode in salesCodesKeys)
            {
                SyncLinkedDictionary<object, object> valuesDictionaryBySaleCode = data.Get(saleCode);
                if (valuesDictionaryBySaleCode != null)
                {
                    List<object> valuesKeys = valuesDictionaryBySaleCode.Keys.ToList();
                    foreach (var key in valuesKeys)
                    {
                        if (key is DateTime)
                        {
                            object count = valuesDictionaryBySaleCode.Get(key);
                            var guid = Guid.NewGuid();
                            insertCommand.Parameters.AddWithValue($"@OP_RecordID{index}", guid);
                            insertCommand.Parameters.AddWithValue($"@OP_Base{index}", saleCode);
                            insertCommand.Parameters.AddWithValue($"@OP_Date{index}", key);

                            DateTime forecastDate = DateUtil.toForecastDay((DateTime)key);
                            insertCommand.Parameters.AddWithValue($"@OP_ForecastDate{index}", forecastDate);
                            insertCommand.Parameters.AddWithValue($"@OP_Quantity{index}", count);
                            var model = valuesDictionaryBySaleCode.Get(SC_MODEL);
                            insertCommand.Parameters.AddWithValue($"@OP_Model{index}", model);
                            index++;
                        }
                    }
                }
            }
        }

        protected string generateInsertQueries(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys)
        {
            string insertQueries = "INSERT INTO [dbo].[OperationsPlanning] (OP_RecordID, OP_Base, OP_Date, OP_ForecastDate, OP_Quantity, OP_Model) VALUES ";
            List<string> parameters = new List<string>();
            int index = 0;
            foreach (string saleCode in salesCodesKeys)
            {
                SyncLinkedDictionary<object, object> valuesDictionaryBySaleCode = data.Get(saleCode);
                if (valuesDictionaryBySaleCode != null)
                {
                    List<object> valuesKeys = valuesDictionaryBySaleCode.Keys.ToList();
                    foreach (var key in valuesKeys)
                    {
                        if (key is DateTime)
                        {
                            parameters.Add($"(@OP_RecordID{index}, @OP_Base{index}, @OP_Date{index}, @OP_ForecastDate{index}, @OP_Quantity{index}, @OP_Model{index})");
                            index++;
                        }
                    }
                }
            }
            insertQueries += string.Join(", ", parameters);
            return insertQueries;
        }
    }
}
