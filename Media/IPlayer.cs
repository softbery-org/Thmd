// Version: 0.1.8.96
using System;
using System.ComponentModel;
using System.Windows;

using Thmd.Controls;
using Thmd.Repeats;

using Vlc.DotNet.Core;

namespace Thmd.Media;

public interface IPlayer
{
	PlaylistView Playlist { get; }
	Visibility PlaylistVisibility { get; set; }
    ControlBox ControlBox { get; }
	ControlBar ControlBar { get; }
    TimeSpan Position { get; set; }
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
	void Dispose();
	event PropertyChangedEventHandler PropertyChanged;
    event EventHandler<VlcMediaPlayerPlayingEventArgs> Playing;
    event EventHandler<VlcMediaPlayerStoppedEventArgs> Stopped;
    event EventHandler<VlcMediaPlayerLengthChangedEventArgs> LengthChanged;
    event EventHandler<VlcMediaPlayerTimeChangedEventArgs> TimeChanged;
}
