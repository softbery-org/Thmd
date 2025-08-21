// Version: 0.1.0.17
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Thmd.Media;

namespace Thmd.Helpers;

public class VideoTypeChecker
{
	private readonly string _filePath;

	public bool IsAvi { get; }

	public bool IsMp4 { get; }

	public bool IsM3u8 { get; }

	public VideoTypeChecker(string filePath)
	{
		_filePath = filePath;
		string extension = Path.GetExtension(filePath).ToLowerInvariant();
		IsAvi = extension == ".avi";
		IsMp4 = extension == ".mp4";
		IsM3u8 = extension == ".m3u8";
	}

	public void DownloadM3u8(string outputPath)
	{
		ValidateM3u8();
		Console.WriteLine("Downloading M3U8 stream from " + _filePath + " to " + outputPath);
	}

	public async Task<string[]> ParseM3u8Async()
	{
		ValidateM3u8();
		Console.WriteLine("Parsing M3U8 playlist from " + _filePath);
		new List<string>();
		HLSStreamer streamer = new HLSStreamer();
		streamer.PlaylistParsed += delegate(List<HlsSegment> segments)
		{
			Console.WriteLine($"Loaded playlist with {segments.Count} segments");
			foreach (HlsSegment current in segments)
			{
				Console.WriteLine(current.Title, current.Duration);
			}
		};
		streamer.ErrorOccurred += delegate(string error)
		{
			Console.WriteLine("Error: " + error);
		};
		streamer.StreamEnded += delegate
		{
			Console.WriteLine("Stream ended");
		};
		streamer.StartStreamingAsync(cancellationToken: new CancellationTokenSource().Token, m3u8Url: _filePath);
		await Task.Delay(30000);
		streamer.StopStreaming();
		streamer.Dispose();
		return Array.Empty<string>();
	}

	private void Stream_StreamEnded()
	{
		throw new NotImplementedException();
	}

	public Stream GetStream()
	{
		ValidateM3u8();
		Console.WriteLine("Getting stream from " + _filePath);
		return Stream.Null;
	}

	private void ValidateM3u8()
	{
		if (!IsM3u8)
		{
			throw new InvalidOperationException("Operation available only for M3U8 streams");
		}
	}

	public override string ToString()
	{
		return "Video type: " + (IsAvi ? "AVI" : IsMp4 ? "MP4" : IsM3u8 ? "M3U8" : "Unknown");
	}
}
