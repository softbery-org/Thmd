// Version: 0.1.1.86
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Thmd.Media;

public class HLSStreamer : IDisposable
{
	private readonly HttpClient _httpClient;

	private CancellationTokenSource _cts;

	private List<HlsSegment> _segments;

	private Uri _playlistUri;

	public event Action<byte[]> SegmentDownloaded;

	public event Action<List<HlsSegment>> PlaylistParsed;

	public event Action<string> ErrorOccurred;

	public event Action StreamEnded;

	public HLSStreamer()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "HLS-Streamer/1.0");
	}

	public async Task StartStreamingAsync(string m3u8Url, CancellationToken cancellationToken = default)
	{
		try
		{
			_playlistUri = new Uri(m3u8Url);
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			while (!_cts.IsCancellationRequested)
			{
				_segments = HlsPlaylistParser.ParsePlaylist(await DownloadPlaylistAsync(_playlistUri), _playlistUri);
                PlaylistParsed?.Invoke(_segments);
				foreach (HlsSegment segment in _segments)
				{
					_ = segment;
					if (_cts.IsCancellationRequested)
					{
						break;
					}
					try
					{
						byte[] segmentData = await _httpClient.GetByteArrayAsync(_playlistUri);
                        SegmentDownloaded?.Invoke(segmentData);
					}
					catch (Exception ex)
					{
                        ErrorOccurred?.Invoke("Error downloading segment: " + ex.Message);
					}
				}
				if (_cts.IsCancellationRequested)
				{
					break;
				}
				await Task.Delay(TimeSpan.FromSeconds(_segments.FirstOrDefault()?.Duration.TotalSeconds ?? 10.0), _cts.Token);
			}
            StreamEnded?.Invoke();
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
            ErrorOccurred?.Invoke("Streaming failed: " + ex3.Message);
		}
	}

	public void StopStreaming()
	{
		_cts?.Cancel();
	}

	private async Task<string> DownloadPlaylistAsync(Uri uri)
	{
		try
		{
			return await _httpClient.GetStringAsync(uri);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
            ErrorOccurred?.Invoke("Playlist download error: " + ex2.Message);
			throw;
		}
	}

	public void Dispose()
	{
		_cts?.Cancel();
		HttpClient httpClient = _httpClient;
		if (httpClient != null)
		{
			httpClient.Dispose();
		}
	}
}
