using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal sealed class SubTabRepositoryOperationsImpl : SubTabRepository
    {
        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<SubTabRepositoryOperationsImpl> _instance = new Lazy<SubTabRepositoryOperationsImpl>(() => new SubTabRepositoryOperationsImpl());

        public static SubTabRepositoryOperationsImpl Instance => _instance.Value;

        private SubTabRepositoryOperationsImpl()
        {
        }

        public List<string> getActiveSubTabs()
        {
            List<string> subTabs = new List<string>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //using (var command = new SqlCommand("select distinct sc.SC_OperationsTab, t1.TAB_DisplayOrder  from [WeilerForecasting].[dbo].[SalesCodes] as sc  join (selecT pt.TAB_TabName, pt.TAB_DisplayOrder  from [WeilerForecasting].[dbo].ParentTabs as pt) as t1 on t1.TAB_TabName = sc.SC_ForecastTab  and sc.SC_ActiveFlag =1   and sc.SC_BaseFlag = 1  order by t1.TAB_DisplayOrder", connection))
                    using (var command = new SqlCommand("select distinct sc.SC_OperationsTab, t1.TAB_DisplayOrder  from [WeilerForecasting].[dbo].[SalesCodes] as sc join (selecT pt.TAB_TabName, pt.TAB_DisplayOrder  from [WeilerForecasting].[dbo].ParentTabs as pt) as t1  on t1.TAB_TabName = sc.SC_ForecastTab and sc.SC_ActiveFlag =1  and sc.SC_BaseFlag = 1  join [WeilerForecasting].[dbo].OperationsSettings os on os.OPS_Tab = sc.SC_OperationsTab and os.OPS_ActiveFlag =1  order by t1.TAB_DisplayOrder", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tabName = reader.GetString(reader.GetOrdinal("SC_OperationsTab"));
                                subTabs.Add(tabName);
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

            return subTabs;
        }
    }
}
