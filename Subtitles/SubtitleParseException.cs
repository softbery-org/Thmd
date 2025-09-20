// Version: 0.1.12.50
using System;

namespace Thmd.Subtitles;

public class SubtitleParseException : Exception
{
	public SubtitleParseException()
	{
	}

	public SubtitleParseException(string message)
		: base(message)
	{
	}

	public SubtitleParseException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
