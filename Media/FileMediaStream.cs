// Version: 0.1.17.19
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Thmd.Media;

/// <summary>
/// Provides functionality to read and manage media data from a local file.
/// Implements <see cref="IMediaStream"/> for consistent media streaming behavior
/// and <see cref="IDisposable"/> for proper resource cleanup.
/// </summary>
public class FileMediaStream : IMediaStream, IDisposable
{
	private readonly string _filePath;
	private Stream _stream;
	private FileStream _fileStream;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileMediaStream"/> class for the specified file path.
	/// Opens the file for reading with shared read access.
	/// </summary>
	/// <param name="path">The path to the media file.</param>
	public FileMediaStream(string path)
	{
		_fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		_filePath = path;
	}

	/// <summary>
	/// Asynchronously downloads M3U8 (HLS) playlist content from the specified URL.
	/// </summary>
	/// <param name="url">The URL of the M3U8 playlist file.</param>
	/// <returns>
	/// A task representing the asynchronous operation.
	/// The task result contains the downloaded playlist content as a string.
	/// </returns>
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

	/// <summary>
	/// Returns a readable stream for the associated file.
	/// </summary>
	/// <returns>
	/// A task containing a <see cref="Stream"/> for reading the file content.
	/// </returns>
	public Task<Stream> GetStream()
	{
		return Task.FromResult((Stream)File.OpenRead(_filePath));
	}

	/// <summary>
	/// Asynchronously loads the file into memory and returns a stream.
	/// </summary>
	/// <returns>
	/// A task containing a <see cref="Stream"/> representing the opened file.
	/// </returns>
	public async Task<Stream> GetStreamAsync()
	{
		FileStream stream = File.OpenRead(_filePath);
		byte[] buffer = new byte[stream.Length];
		await stream.ReadAsync(buffer, 0, buffer.Length);
		_stream = new MemoryStream(buffer);
		return stream;
	}

	/// <summary>
	/// Gets the duration of the media file in seconds.
	/// </summary>
	/// <returns>
	/// The duration of the media file, currently returns a fixed value of 120 seconds.
	/// </returns>
	public double GetDuration()
	{
		return 120.0;
	}

	/// <summary>
	/// Releases all unmanaged resources used by the current instance.
	/// </summary>
	public void Dispose()
	{
		_fileStream?.Dispose();
	}
}
