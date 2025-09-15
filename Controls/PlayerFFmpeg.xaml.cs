// PlayerFFmpeg.xaml.cs
// Version: 1.1.1
// An integrated WPF UserControl for media playback using FFmpeg via process pipes.
// Decodes video frames as raw RGB24 data through stdout pipe and renders them to a WriteableBitmap
// in an Image control. Uses a DispatcherTimer for frame updates and mouse inactivity detection.
// Supports play, stop, pause/resume, seek (via restart with -ss), volume (via -af volume),
// and subtitles (via burn-in with -vf subtitles).
// Requires FFmpeg.exe and ffprobe.exe in PATH or specified path. Handles video only (no audio).
// Integrates with PlaylistView, ControlBox, ProgressBarBox, TimerBox, and SubtitleControl.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Thmd.Helpers;
using Thmd.Logs;
using Thmd.Media;

using Vlc.DotNet.Core;

namespace Thmd.Controls
{
    /// <summary>
    /// A WPF UserControl for integrated FFmpeg video playback.
    /// Renders frames to an Image control using WriteableBitmap from raw pipe data.
    /// </summary>
    public partial class PlayerFFmpeg : UserControl, IPlayer
    {
        private Process _ffmpegProcess;
        private DispatcherTimer _frameTimer;
        private DispatcherTimer _mouseInactivityTimer; // Replaces BackgroundWorker
        private WriteableBitmap _writeableBitmap;
        private Grid _grid;
        private int _frameWidth = 0;
        private int _frameHeight = 0;
        private int _frameStride = 0; // Bytes per row: width * 3 for RGB24
        private byte[] _frameBuffer;
        private bool _isDisposed = false;
        private PlaylistView _playlist;
        private TimeSpan _currentTime = TimeSpan.Zero;
        private MediaPlayerStatus _playerStatus = MediaPlayerStatus.Stop;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isStopped = true;
        private double _volume = 50.0;
        private string _ffmpegPath = "ffmpeg"; // Assume in PATH
        private string _currentFilePath;
        private string _currentSubtitlePath;
        private double _fps = 30.0; // Default; detect from probe
        private SubtitleControl _subtitleControl;
        private TimerBox _timerBox;
        private bool _timerVisibility = true;
        private Visibility _subtitleVisibility = Visibility.Hidden;
        private InfoBox _infoBox;
        private bool _muted = false;
        private bool _fullscreen = false;
        private bool _isMouseMove = false;

        // Image control for rendering
        private System.Windows.Controls.Image _videoImage;

        /// <summary>
        /// Gets or sets the playlist.
        /// </summary>
        public PlaylistView Playlist
        {
            get => _playlist;
            set => _playlist = value ?? new PlaylistView();
        }

        /// <summary>
        /// Gets or sets the current time (approximate via timer).
        /// </summary>
        public TimeSpan Position
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        /// <summary>
        /// Gets or sets the player status.
        /// </summary>
        public MediaPlayerStatus PlayerStatus
        {
            get => _playerStatus;
            set
            {
                if (_playerStatus != value)
                {
                    _playerStatus = value;
                    OnPropertyChanged(nameof(PlayerStatus));
                }
            }
        }

