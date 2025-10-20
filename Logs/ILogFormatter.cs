// Version: 0.1.17.11
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
