// Version: 0.1.13.39
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

using Thmd.Media;

namespace Thmd.Controls;

/// <summary>
/// Logika interakcji dla klasy TimerBox.xaml
/// Represents a WPF UserControl that displays a timer for media playback duration or position.
/// Implements INotifyPropertyChanged for data binding support in WPF .NET 4.8.
/// </summary>
public partial class TimerBox : UserControl, INotifyPropertyChanged
{
    /// <summary>
    /// Reference to the media player instance for synchronization with playback state.
    /// </summary>
    private IPlayer _player;

    /// <summary>
    /// Backing field for the current timer value, formatted as "HH:MM:SS".
    /// </summary>
    private string _timer = "00:00:00";

    /// <summary>
    /// Occurs when a property value changes, enabling data binding notifications.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets or sets the current timer display text.
    /// Updates the associated TextBlock in the XAML template and raises PropertyChanged.
    /// </summary>
    public string Timer
    {
        get => _timer;
        set
        {
            _timer = value;
            _timerTextBlock.Text = value;
            OnPropertyChanged(nameof(Timer));
        }
    }

    /// <summary>
    /// Initializes a new instance of the TimerBox class.
    /// Calls InitializeComponent to load the XAML template.
    /// </summary>
    public TimerBox()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the TimerBox class with a media player reference.
    /// Calls the parameterless constructor and sets the player for synchronization.
    /// </summary>
    /// <param name="player">The IPlayer instance to associate with this timer.</param>
    public TimerBox(IPlayer player) : this()
    {
        _player = player;
    }

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
