// Version: 0.1.11.42
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using LibVLCSharp.Shared;

using Thmd.Consolas;
using Thmd.Media;

namespace Thmd.Views
{
    /// <summary>
    /// Logika interakcji dla ControlBar.xaml
    /// </summary>
    public partial class ControlBar : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private IPlay _player;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Controls Access

        public Button BtnPlay => _playPauseButton;
        public Button BtnStop => _stopButton;
        public Button BtnNext => _fastForwardButton;
        public Button BtnPrevious => _rewindButton;
        public Button BtnMute => _muteButton;
        public Button BtnSubtitle => _openSubtitlesButton;
        public Button BtnOpen => _openMediaButton;
        public Button BtnPlaylist => _openPlaylistButton;
        public Button BtnStream => _streamButton;
        public Slider SliderVolume => _volumeSlider;

        #endregion

        #region Dependency Properties

        public string MediaTitle
        {
            get => (string)GetValue(MediaTitleProperty);
            set => SetValue(MediaTitleProperty, value);
        }
        public static readonly DependencyProperty MediaTitleProperty =
            DependencyProperty.Register(nameof(MediaTitle), typeof(string), typeof(ControlBar),
                new PropertyMetadata("No Media"));

        public string Position
        {
            get => (string)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register(nameof(Position), typeof(string), typeof(ControlBar),
                new PropertyMetadata("00:00:00"));

        public string Duration
        {
            get => (string)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(nameof(Duration), typeof(string), typeof(ControlBar),
                new PropertyMetadata("00:00:00"));

        public double Volume
        {
            get => (double)GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }
        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register(nameof(Volume), typeof(double), typeof(ControlBar),
                new PropertyMetadata(100.0, OnVolumeChanged));

        //private double _previousVolume = 100.0;
        //public double PreviousVolume { 
        //    get => _previousVolume;
        //    set {
        //        if (System.Math.Abs(_previousVolume - _volumeSlider.Value) < 0.001) return; // <-- kluczowe!

        //        _previousVolume = Math.Clamp(_volumeSlider.Value, 0, 100);
        //        this.WriteLine($"PreviousVolume set to {_previousVolume}");
        //        //_previousVolume = _volumeSlider.Value;
        //        if (_player!=null)
        //            _player.Volume = _previousVolume;
        //        OnPropertyChanged(nameof(PreviousVolume));
        //    }
        //}

        public bool IsMuted
        {
            get => (bool)GetValue(IsMutedProperty);
            set => SetValue(IsMutedProperty, value);
        }
        public static readonly DependencyProperty IsMutedProperty =
            DependencyProperty.Register(nameof(IsMuted), typeof(bool), typeof(ControlBar),
                new PropertyMetadata(false, OnIsMutedChanged));

        public string RepeatMode
        {
            get => (string)GetValue(RepeatModeProperty);
            set => SetValue(RepeatModeProperty, value);
        }
        public static readonly DependencyProperty RepeatModeProperty =
            DependencyProperty.Register(nameof(RepeatMode), typeof(string), typeof(ControlBar),
                new PropertyMetadata("None", OnRepeatModeChanged));

        public ObservableCollection<string> RepeatModes
        {
            get => (ObservableCollection<string>)GetValue(RepeatModesProperty);
            set => SetValue(RepeatModesProperty, value);
        }
        public static readonly DependencyProperty RepeatModesProperty =
            DependencyProperty.Register(nameof(RepeatModes), typeof(ObservableCollection<string>), typeof(ControlBar), new PropertyMetadata(new ObservableCollection<string> { "None", "All", "One", "Random" }));

        public bool RepeatPopupVisibility
        {
            get => (bool)GetValue(RepeatPopupVisibilityProperty);
            set => SetValue(RepeatPopupVisibilityProperty, value);
        }
        public static readonly DependencyProperty RepeatPopupVisibilityProperty =
            DependencyProperty.Register(nameof(RepeatPopupVisibility), typeof(bool), typeof(ControlBar),
                new PropertyMetadata(false));

        public double BassLevel
        {
            get => (double)GetValue(BassLevelProperty);
            set => SetValue(BassLevelProperty, value);
        }
        public static readonly DependencyProperty BassLevelProperty =
            DependencyProperty.Register(nameof(BassLevel), typeof(double), typeof(ControlBar),
                new PropertyMetadata(0.0));

        public double MidLevel
        {
            get => (double)GetValue(MidLevelProperty);
            set => SetValue(MidLevelProperty, value);
        }
        public static readonly DependencyProperty MidLevelProperty =
            DependencyProperty.Register(nameof(MidLevel), typeof(double), typeof(ControlBar),
                new PropertyMetadata(0.0));

        public double TrebleLevel
        {
            get => (double)GetValue(TrebleLevelProperty);
            set => SetValue(TrebleLevelProperty, value);
        }
        public static readonly DependencyProperty TrebleLevelProperty =
            DependencyProperty.Register(nameof(TrebleLevel), typeof(double), typeof(ControlBar),
                new PropertyMetadata(0.0));

        public bool EqualizerPopupVisibility
        {
            get => (bool)GetValue(EqualizerPopupVisibilityProperty);
            set => SetValue(EqualizerPopupVisibilityProperty, value);
        }
        public static readonly DependencyProperty EqualizerPopupVisibilityProperty =
            DependencyProperty.Register(nameof(EqualizerPopupVisibility), typeof(bool), typeof(ControlBar),
                new PropertyMetadata(false));

        #endregion

        #region Constructor

        public ControlBar()
        {
            InitializeComponent();
            InitializeEvents();
        }

        public ControlBar(IPlay player) : this()
        {
            SetPlayer(player);
        }

        #endregion

        #region Public API

        public void SetPlayer(IPlay player)
        {
            _player = player;
            SubscribeToPlayerEvents();
            SyncWithPlayer();
        }

        #endregion

        #region Initialization

        private void InitializeEvents()
        {
            // Przyciski
            _playPauseButton.Click += (s, e) => _player?.TogglePlayPause();
            _stopButton.Click += (s, e) => _player?.Stop();
            _fastForwardButton.Click += (s, e) => _player?.Next();
            _rewindButton.Click += (s, e) => _player?.Preview();
            _muteButton.Click += (s, e) => ToggleMute();
            _repeatButton.Click += (s, e) => RepeatPopupVisibility = !RepeatPopupVisibility;
            _equalizerButton.Click += (s, e) => EqualizerPopupVisibility = !EqualizerPopupVisibility;

            // ComboBox repeat
            _repeatComboBox.SelectionChanged += (s, e) =>
            {
                if (_repeatComboBox.SelectedItem is string mode)
                    RepeatMode = mode;
                RepeatPopupVisibility = false;
            };
        }

        private void SubscribeToPlayerEvents()
        {
            if (_player == null) return;

            _player.TimeChanged += OnPlayerTimeChanged;
            _player.Playing += (s, e) => Dispatcher.Invoke(() => UpdatePlayButtonIcon(true));
            _player.Paused += (s, e) => Dispatcher.Invoke(() => UpdatePlayButtonIcon(false));
            _player.Stopped += (s, e) => Dispatcher.Invoke(() => UpdatePlayButtonIcon(false));
            //_player.VolumeChanged += (s, e) => Dispatcher.Invoke(() => _player.Volume = e.Volume);
        }

        private void SyncWithPlayer()
        {
            if (_player == null) return;

            Volume = _player.Volume;
            IsMuted = _player.isMute;

            if (_player.Playlist.Current != null)
            {
                MediaTitle = _player.Playlist.Current.Name ?? "No Media";
                Duration = FormatTime((long)_player.Playlist.Current.Duration.TotalMilliseconds);
            }

            UpdateAllIcons();
        }

        #endregion

        #region Dependency Property Callbacks

        private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cb = (ControlBar)d;
            var newValue = (double)e.NewValue * 10;
            Console.WriteLine(cb._player.Volume + " " + newValue);
            
            if (cb._player != null && System.Math.Abs(cb._player.Volume - newValue) > 0.1)
            {
                var v = Math.Clamp(cb._player.Volume - newValue, 0, 100);
                cb._player.Volume = v;
            }
        }

        private static void OnIsMutedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cb = (ControlBar)d;
            //var newValue = (bool)e.NewValue;

            //if (cb._player != null && cb._player.isMute != newValue)
            //{
            //    cb._player.isMute = newValue;
            //}

            cb.UpdateMuteButtonIcon();
        }

