using Microsoft.Data.Sqlite;
using CalFocus.Core.Abstractions.Services;

namespace CalFocus.Infrastructure.Persistence.Sqlite;

public sealed class SqliteDatabaseInitializer : IDatabaseInitializer
{
    private readonly IAppDataPathService _pathService;

    public SqliteDatabaseInitializer(IAppDataPathService pathService)
    {
        _pathService = pathService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_pathService.AppDataRoot);

        await using var connection = new SqliteConnection($"Data Source={_pathService.DatabaseFilePath}");
        await connection.OpenAsync(cancellationToken);

        var commands = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS ScheduleItems (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                StartAt TEXT NOT NULL,
                EndAt TEXT NOT NULL,
                RepeatRule TEXT NULL,
                Category TEXT NULL,
                ColorHex TEXT NULL,
                Note TEXT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS ReminderItems (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                TriggerAt TEXT NOT NULL,
                RepeatRule TEXT NULL,
                SoundEnabled INTEGER NOT NULL,
                SnoozeMinutes INTEGER NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS PlanItems (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                PeriodType INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                RelatedScheduleIdsJson TEXT NOT NULL
            );
            """
        };

        foreach (var sql in commands)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
