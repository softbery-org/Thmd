// Version: 0.0.0.26
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using LibVLCSharp.Shared;

using Thmd.Views;

namespace Thmd.Media
{
    /// <summary>
    /// Represents a media player with playback controls and playlist management.
    /// </summary>
    public interface IPlay
    {
        /// <summary>
        /// Gets the playlist view associated with the player.
        /// </summary>
        Playlist Playlist { get; }

        /// <summary>
        /// Gets the control bar associated with the player.
        /// </summary>
        ControlBar Controlbar { get; }

        InfoBox InfoBox { get; }

        ProgressBarView ProgressBar { get; }

        SubtitleControl Subtitle { get; }

        /// <summary>
        /// Gets or sets the current playback position.
        /// </summary>
        TimeSpan Position { get; set; }

        /// <summary>
        /// Gets or sets player volume.
        /// </summary>
        double Volume { get; set; }

        /// <summary>
        /// Gets or sets whether the player is in fullscreen mode.
        /// </summary>
        bool isFullscreen { get; set; }
        bool isPlaying { get; set; }
        bool isPaused { get; set; }
        bool isMute { get; set; }
        bool isStopped { get; set; }
        event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;
        event EventHandler<EventArgs> Playing;
        event EventHandler<EventArgs> Paused;
        event EventHandler<EventArgs> Stopped;
        event EventHandler<MediaPlayerVolumeChangedEventArgs> VolumeChanged;

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
        /// Play media next, skipping list
        /// </summary>
        void PlayNext(VideoItem media);

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

        void TogglePlayPause();

        /// <summary>
        /// Seeks the playback to the specified time position.
        /// </summary>
        /// <param name="time">The position to seek to.</param>
        void Seek(TimeSpan time);

        void Seek(TimeSpan time, SeekDirection seek_direction);

        void Dispose();
    }
}
