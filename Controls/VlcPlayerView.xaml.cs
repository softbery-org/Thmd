// Version: 0.1.7.32
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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

using LibVLCSharp.Shared;

using Microsoft.Win32;

using Thmd.Configuration;
using Thmd.Consolas;
using Thmd.Devices.Keyboards;
using Thmd.Media;
using Thmd.Utilities;

namespace Thmd.Controls
{
    /// <summary>
    /// Logic interaction for class VlcPlayerView.xaml
    /// </summary>
    public partial class VlcPlayerView : UserControl, IPlayer, INotifyPropertyChanged
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

        private VLCState _state;

        /// <summary>
        /// Gets or sets the current status of the media player.
        /// </summary>
        public PlaylistView Playlist {
            get => _playlist;
            private set
            {
                if (_playlist == null)
                {
                    _playlist = new PlaylistView(this);
                }
                _playlist = value;
            }
        }
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
                if (value < 0.0) value = 0.0;
                else if (value > 100.0) value = 100.0;
                _volume = value;
                _controlBar.SliderVolume.Value = value;
                _mediaPlayer.Volume = (int)value; // Ensure this is applied
                OnPropertyChanged(nameof(Volume), ref _volume, value);
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

        /// <summary>
        /// Mouse sleep, time with controls like ControlBar or ProgressBar are hide.
        /// </summary>
        public double MouseSleeps { get; private set; } = 7;
        /// <summary>
        /// 
        /// </summary>
        public VLCState State
        {
            get => _mediaPlayer.State;
            set {
                OnPropertyChanged(nameof(State));
            }
        }

        public bool isUpscale
        {
            get => _isUpscale;
            set
            {
                _isUpscale = value;
                OnPropertyChanged(nameof(isUpscale), ref _isUpscale, value);
            }
        }

        /// <summary>
        /// Occurs when a property value changes, used for data binding.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> Playing;
        public event EventHandler<EventArgs> Stopped;
        public event EventHandler<EventArgs> LengthChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private double _volume = 100.0;
        private bool _muted;
        private bool _playing;
        private bool _paused;
        private bool _stopped;
        private bool _fullscreen = false;
        private TimeSpan _position;
        private readonly Random _random = new Random();
        private VideoItem _media;
        private Visibility _subtitleVisibility = Visibility.Hidden;
        private Visibility _playlistVisibility = Visibility.Hidden;
        private bool _isMouseMove;
        private BackgroundWorker _mouseNotMoveWorker;
        private bool _isUpscale = false;

        /// <summary>
        /// Initialize class
        /// </summary>
        public VlcPlayerView()
        {
            InitializeComponent();
            Core.Initialize();

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
            string[] mediaOptions = new string[] { "--no-video-title-show", "--no-sub-autodetect-file" };//,"--verbose=2","--aout=any" };
            _libVLC = new LibVLC(mediaOptions);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _mediaPlayer.EnableHardwareDecoding = false;
            _mediaPlayer.EnableKeyInput = false;
            _mediaPlayer.EnableMouseInput = false;
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.EndReached += OnEndReached;
            _mediaPlayer.Playing += OnPlaying;
            _mediaPlayer.Stopped += OnStopped;
            _mediaPlayer.Paused += OnPaused;
            _mediaPlayer.Buffering += OnBuffering;
            _mediaPlayer.MediaChanged += OnMediaChanged;
            _mediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;

            _mediaPlayer.Volume = (int)_volume;

            _videoView.MediaPlayer = _mediaPlayer;
            _videoView.Background = Brushes.Black;

            // Resize helpers for control bar and playlist
            var resizer1 = new ResizeControlHelper(_controlBar);
            var resizer2 = new ResizeControlHelper(_playlist);

            // Buttons event handlers
            ControlBarButtonEvent();

            // Mouse not moving worker
            _mouseNotMoveWorker = new BackgroundWorker();
            _mouseNotMoveWorker.DoWork += MouseNotMoveWorker_DoWork;
            _mouseNotMoveWorker.RunWorkerAsync();

            LoadPlaylistConfig();
            SaveUpdateConfig();
            LoadUpdateConfig();
        }

        public void LoadUpdateConfig()
        {
            var up = Configuration.Config.LoadFromJsonFile<UpdateConfig>("config/update.json");
            Config.Instance.UpdateConfig = up;
        }

