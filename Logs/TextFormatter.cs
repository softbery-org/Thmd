// Version: 0.1.3.78
using System;

namespace Thmd.Logs;

public class TextFormatter : ILogFormatter
{
	public string Format(LogEntry entry)
	{
		ConsoleColor current = Console.ForegroundColor;
		current = ConsoleColor.Gray;
		string logEntry = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} {entry.Object} [{entry.Level}] {entry.Message}";
		if (entry.Exception != null)
		{
			logEntry = logEntry + "\nException: " + entry.Exception.Message + "\nStack Trace: " + entry.Exception.StackTrace;
		}
		return logEntry;
	}
}
