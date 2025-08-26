// Player.xaml.cs
// Version: 0.1.1.58
// A custom UserControl for media playback using VLC, integrated with a playlist, progress bar,
// control bar, and subtitle functionality. It supports play, pause, stop, seek, volume control,
// fullscreen toggling, and repeat modes including random playback, with event handling for
// mouse interactions and media state changes.

using Microsoft.Win32;
using System;
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
using Thmd.Helpers;
using Thmd.Logs;
using Thmd.Media;
using Thmd.Repeats;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;

namespace Thmd.Controls;

/// <summary>
/// A custom UserControl for media playback using VLC, integrated with a playlist, progress bar,
/// control bar, and subtitle functionality. Provides methods for playing, pausing, stopping,
/// seeking, and controlling volume, as well as handling fullscreen mode, repeat modes (including random),
/// and mouse interactions. Implements IPlayer interface for media control operations.
/// </summary>
public partial class Player : UserControl, IPlayer
{
    // VLC control for media playback.
    private readonly VlcControl _vlcControl;

    // Grid to hold VLC control, subtitle control, progress bar, control bar, and playlist.
    private Grid _grid = new Grid();

    // One status of the media player (e.g., Play, Pause, Stop).
    private MediaPlayerStatus _playerStatus = MediaPlayerStatus.Stop;

    // One playback position of the media.
    private TimeSpan _currentTime = TimeSpan.Zero;

    // Flag indicating if the mouse is moving.
    private bool _isMouseMove;

    // Helper for resizing control elements.
    private readonly ResizeControlHelper _resizeHelper;

    // One volume level of the media player.
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

    // Flags indicating the current playback state.
    private bool _playing = false;
    private bool _paused = false;
    private bool _stoped = true;

    // System execution state flags to prevent system sleep during playback.
    private const uint ES_CONTINUOUS = 2147483648u;
    private const uint ES_SYSTEM_REQUIRED = 1u;
    private const uint ES_DISPLAY_REQUIRED = 2u;
    private const uint ES_AWAYMODE_REQUIRED = 64u;
    // Combination to block sleep mode.
    private const uint BLOCK_SLEEP_MODE = 2147483651u;
    // Combination to allow sleep mode.
    private const uint DONT_BLOCK_SLEEP_MODE = 2147483648u;


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
    public ProgressBarControl ProgressBar { get; set; }

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
    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => OnPropertyChanged("CurrentTime", ref _currentTime, value);
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
    /// Gets or sets whether the player is in a playing state (not implemented).
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown as this property is not implemented.</exception>
    public bool isPlaying
    {
        get => _playing;
        set {
            if (value)
            {
                _playing = true;
                SetThreadExecutionState(BLOCK_SLEEP_MODE);
            }
            else
            {
                _playing = false;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            }
            OnPropertyChanged("isPlaying", ref _playing, value);
        }
    }

    /// <summary>
    /// Gets or sets whether the player is in a paused state (not implemented).
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown as this property is not implemented.</exception>
    public bool isPaused
    {
        get => _paused;
        set => OnPropertyChanged(nameof(isPaused), ref _paused, value);
    }

