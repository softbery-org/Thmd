// Version: 0.1.16.93
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Thmd.Media;

public class FileMediaStream : IMediaStream, IDisposable
{
	private readonly string _filePath;

	private Stream _stream;

	private FileStream _fileStream;

	public FileMediaStream(string path)
	{
		_fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		_filePath = path;
	}

	public async Task<string> DownloadM3U8ContentAsync(string url)
	{
		HttpClient httpClient = new HttpClient();
		try
		{
			return await httpClient.GetStringAsync(url);
		}
		finally
		{
			((IDisposable)httpClient)?.Dispose();
		}
	}

	public Task<Stream> GetStream()
	{
		return Task.FromResult((Stream)File.OpenRead(_filePath));
	}

	public async Task<Stream> GetStreamAsync()
	{
		FileStream stream = File.OpenRead(_filePath);
		byte[] buffer = new byte[stream.Length];
		await stream.ReadAsync(buffer, 0, buffer.Length);
		_stream = new MemoryStream(buffer);
		return stream;
	}

	public double GetDuration()
	{
		return 120.0;
	}

	public void Dispose()
	{
		_fileStream?.Dispose();
	}
}
