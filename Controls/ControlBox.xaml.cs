// ControlBox.xaml.cs
// Version: 0.1.1.85
// A custom UserControl that provides a control bar for media playback, including buttons for
// play, stop, next, previous, volume control, subtitles, fullscreen, and playlist management.
// It also includes repeat mode controls and displays video name and playback time.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Thmd.Configuration;
using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Controls;

/// <summary>
/// A custom UserControl that provides a control bar for media playback.
/// Includes buttons for play, stop, next, previous, volume control, subtitles, fullscreen,
/// playlist management, and repeat mode selection. Displays the current video name and playback time.
/// Implements INotifyPropertyChanged for data binding.
/// </summary>
public partial class ControlBox : UserControl
{
    // The media player interface for controlling playback.
    private IPlayer _player;

    // The name of the currently playing video.
    private string _videoName;

    // The current playback time and duration of the video.
    private string _videoTime;

    private List<RepeatType> _repeatTypes = new List<RepeatType>();

    private int _repeatIndex = 0;

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
    /// Initializes a new instance of the <see cref="ControlBox"/> class.
    /// </summary>
    public ControlBox()
    {
        InitializeComponent();

        _repeatTypes.Add(RepeatType.None);
        _repeatTypes.Add(RepeatType.One);
        _repeatTypes.Add(RepeatType.All);

        _repeatIndex = 0;

        RepeatControl.RepeatType = _repeatTypes[_repeatIndex];
    }

    private void RepeatControl_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            _repeatIndex++;

            if (_repeatIndex > _repeatTypes.Count - 1)
                _repeatIndex = 0;

            RepeatControl.RepeatType = _repeatTypes[_repeatIndex];
        }
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
        // Initialize repeat mode from configuration
        var repeat_type = Enum.Parse(typeof(RepeatType), Config.Instance.PlaylistConfig.RepeatType.ToString());
        var shuffle = bool.Parse(Config.Instance.PlaylistConfig.EnableShuffle.ToString());
        RepeatControl.RepeatType = (RepeatType)repeat_type;
        RepeatControl.EnableShuffle = shuffle;
    }

    private void RepeatControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        base.Cursor = System.Windows.Input.Cursors.Arrow;
    }

    private void RepeatControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        base.Cursor = System.Windows.Input.Cursors.Hand;
    }
}
