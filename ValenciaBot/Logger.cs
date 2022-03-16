using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.PathExtensions;
using ValenciaBot.DirectoryExtensions;

namespace ValenciaBot;

public class Logger
{
    private static ILogger _logger = new LoggerCombiner(new ConsoleLogger(),
                                                        new FileLogger(DirectoryExt.ProjectDirectory!.Parent!.FullName + "\\logs\\log.log"));

    public static void Log(string message) => _logger.Log(message);
    public static void LogWarning(string message) => _logger.LogWarning(message);
    public static void LogError(string message) => _logger.LogError(message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"{DateTime.UtcNow.ToString("[HH:mm:ss] ")}------------" + message + '.');
    public void LogWarning(string message) => Log("Warning: " + message);
    public void LogError(string message) => Log("Error: " + message);
}

public class FileLogger : ILogger, IDisposable
{
    public const string DefaultExtension = ".log";

    private StringBuilder _logBuilder = new StringBuilder();
    private string _filePath;
    private System.Timers.Timer _timer;

    public FileLogger(string path = "log.log", bool addDateAndTimeTime = true, int logPeriodInseconds = 20)
    {
        if(Path.HasExtension(path) == false)
            path = Path.ChangeExtension(path, DefaultExtension);
        if(addDateAndTimeTime)
        {
            string dateTime = DateTime.Now.ToString("yyyyMMdd-hhmmss");
            path = PathExt.SetName(path, Path.GetFileNameWithoutExtension(path) + dateTime);
        }
        _filePath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        _timer = new System.Timers.Timer(logPeriodInseconds * 1000);
        _timer.Elapsed += (o, a) => AppendToFile();
        _timer.AutoReset = false;
        _timer.Start();
    }

    public void Log(string message) => _logBuilder.Append($"{DateTime.UtcNow.ToString("[HH:mm:ss] ")}{message}\n");
    public void LogWarning(string message) => Log("Warning: " + message);
    public void LogError(string message) => Log("Error: " + message);

    public void Dispose()
    {
        AppendToFile();
        _timer.Stop();
        _timer.Dispose();
    }

    public void AppendToFile()
    {
        _timer.Stop();
        File.AppendAllText(_filePath, _logBuilder.ToString());
        _logBuilder.Clear();

        _timer.Start();
    }
}

public class LoggerCombiner : ILogger
{
    private List<ILogger> _loggers;

    public LoggerCombiner(params ILogger[] loggers)
    {
        _loggers = loggers.ToList();
    }
    public LoggerCombiner(IEnumerable<ILogger> loggers)
    {
        _loggers = loggers.ToList();
    }

    public void Log(string message) => _loggers.ForEach(l => l.Log(message));
    public void LogError(string message) => _loggers.ForEach(l => l.LogError(message));
    public void LogWarning(string message) => _loggers.ForEach(l => l.LogWarning(message));
}


public interface ILogger
{
    public void Log(string message);
    public void LogWarning(string message);
    public void LogError(string message);
}