        /// <summary>
        /// Gets or sets playing state.
        /// </summary>
        public bool isPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    if (_isPlaying) StartPlayback();
                    else Pause();
                    OnPropertyChanged(nameof(isPlaying));
                }
            }
        }

        /// <summary>
        /// Gets or sets paused state.
        /// </summary>
        public bool isPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged(nameof(isPaused));
                }
            }
        }

        /// <summary>
        /// Gets or sets stopped state.
        /// </summary>
        public bool isStopped
        {
            get => _isStopped;
            set
            {
                if (_isStopped != value)
                {
                    _isStopped = value;
                    OnPropertyChanged(nameof(isStopped));
                }
            }
        }

        /// <summary>
        /// Gets or sets volume (0-100). Applied via -af volume filter.
        /// </summary>
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged(nameof(Volume));
                if (_isPlaying) RestartPlayback();
            }
        }

        /// <summary>
        /// Gets the handle (not applicable for integrated).
        /// </summary>
        public IntPtr Handle => IntPtr.Zero;

        /// <summary>
        /// Gets or sets timer visibility.
        /// </summary>
        public bool TimerVisibility
        {
            get => _timerVisibility;
            set
            {
                _timerVisibility = value;
                _timerBox.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                OnPropertyChanged(nameof(TimerVisibility));
            }
        }

        /// <summary>
        /// Gets or sets the control box.
        /// </summary>
        public ControlBox ControlBox { get; set; }

        /// <summary>
        /// Gets or sets the duration in seconds after which controls are hidden if mouse is inactive.
        /// </summary>
        public int MouseSleeps { get; set; } = 7;

        /// <summary>
        /// Gets or sets the progress bar control.
        /// </summary>
        public ProgressBarBox ProgressBar { get; set; }

        /// <summary>
        /// Gets or sets stopped state (alias for isStopped).
        /// </summary>
        public bool isStoped
        {
            get => _isStopped;
            set => OnPropertyChanged(nameof(isStoped), ref _isStopped, value);
        }

        /// <summary>
        /// Gets or sets subtitle visibility.
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
                    _subtitleControl.Visibility = value;
                    OnPropertyChanged(nameof(SubtitleVisibility));
                }
            }
        }

        /// <summary>
        /// Gets or sets mute state (stub; no audio in this implementation).
        /// </summary>
        public bool isMute
        {
            get => _muted;
            set
            {
                _muted = value;
                // Volume adjustment would go here if audio supported
                OnPropertyChanged(nameof(isMute));
            }
        }

        /// <summary>
        /// Gets or sets fullscreen state.
        /// </summary>
        public bool Fullscreen
        {
            get => _fullscreen;
            set
            {
                if (_fullscreen != value)
                {
                    ToggleFullscreen();
                    _fullscreen = FullscreenHelper.IsFullscreen;
                    ControlBox.BtnFullscreen.Style = _fullscreen
                        ? FindResource("FullscreenOff") as Style
                        : FindResource("FullscreenOn") as Style;
                    OnPropertyChanged(nameof(Fullscreen));
                }
            }
        }

        public ControlBar ControlBar => throw new NotImplementedException();

        public Visibility PlaylistVisibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<VlcMediaPlayerPlayingEventArgs> Playing;
        public event EventHandler<VlcMediaPlayerStoppedEventArgs> Stopped;
        public event EventHandler<VlcMediaPlayerLengthChangedEventArgs> LengthChanged;
        public event EventHandler<VlcMediaPlayerTimeChangedEventArgs> TimeChanged;

        /// <summary>
        /// Initializes a new instance of the PlayerFFmpeg class.
        /// </summary>
        public PlayerFFmpeg()
        {
            _grid = new Grid();

            InitializeComponent();
            InitializeVideoImage();
            InitializeProgressBar();
            InitializeInfoBox();
            InitializeTimerBox();
            InitializeControlBar();
            InitializeSubtitleControl();
            InitializePlaylist();
            InitContentGrid();
            InitializeTimer();
            InitializeMouseInactivityTimer();
            ControlBarButtonEvent();

            base.MouseMove += OnMouseMove;
            base.Loaded += UserControl_Loaded;
        }

        /// <summary>
        /// Sets up event handlers for control bar buttons.
        /// </summary>
        private void ControlBarButtonEvent()
        {
            ControlBox.BtnPlay.Click += (s, e) =>
            {
                if (_playerStatus == MediaPlayerStatus.Play)
                {
                    Pause();
                    _infoBox.DrawInfoText("Pause");
                }
                else if (_playerStatus == MediaPlayerStatus.Pause)
                {
                    Resume();
                    _infoBox.DrawInfoText("Play");
                }
            };
            ControlBox.BtnStop.Click += (s, e) =>
            {
                Stop();
                _infoBox.DrawInfoText("Stop");
            };
            ControlBox.BtnNext.Click += (s, e) =>
            {
                Next();
                _infoBox.DrawInfoText("Next");
            };
            ControlBox.BtnPrevious.Click += (s, e) =>
            {
                Preview();
                _infoBox.DrawInfoText("Preview");
            };
            ControlBox.BtnClose.Click += async (s, e) =>
            {
                await ControlBox.HideByStoryboard((Storyboard)ControlBox.FindResource("fadeOutControlBar"));
                await ProgressBar.HideByStoryboard((Storyboard)ProgressBar.FindResource("fadeOutProgressBar"));
                base.Cursor = Cursors.None;
            };
            ControlBox.BtnVolumeUp.Click += (s, e) =>
            {
                if (_volume < 100.0)
                {
                    Volume += 10;
                    ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = _volume;
                    ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
                    _infoBox.DrawInfoText($"Volume up: {_volume}");
                }
            };
            ControlBox.BtnVolumeDown.Click += (s, e) =>
            {
                if (_volume > 0.0)
                {
                    Volume -= 10;
                    ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = _volume;
                    ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)_volume}";
                    _infoBox.DrawInfoText($"Volume down: {_volume}");
                }
            };
            ControlBox.BtnMute.Click += (s, e) =>
            {
                isMute = !isMute;
                string muteStatus = isMute ? "On" : "Off";
                ControlBox.BtnMute.Style = isMute ? FindResource("Mute") as Style : FindResource("Unmute") as Style;
                ControlBox._playerBtnVolume._volumeProgressBar._progressBar.Value = isMute ? 0 : _volume;
                ControlBox._playerBtnVolume._volumeProgressBar.ProgressText = $"Volume: {(int)(isMute ? 0 : _volume)}";
                _infoBox.DrawInfoText($"Audio mute: {muteStatus}");
            };
            ControlBox.BtnOpen.Click += (s, e) => OpenMediaFile();
            ControlBox.BtnPlaylist.Click += (s, e) =>
            {
                Playlist.Visibility = Playlist.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                _infoBox.DrawInfoText($"Playlist box: {Playlist.Visibility}");
            };
            ControlBox.BtnFullscreen.Click += (s, e) =>
            {
                Fullscreen = !Fullscreen;
                _infoBox.DrawInfoText($"Fullscreen: {(Fullscreen ? "On" : "Off")}");
            };
            ControlBox.BtnSubtitle.Click += (s, e) =>
            {
                OpenSubtitleFile();
            };
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
        /// Initializes UI on load.
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
            ProgressBar.Value = 0.0;
            ProgressBar.ProgressText = "00:00:00/00:00:00";
            ProgressBar.PopupText = "Volume: 100";
            ControlBox.VideoName = Playlist.Current?.Name ?? "No video loaded";
            ControlBox.VideoTime = "00:00:00/00:00:00";
        }

        /// <summary>
        /// Initializes the mouse inactivity timer.
        /// </summary>
        private void InitializeMouseInactivityTimer()
        {
            _mouseInactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(MouseSleeps)
            };
            _mouseInactivityTimer.Tick += MouseInactivityTimer_Tick;
            _mouseInactivityTimer.Start();
        }

        /// <summary>
        /// Handles mouse inactivity timer tick to hide controls.
        /// </summary>
        private async void MouseInactivityTimer_Tick(object sender, EventArgs e)
        {
            if (!_isMouseMove)
            {
                await ControlBox.HideByStoryboard((Storyboard)ControlBox.FindResource("fadeOutControlBar"));
                await ProgressBar.HideByStoryboard((Storyboard)ProgressBar.FindResource("fadeOutProgressBar"));
                base.Cursor = Cursors.None;
            }
            _isMouseMove = false;
            _mouseInactivityTimer.Stop();
            _mouseInactivityTimer.Start(); // Reset timer
        }

        /// <summary>
        /// Shows controls on mouse move.
        /// </summary>
        private async void OnMouseMove(object sender, MouseEventArgs e)
        {
            _isMouseMove = true;
            await ControlBox.ShowByStoryboard((Storyboard)ControlBox.FindResource("fadeInControlBar"));
            await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
            base.Cursor = Cursors.Arrow;
            _mouseInactivityTimer.Stop();
            _mouseInactivityTimer.Start(); // Restart timer
        }

        /// <summary>
        /// Raises PropertyChanged event.
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged event for a field.
        /// </summary>
        protected void OnPropertyChanged<T>(string propertyName, ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Initializes the Image control for video rendering.
        /// </summary>
        private void InitializeVideoImage()
        {
            _videoImage = new System.Windows.Controls.Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _grid.Children.Add(_videoImage);
        }

        /// <summary>
        /// Initializes the control bar.
        /// </summary>
        private void InitializeControlBar()
        {
            ControlBox = new ControlBox(this);
            var resizer = new ResizeControlHelper(ControlBox);
            ControlBox.VerticalAlignment = VerticalAlignment.Top;
            ControlBox.HorizontalAlignment = HorizontalAlignment.Left;
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
        /// Initializes the frame timer.
        /// </summary>
        private void InitializeTimer()
        {
            _frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000 / _fps)
            };
            _frameTimer.Tick += OnFrameTimerTick;
        }

        /// <summary>
        /// Plays the specified media.
        /// </summary>
        public void Play(VideoItem media)
        {
            if (media == null || string.IsNullOrEmpty(media.Uri?.LocalPath))
            {
                Logger.Log.Log(LogLevel.Warning, new[] { "Console", "File" }, "Invalid media.");
                return;
            }

            Stop();
            _currentFilePath = media.Uri.LocalPath;
            _currentSubtitlePath = media.SubtitlePath ?? string.Empty;
            _isPlaying = true;
            _isPaused = false;
            _isStopped = false;
            PlayerStatus = MediaPlayerStatus.Play;

            ProbeVideoInfo(_currentFilePath);
            StartPlayback();
            Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Playing: {media.Name}");
        }

        /// <summary>
        /// Plays current playlist item.
        /// </summary>
        public void Play()
        {
            if (Playlist.Current == null) return;
            Play(Playlist.Current);
        }

        /// <summary>
        /// Pauses by stopping timer.
        /// </summary>
        public void Pause()
        {
            if (_isPlaying)
            {
                _frameTimer.Stop();
                _isPaused = true;
                _isPlaying = false;
                PlayerStatus = MediaPlayerStatus.Pause;
                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Playback paused.");
            }
        }

        /// <summary>
        /// Resumes from pause.
        /// </summary>
        public void Resume()
        {
            if (_isPaused)
            {
                _frameTimer.Start();
                _isPaused = false;
                _isPlaying = true;
                PlayerStatus = MediaPlayerStatus.Play;
                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Playback resumed.");
            }
        }

        /// <summary>
        /// Stops playback and cleans up.
        /// </summary>
        public void Stop()
        {
            _frameTimer.Stop();
            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _ffmpegProcess.Kill();
                _ffmpegProcess.WaitForExit(2000);
                _ffmpegProcess.Dispose();
            }
            _ffmpegProcess = null;
            _isPlaying = false;
            _isPaused = false;
            _isStopped = true;
            PlayerStatus = MediaPlayerStatus.Stop;
            _currentTime = TimeSpan.Zero;
            ClearImage();
            Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Playback stopped.");
        }

        /// <summary>
        /// Plays next in playlist.
        /// </summary>
        public void Next()
        {
            Stop();
            if (Playlist.Videos.Count > 0)
            {
                Playlist.CurrentIndex = Playlist.NextIndex;
                Play();
                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Next media playing.");
            }
        }

        /// <summary>
        /// Plays previous in playlist.
        /// </summary>
        public void Preview()
        {
            Stop();
            if (Playlist.Videos.Count > 0)
            {
                Playlist.CurrentIndex = Playlist.PreviousIndex;
                Play();
                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Previous media playing.");
            }
        }

        /// <summary>
        /// Seeks to time (restarts ffmpeg with -ss).
        /// </summary>
        public void Seek(TimeSpan time)
        {
            _currentTime = time;
            if (_isPlaying) RestartPlayback();
            Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Seek to: {time}");
        }

        /// <summary>
        /// Seeks forward/backward.
        /// </summary>
        public void Seek(TimeSpan time, SeekDirection direction)
        {
            TimeSpan newTime = direction == SeekDirection.Forward
                ? _currentTime + time
                : _currentTime - time;
            if (newTime < TimeSpan.Zero) newTime = TimeSpan.Zero;
            Seek(newTime);
        }

        /// <summary>
        /// Sets subtitle path (restarts if playing).
        /// </summary>
        public void SetSubtitle(string path)
        {
            if (Playlist.Current != null) Playlist.Current.SubtitlePath = path;
            _currentSubtitlePath = path;
            _subtitleControl.FilePath = path; // Update SubtitleControl
            if (_isPlaying) RestartPlayback();
            Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Subtitles set: {path}");
        }

        /// <summary>
        /// Opens a media file via dialog.
        /// </summary>
        private void OpenMediaFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Media Files (*.mp4,*.avi,*.mkv)|*.mp4;*.avi;*.mkv|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                var media = new VideoItem(dialog.FileName);
                Playlist.Videos.Add(media);
                Playlist.CurrentIndex = Playlist.Videos.Count - 1;
                Play(media);
                ControlBox.VideoName = media.Name;
            }
        }

        /// <summary>
        /// Opens a subtitle file via dialog.
        /// </summary>
        private void OpenSubtitleFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SetSubtitle Files (*.srt)|*.srt|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                SetSubtitle(dialog.FileName);
                SubtitleVisibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Toggles fullscreen mode.
        /// </summary>
        private void ToggleFullscreen()
        {
            this.Fullscreen();
        }

        /// <summary>
        /// Probes video info using ffprobe.
        /// </summary>
        private async void ProbeVideoInfo(string filePath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v quiet -print_format json -show_streams \"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var probeProcess = Process.Start(startInfo);
                string jsonOutput = await probeProcess.StandardOutput.ReadToEndAsync();
                probeProcess.WaitForExit();

                // Parse JSON manually (simple)
                int widthIndex = jsonOutput.IndexOf("\"width\":");
                int heightIndex = jsonOutput.IndexOf("\"height\":");
                if (widthIndex > 0 && heightIndex > 0)
                {
                    _frameWidth = int.Parse(jsonOutput.Substring(widthIndex + 8, jsonOutput.IndexOf(",", widthIndex) - widthIndex - 8).Trim());
                    _frameHeight = int.Parse(jsonOutput.Substring(heightIndex + 9, jsonOutput.IndexOf(",", heightIndex) - heightIndex - 9).Trim());
                }

                int fpsIndex = jsonOutput.IndexOf("\"r_frame_rate\":");
                if (fpsIndex > 0)
                {
                    string fpsStr = jsonOutput.Substring(fpsIndex + 16, jsonOutput.IndexOf(",", fpsIndex) - fpsIndex - 16).Trim().Replace("\"", "");
                    if (fpsStr.Contains("/")) _fps = double.Parse(fpsStr.Split('/')[0]) / double.Parse(fpsStr.Split('/')[1]);
                }

                _frameStride = _frameWidth * 3; // RGB24
                _frameBuffer = new byte[_frameStride * _frameHeight];
                _writeableBitmap = new WriteableBitmap(_frameWidth, _frameHeight, 96, 96, PixelFormats.Rgb24, null);
                _videoImage.Source = _writeableBitmap;

                _frameTimer.Interval = TimeSpan.FromMilliseconds(1000 / _fps);
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Probe error: {ex.Message}");
                _frameWidth = 1920; _frameHeight = 1080; _fps = 30.0;
                _frameStride = _frameWidth * 3;
                _frameBuffer = new byte[_frameStride * _frameHeight];
                _writeableBitmap = new WriteableBitmap(_frameWidth, _frameHeight, 96, 96, PixelFormats.Rgb24, null);
                _videoImage.Source = _writeableBitmap;
                _frameTimer.Interval = TimeSpan.FromMilliseconds(33);
            }
        }

        /// <summary>
        /// Starts FFmpeg process for frame decoding.
        /// </summary>
        private void StartPlayback()
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return;

            Stop();

            try
            {
                string seekArg = _currentTime.TotalSeconds > 0 ? $"-ss {_currentTime.TotalSeconds}" : "";
                string volumeArg = isMute ? "-an" : $"-af volume={_volume / 100.0}"; // -an disables audio if muted
                string subtitleArg = !string.IsNullOrEmpty(_currentSubtitlePath) ? $"-vf subtitles=\\\"{_currentSubtitlePath}\\\"" : "";
                string args = $"{seekArg} -i \"{_currentFilePath}\" {volumeArg} {subtitleArg} -f rawvideo -pix_fmt rgb24 -s {_frameWidth}x{_frameHeight} pipe:1";

                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _ffmpegProcess = Process.Start(startInfo);
                _ffmpegProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Logger.Log.Log(LogLevel.Error, new[] { "Console" }, e.Data); };
                _ffmpegProcess.BeginErrorReadLine();

                _frameTimer.Start();
                _currentTime = TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"FFmpeg start error: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Restarts playback for seek/volume/subtitle changes.
        /// </summary>
        private void RestartPlayback()
        {
            Stop();
            if (_isPlaying) StartPlayback();
        }

        /// <summary>
        /// Reads and renders frames.
        /// </summary>
        private void OnFrameTimerTick(object sender, EventArgs e)
        {
            if (_ffmpegProcess == null || _ffmpegProcess.HasExited || _frameBuffer == null) return;

            try
            {
                int bytesRead = _ffmpegProcess.StandardOutput.BaseStream.Read(_frameBuffer, 0, _frameBuffer.Length);
                if (bytesRead == 0) // EOF
                {
                    _ffmpegProcess.WaitForExit();
                    Stop();
                    return;
                }

                _writeableBitmap.WritePixels(
                    new Int32Rect(0, 0, _frameWidth, _frameHeight),
                    _frameBuffer,
                    _frameStride,
                    0);

                _currentTime += TimeSpan.FromMilliseconds(1000 / _fps);
                ProgressBar.Value = _currentTime.TotalMilliseconds;
                _timerBox.Timer = $"{_currentTime.Hours:00}:{_currentTime.Minutes:00}:{_currentTime.Seconds:00}";
                ControlBox.VideoTime = $"{_currentTime.Hours:00}:{_currentTime.Minutes:00}:{_currentTime.Seconds:00}/{Playlist.Current?.Duration.Hours:00}:{Playlist.Current?.Duration.Minutes:00}:{Playlist.Current?.Duration.Seconds:00}";
                _subtitleControl.PositionTime = _currentTime; // Sync subtitles
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Frame read error: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Clears the image source.
        /// </summary>
        private void ClearImage()
        {
            _videoImage.Source = null;
            _writeableBitmap = null;
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _frameTimer?.Stop();
                _mouseInactivityTimer?.Stop();
                _frameTimer = null;
                _mouseInactivityTimer = null;
                _isDisposed = true;
            }
        }
    }
}
