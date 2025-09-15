// PlayerMediaElement.xaml.cs
// Version: 0.1.3.41
// A custom UserControl for media playback using WPF MediaElement, integrated with a playlist, progress bar,
// control bar, and subtitle functionality. It supports play, pause, stop, seek, volume control,
// fullscreen toggling, and repeat modes including random playback, with event handling for
// mouse interactions and media state changes.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Thmd.Helpers;
using Thmd.Logs;
using Thmd.Media;
using Thmd.Media.Effects;
using Thmd.Repeats;

using Vlc.DotNet.Core;

namespace Thmd.Controls;

public partial class PlayerMediaElement : UserControl, IPlayer
{
    // MediaElement for media playback.
    private readonly MediaElement _mediaElement;

    // Grid to hold MediaElement, subtitle control, progress bar, control bar, and playlist.
    private Grid _grid = new Grid();

    // Current status of the media player (e.g., Play, Pause, Stop).
    private MediaPlayerStatus _playerStatus = MediaPlayerStatus.Stop;

    // Current playback position of the media.
    private TimeSpan _currentTime = TimeSpan.Zero;

    // Flag indicating if the mouse is moving.
    private bool _isMouseMove;

    // Current volume level of the media player.
    private double _volume;

    // Background worker to detect mouse inactivity.
    private BackgroundWorker _mouseNotMoveWorker;

    // Flag indicating if the audio is muted.
    private bool _isMuted;

    // Flag indicating if the player is in fullscreen mode.
    private bool _fullscreen = false;

    // Duration for seeking forward or backward.
    private TimeSpan _seekDuration = TimeSpan.FromSeconds(5.0);

    // Stores the last window stance before entering fullscreen.
    private WindowLastStance _lastWindowStance;

    // Random number generator for random repeat mode.
    private readonly Random _random = new Random();

    // Control for displaying subtitles.
    private SubtitleControl _subtitleControl;

    private TimerBox _timerBox;

    private InfoBox _infoBox;

    private bool _timerVisibility = true;

    private Visibility _subtitleVisibility = Visibility.Hidden;

    // Flags indicating the current playback state.
    private bool _playing = false;
    private bool _paused = false;
    private bool _stoped = true;

    // Timer to track position changes
    private readonly DispatcherTimer _positionTimer;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    /*
     * // System execution state flags to prevent system sleep during playback.
     * private const uint ES_CONTINUOUS = 2147483648u;
     * private const uint ES_SYSTEM_REQUIRED = 1u;
     * private const uint ES_DISPLAY_REQUIRED = 2u;
     * private const uint ES_AWAYMODE_REQUIRED = 64u;
     * // Combination to block sleep mode.
     * private const uint BLOCK_SLEEP_MODE = 2147483651u;
     * // Combination to allow sleep mode.
     * private const uint DONT_BLOCK_SLEEP_MODE = 2147483648u;
     */

