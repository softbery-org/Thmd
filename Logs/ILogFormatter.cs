// Version: 0.1.17.13
namespace Thmd.Logs;

public interface ILogFormatter
{
	string Format(LogEntry entry);
}
