// Version: 0.1.0.34
// ControlBarControl.cs
// A custom UserControl that provides a control bar for media playback, including buttons for
// play, stop, next, previous, volume control, subtitles, fullscreen, and playlist management.
// It also includes repeat mode controls and displays video name and playback time.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Controls;

/// <summary>
/// A custom UserControl that provides a control bar for media playback.
/// Includes buttons for play, stop, next, previous, volume control, subtitles, fullscreen,
/// playlist management, and repeat mode selection. Displays the current video name and playback time.
/// Implements INotifyPropertyChanged for data binding.
/// </summary>
public partial class ControlBarControl : UserControl
{
    // The media player interface for controlling playback.
    private IPlayer _player;

    // The name of the currently playing video.
    private string _videoName;

    // The current playback time and duration of the video.
    private string _videoTime;

    /// <summary>
    /// Gets the play/pause button.
    /// </summary>
    public Button BtnPlay => _playerBtnControl._btnPlay;

    /// <summary>
    /// Gets the stop button.
    /// </summary>
    public Button BtnStop => _playerBtnControl._btnStop;

    /// <summary>
    /// Gets the next track button.
    /// </summary>
    public Button BtnNext => _playerBtnControl._btnNext;

    /// <summary>
    /// Gets the previous track button.
    /// </summary>
    public Button BtnPrevious => _playerBtnControl._btnPrevious;

    /// <summary>
    /// Gets the volume up button.
    /// </summary>
    public Button BtnVolumeUp => _playerBtnVolume._btnVolumeUp;

    /// <summary>
    /// Gets the volume down button.
    /// </summary>
    public Button BtnVolumeDown => _playerBtnVolume._btnVolumeDown;

    /// <summary>
    /// Gets the mute/unmute button.
    /// </summary>
    public Button BtnMute => _playerBtnVolume._btnMute;

    /// <summary>
    /// Gets the settings window button.
    /// </summary>
    public Button BtnSettingsWindow => _playerBtnSecondRow._btnSettings;

    /// <summary>
    /// Gets the subtitle toggle button.
    /// </summary>
    public Button BtnSubtitle => _playerBtnSecondRow._btnSubtitle;

    /// <summary>
    /// Gets the update button.
    /// </summary>
    public Button BtnUpdate => _playerBtnSecondRow._btnUpdate;

    /// <summary>
    /// Gets the open file button.
    /// </summary>
    public Button BtnOpen => _playerBtnControl._btnOpen;

    /// <summary>
    /// Gets the playlist toggle button.
    /// </summary>
    public Button BtnPlaylist => _playerBtnSecondRow._btnPlaylist;

    /// <summary>
    /// Gets the fullscreen toggle button.
    /// </summary>
    public Button BtnFullscreen => _playerBtnSecondRow._btnFullscreen;

    /// <summary>
    /// Gets the close button.
    /// </summary>
    public Button BtnClose => _btnClose._btnClose;

    /// <summary>
    /// Gets or sets the name of the currently playing video, updating the UI text block.
    /// </summary>
    public string VideoName
    {
        get => _videoName;
        set
        {
            base.Dispatcher.InvokeAsync(delegate
            {
                if (_videoNameTextBlock != null)
                {
                    _videoNameTextBlock.Text = value;
                }
            });
            OnPropertyChanged("VideoName", ref _videoName, value);
        }
    }

    /// <summary>
    /// Gets or sets the current playback time and duration, updating the UI text block.
    /// </summary>
    public string VideoTime
    {
        get => _videoTime;
        set
        {
            base.Dispatcher.InvokeAsync(delegate
            {
                if (_videoTimeTextBlock != null)
                {
                    _videoTimeTextBlock.Text = value;
                }
            });
            OnPropertyChanged("VideoTime", ref _videoTime, value);
        }
    }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlBarControl"/> class.
    /// </summary>
    public ControlBarControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Checked event for the random repeat mode checkbox, setting the player's repeat mode to Random.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void _repeatRandom_Checked(object sender, RoutedEventArgs e)
    {
        _repeatOne.IsChecked = false;
        _repeatAll.IsChecked = false;
        _repeatNone.IsChecked = false;
        _player.Repeat = RepeatType.Random;
        _repeatControl.RepeatMode = RepeatType.Random.ToString();
    }

    /// <summary>
    /// Handles the Checked event for the none repeat mode checkbox, setting the player's repeat mode to None.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void _repeatNone_Checked(object sender, RoutedEventArgs e)
    {
        _repeatAll.IsChecked = false;
        _repeatOne.IsChecked = false;
        _repeatRandom.IsChecked = false;
        _player.Repeat = RepeatType.None;
        _repeatControl.RepeatMode = RepeatType.None.ToString();
    }

    /// <summary>
    /// Handles the Checked event for the all repeat mode checkbox, setting the player's repeat mode to All.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void _repeatAll_Checked(object sender, RoutedEventArgs e)
    {
        _repeatNone.IsChecked = false;
        _repeatOne.IsChecked = false;
        _repeatRandom.IsChecked = false;
        _player.Repeat = RepeatType.All;
        _repeatControl.RepeatMode = RepeatType.All.ToString();
    }

    /// <summary>
    /// Handles the Checked event for the one repeat mode checkbox, setting the player's repeat mode to Current.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void _repeatOne_Checked(object sender, RoutedEventArgs e)
    {
        _repeatRandom.IsChecked = false;
        _repeatAll.IsChecked = false;
        _repeatNone.IsChecked = false;
        _player.Repeat = RepeatType.Current;
        _repeatControl.RepeatMode = RepeatType.Current.ToString();
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify the UI of property changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        if (this.PropertyChanged != null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
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
        this.PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the media player for the control bar and initializes repeat mode event handlers.
    /// </summary>
    /// <param name="player">The media player to associate with the control bar.</param>
    public void SetPlayer(IPlayer player)
    {
        _player = player;
        _repeatOne.Checked += _repeatOne_Checked;
        _repeatAll.Checked += _repeatAll_Checked;
        _repeatNone.Checked += _repeatNone_Checked;
        _repeatRandom.Checked += _repeatRandom_Checked;
        _repeatNone.IsChecked = true;
    }
}
