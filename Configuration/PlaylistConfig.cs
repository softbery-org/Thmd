// PlaylistConfig.cs
// Version: 0.1.1.86
// A class representing the configuration settings for playlists in the application.
// Stores properties such as default playlist path, shuffle mode, repeat mode, auto-play settings,
// and a list of media file paths or URIs.

using System.Collections.Generic;
using Thmd.Repeats;

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for playlists in the application.
/// Provides properties to define the default playlist path, shuffle mode, repeat mode,
/// auto-play behavior, and a list of media file paths or URIs.
/// </summary>
public class PlaylistConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled for playlist playback.
    /// Defaults to false.
    /// </summary>
    public bool EnableShuffle { get; set; } = false;

    /// <summary>
    /// Gets or sets the repeat mode for playlist playback (e.g., None, One, All, Random).
    /// Defaults to <see cref="RepeatType.None"/>.
    /// </summary>
    public RepeatType RepeatType { get; set; } = RepeatType.None;

    /// <summary>
    /// Gets or sets a value indicating whether auto-play is enabled, starting playback automatically when a playlist is loaded.
    /// Defaults to true.
    /// </summary>
    public bool AutoPlay { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of media file paths or URIs in the playlist.
    /// Defaults to an empty list.
    /// </summary>
    public List<string> MediaList { get; set; } = new List<string>();
}
