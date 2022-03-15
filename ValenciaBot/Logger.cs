using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot;

public class Logger
{
    private static ILogger _logger = new ConsoleLogger();

    public static void Log(string message) => _logger.Log(message);
    public static void LogWarning(string message) => _logger.LogWarning(message);
    public static void LogError(string message) => _logger.LogError(message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine("------------Logger: " + message + '.');
    public void LogWarning(string message) => Log("Warning: " + message);
    public void LogError(string message) => Log("Error: " + message);
}


public interface ILogger
{
    public void Log(string message);
    public void LogWarning(string message);
    public void LogError(string message);
}
