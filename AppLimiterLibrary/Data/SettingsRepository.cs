using HourglassLibrary.Dtos;
using HourglassLibrary.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HourglassLibrary.Data
{
    public class SettingsRepository
    {
        private string _computerId;
        public SettingsRepository(string computerId) 
        {
            _computerId = computerId;
        }

        public async Task<int> GetMessageLimit()
        {
            var sql = "SELECT MessageLimit FROM Settings WHERE ComputerId = @ComputerId";

            return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
            {
                if (reader.Read())
                {
                    return Convert.ToInt32(reader[0]);
                }

                return -1;
            }, command =>
            {
                command.Parameters.AddWithValue("@ComputerId", _computerId);
            });
        }

        public async Task SaveMessageLimit(int newLimit)
        {
            var sql = @"
                        IF NOT EXISTS (SELECT 1 FROM Settings WHERE ComputerId = @ComputerId)
                                INSERT INTO Settings (ComputerId, MessageLimit)
                                VALUES (@ComputerId, @NewLimit);

                        ELSE UPDATE Settings SET MessageLimit = @NewLimit WHERE ComputerId = @ComputerId;
                    ";

            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@ComputerId", _computerId);
                command.Parameters.AddWithValue("@NewLimit", newLimit);
            });
        }

    }
}
