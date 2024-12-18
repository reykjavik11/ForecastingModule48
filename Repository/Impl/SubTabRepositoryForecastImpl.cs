using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal sealed class SubTabRepositoryForecastImpl : SubTabForecastRepository
    {
        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<SubTabRepositoryForecastImpl> _instance = new Lazy<SubTabRepositoryForecastImpl>(() => new SubTabRepositoryForecastImpl());

        public static SubTabRepositoryForecastImpl Instance => _instance.Value;

        private SubTabRepositoryForecastImpl()
        {
        }

        public List<string> getActiveSubTabs(string parentTab)
        {
            List<string> subTabs = new List<string>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand($"select st.SUB_SubTabName from [WeilerForecasting].[dbo].SubTabs st join ParentTabs pt on pt.TAB_TabName = st.SUB_ParentTab where st.SUB_ParentTab='{parentTab}' and st.SUB_ActiveFlag=1  order by st.SUB_DisplayOrder", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tabName = reader.GetString(reader.GetOrdinal("SUB_SubTabName"));
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
