// Version: 0.1.12.50
using System;
using System.ComponentModel;

namespace Thmd.Media;

public class MediaViewModel : INotifyPropertyChanged
{
	private TimeSpan _playbackPosition;

	public VideoItem Media { get; }

	public string Name => Media.Name;

	public TimeSpan Duration => Media.Duration;

	public double Fps => Media.Fps;

	public string FrameSize => Media.FrameSize;

	public string Format => Media.Format;

	public TimeSpan Position
	{
		get
		{
			return _playbackPosition;
		}
		set
		{
			_playbackPosition = value;
			OnPropertyChanged("Position");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public MediaViewModel(VideoItem media)
	{
		Media = media;
	}

	protected virtual void OnPropertyChanged(string propertyName)
	{
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public override string ToString()
	{
		return $"Name: {Name}, Duration: {Duration}, Position: {Position}";
	}
}
