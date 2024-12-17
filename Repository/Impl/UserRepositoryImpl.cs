using System;
using System.Data.SqlClient;
using ForecastingModule.Model;
using ForecastingModule.Helper;
using ForecastingModule.Utilities;

namespace ForecastingModule.Repository.Impl
{
    internal sealed class UserRepositoryImpl : UserRepository
    {
        private readonly string connectionString = (string) ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private readonly DatabaseHelper db = DatabaseHelper.Instance;
        private static readonly Lazy<UserRepositoryImpl> _instance = new Lazy<UserRepositoryImpl>(() => new UserRepositoryImpl());

        public static UserRepositoryImpl Instance => _instance.Value;

        private UserRepositoryImpl()
        {

        }
        public Optional<UserDto> findUserByName(string userName, bool active = true)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand($"SELECT USR_UserName, USR_Access_OperationsPlanning, USR_Access_Forecast, USR_Access_OperationsSettings, USR_Access_ForecastSettings, USR_Access_Manage, USR_ActiveFlag FROM WeilerForecasting.dbo.Users where USR_UserName='{userName}' and USR_ActiveFlag={Convert.ToInt32(active)}", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new UserDto
                                {
                                    userName = reader.GetString(reader.GetOrdinal("USR_UserName")),
                                    accessOperationsPlanning = reader.GetBoolean(reader.GetOrdinal("USR_Access_OperationsPlanning")),
                                    accessForecast = reader.GetBoolean(reader.GetOrdinal("USR_Access_Forecast")),
                                    accessOperationsSettings = reader.GetBoolean(reader.GetOrdinal("USR_Access_OperationsSettings")),
                                    accessForecastSettings = reader.GetBoolean(reader.GetOrdinal("USR_Access_ForecastSettings")),
                                    accessManage = reader.GetBoolean(reader.GetOrdinal("USR_Access_Manage")),
                                    activeFlag = reader.GetBoolean(reader.GetOrdinal("USR_ActiveFlag"))
                                };
                                return Optional<UserDto>.Of(user);
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

            Optional<UserDto> optionalUser = Optional<UserDto>.Empty();
            return optionalUser;
        }
    }
}
