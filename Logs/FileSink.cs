// Version: 0.1.9.33
using System;
using System.IO;
using System.Threading.Tasks;

namespace Thmd.Logs;

public class FileSink : ILogSink
{
	private readonly string _logDirectory;

	private readonly string _filePrefix;

	private readonly ILogFormatter _formatter;

	private readonly long _maxFileSize;

	private readonly int _maxRetainedFiles;

	private readonly object _lock = new object();

	private string _currentFilePath;

	public FileSink(string logDirectory = "logs", string filePrefix = "log", ILogFormatter formatter = null, long maxFileSize = 10485760L, int maxRetainedFiles = 5)
	{
		_logDirectory = logDirectory;
		_filePrefix = filePrefix;
		_formatter = formatter ?? new TextFormatter();
		_maxFileSize = maxFileSize;
		_maxRetainedFiles = maxRetainedFiles;
		Directory.CreateDirectory(logDirectory);
		UpdateCurrentFile();
	}

	public void Write(LogEntry entry)
	{
		lock (_lock)
		{
			CheckFileRotation();
			File.AppendAllText(_currentFilePath, _formatter.Format(entry) + Environment.NewLine);
		}
	}

	private void CheckFileRotation()
	{
		FileInfo fileInfo = new FileInfo(_currentFilePath);
		if (fileInfo.Exists && fileInfo.Length > _maxFileSize)
		{
			RotateFiles();
			UpdateCurrentFile();
		}
	}

	private void UpdateCurrentFile()
	{
		_currentFilePath = Path.Combine(_logDirectory, $"{_filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
	}

	private void RotateFiles()
	{
		string[] files = Directory.GetFiles(_logDirectory, _filePrefix + "_*.txt");
		if (files.Length >= _maxRetainedFiles)
		{
			Array.Sort(files);
			for (int i = 0; i < files.Length - _maxRetainedFiles + 1; i++)
			{
				File.Delete(files[i]);
			}
		}
	}

	public async Task WriteAsync(LogEntry entry)
	{
		await Task.Run(delegate
		{
			Write(entry);
		});
	}

	public bool AcceptsCategory(string category)
	{
		if (category == null)
		{
			return false;
		}
		return true;
	}
}
