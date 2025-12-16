// Version: 0.0.1.73
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

using Microsoft.Win32;

using Thmd.Consolas;
using Thmd.Converters;
using Thmd.Images;
using Thmd.Media;
using Thmd.Utilities;

namespace Thmd.Views
{
    public partial class VlcPlayer : VideoView, IPlay, IDisposable, INotifyPropertyChanged
    {
        #region WinAPI

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint BLOCK_SLEEP_MODE = 2147483651u;   // ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED
        private const uint DONT_BLOCK_SLEEP_MODE = 2147483648u; // ES_CONTINUOUS

        #endregion

        #region Fields

        private LibVLC _libVLC;
        private TimeSpan _position;
        private double _volume;
        private bool _playing;
        private bool _paused;
        private bool _stopped;
        private bool _mute;
        private bool _fullscreen;
        private Visibility _subtitleVisibility;

        private readonly Random _random = new Random();
        private readonly Engine _ffmpegEngine = new Engine();

        private bool _isThumbnailLoading;
        private CancellationTokenSource _thumbCts;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;
        public event EventHandler<MediaPlayerVolumeChangedEventArgs> VolumeChanged;
        public event EventHandler<EventArgs> Playing;
        public event EventHandler<EventArgs> Paused;
        public event EventHandler<EventArgs> Stopped;
        public event EventHandler<MediaPlayerSeekableChangedEventArgs> Seekable;

        #endregion

        #region Properties

        public Playlist Playlist => _playlist;
        public ControlBar Controlbar => _controlbar;
        public ProgressBarView ProgressBar => _progressbar;
        public SubtitleControl Subtitle => _subtitle;
        public InfoBox InfoBox => _infobox;

        public double Volume
        {
            get => _volume;
            set
            {
                if (System.Math.Abs(_volume - value) < 0.001) return; // <-- kluczowe!

                _volume = Math.Clamp(value, 0, 100);
                Dispatcher.Invoke(() =>
                {
                    _controlbar.SliderVolume.Value = _volume;
                    if (MediaPlayer != null)
                        MediaPlayer.Volume = (int)_volume;
                });
                OnPropertyChanged(nameof(Volume), ref _volume, value);
            }
        }

        public bool isFullscreen
        {
            get => _fullscreen;
            set
            {
                _fullscreen = value;
                Dispatcher.InvokeAsync(() => {
                    this.Fullscreen();
                });
                OnPropertyChanged(nameof(isFullscreen), ref _fullscreen, value);
            }
        }

        public bool isPlaying
        {
            get => _playing;
            set
            {
                _playing = value;
                if (value) Play();
                OnPropertyChanged(nameof(isPlaying), ref _playing, value);
            }
        }

        public bool isPaused
        {
            get => _paused;
            set
            {
                _paused = value;
                if (value) Pause();
                OnPropertyChanged(nameof(isPaused), ref _paused, value);
            }
        }

        public bool isStopped
        {
            get => _stopped;
            set
            {
                _stopped = value;
                if (value) Stop();
                OnPropertyChanged(nameof(isStopped), ref _stopped, value);
            }
        }

        public bool isMute
        {
            get => _mute;
            set
            {
                if (_mute == value) return;

                _mute = value;
                Dispatcher.Invoke(() => { 
                    if (MediaPlayer != null) 
                        MediaPlayer.Mute = _mute; 
                });
                OnPropertyChanged(nameof(isMute), ref _mute, value);
            }
        }

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

        public Visibility SubtitleVisibility
        {
            get => _subtitleVisibility;
            set
            {
                _subtitleVisibility = value;
                Dispatcher.Invoke(() => _subtitle.Visibility = value);
            }
        }

        #endregion

        #region Constructor

        public VlcPlayer()
        {
            InitializeComponent();

            _playlist.SetPlayer(this);
            _controlbar.SetPlayer(this);
            _progressbar.SetPlayer(this);

            _libVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVLC);

            ControlControllerHelper.Attach(_playlist);
            ControlControllerHelper.Attach(_controlbar);
            ControlControllerHelper.Attach(_keyboardShortcuts);

            _keyboardShortcuts.Visibility = Visibility.Hidden;

            InitializeEvents();
            InitializeProgressbarEvents();
            ControlBarButtonEvent();
            InitializeKeyboardShortcuts();
            InitializeFocus();
        }

        #endregion

        #region Event Initialization

