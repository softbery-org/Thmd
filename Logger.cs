// Version: 0.1.11.28
using System;
using System.Collections.Generic;
using Thmd.Configuration;
using Thmd.Logs;

namespace Thmd;

/// <summary>
/// Logger class for centralized logging functionality.
/// </summary>
public static class Logger
{
	private static List<string> _categories = new List<string> { "Console", "File" };

	private static AsyncLogger _log { get; set; } = new AsyncLogger();

	/// <summary>
	/// Gets the configuration settings for the logger.
	/// </summary>
	public static Config Config { get; set; } = Config.Instance;

	/// <summary>
	/// Gets the logger instance.
	/// </summary>
	public static AsyncLogger Log
	{
		get
		{
			return _log;
		}
		set
		{
			_log = value;
		}
	}

	/// <summary>
	/// Initializes the logging system.
	/// </summary>
	/// <returns>Async logger</returns>
	public static AsyncLogger InitLogs()
	{
		AsyncLogger asyncLogger = new AsyncLogger();
		asyncLogger.MinLogLevel = Config.LogLevel;
		asyncLogger.CategoryFilters["Console"] = true;
		asyncLogger.CategoryFilters["File"] = true;
		_log = asyncLogger;
		_log.AddSink(new CategoryFilterSink(new FileSink("Logs", "log", new TextFormatter(), 10485760, 5), new string[1] { "File" }));
		_log.AddSink(new CategoryFilterSink(new ConsoleSink(new TextFormatter()), new string[1] { "Console" }));
		return _log;
	}

	/// <summary>
	/// Initializes the logging system.
	/// </summary>
	/// <param name="level">log level</param>
	/// <param name="message">log message</param>
	/// <param name="category">log categories</param>
	/// <param name="exception">exception details</param>
	public static void AddLog(LogLevel level, string message, string[] category = null, Exception exception = null)
	{
		if (category != null)
		{
			foreach (string cat in _categories)
			{
				string[] array = category ?? Array.Empty<string>();
				foreach (string c in array)
				{
					if (!string.IsNullOrEmpty(c) && !cat.Equals(c, StringComparison.OrdinalIgnoreCase) && !_categories.Contains(c))
					{
						_categories.Add(c);
					}
				}
			}
		}
		_log.Log(level, _categories.ToArray(), message, exception);
	}
}
