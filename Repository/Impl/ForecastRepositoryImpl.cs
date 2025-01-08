using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal class ForecastRepositoryImpl : ForecastRepository
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
    }
}
