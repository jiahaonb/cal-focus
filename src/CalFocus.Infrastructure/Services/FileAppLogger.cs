using CalFocus.Core.Abstractions.Services;

namespace CalFocus.Infrastructure.Services;

public sealed class FileAppLogger : IAppLogger
{
    private readonly IAppDataPathService _pathService;

    public FileAppLogger(IAppDataPathService pathService)
    {
        _pathService = pathService;
    }

    public void LogInfo(string message)
    {
        WriteLine("INFO", message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message} | {exception}";
        WriteLine("ERROR", details);
    }

    private void WriteLine(string level, string message)
    {
        Directory.CreateDirectory(_pathService.AppDataRoot);

        var logPath = Path.Combine(_pathService.AppDataRoot, "app.log");
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
        File.AppendAllText(logPath, line);
    }
}
