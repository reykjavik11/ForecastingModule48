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
    internal class ForecastRepositoryImpl : ForecastRepository, ISqlBaseOperations
    {
        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;
        public const string BASE_FLAG = "SC_BaseFlag";

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<ForecastRepositoryImpl> _instance = new Lazy<ForecastRepositoryImpl>(() => new ForecastRepositoryImpl());

        public static ForecastRepositoryImpl Instance => _instance.Value;

        private ForecastRepositoryImpl()
        {
        }

        public SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> retrieveForecast(string modelName)
        {
            SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> result = new SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //must be ordering - return structure is linked depend by ordering elements
                    using (var command = new SqlCommand($"select distinct IsNUll(sc.SC_SalesCode, '') as SC_SalesCode, IsNUll(sc.SC_ItemName, '') as SC_ItemName, IsNUll(sc.SC_FCPercent, 0) as SC_FCPercent, IsNUll(fr.FC_Quantity, 0) as FC_Quantity, IsNUll(sc.SC_Comments, '') as SC_Comments, IsNUll(sc.SC_Model, '') as SC_Model, IsNUll(sc.SC_BaseFlag, 0) as SC_BaseFlag, DATEADD(DAY,    -((DATEPART(WEEKDAY, EOMONTH(fr.FC_Date)) + 5) % 7),    EOMONTH(fr.FC_Date)) AS FC_Date from [WeilerForecasting].[dbo].[SalesCodes] sc left join [WeilerForecasting].[dbo].[OperationsSettings] os on os.OPS_Tab = sc.SC_OperationsTab left join [WeilerForecasting].[dbo].Forecast fr on fr.FC_Model = sc.SC_Model and fr.FC_SalesCode = sc.SC_SalesCode  and DATEFROMPARTS(YEAR(DATEADD(month, 1, fr.FC_Date)), MONTH(DATEADD(month, 1, fr.FC_Date)), 1) between DATEFROMPARTS(YEAR(DATEADD(day, os.OPS_NbrDays, GETDATE())), MONTH(DATEADD(day, os.OPS_NbrDays, GETDATE())), 1) and EOMONTH(DATEADD(month, os.OPS_NbrMonths, GETDATE())) where sc.SC_Model = '{modelName}' and  sc.SC_ActiveFlag = 1 order by SC_SalesCode, SC_BaseFlag", connection))
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
                                    syncLinkedDictionary.Add(OperationsPlanningRepositoryImpl.SC_MODEL, reader.GetString(reader.GetOrdinal("SC_Model")));
                                    syncLinkedDictionary.Add("SC_BaseFlag", reader.GetBoolean(reader.GetOrdinal(BASE_FLAG)));
                                    syncLinkedDictionary.Add("SC_FCPercent", reader.GetInt32(reader.GetOrdinal("SC_FCPercent")));
                                    syncLinkedDictionary.Add("SC_Comments", reader.GetString(reader.GetOrdinal("SC_Comments")));

                                    syncLinkedDictionary.Add("SC_ItemName", reader.GetString(reader.GetOrdinal("SC_ItemName")));
                                    syncLinkedDictionary.Add("HIDEN_KEYS", new List<string> { "SC_Model", "SC_BaseFlag", "SC_Comments", "SC_FCPercent", "SC_ItemName" });

                                    result.Add(salesCode, syncLinkedDictionary);
                                }
                                Nullable<DateTime> nullableDateTime = DataReaderExtensions.GetNullableValue<DateTime>(reader, "FC_Date");// reader.GetDateTime(reader.GetOrdinal("FC_Date"));
                                if(nullableDateTime.HasValue)
                                {
                                    int quantity = reader.GetInt32(reader.GetOrdinal("FC_Quantity"));
                                    //syncLinkedDictionary.Add(dateKey, quantity);
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
                log.LogWarning($"ForecastRepositoryImpl -> save: Operation model is null");
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
                    StringBuilder deleteQuery = SqlHelper.generateDeleteQuery(opModelList, "Forecast", "FC_Model");

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
                    transaction.Commit();
                    log.LogInfo($"Save ForecastRepositoryImpl - Deleted {deletedRows} rows. Inserted {insertedRows}");
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
                            insertCommand.Parameters.AddWithValue($"@FC_RecordID{index}", guid);
                            insertCommand.Parameters.AddWithValue($"@FC_SalesCode{index}", saleCode);
                            insertCommand.Parameters.AddWithValue($"@FC_Date{index}", key);

                            insertCommand.Parameters.AddWithValue($"@FC_Quantity{index}", count);
                            var model = valuesDictionaryBySaleCode.Get(OperationsPlanningRepositoryImpl.SC_MODEL);
                            insertCommand.Parameters.AddWithValue($"@FC_Model{index}", model);
                            index++;
                        }
                    }
                }
            }
        }

        protected string generateInsertQueries(SyncLinkedDictionary<string, SyncLinkedDictionary<object, object>> data, List<string> salesCodesKeys)
        {
            string insertQueries = "INSERT INTO [dbo].[Forecast] (FC_RecordID, FC_SalesCode, FC_Date, FC_Quantity, FC_Model) VALUES ";
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
                            parameters.Add($"(@FC_RecordID{index}, @FC_SalesCode{index}, @FC_Date{index}, @FC_Quantity{index}, @FC_Model{index})");
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
