// Version: 0.1.10.53
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Thmd.Utilities;
using Thmd.Media;
using LibVLCSharp.Shared;
using System.Collections.Generic;
using Thmd.Translator;

namespace Thmd.Controls;

/// <summary>
/// Logika interakcji dla klasy ControlBox.xaml
/// </summary>
public partial class ControlBar : UserControl
{
    private IPlayer _player;
    private ObservableCollection<ILanguage> _languages;
    /// <summary>
    /// Gets the play button.
    /// </summary>
    public Button BtnPlay => _playPauseButton;
    /// <summary>
    /// Gets the stop button.
    /// </summary>
    public Button BtnStop => _stopButton;
    /// <summary>
    /// Gets the next button.
    /// </summary>
    public Button BtnNext => _fastForwardButton;
    /// <summary>
    /// Gets the previous button.
    /// </summary>
    public Button BtnPrevious => _rewindButton;
    /// <summary>
    /// Gets the mute button.
    /// </summary>
    public Button BtnMute => _muteButton;
    /// <summary>
    /// Gets the subtitle button.
    /// </summary>
    public Button BtnSubtitle => _openSubtitlesButton;
    /// <summary>
    /// Gets the open media button.
    /// </summary>
    public Button BtnOpen => _openMediaButton;
    /// <summary>
    /// Gets the playlist button.
    /// </summary>
    public Button BtnPlaylist => _openPlaylistButton;
    /// <summary>
    /// Gets the volume slider.
    /// </summary>
    public Slider SliderVolume => _volumeSlider;
    /// <summary>
    /// Gets the stream button.
    /// </summary>
    public Button BtnStream => _streamButton;
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    // Existing properties

    // Property for media title
    /// <summary>
    /// Gets or sets the title of the currently playing media.
    /// </summary>
    public string MediaTitle
    {
        get => (string)GetValue(MediaTitleProperty);
        set => SetValue(MediaTitleProperty, value);
    }
    /// <summary>
    /// Gets or sets the title of the currently playing media.
    /// </summary>
    public static readonly DependencyProperty MediaTitleProperty =
        DependencyProperty.Register(nameof(MediaTitle), typeof(string), typeof(ControlBar), new PropertyMetadata("No Media", OnMediaTitleChanged));

    // New properties for timer and duration
    /// <summary>
    /// Gets or sets the current playback time of the media.
    /// </summary>
    public string CurrentTime
    {
        get => (string)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }
    /// <summary>
    /// Gets or sets the current playback time of the media.
    /// </summary>
    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(nameof(Position), typeof(string), typeof(ControlBar), new PropertyMetadata("00:00", OnCurrentTimeChanged));
    /// <summary>
    /// Gets or sets the total duration of the media.
    /// </summary>
    public string MediaDuration
    {
        get => (string)GetValue(MediaDurationProperty);
        set => SetValue(MediaDurationProperty, value);
    }
    /// <summary>
    /// Gets or sets the total duration of the media.
    /// </summary>
    public static readonly DependencyProperty MediaDurationProperty =
        DependencyProperty.Register(nameof(MediaDuration), typeof(string), typeof(ControlBar), new PropertyMetadata("00:00", OnMediaDurationChanged));

