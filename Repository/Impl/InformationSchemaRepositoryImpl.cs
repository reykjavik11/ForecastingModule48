using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal class InformationSchemaRepositoryImpl : InformationSchemaRepository
    {
        private readonly string connectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private static readonly Lazy<InformationSchemaRepositoryImpl> _instance = new Lazy<InformationSchemaRepositoryImpl>(() => new InformationSchemaRepositoryImpl());

        public static InformationSchemaRepositoryImpl Instance => _instance.Value;
        private InformationSchemaRepositoryImpl()
        {
        }

        public List<Tuple<string, string>> getColumsMetaByTable(string tableName)
        {
            List<Tuple<string, string>> columnsMeta = new List<Tuple<string, string>>();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand($"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader.GetString(reader.GetOrdinal("COLUMN_NAME"));
                                string dataType = reader.GetString(reader.GetOrdinal("DATA_TYPE"));

                                columnsMeta.Add(Tuple.Create(columnName, dataType));
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
            return columnsMeta;
        }
    }
}
