// Version: 0.1.7.83
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

using Thmd.Repeats;

namespace Thmd.Controls;

///

/// A user control that manages repeat functionality for media playback, supporting different repeat modes and shuffle options. 
/// /// Clicking the control cycles through available repeat modes (None, One, All). 
/// /// [ContentProperty("RepeatType")] 
public partial class RepeatControl : UserControl
{
    private RepeatType _repeatType = RepeatType.None;
    private readonly List<RepeatType> _repeatModeList = new List<RepeatType>();



    /// <summary>
    /// Occurs when a property value changes, enabling data binding support.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets or sets the current repeat type for media playback.
    /// Updates the associated text block with the string representation of the repeat type.
    /// </summary>
    /// <value>The current <see cref="RepeatType"/> value.</value>
    public RepeatType RepeatType
    {
        get => _repeatType;
        set
        {
            if (_repeatType != value)
            {
                _repeatType = value;
                _repeatTextBlock.Text = value.ToString();
                OnPropertyChanged(nameof(RepeatType));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled.
    /// </summary>
    /// <value><c>true</c> if shuffle mode is enabled; otherwise, <c>false</c>.</value>
    public bool EnableShuffle
    {
        get => _enableShuffleCheckBox.IsChecked ?? false;
        set
        {
            if (_enableShuffleCheckBox.IsChecked != value)
            {
                _enableShuffleCheckBox.IsChecked = value;
                OnPropertyChanged(nameof(EnableShuffle));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatControl"/> class.
    /// Sets up the component, populates the repeat mode list, and adds click event handling.
    /// </summary>
    public RepeatControl()
    {
        InitializeComponent();

        _repeatModeList.Add(RepeatType.None);
        _repeatModeList.Add(RepeatType.One);
        _repeatModeList.Add(RepeatType.All);

        MouseLeftButtonDown += OnRepeatControlClick;
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                OnRepeatControlClick(this, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            }
        };
    }

    /// <summary>
    /// Handles the click event on the control to cycle through available repeat modes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse button event arguments.</param>
    private void OnRepeatControlClick(object sender, MouseButtonEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            int currentIndex = _repeatModeList.IndexOf(RepeatType);
            int nextIndex = (currentIndex + 1) % _repeatModeList.Count;
            RepeatType = _repeatModeList[nextIndex];
        });
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
