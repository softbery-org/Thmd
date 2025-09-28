// Version: 0.1.15.17
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
