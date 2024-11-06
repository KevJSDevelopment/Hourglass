using AppLimiterLibrary.Dtos;

namespace AppLimiterLibrary.Data
{
    public class AppRepository
    {
        public async Task SaveLimits(ProcessInfo processInfo)
        {
            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM UserComputers WHERE ComputerId = @ComputerId)
                    INSERT INTO UserComputers (ComputerId, ComputerName)
                    VALUES (@ComputerId, NULL);

                IF EXISTS (SELECT 1 FROM Apps WHERE Executable = @Executable AND ComputerId = @ComputerId)
                    UPDATE Apps 
                    SET Name = @Name, Ignore = @Ignore, WarningTime = @WarningTime, KillTime = @KillTime 
                    WHERE Executable = @Executable AND ComputerId = @ComputerId
                ELSE
                    INSERT INTO Apps (Name, Executable, Ignore, WarningTime, KillTime, ComputerId) 
                    VALUES (@Name, @Executable, @Ignore, @WarningTime, @KillTime, @ComputerId)";

            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Name", processInfo.Name);
                command.Parameters.AddWithValue("@Executable", processInfo.Executable);
                command.Parameters.AddWithValue("@Ignore", processInfo.Ignore);
                command.Parameters.AddWithValue("@WarningTime", processInfo.WarningTime);
                command.Parameters.AddWithValue("@KillTime", processInfo.KillTime);
                command.Parameters.AddWithValue("@ComputerId", ComputerIdentifier.GetUniqueIdentifier());
            });
        }

        public async Task<List<ProcessInfo>> LoadAllLimits(string computerId)
        {
            var sql = "SELECT * FROM Apps WHERE ComputerId = @ComputerId";
            return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
            {
                var results = new List<ProcessInfo>();
                while (reader.Read())
                {
                    results.Add(new ProcessInfo
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        ComputerId = reader.GetString(reader.GetOrdinal("ComputerId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Executable = reader.GetString(reader.GetOrdinal("Executable")),
                        Ignore = reader.GetBoolean(reader.GetOrdinal("Ignore")),
                        WarningTime = reader.GetString(reader.GetOrdinal("WarningTime")),
                        KillTime = reader.GetString(reader.GetOrdinal("KillTime"))
                    });
                }
                return results;
            },
            command => command.Parameters.AddWithValue("@ComputerId", computerId));
        }

        public async Task UpdateIgnoreStatus(string processName, bool ignore)
        {
            var sql = "UPDATE Apps SET Ignore = @Ignore WHERE Name = @Name AND ComputerId = @ComputerId";
            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Ignore", ignore);
                command.Parameters.AddWithValue("@Name", processName);
                command.Parameters.AddWithValue("@ComputerId", ComputerIdentifier.GetUniqueIdentifier());
            });
        }

        public async Task<bool> CheckIgnoreStatus(string executable)
        {
            var sql = "SELECT CASE WHEN Ignore = 1 THEN 1 ELSE 0 END FROM Apps WHERE Executable = @Executable AND ComputerId = @ComputerId";

            return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
            {
                if (reader.Read())
                {
                    return reader.GetInt32(0) == 1;
                }
                return false; // Return false if no matching record is found
            },
            command =>
            {
                command.Parameters.AddWithValue("@Executable", executable);
                command.Parameters.AddWithValue("@ComputerId", ComputerIdentifier.GetUniqueIdentifier());
            });
        }

        public async Task DeleteApp(string executable)
        {
            var sql = "DELETE FROM Apps WHERE Executable = @Executable AND ComputerId = @ComputerId";
            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Executable", executable);
                command.Parameters.AddWithValue("@ComputerId", ComputerIdentifier.GetUniqueIdentifier());
            });
        }

        // Other app-related database methods can go here
    }
}