// Version: 0.1.16.72
using System;
using System.ComponentModel;
using System.Windows;

using Thmd.Controls;
using Thmd.Repeats;

using LibVLCSharp.Shared;
using System.Windows.Media.Imaging;

namespace Thmd.Media;

public interface IPlayer
{
	PlaylistView Playlist { get; }
	Visibility PlaylistVisibility { get; set; }
	ControlBar ControlBar { get; }
    TimeSpan Position { get; set; }
	VLCState State { get; set; }
	bool isPlaying { get; set; }
	bool isPaused { get; set; }
	bool isStoped { get; set; }
	Visibility SubtitleVisibility { get; set; }
	double Volume { get; set; }
	bool isMute { get; set; }
	bool Fullscreen { get; set; }
	void Play();
	void Play(VideoItem media);
	void Pause();
	void Stop();
	void Next();
	void Preview();
	void Seek(TimeSpan time);
	void SetSubtitle(string path);
	void SavePlaylistConfig();
    void LoadPlaylistConfig();
	BitmapSource GetCurrentFrame();
    void Dispose();
	event PropertyChangedEventHandler PropertyChanged;
    event EventHandler<EventArgs> Playing;
    event EventHandler<EventArgs> Stopped;
    event EventHandler<EventArgs> LengthChanged;
    event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;
}
