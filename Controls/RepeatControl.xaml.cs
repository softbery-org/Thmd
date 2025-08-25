// RepeatControl.xaml.cs
// Version: 0.1.0.78
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using Thmd.Repeats;

namespace Thmd.Controls;

/// <summary>
/// A user control that manages repeat functionality for media playback, supporting different repeat modes and shuffle options.
/// </summary>
[ContentProperty("RepeatType")]
public partial class RepeatControl : UserControl
{
    private RepeatType _repeatType = RepeatType.None;

    /// <summary>
    /// Occurs when a property value changes, enabling data binding support.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private List<RepeatType> _repeatModeList = new List<RepeatType>();

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
            _repeatTextBlock.Text = value.ToString();
            OnPropertyChanged(nameof(RepeatType), ref _repeatType, value);
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
            _enableShuffleCheckBox.IsChecked = value;
            OnPropertyChanged(nameof(EnableShuffle));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatControl"/> class.
    /// Sets up the component and populates the repeat mode list with available options.
    /// </summary>
    public RepeatControl()
    {
        InitializeComponent();

        _repeatModeList.Add(RepeatType.None);
        _repeatModeList.Add(RepeatType.One);
        _repeatModeList.Add(RepeatType.All);
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for a specific field and updates its value.
    /// </summary>
    /// <typeparam name="T">The type of the field being updated.</typeparam>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="field">A reference to the field to update.</param>
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
}