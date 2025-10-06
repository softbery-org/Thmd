// Version: 0.1.16.93
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
