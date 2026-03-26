namespace CalFocus.Core.Abstractions.Services;

public interface IAppLogger
{
    void LogInfo(string message);
    void LogError(string message, Exception? exception = null);
}
