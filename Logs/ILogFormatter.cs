// Version: 0.1.3.33
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
