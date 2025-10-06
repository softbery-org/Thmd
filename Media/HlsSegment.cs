// Version: 0.1.16.92
using System;
using System.Collections.Generic;

namespace Thmd.Media;

public class HlsSegment
{
	public string Title { get; set; }

	public string Description { get; set; }

	public string Url { get; set; }

	public TimeSpan Duration { get; set; }

	public string Uri { get; set; }

	public bool IsDiscontinuity { get; set; }

	public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}
