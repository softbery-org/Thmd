// Version: 0.1.16.93
using Newtonsoft.Json;

namespace Thmd.Logs;

public class JsonFormatter : ILogFormatter
{
	public string Format(LogEntry entry)
	{
		var logData = new
		{
			entry.Timestamp,
			entry.Level,
			entry.Message,
			Exception = entry.Exception?.Message,
			entry.Exception?.StackTrace
		};
		return JsonConvert.SerializeObject(logData);
	}
}