    // Existing properties
    /// <summary>
    /// Gets or sets the volume level (0 to 100).
    /// </summary>
    public double Volume
    {
        get => (double)GetValue(VolumeProperty);
        set => SetValue(VolumeProperty, value);
    }
    /// <summary>
    /// Gets or sets the volume level (0 to 100).
    /// </summary>
    public static readonly DependencyProperty VolumeProperty =
        DependencyProperty.Register(nameof(Volume), typeof(double), typeof(ControlBar), new PropertyMetadata(100.0, OnVolumeChanged));
    /// <summary>
    /// Gets or sets the visibility of the volume popup.
    /// </summary>
    public bool VolumePopupVisibility
    {
        get => (bool)GetValue(VolumePopupVisibilityProperty);
        set => SetValue(VolumePopupVisibilityProperty, value);
    }
    /// <summary>
    /// Gets or sets the text displayed in the volume popup.
    /// </summary>
    public static readonly DependencyProperty VolumePopupVisibilityProperty =
        DependencyProperty.Register(nameof(VolumePopupVisibility), typeof(bool), typeof(ControlBar), new PropertyMetadata(false));
    /// <summary>
    /// Gets or sets the text displayed in the volume popup.
    /// </summary>
    public string VolumePopupText
    {
        get => (string)GetValue(VolumePopupTextProperty);
        set => SetValue(VolumePopupTextProperty, value);
    }
    /// <summary>
    /// Gets or sets the text displayed in the volume popup.
    /// </summary>
    public static readonly DependencyProperty VolumePopupTextProperty =
        DependencyProperty.Register(nameof(VolumePopupText), typeof(string), typeof(ControlBar), new PropertyMetadata("Volume: 100"));
    /// <summary>
    /// Gets or sets the playlist items.
    /// </summary>
    public ObservableCollection<string> PlaylistItems
    {
        get => (ObservableCollection<string>)GetValue(PlaylistItemsProperty);
        set => SetValue(PlaylistItemsProperty, value);
    }
    /// <summary>
    /// Gets or sets the playlist items.
    /// </summary>
    public static readonly DependencyProperty PlaylistItemsProperty =
        DependencyProperty.Register(nameof(PlaylistItems), typeof(ObservableCollection<string>), typeof(ControlBar), new PropertyMetadata(new ObservableCollection<string>()));
    /// <summary>
    /// Gets or sets the selected index in the playlist.
    /// </summary>
    public int SelectedPlaylistIndex
    {
        get => (int)GetValue(SelectedPlaylistIndexProperty);
        set => SetValue(SelectedPlaylistIndexProperty, value);
    }
    /// <summary>
    /// Gets or sets the selected index in the playlist.
    /// </summary>
    public static readonly DependencyProperty SelectedPlaylistIndexProperty =
        DependencyProperty.Register(nameof(SelectedPlaylistIndex), typeof(int), typeof(ControlBar), new PropertyMetadata(-1));

    public bool PlaylistPopupVisibility
    {
        get => (bool)GetValue(PlaylistPopupVisibilityProperty);
        set => SetValue(PlaylistPopupVisibilityProperty, value);
    }
    public static readonly DependencyProperty PlaylistPopupVisibilityProperty =
        DependencyProperty.Register(nameof(PlaylistPopupVisibility), typeof(bool), typeof(ControlBar), new PropertyMetadata(false));

    public double BassLevel
    {
        get => (double)GetValue(BassLevelProperty);
        set => SetValue(BassLevelProperty, value);
    }
    public static readonly DependencyProperty BassLevelProperty =
        DependencyProperty.Register(nameof(BassLevel), typeof(double), typeof(ControlBar), new PropertyMetadata(0.0));

    public double MidLevel
    {
        get => (double)GetValue(MidLevelProperty);
        set => SetValue(MidLevelProperty, value);
    }
    public static readonly DependencyProperty MidLevelProperty =
        DependencyProperty.Register(nameof(MidLevel), typeof(double), typeof(ControlBar), new PropertyMetadata(0.0));

    public double TrebleLevel
    {
        get => (double)GetValue(TrebleLevelProperty);
        set => SetValue(TrebleLevelProperty, value);
    }
    public static readonly DependencyProperty TrebleLevelProperty =
        DependencyProperty.Register(nameof(TrebleLevel), typeof(double), typeof(ControlBar), new PropertyMetadata(0.0));

    public bool EqualizerPopupVisibility
    {
        get => (bool)GetValue(EqualizerPopupVisibilityProperty);
        set => SetValue(EqualizerPopupVisibilityProperty, value);
    }
    public static readonly DependencyProperty EqualizerPopupVisibilityProperty =
        DependencyProperty.Register(nameof(EqualizerPopupVisibility), typeof(bool), typeof(ControlBar), new PropertyMetadata(false));

    public ObservableCollection<string> RepeatModes
    {
        get => (ObservableCollection<string>)GetValue(RepeatModesProperty);
        set => SetValue(RepeatModesProperty, value);
    }
    public static readonly DependencyProperty RepeatModesProperty =
        DependencyProperty.Register(nameof(RepeatModes), typeof(ObservableCollection<string>), typeof(ControlBar), new PropertyMetadata(new ObservableCollection<string> { "None", "All", "One", "Random" }));

    public string RepeatMode
    {
        get => (string)GetValue(RepeatModeProperty);
        set => SetValue(RepeatModeProperty, value);
    }
    public static readonly DependencyProperty RepeatModeProperty =
        DependencyProperty.Register(nameof(RepeatMode), typeof(string), typeof(ControlBar), new PropertyMetadata("None", OnRepeatModeChanged));

    public bool RepeatPopupVisibility
    {
        get => (bool)GetValue(RepeatPopupVisibilityProperty);
        set => SetValue(RepeatPopupVisibilityProperty, value);
    }
    public static readonly DependencyProperty RepeatPopupVisibilityProperty =
        DependencyProperty.Register(nameof(RepeatPopupVisibility), typeof(bool), typeof(ControlBar), new PropertyMetadata(false));

