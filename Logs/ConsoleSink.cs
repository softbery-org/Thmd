// Version: 0.1.0.78
using System;
using System.Threading.Tasks;

namespace Thmd.Logs;

public class ConsoleSink : ILogSink
{
	private readonly ILogFormatter _formatter;

	public ConsoleSink(ILogFormatter formatter = null)
	{
		_formatter = formatter ?? new TextFormatter();
	}

	public bool AcceptsCategory(string category)
	{
		if (category == null)
		{
			return false;
		}
		return true;
	}

	public void Write(LogEntry entry)
	{
		ConsoleColor currentColor = Console.ForegroundColor;
		switch (entry.Level)
		{
		case LogLevel.Debug:
			Console.ForegroundColor = ConsoleColor.Yellow;
			break;
		case LogLevel.Info:
			Console.ForegroundColor = ConsoleColor.Green;
			break;
		case LogLevel.Warning:
			Console.ForegroundColor = ConsoleColor.Blue;
			break;
		case LogLevel.Error:
			Console.ForegroundColor = ConsoleColor.Red;
			break;
		default:
			Console.ForegroundColor = currentColor;
			break;
		}
		Console.WriteLine(_formatter.Format(entry));
	}

	public async Task WriteAsync(LogEntry entry)
	{
		await Task.Run(delegate
		{
			Write(entry);
		});
	}
}
