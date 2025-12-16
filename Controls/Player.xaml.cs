// Version: 0.0.0.28
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Controls; // dla Also

using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

using Thmd.Consolas;
using Thmd.Media;
using Thmd.Utilities;

namespace Thmd.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy Player.xaml
    /// </summary>
    public partial class Player : UserControl, IPlayer, INotifyPropertyChanged
    {
        #region Fields
        private LibVLCSharp.Shared.LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isStoped = true;
        private Hlsarc.Core.HlsarcReader _hlsarc;

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
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<EventArgs> Playing;
        public event EventHandler<EventArgs> Stopped;
        public event EventHandler<EventArgs> LengthChanged;
        public event EventHandler<MediaPlayerTimeChangedEventArgs> TimeChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Pobiera lub ustawia widok listy odtwarzania
        /// </summary>
        public Playlist Playlist
        {
            get { return _playlist; }
            set { _playlist = value; }
        }
        /// <summary>
        /// Pobiera lub ustawia pasek sterowania odtwarzaczem
        /// </summary>
        public ControlBar ControlBar
        {
            get { return _controlBar; }
            set { _controlBar = value; }
        }
        /// <summary>
        /// Pobiera lub ustawia pasek postępu odtwarzania
        /// </summary>
        public ProgressBarView ProgressBar
        {
            get { return _progressBar; }
            set { _progressBar = value; }
        }
        /// <summary>
        /// Pobiera lub ustawia kontrolkę napisów
        /// </summary>
        public SubtitleControl Subtitle
        {
            get { return _subtitle; }
            set { _subtitle = value; }
        }

        public InfoBox InfoBox => throw new NotImplementedException();

        public EditControl EditControl => throw new NotImplementedException();

        public Visibility PlaylistVisibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public TimeSpan Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public VLCState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        /// <summary>
        /// Pobiera lub ustawia wartość wskazującą, czy odtwarzacz aktualnie odtwarza media
        /// </summary>
        public bool isPlaying 
        { 
            get { return _isPlaying; } 
            set { _isPlaying = value; } 
        }
        /// <summary>
        /// Pobiera lub ustawia wartość wskazującą, czy odtwarzacz jest wstrzymany
        /// </summary>
        public bool isPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }
        /// <summary>
        /// Pobiera lub ustawia wartość wskazującą, czy odtwarzacz jest zatrzymany
        /// </summary>
        public bool isStopped
        {
            get { return _isStoped; }
            set { _isStoped = value; }
        }
        public Visibility SubtitleVisibility { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool isMute { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Fullscreen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        public Player()
        {
            InitializeComponent();
            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _mediaPlayer.Hwnd = IntPtr.Zero;
            _player.MediaPlayer = _mediaPlayer;

            _playlist.SetPlayer(this);
            _controlBar.SetPlayer(this);
            _progressBar.SetPlayer(this);

            //ControlControllerHelper.Attach(_playlist);
            ControlControllerHelper.Attach(_controlBar);
        }
        #endregion

        #region OnPropertyChanged Methods
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
        #endregion

        #region Media Control Methods
        /// <summary>
        /// Internal method to play media, handling pause resume or new media load.
        /// Supports both local files and network streams via URI.
        /// </summary>
        /// <param name="media">The media item to play, or null to resume current.</param>
        private void _Play(VideoItem media = null)
        {
            if (Playlist.Current == null)
            {
                this.WriteLine($"Playlist is empty or current media is not set.");
                return;
            }

            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    isPlaying = true;
                    isPaused = false;
                    isStopped = false;

                    Dispatcher.Invoke(() =>
                    {
                        var url = String.Empty;
                        if (isPaused && _player.MediaPlayer.CanPause && Position > TimeSpan.Zero)
                        {
                            this.WriteLine($"Resuming paused media: {Playlist.Current.Name}");
                            _player.MediaPlayer.Play();
                        }
                        else if (media != null)
                        {
                            if (media.Extension == ".hlsarc")
                            {
                                _hlsarc = new Hlsarc.Core.HlsarcReader(media.Uri.AbsolutePath);
                                _hlsarc.StartServer(8080, msg => Console.WriteLine(msg));
                                _hlsarc.Open();
                                url = "http://localhost:8080";
                            }

                            LibVLCSharp.Shared.Media vlcMedia = null;
                            if (url != String.Empty)
                                vlcMedia = new LibVLCSharp.Shared.Media(_libVLC, url);
                            else
                                vlcMedia = new LibVLCSharp.Shared.Media(_libVLC, media.Uri);

                            /*if (IsLowResolution(media))
                            {
                                ConfigureRealTimeUpscale(vlcMedia);
                            }*/
                            this.WriteLine($"Playing media: {media.Name} from {media.Uri}");
                            _player.MediaPlayer.Play(vlcMedia);
                        }
                        else
                        {
                            this.WriteLine($"Resuming media: {Playlist.Current.Name}");
                            _player.MediaPlayer.Play();
                        }

                        this.Playlist.Current.IsPlaying = true;
                        SetThreadExecutionState(BLOCK_SLEEP_MODE);
                    });
                });
            }
            catch (Exception ex)
            {
                this.WriteLine($"Error while playing media: {ex.Message}");
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
                this.WriteLine($"Playlist is empty or current media is not set.");
                return;
            }
            _Play();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Next()
        {
            throw new NotImplementedException();
        }

        public void Preview()
        {
            throw new NotImplementedException();
        }

        public void Seek(TimeSpan time)
        {
            throw new NotImplementedException();
        }

        public void SetSubtitle(string path)
        {
            throw new NotImplementedException();
        }

        public void SavePlaylistConfig()
        {
            throw new NotImplementedException();
        }

        public void LoadPlaylistConfig_Question()
        {
            throw new NotImplementedException();
        }

        public BitmapSource GetCurrentFrame()
        {
            throw new NotImplementedException();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            // Dostosuj rozmiar elementu VideoView do rozmiaru kontrolki Player
            _player.Width = sizeInfo.NewSize.Width;
            _player.Height = sizeInfo.NewSize.Height;
        }

        /// <summary>
        /// Odwołanie zasobów używanych przez odtwarzacz
        /// </summary>
        public void Dispose()
        {
            ControlControllerHelper.Detach(_controlBar);
            //ControlControllerHelper.Detach(_playlist);

            _player.MediaPlayer.Dispose();
            _player.Dispose();
            _libVLC.Dispose();
            _libVLC = null;
            _controlBar = null;
            _progressBar = null;
            _playlist = null;
        }
        #endregion
    }
}
