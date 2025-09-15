// Version: 0.1.2.34
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Microsoft.Win32;

using Thmd.Helpers;
using Thmd.Media;
using Thmd.Repeats;

using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;

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
        /// Gets or sets the visibility of the playlist.
        /// </summary>
        public Visibility PlaylistVisibility { get => _playlist.Visibility; set => _playlist.Visibility = value; }
        /// <summary>
        /// Gets or sets the progress bar control for displaying playback progress.
        /// </summary>
        public ProgressBarView ProgressBar { get => _progressBar; set => _progressBar = value; }
        public ControlBox ControlBox => throw new NotImplementedException(); // FOR REFATORING
        /// <summary>
        /// Gets or sets the control bar for playback controls (play, pause, stop, etc.).
        /// </summary>
        public ControlBar ControlBar { get => _controlBar; set => _controlBar = value; }
        /// <summary>
        /// Gets or sets the current playback position of the media.
        /// </summary>
        public TimeSpan Position { get => _position;
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
                _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)value;
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
                    _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = 0;
                }
                else
                {
                    _muted = false;
                    _vlcControl.SourceProvider.MediaPlayer.Audio.Volume = (int)_volume;
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
                _fullscreen = FullscreenHelper.IsFullscreen;
                /*if (!_fullscreen)
                    ControlBox.BtnFullscreen.Style = FindResource("FullscreenOn") as Style;
                else
                    ControlBox.BtnFullscreen.Style = FindResource("FullscreenOff") as Style;*/
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
        public event EventHandler<VlcMediaPlayerPlayingEventArgs> Playing;
        public event EventHandler<VlcMediaPlayerStoppedEventArgs> Stopped;
        public event EventHandler<VlcMediaPlayerLengthChangedEventArgs> LengthChanged;
        public event EventHandler<VlcMediaPlayerTimeChangedEventArgs> TimeChanged;

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

            // VlcConstrol events and values
            string[] mediaOptions = new string[] { "--no-video-title-show", "--no-sub-autodetect-file" };
            _vlcControl.SourceProvider.CreatePlayer(new DirectoryInfo(Path.Combine("libvlc", (IntPtr.Size == 4) ? "win-x86" : "win-x64")), mediaOptions);
            _vlcControl.SourceProvider.MediaPlayer.TimeChanged += OnTimeChanged;
            _vlcControl.SourceProvider.MediaPlayer.EndReached += OnEndReached;
            _vlcControl.SourceProvider.MediaPlayer.Playing += OnPlaying;
            _vlcControl.SourceProvider.MediaPlayer.Stopped += OnStopped;
            _vlcControl.SourceProvider.MediaPlayer.Paused += OnPaused;

            // Resize helpers for control bar and playlist
            var resizer1 = new ResizeControlHelper(_controlBar);
            var resizer2 = new ResizeControlHelper(_playlist);

            // Buttons event handlers
            ControlBarButtonEvent();

            // Mouse not moving worker
            _mouseNotMoveWorker = new BackgroundWorker();
            _mouseNotMoveWorker.DoWork += MouseNotMoveWorker_DoWork;
            _mouseNotMoveWorker.RunWorkerAsync();

            // Mouse and keyborad events
            this.MouseMove += OnMouseMove;
            this.KeyDown += PlayerView_KeyDown;
            this.KeyUp += PlayerView_KeyUp;
        }

        private void PlayerView_KeyUp(object sender, KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PlayerView_KeyDown(object sender, KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ControlBar_SliderVolume_MouseMove(object sender, MouseEventArgs e)
        {
            if(DesignerProperties.GetIsInDesignMode(this))
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

        private void MediaPlayer_Playing(object sender, VlcMediaPlayerPlayingEventArgs e)
        {
            if (Playing!=null)
            {
                _vlcControl.SourceProvider.MediaPlayer.Playing += (sender, e) =>
                {
                    Playing?.Invoke(sender, e);
                };                
            }
        }

        private void MediaPlayer_Stopped(object sender, VlcMediaPlayerStoppedEventArgs e)
        {
            if (Stopped != null)
            {
                _vlcControl.SourceProvider.MediaPlayer.Stopped += (sender, e) =>
                {
                    Stopped?.Invoke(sender, e);
                };
            }
        }

        private void MediaPlayer_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            if (TimeChanged != null)
            {
                _vlcControl.SourceProvider.MediaPlayer.TimeChanged += (sender, e) =>
                {
                    TimeChanged?.Invoke(sender, e);
                };
            }
        }

        private void MediaPlayer_LenghtChanges(object sender, VlcMediaPlayerLengthChangedEventArgs e)
        {
            if (LengthChanged != null)
            {
                _vlcControl.SourceProvider.MediaPlayer.LengthChanged += (sender, e) =>
                {
                    LengthChanged?.Invoke(sender, e);
                };
            }
        }

        private void OnEndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            _vlcControl.Dispatcher.InvokeAsync(() =>
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Stop();
                });
                var repeat = ControlBar.RepeatMode;
                HandleRepeat(repeat);
            });
         }

        private void OnTimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            _vlcControl.Dispatcher.InvokeAsync(() =>
            {
                _playlist.Current.Position = e.NewTime;
             
                _progressBar.Value = e.NewTime;
                _progressBar.Duration = _playlist.Current.Duration;
                _progressBar.ProgressText = string.Format("{0:00} : {1:00} : {2:00} / {3}", TimeSpan.FromMilliseconds(e.NewTime).Hours, TimeSpan.FromMilliseconds(e.NewTime).Minutes, TimeSpan.FromMilliseconds(e.NewTime).Seconds, ProgressBar.Duration.ToString("hh\\:mm\\:ss"));

                _controlBar.MediaTitle = _playlist.Current.Name;
                _controlBar.Position =TimeSpan.FromMilliseconds(e.NewTime).ToString("hh\\:mm\\:ss");
                _controlBar.Duration = _playlist.Current.Duration.ToString("hh\\:mm\\:ss");

                _subtitleControl.PositionTime = TimeSpan.FromMilliseconds(e.NewTime);
            });
        }

        private async void ProgressBar_MouseEnter(object sender, MouseEventArgs e)
        {
            
        }

        private async void ProgressBar_MouseLeave(object sender, MouseEventArgs e)
        {
            
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

        private void KeyboardButtonEvent()
        {

        }

        /// <summary>
        /// Sets up event handlers for control bar buttons (play, stop, next, previous, volume, etc.).
        /// </summary>
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
                if (_vlcControl.SourceProvider.MediaPlayer.Audio.IsMute)
                {
                   // ControlBar.BtnMute.Style = FindResource("Mute") as Style;
                    _volume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                    mute = "On";
                }
                else
                {
                   // ControlBar.BtnMute.Style = FindResource("Unmute") as Style;
                    _volume = _vlcControl.SourceProvider.MediaPlayer.Audio.Volume;
                    mute = "Off";
                }
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

        /// <summary>
        /// Loads and displays subtitles from a specified file path.
        /// </summary>
        /// <param name="path">The path to the subtitle file.</param>
        public void SetSubtitle(string path)
        {
            _vlcControl.Dispatcher.InvokeAsync(() =>
            {
                _subtitleControl.FilePath = path;
                _subtitleControl.TimeChanged += delegate (object sender, TimeSpan time)
                {
                    if (_vlcControl.SourceProvider.MediaPlayer != null)
                    {
                        _subtitleControl.PositionTime = time;
                    }
                };
            });
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
                    _stopped = false;
                    _vlcControl.SourceProvider.MediaPlayer?.SetPause(true);
                    SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: {ex.Message}");
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
                    _stopped = true;
                    _vlcControl.SourceProvider.MediaPlayer?.Stop();
                    SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays the next video in the playlist.
        /// </summary>
        public void Next()
        {
            Stop();
            Playlist.MoveNext.Play();
        }

        /// <summary>
        /// Plays the previous video in the playlist.
        /// </summary>
        public void Preview()
        {
            Stop();
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
        /// Plays the specified media or the current playlist item.
        /// </summary>
        /// <param name="media">The video to play, or null to play the current playlist item.</param>
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
                    
                    if (_paused)
                        _vlcControl.SourceProvider.MediaPlayer?.SetPause(false);
                    else if(media==null)
                        _vlcControl.SourceProvider.MediaPlayer?.Play();
                    else
                        _vlcControl.SourceProvider.MediaPlayer?.Play(media.Uri);

                    SetThreadExecutionState(BLOCK_SLEEP_MODE);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VlcControlView]: {ex.Message}");
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
                Console.WriteLine($"[VlcControlView]: Playlist is empty or current media is not set.");
                return;
            }
            _Play();
        }

        /// <summary>
        /// Handles repeat logic based on the current repeat mode.
        /// </summary>
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
                                randomIndex = (randomIndex + 1) % Playlist.Items.Count; // Ensure we don't repeat the current video
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
                    await ControlBar.HideByStoryboard((Storyboard)ControlBar.FindResource("fadeOutControlBar"));
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
            await ControlBar.ShowByStoryboard((Storyboard)ControlBar.FindResource("fadeInControlBar"));
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
            SetThreadExecutionState(BLOCK_SLEEP_MODE);
        }

        /// <summary>
        /// Handles the Stopped event of the media player, stopping playback and updating the system state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnStopped(object sender, VlcMediaPlayerStoppedEventArgs e)
        {
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }

        /// <summary>
        /// Handles the Paused event of the media player, updating the system state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPaused(object sender, VlcMediaPlayerPausedEventArgs e)
        {
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
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
        /// Handles the Loaded event to initialize the progress bar and control bar with the current video's information.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
            ProgressBar.Value = (long)0.0;
            ProgressBar.ProgressText = "00 : 00 : 00/00 : 00 : 00";
            ProgressBar.PopupText = "Volume: 100";
            ControlBar.MediaTitle = Playlist.Current?.Name ?? "No video loaded";
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
            PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event to notify the UI of property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            _vlcControl.SourceProvider.MediaPlayer?.Dispose();
            _vlcControl.SourceProvider.Dispose();
            _vlcControl.Dispose();
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }
    }
}
