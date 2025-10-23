// Version: 0.1.17.13
using System;
using System.Collections.Generic;

namespace Thmd.Media;

/// <summary>
/// Provides functionality to parse HLS (HTTP Live Streaming) M3U8 playlist content.
/// Converts playlist data into a list of <see cref="HlsSegment"/> objects
/// containing metadata such as duration, URI, and discontinuity markers.
/// </summary>
public static class HlsPlaylistParser
{
	/// <summary>
	/// Parses the specified M3U8 playlist content into a collection of <see cref="HlsSegment"/> objects.
	/// </summary>
	/// <param name="m3u8Content">The raw M3U8 playlist content as a string.</param>
	/// <param name="baseUri">The base URI used to resolve relative segment paths.</param>
	/// <returns>
	/// A list of <see cref="HlsSegment"/> objects representing the parsed media segments.
	/// </returns>
	/// <remarks>
	/// The parser handles the following HLS tags:
	/// <list type="bullet">
	/// <item><description><c>#EXTINF</c> – Defines the duration of a media segment.</description></item>
	/// <item><description><c>#EXT-X-DISCONTINUITY</c> – Marks a boundary between segments.</description></item>
	/// <item><description>Any other tag (starting with <c>#</c>) is stored as a key-value pair in <see cref="HlsSegment.Tags"/>.</description></item>
	/// </list>
	/// Lines not starting with <c>#</c> are treated as segment URIs and combined with the <paramref name="baseUri"/> if relative.
	/// </remarks>
	public static List<HlsSegment> ParsePlaylist(string m3u8Content, Uri baseUri)
	{
		List<HlsSegment> segments = new List<HlsSegment>();
		string[] lines = m3u8Content.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		HlsSegment currentSegment = null;

		foreach (string line in lines)
		{
			string trimmedLine = line.Trim();

			if (trimmedLine.StartsWith("#EXTINF:"))
			{
				currentSegment = new HlsSegment();
				string durationPart = trimmedLine.Split(':')[1].Split(',')[0];
				currentSegment.Duration = TimeSpan.FromMilliseconds(double.Parse(durationPart));
			}
			else if (trimmedLine.StartsWith("#EXT-X-DISCONTINUITY"))
			{
				if (currentSegment != null)
				{
					currentSegment.IsDiscontinuity = true;
				}
			}
			else if (trimmedLine.StartsWith("#"))
			{
				if (currentSegment != null)
				{
					string[] parts = trimmedLine.Split(new char[1] { ':' }, 2);
					if (parts.Length > 1)
					{
						currentSegment.Tags[parts[0]] = parts[1];
					}
				}
			}
			else if (!trimmedLine.StartsWith("#") && currentSegment != null)
			{
				currentSegment.Uri = new Uri(baseUri, trimmedLine).AbsoluteUri;
				segments.Add(currentSegment);
				currentSegment = null;
			}
		}
		return segments;
	}
}