        public void SaveUpdateConfig()
        {
            var up = new UpdateConfig();
            up.CheckForUpdates = true;
            up.UpdateUrl = "http://thmdplayer.softbery.org/themedit.zip";
            up.UpdatePath = "update";
            up.UpdateFileName = "themedit.zip";
            up.Version = "4.0.0";
            up.VersionUrl = "http://thmdplayer.softbery.org/version.txt";
            up.UpdateInterval = 86400;
            up.UpdateTimeout = 30;

            Configuration.Config.SaveToFile("config/update.json", up);
        }

        public void LoadPlaylistConfig()
        {
            try
            {
                var pl = Configuration.Config.LoadFromJsonFile<PlaylistConfig>("config/playlist.json");
                
                _controlBar.RepeatMode = pl.Repeat;

                for (int i = 0; i < pl.MediaList.Count; i++)
                {
                    var media = new VideoItem(pl.MediaList[i]);
                    media.SubtitlePath = pl.Subtitles[i];
                    
                    _playlist.AddAsync(media);
                }
                _playlist.Width = pl.Size.Width;
                _playlist.Height = pl.Size.Height;
                _playlist.CurrentIndex = pl.Current;
                _playlist.SelectedIndex = pl.Current;
                _playlist.Margin = new Thickness(pl.Position.X, pl.Position.Y, 0, 0);
                _playlist.Visibility = pl.SubtitleVisible ? Visibility.Visible : Visibility.Hidden;

                this.WriteLine($"Playlist config was read succesfull");
            }
            catch (Exception ex)
            {
                this.WriteLine($"{ex.Message}");
            }
        }

        public void SavePlaylistConfig()
        {
            var pl = new Configuration.PlaylistConfig();
            pl.Repeat = (string)_controlBar._repeatComboBox.SelectedItem;
            pl.AutoPlay = true;
            pl.EnableShuffle = true;
            foreach (var item in this.Playlist.Videos)
            {
                pl.MediaList.Add(item.Uri.LocalPath);
                pl.Subtitles.Add((item.SubtitlePath != null) ? item.SubtitlePath : null);
            }
            pl.Size = new Size(Playlist.Width, Playlist.Height);
            pl.Current = _playlist.CurrentIndex;
            pl.Position = new Point(Playlist.Margin.Left, Playlist.Margin.Top);
            Configuration.Config.SaveToFile("config/playlist.json", pl);

            this.WriteLine($"Save playlist in config/playlist.json");
        }

        public void ApplyGrayscaleEffect(Image image)
        {
            // Tworzenie bitmapy na podstawie wymiar�w odtwarzacza
            WriteableBitmap bitmap = new WriteableBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Gray256Transparent);

            // Przyk�adowa modyfikacja pikseli (np. konwersja do szaro�ci)
            bitmap.Lock();
            unsafe
            {
                byte* pixels = (byte*)bitmap.BackBuffer;
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        int index = (y * bitmap.BackBufferStride) + (x * 4); // BGRA: 4 bajty na piksel
                        byte gray = (byte)((pixels[index + 2] + pixels[index + 1] + pixels[index]) / 3); // �rednia RGB -> szary
                        pixels[index] = gray;     // B
                        pixels[index + 1] = gray; // G
                        pixels[index + 2] = gray; // R
                                                  // pixels[index + 3] = alfa (pozostawiamy bez zmian)
                    }
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();

