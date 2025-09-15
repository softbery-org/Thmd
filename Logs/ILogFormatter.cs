// Version: 0.1.9.1
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
