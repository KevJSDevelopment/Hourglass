namespace AppLimiterLibrary
{
    public class AppRepository
    {
        public async Task SaveLimits(ProcessInfo processInfo)
        {
            var sql = @"
                IF EXISTS (SELECT 1 FROM Apps WHERE Executable = @Executable)
                    UPDATE Apps 
                    SET Name = @Name, Ignore = @Ignore, WarningTime = @WarningTime, KillTime = @KillTime 
                    WHERE Executable = @Executable
                ELSE
                    INSERT INTO Apps (Name, Executable, Ignore, WarningTime, KillTime) 
                    VALUES (@Name, @Executable, @Ignore, @WarningTime, @KillTime)";

            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Name", processInfo.Name);
                command.Parameters.AddWithValue("@Executable", processInfo.Executable);
                command.Parameters.AddWithValue("@Ignore", processInfo.Ignore);
                command.Parameters.AddWithValue("@WarningTime", processInfo.WarningTime);
                command.Parameters.AddWithValue("@KillTime", processInfo.KillTime);
            });
        }

        public async Task<List<ProcessInfo>> LoadAllLimits()
        {
            var sql = "SELECT Id, Name, Executable, Ignore, WarningTime, KillTime FROM Apps";
            return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
            {
                var results = new List<ProcessInfo>();
                while (reader.Read())
                {
                    results.Add(new ProcessInfo
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Executable = reader.GetString(reader.GetOrdinal("Executable")),
                        Ignore = reader.GetBoolean(reader.GetOrdinal("Ignore")),
                        WarningTime = reader.GetString(reader.GetOrdinal("WarningTime")),
                        KillTime = reader.GetString(reader.GetOrdinal("KillTime"))
                    });
                }
                return results;
            });
        }

        public async Task UpdateIgnoreStatus(string executable, bool ignore)
        {
            var sql = "UPDATE Apps SET Ignore = @Ignore WHERE Executable = @Executable";
            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Ignore", ignore);
                command.Parameters.AddWithValue("@Executable", executable);
            });
        }

        public async Task DeleteApp(string executable)
        {
            var sql = "DELETE FROM Apps WHERE Executable = @Executable";
            await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
            {
                command.Parameters.AddWithValue("@Executable", executable);
            });
        }

        // Other app-related database methods can go here
    }
}