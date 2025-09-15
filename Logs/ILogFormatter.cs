// Version: 0.1.9.4
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
