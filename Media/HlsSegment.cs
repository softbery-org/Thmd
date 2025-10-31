// Version: 0.1.16.97
using System;
using System.Collections.Generic;

namespace Thmd.Media;

/// <summary>
/// Represents a single media segment in an HLS (HTTP Live Streaming) playlist.
/// Stores metadata such as duration, title, URI, tags, and discontinuity markers
/// used to describe and manage HLS streaming segments.
/// </summary>
public class HlsSegment
{
	/// <summary>
	/// Gets or sets the title of the segment, if available in the playlist metadata.
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Gets or sets a human-readable description of the segment.
	/// This value is optional and may not be provided in most playlists.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Gets or sets the URL associated with the segment.
	/// This may be used interchangeably with <see cref="Uri"/> for backward compatibility.
	/// </summary>
	public string Url { get; set; }

	/// <summary>
	/// Gets or sets the playback duration of the segment.
	/// Derived from the <c>#EXTINF</c> tag in the M3U8 playlist.
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// Gets or sets the absolute URI of the segment.
	/// Constructed from the base URI and relative path found in the M3U8 playlist.
	/// </summary>
	public string Uri { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this segment marks a discontinuity
	/// in the stream, typically corresponding to a change in encoding, bitrate,
	/// or timestamp sequence.
	/// </summary>
	public bool IsDiscontinuity { get; set; }

	/// <summary>
	/// Gets or sets additional HLS tags associated with this segment.
	/// Keys correspond to tag names (e.g., <c>#EXT-X-BYTERANGE</c>), and values
	/// contain the tag’s data.
	/// </summary>
	public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}
