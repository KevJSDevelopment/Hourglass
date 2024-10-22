using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Data;
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

    public async Task<MotivationalMessage> AddAudioMessage(string computerId, Stream audioStream, string fileName, string fileExtension)
    {
        string[] fileInfo = await _audioFileManager.SaveAudioFileAsync(computerId, audioStream, fileName, fileExtension);
        string filePath = fileInfo[0];
        string newFileName = fileInfo[1];
        var sql = @"
        INSERT INTO MotivationalMessage (TypeId, TypeDescription, ComputerId, FilePath, FileName)
        VALUES (@TypeId, @TypeDescription, @ComputerId, @FilePath, @FileName);
        
        SELECT * FROM MotivationalMessage 
        WHERE Id = SCOPE_IDENTITY();";

        #pragma warning disable CS8603 // Possible null reference return.
        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            if (reader.Read())
            {
                return new MotivationalMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    TypeId = Convert.ToInt32(reader["TypeId"]),
                    TypeDescription = reader["TypeDescription"].ToString(),
                    ComputerId = reader["ComputerId"].ToString(),
                    FilePath = reader["FilePath"].ToString(),
                    FileName = reader["FileName"].ToString()
                };
            }
            return null;
        }, command =>
        {
            command.Parameters.AddWithValue("@TypeId", 2);
            command.Parameters.AddWithValue("@TypeDescription", "Audio");
            command.Parameters.AddWithValue("@ComputerId", computerId);
            command.Parameters.AddWithValue("@FilePath", filePath);
            command.Parameters.AddWithValue("@FileName", newFileName);
        });
        #pragma warning restore CS8603 // Possible null reference return.
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

    public async Task UpdateMessage(MotivationalMessage message)
    {
        var sql = @"
        UPDATE MotivationalMessage 
        SET Message = @Message
        WHERE Id = @Id AND ComputerId = @ComputerId";

        await DatabaseManager.ExecuteNonQueryAsync(sql, command =>
        {
            command.Parameters.AddWithValue("@Id", message.Id);
            command.Parameters.AddWithValue("@ComputerId", message.ComputerId);
            command.Parameters.AddWithValue("@Message", message.Message);
        });
    }
    public async Task<bool> DeleteMessage(int messageId)
    {
        var selectSql = "SELECT FilePath FROM MotivationalMessage WHERE Id = @MessageId";

        try
        {
            string? filePath = await DatabaseManager.ExecuteQueryAsync(selectSql, reader =>
            {
                while (reader.Read())
                {
                    var path = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? null : reader.GetString(reader.GetOrdinal("FilePath"));

                    return path;
                }

                return null;
            }, command =>
            {
                command.Parameters.AddWithValue("@MessageId", messageId);
            });

            if (filePath != null) _audioFileManager.DeleteAudioFile(filePath);

            var deleteSql = "DELETE FROM MotivationalMessage WHERE Id = @MessageId";
            await DatabaseManager.ExecuteNonQueryAsync(deleteSql, command =>
            {
                command.Parameters.AddWithValue("@MessageId", messageId);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
        
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
                    FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? null : reader.GetString(reader.GetOrdinal("FilePath")),
                    FileName = reader.IsDBNull(reader.GetOrdinal("FileName")) ? null : reader.GetString(reader.GetOrdinal("FileName"))
                });
            }
            return messages;
        }, command =>
        {
            command.Parameters.AddWithValue("@ComputerId", computerId);
        });
    }
}