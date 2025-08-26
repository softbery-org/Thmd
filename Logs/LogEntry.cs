// Version: 0.1.1.86
using System;

namespace Thmd.Logs;

public class LogEntry
{
	public DateTime Timestamp { get; }

	public LogLevel Level { get; }

	public string Category { get; }

	public string Message { get; }

	public Exception Exception { get; }

	public string Object { get; } = null;

	public LogEntry(LogLevel level, string category, string message, Exception exception = null)
	{
		Timestamp = DateTime.Now;
		Level = level;
		Category = category;
		Message = message;
		Exception = exception;
	}

	public LogEntry(object obj, LogLevel level, string category, string message, Exception exception = null)
	{
		Timestamp = DateTime.Now;
		Level = level;
		Category = category;
		Message = message;
		Exception = exception;
		Object = obj?.ToString();
	}
}