        private static void OnRepeatModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cb = (ControlBar)d;
            cb.Dispatcher.Invoke(cb.UpdateRepeatButtonIcon);
        }

        #endregion

        #region UI Updates

        private void UpdateAllIcons()
        {
            UpdatePlayButtonIcon(_player?.isPlaying ?? false);
            UpdateMuteButtonIcon();
            UpdateRepeatButtonIcon();
        }

        private void UpdatePlayButtonIcon(bool isPlaying = false)
        {
            Dispatcher.Invoke(() =>
            {
                if (_player?.isStopped == true)
                {
                    _playPauseButton.Content = "▶";
                    _playPauseButton.ToolTip = "Play";
                }
                else if (isPlaying)
                {
                    _playPauseButton.Content = "⏸️";
                    _playPauseButton.ToolTip = "Pause";
                }
                else
                {
                    _playPauseButton.Content = "▶";
                    _playPauseButton.ToolTip = "Play";
                }
            });
        }

        private void UpdateMuteButtonIcon()
        {
            Dispatcher.Invoke(() =>
            {
                _muteButton.Content = IsMuted ? "🔇" : "🔊";
                _muteButton.ToolTip = IsMuted ? "Unmute" : "Mute";
            });
        }

        private void UpdateRepeatButtonIcon()
        {
            Dispatcher.Invoke(() =>
            {
                switch (RepeatMode)
                {
                    case "All":
                        _repeatButton.Content = "🔁";
                        _repeatButton.ToolTip = "Repeat All";
                        break;
                    case "One":
                        _repeatButton.Content = "🔂";
                        _repeatButton.ToolTip = "Repeat One";
                        break;
                    case "Random":
                        _repeatButton.Content = "🔀";
                        _repeatButton.ToolTip = "Shuffle";
                        break;
                    default:
                        _repeatButton.Content = "🔁";
                        _repeatButton.ToolTip = "No Repeat";
                        break;
                }
            });
        }

        private void OnPlayerTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Position = FormatTime(e.Time);

                if (_player?.Playlist.Current != null)
                {
                    Duration = FormatTime((long)_player.Playlist.Current.Duration.TotalMilliseconds);
                    MediaTitle = _player.Playlist.Current.Name ?? "No Media";
                }
            });
        }

        private string FormatTime(long milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}"
                : $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        #endregion

        #region PropertyChanged Helper

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
