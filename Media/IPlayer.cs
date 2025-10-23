// Version: 0.1.17.10
using System;
using System.ComponentModel;
using System.Windows;

using Thmd.Controls;
using Thmd.Repeats;

using LibVLCSharp.Shared;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace Thmd.Media
{
    /// <summary>
    /// Represents a media player with playback controls, playlist management, and event notifications.
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Gets the playlist view associated with the player.
        /// </summary>
        PlaylistView Playlist { get; }

        /// <summary>
        /// Gets or sets the visibility of the playlist UI.
        /// </summary>
        Visibility PlaylistVisibility { get; set; }

        /// <summary>
        /// Gets the control bar associated with the player.
        /// </summary>
        ControlBar ControlBar { get; }

        /// <summary>
        /// Gets or sets the current playback position.
        /// </summary>
        TimeSpan Position { get; set; }

        /// <summary>
        /// Gets or sets the current state of the player.
        /// </summary>
        VLCState State { get; set; }

        /// <summary>
        /// Gets or sets whether the player is currently playing.
        /// </summary>
        bool isPlaying { get; set; }

        /// <summary>
        /// Gets or sets whether the player is currently paused.
        /// </summary>
        bool isPaused { get; set; }

        /// <summary>
        /// Gets or sets whether the player is currently stopped.
        /// </summary>
        bool isStoped { get; set; }

        /// <summary>
        /// Gets or sets the visibility of subtitles.
        /// </summary>
        Visibility SubtitleVisibility { get; set; }

        /// <summary>
        /// Gets or sets the playback volume (0.0 - 100.0).
        /// </summary>
        double Volume { get; set; }

        /// <summary>
        /// Gets or sets whether the player is muted.
        /// </summary>
        bool isMute { get; set; }

        /// <summary>
        /// Gets or sets whether the player is in fullscreen mode.
        /// </summary>
        bool Fullscreen { get; set; }

        /// <summary>
        /// Starts or resumes playback.
        /// </summary>
        void Play();

        /// <summary>
        /// Plays the specified media item.
        /// </summary>
        /// <param name="media">The media item to play.</param>
        void Play(VideoItem media);

        /// <summary>
        /// Pauses playback.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops playback.
        /// </summary>
        void Stop();

        /// <summary>
        /// Skips to the next media item in the playlist.
        /// </summary>
        void Next();

        /// <summary>
        /// Goes back to the previous media item in the playlist.
        /// </summary>
        void Preview();

        /// <summary>
        /// Seeks the playback to the specified time position.
        /// </summary>
        /// <param name="time">The position to seek to.</param>
        void Seek(TimeSpan time);

        /// <summary>
        /// Sets the subtitle file to be displayed during playback.
        /// </summary>
        /// <param name="path">The path to the subtitle file.</param>
        void SetSubtitle(string path);

        /// <summary>
        /// Saves the current playlist configuration.
        /// </summary>
        void SavePlaylistConfig();

        /// <summary>
        /// Loads the playlist configuration.
        /// </summary>
        void LoadPlaylistConfig_Question();

        /// <summary>
        /// Gets the current video frame as a <see cref="BitmapSource"/>.
        /// </summary>
        /// <returns>The current video frame.</returns>
        BitmapSource GetCurrentFrame();

        /// <summary>
        /// Gets the video frame at the specified time as a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="time">Time span</param>
        /// <returns>bitmap from time span</returns>
        Bitmap GetFrameAt(TimeSpan time);

        /// <summary>
        /// Releases all resources used by the player.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when playback starts.
        /// </summary>
        event EventHandler<EventArgs> Playing;

        /// <summary>
        /// Occurs when playback stops.
        /// </summary>
        event EventHandler<EventArgs> Stopped;

        /// <summary>
        /// Occurs when the length of the media changes.
        /// </summary>
        event EventHandler<EventArgs> LengthChanged;

        /// <summary>
        /// Occurs when the playback time changes.
        /// </summary>
        event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;
    }
}
