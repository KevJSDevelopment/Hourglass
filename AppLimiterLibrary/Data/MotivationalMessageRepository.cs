using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

public class MotivationalMessageRepository
{
    private readonly LocalAudioFileManager _fileManager;

    public MotivationalMessageRepository(LocalAudioFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public async Task<int> AddAudioMessage(string computerId, Stream audioStream, string fileExtension)
    {
        string filePath = await _fileManager.SaveAudioFileAsync(computerId, audioStream, fileExtension);

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

    public async Task<bool> DeleteAudioMessage(int messageId)
    {
        var selectSql = "SELECT FilePath FROM MotivationalMessage WHERE Id = @MessageId";
        string filePath = await DatabaseManager.ExecuteQueryAsync(selectSql, reader =>
        {
            return reader.Read() ? reader.GetString(0) : null;
        }, command =>
        {
            command.Parameters.AddWithValue("@MessageId", messageId);
        });

        if (filePath != null && _fileManager.DeleteAudioFile(filePath))
        {
            var deleteSql = "DELETE FROM MotivationalMessage WHERE Id = @MessageId";
            await DatabaseManager.ExecuteNonQueryAsync(deleteSql, command =>
            {
                command.Parameters.AddWithValue("@MessageId", messageId);
            });
            return true;
        }

        return false;
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