    public bool IsMuted
    {
        get => (bool)GetValue(IsMutedProperty);
        set => SetValue(IsMutedProperty, value);
    }
    public static readonly DependencyProperty IsMutedProperty =
        DependencyProperty.Register(nameof(IsMuted), typeof(bool), typeof(ControlBar), new PropertyMetadata(false, OnIsMutedChanged));

    public bool IsPlaying
    {
        get => _player.isPlaying;
        set
        {
            UpdatePlayButtonContent();
        }
    }

    public bool IsPaused { 
        get => _player.isPaused;
        set
        {
            UpdatePlayButtonContent();
        }
    }

    /*public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(Controlbar), new PropertyMetadata(false, OnPlayerStateChanged));*/

    /*public bool PlayerState
    {
        get => (VLCState)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(PlayerState), typeof(bool), typeof(Controlbar), new PropertyMetadata(false, OnPlayerStateChanged));*/

    /// <summary>
    /// Gets or sets the current playback position of the media.
    /// </summary>
    public string Position
    {
        get => _position;
        set
        {
            _position = value;
            _currentTimeTextBlock.Text = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    /// <summary>
    /// Gets or sets the total duration of the media.
    /// </summary>
    public string Duration
    {
        get => _duration;
        set
        {
            _duration = value;
            _durationTextBlock.Text = value;
            OnPropertyChanged(nameof(Duration));
        }
    }

    /*public bool IsPaused
    {
        get { return _player.isPaused; }
        set
        {
            _player.isPaused = value;
            UpdatePlayButtonContent();
            OnPropertyChanged("IsPaused");
        }  
    }
    public bool IsStopped { get; set; }*/

    // Store the volume before muting
    private double _previousVolume;
    private string _position = "00:00:00";
    private string _duration = "00:00:00";
    private int _scrollTextIndex = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlBar"/> class.
    /// </summary>
    public ControlBar()
    {
        InitializeComponent();
        
        _playPauseButton.Click += (s, e) =>
        {
            UpdatePlayButtonContent();
        };
        _repeatButton.Click += (s, e) => RepeatPopupVisibility = !RepeatPopupVisibility;
        _repeatComboBox.SelectionChanged += (s, e) =>
        {
            RepeatPopupVisibility = false;
            UpdateRepeatButtonContent();
        };
        _muteButton.Click += (s, e) => ToggleMute();
        _volumeSlider.ValueChanged += (s, e) =>
        {
            if (IsMuted && Volume > 0)
            {
                IsMuted = false;
            }
            UpdateVolumePopupText();
        };

        UpdateRepeatButtonContent();
        UpdateMuteButtonContent();
        UpdatePlayButtonContent();
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlBar"/> class with a specified media player.
    /// </summary>
    /// <param name="player">The media player instance to associate with the control bar.</param>
    public ControlBar(IPlayer player) : this()
    {
        _player = player;

        SubscribePlayerEvents();
    }
    /// <summary>
    /// Sets the media player for the control bar.
    /// </summary>
    /// <param name="player">The media player instance to associate with the control bar.</param>
    public void SetPlayer(IPlayer player)
    {
        _player = player;

        SubscribePlayerEvents();
    }

    private void SubscribePlayerEvents()
    {
        // Subscribe player events
        if (_player != null)
        {

            _player.Playing += (s, e) => UpdateMediaTitle();
            _player.Stopped += (s, e) =>
            {
                MediaTitle = "No Media";
                CurrentTime = "00:00";
                MediaDuration = "00:00";
            };
            _player.TimeChanged += (s, e) => UpdateCurrentTime(e.Time);
            _player.LengthChanged += (s, e) => UpdateMediaDuration((long)_player.Playlist.Current.Duration.TotalMilliseconds);
        }
    }
    /// <summary>
    /// Starts the scrolling animation for the media title.
    /// </summary>
    public void StartTitleScroll()
    {
        var scrollAnimation = new DoubleAnimation
        {
            From = 0,
            To = -(_mediaTitleTextBlock.ActualWidth),
            Duration = new Duration(TimeSpan.FromSeconds(10)),
            RepeatBehavior = RepeatBehavior.Forever
        };
        _mediaTitleTextBlock.BeginAnimation(Canvas.LeftProperty, scrollAnimation);
    }
    /// <summary>
    /// Stops the scrolling animation for the media title.
    /// </summary>
    public void StopTitleScroll()
    {
        _mediaTitleTextBlock.BeginAnimation(Canvas.LeftProperty, null);
    }
    /// <summary>
    /// Closes/Hide the control bar.
    /// </summary>
    public void CloseControlBar()
    {
        this.Visibility = Visibility.Collapsed;
    }

    private static void OnPlayerStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        if (control._player != null)
        {
            if (control._player.isPlaying)
            {
                control.UpdatePlayButtonContent();
            }
            //switch (control._player.State)
            //{
            //    case VLCState.NothingSpecial:
            //        break;
            //    case VLCState.Opening:
            //        break;
            //    case VLCState.Buffering:
            //        break;
            //    case VLCState.Playing:
            //        control.UpdatePlayButtonContent();
            //        break;
            //    case VLCState.Paused:
            //        control.UpdatePlayButtonContent();
            //        break;
            //    case VLCState.Stopped:
            //        break;
            //    case VLCState.Ended:
            //        break;
            //    case VLCState.Error:
            //        break;
            //    default:
            //        break;
            //}
        }

        control.OnPropertyChanged(nameof(IsPlaying));
    }

    private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        if (control._player != null)
        {
            control._player.Volume = control.IsMuted ? 0 : (int)control.Volume;
        }
        control.UpdateVolumePopupText();
        control.OnPropertyChanged(nameof(Volume));
    }

    private static void OnRepeatModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        control.UpdateRepeatButtonContent();
        control.OnPropertyChanged(nameof(RepeatMode));
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        if (control._player != null)
        {
            control._player.isPlaying = control.IsPlaying;
            control._player.isPaused = control.IsPaused;
            //control._player.isStopped = control.IsStopped;
        }
        control.UpdateMuteButtonContent();
        control.UpdateVolumePopupText();
        control.OnPropertyChanged(nameof(IsMuted));
    }

