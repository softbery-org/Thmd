// Version: 0.1.9.3
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Shapes;

using LibVLCSharp.Shared;

using Microsoft.VisualBasic;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;

using Thmd.Configuration;
using Thmd.Consolas;
using Thmd.Converters;
using Thmd.Devices.Keyboards;
using Thmd.Media;
using Thmd.Utilities;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Thmd.Controls;

/// <summary>
/// Logic interaction for class VlcPlayerView.xaml
/// Represents a WPF UserControl that integrates a VLC media player for video playback with controls, playlist, and subtitle support.
/// Implements IPlayer interface for media operations and INotifyPropertyChanged for data binding.
/// </summary>
public partial class VlcPlayerView : UserControl, IPlayer, INotifyPropertyChanged
{
    // System execution state flags to prevent system sleep during playback.
    private const uint ES_CONTINUOUS = 2147483648u;
    private const uint ES_SYSTEM_REQUIRED = 1u;
    private const uint ES_DISPLAY_REQUIRED = 2u;
    private const uint ES_AWAYMODE_REQUIRED = 64u;

    // When you don't move mouse or keybord this IntPtr:
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

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private VLCState _state;

    public static bool IsConsole()
    {
        return GetConsoleWindow() != IntPtr.Zero;
    }

    /// <summary>
    /// Gets or sets the current status of the media player.
    /// </summary>
    public PlaylistView Playlist
    {
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
    /// Gets or sets the current VLC player state.
    /// </summary>
    public VLCState State
    {
        get => _mediaPlayer.State;
        set
        {
            OnPropertyChanged(nameof(State));
        }
    }

    /// <summary>
    /// Gets or sets whether upscale is enabled for low-resolution media.
    /// </summary>
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
    private Visibility _addStreamViewVisibility = Visibility.Hidden;
    private bool _isMouseMove;
    private BackgroundWorker _mouseNotMoveWorker;
    private bool _isUpscale = false;

    /// <summary>
    /// Initialize class
    /// Initializes the VLC player view, sets up controls, events, and loads configurations.
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
        _mediaPlayer.VolumeChanged += OnVolumeChanged;

        _mediaPlayer.Volume = (int)_volume;

        _videoView.MediaPlayer = _mediaPlayer;
        _videoView.Background = Brushes.Black;

        // Resize helpers for control bar and playlist
        var resizer1 = new ResizeControlHelper(_controlBar);
        var resizer2 = new ResizeControlHelper(_playlist);
        //var resizer3 = new ResizeControlHelper(_addStreamView);

        // Buttons event handlers
        ControlBarButtonEvent();

        // Mouse not moving worker
        _mouseNotMoveWorker = new BackgroundWorker();
        _mouseNotMoveWorker.DoWork += MouseNotMoveWorker_DoWork;
        _mouseNotMoveWorker.RunWorkerAsync();

        LoadPlaylistConfig_Question();
        LoadOpenAiConfig();
        SaveUpdateConfig();
        LoadUpdateConfig();
    }

    /// <summary>
    /// Loads the OpenAI configuration from JSON file.
    /// </summary>
    public void LoadOpenAiConfig()
    {
        var ai = Configuration.Config.LoadFromJsonFile<OpenAiConfig>("config/openai.json");
        Config.Instance.OpenAiConfig = ai;
    }

    /// <summary>
    /// Loads the update configuration from JSON file.
    /// </summary>
    public void LoadUpdateConfig()
    {
        var up = Configuration.Config.LoadFromJsonFile<UpdateConfig>("config/update.json");
        Config.Instance.UpdateConfig = up;
    }

    /// <summary>
    /// Saves the default update configuration to JSON file.
    /// </summary>
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

