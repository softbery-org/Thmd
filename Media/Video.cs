// Version: 0.1.1.18
using System;
using System.ComponentModel;
using System.IO;
using MediaToolkit;
using MediaToolkit.Model;
using Thmd.Logs;

namespace Thmd.Media;

[Serializable]
public class Video : INotifyPropertyChanged
{
	private Uri _uri;

	private string _name;

	private double _position = 0.0;

	private double _volume = 1.0;

	private double _duration;

	private IPlayer _player;

	private double _fps;

	private Metadata _metadataMediaToolkit;

	private string _format;

	private string _frameSize;

	private string _audioFormat;

	private string _audioSampleRate;

	private int _audioBitRate;

	private string _audioChanelOutput;

	public object MediaType { get; private set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			OnPropertyChanged("Name");
		}
	}

	public Uri Uri => _uri;

	public TimeSpan Duration
	{
		get
		{
			string stringTime = TimeSpan.FromMilliseconds(_duration).ToString("hh\\:mm\\:ss");
			return TimeSpan.Parse(stringTime);
		}
	}

	public IPlayer Player => _player;

	public double Fps => GetFPS();

	public string Format => GetFormat();

	public string FrameSize => GetFrameSize();

	public string AudioFormat => GetAudioFormat();

	public string AudioSampleRate => GetAudioSampleRate();

	public int AudioBitRate => GetAudioBitRate();

	public string AudioChanelOutput => GetAudioChanelOutput();

	public IMediaStream MediaStream { get; private set; }

	public double Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (value >= 0.0 && value <= _duration)
			{
				_position = value;
				OnPositionChanged(value);
				OnPropertyChanged("Position");
				OnPropertyChanged("PositionFormatted");
			}
		}
	}

	public string PositionFormatted => TimeSpan.FromMilliseconds(_position).ToString("hh\\:mm\\:ss");

	public double Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			if (value >= 0.0 && value <= 1.0)
			{
				_volume = value;
				OnVolumeChanged(value);
				OnPropertyChanged("Volume");
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public event EventHandler<double> PositionChanged;

	public event EventHandler<double> VolumeChanged;

	public event EventHandler<IPlayer> PlayerChanged;

	protected virtual void OnPropertyChanged(string propertyName)
	{
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual void OnPositionChanged(double newPosition)
	{
		Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, $"CurrentTime change event: {newPosition}");
        PositionChanged?.Invoke(this, newPosition);
	}

	protected virtual void OnVolumeChanged(double newVolume)
	{
		Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, $"Volume change: {newVolume}");
        VolumeChanged?.Invoke(this, newVolume);
	}

	protected virtual void OnPlayerChanged(IPlayer player)
	{
		Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, $"Player change event: {player}");
        PlayerChanged?.Invoke(this, player);
	}

	public Video(string path)
	{
		_uri = new Uri(path);
		_name = new FileInfo(_uri.LocalPath).Name;
		_metadataMediaToolkit = GetMetadata();
		if (_metadataMediaToolkit != null)
		{
			_fps = GetFPS();
			_duration = GetDuration();
			_format = GetFormat();
			_frameSize = GetFrameSize();
			_audioFormat = GetAudioFormat();
			_audioSampleRate = GetAudioSampleRate();
			_audioBitRate = GetAudioBitRate();
		}
		else
		{
			_fps = 0.0;
			_duration = 0.0;
			_format = string.Empty;
			_frameSize = string.Empty;
			_audioFormat = string.Empty;
			_audioSampleRate = string.Empty;
			_audioBitRate = 0;
			_audioChanelOutput = string.Empty;
		}
	}

	public Video(string path, IPlayer player)
		: this(path)
	{
		_player = player;
	}

	public void Dispose()
	{
		_player.Dispose();
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Disposing: " + Name);
	}

	public void SetPlayer(IPlayer player)
	{
		try
		{
			_player = player;
			OnPlayerChanged(player);
		}
		catch (Exception ex)
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, $"Error with player set in media class. In media: {Uri}. {ex.Message}");
		}
	}

	public void Play()
	{
		_player.Play(this);
		Logger.Log.Log(LogLevel.Info, new string[]{"Console", "File"}, "[" + GetType().Name + "]: Playing media " + Name);
	}

	public void Pause()
	{
		_player.Pause();
        Logger.Log.Log(LogLevel.Info, new string[] { "Console", "File" }, "[" + GetType().Name + "]: Pause media " + Name);
    }

	public void Stop()
	{
		_player.Stop();
		Position = 0.0;
        Logger.Log.Log(LogLevel.Info, new string[] { "Console", "File" }, "[" + GetType().Name + "]: Stopped media " + Name);
	}

	public void Forward(double seconds)
	{
		Position += seconds;
		Logger.Log.Log(LogLevel.Info, new string[] { "Console", "File" }, $"[{GetType().Name}]: Change position to forward with +{seconds} second(s)");
	}

	public void Rewind(double seconds)
	{
		Position -= seconds;
		Logger.Log.Log(LogLevel.Info, new string[] { "Console", "File" }, $"[{GetType().Name}]: Rewind position by -{seconds} second(s)");
	}

	private Metadata GetMetadata()
	{
		if (_uri == null || string.IsNullOrEmpty(_uri.LocalPath) || !File.Exists(_uri.LocalPath))
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Invalid media file path: " + _uri?.LocalPath);
			return null;
		}
		try
		{
			MediaFile inputFile = new MediaFile
			{
				Filename = _uri.LocalPath
			};
			using (Engine engine = new Engine())
			{
				engine.GetMetadata(inputFile);
			}
			Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get metadata for media: " + _uri.LocalPath);
			return inputFile.Metadata;
		}
		catch (Exception exception)
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Error getting metadata for media: " + _uri.LocalPath, exception);
			return null;
		}
	}

	private double GetDuration()
	{
		double duration = _metadataMediaToolkit.Duration.TotalMilliseconds;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {TimeSpan.FromMilliseconds(duration)} duration");
		return duration;
	}

	private double GetFPS()
	{
		double fps = _metadataMediaToolkit.VideoData.Fps;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {fps} frame_size");
		return fps;
	}

	private string GetFormat()
	{
		string format = _metadataMediaToolkit.VideoData.Format;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + format + " format");
		return format;
	}

	private string GetFrameSize()
	{
		string frame_size = _metadataMediaToolkit.VideoData.FrameSize;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + frame_size + " frame_size");
		return frame_size;
	}

	private string GetAudioFormat()
	{
		string format = _metadataMediaToolkit.AudioData.Format;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + format + " audio_format");
		return format;
	}

	private string GetAudioSampleRate()
	{
		string rate = _metadataMediaToolkit.AudioData.SampleRate;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + rate + " audio_sample_rate");
		return rate;
	}

	private int GetAudioBitRate()
	{
		int rate = _metadataMediaToolkit.AudioData.BitRateKbs;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {rate} audio_bit_rate");
		return rate;
	}

	private string GetAudioChanelOutput()
	{
		string chanel_output = _metadataMediaToolkit.AudioData.ChannelOutput;
		Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + chanel_output + " audio_channel_output");
		return chanel_output;
	}

	public override string ToString()
	{
		return $"Name: {Name}, Duration: {Duration}, Format: {Format}";
	}
}
