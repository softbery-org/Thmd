// Version: 0.1.0.18
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
