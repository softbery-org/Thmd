// Version: 0.1.10.25
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Newtonsoft.Json.Linq;

using Thmd.Media;

namespace Thmd.Controls;

public partial class ProgressBarView : UserControl, INotifyPropertyChanged
{
    //private string _progressText;
    //private string _popupText;
    private TimeSpan _duration;
    private double _bufforBarValue;
    private IPlayer _player;

    public ProgressBarView()
    {
        InitializeComponent();
        DataContext = this;
        /*_progressBar.MouseMove += ProgressBar_MouseMove;
        _progressBar.MouseDown += ProgressBar_MouseDown;*/
        _progressBar.ValueChanged += ProgressBar_ValueChanged;
        _popup.IsOpen = false;
        _popup.MouseLeave += Popup_MouseLeave;
    }

    private void Popup_MouseLeave(object sender, MouseEventArgs e)
    {
        _popup.IsOpen = false;
    }

    private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _progressBar.Value = e.NewValue;
        _rectangleBufforBar.Margin = new Thickness(_progressBar.ActualWidth / Value, 0, 0, 0);
    }

    public ProgressBarView(IPlayer player) : this()
    {
        _player = player;
    }

    public void SetPlayer(IPlayer player)
    {
        _player = player;
    }

    public string ProgressText
    {
        get => _progressText.Text;
        set
        {
            if (_progressText.Text != value)
            {
                _progressText.Text = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string PopupText
    {
        get => _popupText.Text;
        set
        {
            if (_popupText.Text != value)
            {
                _popupText.Text = value;
                OnPropertyChanged(nameof(PopupText));
            }
        }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                _duration = value;
                _progressBar.Maximum = value.TotalMilliseconds;
                ProgressText = $"00:00:00/{value:hh\\:mm\\:ss}";
                OnPropertyChanged(nameof(Duration));
            }
        }
    }
    private double _value = 0.0;
    public double Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                _progressBar.Value = value;
                _rectangleBufforBar.Margin = new Thickness(_progressBar.ActualWidth / Value, 0, 0, 0);
                UpdateProgressText();
                SeekRequested?.Invoke(this, TimeSpan.FromMilliseconds(value));
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public void SetValue(double value)
    {
        _progressBar.Value = value;
    }

    public double BufforBarValue
    {
        get => _bufforBarValue;
        set
        {
            if (_bufforBarValue != value)
            {
                _bufforBarValue = value;
                _rectangleBufforBar.Width = (_progressBar.ActualWidth > 0) ? (value / _progressBar.Maximum) * _progressBar.ActualWidth : 0;
                _rectangleBufforBar.Width -= _value;
                OnPropertyChanged(nameof(BufforBarValue));
            }
        }
    }

    public double Maximum
    {
        get => _progressBar.Maximum;
        set
        {
            _progressBar.Maximum = value;
            OnPropertyChanged(nameof(Maximum));
        }
    }

    public double Minimum
    {
        get => _progressBar.Minimum;
        set
        {
            _progressBar.Minimum = value;
            OnPropertyChanged(nameof(Minimum));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<TimeSpan> SeekRequested; // Zdarzenie dla przewijania

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

    private void UpdateProgressText()
    {
        TimeSpan currentTime = TimeSpan.FromMilliseconds(_progressBar.Value);
        ProgressText = $"{currentTime.Hours:00}:{currentTime.Minutes:00}:{currentTime.Seconds:00}/{_duration:hh\\:mm\\:ss}";
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _popup.IsOpen = false;
        _rectangleMouseOverPoint.Visibility = Visibility.Visible;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _popup.IsOpen = false;
        _rectangleMouseOverPoint.Visibility = Visibility.Hidden;
    }
}
