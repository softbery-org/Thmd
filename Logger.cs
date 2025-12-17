// Version: 0.2.0.34
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
    /// <summary>
    /// Zwraca lub ustawia instancję konfiguracji aplikacji.
    /// </summary>
    public static Config Config
    { 
        get; 
        set;
    } = Config.Instance;

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
