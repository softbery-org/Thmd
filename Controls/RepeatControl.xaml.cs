// Version: 0.1.0.74
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using Thmd.Repeats;

namespace Thmd.Controls;

public partial class RepeatControl : UserControl
{
    private RepeatType _repeatType = RepeatType.None;

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private List<RepeatType> _repeatModeList = new List<RepeatType>();

    /// <summary>
    /// Gets or sets the current repeat type as a string.
    /// </summary>
    public RepeatType RepeatType
    {
        get => _repeatType;
        set
        {
            _repeatTextBlock.Text = value.ToString();
            OnPropertyChanged(nameof(RepeatType), ref _repeatType, value);
        }
    }

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
    /// </summary>
    public RepeatControl()
	{
		InitializeComponent();

        _repeatModeList.Add(RepeatType.None);
        _repeatModeList.Add(RepeatType.One);
        _repeatModeList.Add(RepeatType.All);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}