    /// <summary>
    /// Gets or sets whether the player is in a stopped state (not implemented).
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown as this property is not implemented.</exception>
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
        get => _subtitleControl.Visibility;
        set
        {
            if (value == Visibility.Visible && string.IsNullOrEmpty(_subtitleControl.FilePath))
            {
                MessageBox.Show("No subtitle file selected.", "Subtitle Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                _subtitleControl.Visibility = value;
            }
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
            _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)value;
            OnPropertyChanged("Volume", ref _volume, value);
        }
    }

    /// <summary>
    /// Gets or sets whether the audio is muted.
    /// </summary>
    public bool isMute
    {
        get => _isMuted;
        set
        {
            if (value)
            {
                _isMuted = true;
                _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = 0;
            }
            else
            {
                _isMuted = false;
                _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)_volume;
            }
            OnPropertyChanged("isMute", ref _isMuted, value);
        }
    }

    /// <summary>
    /// Gets or sets whether the player is in fullscreen mode.
    /// </summary>
    public bool Fullscreen
    {
        get => _fullscreen;
        set
        {
            this.Fullscreen();
            _fullscreen = FullscreenHelper.IsFullscreen;
            OnPropertyChanged("Fullscreen", ref _fullscreen, value);
        }
    }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Sets the system execution state to prevent sleep during playback.
    /// </summary>
    /// <param name="esFlags">The execution state flags.</param>
    /// <returns>The previous execution state.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    /// <summary>
    /// Initializes a new instance of the <see cref="Player"/> class.
    /// Sets up the VLC control, progress bar, control bar, playlist, and subtitle control,
    /// and initializes event handlers for media playback and user interactions.
    /// </summary>
    public Player()
    {
        InitializeComponent();

        ProgressBar = new ProgressBarControl();
        ProgressBar.SetPlayer(this);
        ProgressBar.VerticalAlignment = VerticalAlignment.Bottom;
        ProgressBar.HorizontalAlignment = HorizontalAlignment.Stretch;
        ProgressBar.MouseDown += ProgressBar_MouseDown;
        ProgressBar.MouseMove += ProgressBar_MouseMove;

        ControlBox = new ControlBox();
        ControlBox.SetPlayer(this);
        _resizeHelper = new ResizeControlHelper(ControlBox);
        ControlBox.VerticalAlignment = VerticalAlignment.Top;
        ControlBox.HorizontalAlignment = HorizontalAlignment.Left;
        ControlBarButtonEvent();

        Playlist = new PlaylistView(this);
        Playlist.Width = 600.0;
        Playlist.Height = 350.0;
        Playlist.Visibility = Visibility.Hidden;
        _resizeHelper = new ResizeControlHelper(Playlist);

        string[] mediaOptions = new string[2] { "--no-video-title-show", "--no-xlib" };
        _vlcControl = new VlcControl();
        _vlcControl.SourceProvider.CreatePlayer(new DirectoryInfo(Path.Combine(Logger.Config.LibVlcPath, (IntPtr.Size == 4) ? "win-x86" : "win-x64")), mediaOptions);
        _vlcControl.SourceProvider.MediaPlayer.Playing += OnPlaying;
        _vlcControl.SourceProvider.MediaPlayer.Stopped += OnStopped;
        _vlcControl.SourceProvider.MediaPlayer.Paused += OnPaused;
        _vlcControl.SourceProvider.MediaPlayer.EndReached += OnEndReached;
        _vlcControl.SourceProvider.MediaPlayer.Buffering += OnBuffering;
        _vlcControl.SourceProvider.MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
        _volume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
        ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
        ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = _volume;
        ControlBox._playerBtnVolume._volumeProgressBar.MouseDown += VolumeProgressBar_MouseDown;
        ControlBox._playerBtnVolume._volumeProgressBar.MouseMove += VolumeProgressBar_MouseMove;

        _subtitleControl = new SubtitleControl(this);
        _subtitleControl.VerticalAlignment = VerticalAlignment.Bottom;
        _subtitleControl.HorizontalAlignment = HorizontalAlignment.Stretch;
        _subtitleControl.Margin = new Thickness(0.0, 0.0, 0.0, 30.0);
        _subtitleControl.Visibility = Visibility.Collapsed;

        base.Content = _grid;
        _grid.Children.Add(_vlcControl);
        _grid.Children.Add(_subtitleControl);
        _grid.Children.Add(ProgressBar);
        _grid.Children.Add(ControlBox);
        _grid.Children.Add(Playlist);

        _mouseNotMoveWorker = new BackgroundWorker();
        _mouseNotMoveWorker.DoWork += _mouseNotMoveWorker_DoWork;
        _mouseNotMoveWorker.RunWorkerAsync();

        base.MouseMove += OnMouseMove;
        base.Loaded += UserControl_Loaded;
    }

    /// <summary>
    /// Sets up event handlers for control bar buttons (play, stop, next, previous, volume, etc.).
    /// </summary>
    private void ControlBarButtonEvent()
    {
        ControlBox.BtnPlay.Click += delegate
        {
            if (_playerStatus == MediaPlayerStatus.Play)
            {
                Pause();
            }
            else if (_playerStatus == MediaPlayerStatus.Pause)
            {
                Play();
            }
        };
        ControlBox.BtnStop.Click += delegate
        {
            Stop();
        };
        ControlBox.BtnNext.Click += delegate
        {
            Next();
        };
        ControlBox.BtnPrevious.Click += delegate
        {
            Preview();
        };
        ControlBox.BtnClose.Click += async delegate
        {
            await ControlBox.HideByStoryboard((Storyboard)ControlBox.FindResource("fadeOutControlBar"));
            await ProgressBar.HideByStoryboard((Storyboard)ProgressBar.FindResource("fadeOutProgressBar"));
            base.Cursor = Cursors.None;
        };
        ControlBox.BtnVolumeUp.Click += delegate
        {
            if (_volume < 100.0)
            {
                _volume = ++_vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = (int)_volume;
                ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
            }
        };
        ControlBox.BtnVolumeDown.Click += delegate
        {
            if (_volume > 0.0)
            {
                _volume = --_vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = (int)_volume;
                ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
            }
        };
        ControlBox.BtnMute.Click += delegate
        {
            if (_vlcControl.SourceProvider.MediaPlayer.Audio.IsMute)
            {
                _vlcControl.SourceProvider.MediaPlayer.Audio.IsMute = false;
                ControlBox.BtnMute.Content = "isMute";
                ControlBox.BtnMute.Style = FindResource("isMute") as Style;
                ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
                _volume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
            }
            else
            {
                _vlcControl.SourceProvider.MediaPlayer.Audio.IsMute = true;
                ControlBox.BtnMute.Style = FindResource("Unmute") as Style;
                ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = 0.0;
                _volume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
            }
        };
        ControlBox.BtnOpen.Click += delegate
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Video files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.wmv|All files|*.*",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string[] fileNames = openFileDialog.FileNames;
                foreach (string path in fileNames)
                {
                    Playlist.Add(new Video(path));
                }
            }
        };
        ControlBox.BtnPlaylist.Click += delegate
        {
            if (Playlist.Visibility == Visibility.Visible)
            {
                Playlist.Visibility = Visibility.Hidden;
            }
            else
            {
                Playlist.Visibility = Visibility.Visible;
            }
        };
        ControlBox.BtnFullscreen.Click += delegate
        {
            if (_vlcControl.SourceProvider.MediaPlayer.Video.FullScreen)
            {
                _vlcControl.SourceProvider.MediaPlayer.Video.FullScreen = false;
                ControlBox.BtnFullscreen.Style = FindResource("FullscreenOn") as Style;
                Fullscreen = false;
            }
            else
            {
                _vlcControl.SourceProvider.MediaPlayer.Video.FullScreen = true;
                ControlBox.BtnFullscreen.Style = FindResource("FullscreenOff") as Style;
                Fullscreen = true;
            }
        };
        ControlBox.BtnSubtitle.Click += delegate
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Subtitle files|*.txt;*.sub;*.srt|All files|*.*",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string[] fileNames = openFileDialog.FileNames;
                foreach (string path in fileNames)
                {
                    Subtitle(path);
                }
            }
        };
    }

    /// <summary>
    /// Handles the Loaded event to initialize the progress bar and control bar with the current video's information.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
        ProgressBar.Value = 0.0;
        ProgressBar.ProgressText = "00 : 00 : 00/00 : 00 : 00";
        ControlBox.VideoName = Playlist.Current?.Name ?? "No video loaded";
        ControlBox.VideoTime = "00 : 00 : 00/00 : 00 : 00";
    }

    /// <summary>
    /// Handles mouse move events on the progress bar to display the time at the mouse position.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
    {
        Point mouse_position = e.GetPosition(ProgressBar);
        double width = ProgressBar.ActualWidth;
        double position = mouse_position.X / width * ProgressBar.Maximum;
        double time_in_ms = (double)_vlcControl.SourceProvider.MediaPlayer.Length * position / ProgressBar.Maximum;
        TimeSpan time = TimeSpan.FromMilliseconds(time_in_ms);
        ProgressBar.PopupText = $"{time.Hours:00} : {time.Minutes:00} : {time.Seconds:00}";

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ProgressBarMouseEventHandler(sender, e);
        }
    }

    /// <summary>
    /// Handles mouse down events on the progress bar to seek to the clicked position.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ProgressBarMouseEventHandler(sender, e);
    }

    /// <summary>
    /// Handles mouse move events on the volume progress bar to adjust the volume.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void VolumeProgressBar_MouseMove(object sender, MouseEventArgs e)
    {
        double position = e.GetPosition(sender as ProgressBarControl).X;
        double width = (sender as ProgressBarControl).ActualWidth;
        double result = position / width * (sender as ProgressBarControl).Maximum;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            (sender as ProgressBarControl)._progressBar.Value = result;
            (sender as ProgressBarControl).ProgressText = $"Volume: {(int)result}";
            int num = (_vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)result);
            _volume = num;
        }
    }

    /// <summary>
    /// Handles mouse down events on the volume progress bar to set the volume.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void VolumeProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        double position = e.GetPosition(sender as ProgressBarControl).X;
        double width = (sender as ProgressBarControl).ActualWidth;
        double result = position / width * (sender as ProgressBarControl).Maximum;
        (sender as ProgressBarControl)._progressBar.Value = result;
        (sender as ProgressBarControl).ProgressText = $"Volume: {(int)result}";
        Console.WriteLine($"Volume: {result}");
        int num = (_vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)result);
        _volume = num;
    }

    /// <summary>
    /// Handles mouse events on the progress bar to seek to a specific time.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
    {
        double position = e.GetPosition(sender as ProgressBarControl).X;
        double width = (sender as ProgressBarControl).ActualWidth;
        double result = ((sender as ProgressBarControl).Value = position / width * (sender as ProgressBarControl).Maximum);
        double jump_to_time = (double)_vlcControl.SourceProvider.MediaPlayer.Length * result / (sender as ProgressBarControl).Maximum;
        _vlcControl.SourceProvider.MediaPlayer.Time = (long)jump_to_time;
    }

    /// <summary>
    /// Handles time changes in the media player, updating the progress bar, control bar, and subtitles.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The time changed event arguments.</param>
    private void MediaPlayer_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
    {
        base.Dispatcher.InvokeAsync(delegate
        {
            Playlist.Current.Position = e.NewTime;
            ProgressBar.Duration = Playlist.Current.Duration;
            ProgressBar.ProgressText = string.Format("{0:00} : {1:00} : {2:00}/{3}", TimeSpan.FromMilliseconds(e.NewTime).Hours, TimeSpan.FromMilliseconds(e.NewTime).Minutes, TimeSpan.FromMilliseconds(e.NewTime).Seconds, ProgressBar.Duration.ToString("hh\\:mm\\:ss"));
            ProgressBar.Value = e.NewTime;
            ControlBox.VideoName = Playlist.Current.Name;
            ControlBox.VideoTime = TimeSpan.FromMilliseconds(e.NewTime).ToString("hh\\:mm\\:ss") + "/" + Playlist.Current.Duration.ToString("hh\\:mm\\:ss");
            _subtitleControl.PositionTime = TimeSpan.FromMilliseconds(e.NewTime);
        });
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify the UI of property changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        if (this.PropertyChanged != null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a specific field and updates its value.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="field">The field to update.</param>
    /// <param name="value">The new value for the field.</param>
    private void OnPropertyChanged<T>(string propertyName, ref T field, T value)
    {
        if (field != null || value == null)
        {
            if (field == null)
            {
                return;
            }
            object obj = value;
            if (field.Equals(obj))
            {
                return;
            }
        }
        field = value;
        this.PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Background worker to hide the control bar and progress bar after mouse inactivity.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void _mouseNotMoveWorker_DoWork(object sender, DoWorkEventArgs e)
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
        await ControlBox.ShowByStoryboard((Storyboard)ControlBox.FindResource("fadeInControlBar"));
        await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
        base.Cursor = Cursors.Arrow;
    }

    /// <summary>
    /// Handles the Playing event of the media player, updating the UI and system state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPlaying(object sender, VlcMediaPlayerPlayingEventArgs e)
    {
        this.Dispatcher.InvokeAsync(delegate
        {
            ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
            ProgressBar.Value = 0.0;
            ProgressBar.ProgressText = "00 : 00 : 00/00 : 00 : 00";
            _playerStatus = MediaPlayerStatus.Play;
            _playing = true;
            _paused = false;
            _stoped = false;
            ControlBox.VideoName = Playlist.Current?.Name ?? "No video loaded";
            ControlBox.VideoTime = "00 : 00 : 00/00 : 00 : 00";
            ControlBox._videoNextName.Text = Playlist.Next?.Name ?? "No next media";
            ControlBox._videoPreviewName.Text = Playlist.Previous?.Name ?? "No previous media";
            SetThreadExecutionState(BLOCK_SLEEP_MODE);
        });
    }

    /// <summary>
    /// Handles the Stopped event of the media player, stopping playback and updating the system state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnStopped(object sender, VlcMediaPlayerStoppedEventArgs e)
    {       
        _playing = false;
        _paused = false;
        _stoped = true;
        _playerStatus = MediaPlayerStatus.Stop;
        SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
    }

    /// <summary>
    /// Handles the Paused event of the media player, updating the system state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPaused(object sender, VlcMediaPlayerPausedEventArgs e)
    {
        _playing = false;
        _paused = true;
        _stoped = false;
        _playerStatus = MediaPlayerStatus.Pause;
        SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
    }

    /// <summary>
    /// Handles the EndReached event of the media player, processing repeat logic.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnEndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            Stop();
        });
        var repeat = ControlBox.RepeatControl.RepeatType;
        HandleRepeat(repeat);
    }

    /// <summary>
    /// Handles repeat logic based on the current repeat mode.
    /// </summary>
    private void HandleRepeat(RepeatType repeat)
    {
        switch (repeat)
        {
            case RepeatType.One:
                base.Dispatcher.InvokeAsync(delegate
                {
                    Stop();
                    Playlist.Current.Play();
                });
                break;
            case RepeatType.All:
                base.Dispatcher.InvokeAsync(delegate
                {
                    if (ControlBox.RepeatControl.EnableShuffle)
                    {
                        if (Playlist.Videos.Count > 0)
                        {
                            int randomIndex = _random.Next(0, Playlist.Videos.Count);
                            if (randomIndex == Playlist.CurrentIndex)
                            {
                                randomIndex = (randomIndex + 1) % Playlist.Videos.Count; // Ensure we don't repeat the current video
                            }
                            Playlist.CurrentIndex = randomIndex;
                            Stop();
                            Playlist.Current.Play();
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        if (Playlist.MoveNext != null)
                            Next();
                        else
                            Playlist.CurrentIndex = 0; // Loop back to the first media
                    }
                });
                break;
            case RepeatType.None:
                base.Dispatcher.InvokeAsync(delegate
                {
                    Stop();
                });
                break;
        }
    }

    /// <summary>
    /// Handles the Buffering event of the media player, updating the progress bar's buffer value.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The buffering event arguments.</param>
    private void OnBuffering(object sender, VlcMediaPlayerBufferingEventArgs e)
    {
        ProgressBar.BufforBarValue = e.NewCache;
    }

    /// <summary>
    /// Pauses the current media playback.
    /// </summary>
    public void Pause()
    {
        try
        {
            ThreadPool.QueueUserWorkItem(delegate
            {

                _playing = false;
                _paused = true;
                _stoped = false;
                _vlcControl.SourceProvider.MediaPlayer?.SetPause(true);
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                _playerStatus = MediaPlayerStatus.Pause;
                Logger.Log.Log(LogLevel.Debug, new string[2] { "File", "Console" }, "Media was paused.");
            });
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Error while pausing media: " + ex.Message);
        }
    }

    /// <summary>
    /// Stops the current media playback.
    /// </summary>
    public void Stop()
    {
        try
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                _playing = false;
                _paused = false;
                _stoped = true;
                _playerStatus = MediaPlayerStatus.Stop;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, "Media was stopped.");
                _vlcControl.SourceProvider.MediaPlayer?.Stop();
            });
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, ex.Message ?? "");
        }
    }

    /// <summary>
    /// Plays the next video in the playlist.
    /// </summary>
    public void Next()
    {
        if (Playlist.MoveNext == null)
        {
            Logger.Log.Log(LogLevel.Warning, new string[2] { "Console", "File" }, "No next media in the playlist.");
            return;
        }
        Stop();
        Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, "Next media is playing.");
        Playlist.MoveNext.Play();
    }

    /// <summary>
    /// Plays the previous video in the playlist.
    /// </summary>
    public void Preview()
    {
        if (Playlist.MovePrevious == null)
        {
            Logger.Log.Log(LogLevel.Warning, new string[2] { "Console", "File" }, "No previous media in the playlist.");
            return;
        }
        Stop();
        Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, "Before media is playing.");
        Playlist.MovePrevious.Play();
    }

    /// <summary>
    /// Seeks to a specific time in the current media.
    /// </summary>
    /// <param name="time">The time to seek to.</param>
    public void Seek(TimeSpan time)
    {
        if (_vlcControl != null)
        {
            _vlcControl.SourceProvider.MediaPlayer.Time = (long)time.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Seeks forward or backward by a specified duration.
    /// </summary>
    /// <param name="time">The duration to seek by.</param>
    /// <param name="direction">The direction to seek (Forward or Backward).</param>
    public void Seek(TimeSpan time, SeekDirection direction)
    {
        if (_vlcControl != null)
        {
            switch (direction)
            {
                case SeekDirection.Forward:
                    _vlcControl.SourceProvider.MediaPlayer.Time += (long)time.TotalMilliseconds;
                    break;
                case SeekDirection.Backward:
                    _vlcControl.SourceProvider.MediaPlayer.Time -= (long)time.TotalMilliseconds;
                    break;
            }
        }
    }

    /// <summary>
    /// Loads and displays subtitles from a specified file path.
    /// </summary>
    /// <param name="path">The path to the subtitle file.</param>
    public void Subtitle(string path)
    {
        _subtitleControl.FilePath = path;
        _subtitleControl.Visibility = Visibility.Visible;
        _subtitleControl.TimeChanged += delegate (object sender, TimeSpan time)
        {
            if (_vlcControl.SourceProvider.MediaPlayer != null)
            {
                _subtitleControl.PositionTime = time;
            }
        };
    }

    /// <summary>
    /// Plays the specified media or the current playlist item.
    /// </summary>
    /// <param name="media">The video to play, or null to play the current playlist item.</param>
    private void _Play(Video media = null)
    {
        if (Playlist.Current == null)
        {
            Logger.Log.Log(LogLevel.Warning, new string[2] { "Console", "File" }, "Playlist is empty or current media is not set.");
            return;
        }
        if (media == null)
        {
            media = Playlist.Current;
        }
        try
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                _playing = true;
                _paused = false;
                _stoped = false;
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "Playing media: " + Playlist.Current.Name);
                Console.WriteLine(Playlist.Next.Name);
                if (_vlcControl.SourceProvider.MediaPlayer.IsPausable())
                    _vlcControl.SourceProvider.MediaPlayer?.SetPause(false);
                else
                    _vlcControl.SourceProvider.MediaPlayer?.Play(media.Uri);
                _playerStatus = MediaPlayerStatus.Play;
            });
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Error while playing media: " + ex.Message);
        }
    }

    /// <summary>
    /// Plays the specified video.
    /// </summary>
    /// <param name="media">The video to play.</param>
    public void Play(Video media)
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
            Logger.Log.Log(LogLevel.Warning, new string[2] { "Console", "File" }, "Playlist is empty or current media is not set.");
            return;
        }
        _Play();
    }

    /// <summary>
    /// Disposes of the player control, releasing resources and unsubscribing from events.
    /// </summary>
    public void Dispose()
    {
        if (_mouseNotMoveWorker != null)
        {
            _mouseNotMoveWorker.CancelAsync();
            _mouseNotMoveWorker.Dispose();
        }
        if (_vlcControl != null)
        {
            _vlcControl.SourceProvider.MediaPlayer.Stopped -= OnStopped;
            _vlcControl.SourceProvider.MediaPlayer.Paused -= OnPaused;
            _vlcControl.SourceProvider.MediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
        }
        _vlcControl.SourceProvider.Dispose();
        _vlcControl.Dispose();
        base.MouseMove -= OnMouseMove;
        base.Loaded -= UserControl_Loaded;
    }
}
