using System.Data;
using DauChanDiSan.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DauChanDiSan.Api.Shared;

public sealed class SqlProcedures(DauChanDiSanDbContext db)
{
    public Task<List<Dictionary<string, object?>>> RunAsync(
        string procedure, CancellationToken ct, params (string Name, object? Value)[] arguments)
        => ReadAsync(procedure, CommandType.StoredProcedure, ct, arguments);

    public Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql, CancellationToken ct, params (string Name, object? Value)[] arguments)
        => ReadAsync(sql, CommandType.Text, ct, arguments);

    private async Task<List<Dictionary<string, object?>>> ReadAsync(
        string text, CommandType type, CancellationToken ct, (string Name, object? Value)[] arguments)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = text;
            command.CommandType = type;
            command.Transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (name, value) in arguments)
                command.Parameters.AddWithValue("@" + name, value ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(ct);
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    var value = reader.GetValue(index);
                    row[reader.GetName(index)] = value switch
                    {
                        DBNull => null,
                        DateTime date when reader.GetDataTypeName(index) == "date" => DateOnly.FromDateTime(date),
                        DateTime timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        _ => value
                    };
                }
                rows.Add(row);
            }
            return rows;
        }
        finally
        {
            if (openedHere)
                await db.Database.CloseConnectionAsync();
        }
    }
}
