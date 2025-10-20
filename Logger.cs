// Version: 0.2.0.19
using System;
using System.Collections.Generic;
using System.Linq;

using Thmd.Configuration;
using Thmd.Logs;

namespace Thmd;

/// <summary>
/// Centralny logger aplikacji z obs�ug� kategorii i poziom�w logowania.
/// </summary>
public static class Logger
{
    private static readonly List<string> _categories = new() { "Console", "File" };
    private static AsyncLogger _log = new();
    public static Config Config { get; set; } = Config.Instance;

    /// <summary>
    /// Zwraca instancj� loggera asynchronicznego.
    /// </summary>
    public static AsyncLogger Log
    {
        get => _log;
        set => _log = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Inicjalizuje system logowania i dodaje podstawowe sinki (Console, File).
    /// </summary>
    public static AsyncLogger InitLogs()
    {
        var asyncLogger = new AsyncLogger
        {
            MinLogLevel = Config.LogLevel
        };

        asyncLogger.CategoryFilters["Console"] = true;
        asyncLogger.CategoryFilters["File"] = true;

        asyncLogger.AddSink(new CategoryFilterSink(
            new FileSink("Logs", "log", new TextFormatter(), 10 * 1024 * 1024, 5),
            new[] { "File" }));

        asyncLogger.AddSink(new CategoryFilterSink(
            new ConsoleSink(new TextFormatter()),
            new[] { "Console" }));

        _log = asyncLogger;
        return _log;
    }

    /// <summary>
    /// Dodaje nowy wpis do log�w.
    /// </summary>
    public static void AddLog(LogLevel level, string message, string[] categories = null, Exception exception = null)
    {
        var finalCategories = new List<string>(_categories);

        if (categories != null)
        {
            foreach (var cat in categories)
            {
                if (!string.IsNullOrWhiteSpace(cat) && !finalCategories.Contains(cat, StringComparer.OrdinalIgnoreCase))
                    finalCategories.Add(cat);
            }
        }

        _log.Log(level, finalCategories.ToArray(), message, exception);
    }

    /// <summary>
    /// Skr�t do logowania informacji.
    /// </summary>
    public static void Info(string message) => AddLog(LogLevel.Info, message);

    /// <summary>
    /// Skr�t do logowania b��d�w.
    /// </summary>
    public static void Error(string message, Exception ex = null) => AddLog(LogLevel.Error, message, exception: ex);

    /// <summary>
    /// Skr�t do logowania ostrze�e�.
    /// </summary>
    public static void Warn(string message) => AddLog(LogLevel.Warning, message);
}
