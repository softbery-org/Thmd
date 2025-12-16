// IPlaylistConfig.cs
// Version: 0.1.17.23
// A class representing the configuration settings for playlists in the application.
// Stores properties such as default playlist path, shuffle mode, repeat mode, auto-play settings,
// and a list of media file paths or URIs.

using System.Collections.Generic;
using System.Windows;

using LibVLCSharp.Shared;

using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Configuration;

/// <summary>
/// A class representing the configuration settings for playlists in the application.
/// </summary>
public class PlaylistConfig : IPlaylistConfig, IConfig
{
    private readonly object _lock = new();
    /// <summary>
    /// Playlist name.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled for playlist playback.
    /// </summary>
    public bool EnableShuffle { get; set; } = true;
    /// <summary>
    /// Gets or sets the repeat mode for playlist playback (e.g., None, One, All, Random).
    /// </summary>
    public string Repeat { get; set; } = "None";
    /// <summary>
    /// Gets or sets a value indicating whether auto-play is enabled, starting playback automatically when a playlist is loaded.
    /// </summary>
    public bool AutoPlay { get; set; } = true;
    /// <summary>
    /// Gets or sets the list of media file paths or URIs in the playlist.
    /// </summary>
    public List<string> MediaList { get; set; } = new List<string>();
    /// <summary>
    /// Gets or sets the position of the playlist window.
    /// </summary>
    public Point Position { get; set; }
    /// <summary>
    /// Gets or sets the size of the playlist window.
    /// </summary>
    public Size Size { get; set; }
    /// <summary>
    /// Gets or sets the list of subtitle file paths or URIs in the playlist.
    /// </summary>
    public List<string> Subtitles { get; set; } = new List<string>();
    /// <summary>
    /// Gets or sets a value indicating whether subtitles are visible during playback.
    /// </summary>
    public bool SubtitleVisible {get;set;}=true;
    /// <summary>
    /// Gets or sets the index of the currently playing media item in the playlist.
    /// </summary>
    public int Current { get; set; }
    /// <summary>
    /// Gets or sets the list of video indents associated with the playlist.
    /// </summary>
    public List<VideoIndent> Indents { get; set; }

    /// <summary>
    /// Loads the playlist configuration from the JSON file.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            var loadedConfig = Config.LoadFromJsonFile<PlaylistConfig>(Config.PlaylistConfigPath);
            Name = loadedConfig.Name;
            Current = loadedConfig.Current;
            EnableShuffle = loadedConfig.EnableShuffle;
            Repeat = loadedConfig.Repeat;
            AutoPlay = loadedConfig.AutoPlay;
            MediaList = loadedConfig.MediaList;
            Subtitles = loadedConfig.Subtitles;
            SubtitleVisible = loadedConfig.SubtitleVisible;
            Indents = loadedConfig.Indents;
        }
    }

    /// <summary>
    /// Saves the playlist configuration to the JSON file.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            Config.SaveToFile(Config.PlaylistConfigPath, this);
        }
    }
}

/// <summary>
/// Represents the configuration settings for playlists in the application.
/// Provides properties to define the default playlist path, shuffle mode, repeat mode,
/// auto-play behavior, and a list of media file paths or URIs.
/// </summary>
public interface IPlaylistConfig
{
    /// <summary>
    /// Gets or sets the position of the playlist window.
    /// </summary>
    public Point Position { get; set; }
    /// <summary>
    /// Gets or sets the size of the playlist window.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the list of subtitle file paths or URIs in the playlist.
    /// </summary>
    public List<string> Subtitles { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether subtitles are visible during playback.
    /// </summary>
    public bool SubtitleVisible { get; set; }
    /// <summary>
    /// Gets or sets the index of the currently playing media item in the playlist.
    /// </summary>
    public int Current { get; set; }
    /// <summary>
    /// Gets or sets the list of video indents associated with the playlist.
    /// </summary>
    public List<VideoIndent> Indents { get; set; }
}
