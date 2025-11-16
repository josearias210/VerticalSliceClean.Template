using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace Acme.Infrastructure.Extensions;

public static class FileLoggingExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string logDirectory = "logs")
    {
        builder.AddProvider(new FileLoggerProvider(logDirectory));
        return builder;
    }
}

public class FileLoggerProvider(string logDirectory) : ILoggerProvider
{
    private readonly string logDirectory = logDirectory;

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, logDirectory);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class FileLogger(string categoryName, string logDirectory) : ILogger
{
    private readonly string categoryName = categoryName;
    private readonly string logDirectory = logDirectory;
    private static readonly object lockObject = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var logFilePath = GetLogFilePath();
        var logMessage = FormatLogMessage(logLevel, categoryName, formatter(state, exception), exception);

        lock (lockObject)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.AppendAllText(logFilePath, logMessage);
        }
    }

    private string GetLogFilePath()
    {
        var fileName = $"app-{DateTime.UtcNow:yyyy-MM-dd}.log";
        return Path.Combine(logDirectory, fileName);
    }

    private static string FormatLogMessage(LogLevel logLevel, string category, string message, Exception? exception)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} ");
        sb.Append(CultureInfo.InvariantCulture, $"[{logLevel}] ");
        sb.Append(CultureInfo.InvariantCulture, $"{category}: ");
        sb.AppendLine(message);

        if (exception != null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Exception: {exception.GetType().Name}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"StackTrace: {exception.StackTrace}");
        }

        return sb.ToString();
    }
}