    [Flags]
    private enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001,
        // Combination to block sleep mode.
        BLOCK_SLEEP_MODE = 2147483651u,
        // Combination to allow sleep mode.
        DONT_BLOCK_SLEEP_MODE = 2147483648u
    }

    public bool TimerVisibility { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds after which the control bar and progress bar are hidden if the mouse is inactive.
    /// </summary>
    public int MouseSleeps { get; set; } = 7;

    /// <summary>
    /// Gets or sets the playlist view associated with the player.
    /// </summary>
    public PlaylistView Playlist { get; set; }

    /// <summary>
    /// Gets or sets the progress bar control for displaying playback progress.
    /// </summary>
    public ProgressBarBox ProgressBar { get; set; }

    /// <summary>
    /// Gets or sets the control bar for media playback controls.
    /// </summary>
    public ControlBox ControlBox { get; set; }

    /// <summary>
    /// Gets or sets the duration for seeking forward or backward.
    /// </summary>
    public TimeSpan SeekDuration
    {
        get => _seekDuration;
        set => _seekDuration = value;
    }

    /// <summary>
    /// Gets or sets the current status of the media player (e.g., Play, Pause, Stop).
    /// </summary>
    public MediaPlayerStatus PlayerStatus
    {
        get => _playerStatus;
        set
        {
            if (_playerStatus != value)
            {
                _playerStatus = value;
                OnPropertyChanged("PlayerStatus");
            }
        }
    }

    /// <summary>
    /// Gets or sets the current playback position of the media.
    /// </summary>
    public TimeSpan Position
    {
        get => _currentTime;
        set => OnPropertyChanged("Position", ref _currentTime, value);
    }

    /// <summary>
    /// Gets the handle to the player control (not implemented).
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown as this property is not implemented.</exception>
    public IntPtr Handle
    {
        get => throw new NotImplementedException();
    }

    /// <summary>
    /// Gets or sets whether the player is in a playing state.
    /// </summary>
    public bool isPlaying
    {
        get => _playing;
        set
        {
            if (value)
            {
                _playing = true;
                SetThreadExecutionState(EXECUTION_STATE.BLOCK_SLEEP_MODE);
            }
            else
            {
                _playing = false;
                SetThreadExecutionState(EXECUTION_STATE.DONT_BLOCK_SLEEP_MODE);
            }
            OnPropertyChanged("isPlaying", ref _playing, value);
        }
    }

    /// <summary>
    /// Gets or sets whether the player is in a paused state.
    /// </summary>
    public bool isPaused
    {
        get => _paused;
        set => OnPropertyChanged(nameof(isPaused), ref _paused, value);
    }

    /// <summary>
    /// Gets or sets whether the player is in a stopped state.
    /// </summary>
    public bool isStoped
    {
        get => _stoped;
        set => OnPropertyChanged(nameof(isStoped), ref _stoped, value);
    }

    /// <summary>
    /// Gets or sets the visibility of the subtitle control.
    /// Displays an error message if no subtitle file is selected when attempting to show subtitles.
    /// </summary>
    public Visibility SubtitleVisibility
    {
        get => _subtitleVisibility;
        set
        {
            if (value == Visibility.Visible && string.IsNullOrEmpty(_subtitleControl.FilePath))
            {
                MessageBox.Show("No subtitle file selected.", "SetSubtitle Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                _subtitleVisibility = value;
                _subtitleControl.Visibility = _subtitleVisibility;
            }
            OnPropertyChanged(nameof(SubtitleVisibility), ref _subtitleVisibility, value);
        }
    }

    /// <summary>
    /// Gets or sets the volume level of the media player (0 to 100).
    /// </summary>
    public double Volume
    {
        get => _volume;
        set
        {
            if (value < 0.0)
            {
                value = 0.0;
            }
            else if (value > 100.0)
            {
                value = 100.0;
            }
            _volume = value;
            _mediaElement.Volume = value / 100.0; // MediaElement uses 0-1 scale
            OnPropertyChanged("Volume", ref _volume, value);
        }
    }

    public bool isMute { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool Fullscreen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ControlBar ControlBar => throw new NotImplementedException();

    public Visibility PlaylistVisibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<VlcMediaPlayerPlayingEventArgs> Playing;
    public event EventHandler<VlcMediaPlayerStoppedEventArgs> Stopped;
    public event EventHandler<VlcMediaPlayerLengthChangedEventArgs> LengthChanged;
    public event EventHandler<VlcMediaPlayerTimeChangedEventArgs> TimeChanged;

    public PlayerMediaElement()
    {
        InitializeComponent();

        _grid = new Grid();

        InitializeTimerBox();
        InitializeInfoBox();
        InitializeSubtitleControl();
        InitializeControlBar();
        InitializePlaylist();
        InitializeProgressBar();
        InitContentGrid();

        // Initialize MediaElement
        _mediaElement = new MediaElement
        {
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Manual,
            Stretch = Stretch.UniformToFill,
            Volume = 0.5, // Default 50%
            IsMuted = false
        };

        // Add MediaElement to the grid
        _grid.Children.Add(_mediaElement);

        // Event handlers for MediaElement
        _mediaElement.MediaEnded += OnMediaEnded;
        _mediaElement.MediaOpened += OnMediaOpened;
        // Remove _mediaElement.PositionChanged += OnPositionChanged;
        _mediaElement.BufferingStarted += OnBufferingStarted;
        _mediaElement.BufferingEnded += OnBufferingEnded;

        // Initialize DispatcherTimer for position updates
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // Update every 100ms
        };
        _positionTimer.Tick += OnPositionTimerTick;

        // Initialize other components
        _subtitleControl = new SubtitleControl();
        _timerBox = new TimerBox(this);

        // Mouse inactivity timer
       // _mouseNotMoveWorker = new BackgroundWorker();
        //_mouseNotMoveWorker.DoWork += (s, e) => HandleMouseInactivity();


        _mouseNotMoveWorker = new BackgroundWorker();
        _mouseNotMoveWorker.DoWork += MouseNotMoveWorker_DoWork;
        _mouseNotMoveWorker.RunWorkerAsync();

        // Base events
        MouseMove += OnMouseMove;
        Loaded += UserControl_Loaded;
    }
    /// <summary>
    /// Initializes the control bar.
    /// </summary>
    private void InitializeControlBar()
    {
        ControlBox = new ControlBox(this);
        ControlBox.VerticalAlignment = VerticalAlignment.Top;
        ControlBox.HorizontalAlignment = HorizontalAlignment.Left;
        var resizer = new ResizeControlHelper(ControlBox);

        _grid.Children.Add(ControlBox);
    }

    /// <summary>
    /// Initializes the playlist.
    /// </summary>
    private void InitializePlaylist()
    {
        Playlist = new PlaylistView(this)
        {
            Width = 600.0,
            Height = 350.0,
            Visibility = Visibility.Hidden
        };
        var resizer = new ResizeControlHelper(Playlist);

        _grid.Children.Add(Playlist);
    }

    /// <summary>
    /// Initializes the progress bar.
    /// </summary>
    private void InitializeProgressBar()
    {
        ProgressBar = new ProgressBarBox(this)
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        ProgressBar.MouseDown += ProgressBar_MouseDown;
        ProgressBar.MouseMove += ProgressBar_MouseMove;
        _grid.Children.Add(ProgressBar);
    }

    /// <summary>
    /// Handles mouse move on progress bar for time preview.
    /// </summary>
    private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
    {
        System.Windows.Point mousePosition = e.GetPosition(ProgressBar);
        double width = ProgressBar.ActualWidth;
        double position = mousePosition.X / width * ProgressBar.Maximum;
        double timeInMs = (double)(Playlist.Current?.Duration.TotalMilliseconds ?? 0) * position / ProgressBar.Maximum;
        TimeSpan time = TimeSpan.FromMilliseconds(timeInMs);
        ProgressBar.PopupText = $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ProgressBarMouseEventHandler(sender, e);
        }
    }

    /// <summary>
    /// Handles mouse down on progress bar for seeking.
    /// </summary>
    private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ProgressBarMouseEventHandler(sender, e);
    }

    /// <summary>
    /// Handles progress bar mouse events for seeking.
    /// </summary>
    private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
    {
        double position = e.GetPosition(sender as ProgressBarBox).X;
        double width = (sender as ProgressBarBox).ActualWidth;
        double result = position / width * (sender as ProgressBarBox).Maximum;
        double jumpToTime = (double)(Playlist.Current?.Duration.TotalMilliseconds ?? 0) * result / (sender as ProgressBarBox).Maximum;
        Seek(TimeSpan.FromMilliseconds(jumpToTime));
    }

    /// <summary>
    /// Initializes the info box.
    /// </summary>
    private void InitializeInfoBox()
    {
        _infoBox = new InfoBox(this);
        _infoBox.DrawInfoText(string.Empty);
        _grid.Children.Add(_infoBox);
    }

    /// <summary>
    /// Initializes the timer box.
    /// </summary>
    private void InitializeTimerBox()
    {
        _timerBox = new TimerBox(this)
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        TimerVisibility = true;
        _grid.Children.Add(_timerBox);
    }

    /// <summary>
    /// Initializes the subtitle control.
    /// </summary>
    private void InitializeSubtitleControl()
    {
        _subtitleControl = new SubtitleControl
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Hidden
        };
        _grid.Children.Add(_subtitleControl);
    }

    /// <summary>
    /// Sets the grid as content.
    /// </summary>
    private void InitContentGrid()
    {
        this.Content = _grid;
    }


    /// <summary>
    /// Handles the MediaEnded event to advance to the next track based on repeat mode.
    /// </summary>
    private void OnMediaEnded(object sender, RoutedEventArgs e)
    {
        // Implement repeat logic similar to VLC version
        var repeatControl = ControlBox?.RepeatControl ?? new RepeatControl();
        var repeatType = repeatControl.RepeatType;
        bool enableShuffle = repeatControl.EnableShuffle;

        Dispatcher.InvokeAsync(() =>
        {
            switch (repeatType)
            {
                case RepeatType.All:
                    if (enableShuffle)
                    {
                        int randomIndex = _random.Next(0, Playlist.Videos.Count);
                        Playlist.CurrentIndex = randomIndex;
                        Playlist.Current.Play();
                    }
                    else
                    {
                        Next();
                    }
                    break;
                case RepeatType.One:
                    // Restart current video
                    _mediaElement.Position = TimeSpan.Zero;
                    _mediaElement.Play();
                    break;
                case RepeatType.None:
                    Stop();
                    break;
            }
        });
    }

    /// <summary>
    /// Handles position changes to update current time and progress.
    /// </summary>
    private void OnPositionChanged(object sender, RoutedEventArgs e)
    {
        Position = _mediaElement.Position;
        ProgressBar.Value = Position.TotalMilliseconds;
        // Update timer
        _timerBox.Timer = FormatTime(Position) + " / " + FormatTime(ProgressBar.Duration);
        // Update subtitle position
        _subtitleControl.PositionTime = Position;
    }

    /// <summary>
    /// Handles buffering started (simplified, as MediaElement doesn't have direct cache like VLC).
    /// </summary>
    private void OnBufferingStarted(object sender, RoutedEventArgs e)
    {
        ProgressBar.BufforBarValue = 0; // Or handle as needed
    }

    /// <summary>
    /// Handles buffering ended.
    /// </summary>
    private void OnBufferingEnded(object sender, RoutedEventArgs e)
    {
        ProgressBar.BufforBarValue = 1.0; // Full buffer for simplicity
    }

    /// <summary>
    /// Handles the MediaOpened event to update duration, start playback, and start the position timer.
    /// </summary>
    private void OnMediaOpened(object sender, RoutedEventArgs e)
    {
        if (_mediaElement.NaturalDuration.HasTimeSpan)
        {
            ProgressBar.Duration = _mediaElement.NaturalDuration.TimeSpan;
            ProgressBar.Maximum = _mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
        }
        _playerStatus = MediaPlayerStatus.Play;
        isPlaying = true;
        _positionTimer.Start(); // Start the timer when media opens
    }

    /// <summary>
    /// Handles timer ticks to update playback position, progress bar, and subtitles.
    /// </summary>
    private void OnPositionTimerTick(object sender, EventArgs e)
    {
        if (_mediaElement != null)
        {
            Position = _mediaElement.Position;
            ProgressBar.Value = Position.TotalMilliseconds;
            _timerBox.Timer = FormatTime(Position) + " / " + FormatTime(ProgressBar.Duration);
            _subtitleControl.PositionTime = Position;
        }
    }

    /// <summary>
    /// Plays the specified media or the current playlist item.
    /// </summary>
    private void _Play(VideoItem media = null)
    {
        if (Playlist.Current == null)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Warning, new string[2] { "Console", "File" }, "Playlist is empty or current media is not set.");
            return;
        }
        if (media == null)
        {
            media = Playlist.Current;
        }
        try
        {
            _mediaElement.Source = media.Uri;
            _mediaElement.Play();
            isPlaying = true;
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, "Playing media: " + media.Name);
            AutoOpenSubtitle(media.Uri.LocalPath);
        }
        catch (Exception ex)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Error, new string[2] { "Console", "File" }, "Error while playing media: " + ex.Message);
        }
    }

    /// <summary>
    /// Pauses the current media playback.
    /// </summary>
    public void Pause()
    {
        try
        {
            _mediaElement.Pause();
            isPlaying = false;
            isPaused = true;
            _playerStatus = MediaPlayerStatus.Pause;
            _positionTimer.Stop(); // Stop the timer when paused
            Logger.Log.Log(Thmd.Logs.LogLevel.Debug, new string[2] { "File", "Console" }, "Media was paused.");
        }
        catch (Exception ex)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Error, new string[2] { "Console", "File" }, "Error while pausing media: " + ex.Message);
        }
    }

    /// <summary>
    /// Stops the current media playback.
    /// </summary>
    public void Stop()
    {
        try
        {
            _mediaElement.Stop();
            _mediaElement.Source = null;
            isPlaying = false;
            isPaused = false;
            isStoped = true;
            _playerStatus = MediaPlayerStatus.Stop;
            _positionTimer.Stop(); // Stop the timer when stopped
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "File", "Console" }, "Media was stopped.");
        }
        catch (Exception ex)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Error, new string[2] { "File", "Console" }, ex.Message ?? "");
        }
    }

    /// <summary>
    /// Plays the specified video.
    /// </summary>
    /// <param name="media">The video to play.</param>
    public void Play(VideoItem media)
    {
        _Play(media);
    }

    /// <summary>
    /// Plays the current video in the playlist.
    /// </summary>
    public void Play()
    {
        if (Playlist.Current == null)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Warning, new string[2] { "Console", "File" }, "Playlist is empty or current media is not set.");
            return;
        }
        _Play();
    }

    /// <summary>
    /// Plays the next video in the playlist.
    /// </summary>
    public void Next()
    {
        Stop();
        Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "File", "Console" }, "Next media is playing.");
        Playlist.MoveNext.Play();
    }

    /// <summary>
    /// Plays the previous video in the playlist.
    /// </summary>
    public void Preview()
    {
        Stop();
        Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "File", "Console" }, "Before media is playing.");
        Playlist.MovePrevious.Play();
    }

    /// <summary>
    /// Seeks to a specific time in the current media.
    /// </summary>
    /// <param name="time">The time to seek to.</param>
    public void Seek(TimeSpan time)
    {
        if (_mediaElement.NaturalDuration.HasTimeSpan && time >= TimeSpan.Zero && time <= _mediaElement.NaturalDuration.TimeSpan)
        {
            _mediaElement.Position = time;
        }
    }

    /// <summary>
    /// Seeks forward or backward by a specified duration.
    /// </summary>
    /// <param name="time">The duration to seek by.</param>
    /// <param name="direction">The direction to seek (Forward or Backward).</param>
    public void Seek(TimeSpan time, SeekDirection direction)
    {
        if (_mediaElement.NaturalDuration.HasTimeSpan)
        {
            TimeSpan newPosition;
            switch (direction)
            {
                case SeekDirection.Forward:
                    newPosition = _mediaElement.Position + time;
                    if (newPosition <= _mediaElement.NaturalDuration.TimeSpan)
                    {
                        _mediaElement.Position = newPosition;
                    }
                    break;
                case SeekDirection.Backward:
                    newPosition = _mediaElement.Position - time;
                    if (newPosition >= TimeSpan.Zero)
                    {
                        _mediaElement.Position = newPosition;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Loads and displays subtitles from a specified file path.
    /// </summary>
    /// <param name="path">The path to the subtitle file.</param>
    public void SetSubtitle(string path)
    {
        this.Dispatcher.InvokeAsync(() =>
        {
            _subtitleControl.FilePath = path;
            // Sync subtitle with position changes via PositionChanged event
        });
    }

    /// <summary>
    /// Automatically opens subtitle if available (e.g., .srt next to media file).
    /// </summary>
    /// <param name="mediaPath">Path to the media file.</param>
    private void AutoOpenSubtitle(string mediaPath)
    {
        string subtitlePath = Path.ChangeExtension(mediaPath, ".srt");
        if (File.Exists(subtitlePath))
        {
            SetSubtitle(subtitlePath);
        }
    }

    /// <summary>
    /// Formats TimeSpan to string like "HH:MM:SS".
    /// </summary>
    private string FormatTime(TimeSpan time)
    {
        return time.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyChanged<T>(string propertyName, ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// Handles user control loaded event.
    /// </summary>
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize UI elements
        // Add subtitle control to grid, etc.
    }

    /// <summary>
    /// Handles mouse inactivity to hide controls.
    /// </summary>
    private void HandleMouseInactivity()
    {
        // Implement timer-based hiding
    }

    /// <summary>
    /// Background worker to hide the control bar and progress bar after mouse inactivity.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void MouseNotMoveWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        bool val = true;
        while (val)
        {
            await IfMouseMoved();
            if (e.Cancel)
            {
                val = false;
            }
        }
    }

    /// <summary>
    /// Checks for mouse movement and hides controls after a period of inactivity.
    /// </summary>
    /// <returns>A task that returns true if the mouse moved, false otherwise.</returns>
    private async Task<bool> IfMouseMoved()
    {
        base.MouseMove += MouseMovedCallback;
        bool isMouseMove;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(MouseSleeps));
            isMouseMove = _isMouseMove;
        }
        finally
        {
            base.MouseMove -= MouseMovedCallback;
            await base.Dispatcher.InvokeAsync((Func<Task>)async delegate
            {
                await ControlBox.HideByStoryboard((Storyboard)ControlBox.FindResource("fadeOutControlBar"));
                await ProgressBar.HideByStoryboard((Storyboard)ProgressBar.FindResource("fadeOutProgressBar"));
                base.Cursor = Cursors.None;
            });
            _isMouseMove = false;
        }
        return isMouseMove;
        void MouseMovedCallback(object sender, MouseEventArgs e)
        {
            _isMouseMove = true;
        }
    }

    /// <summary>
    /// Shows the control bar and progress bar when the mouse moves.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private async void OnMouseMove(object sender, MouseEventArgs e)
    {
        _isMouseMove = true;
        await ControlBox.ShowByStoryboard((Storyboard)ControlBox.FindResource("fadeInControlBar"));
        await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
        base.Cursor = Cursors.Arrow;
    }

    /// <summary>
    /// Disposes of the player control, releasing resources.
    /// </summary>
    public void Dispose()
    {
        if (_mouseNotMoveWorker != null)
        {
            _mouseNotMoveWorker.CancelAsync();
            _mouseNotMoveWorker.Dispose();
        }
        _mediaElement.MediaEnded -= OnMediaEnded;
        _mediaElement.MediaOpened -= OnMediaOpened;
        _mediaElement.BufferingStarted -= OnBufferingStarted;
        _mediaElement.BufferingEnded -= OnBufferingEnded;
        _positionTimer.Stop(); // Stop the timer
        _positionTimer.Tick -= OnPositionTimerTick;
        _mediaElement.Stop();
        MouseMove -= OnMouseMove;
        Loaded -= UserControl_Loaded;
    }
}
