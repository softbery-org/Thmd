// Version: 0.1.0.18
using System;
using System.Collections.Generic;

namespace Thmd.Media;

public static class HlsPlaylistParser
{
	public static List<HlsSegment> ParsePlaylist(string m3u8Content, Uri baseUri)
	{
		List<HlsSegment> segments = new List<HlsSegment>();
		string[] lines = m3u8Content.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		HlsSegment currentSegment = null;
		string[] array = lines;
		foreach (string line in array)
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
