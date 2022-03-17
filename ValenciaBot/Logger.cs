using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValenciaBot.PathExtensions;
using ValenciaBot.DirectoryExtensions;
using System.Diagnostics;
using System.Reflection;

namespace ValenciaBot;

public class Logger
{
    private static readonly ILogger _logger = new LoggerCombiner(new ConsoleLogger(),
                                                        new FileLogger(Program.LogPath));

    public static void Log(string message) => _logger.Log(message);
    public static void LogWarning(string message) => _logger.LogWarning(message);
    public static void LogError(string message) => _logger.LogError(message);
}

public class ClassLogger : ILogger
{
    private readonly string _prefix;
    private readonly int _startFrameCount;

    public ClassLogger(string className)
    {
        _prefix = $"[{className}] ";
        _startFrameCount = new StackTrace().FrameCount;
    }

    public void Log(string message) => Logger.Log(_prefix + message);
    public void LogError(string message) => Logger.LogError(_prefix + message);
    public void LogWarning(string message) => Logger.LogWarning(_prefix + message);

    public void StartMethod(params object[] args)
    {
        StackTrace stackTrace = new();
        MethodBase method = stackTrace.GetFrame(1)!.GetMethod()!;
        int tabSize = stackTrace.FrameCount - _startFrameCount;

        string tab = new(' ', tabSize);
        StringBuilder stringBuilder = new();
        stringBuilder.Append(_prefix);
        stringBuilder.Append(tab);
        stringBuilder.Append($"Starting Method '{method.Name}'");
        for(int i = 0; i < args.Length; i++)
            stringBuilder.Append($" arg-{i} '{args[i]}'");

        Logger.Log(stringBuilder.ToString());
    }

    public void StopMethod(params object?[] results)
    {
        StackTrace stackTrace = new();
        MethodBase method = stackTrace.GetFrame(1)!.GetMethod()!;
        MethodInfo? methodInfo = method as MethodInfo;
        int tabSize = stackTrace.FrameCount - _startFrameCount;

        string tab = new(' ', tabSize);
        StringBuilder stringBuilder = new();
        stringBuilder.Append(_prefix);
        stringBuilder.Append(tab);
        if(typeof(void) == methodInfo?.ReturnType && results.Length == 0)
        {
            stringBuilder.Append($"Method '{method.Name}' finished");
        }
        else
        {
            stringBuilder.Append($"Method '{method.Name}' finished with results");
            for(int i = 0; i < results.Length; i++)
                stringBuilder.Append($" result-{i} '{results[i] ?? "null"}'");
        }

        Logger.Log(stringBuilder.ToString());
    }
}


public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"{DateTime.UtcNow:[HH:mm:ss UTC]} ------------" + message + '.');
    public void LogWarning(string message) => Log("Warning: " + message);
    public void LogError(string message) => Log("Error: " + message);
}

public class FileLogger : ILogger, IDisposable
{
    public const string DefaultExtension = ".log";

    private readonly StringBuilder _logBuilder = new();
    private string _filePath = String.Empty;
    private readonly string _originalFilePath;
    private readonly System.Timers.Timer _timer;
    private readonly int _maxLettersInFile;
    private int _lettersLogged;

    public FileLogger(string path = "log.log", int maxLettersInFile = 100000, int logPeriodInseconds = 20)
    {
        _originalFilePath = path;
        Directory.CreateDirectory(Path.GetDirectoryName(_originalFilePath)!);
        SetupNewFile();

        _maxLettersInFile = maxLettersInFile;

        _timer = new System.Timers.Timer(logPeriodInseconds * 1000);
        _timer.Elapsed += (o, a) => AppendToFile();
        _timer.AutoReset = false;
        _timer.Start();
    }

    public void Log(string message) => _logBuilder.Append($"{DateTime.UtcNow.ToString("[HH:mm:ss UTC] ")}{message}\n");
    public void LogWarning(string message) => Log("Warning: " + message);
    public void LogError(string message) => Log("Error: " + message);

    public void Dispose()
    {
        AppendToFile();
        _timer.Stop();
        _timer.Dispose();
    }

    private void SetupNewFile()
    {
        string dateTime = DateTime.Now.ToString("yyyyMMdd-hhmmss");
        string path = PathExt.SetName(_originalFilePath, Path.GetFileNameWithoutExtension(_originalFilePath) + dateTime);

        if(Path.HasExtension(path) == false)
            path = Path.ChangeExtension(path, DefaultExtension);

        _filePath = Path.GetFullPath(path);
        _lettersLogged = 0;
    }

    private void AppendToFile()
    {
        _timer.Stop();
        string newLog = _logBuilder.ToString();

        if(_lettersLogged != 0 && _lettersLogged + newLog.Length > _maxLettersInFile)
            SetupNewFile();

        File.AppendAllText(_filePath, newLog);
        _lettersLogged += newLog.Length;
        _logBuilder.Clear();
        _timer.Start();
    }
}

public class LoggerCombiner : ILogger
{
    private readonly List<ILogger> _loggers;

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
