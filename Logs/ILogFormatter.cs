// Version: 0.1.0.78
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
