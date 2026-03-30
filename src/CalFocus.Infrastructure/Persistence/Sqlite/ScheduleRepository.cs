using Microsoft.Data.Sqlite;
using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;
using System.Text.Json;

namespace CalFocus.Infrastructure.Persistence.Sqlite;

public sealed class ScheduleRepository : IScheduleRepository
{
    private readonly IAppDataPathService _pathService;

    public ScheduleRepository(IAppDataPathService pathService)
    {
        _pathService = pathService;
    }

    public async Task<ScheduleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, StartAt, EndAt, RepeatRule, Category, ColorHex, Note FROM ScheduleItems WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToScheduleItem(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<ScheduleItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, StartAt, EndAt, RepeatRule, Category, ColorHex, Note FROM ScheduleItems ORDER BY StartAt";

        var items = new List<ScheduleItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToScheduleItem(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<ScheduleItem>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue);

        return await GetByDateRangeAsync(date, date, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleItem>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Title, StartAt, EndAt, RepeatRule, Category, ColorHex, Note 
            FROM ScheduleItems 
            WHERE date(StartAt) >= @startDate AND date(StartAt) <= @endDate
            ORDER BY StartAt";
        command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

        var items = new List<ScheduleItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapToScheduleItem(reader));
        }

        return items;
    }

    public async Task<Guid> AddAsync(ScheduleItem item, CancellationToken cancellationToken = default)
    {
        if (item.Id == Guid.Empty)
        {
            item.Id = Guid.NewGuid();
        }

        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ScheduleItems (Id, Title, StartAt, EndAt, RepeatRule, Category, ColorHex, Note)
            VALUES (@id, @title, @startAt, @endAt, @repeatRule, @category, @colorHex, @note)";
        
        command.Parameters.AddWithValue("@id", item.Id.ToString());
        command.Parameters.AddWithValue("@title", item.Title);
        command.Parameters.AddWithValue("@startAt", item.StartAt.ToString("O"));
        command.Parameters.AddWithValue("@endAt", item.EndAt.ToString("O"));
        command.Parameters.AddWithValue("@repeatRule", item.RepeatRule ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@colorHex", item.ColorHex ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@note", item.Note ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(ScheduleItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE ScheduleItems 
            SET Title = @title, StartAt = @startAt, EndAt = @endAt, RepeatRule = @repeatRule, 
                Category = @category, ColorHex = @colorHex, Note = @note
            WHERE Id = @id";
        
        command.Parameters.AddWithValue("@id", item.Id.ToString());
        command.Parameters.AddWithValue("@title", item.Title);
        command.Parameters.AddWithValue("@startAt", item.StartAt.ToString("O"));
        command.Parameters.AddWithValue("@endAt", item.EndAt.ToString("O"));
        command.Parameters.AddWithValue("@repeatRule", item.RepeatRule ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@colorHex", item.ColorHex ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@note", item.Note ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ScheduleItems WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ScheduleItem MapToScheduleItem(SqliteDataReader reader)
    {
        return new ScheduleItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            Title = reader.GetString(1),
            StartAt = DateTimeOffset.Parse(reader.GetString(2)),
            EndAt = DateTimeOffset.Parse(reader.GetString(3)),
            RepeatRule = reader.IsDBNull(4) ? null : reader.GetString(4),
            Category = reader.IsDBNull(5) ? null : reader.GetString(5),
            ColorHex = reader.IsDBNull(6) ? null : reader.GetString(6),
            Note = reader.IsDBNull(7) ? null : reader.GetString(7)
        };
    }
}
