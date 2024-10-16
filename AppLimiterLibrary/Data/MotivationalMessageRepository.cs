using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

public class MotivationalMessageRepository
{
    private readonly LocalAudioFileManager _audioFileManager;

    public MotivationalMessageRepository()
    {
        _audioFileManager = new LocalAudioFileManager();
    }

    public async Task<int> AddMessage(string computerId, string message)
    {
        var sql = @"
                INSERT INTO MotivationalMessage (TypeId, TypeDescription, ComputerId, Message)
                VALUES (@TypeId, @TypeDescription, @ComputerId, @Message);
                SELECT SCOPE_IDENTITY();
                ";

        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            if (reader.Read())
            {
                return Convert.ToInt32(reader[0]);
            }

            return -1;
        }, command =>
        {
            command.Parameters.AddWithValue("@TypeId", 1); // Assuming 1 is Message, 2 is the TypeId for audio messages, and 3 is for goals.
            command.Parameters.AddWithValue("@TypeDescription", "Message");
            command.Parameters.AddWithValue("@ComputerId", computerId);
            command.Parameters.AddWithValue("@Message", message);
        });
    }
    
    public async Task<int> AddAudioMessage(string computerId, Stream audioStream, string fileExtension)
    {
        string filePath = await _audioFileManager.SaveAudioFileAsync(computerId, audioStream, fileExtension);

        var sql = @"
            INSERT INTO MotivationalMessage (TypeId, TypeDescription, ComputerId, FilePath)
            VALUES (@TypeId, @TypeDescription, @ComputerId, @FilePath);
            SELECT SCOPE_IDENTITY();";

        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            if (reader.Read())
            {
                return Convert.ToInt32(reader[0]);
            }
            return -1;
        }, command =>
        {
            command.Parameters.AddWithValue("@TypeId", 2); // Assuming 2 is the TypeId for audio messages
            command.Parameters.AddWithValue("@TypeDescription", "Audio");
            command.Parameters.AddWithValue("@ComputerId", computerId);
            command.Parameters.AddWithValue("@FilePath", filePath);
        });
    }
    
    public async Task<int> AddGoalMessage(string computerId, string goal)
    {
        var sql = @"
                INSERT INTO MotivationalMessage (TypeId, TypeDescription, ComputerId, Message)
                VALUES (@TypeId, @TypeDescription, @ComputerId, @GoalMessage);
                SELECT SCOPE_IDENTITY();
                ";

        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            if (reader.Read())
            {
                return Convert.ToInt32(reader[0]);
            }

            return -1;
        }, command =>
        {
            command.Parameters.AddWithValue("@TypeId", 3); // Assuming 1 is Message, 2 is the TypeId for audio messages, and 3 is for goals.
            command.Parameters.AddWithValue("@TypeDescription", "Goal");
            command.Parameters.AddWithValue("@ComputerId", computerId);
            command.Parameters.AddWithValue("@GoalMessage", goal);
        });
    }

    public async Task<bool> DeleteMessage(int messageId)
    {
        var selectSql = "SELECT FilePath FROM MotivationalMessage WHERE Id = @MessageId";

        string? filePath = await DatabaseManager.ExecuteQueryAsync(selectSql, reader =>
        {
            return reader.Read() ? reader.GetString(0) : null;
        }, command =>
        {
            command.Parameters.AddWithValue("@MessageId", messageId);
        });

        if(filePath != null) _audioFileManager.DeleteAudioFile(filePath);

        var deleteSql = "DELETE FROM MotivationalMessage WHERE Id = @MessageId";
        await DatabaseManager.ExecuteNonQueryAsync(deleteSql, command =>
        {
            command.Parameters.AddWithValue("@MessageId", messageId);
        });
            
        return true;
    }
    public async Task<List<MotivationalMessage>> GetMessagesForComputer(string computerId)
    {
        var sql = "SELECT * FROM MotivationalMessage WHERE ComputerId = @ComputerId";
        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            var messages = new List<MotivationalMessage>();
            while (reader.Read())
            {
                messages.Add(new MotivationalMessage
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    TypeId = reader.GetInt32(reader.GetOrdinal("TypeId")),
                    TypeDescription = reader.GetString(reader.GetOrdinal("TypeDescription")),
                    ComputerId = reader.GetString(reader.GetOrdinal("ComputerId")),
                    Message = reader.IsDBNull(reader.GetOrdinal("Message")) ? null : reader.GetString(reader.GetOrdinal("Message")),
                    FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? null : reader.GetString(reader.GetOrdinal("FilePath"))
                });
            }
            return messages;
        }, command =>
        {
            command.Parameters.AddWithValue("@ComputerId", computerId);
        });
    }
}