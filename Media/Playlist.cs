// Version: 0.1.0.18
using System;
using System.Collections.Generic;

namespace Thmd.Media;

public class Playlist : List<Video>
{
	private Video _current;

	private Video _next;

	private Video _previous;

	private TimeSpan _playlistDuration = TimeSpan.Zero;

	public string Name { get; set; }

	public string Description { get; set; }

	public List<Video> Media { get; private set; }

	public Video Current
	{
		get
		{
			return _current;
		}
		private set
		{
			_current = value;
		}
	}

	public Video Next
	{
		get
		{
			return _next;
		}
		private set
		{
			_next = value;
		}
	}

	public Video Previous
	{
		get
		{
			return _previous;
		}
		private set
		{
			_previous = value;
		}
	}

	public int CurrentIndex => Media.IndexOf(Current);

	public int NextIndex
	{
		get
		{
			int index = Media.IndexOf(Current) + 1;
			if (index >= Media.Count)
			{
				return 0;
			}
			return index;
		}
	}

	public int PreviousIndex
	{
		get
		{
			int index = Media.IndexOf(Current) - 1;
			if (index < 0)
			{
				return Media.Count - 1;
			}
			return index;
		}
	}

	public DateTime CreationDate { get; private set; }

	public string Title
	{
		get
		{
			return Name;
		}
		set
		{
			Name = value;
		}
	}

	public TimeSpan Duration => _playlistDuration;

	public Playlist(string name, string description = "")
	{
		Name = name;
		Description = description;
		Media = new List<Video>();
		CreationDate = DateTime.Now;
	}

	public new void Add(Video media)
	{
		if (media != null)
		{
			Media.Add(media);
			Console.WriteLine("Media \"" + media.Name + "\" has been added to the playlist \"" + Name + "\".");
		}
		else
		{
			Console.WriteLine("Cannot add a null media.");
		}
	}

	public void RemoveMedia(Video media)
	{
		if (media != null)
		{
			if (Media.Remove(media))
			{
				Console.WriteLine("Song \"" + media.Name + "\" has been removed from the playlist \"" + Name + "\".");
			}
			else
			{
				Console.WriteLine("Song \"" + media.Name + "\" was not found in the playlist \"" + Name + "\".");
			}
		}
		else
		{
			Console.WriteLine("Cannot remove a null mediia.");
		}
	}

	public void DisplayMedia()
	{
		if (Media.Count == 0)
		{
			Console.WriteLine("Playlist \"" + Name + "\" is empty.");
			return;
		}
		Console.WriteLine("Media in playlist \"" + Name + "\":");
		foreach (Video mediia in Media)
		{
			Console.WriteLine("- (" + mediia.ToString() + ")");
		}
	}

	public int GetMediaCount()
	{
		return Media.Count;
	}

	public TimeSpan GetTotalDuration()
	{
		TimeSpan totalDuration = TimeSpan.Zero;
		foreach (Video media in Media)
		{
			totalDuration += media.Duration;
		}
		return totalDuration;
	}
}
