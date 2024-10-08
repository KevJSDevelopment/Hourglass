using System;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient for .NET Core 3.x or later
using Microsoft.Extensions.Configuration;

namespace AppLimiterLibrary
{
    public class DatabaseManager
    {
        private static string _connectionString;

        public static void Initialize(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string is not configured.");
            }
        }

        public static SqlConnection GetConnection()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DatabaseManager is not initialized. Call Initialize method first.");
            }
            return new SqlConnection(_connectionString);
        }

        public static async Task<T> ExecuteQueryAsync<T>(string query, Func<SqlDataReader, T> map, Action<SqlCommand> setParameters = null)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    setParameters?.Invoke(command);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        return map(reader);
                    }
                }
            }
        }

        public static async Task ExecuteNonQueryAsync(string query, Action<SqlCommand> setParameters = null)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    setParameters?.Invoke(command);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}