    /// <summary>
    /// Loads the playlist configuration from JSON file and applies it to the player.
    /// </summary>
    public void LoadPlaylistConfig_Question()
    {
        Loaded += async (s, e) =>
        {
            //Startup with question to load previous playlist
            //if (!Config.Instance.Question_AutoLoadPlaylist)
            //    return;

            //if (!IsConsole())
            //{
            //    var result = MessageBox.Show("Do you want to load the previous playlist?", "Load Playlist", MessageBoxButton.YesNo, MessageBoxImage.Question);
            //    if (result == MessageBoxResult.Yes)
            //    {
            //        await LoadPlaylistConfigAsync();
            //    }
            //}
            //else
            //    await LoadPlaylistConfigAsync();

            // Directly load playlist without question
            await LoadPlaylistConfigAsync();
        };
    }

    private async Task LoadPlaylistConfigAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var pl = Configuration.Config.LoadFromJsonFile<PlaylistConfig>("config/playlist.json");

                Dispatcher.Invoke(async() =>
                {
                    _controlBar.RepeatMode = pl.Repeat;

                    for (int i = 0; i < pl.MediaList.Count; i++)
                    {
                        var item = new VideoItem(pl.MediaList[i], deferMetadata: true);
                        var media = new VideoItem(pl.MediaList[i]);
                        media.SubtitlePath = pl.Subtitles[i];
                        media.Indents = pl.Indents;
                        //_playlist.AddAsync(media);
                        await _playlist.AddAsync(item);
                    }

                    _playlist.Width = pl.Size.Width;
                    _playlist.Height = pl.Size.Height;
                    _playlist.CurrentIndex = pl.Current;
                    _playlist.SelectedIndex = pl.Current;
                    _playlist.Margin = new Thickness(pl.Position.X, pl.Position.Y, 0, 0);
                    _playlist.Visibility = pl.SubtitleVisible ? Visibility.Visible : Visibility.Hidden;

                    this.WriteLine("Playlist config loaded successfully (async).");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    this.WriteLine($"Error loading playlist config: {ex.Message}");
                });
            }
        });
    }

    /// <summary>
    /// Saves the current playlist configuration to JSON file.
    /// </summary>
    public void SavePlaylistConfig()
    {
        var pl = new Configuration.PlaylistConfig();
        pl.Repeat = (string)_controlBar._repeatComboBox.SelectedItem;
        pl.AutoPlay = true;
        pl.EnableShuffle = true;
        foreach (var item in this.Playlist.Videos)
        {
            // Support both local paths and network URLs by saving the original URI string
            pl.MediaList.Add(item.Uri.OriginalString);
            pl.Subtitles.Add((item.SubtitlePath != null) ? item.SubtitlePath : null);
            //if (item.Id == 1)
            //{
            //    pl.Indents.Add(new VideoIndent() { Id = 0, Name = "Jump", Start = TimeSpan.Parse("00:01:16"), End = TimeSpan.Parse("00:01:25") });
            //}
        }
        pl.Size = new Size(Playlist.Width, Playlist.Height);
        pl.Current = _playlist.CurrentIndex;
        pl.Position = new Point(Playlist.Margin.Left, Playlist.Margin.Top);
        Configuration.Config.SaveToFile("config/playlist.json", pl);

        this.WriteLine($"Save playlist in config/playlist.json");
    }

    /// <summary>
    /// Applies a grayscale effect to the specified image.
    /// </summary>
    /// <param name="image">The image to apply the effect to.</param>
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
        image.Source = bitmap;
    }

    /// <summary>
    /// Captures the current video frame as a BitmapSource.
    /// </summary>
    /// <returns>The captured frame or null if capture fails.</returns>
    public BitmapSource GetCurrentFrame()
    {
        /*if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
        {
            return null; // Brak danych, jeśli nie odtwarza
        }

        try
        {
            using (var ms = new MemoryStream())
            {
                // Pobranie rozmiaru okna wideo
                //var windowSize = _videoView.Width;
                uint actualWidth = (uint)Math.Round(_videoView.Width);  // Konwersja double na int z zaokrągleniem
                uint height = (uint)Math.Round(_videoView.Height); // Konwersja double na int z zaokrągleniem

                // Walidacja rozmiaru
                if (actualWidth <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Nieprawidłowy rozmiar okna wideo.");
                    return null;
                }

                // Pobranie snapshotu do strumienia
                var result = _mediaPlayer.TakeSnapshot(
                    (uint)_mediaPlayer.Hwnd,          // Strumień do zapisu
                    null,
                    (uint)actualWidth,       // Szerokość w pikselach
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
            }*/
        if (_mediaPlayer.IsPlaying)
        {
            // Opcje: szerokość, wysokość, ścieżka (domyślnie bieżący katalog), format (png/jpg)
            var result = _playlist.Current.FrameSize.Split('x');
            var width = uint.Parse(result[0]);
            var height = uint.Parse(result[1]);
            bool success = _mediaPlayer.TakeSnapshot(0, null, width, height);

            if (success)
            {
                MessageBox.Show("Klatka przrekazana ");
            }
            else
            {
                MessageBox.Show("Błąd przechwytywania – sprawdź, czy wideo jest odtwarzane.");
            }
            /*
        }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd pobierania klatki: {ex.Message}");
                return null;
            }*/
        }
        return null;
    }

    #region Mouse events
    /// <summary>
    /// Background worker method to handle mouse inactivity and hide controls after a delay.
    /// </summary>
    /// <param name="sender">The event sender.</param>
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
    /// Checks if the mouse has moved during the sleep delay and hides controls if not.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Handles mouse move events to show controls and set cursor.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override async void OnMouseMove(MouseEventArgs e)
    {
        await ControlBar.ShowByStoryboard((Storyboard)ControlBar.FindResource("fadeInControlBar"));
        await ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar"));
        _videoView.Cursor = Cursors.Arrow;
    }

    /// <summary>
    /// Handles double-click to toggle fullscreen mode.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
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
    /// <summary>
    /// Handles left mouse button down events, including double-click detection and play/pause toggle.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
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

    /// <summary>
    /// Handles mouse wheel events to adjust volume.
    /// </summary>
    /// <param name="e">The mouse wheel event arguments.</param>
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
    #endregion

    #region Keybord events
    /// <summary>
    /// Handles key down events for media control shortcuts.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
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
                new ShortcutKeyBinding { MainKey = Key.M, ModifierKey = null, Shortcut = "M", Description = "Toggle mute_txt", RunAction = () => isMute = !isMute },
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
    }

    /// <summary>
    /// Handles key up events.
    /// </summary>
    /// <param name="e">The key event arguments.</param>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        // Handle key release if needed
    }
    #endregion

    #region Actions
    /// <summary>
    /// Toggles subtitle visibility and opens subtitle file if hidden.
    /// </summary>
    /// <returns>An action to toggle subtitles.</returns>
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

    /// <summary>
    /// Toggles lector functionality (placeholder).
    /// </summary>
    /// <returns>An action to toggle lector.</returns>
    private Action ToggleLector()
    {
        return new Action(() =>
        {
            // Placeholder for lector toggle functionality
            Console.WriteLine("[VlcPlayerView]: Toggling lector (not implemented)");
        });
    }

    /// <summary>
    /// Clears focus on the player.
    /// </summary>
    /// <returns>An action to clear focus.</returns>
    private Action ClearFocus()
    {
        return new Action(() =>
        {
            this.Focus();
        });
    }

    /// <summary>
    /// Toggles play/pause or starts playback if stopped.
    /// </summary>
    /// <returns>An action to toggle play/pause.</returns>
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
            else if (!isPlaying)
            {
                Play(_playlist.Current);
            }
        });
    }

    /// <summary>
    /// Toggles url view controler show or hide
    /// </summary>
    /// <returns>Visible or Hidden</returns>
    public Action ToggleStreamUrl()
    {
        return new Action(() =>
        {
            //if (_addStreamView.Visibility == Visibility.Visible)
               // _addStreamView.Visibility = Visibility.Hidden;
            //else
               // _addStreamView.Visibility = Visibility.Visible;
        });
    }

    /// <summary>
    /// Toggles fullscreen mode.
    /// </summary>
    /// <returns>An action to toggle fullscreen.</returns>
    private Action ToggleFullscreen()
    {
        return new Action(() =>
        {
            _videoView.Background = Brushes.Black;
            _fullscreen = ScreenHelper.IsFullscreen;
        });
    }

    /// <summary>
    /// Toggles help window (placeholder).
    /// </summary>
    /// <returns>An action to toggle help window.</returns>
    private Action ToggleHelpWindow()
    {
        return new Action(() =>
        {
            // Placeholder for help window toggle
            Console.WriteLine("[VlcPlayerView]: Toggling help window (not implemented)");
        });
    }

    /// <summary>
    /// Toggles playlist visibility.
    /// </summary>
    /// <returns>An action to toggle playlist.</returns>
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

    /// <summary>
    /// Seeks forward by 5 minutes.
    /// </summary>
    /// <returns>An action to move forward 5 minutes.</returns>
    private Action MoveForwardMinutes()
    {
        return new Action(() =>
        {
            Seek(TimeSpan.FromMinutes(5), SeekDirection.Forward);
        });
    }

    /// <summary>
    /// Seeks backward by 5 minutes.
    /// </summary>
    /// <returns>An action to move backward 5 minutes.</returns>
    private Action MoveBackwardMinutes()
    {
        return new Action(() =>
        {
            Seek(TimeSpan.FromMinutes(5), SeekDirection.Backward);
        });
    }
    #endregion

    #region ControlBar -> SliderVolume -> Mouse Events

    /// <summary>
    /// Handles mouse move on volume slider to update volume preview.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
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

    /// <summary>
    /// Handles mouse down on volume slider.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse button event arguments.</param>
    private void ControlBar_SliderVolume_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ControlBar_SliderVolume_MouseEventHandler(sender, e);
    }

    /// <summary>
    /// Common handler for volume slider mouse events.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
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
    /// <summary>
    /// Handles volume change events from the media player.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The volume changed event arguments.</param>
    private void OnVolumeChanged(object sender, MediaPlayerVolumeChangedEventArgs e)
    {

    }

    /// <summary>
    /// Subscribes to the playing event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
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

    /// <summary>
    /// Subscribes to the stopped event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
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

    /// <summary>
    /// Subscribes to the time changed event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The media changed event arguments.</param>
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

    /// <summary>
    /// Subscribes to the length changed event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The length changed event arguments.</param>
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

    /// <summary>
    /// Handles end of media playback and manages repeat behavior.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
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

    /// <summary>
    /// Handles media change events.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The media changed event arguments.</param>
    private void OnMediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
    {

    }

    /// <summary>
    /// Updates UI elements with current playback time and progress.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The time changed event arguments.</param>
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

            foreach (var item in this.Playlist.Current.Indents)
            {
                if (this.Playlist.Current.isIndents)
                {
                    if (TimeSpan.FromMilliseconds(e.Time) >= item.Start && TimeSpan.FromMilliseconds(e.Time) <= item.End)
                    {
                        Seek(item.End, SeekDirection.Forward);
                        break;
                    }
                }
            }
            
        });
    }
    #endregion


    /// <summary>
    /// Handles mouse move on progress bar to preview seek position.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        Point mousePosition = e.GetPosition(_progressBar);
        double actualWidth = _progressBar.ActualWidth;
        if (actualWidth <= 0) return;

        var time = TimeToPositionConverter.Convert(mousePosition.X, actualWidth, _progressBar._progressBar.Maximum);
        _progressBar.PopupText = $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        _progressBar._popup.IsOpen = true;
        _progressBar._popup.HorizontalOffset = mousePosition.X - (_progressBar._popupText.ActualWidth / 2);

        _progressBar._rectangleMouseOverPoint.Margin = new Thickness(mousePosition.X - (_progressBar._rectangleMouseOverPoint.Width / 2), 0, 0, 0);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.Position = time;
        }
    }

    /// <summary>
    /// Handles mouse down on progress bar to seek.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse button event arguments.</param>
    private void ProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ProgressBarMouseEventHandler(sender, e);
    }

    /// <summary>
    /// Common handler for progress bar mouse events to perform seeking.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        Point mousePosition = e.GetPosition(_progressBar);
        double width = _progressBar.ActualWidth;
        if (width <= 0) return;

        // Calculate the corresponding time from the mouse position using the forward converter
        var time = TimeToPositionConverter.Convert(mousePosition.X, width, _progressBar._progressBar.Maximum);

        // Set the progress bar value to the milliseconds (as long) and seek the player
        _progressBar.Value = (long)time.TotalMilliseconds;
        this.Position = time;

        // Update the visual indicator for the mouse position
        _progressBar._rectangleMouseOverPoint.Margin = new Thickness(mousePosition.X - (_progressBar._rectangleMouseOverPoint.Width / 2), 0, 0, 0);
    }

    /// <summary>
    /// Sets up event handlers for control bar buttons.
    /// </summary>
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
            var mute_txt = String.Empty;

            if (_mediaPlayer.Mute)
                mute_txt = "On";
            else
                mute_txt = "Off";



            _volume = _mediaPlayer.Volume;
            isMute = !isMute;
        };
        ControlBar.BtnOpen.Click += delegate
        {
            OpenMediaFile();
        };
        // Assuming a BtnStream button exists in ControlBar for network streams
        ControlBar.BtnStream.Click += delegate
        {
            ToggleStreamUrl();

            //if (!_addStreamView._addButton.IsCancel)
            //{
            //    OpenNetworkStream(_addStreamView.ReturnUrl);
            //    _addStreamView.Visibility = Visibility.Hidden;
            //}
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

    /// <summary>
    /// Opens a file dialog to select and add media files to the playlist.
    /// </summary>
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

    /// <summary>
    /// Opens a dialog to input a network stream URL and adds it to the playlist.
    /// Supports HTTP, RTSP, and other streaming protocols via LibVLCSharp.
    /// </summary>
    /// <param name="url">The URL of the network stream to open.</param>
    private async void OpenNetworkStream(string url)
    {
        //string url = Interaction.InputBox("Enter the URL of the network stream (e.g., http://example.com/stream.m3u8 or rtsp://example.com/stream):", "Open Network Stream", "http://");

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                // Validate and create URI
                var uri = new Uri(url, UriKind.Absolute);
                var videoItem = new VideoItem(uri.ToString()); // Use string constructor to set Uri
                videoItem.Name = uri.Host; // Set a simple name based on host
                await Playlist.AddAsync(videoItem);
                // Automatically play the stream
                Play(videoItem);
                this.WriteLine($"Added and playing network stream: {url}");
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"Invalid URL format: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding stream: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Opens a file dialog to select subtitle files.
    /// </summary>
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
    /// Sets the subtitle file path and enables AI translation.
    /// </summary>
    /// <param name="path">The path to the subtitle file.</param>
    public void SetSubtitle(string path)
    {
        _videoView.Dispatcher.InvokeAsync(() =>
        {
            _subtitleControl.FilePath = path;
            _subtitleControl.EnableAiTranslation = true;
            _subtitleControl.TimeChanged += delegate (object sender, TimeSpan time)
            {
                if (_videoView.MediaPlayer != null)
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
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Pause();
                    this.Playlist.Current.IsPlaying = false;
                }
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VlcPlayerView]: {ex.Message}");
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
                _mediaPlayer.Stop();
                this.Playlist.Current.IsPlaying = false;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VlcPlayerView]: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays the next item in the playlist.
    /// </summary>
    public void Next()
    {
        Stop();
        Playlist.MoveNext.Play();
    }

    /// <summary>
    /// Plays the previous item in the playlist.
    /// </summary>
    public void Preview()
    {
        Stop();
        Playlist.MovePrevious.Play();
    }

    /// <summary>
    /// Seeks to a specific time position in the media.
    /// </summary>
    /// <param name="time">The time span to seek to.</param>
    public void Seek(TimeSpan time)
    {
        if (_mediaPlayer != null)
        {
            ProgressBar.ShowByStoryboard((Storyboard)ProgressBar.FindResource("fadeInProgressBar")).GetAwaiter();
            _mediaPlayer.Time = (long)time.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Seeks forward or backward by a specified time span.
    /// </summary>
    /// <param name="time">The time span to seek by.</param>
    /// <param name="direction">The seek direction (Forward or Backward).</param>
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
            _mediaPlayer.Time = (long)this.Position.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Internal method to play media, handling pause resume or new media load.
    /// Supports both local files and network streams via URI.
    /// </summary>
    /// <param name="media">The media item to play, or null to resume current.</param>
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

                    this.Playlist.Current.IsPlaying = true;
                    SetThreadExecutionState(BLOCK_SLEEP_MODE);
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VlcPlayerView]: Error while playing media: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays a specific media item.
    /// </summary>
    /// <param name="media">The media item to play.</param>
    public void Play(VideoItem media)
    {
        _Play(media);
    }

    /// <summary>
    /// Plays or resumes the current media item.
    /// </summary>
    public void Play()
    {
        if (Playlist.Current == null)
        {
            Console.WriteLine($"[VlcPlayerView]: Playlist is empty or current media is not set.");
            return;
        }
        _Play();
    }

    /// <summary>
    /// Configures real-time upscale options for the media.
    /// </summary>
    /// <param name="media">The media to configure.</param>
    /// <param name="targetWidth">The target width for upscale (default 1920).</param>
    /// <param name="targetHeight">The target height for upscale (default 1080).</param>
    private void ConfigureRealTimeUpscale(LibVLCSharp.Shared.Media media, int targetWidth = 1920, int targetHeight = 1080)
    {
        try
        {
            media.AddOption(":video-filter=scale");
            media.AddOption($":scale-actualWidth={targetWidth}");
            media.AddOption($":scale-height={targetHeight}");
            media.AddOption(":video-filter=hqdn3d");

            this.WriteLine($"Applied real-time upscale to {targetWidth}x{targetHeight} with hqdn3d");
        }
        catch (Exception ex)
        {
            this.WriteLine($"Failed to apply upscale: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the media resolution is low enough to warrant upscaling.
    /// </summary>
    /// <param name="media">The media item to check.</param>
    /// <returns>True if resolution is below 1280 width.</returns>
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

    /// <summary>
    /// Handles repeat mode behavior at end of playback.
    /// </summary>
    /// <param name="repeat">The repeat mode string ("One", "All", "Random").</param>
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

    /// <summary>
    /// Handles playing state to block system sleep.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPlaying(object sender, EventArgs e)
    {
        SetThreadExecutionState(BLOCK_SLEEP_MODE);
    }

    /// <summary>
    /// Handles stopped state to allow system sleep.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnStopped(object sender, EventArgs e)
    {
        SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
    }

    /// <summary>
    /// Handles paused state to allow system sleep.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPaused(object sender, EventArgs e)
    {
        SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
    }

    /// <summary>
    /// Updates buffering progress in the UI.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The buffering event arguments.</param>
    private void OnBuffering(object sender, MediaPlayerBufferingEventArgs e)
    {
        this.Dispatcher.InvokeAsync(() =>
        {
            var x = (100 * e.Cache) / _progressBar.Width;
            ProgressBar.BufforBarValue = x;
        });
    }

    /// <summary>
    /// Initializes UI elements when the control is loaded.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void VlcPlayerView_Loaded(object sender, RoutedEventArgs e)
    {
        ProgressBar.Duration = Playlist.Current?.Duration ?? TimeSpan.Zero;
        ProgressBar.Value = (long)0.0;
        ProgressBar.ProgressText = "00 : 00 : 00/00 : 00 : 00";
        ProgressBar.PopupText = "Volume: 100";
        ControlBar.MediaTitle = Playlist.Current?.Name ?? "No video loaded";
    }

    /// <summary>
    /// Raises the PropertyChanged event for a property with value comparison.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
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
    /// Raises the PropertyChanged event for a property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Disposes resources and saves configuration.
    /// </summary>
    public void Dispose()
    {
        _mediaPlayer?.Dispose();
        SavePlaylistConfig();
        _libVLC?.Dispose();
        SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
    }
}
