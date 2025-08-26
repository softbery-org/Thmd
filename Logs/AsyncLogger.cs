// Version: 0.1.1.60
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Thmd.Logs;

public class AsyncLogger : IDisposable
{
	private readonly BlockingCollection<LogEntry> _logQueue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>());

	private readonly List<ILogSink> _sinks = new List<ILogSink>();

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private readonly Metrics _metrics = new Metrics();

	private readonly int _batchSize = 10;

	private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5.0);

	public LogLevel MinLogLevel { get; set; } = LogLevel.Info;

	public Dictionary<string, bool> CategoryFilters { get; } = new Dictionary<string, bool>();

	public AsyncLogger()
	{
		Task.Run(ProcessLogs);
	}

	public void AddSink(ILogSink sink)
	{
		_sinks.Add(sink);
	}

	public void Log(LogLevel level, string category, string message, Exception exception = null)
	{
		if (level >= MinLogLevel && IsCategoryEnabled(category) && _logQueue != null)
		{
			if (_logQueue.IsAddingCompleted)
			{
				Console.WriteLine("Log queue is completed. Cannot add new log entries.");
				return;
			}
			_logQueue.Add(new LogEntry(level, category, message, exception));
			_metrics.IncrementTotalLogs();
		}
	}

	public void Log(LogLevel level, string[] categories, string message, Exception exception = null)
	{
		foreach (string category in categories)
		{
			Log(level, category, message, exception);
		}
	}

	private bool IsCategoryEnabled(string category)
	{
		bool enabled;
		return !CategoryFilters.TryGetValue(category, out enabled) || enabled;
	}

	private async Task ProcessLogs()
	{
		List<LogEntry> batch = new List<LogEntry>();
		DateTime lastFlush = DateTime.UtcNow;
		while (!_cts.IsCancellationRequested)
		{
			try
			{
				LogEntry entry = _logQueue.Take(_cts.Token);
				batch.Add(entry);
				if (batch.Count >= _batchSize || DateTime.UtcNow - lastFlush >= _flushInterval)
				{
					await FlushBatch(batch);
					batch.Clear();
					lastFlush = DateTime.UtcNow;
				}
			}
			catch (OperationCanceledException)
			{
				await FlushBatch(batch);
				break;
			}
		}
	}

	private async Task FlushBatch(List<LogEntry> batch)
	{
		Stopwatch sw = Stopwatch.StartNew();
		try
		{
			foreach (ILogSink sink in _sinks)
			{
				List<LogEntry> filtered = batch.FindAll((e) => sink.AcceptsCategory(e.Category));
				foreach (LogEntry entry in filtered)
				{
					await sink.WriteAsync(entry);
				}
			}
			_metrics.RecordSuccess(sw.Elapsed);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_metrics.RecordError(ex2);
		}
	}

	public Metrics GetMetrics()
	{
		return _metrics;
	}

	public void Dispose()
	{
		_cts.Cancel();
		_logQueue.CompleteAdding();
	}
}
