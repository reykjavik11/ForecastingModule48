using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal sealed class TabRepositoryImpl : TabRepository
    {

        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<TabRepositoryImpl> _instance = new Lazy<TabRepositoryImpl>(() => new TabRepositoryImpl());

        public static TabRepositoryImpl Instance => _instance.Value;

        private TabRepositoryImpl()
        {
        }

        public List<string> getActiveTabs()
        {
            {
                List<string> tabs = new List<string>();
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        using (var command = new SqlCommand("SELECT [TAB_TabName] FROM [WeilerForecasting].[dbo].[ParentTabs] where [TAB_ActiveFlag] = 1 order by [TAB_DisplayOrder]", connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string tabName = reader.GetString(reader.GetOrdinal("TAB_TabName"));
                                    tabs.Add(tabName);
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

                return tabs;
            }
        }
    }
}
