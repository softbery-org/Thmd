// Version: 0.1.1.20
using System;

namespace Thmd.Subtitles;

public class Subtitle
{
	public int Id { get; }

	public TimeSpan StartTime { get; }

	public TimeSpan EndTime { get; }

	public string[] Items { get; }

	public Subtitle(int id, TimeSpan startTime, TimeSpan endTime, string[] text)
	{
		Id = id;
		StartTime = startTime;
		EndTime = endTime;
		Items = text;
	}

	public override string ToString()
	{
		return string.Format("[{0}] {1} --> {2}: {3}", Id, StartTime, EndTime, string.Join(" ", Items));
	}
}
