// Version: 0.1.0.35
using System;
using System.Collections.Generic;
using Thmd.Configuration;
using Thmd.Logs;

namespace Thmd;

public static class Logger
{
	private static List<string> _categories = new List<string> { "Console", "File" };

	private static AsyncLogger _log { get; set; } = new AsyncLogger();

	public static Config Config { get; set; } = Config.Instance;

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

	public static AsyncLogger InitLogs()
	{
		AsyncLogger asyncLogger = new AsyncLogger();
		asyncLogger.MinLogLevel = Config.LogLevel;
		asyncLogger.CategoryFilters["Console"] = true;
		asyncLogger.CategoryFilters["File"] = true;
		_log = asyncLogger;
		_log.AddSink(new CategoryFilterSink(new FileSink("Logs", "log", new TextFormatter(), 10485760L), new string[1] { "File" }));
		_log.AddSink(new CategoryFilterSink(new ConsoleSink(new TextFormatter()), new string[1] { "Console" }));
		return _log;
	}

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
