﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using ForecastingModule.Helper;
using ForecastingModule.Util;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal class OperationsPlanningRepositoryImpl : OperationsPlanningRepository
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
                                    syncLinkedDictionary.Add(SC_MODEL, reader.GetString(reader.GetOrdinal("SC_Model")));
                                    syncLinkedDictionary.Add("HIDEN_KEYS", new List<string> { "SC_Model" });

                                    result.Add(salesCode, syncLinkedDictionary);
                                }
                                DateTime dateKey = reader.GetDateTime(reader.GetOrdinal("OP_Date"));
                                int quantity = reader.GetInt32(reader.GetOrdinal("OP_Quantity"));
                                syncLinkedDictionary.Add(dateKey, quantity);
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

        public int saveOperationsPlanning(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data)
        {
            int insertedRows = 0;
            if (data == null)
            {
                log.LogWarning($"OperationsPlanningRepositoryImpl -> saveOperationsPlanning: Operation model is null");
                return insertedRows;
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                List<string> salesCodesKeys = data.Keys.ToList();
                HashSet<string> opModelSet = getModelSet(data, salesCodesKeys);

                List<string> opModelList = new List<string>(opModelSet);
                try
                {
                    // Delete Command
                    StringBuilder deleteQuery = generateDeleteQuery(opModelList);

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

        private static void insertCommands(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys, SqlCommand insertCommand)
        {
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
                            insertCommand.Parameters.Clear();//TODO bug here, looks like commit last lane 

                            object count = valuesDictionaryBySaleCode.Get(key);
                            insertCommand.Parameters.AddWithValue("@OP_RecordID", Guid.NewGuid());
                            insertCommand.Parameters.AddWithValue("@OP_Base", saleCode);
                            insertCommand.Parameters.AddWithValue("@OP_Date", key);
                            insertCommand.Parameters.AddWithValue("@OP_ForecastDate", DateUtil.calculateForecastDay((DateTime)key));
                            insertCommand.Parameters.AddWithValue("@OP_Quantity", valuesDictionaryBySaleCode.Get(key));
                            insertCommand.Parameters.AddWithValue("@OP_Model", valuesDictionaryBySaleCode.Get(SC_MODEL));
                        }
                    }
                }
            }
        }

        private static string generateInsertQueries(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys)
        {
            string insertQueries = "INSERT INTO [dbo].[OperationsPlanning] ([OP_RecordID], [OP_Base], [OP_Date], [OP_ForecastDate], [OP_Quantity] ,[OP_Model]) VALUES ";
            List<string> parameters = new List<string>();
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
                            parameters.Add("(@OP_RecordID, @OP_Base, @OP_Date, @OP_ForecastDate, @OP_Quantity, @OP_Model)");
                        }
                    }
                }
            }
            insertQueries += string.Join(", ", parameters);
            return insertQueries;
        }

        private static HashSet<string> getModelSet(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys)
        {
            HashSet<String> opModelSet = new HashSet<string>();

            foreach (string saleCode in salesCodesKeys)
            {
                SyncLinkedDictionary<object, object> valuesDictionaryBySaleCode = data.Get(saleCode);
                if (valuesDictionaryBySaleCode != null)
                {
                    string scModel = (string)valuesDictionaryBySaleCode.Get(SC_MODEL);
                    if (scModel != null)
                    {
                        opModelSet.Add(scModel);
                    }
                }
            }

            return opModelSet;
        }

        private static StringBuilder generateDeleteQuery(List<string> opModelList)
        {
            StringBuilder deleteQuery = new StringBuilder($"DELETE FROM [dbo].[OperationsPlanning] WHERE [OP_Model] in (");
            for (int i = 0; i < opModelList.Count; i++)
            {
                deleteQuery.Append($"@DeleteId{i}");
                if (i < opModelList.Count - 1)
                {
                    deleteQuery.Append(", ");
                }
            }
            deleteQuery.Append(")");
            return deleteQuery;
        }
    }
}
