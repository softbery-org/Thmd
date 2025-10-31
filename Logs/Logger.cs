using System;
using System.Collections.Generic;
using System.IO;
using System.Windows; // For WPF integration, e.g., MessageBox for UI alerts
using System.Threading.Tasks; // For asynchronous logging
using System.Windows.Threading; // For Dispatcher

namespace Thmd.Logs
{
    /// <summary>
    /// Enum representing different levels of logging severity.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level
        /// </summary>
        Debug,
        /// <summary>
        /// Info level
        /// </summary>
        Info,
        /// <summary>
        /// Warning level
        /// </summary>
        Warning,
        /// <summary>
        /// Error level
        /// </summary>
        Error,
        /// <summary>
        /// Critical level
        /// </summary>
        Critical
    }

    /// <summary>
    /// Interface defining the contract for log handlers.
    /// Handlers implement this to process log messages.
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        /// Logs a message with the specified level and optional exception.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        void Log(LogLevel level, string message, Exception ex = null);
    }

    /// <summary>
    /// Abstract base class for log handlers providing common functionality
    /// such as minimum level filtering.
    /// </summary>
    public abstract class BaseLogHandler : ILogHandler
    {
        /// <summary>
        /// Gets or sets the minimum log level this handler will process.
        /// </summary>
        protected LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Initializes a new instance of the BaseLogHandler class.
        /// </summary>
        /// <param name="minLevel">The minimum log level to handle (default: Info).</param>
        protected BaseLogHandler(LogLevel minLevel = LogLevel.Info)
        {
            MinimumLevel = minLevel;
        }

        /// <summary>
        /// Logs a message if the level meets or exceeds the minimum level.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        public void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level >= MinimumLevel)
            {
                HandleLog(level, message, ex);
            }
        }

        /// <summary>
        /// Abstract method to be implemented by derived handlers for actual logging logic.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        protected abstract void HandleLog(LogLevel level, string message, Exception ex = null);
    }

    /// <summary>
    /// Console log handler that outputs logs to the console.
    /// </summary>
    public class ConsoleLogHandler : BaseLogHandler
    {
        /// <summary>
        /// Initializes a new instance of the ConsoleLogHandler class.
        /// </summary>
        /// <param name="minLevel">The minimum log level to handle (default: Info).</param>
        public ConsoleLogHandler(LogLevel minLevel = LogLevel.Info) : base(minLevel) { }

        /// <summary>
        /// Handles logging by writing to the console.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        protected override void HandleLog(LogLevel level, string message, Exception ex = null)
        {
            var logMessage = $"[{DateTime.Now}] [{level}] {message}";
            if (ex != null)
            {
                logMessage += $"\n{ex}";
            }
            Console.WriteLine(logMessage);
        }
    }

    /// <summary>
    /// File log handler that appends logs to a specified file.
    /// </summary>
    public class FileLogHandler : BaseLogHandler
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the FileLogHandler class.
        /// </summary>
        /// <param name="filePath">The path to the log file.</param>
        /// <param name="minLevel">The minimum log level to handle (default: Info).</param>
        public FileLogHandler(string filePath, LogLevel minLevel = LogLevel.Info) : base(minLevel)
        {
            _filePath = filePath;
            EnsureFileExists();
        }

        /// <summary>
        /// Ensures the log file exists and creates the directory if necessary.
        /// </summary>
        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? string.Empty);
                File.Create(_filePath).Dispose();
            }
        }

        /// <summary>
        /// Handles logging by appending to the file (thread-safe).
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        protected override void HandleLog(LogLevel level, string message, Exception ex = null)
        {
            lock (_lock)
            {
                using (var writer = File.AppendText(_filePath))
                {
                    var logMessage = $"[{DateTime.Now}] [{level}] {message}";
                    if (ex != null)
                    {
                        logMessage += $"\n{ex}";
                    }
                    writer.WriteLine(logMessage);
                }
            }
        }
    }

    /// <summary>
    /// WPF UI log handler that displays logs via MessageBox for UI thread safety.
    /// </summary>
    public class WpfUiLogHandler : BaseLogHandler
    {
        /// <summary>
        /// Initializes a new instance of the WpfUiLogHandler class.
        /// </summary>
        /// <param name="minLevel">The minimum log level to handle (default: Error).</param>
        public WpfUiLogHandler(LogLevel minLevel = LogLevel.Error) : base(minLevel) { }

        /// <summary>
        /// Handles logging by showing a MessageBox on the UI thread.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        protected override void HandleLog(LogLevel level, string message, Exception ex = null)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var fullMessage = $"{message}{(ex != null ? $"\n{ex.Message}\n{ex.StackTrace}" : "")}";
                MessageBox.Show($"[{level}] {fullMessage}", "Log Message", MessageBoxButton.OK, GetMessageBoxImage(level));
            });
        }

        /// <summary>
        /// Gets the appropriate MessageBox image based on the log level.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <returns>The corresponding MessageBoxImage.</returns>
        private MessageBoxImage GetMessageBoxImage(LogLevel level)
        {
            return level switch
            {
                LogLevel.Warning => MessageBoxImage.Warning,
                LogLevel.Error => MessageBoxImage.Error,
                LogLevel.Critical => MessageBoxImage.Error,
                _ => MessageBoxImage.Information
            };
        }
    }

    /// <summary>
    /// Main singleton Logger class that manages multiple handlers and global settings.
    /// </summary>
    public class Logger
    {
        private static Logger _instance;
        private static readonly object _instanceLock = new object();
        private readonly List<ILogHandler> _handlers = new List<ILogHandler>();
        private LogLevel _globalMinLevel = LogLevel.Info;

        private Logger() { }

        /// <summary>
        /// Gets the singleton instance of the Logger.
        /// </summary>
        public static Logger Instance
        {
            get
            {
                lock (_instanceLock)
                {
                    return _instance ??= new Logger();
                }
            }
        }

        /// <summary>
        /// Adds a log handler to the list of active handlers.
        /// </summary>
        /// <param name="handler">The ILogHandler to add.</param>
        public void AddHandler(ILogHandler handler)
        {
            _handlers.Add(handler);
        }

        /// <summary>
        /// Sets the global minimum log level for all handlers.
        /// </summary>
        /// <param name="level">The new global minimum level.</param>
        public void SetGlobalMinLevel(LogLevel level)
        {
            _globalMinLevel = level;
        }

        /// <summary>
        /// Logs a message synchronously (without exception).
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        public void Log(LogLevel level, string message)
        {
            Log(level, message, null);
        }

        /// <summary>
        /// Logs a message synchronously with optional exception.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        public void Log(LogLevel level, string message, Exception ex)
        {
            if (level < _globalMinLevel) return;

            foreach (var handler in _handlers)
            {
                handler.Log(level, message, ex);
            }
        }

        /// <summary>
        /// Logs a message asynchronously (without exception).
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LogAsync(LogLevel level, string message)
        {
            await LogAsync(level, message, null);
        }

        /// <summary>
        /// Logs a message asynchronously with optional exception.
        /// </summary>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="ex">Optional exception associated with the log.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LogAsync(LogLevel level, string message, Exception ex)
        {
            if (level < _globalMinLevel) return;

            await Task.Run(() => Log(level, message, ex));
        }

        // Convenience methods (sync)
        /// <summary>
        /// Logs a debug message synchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Debug(string message) => Log(LogLevel.Debug, message);
        /// <summary>
        /// Logs a debug message synchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        public void Debug(string message, Exception ex) => Log(LogLevel.Debug, message, ex);
        /// <summary>
        /// Logs an info message synchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Info(string message) => Log(LogLevel.Info, message);
        /// <summary>
        /// Logs an info message synchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        public void Info(string message, Exception ex) => Log(LogLevel.Info, message, ex);
        /// <summary>
        /// Logs a warning message synchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Warn(string message) => Log(LogLevel.Warning, message);
        /// <summary>
        /// Logs a warning message synchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        public void Warn(string message, Exception ex) => Log(LogLevel.Warning, message, ex);
        /// <summary>
        /// Logs an error message synchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Error(string message) => Log(LogLevel.Error, message);
        /// <summary>
        /// Logs an error message synchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        public void Error(string message, Exception ex) => Log(LogLevel.Error, message, ex);
        /// <summary>
        /// Logs a critical message synchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        public void Critical(string message) => Log(LogLevel.Critical, message);
        /// <summary>
        /// Logs a critical message synchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        public void Critical(string message, Exception ex) => Log(LogLevel.Critical, message, ex);

        // Simplified async convenience methods
        /// <summary>
        /// Logs a debug message asynchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DebugAsync(string message) => await LogAsync(LogLevel.Debug, message);
        /// <summary>
        /// Logs a debug message asynchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DebugAsync(string message, Exception ex) => await LogAsync(LogLevel.Debug, message, ex);
        /// <summary>
        /// Logs an info message asynchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InfoAsync(string message) => await LogAsync(LogLevel.Info, message);
        /// <summary>
        /// Logs an info message asynchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InfoAsync(string message, Exception ex) => await LogAsync(LogLevel.Info, message, ex);
        /// <summary>
        /// Logs a warning message asynchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WarnAsync(string message) => await LogAsync(LogLevel.Warning, message);
        /// <summary>
        /// Logs a warning message asynchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task WarnAsync(string message, Exception ex) => await LogAsync(LogLevel.Warning, message, ex);
        /// <summary>
        /// Logs an error message asynchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ErrorAsync(string message) => await LogAsync(LogLevel.Error, message);
        /// <summary>
        /// Logs an error message asynchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ErrorAsync(string message, Exception ex) => await LogAsync(LogLevel.Error, message, ex);
        /// <summary>
        /// Logs a critical message asynchronously (without exception).
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CriticalAsync(string message) => await LogAsync(LogLevel.Critical, message);
        /// <summary>
        /// Logs a critical message asynchronously with exception.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CriticalAsync(string message, Exception ex) => await LogAsync(LogLevel.Critical, message, ex);
    }

    // Example usage in a WPF application
    // In App.xaml.cs or MainWindow.xaml.cs:
    // Logger.Instance.AddHandler(new ConsoleLogHandler());
    // Logger.Instance.AddHandler(new FileLogHandler("app.log"));
    // Logger.Instance.AddHandler(new WpfUiLogHandler(LogLevel.Critical));
    // await Logger.Instance.InfoAsync("Application started.");
}
// Version: 0.1.0.26
