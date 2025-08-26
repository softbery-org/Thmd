// Version: 0.1.1.30
using System;
using System.Windows;
using Thmd.Controls;
using Thmd.Repeats;

namespace Thmd.Media;

public interface IPlayer
{
	PlaylistView Playlist { get; set; }

	IntPtr Handle { get; }

	ControlBarControl ControlBar { get; }

	MediaPlayerStatus PlayerStatus { get; set; }

	TimeSpan CurrentTime { get; set; }

	bool isPlaying { get; set; }

	bool isPaused { get; set; }

	bool isStoped { get; set; }

	Visibility SubtitleVisibility { get; set; }

	double Volume { get; set; }

	bool isMute { get; set; }

	void Play();

	void Play(Video media);

	void Pause();

	void Stop();

	void Next();

	void Preview();

	void Seek(TimeSpan time);

	void Subtitle(string path);

	void Dispose();
}