            // Przypisanie do kontrolki Image w XAML
            _image.Source = bitmap;
        }

        public BitmapSource GetCurrentFrame()
        {
            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
            {
                return null; // Brak danych, jeśli nie odtwarza
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    // Pobranie rozmiaru okna wideo
                    //var windowSize = _videoView.Width;
                    uint width = (uint)Math.Round(_videoView.Width);  // Konwersja double na int z zaokrągleniem
                    uint height = (uint)Math.Round(_videoView.Height); // Konwersja double na int z zaokrągleniem

                    // Walidacja rozmiaru
                    if (width <= 0 || height <= 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Nieprawidłowy rozmiar okna wideo.");
                        return null;
                    }

                    // Pobranie snapshotu do strumienia
                    var result = _mediaPlayer.TakeSnapshot(
                        _videoView.MediaPlayer.FileCaching,          // Strumień do zapisu
                        null,
                        (uint)width,       // Szerokość w pikselach
                        (uint)height       // Wysokość w pikselach
                    );

                    if (!result)
                    {
                        System.Diagnostics.Debug.WriteLine($"Błąd pobierania snapshotu, kod: {result}");
                        return null;
                    }

                    // Przywrócenie pozycji strumienia na początek
                    ms.Seek(0, SeekOrigin.Begin);

                    // Utworzenie BitmapSource z danych strumienia
                    var bitmapFrame = BitmapFrame.Create(
                        ms,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.OnLoad
                    );
                    _image.Source = bitmapFrame;
                    return bitmapFrame;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd pobierania klatki: {ex.Message}");
                return null;
            }
        }

        #region Mouse events
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
                // and run protected override async void OnMouseMove
            }
        }

        protected override async void OnMouseMove(MouseEventArgs e)
        {
            await ControlBar.ShowByStoryboard((Storyboard)ControlBar.FindResource("fadeInControlBar"));
            await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
            _videoView.Cursor = Cursors.Arrow;
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Double-click to toggle fullscreen
                Fullscreen = !Fullscreen;
            }
            base.OnMouseDoubleClick(e);
        }


        [NonSerialized]
        private DateTime _lastClickTime = DateTime.MinValue;
        [NonSerialized]
        private Point _lastClickPosition;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DateTime now = DateTime.Now;
            Point pos = e.GetPosition(this);
            if ((now - _lastClickTime).TotalMilliseconds < System.Windows.Forms.SystemInformation.DoubleClickTime &&
                Math.Abs(pos.X - _lastClickPosition.X) <= 4 &&
                Math.Abs(pos.Y - _lastClickPosition.Y) <= 4)
            {
                OnMouseDoubleClick(e);
                e.Handled = true;
            }

            if (e.ChangedButton == MouseButton.Right)
                TogglePlaylist()();
            else if (e.ChangedButton == MouseButton.Left)
                TogglePlayPause()();

            _lastClickTime = now;
            _lastClickPosition = pos;

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            // Adjust volume with mouse wheel
            double volumeChange = e.Delta > 0 ? 5.0 : -5.0;
            Volume += volumeChange;
            _controlBar.SliderVolume.Value = Volume;
            _progressBar.PopupText = $"Volume: {(int)Volume}";
            _progressBar._popup.IsOpen = true;

            // Show popup briefly
            Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(() => _progressBar._popup.IsOpen = false));
        }

        /*protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
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
        }*/
        #endregion

        #region Keybord events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var keyBindingList = new List<ShortcutKeyBinding>
            {
                new ShortcutKeyBinding { MainKey = Key.Space, ModifierKey = null, Shortcut = "Space", Description = "Pause and play media", RunAction = TogglePlayPause() },
                new ShortcutKeyBinding { MainKey = Key.F, ModifierKey = null, Shortcut = "F", Description = "Toggle fullscreen", RunAction = ToggleFullscreen() },
                new ShortcutKeyBinding { MainKey = Key.H, ModifierKey = null, Shortcut = "H", Description = "Toggle help window", RunAction = ToggleHelpWindow() },
                new ShortcutKeyBinding { MainKey = Key.P, ModifierKey = null, Shortcut = "P", Description = "Toggle playlist", RunAction = TogglePlaylist() },
                new ShortcutKeyBinding { MainKey = Key.S, ModifierKey = null, Shortcut = "S", Description = "Toggle subtitle", RunAction = ToggleSubtitle() },
                new ShortcutKeyBinding { MainKey = Key.Left, ModifierKey = null, Shortcut = "Left", Description = "Move media backward 5 seconds", RunAction = () => Seek(TimeSpan.FromSeconds(5), SeekDirection.Backward) },
                new ShortcutKeyBinding { MainKey = Key.Right, ModifierKey = null, Shortcut = "Right", Description = "Move media forward 5 seconds", RunAction = () => Seek(TimeSpan.FromSeconds(5), SeekDirection.Forward) },
                new ShortcutKeyBinding { MainKey = Key.Left, ModifierKey = ModifierKeys.Control, Shortcut = "Ctrl+Left", Description = "Move media backward 5 minutes", RunAction = MoveBackwardMinutes() },
                new ShortcutKeyBinding { MainKey = Key.Right, ModifierKey = ModifierKeys.Control, Shortcut = "Ctrl+Right", Description = "Move media forward 5 minutes", RunAction = MoveForwardMinutes() },
                new ShortcutKeyBinding { MainKey = Key.Up, ModifierKey = null, Shortcut = "Up", Description = "Increase volume by 2", RunAction = () => Volume += 2 },
                new ShortcutKeyBinding { MainKey = Key.Down, ModifierKey = null, Shortcut = "Down", Description = "Decrease volume by 2", RunAction = () => Volume -= 2 },
                new ShortcutKeyBinding { MainKey = Key.M, ModifierKey = null, Shortcut = "M", Description = "Toggle mute", RunAction = () => isMute = !isMute },
                new ShortcutKeyBinding { MainKey = Key.L, ModifierKey = null, Shortcut = "L", Description = "Toggle lector if subtitles are available", RunAction = ToggleLector() },
                new ShortcutKeyBinding { MainKey = Key.Escape, ModifierKey = null, Shortcut = "Esc", Description = "Clear focus, minimize fullscreen", RunAction = ClearFocus() },
                new ShortcutKeyBinding { MainKey = Key.N, ModifierKey = null, Shortcut = "N", Description = "Play next video", RunAction = () => Next() },
                new ShortcutKeyBinding { MainKey = Key.P, ModifierKey = ModifierKeys.Control, Shortcut = "Ctrl+P", Description = "Play previous video", RunAction = () => Preview() },
            };

            foreach (var key in keyBindingList)
            {
                if (e.Key == key.MainKey && key.ModifierKey == null && e.IsDown)
                {
                    key.RunAction();
                }
                else if (e.Key == key.MainKey && key.ModifierKey == Keyboard.Modifiers && e.IsDown)
                {
                    key.RunAction();
                }
            }

            if (e.IsDown)
            {
                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    this.WriteLine($"Pressed: {e.Key}");
                }
                else
                    this.WriteLine($"Pressed: [{Keyboard.Modifiers}]+ {e.Key}");

            }

            //base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            // Handle key release if needed
        }
        #endregion

        #region Actions
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
                Console.WriteLine("[VlcPlayerView]: Toggling lector (not implemented)");
            });
        }

        private Action ClearFocus()
        {
            return new Action(() =>
            {
                this.Focus();
            });
        }

        private Action TogglePlayPause()
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
                else if (!isPlaying) {
                    Play(_playlist.Current);
                }
            });
        }

        private Action ToggleFullscreen()
        {
            return new Action(() =>
            {
                //this.Fullscreen();
                _videoView.Background = Brushes.Black;
                _fullscreen = ScreenHelper.IsFullscreen;
            });
        }

        private Action ToggleHelpWindow()
        {
            return new Action(() =>
            {
                // Placeholder for help window toggle
                Console.WriteLine("[VlcPlayerView]: Toggling help window (not implemented)");
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
        #endregion

        #region ControlBar -> SliderVolume -> Mouse Events

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
        #endregion

        #region MediaPlayer Events
        private void MediaPlayer_VolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
        {

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

        private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            
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
        #endregion


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
                TogglePlayPause()();
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

        private async void OpenMediaFile()
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
                    await Playlist.AddAsync(new VideoItem(path));
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
                Console.WriteLine($"[VlcPlayerView]: {ex.Message}");
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
                Console.WriteLine($"[VlcPlayerView]: {ex.Message}");
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
                ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar")).GetAwaiter();
                _mediaPlayer.Time = (long)time.TotalMilliseconds;
            }
        }

        public void Seek(TimeSpan time, SeekDirection direction)
        {
            if (_mediaPlayer != null)
            {
                ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar")).GetAwaiter();
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
                Console.WriteLine($"[VlcPlayerView]: Playlist is empty or current media is not set.");
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
                        if (_paused && _mediaPlayer.CanPause && _position > TimeSpan.Zero)
                        {
                            _mediaPlayer.Play();
                        }
                        else if (media != null)
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
                Console.WriteLine($"[VlcPlayerView]: Error while playing media: {ex.Message}");
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
                Console.WriteLine($"[VlcPlayerView]: Playlist is empty or current media is not set.");
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

                this.WriteLine($"Applied real-time upscale to {targetWidth}x{targetHeight} with hqdn3d");
            }
            catch (Exception ex)
            {
                this.WriteLine($"Failed to apply upscale: {ex.Message}");
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
                Console.WriteLine($"[VlcPlayerView]: Failed to check resolution with mediatoolkit, upscale off");
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
            SavePlaylistConfig();
            _libVLC?.Dispose();
            SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
        }
    }
}
