using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastingModule.Util;
using MySql.Data.MySqlClient;
using Serilog.Core;
using ForecastingModule.exception;

namespace ForecastingModule.Utilities
{
    public sealed class DatabaseHelper
    {
        private const int ACCEPTABLE_TIME = 1000;
        private static readonly Lazy<DatabaseHelper> _instance = new Lazy<DatabaseHelper>(() => new DatabaseHelper());

        private readonly string ConnectionString = (string)ConfigFileManager.Instance.Read(ConfigFileManager.KEY_HOST);
        private readonly Logger log = Logger.Instance;

        private DatabaseHelper() { }

        public static DatabaseHelper Instance => _instance.Value;

        /// <summary>
        /// Executes a SQL query and returns the results as a DataTable.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>A DataTable containing the query results.</returns>
        public DataTable ExecuteQuery(string query)
        {
            var dataTable = new DataTable();
            try
            {
                var startDate = DateTime.Now;
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            log.LogInfo($"Row: {reader["USR_UserName"]}, {reader["USR_Access_OperationsPlanning"]}");
                        }
                        //dataTable.Load(reader);
                    }
                }

                var endDate = DateTime.Now;
                LogWhenExceed(query, startDate, endDate);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.StackTrace}\n Query: {query}");
                throw new DBException(ex.Message);
            }
            return dataTable;
        }

        /*
         * TODO - Test connection to MySql
        */
        public DataTable ExecuteMySqlQuery(string query)
        {
            var dataTable = new DataTable();
            try
            {
                var startDate = DateTime.Now;
                log.LogInfo("Connected to MySQL database!");

                using (var connection = new MySqlConnection(ConnectionString))
                {
                    try
                    {
                        connection.Open();

                        using (var command = new MySqlCommand(query, connection))
                        using (var reader = command.ExecuteReader())
                        {
                            //dataTable.Load(reader);
                            while (reader.Read())
                            {
                                log.LogInfo($"Row: {reader["user_name"]}, {reader["device_id"]}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError($"Error: {ex.Message}");
                    }
                    finally
                    {
                        connection.Close();
                        log.LogInfo("MySqlConnection is closed.");
                    }
                }

                var endDate = DateTime.Now;
                LogWhenExceed(query, startDate, endDate);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.StackTrace}");
                throw new DBException(ex.Message);
            }
            return dataTable;
        }

        private void LogWhenExceed(string query, DateTime startDate, DateTime endDate)
        {
            var duration = endDate.Millisecond - startDate.Millisecond;
            if (duration > ACCEPTABLE_TIME)
            {
                log.LogWarning($"Running of sql: {query} exceed {ACCEPTABLE_TIME}ms - [duration is {duration}ms]");
            }
        }

        /// <summary>
        /// Executes a non-query SQL command (e.g., INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <returns>The number of rows affected.</returns>
        public int ExecuteNonQuery(string query)
        {
            try
            {
                var startDate = DateTime.Now;

                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(query, connection))
                    {
                        int count = command.ExecuteNonQuery();

                        var endDate = DateTime.Now;
                        LogWhenExceed(query, startDate, endDate);

                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.StackTrace}\n Query: {query}");
                throw new DBException(ex.Message);
            }
        }
    }
}
