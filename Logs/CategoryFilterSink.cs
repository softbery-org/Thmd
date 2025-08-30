// Version: 0.1.3.33
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Thmd.Logs;

public class CategoryFilterSink : ILogSink
{
	private readonly ILogSink _innerSink;

	private readonly HashSet<string> _allowedCategories;

	public CategoryFilterSink(ILogSink innerSink, IEnumerable<string> allowedCategories)
	{
		_innerSink = innerSink;
		_allowedCategories = new HashSet<string>(allowedCategories);
	}

	public bool AcceptsCategory(string category)
	{
		return _allowedCategories.Contains(category);
	}

	public async Task WriteAsync(LogEntry entry)
	{
		if (AcceptsCategory(entry.Category))
		{
			await _innerSink.WriteAsync(entry);
		}
	}
}
