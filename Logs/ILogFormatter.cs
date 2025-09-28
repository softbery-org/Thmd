// Version: 0.1.15.4
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
