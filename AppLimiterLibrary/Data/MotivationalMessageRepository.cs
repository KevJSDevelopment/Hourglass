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
    public async Task<List<GoalStep>> GetGoalSteps(int goalMessageId)
    {
        var sql = @"SELECT StepId, StepText, StepOrder 
                   FROM GoalStep 
                   WHERE GoalMessageId = @GoalMessageId 
                   ORDER BY StepOrder";

        return await DatabaseManager.ExecuteQueryAsync(sql, reader =>
        {
            var steps = new List<GoalStep>();
            while (reader.Read())
            {
                steps.Add(new GoalStep
                {
                    Text = reader.GetString(reader.GetOrdinal("StepText")),
                    StepOrder = reader.GetInt32(reader.GetOrdinal("StepOrder"))
                });
            }
            return steps;
        },
        command =>
        {
            command.Parameters.AddWithValue("@GoalMessageId", goalMessageId);
        });
    }

    public async Task<int> AddGoalSteps(int goalMessageId, List<GoalStep> steps)
    {
        if (goalMessageId <= 0)
            return 0;

        var deleteSql = "DELETE FROM GoalStep WHERE GoalMessageId = @GoalMessageId";
        var insertSql = @"
        INSERT INTO GoalStep (GoalMessageId, StepText, StepOrder)
        VALUES (@GoalMessageId, @StepText, @StepOrder)";

        int insertedCount = 0;

        try
        {
            using (var connection = DatabaseManager.GetConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // First delete existing steps
                        using (var deleteCommand = connection.CreateCommand())
                        {
                            deleteCommand.Transaction = transaction;
                            deleteCommand.CommandText = deleteSql;
                            deleteCommand.Parameters.AddWithValue("@GoalMessageId", goalMessageId);
                            await deleteCommand.ExecuteNonQueryAsync();
                        }

                        // Then insert new steps if there are any
                        if (steps != null && steps.Any())
                        {
                            using (var insertCommand = connection.CreateCommand())
                            {
                                insertCommand.Transaction = transaction;
                                insertCommand.CommandText = insertSql;

                                // Create parameters once
                                var goalMessageIdParam = insertCommand.Parameters.Add("@GoalMessageId", System.Data.SqlDbType.Int);
                                var stepTextParam = insertCommand.Parameters.Add("@StepText", System.Data.SqlDbType.NVarChar);
                                var stepOrderParam = insertCommand.Parameters.Add("@StepOrder", System.Data.SqlDbType.Int);

                                for (int i = 0; i < steps.Count; i++)
                                {
                                    // Update parameter values
                                    goalMessageIdParam.Value = goalMessageId;
                                    stepTextParam.Value = steps[i].Text;
                                    stepOrderParam.Value = i + 1; // Use 1-based indexing for order

                                    insertedCount += await insertCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating goal steps", ex);
        }

        return insertedCount;
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