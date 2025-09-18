// Version: 0.1.11.28
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
