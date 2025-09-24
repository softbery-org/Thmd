// Version: 0.1.12.99
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
