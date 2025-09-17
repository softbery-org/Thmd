// Version: 0.1.9.33
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
