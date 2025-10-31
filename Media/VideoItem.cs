// Version: 0.1.17.2
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

using MediaToolkit;
using MediaToolkit.Model;

using Thmd.Consolas;
using Thmd.Controls;
using Thmd.Logs;

namespace Thmd.Media;

/// <summary>
/// Video item, media
/// </summary>
[Serializable]
public class VideoItem : UIElement, INotifyPropertyChanged
{
    private int _index = 0;
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
    private bool _isPlaying;
    private List<VideoIndent> _indent = new List<VideoIndent>();
    private bool _isIndents = false;

    /// <summary>
    /// Is video playing
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
    }

    public bool isIndents {
        get => _isIndents;
        set {
            _isIndents = value;
            OnPropertyChanged(nameof(isIndents));
        } }

    public List<VideoIndent> Indents {  get => _indent; 
        set { 
            _indent = value;
            OnPropertyChanged(nameof(Indents));
        } }
    /// <summary>
    /// MediaType property representing the type of media (e.g., video, audio, etc.).
    /// </summary>
    public object MediaType { get; private set; }
    /// <summary>
    /// Video item id.
    /// </summary>
    public int Id { get => _index; }
    /// <summary>
    /// BaseString property representing the media name.
    /// </summary>
    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            _name = value;
            OnPropertyChanged("BaseString");
        }
    }
    /// <summary>
    /// URI property representing the media file path.
    /// </summary>
    public Uri Uri => _uri;
    /// <summary>
    /// Duration property in TimeSpan format.
    /// </summary>
    public TimeSpan Duration
    {
        get
        {
            string stringTime = TimeSpan.FromMilliseconds(_duration).ToString("hh\\:mm\\:ss");
            return TimeSpan.Parse(stringTime);
        }
    }
    /// <summary>
    /// Player instance associated with this media item.
    /// </summary>
    public IPlayer Player => _player;
    /// <summary>
    /// Frames per second (FPS) of the video, e.g., 24.0, 30.0, 60.0, etc.
    /// </summary>
    public double Fps => GetFPS();
    /// <summary>
    /// Video format, e.g., "mp4", "mkv", "avi", etc.
    /// </summary>
    public string Format => GetFormat();
    /// <summary>
    /// Frame size, e.g., "1920x1080", "1280x720", etc.
    /// </summary>
    public string FrameSize => GetFrameSize();
    /// <summary>
    /// Audio format, e.g., "mp3", "aac", "ac3", etc.
    /// </summary>
    public string AudioFormat => GetAudioFormat();
    /// <summary>
    /// Audio sample rate in Hz, e.g., "44100 Hz", "48000 Hz", etc.
    /// </summary>
    public string AudioSampleRate => GetAudioSampleRate();
    /// <summary>
    /// Audio bit rate in kbps, e.g., 128, 256, etc.
    /// </summary>
    public int AudioBitRate => GetAudioBitRate();
    /// <summary>
    /// Audio channel output, e.g., "stereo", "5.1", etc.
    /// </summary>
    public string AudioChanelOutput => GetAudioChanelOutput();
    /// <summary>
    /// Media stream information, if no media stream is found, it will be null.
    /// </summary>
    public IMediaStream MediaStream { get; private set; }
    /// <summary>
    /// Subtitle file path, if no subtitle is found, it will be an empty string.
    /// </summary>
    public string SubtitlePath { get; set; }
    /// <summary>
    /// Position property in milliseconds, range from 0.0 to Duration.
    /// </summary>
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

    /// <summary>
    /// Get the formatted position as a string in "hh:mm:ss" format.
    /// </summary>
    public string PositionFormatted => TimeSpan.FromMilliseconds(_position).ToString("hh\\:mm\\:ss");
    /// <summary>
    /// Volume property, range from 0.0 (mute) to 1.0 (max volume).
    /// </summary>
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

    /// <summary>
    /// Invoke when a property is changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Invoke when the position is changed.
    /// </summary>
    public event EventHandler<double> PositionChanged;

    /// <summary>
    /// Invoke when the volume is changed.
    /// </summary>
    public event EventHandler<double> VolumeChanged;

    /// <summary>
    /// Invoke when the player instance is changed.
    /// </summary>
    public event EventHandler<IPlayer> PlayerChanged;

    /// <summary>
    /// Invoke when a property is changed.
    /// </summary>
    /// <param name="propertyName">property name in string</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Invoke when the position is changed.
    /// </summary>
    /// <param name="newPosition">New position</param>
    protected virtual void OnPositionChanged(double newPosition)
    {
        this.WriteLine($"Position change event: {newPosition}");
        PositionChanged?.Invoke(this, newPosition);
    }

    /// <summary>
    /// Invoke when the volume is changed.
    /// </summary>
    /// <param name="newVolume">double volume value</param>
    protected virtual void OnVolumeChanged(double newVolume)
    {
        this.WriteLine($"Volume change: {newVolume}");
        VolumeChanged?.Invoke(this, newVolume);
    }

    /// <summary>
    /// Invoke when the player instance is changed.
    /// </summary>
    /// <param name="player">IPlayer interface</param>
    protected virtual void OnPlayerChanged(IPlayer player)
    {
        this.WriteLine($"Player change event: {player}");
        PlayerChanged?.Invoke(this, player);
    }

    /// <summary>
    /// Initialize a new instance of the VideoItem class with a specified file path.
    /// </summary>
    /// <param name="path">Media path</param>
    public VideoItem(string path)
    {
        _index++;
        _uri = new Uri(path);
        _name = new FileInfo(_uri.LocalPath).Name;
        LoadMetadata();
        AutoSetSubtitle(path);
    }

    /// <summary>
    /// Initialize a new instance of the VideoItem class with a specified media player.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="player"></param>
    public VideoItem(string path, IPlayer player)
        : this(path)
    {
        _player = player;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItem"/> class with the specified file path and metadata
    /// loading option.
    /// </summary>
    /// <remarks>If <paramref name="deferMetadata"/> is set to <see langword="false"/>, metadata for the video
    /// is loaded immediately. Otherwise, metadata loading is deferred until explicitly triggered.</remarks>
    /// <param name="path">The file path of the video. This must be a valid URI or a local file path.</param>
    /// <param name="deferMetadata">A value indicating whether to defer loading metadata for the video.  <see langword="true"/> to defer metadata
    /// loading; otherwise, <see langword="false"/>.</param>
    public VideoItem(string path, bool deferMetadata)
    {
        _index++;
        _uri = new Uri(path);
        _name = new FileInfo(_uri.LocalPath).Name;

        if (!deferMetadata)
        {
            LoadMetadata();
        }
    }

    private void LoadMetadata()
    {
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

    /// <summary>
    /// Asynchronously loads metadata and updates the associated properties.
    /// </summary>
    /// <remarks>This method retrieves metadata in a background task and updates the  <see cref="Duration"/>,
    /// <see cref="Format"/>, <see cref="Fps"/>, and  <see cref="FrameSize"/> properties upon completion. It is designed
    /// to  avoid blocking the calling thread.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadMetadataAsync()
    {
        await Task.Run(() =>
        {
            LoadMetadata();
        });

        // Aktualizuj UI po zako�czeniu
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(Format));
        OnPropertyChanged(nameof(Fps));
        OnPropertyChanged(nameof(FrameSize));
    }


    /// <summary>
    /// Dispose the media item and release resources.
    /// </summary>
    public void Dispose()
    {
        _player.Dispose();
        Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Disposing: " + Name);
    }

    /// <summary>
    /// Set the media player instance for this media item.
    /// </summary>
    /// <param name="player"></param>
    public void SetPlayer(IPlayer player)
    {
        try
        {
            _player = player;
            OnPlayerChanged(player);
        }
        catch (Exception ex)
        {
            this.WriteLine($"Error with player set in media class. In media: {Uri}. {ex.Message}");
        }
    }

    private void AutoSetSubtitle(string path)
    {
        FileInfo file = new FileInfo(path);
        var tmp = ReturnNameWithoutExtension(file);
        var subtitle = String.Empty;

        if (File.Exists($"{tmp}.{Subtitles.SubtitleExtensions.txt}"))
        {
            SubtitlePath = $"{tmp}.{Subtitles.SubtitleExtensions.txt}";
            this.WriteLine($"Auto set subtitle {SubtitlePath}");
        }
        else if (File.Exists($"{tmp}.{Subtitles.SubtitleExtensions.sub}"))
        {
            SubtitlePath = $"{tmp}.{Subtitles.SubtitleExtensions.sub}";
            this.WriteLine($"Auto set subtitle {SubtitlePath}");
        }
        else if (File.Exists($"{tmp}.{Subtitles.SubtitleExtensions.srt}"))
        {
            SubtitlePath = $"{tmp}.{Subtitles.SubtitleExtensions.srt}";
            this.WriteLine($"Auto set subtitle {SubtitlePath}");
        }
        else
        {
            SubtitlePath = String.Empty;
            this.WriteLine("No auto subtitle.");
        }
    }

    private string ReturnNameWithoutExtension(FileInfo item)
    {
        return item.FullName.Remove(item.FullName.Length - item.Extension.Length, item.Extension.Length);
    }

    /// <summary>
    /// Start or resume media playback.
    /// </summary>
    public void Play()
    {
        _player.Play(this);
        this.WriteLine($"[{GetType().Name}]: Playing media {Name}");
    }

    /// <summary>
    /// Pause the media playback.
    /// </summary>
    public void Pause()
    {
        _player.Pause();
        this.WriteLine($"[{GetType().Name}]Pause media {Name}");
    }

    /// <summary>
    /// Stop the media playback and reset the position to the beginning.
    /// </summary>
    public void Stop()
    {
        _player.Stop();
        Position = 0.0;
        this.WriteLine($"[{GetType().Name}]Stopped media {Name}");
    }

    /// <summary>
    /// Move the media position forward by a specified number of seconds.
    /// </summary>
    /// <param name="seconds">In seconds</param>
    public void Forward(double seconds)
    {
        Position += seconds;
        Logger.Log.Log(LogLevel.Info, new string[] { "Console", "File" }, $"[{GetType().Name}]: Change position to forward with +{seconds} second(s)");
    }

    /// <summary>
    /// Rewind the media position by a specified number of seconds.
    /// </summary>
    /// <param name="seconds">in seconds</param>
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
        try
        {
            double duration = _metadataMediaToolkit.Duration.TotalMilliseconds;
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {TimeSpan.FromMilliseconds(duration)} duration");
            return duration;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return 0;
        }
    }

    private double GetFPS()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                double fps = _metadataMediaToolkit.VideoData.Fps;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {fps} frame_size");
                return fps;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return 0;
        }
    }

    private string GetFormat()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                string format = _metadataMediaToolkit.VideoData.Format;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + format + " format");
                return format;
            }
            return String.Empty;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return String.Empty;
        }
    }

    private string GetFrameSize()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                string frame_size = _metadataMediaToolkit.VideoData.FrameSize;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + frame_size + " frame_size");
                return frame_size;
            }
            return String.Empty;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return String.Empty;
        }
    }

    private string GetAudioFormat()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                string format = _metadataMediaToolkit.AudioData.Format;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + format + " audio_format");
                return format;
            }
            return String.Empty;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return String.Empty;
        }
    }

    private string GetAudioSampleRate()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                string rate = _metadataMediaToolkit.AudioData.SampleRate;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + rate + " audio_sample_rate");
                return rate;
            }
            return String.Empty;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return String.Empty;
        }
    }

    private int GetAudioBitRate()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                int rate = _metadataMediaToolkit.AudioData.BitRateKbs;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: Get media {rate} audio_bit_rate");
                return rate;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return 0;
        }
    }

    private string GetAudioChanelOutput()
    {
        try
        {
            if (_metadataMediaToolkit != null)
            {
                string chanel_output = _metadataMediaToolkit.AudioData.ChannelOutput;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "[" + GetType().Name + "]: Get media " + chanel_output + " audio_channel_output");
                return chanel_output;
            }
            return String.Empty;
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"[{GetType().Name}]: {ex.Message}");
            return String.Empty;
        }
    }
    /// <summary>
    /// Override ToString method to provide a string representation of the VideoItem.
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"BaseString: {Name}, Duration: {Duration}, Format: {Format}";
    }
}
