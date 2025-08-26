// Version: 0.1.1.30
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
