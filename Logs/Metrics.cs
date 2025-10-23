// Version: 0.1.17.13
using System;
using System.Threading;

namespace Thmd.Logs;

public class Metrics
{
	private int _totalLogs;

	private int _failedWrites;

	private TimeSpan _totalProcessingTime;

	public void IncrementTotalLogs()
	{
		Interlocked.Increment(ref _totalLogs);
	}

	public void RecordError(Exception _)
	{
		Interlocked.Increment(ref _failedWrites);
	}

	public void RecordSuccess(TimeSpan duration)
	{
		_totalProcessingTime += duration;
	}

	public override string ToString()
	{
		return $"Logs: {_totalLogs}, Errors: {_failedWrites}, Avg Time: {_totalProcessingTime.TotalMilliseconds / _totalLogs} ms";
	}
}
