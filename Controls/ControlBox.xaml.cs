// Version: 0.1.7.60
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using Thmd.Configuration;
using Thmd.Logs;
using Thmd.Media;
using Thmd.Repeats;
using Thmd.Utilities;
using System.Threading.Tasks;

namespace Thmd.Controls
{
    public partial class ControlBox : UserControl, INotifyPropertyChanged
    {
        private IPlayer _player;
        private string _videoName;
        private string _videoTime;
        private RepeatType _repeatType;
        private bool _enableShuffle;
        private int _scrollTextIndex = 0;
        private ObservableCollection<VideoItem> _videos; // From MediaPlayerControl

        public event PropertyChangedEventHandler PropertyChanged;

        public string VideoName
        {
            get => _videoName;
            set
            {
                _videoName = value;
                OnPropertyChanged(nameof(VideoName));
                //StartTextAnimation();
            }
        }
        
        public string VideoPreviewName
        {
            get => _videoPreviewName.Text;
            set
            {
                _videoPreviewName.Text = value;
                OnPropertyChanged(nameof(VideoPreviewName));
            }
        }

        public string VideoNextName
        {
            get => _videoNextName.Text;
            set
            {
                _videoNextName.Text = value;
                OnPropertyChanged(nameof(VideoNextName));
            }
        }

        public string VideoTime
        {
            get => _videoTime;
            set
            {
                _videoTime = value;
                OnPropertyChanged(nameof(VideoTime));
            }
        }

        public RepeatType RepeatType
        {
            get => _repeatType;
            set
            {
                _repeatType = value;
                OnPropertyChanged(nameof(RepeatType));
                RepeatControl.RepeatType = value;
            }
        }

        public bool EnableShuffle
        {
            get => _enableShuffle;
            set
            {
                _enableShuffle = value;
                OnPropertyChanged(nameof(EnableShuffle));
                RepeatControl.EnableShuffle = value;
            }
        }

        public ObservableCollection<VideoItem> Videos
        {
            get => _videos;
            set
            {
                _videos = value;
                OnPropertyChanged(nameof(Videos));
                UpdateVideoInfo();
            }
        }

        public Button BtnPlay => _playerBtnControl._btnPlay;
        public Button BtnStop => _playerBtnControl._btnStop;
        public Button BtnNext => _playerBtnControl._btnNext;
        public Button BtnPrevious => _playerBtnControl._btnPrevious;
        public Button BtnVolumeUp => _playerBtnVolume._btnVolumeUp;
        public Button BtnVolumeDown => _playerBtnVolume._btnVolumeDown;
        public Button BtnMute => _playerBtnVolume._btnMute;
        public Button BtnSettingsWindow => _playerBtnSecondRow._btnSettings;
        public Button BtnSubtitle => _playerBtnSecondRow._btnSubtitle;
        public Button BtnUpdate => _playerBtnSecondRow._btnUpdate;
        public Button BtnOpen => _playerBtnControl._btnOpen;
        public Button BtnPlaylist => _playerBtnSecondRow._btnPlaylist;
        public Button BtnFullscreen => _playerBtnSecondRow._btnFullscreen;
        public Button BtnClose => _btnClose._btnClose;

        public ControlBox()
        {
            InitializeComponent();
            DataContext = this;
            VideoName = "No Video Loaded";
            VideoPreviewName = "Previous: None";
            VideoNextName = "Next: None";
            VideoTime = "00:00:00/00:00:00";
            RepeatType = RepeatType.None;
            EnableShuffle = false;
            Videos = new ObservableCollection<VideoItem>();

            //StartTextAnimation();
        }

        public ControlBox(IPlayer player) : this()
        {
            SetPlayer(player);
        }

        public void SetPlayer(IPlayer player)
        {
            _player = player;
            if (_player != null)
            {
                try
                {
                    var repeat_type = Enum.Parse(typeof(RepeatType), Config.Instance.PlaylistConfig.RepeatType.ToString());
                    var shuffle = bool.Parse(Config.Instance.PlaylistConfig.EnableShuffle.ToString());
                    RepeatControl.RepeatType = (RepeatType)repeat_type;
                    RepeatControl.EnableShuffle = shuffle;
                    UpdateVideoInfo();
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to initialize player settings: {ex.Message}");
                }
            }
        }