    private static void OnIsMutedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        if (control._player != null)
        {
            control._player.isMute = control.IsMuted;
            control._player.Volume = control.IsMuted ? 0 : (int)control.Volume;
        }
        control.UpdateMuteButtonContent();
        control.UpdateVolumePopupText();
        control.OnPropertyChanged(nameof(IsMuted));
    }

    private static void OnMediaTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        control.OnPropertyChanged(nameof(MediaTitle));
    }

    private static void OnCurrentTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        control.OnPropertyChanged(nameof(Position));
    }

    private static void OnMediaDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ControlBar)d;
        control.OnPropertyChanged(nameof(MediaDuration));
    }

    private void UpdateRepeatButtonContent()
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
                _repeatButton.ToolTip = "Random";
                break;
            default:
                _repeatButton.Content = "🔁";
                _repeatButton.ToolTip = "No Repeat";
                break;
        }
    }

    private void UpdateMuteButtonContent()
    {
        _muteButton.Content = IsMuted ? "🔇" : "🔊";
        _muteButton.ToolTip = IsMuted ? "Unmute" : "Mute";
    }

    private void UpdatePlayButtonContent()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_player.isStopped)
            {
                _playPauseButton.Content = "▶";
                _playPauseButton.ToolTip = "Play";
                return;
            }
            if (IsPaused)
            {
                _playPauseButton.Content = "⏸️";
                _playPauseButton.ToolTip = "Pause";
                return;
            }
            if (IsPlaying)
            {
                _playPauseButton.Content = "▶";
                _playPauseButton.ToolTip = "Play";
                return;
            }
        });
        //else if (IsStopped)
        //{
        //    _playPauseButton.Content = "▶";
        //    _playPauseButton.ToolTip = "Stopped";
        //}
        //_playPauseButton.ToolTip = _player.isPlaying ? "Play" : "Pause";
    }

    private void ToggleMute()
    {
        if (IsMuted)
        {
            Volume = _previousVolume;
            IsMuted = false;
        }
        else
        {
            _previousVolume = Volume;
            Volume = 0;
            IsMuted = true;
        }
    }

    private void UpdateVolumePopupText()
    {
        VolumePopupText = IsMuted ? "Muted" : $"Volume: {Volume:F0}";
    }

    private void UpdateMediaTitle()
    {
        if (_player.Playlist.Current != null)
        {
            var title = _player.Playlist.Current.Name;
            if (_player.Playlist.Current.IsPlaying)
            {
                IsPlaying = true;
            }

            MediaTitle = string.IsNullOrEmpty(title) ? "Unknown Title" : title;
        }
        else
        {
            MediaTitle = "No Media";
        }
    }

    private void UpdateCurrentTime(long milliseconds)
    {
        CurrentTime = FormatTime(milliseconds);
    }

    private void UpdateMediaDuration(long milliseconds)
    {
        MediaDuration = FormatTime(milliseconds);
    }

    private string FormatTime(long milliseconds)
    {
        TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    /// <summary>
    /// Raises the PropertyChanged event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
