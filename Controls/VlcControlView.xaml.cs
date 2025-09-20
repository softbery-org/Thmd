using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

using Microsoft.Win32;

using Thmd.Consolas;
using Thmd.Media;
using Thmd.Repeats;
using Thmd.Utilities;

namespace Thmd.Controls
{
    /// <summary>
    /// Logic interaction for class VlcControlView.xaml
    /// </summary>
    public partial class VlcControlView : UserControl, IPlayer, INotifyPropertyChanged
    {
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
        /// Sets the system execution state to prevent sleep during playback.
        /// </summary>
        /// <param name="esFlags">The execution state flags.</param>
        /// <returns>The previous execution state.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        /// <summary>
        /// Gets or sets the current status of the media player.
        /// </summary>
        public PlaylistView Playlist { get => _playlist; }
        /// <summary>
        /// Count items in Videos
        /// </summary>
        public int Count { get => _playlist.Videos.Count(); }
        /// <summary>
        /// Gets or sets the visibility of the playlist.
        /// </summary>
        public Visibility PlaylistVisibility { get => _playlist.Visibility; set => _playlist.Visibility = value; }
        /// <summary>
        /// Gets or sets the progress bar control for displaying playback progress.
        /// </summary>
        public ProgressBarView ProgressBar { get => _progressBar; set => _progressBar = value; }
        /// <summary>
        /// Gets or sets the control bar for playback controls (play, pause, stop, etc.).
        /// </summary>
        public ControlBar ControlBar { get => _controlBar; set => _controlBar = value; }
        /// <summary>
        /// Gets or sets the current playback position of the media.
        /// </summary>
        public TimeSpan Position
        {
            get => _position;
            set
            {
                _position = value;
                Seek(_position);
                OnPropertyChanged(nameof(Position), ref _position, value);
            }
        }
        /// <summary>
        /// Indicates whether media is currently playing.
        /// </summary>
        public bool isPlaying
        {
            get => _playing;
            set
            {
                _playing = value;
                if (value)
                {
                    _paused = false;
                    _stopped = false;
                }
                OnPropertyChanged(nameof(isPlaying), ref _playing, value);
            }
        }
        /// <summary>
        /// Indicates whether media playback is currently paused.
        /// </summary>
        public bool isPaused
        {
            get => _paused;
            set
            {
                _paused = value;
                if (value)
                {
                    _playing = false;
                    _stopped = false;
                }
                OnPropertyChanged(nameof(isPaused), ref _paused, value);
            }
        }
        /// <summary>
        /// Indicates whether media playback is currently stopped.
        /// </summary>
        public bool isStoped
        {
            get => _stopped;
            set
            {
                _stopped = value;
                if (value)
                {
                    _paused = false;
                    _playing = false;
                }
                OnPropertyChanged(nameof(isStoped), ref _stopped, value);
            }
        }
        /// <summary>
        /// Gets or sets the subtitle control for managing subtitles.
        /// </summary>
        public SubtitleControl SubtitleControl
        {
            get => _subtitleControl;
            set => _subtitleControl = value;
        }
        /// <summary>
        /// Gets or sets the visibility of subtitles.
        /// </summary>
        public Visibility SubtitleVisibility { get => _subtitleVisibility; set => _subtitleVisibility = value; }
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
                _mediaPlayer.Volume = (int)value;
                OnPropertyChanged("Volume", ref _volume, value);
            }
        }
        /// <summary>
        /// Gets or sets whether the audio is muted.
        /// </summary>
        public bool isMute
        {
            get => _muted;
            set
            {
                if (value)
                {
                    _muted = true;
                    _mediaPlayer.Mute = true;
                }
                else
                {
                    _muted = false;
                    _mediaPlayer.Mute = false;
                }
                OnPropertyChanged("isMute", ref _muted, value);
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether the media is in fullscreen mode.
        /// </summary>
        public bool Fullscreen
        {
            get => _fullscreen;
            set
            {
                this.Fullscreen();
                _fullscreen = ScreenHelper.IsFullscreen;
                OnPropertyChanged("Fullscreen", ref _fullscreen, value);
            }
        }
        /// <summary>
        /// Gets the currently loaded media item.
        /// </summary>
        public VideoItem Media { get => _playlist.Current; }
        public double MouseSleeps { get; private set; } = 7;

        /// <summary>
        /// Occurs when a property value changes, used for data binding.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> Playing;
        public event EventHandler<EventArgs> Stopped;
        public event EventHandler<EventArgs> LengthChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private double _volume = 100.0;
        private bool _muted;
        private bool _playing;
        private bool _paused;
        private bool _stopped;
        private bool _fullscreen;
        private TimeSpan _position;
        private readonly Random _random = new Random();
        private VideoItem _media;
        private Visibility _subtitleVisibility = Visibility.Hidden;
        private Visibility _playlistVisibility = Visibility.Hidden;
        private bool _isMouseMove;
        private BackgroundWorker _mouseNotMoveWorker;

        public VlcControlView()
        {
            InitializeComponent();

            // Set player references for controls
            _controlBar.SetPlayer(this);
            _playlist.SetPlayer(this);
            _progressBar.SetPlayer(this);

            _controlBar.SliderVolume.MouseDown += ControlBar_SliderVolume_MouseDown;
            _controlBar.SliderVolume.MouseMove += ControlBar_SliderVolume_MouseMove;

            // Progress bar events
            _progressBar.MouseDown += ProgressBar_MouseDown;
            _progressBar.MouseMove += ProgressBar_MouseMove;

            // VlcControl events and values
            string[] mediaOptions = new string[] { "--no-video-title-show", "--no-sub-autodetect-file" };
            _libVLC = new LibVLC(mediaOptions);
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EnableHardwareDecoding = false;
            _mediaPlayer.EnableKeyInput = false;
            _mediaPlayer.EnableMouseInput = false;
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.EndReached += OnEndReached;
            _mediaPlayer.Playing += OnPlaying;
            _mediaPlayer.Stopped += OnStopped;
            _mediaPlayer.Paused += OnPaused;
            _mediaPlayer.Buffering += OnBuffering;

            _videoView.MediaPlayer = _mediaPlayer;

            // Resize helpers for control bar and playlist
            var resizer1 = new ResizeControlHelper(_controlBar);
            var resizer2 = new ResizeControlHelper(_playlist);

            // Buttons event handlers
            ControlBarButtonEvent();

            // Mouse and keyboard events
            //this.MouseMove += OnMouseMove;
            this.MouseDown += OnMouseDown;
            this.MouseDoubleClick += OnMouseDoubleClick;
            this.MouseWheel += OnMouseWheel;
            this.KeyDown += PlayerView_KeyDown;
            this.KeyUp += PlayerView_KeyUp;

            // Mouse not moving worker
            _mouseNotMoveWorker = new BackgroundWorker();
            _mouseNotMoveWorker.DoWork += MouseNotMoveWorker_DoWork;
            _mouseNotMoveWorker.RunWorkerAsync();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                // Single left-click to toggle play/pause
                if (isPlaying)
                {
                    Pause();
                }
                else if (isPaused)
                {
                    Play();
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                // Right-click to toggle playlist visibility
                TogglePlaylist()();
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Double-click to toggle fullscreen
                ToggleFullscreen()();
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Adjust volume with mouse wheel
            double volumeChange = e.Delta > 0 ? 5.0 : -5.0;
            Volume += volumeChange;
            _controlBar.SliderVolume.Value = Volume;
            _progressBar.PopupText = $"Volume: {(int)Volume}";
            _progressBar._popup.IsOpen = true;
            // Show popup briefly
            Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(() => _progressBar._popup.IsOpen = false));
        }

        private void PlayerView_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle key release if needed
        }

        private void PlayerView_KeyDown(object sender, KeyEventArgs e)
        {
            var keyBindingList = new List<ShortcutKeyBinding>
            {
                new ShortcutKeyBinding { MainKey = Key.Space, SecondKey = null, Shortcut = "Space", Description = "Pause and play media", RunAction = PausePlay() },
                new ShortcutKeyBinding { MainKey = Key.F, SecondKey = null, Shortcut = "F", Description = "Toggle fullscreen", RunAction = ToggleFullscreen() },
                new ShortcutKeyBinding { MainKey = Key.H, SecondKey = null, Shortcut = "H", Description = "Toggle help window", RunAction = ToggleHelpWindow() },
                new ShortcutKeyBinding { MainKey = Key.P, SecondKey = null, Shortcut = "P", Description = "Toggle playlist", RunAction = TogglePlaylist() },
                new ShortcutKeyBinding { MainKey = Key.S, SecondKey = null, Shortcut = "S", Description = "Toggle subtitle", RunAction = ToggleSubtitle() },
                new ShortcutKeyBinding { MainKey = Key.Left, SecondKey = null, Shortcut = "Left", Description = "Move media backward 5 seconds", RunAction = () => Seek(TimeSpan.FromSeconds(5), SeekDirection.Backward) },
                new ShortcutKeyBinding { MainKey = Key.Right, SecondKey = null, Shortcut = "Right", Description = "Move media forward 5 seconds", RunAction = () => Seek(TimeSpan.FromSeconds(5), SeekDirection.Forward) },
                new ShortcutKeyBinding { MainKey = Key.Left, SecondKey = ModifierKeys.Control, Shortcut = "Ctrl+Left", Description = "Move media backward 5 minutes", RunAction = MoveBackwardMinutes() },
                new ShortcutKeyBinding { MainKey = Key.Right, SecondKey = ModifierKeys.Control, Shortcut = "Ctrl+Right", Description = "Move media forward 5 minutes", RunAction = MoveForwardMinutes() },
                new ShortcutKeyBinding { MainKey = Key.Up, SecondKey = null, Shortcut = "Up", Description = "Increase volume by 2", RunAction = () => Volume += 2 },
                new ShortcutKeyBinding { MainKey = Key.Down, SecondKey = null, Shortcut = "Down", Description = "Decrease volume by 2", RunAction = () => Volume -= 2 },
                new ShortcutKeyBinding { MainKey = Key.M, SecondKey = null, Shortcut = "M", Description = "Toggle mute", RunAction = () => isMute = !isMute },
                new ShortcutKeyBinding { MainKey = Key.L, SecondKey = null, Shortcut = "L", Description = "Toggle lector if subtitles are available", RunAction = ToggleLector() },
                new ShortcutKeyBinding { MainKey = Key.Escape, SecondKey = null, Shortcut = "Esc", Description = "Clear focus, minimize fullscreen", RunAction = ClearFocus() },
                new ShortcutKeyBinding { MainKey = Key.N, SecondKey = null, Shortcut = "N", Description = "Play next video", RunAction = () => Next() },
                new ShortcutKeyBinding { MainKey = Key.P, SecondKey = ModifierKeys.Control, Shortcut = "Ctrl+P", Description = "Play previous video", RunAction = () => Preview() },
            };

            foreach (var key in keyBindingList)
            {
                if (e.Key == key.MainKey && key.SecondKey == null && e.IsDown)
                {
                    key.RunAction();
                }
                else if (e.Key == key.MainKey && key.SecondKey == Keyboard.Modifiers && e.IsDown)
                {
                    key.RunAction();
                }
            }

            if (e.IsDown)
            {
                this.WriteLine($"Pressed: {Keyboard.Modifiers}+{e.Key}");
            }
        }

        private Action ToggleSubtitle()
        {
            return new Action(() =>
            {
                if (SubtitleVisibility == Visibility.Visible)
                {
                    SubtitleVisibility = Visibility.Hidden;
                }
                else
                {
                    OpenSubtitleFile();
                }
            });
        }

        private Action ToggleLector()
        {
            return new Action(() =>
            {
                // Placeholder for lector toggle functionality
                Console.WriteLine("[VlcControlView]: Toggling lector (not implemented)");
            });
        }

        private Action ClearFocus()
        {
            return new Action(() =>
            {
                if (ScreenHelper.IsFullscreen)
                {
                    this.Fullscreen();
                    _fullscreen = ScreenHelper.IsFullscreen;
                }
            });
        }

        private Action PausePlay()
        {
            return new Action(() =>
            {
                if (isPlaying)
                {
                    Pause();
                }
                else if (isPaused)
                {
                    Play();
                }
            });
        }

        private Action ToggleFullscreen()
        {
            return new Action(() =>
            {
                this.Fullscreen();
                _fullscreen = ScreenHelper.IsFullscreen;
            });
        }

        private Action ToggleHelpWindow()
        {
            return new Action(() =>
            {
                // Placeholder for help window toggle
                Console.WriteLine("[VlcControlView]: Toggling help window (not implemented)");
            });
        }

        private Action TogglePlaylist()
        {
            return new Action(() =>
            {
                if (Playlist.Visibility == Visibility.Visible)
                {
                    Playlist.Visibility = Visibility.Hidden;
                }
                else
                {
                    Playlist.Visibility = Visibility.Visible;
                }
            });
        }

        private Action MoveForwardMinutes()
        {
            return new Action(() =>
            {
                Seek(TimeSpan.FromMinutes(5), SeekDirection.Forward);
            });
        }

        private Action MoveBackwardMinutes()
        {
            return new Action(() =>
            {
                Seek(TimeSpan.FromMinutes(5), SeekDirection.Backward);
            });
        }

        public class ShortcutKeyBinding
        {
            public Key MainKey { get; set; }
            public ModifierKeys? SecondKey { get; set; }
            public string Shortcut { get; set; }
            public string Description { get; set; }
            public Action RunAction { get; set; }
        }

        private void ControlBar_SliderVolume_MouseMove(object sender, MouseEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Point mousePosition = e.GetPosition(_controlBar.SliderVolume);
            double width = _controlBar.SliderVolume.ActualWidth;
            if (width <= 0) return;

            double position = mousePosition.X / width * _controlBar.SliderVolume.Maximum;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _volume = position;
                _controlBar.SliderVolume.Value = position;
            }
        }

        private void ControlBar_SliderVolume_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ControlBar_SliderVolume_MouseEventHandler(sender, e);
        }

        private void ControlBar_SliderVolume_MouseEventHandler(object sender, MouseEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Point mousePosition = e.GetPosition(_controlBar.SliderVolume);
            double width = _controlBar.SliderVolume.ActualWidth;
            if (width <= 0) return;

            double position = mousePosition.X / width * _controlBar.SliderVolume.Maximum;

            _volume = position;
            _controlBar.SliderVolume.Value = position;
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            if (Playing != null)
            {
                _mediaPlayer.Playing += (sender, e) =>
                {
                    Playing?.Invoke(sender, e);
                };
            }
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            if (Stopped != null)
            {
                _mediaPlayer.Stopped += (sender, e) =>
                {
                    Stopped?.Invoke(sender, e);
                };
            }
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            if (TimeChanged != null)
            {
                _mediaPlayer.TimeChanged += (sender, e) =>
                {
                    TimeChanged?.Invoke(sender, e);
                };
            }
        }

        private void MediaPlayer_LenghtChanges(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            if (LengthChanged != null)
            {
                _mediaPlayer.LengthChanged += (sender, e) =>
                {
                    LengthChanged?.Invoke(sender, e);
                };
            }
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            _videoView.Dispatcher.InvokeAsync(() =>
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Stop();
                });
                var repeat = ControlBar.RepeatMode;
                HandleRepeat(repeat);
            });
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            _videoView.Dispatcher.InvokeAsync(() =>
            {
                _playlist.Current.Position = e.Time;

                _progressBar.Value = e.Time;
                _progressBar.Duration = _playlist.Current.Duration;
                _progressBar.ProgressText = string.Format("{0:00} : {1:00} : {2:00} / {3}", TimeSpan.FromMilliseconds(e.Time).Hours, TimeSpan.FromMilliseconds(e.Time).Minutes, TimeSpan.FromMilliseconds(e.Time).Seconds, ProgressBar.Duration.ToString("hh\\:mm\\:ss"));

                _controlBar.MediaTitle = _playlist.Current.Name;
                _controlBar.Position = TimeSpan.FromMilliseconds(e.Time).ToString("hh\\:mm\\:ss");
                _controlBar.Duration = _playlist.Current.Duration.ToString("hh\\:mm\\:ss");

                _subtitleControl.PositionTime = TimeSpan.FromMilliseconds(e.Time);
            });
        }

        private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Point mousePosition = e.GetPosition(_progressBar);
            double width = _progressBar.ActualWidth;
            if (width <= 0) return;

            double position = mousePosition.X / width * _progressBar._progressBar.Maximum;
            TimeSpan time = TimeSpan.FromMilliseconds(position);
            _progressBar.PopupText = $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
            _progressBar._popup.IsOpen = true;
            _progressBar._popup.HorizontalOffset = mousePosition.X - (_progressBar._popupText.ActualWidth / 2);

            _progressBar._rectangleMouseOverPoint.Margin = new Thickness(mousePosition.X - (_progressBar._rectangleMouseOverPoint.Width / 2), 0, 0, 0);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Position = time;
            }
        }

        private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ProgressBarMouseEventHandler(sender, e);
        }

        private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            Point mousePosition = e.GetPosition(_progressBar);
            double width = _progressBar.ActualWidth;
            if (width <= 0) return;

            double position = mousePosition.X / width * _progressBar._progressBar.Maximum;
            TimeSpan time = TimeSpan.FromMilliseconds(position);
            _progressBar.Value = (long)position;
            this.Position = time;
            _progressBar._rectangleMouseOverPoint.Margin = new Thickness(mousePosition.X - (_progressBar._rectangleMouseOverPoint.Width / 2), 0, 0, 0);
        }

        private void ControlBarButtonEvent()
        {
            ControlBar.BtnPlay.Click += delegate
            {
                if (isPlaying)
                {
                    Pause();
                }
                else if (isPaused)
                {
                    Play();
                }
            };
            ControlBar.BtnStop.Click += delegate
            {
                Stop();
            };
            ControlBar.BtnNext.Click += delegate
            {
                Next();
            };
            ControlBar.BtnPrevious.Click += delegate
            {
                Preview();
            };
            ControlBar.BtnMute.Click += delegate
            {
                var mute = String.Empty;
                if (_mediaPlayer.Mute)
                {
                    _volume = _mediaPlayer.Volume;
                    mute = "On";
                }
                else
                {
                    _volume = _mediaPlayer.Volume;
                    mute = "Off";
                }
                isMute = !isMute;
            };
            ControlBar.BtnOpen.Click += delegate
            {
                OpenMediaFile();
            };
            ControlBar.BtnPlaylist.Click += delegate
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
            ControlBar.BtnSubtitle.Click += delegate
            {
                OpenSubtitleFile();
                SubtitleVisibility = Visibility.Visible;
            };
        }

        private void OpenMediaFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "VideoItem files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.wmv|All files|*.*",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames)
                {
                    Playlist.AddAsync(new VideoItem(path));
                }
            }
        }

        private void OpenSubtitleFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SetSubtitle files|*.txt;*.sub;*.srt|All files|*.*",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames)
                {
                    SetSubtitle(path);
                    _subtitleVisibility = Visibility.Visible;
                }
            }
        }

        public void SetSubtitle(string path)
        {
            _videoView.Dispatcher.InvokeAsync(() =>
            {
                _subtitleControl.FilePath = path;
                _subtitleControl.TimeChanged += delegate (object sender, TimeSpan time)
                {
                    if (_videoView.MediaPlayer != null)
                    {
                        _subtitleControl.PositionTime = time;
                    }
                };
            });
        }

        public void Pause()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    _playing = false;
                    _paused = true;
                    _stopped = false;
                    if (_mediaPlayer.IsPlaying)
                    {
                        _mediaPlayer.Pause();
                    }
                    SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    _playing = false;
                    _paused = false;
                    _stopped = true;
                    _mediaPlayer.Stop();
                    SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: {ex.Message}");
            }
        }

        public void Next()
        {
            Stop();
            Playlist.MoveNext.Play();
        }

        public void Preview()
        {
            Stop();
            Playlist.MovePrevious.Play();
        }

        public void Seek(TimeSpan time)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)time.TotalMilliseconds;
            }
        }

        public void Seek(TimeSpan time, SeekDirection direction)
        {
            if (_mediaPlayer != null)
            {
                switch (direction)
                {
                    case SeekDirection.Forward:
                        this.Position += time;
                        break;
                    case SeekDirection.Backward:
                        this.Position -= time;
                        break;
                }
            }
        }

        private void _Play(VideoItem media = null)
        {
            if (Playlist.Current == null)
            {
                Console.WriteLine($"[VlcControlView]: Playlist is empty or current media is not set.");
                return;
            }

            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    _playing = true;
                    _paused = false;
                    _stopped = false;

                    Dispatcher.Invoke(() =>
                    {
                        if (_paused && _mediaPlayer.CanPause && _position>TimeSpan.Zero)
                        {
                            _mediaPlayer.Play();
                        }
                        else if(media != null) 
                        {
                            using var vlcMedia = new LibVLCSharp.Shared.Media(_libVLC, media.Uri);

                            if (IsLowResolution(media))
                            {
                                ConfigureRealTimeUpscale(vlcMedia);
                            }

                            _mediaPlayer.Play(vlcMedia);
                        }
                        else
                        {
                            _mediaPlayer.Play();
                        }

                            SetThreadExecutionState(BLOCK_SLEEP_MODE);
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: Error while playing media: {ex.Message}");
            }
        }

        public void Play(VideoItem media)
        {
            _Play(media);
        }

        public void Play()
        {
            if (Playlist.Current == null)
            {
                Console.WriteLine($"[VlcControlView]: Playlist is empty or current media is not set.");
                return;
            }
            _Play();
        }

        private void ConfigureRealTimeUpscale(LibVLCSharp.Shared.Media media, int targetWidth = 1920, int targetHeight = 1080)
        {
            try
            {
                media.AddOption(":video-filter=scale");
                media.AddOption($":scale-width={targetWidth}");
                media.AddOption($":scale-height={targetHeight}");
                media.AddOption(":video-filter=hqdn3d");
                Console.WriteLine($"[VlcControlView]: Applied real-time upscale to {targetWidth}x{targetHeight} with hqdn3d");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: Failed to apply upscale: {ex.Message}");
            }
        }

        private bool IsLowResolution(VideoItem media)
        {
            try
            {
                if (media != null && int.TryParse(media.FrameSize.Split('x')[0], out int width))
                {
                    this.WriteLine($"Resolution: {media.FrameSize}, try to upscale.");
                    return width < 1280;
                }
                return true;
            }
            catch
            {
                Console.WriteLine($"[VlcControlView]: Failed to check resolution with mediatoolkit, upscale off");
                return false;
            }
        }

        private void HandleRepeat(string repeat)
        {
            switch (repeat)
            {
                case "One":
                    base.Dispatcher.InvokeAsync(delegate
                    {
                        Playlist.Current.Play();
                    });
                    break;
                case "All":
                    base.Dispatcher.InvokeAsync(delegate
                    {
                        Next();
                    });
                    break;
                case "Random":
                    base.Dispatcher.InvokeAsync(delegate
                    {
                        if (Playlist.Items.Count > 0)
                        {
                            int randomIndex = _random.Next(0, Playlist.Items.Count);
                            if (randomIndex == Playlist.CurrentIndex)
                            {
                                randomIndex = (randomIndex + 1) % Playlist.Items.Count;
                            }
                            Playlist.CurrentIndex = randomIndex;
                            Playlist.Current.Play();
                        }
                        else
                        {
                            Stop();
                        }
                    });
                    break;
            }
        }

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

        private async Task<bool> IfMouseMoved()
        {
            _videoView.MouseMove += MouseMovedCallback;
            bool isMouseMove;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(MouseSleeps));
                isMouseMove = _isMouseMove;
            }
            finally
            {
                _videoView.MouseMove -= MouseMovedCallback;
                await _videoView.Dispatcher.InvokeAsync((Func<Task>)async delegate
                {
                    await ControlBar.HideByStoryboard((Storyboard)ControlBar.FindResource("fadeOutControlBar"));
                    await ProgressBar.HideByStoryboard((Storyboard)ProgressBar.FindResource("fadeOutProgressBar"));
                    _videoView.Cursor = Cursors.None;
                });
                _isMouseMove = false;
            }
            return isMouseMove;
            void MouseMovedCallback(object sender, MouseEventArgs e)
            {
                _isMouseMove = true;
                ControlBar.ShowByStoryboard((Storyboard)ControlBar.FindResource("fadeInControlBar")).GetAwaiter();
                ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar")).GetAwaiter();
                _videoView.Cursor = Cursors.Arrow;
            }
        }

        private async void OnMouseMove(object sender, MouseEventArgs e)
        {
            await ControlBar.ShowByStoryboard((Storyboard)ControlBar.FindResource("fadeInControlBar"));
            await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
            _videoView.Cursor = Cursors.Arrow;
        }

        private void OnPlaying(object sender, EventArgs e)
        {
            SetThreadExecutionState(BLOCK_SLEEP_MODE);
        }

        private void OnStopped(object sender, EventArgs e)
        {
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }

        private void OnPaused(object sender, EventArgs e)
        {
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }

        private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                ProgressBar.BufforBarValue = e.Cache;
            });
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
            ProgressBar.Value = (long)0.0;
            ProgressBar.ProgressText = "00 : 00 : 00/00 : 00 : 00";
            ProgressBar.PopupText = "Volume: 100";
            ControlBar.MediaTitle = Playlist.Current?.Name ?? "No video loaded";
        }

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
            PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }
    }
}
// Version: 0.1.0.14