        private void StartTextAnimation()
        {
            // Ustawienie pocz�tkowego tekstu jako pustego
            _videoNameTextBlock.Text = string.Empty;

            // Tworzenie animacji opartej na timerze
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // Szybko�� pojawiania si� znak�w (100 ms na znak)
            };

            timer.Tick += (s, e) =>
            {
                if (_scrollTextIndex < VideoName.Length)
                {
                    // Dodawanie kolejnego znaku
                    _videoNameTextBlock.Text = VideoName.Substring(0, _scrollTextIndex + 1);
                    _scrollTextIndex++;
                }
                else
                {
                    // Po wy�wietleniu ca�ego tekstu resetujemy i powtarzamy
                    Task.Delay(3000).Wait();
                    _scrollTextIndex = 0;
                    _videoNameTextBlock.Text = string.Empty;
                }
            };

            // Uruchomienie animacji po za�adowaniu okna
            Loaded += (s, e) => timer.Start();
        }

        private void UpdateVideoInfo()
        {
            if (_player != null && Videos != null && Videos.Count > 0)
            {
                try
                {
                    int currentIndex = _player.Playlist.CurrentIndex;
                    VideoName = _player.Playlist.Current.Name ?? "Unknown Video";
                    VideoPreviewName = currentIndex > 0 ? $"Previous: {Videos[currentIndex - 1].Name}" : "Previous: None";
                    VideoNextName = currentIndex < Videos.Count - 1 ? $"Next: {Videos[currentIndex + 1].Name}" : "Next: None";
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to update video info: {ex.Message}");
                }
            }
        }

        private void OnNextVideo()
        {
            try
            {
                _player?.Next();
                UpdateVideoInfo();
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to load next video: {ex.Message}");
            }
        }

        private void OnPreviousVideo()
        {
            try
            {
                _player?.Preview();
                UpdateVideoInfo();
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to load previous video: {ex.Message}");
            }
        }

        private void AdjustVolume(double delta)
        {
            try
            {
                if (_player != null)
                {
                    double newVolume = MathHelper.Clamp(_player.Volume + delta, 0.0, 1.0);
                    _player.Volume = newVolume;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to adjust volume: {ex.Message}");
            }
        }

        private void OnOpenSettings()
        {
            try
            {
                // Implement settings window logic (e.g., open a new window)
                Logger.Log.Log(LogLevel.Info, new[] { "Console" }, "Settings window opened");
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to open settings: {ex.Message}");
            }
        }

        private void OnUpdate()
        {
            try
            {
                // Implement update logic (e.g., check for software updates)
                Logger.Log.Log(LogLevel.Info, new[] { "Console" }, "Update check initiated");
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to check updates: {ex.Message}");
            }
        }

        private void OnOpenFile()
        {
            try
            {
                // Implement file open dialog (e.g., using OpenFileDialog)
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Media Files (*.mp4,*.avi,*.mkv)|*.mp4;*.avi;*.mkv|All Files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    var media = new VideoItem(dialog.FileName);
                    _player?.Play(media);
                    Videos.Add(media);
                    UpdateVideoInfo();
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to open file: {ex.Message}");
            }
        }

        private void OnTogglePlaylist()
        {
            try
            {
                // Implement playlist toggle logic (e.g., show/hide playlist UI)
                Logger.Log.Log(LogLevel.Info, new[] { "Console" }, "Playlist toggled");
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to toggle playlist: {ex.Message}");
            }
        }

        private void OnClose()
        {
            try
            {
                _player?.Stop();
                Application.Current.MainWindow?.Close();
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to close: {ex.Message}");
            }
        }

        private void RepeatControl_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    _repeatType = _repeatType switch
                    {
                        RepeatType.None => RepeatType.One,
                        RepeatType.One => RepeatType.All,
                        RepeatType.All => RepeatType.None,
                        _ => RepeatType.None
                    };
                    RepeatType = _repeatType;
                    //_player?.ToggleRepeat(RepeatType);
                    Config.Instance.PlaylistConfig.RepeatType = RepeatType;
                }
                catch (Exception ex)
                {
                    Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, $"Failed to toggle repeat: {ex.Message}");
                }
            }
        }

        private void RepeatControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Hand;
            var fadeIn = (Storyboard)FindResource("fadeInControlBar");
            fadeIn.Begin();
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            var fadeIn = (Storyboard)FindResource("fadeInControlBar");
            fadeIn.Begin();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            var fadeOut = (Storyboard)FindResource("fadeOutControlBar");
            fadeOut.Begin();

            base.OnMouseLeave(e);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
