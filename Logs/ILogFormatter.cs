// Version: 0.1.1.60
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
