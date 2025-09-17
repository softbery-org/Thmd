// Version: 0.1.9.33
using System.Threading.Tasks;

namespace Thmd.Logs;

public interface ILogSink
{
	Task WriteAsync(LogEntry entry);

	bool AcceptsCategory(string category);
}
