// IPlaylistConfig.cs
// Version: 0.1.16.93
// A class representing the configuration settings for playlists in the application.
// Stores properties such as default playlist path, shuffle mode, repeat mode, auto-play settings,
// and a list of media file paths or URIs.

using System.Collections.Generic;
using System.Windows;

using LibVLCSharp.Shared;

using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Configuration;

public class PlaylistConfig : IPlaylistConfig
{
    public string Name { get; set; }
    public bool EnableShuffle { get; set; } = true;
    public string Repeat { get; set; } = "None";
    public bool AutoPlay { get; set; } = true;
    public List<string> MediaList { get; set; } = new List<string>();
    public Point Position { get; set; }
    public Size Size { get; set; }
    public List<string> Subtitles { get; set; } = new List<string>();
    public bool SubtitleVisible {get;set;}=true;
    public int Current { get; set; }
}

/// <summary>
/// Represents the configuration settings for playlists in the application.
/// Provides properties to define the default playlist path, shuffle mode, repeat mode,
/// auto-play behavior, and a list of media file paths or URIs.
/// </summary>
public interface IPlaylistConfig
{
    public Point Position { get; set; }
    public Size Size { get; set; }
    /// <summary>
    /// Playlist name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled for playlist playback.
    /// Defaults to false.
    /// </summary>
    public bool EnableShuffle { get; set; }

    /// <summary>
    /// Gets or sets the repeat mode for playlist playback (e.g., None, One, All, Random).
    /// Defaults to <see cref="RepeatType.None"/>.
    /// </summary>
    public string Repeat { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether auto-play is enabled, starting playback automatically when a playlist is loaded.
    /// Defaults to true.
    /// </summary>
    public bool AutoPlay { get; set; }

    /// <summary>
    /// Gets or sets the list of media file paths or URIs in the playlist.
    /// Defaults to an empty list.
    /// </summary>
    public List<string> MediaList { get; set; }
    public List<string> Subtitles { get; set; }
    public bool SubtitleVisible { get; set; }
    public int Current { get; set; }
}
