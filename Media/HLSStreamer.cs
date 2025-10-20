// Version: 0.1.17.11
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Thmd.Media
{
    /// <summary>
    /// Handles HLS (HTTP Live Streaming) streaming.
    /// Downloads the .m3u8 playlist, fetches video segments, and raises events for updates.
    /// </summary>
    public class HLSStreamer : IDisposable
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private List<HlsSegment> _segments;
        private Uri _playlistUri;

        /// <summary>
        /// Occurs when a video segment has been downloaded.
        /// </summary>
        public event Action<byte[]> SegmentDownloaded;

        /// <summary>
        /// Occurs when the HLS playlist has been parsed.
        /// </summary>
        public event Action<List<HlsSegment>> PlaylistParsed;

        /// <summary>
        /// Occurs when an error happens during streaming or segment download.
        /// </summary>
        public event Action<string> ErrorOccurred;

        /// <summary>
        /// Occurs when the streaming ends.
        /// </summary>
        public event Action StreamEnded;

        /// <summary>
        /// Initializes a new instance of the <see cref="HLSStreamer"/> class.
        /// </summary>
        public HLSStreamer()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HLS-Streamer/1.0");
        }

        /// <summary>
        /// Starts streaming HLS from the specified .m3u8 playlist URL.
        /// </summary>
        /// <param name="m3u8Url">The URL of the HLS playlist (.m3u8).</param>
        /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
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
                        if (_cts.IsCancellationRequested)
                            break;

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
                        break;

                    await Task.Delay(TimeSpan.FromSeconds(_segments.FirstOrDefault()?.Duration.TotalSeconds ?? 10.0), _cts.Token);
                }

                StreamEnded?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("Streaming failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Stops the streaming operation.
        /// </summary>
        public void StopStreaming()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Downloads the HLS playlist content as a string.
        /// </summary>
        /// <param name="uri">The URI of the HLS playlist.</param>
        /// <returns>The content of the HLS playlist.</returns>
        /// <exception cref="HttpRequestException">Thrown if downloading the playlist fails.</exception>
        private async Task<string> DownloadPlaylistAsync(Uri uri)
        {
            try
            {
                return await _httpClient.GetStringAsync(uri);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("Playlist download error: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Releases resources used by the <see cref="HLSStreamer"/>.
        /// </summary>
        public void Dispose()
        {
            _cts?.Cancel();
            _httpClient?.Dispose();
        }
    }
}