        private void InitializeEvents()
        {
            MediaPlayer.Playing += (s, e) => Dispatcher.Invoke(() =>
            {
                _playing = true; _paused = _stopped = false;
                SetThreadExecutionState(BLOCK_SLEEP_MODE);
            });

            MediaPlayer.Paused += (s, e) => Dispatcher.Invoke(() =>
            {
                _playing = false; _paused = true; _stopped = false;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });

            MediaPlayer.Stopped += (s, e) => Dispatcher.Invoke(() =>
            {
                _playing = _paused = false; _stopped = true;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });

            MediaPlayer.EndReached += (s, e) => Dispatcher.InvokeAsync(HandleEndReached);

            MediaPlayer.TimeChanged += (s, e) => Dispatcher.InvokeAsync(() => UpdateUIOnTimeChanged(e.Time));

            MediaPlayer.VolumeChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                _volume = e.Volume;
                _controlbar.SliderVolume.Value = e.Volume;
            });
        }

        private void InitializeProgressbarEvents()
        {
            _progressbar.MouseMove += ProgressBarMouseMove;
        }

        private void InitializeKeyboardShortcuts()
        {
            // Obsługa klawiatury na całym kontrolu odtwarzacza
            this.PreviewKeyDown += VlcPlayer_PreviewKeyDown;
            this.Focusable = true;
            this.IsTabStop = true;
        }

        private void InitializeFocus()
        {
            this.PreviewKeyDown += VlcPlayer_PreviewKeyDown;

            this.Loaded += (s, e) => this.Focus();

            this.MouseLeftButtonDown += (s, e) => this.Focus();

            this.LostKeyboardFocus += (s, e) =>
            {
                var newFocus = e.NewFocus as DependencyObject;

                if (newFocus != null && this.IsAncestorOf(newFocus))
                {
                    Dispatcher.BeginInvoke(new Action(() => this.Focus()), DispatcherPriority.Background);
                }
            };
        }

        #endregion

        #region Keyboard Shortcuts

        private void VlcPlayer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is ComboBox)
                return;

            switch (e.Key)
            {
                case Key.Space:
                    TogglePlayPause();
                    e.Handled = true;
                    break;

                case Key.Right:
                    Seek(TimeSpan.FromSeconds(5), SeekDirection.Forward);
                    e.Handled = true;
                    break;

                case Key.Left:
                    Seek(TimeSpan.FromSeconds(5), SeekDirection.Backward);
                    e.Handled = true;
                    break;

                case Key.Up:
                    //Volume += 5;
                    MediaPlayer.Volume += 5;
                    e.Handled = true;
                    break;

                case Key.Down:
                    //Volume -= 5;
                    MediaPlayer.Volume -= 5;
                    e.Handled = true;
                    break;

                case Key.F:
                    isFullscreen = !isFullscreen;
                    e.Handled = true;
                    break;

                case Key.Escape:
                    if (isFullscreen)
                    {
                        isFullscreen = false;
                        e.Handled = true;
                    }
                    else
                    {
                        isFullscreen= true;
                        e.Handled = true;
                    }
                    break;

                case Key.P:
                    Playlist.Visibility = Playlist.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    e.Handled = true;
                    break;

                case Key.S:
                    SubtitleVisibility = SubtitleVisibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    e.Handled = true;
                    break;

                case Key.M:
                    ToggleMute();
                    e.Handled = true;
                    break;

                case Key.N:
                    Next();
                    e.Handled = true;
                    break;

                case Key.B:
                    Preview();
                    e.Handled = true;
                    break;

                case Key.H:
                    _keyboardShortcuts.Visibility = _keyboardShortcuts.Visibility == Visibility.Collapsed
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region IPlay Implementation

        private void _Play(VideoItem media = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (Playlist.Current == null)
                {
                    this.WriteLine("Playlist is empty or current media is not set.");
                    return;
                }

                try
                {
                    if (_paused && MediaPlayer.CanPause && _position > TimeSpan.Zero)
                    {
                        MediaPlayer.Play();
                    }
                    else if (media != null)
                    {
                        var vlcMedia = new LibVLCSharp.Shared.Media(_libVLC, media.Uri);
                        MediaPlayer.Play(vlcMedia);
                    }
                    else
                    {
                        MediaPlayer.Play();
                    }

                    SetThreadExecutionState(BLOCK_SLEEP_MODE);
                }
                catch (Exception ex)
                {
                    this.WriteLine($"Error while playing media: {ex.Message}");
                }
            });
        }

        public void Play(VideoItem media) => _Play(media);

        public void Play()
        {
            if (Playlist.Current == null)
            {
                this.WriteLine("Playlist is empty or current media is not set.");
                MessageBox.Show("Playlist is empty or current media is not set.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _Play();
        }

        public void PlayNext(VideoItem media)
        {
            if (Playlist != null)
                Playlist.PlayNext = Playlist.Videos.IndexOf(media);
        }

        public void Pause()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (MediaPlayer.IsPlaying)
                    MediaPlayer.Pause();

                _paused = true;
                _playing = _stopped = false;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });
        }

        public void Stop()
        {
            Dispatcher.InvokeAsync(() =>
            {
                MediaPlayer.Stop();
                _stopped = true;
                _playing = _paused = false;
                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            });
        }

        public void Next()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (Playlist?.Next != null)
                {
                    Playlist.CurrentIndex = Playlist.Videos.IndexOf(Playlist.Next);
                    Play(Playlist.Current);
                }
            });
        }

        public void Preview()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (Playlist?.Previous != null)
                {
                    Playlist.CurrentIndex = Playlist.Videos.IndexOf(Playlist.Previous);
                    Play(Playlist.Current);
                }
            });
        }

        public void Seek(TimeSpan time)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (MediaPlayer != null)
                {
                    MediaPlayer.Time = (long)time.TotalMilliseconds;
                    _position = time;
                }
            });
        }

        public void Seek(TimeSpan time, SeekDirection direction)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (direction == SeekDirection.Forward)
                    Position += time;
                else
                    Position -= time;

                MediaPlayer.Time = (long)Position.TotalMilliseconds;
            });
        }

        #endregion

        #region Overides events
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
                Volume += 5;
            else
                Volume -= 5;
            e.Handled = true;
        }
        //protected override void OnMouseWheel(MouseWheelEventArgs e)
        //{
        //    base.OnMouseWheel(e);
        //    // Adjust volume with mouse wheel
        //    double volumeChange = e.Delta > 0 ? 5.0 : -5.0;
        //    Volume += volumeChange;
        //    _controlBar.SliderVolume.Value = Volume;
        //    _progressBar.PopupText = $"Volume: {(int)Volume}";
        //    _progressBar._popup.IsOpen = true;

        //    // Show popup briefly
        //    Task.Delay(1000).ContinueWith(_ => Dispatcher.InvokeAsync(() => _progressBar._popup.IsOpen = false));
        //}
        #endregion

        #region UI Event Handlers

        private void ControlBarButtonEvent()
        {
            Controlbar.BtnPlay.Click += (s, e) => TogglePlayPause();
            Controlbar.BtnStop.Click += (s, e) => Stop();
            Controlbar.BtnNext.Click += (s, e) => Next();
            Controlbar.BtnPrevious.Click += (s, e) => Preview();
            Controlbar.BtnMute.Click += (s, e) => ToggleMute();
            Controlbar.BtnOpen.Click += (s, e) => OpenMediaFile();
            Controlbar.BtnPlaylist.Click += (s, e) =>
                Playlist.Visibility = Playlist.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            Controlbar.BtnSubtitle.Click += (s, e) =>
            {
                OpenSubtitleFile();
                SubtitleVisibility = Visibility.Visible;
            };
        }

        private void ProgressBarMouseMove(object sender, MouseEventArgs e)
        {
            //if (DesignerProperties.GetIsInDesignMode(this)) return;
            if (Playlist.Current == null) return;

            Dispatcher.Invoke(() =>
            {
                var pos = e.GetPosition(_progressbar);
                double width = _progressbar.ActualWidth;
                if (width <= 0) return;

                var percentage = pos.X / width;
                var time = TimeSpan.FromMilliseconds((percentage * Playlist.Current.Duration.TotalMilliseconds));
                _progressbar.PopupText = $"{time:hh\\:mm\\:ss}";
                _progressbar._popup.IsOpen = true;
                _progressbar._popup.HorizontalOffset = pos.X - (_progressbar._popupText.ActualWidth / 2);
                _progressbar._rectangleMouseOverPoint.Margin = new Thickness(pos.X - (_progressbar._rectangleMouseOverPoint.Width / 2), 0, 0, 0);

                _ = UpdateThumbnailAsync(time);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Position = time;
                    this.WriteLine("Seeking to: " + time.ToString());
                }
            });
        }

        #endregion

        #region Media Control Helpers

        public void TogglePlayPause()
        {
            if (isPlaying) Pause();
            else Play();
        }

        private void ToggleMute() => isMute = !isMute;

        private void HandleRepeat(string repeat)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (repeat)
                {
                    case "One":
                        Play(Playlist.Current);
                        break;
                    case "All":
                        Next();
                        break;
                    case "Random":
                        if (Playlist.Items.Count > 0)
                        {
                            int randomIndex = _random.Next(Playlist.Items.Count);
                            Playlist.CurrentIndex = randomIndex;
                            Play(Playlist.Current);
                        }
                        break;
                }
            });
        }

        private void HandleEndReached()
        {
            Stop();

            if (Playlist.PlayNext.HasValue)
            {
                var index = Playlist.PlayNext.Value;
                Play(Playlist.Videos[index]);
                Playlist.PlayNext = null;
            }

            HandleRepeat(Controlbar.RepeatMode);
        }

        #endregion

        #region File Operations

        private async void OpenMediaFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Pliki wideo|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.wmv;*.ts;*.m3u8;*.hlsarc|Wszystkie pliki|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string path in openFileDialog.FileNames)
                {
                    await Playlist.AddAsync(new VideoItem(path));
                }

                if (Playlist.CurrentIndex == -1 && Playlist.Items.Count > 0)
                    Playlist.CurrentIndex = 0;
            }
        }

        private void OpenSubtitleFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Pliki napisów|*.srt;*.sub;*.txt|Wszystkie pliki|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SetSubtitle(openFileDialog.FileName);
                SubtitleVisibility = Visibility.Visible;
            }
        }

        public void SetSubtitle(string path)
        {
            Dispatcher.InvokeAsync(() =>
            {
                _subtitle.FilePath = path;
                _subtitle.EnableAiTranslation = true;
                _subtitle.TimeChanged += (sender, time) => _subtitle.PositionTime = time;
            });
        }

        #endregion

        #region Thumbnail Generation

        public async Task<Bitmap> GetThumbnailAsync(TimeSpan time)
        {
            _thumbCts?.Cancel();
            _thumbCts = new CancellationTokenSource();
            var token = _thumbCts.Token;

            var tempPath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.png");

            try
            {
                var inputFile = new MediaFile { Filename = Playlist.Current.Uri.LocalPath };
                var outputFile = new MediaFile { Filename = tempPath };

                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var options = new ConversionOptions
                    {
                        Seek = time,
                        CustomWidth = 320,
                        CustomHeight = 180
                    };

                    _ffmpegEngine.GetMetadata(inputFile);
                    _ffmpegEngine.GetThumbnail(inputFile, outputFile, options);
                }, token).ConfigureAwait(false);

                using var bmp = new Bitmap(outputFile.Filename);
                return new Bitmap(bmp);
            }
            catch
            {
                return null;
            }
            finally
            {
                try { File.Delete(tempPath); }
                catch { }
            }
        }

        private async Task UpdateThumbnailAsync(TimeSpan time)
        {
            if (Playlist?.Current == null || _isThumbnailLoading) return;

            _isThumbnailLoading = true;

            await Task.Run(async () =>
            {
                var bmp = await GetThumbnailAsync(time);
                if (bmp != null)
                {
                    await Dispatcher.BeginInvoke(() =>
                    {
                        _progressbar._popupImage.Source = BitmapHelper.BitmapToImageSource(bmp);
                    });
                }
            });

            _isThumbnailLoading = false;
        }

        #endregion

        #region UI Updates

        private void UpdateUIOnTimeChanged(long timeMs)
        {
            if (Playlist.Current == null) return;

            _position = TimeSpan.FromMilliseconds(timeMs);

            Dispatcher.BeginInvoke(() => _playlist.Current.Position = timeMs);

            _progressbar.Value = (_progressbar.Maximum * timeMs)/Playlist.Current.Duration.TotalMilliseconds;
            
            _progressbar.ProgressText = $"{_position:hh\\:mm\\:ss} / {Playlist.Current.Duration:hh\\:mm\\:ss}";

            _controlbar.MediaTitle = Playlist.Current.Name;
            _controlbar.Position = _position.ToString("hh\\:mm\\:ss");
            _controlbar.Duration = Playlist.Current.Duration.ToString("hh\\:mm\\:ss");

            _subtitle.PositionTime = _position;
        }

        #endregion

        #region PropertyChanged Helpers

        protected void OnPropertyChanged<T>(string propertyName, ref T field, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ControlControllerHelper.Detach(_playlist);
                ControlControllerHelper.Detach(_controlbar);
                ControlControllerHelper.Detach(_keyboardShortcuts);

                MediaPlayer?.Stop();
                MediaPlayer?.Dispose();
                _libVLC?.Dispose();

                SetThreadExecutionState(DONT_BLOCK_SLEEP_MODE);
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
