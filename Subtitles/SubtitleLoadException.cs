// Version: 0.1.17.20
using System;

namespace Thmd.Subtitles;

public class SubtitleLoadException : Exception
{
	public SubtitleLoadException()
	{
	}

	public SubtitleLoadException(string message)
		: base(message)
	{
	}

	public SubtitleLoadException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
