// Logger.cs
// Version: 1.0.0.2
// Prosty, thread-safe logger z obsługą poziomów logów, zapisu do pliku i opcjonalnie konsoli.

using System;
using System.IO;
using System.Text;

using Thmd.Configuration;

namespace Thmd.Logs;

public static class Logger
{
    private static readonly object _lock = new();
    private static bool _isInitialized = false;
    private static string _logFilePath = string.Empty;
    private static LogLevel _minimumLevel = LogLevel.Info;

    /// <summary>
    /// Inicjalizuje system logowania. Wywoływać raz na starcie aplikacji.
    /// </summary>
    public static void InitLogs()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;

            var config = Config.Instance;
            
            _minimumLevel = config.LogLevel;

            // Tworzenie katalogu logs
            var logsDirectory = Path.GetFullPath(config.LogsDirectoryPath);
            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);

            // Nazwa pliku: logs/yyyy-MM-dd.log
            var datePrefix = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logsDirectory, $"{datePrefix}.log");

            // Nagłówek pliku logów
            var header = $"\r\n" +
                         $"============================================================\r\n" +
                         $" LOG STARTED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                         $" Application version: 0.2.0.33\r\n" +
                         $"============================================================\r\n";

            File.AppendAllText(_logFilePath, header);

            _isInitialized = true;

            WriteLog(LogLevel.Info, "Logger initialized successfully.");
        }
    }

    /// <summary>
    /// Zapisuje wiadomość do loga (jeśli poziom pozwala).
    /// </summary>
    public static void Log(LogLevel level, string message, params object[] args)
    {
        if (!_isInitialized)
            InitLogs(); // Automatyczna inicjalizacja, jeśli zapomniano

        if (level < _minimumLevel)
            return;

        WriteLog(level, string.Format(message, args));
    }

    /// <summary>
    /// Skróty dla poszczególnych poziomów
    /// </summary>
    public static void Debug(string message, params object[] args) => Log(LogLevel.Debug, message, args);
    public static void Info(string message, params object[] args) => Log(LogLevel.Info, message, args);
    public static void Warning(string message, params object[] args) => Log(LogLevel.Warning, message, args);
    public static void Error(string message, params object[] args) => Log(LogLevel.Error, message, args);
    public static void Fatal(string message, params object[] args) => Log(LogLevel.Fatal, message, args);

    private static void WriteLog(LogLevel level, string message)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var levelStr = level.ToString().PadRight(7);
            var threadId = AppDomain.GetCurrentThreadId().ToString().PadLeft(3);

            var line = $"[{timestamp}][{levelStr}][Thread:{threadId}] {message}\r\n";

            try
            {
                File.AppendAllText(_logFilePath, line);

                // Opcjonalnie: wypisz na konsolę (pomocne przy debugowaniu)
                if (Config.Instance.EnableConsolLogging)
                {
                    var consoleColor = level switch
                    {
                        LogLevel.Debug => ConsoleColor.Gray,
                        LogLevel.Info => ConsoleColor.White,
                        LogLevel.Warning => ConsoleColor.Yellow,
                        LogLevel.Error => ConsoleColor.Red,
                        LogLevel.Fatal => ConsoleColor.DarkRed,
                        _ => ConsoleColor.White
                    };

                    Console.ForegroundColor = consoleColor;
                    Console.Write(line.TrimEnd());
                    Console.ResetColor();
                }
            }
            catch
            {
                // Nie rzucamy wyjątków z loggera – to mogłoby zabić aplikację
            }
        }
    }
}
