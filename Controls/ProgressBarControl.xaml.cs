// Version: 0.1.0.16
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Thmd.Logs;
using Thmd.Media;

namespace Thmd.Controls;

public partial class ProgressBarControl : UserControl, INotifyPropertyChanged
{
	private IPlayer _player;

	private string _progressBarText = "";

	private TimeSpan _duration;

	private bool _popupVisibility = false;

	private Brush _backgroundBrush = Brushes.Black;

	private Brush _foregroundBrush = Brushes.DarkOrange;

	private double _value = 0.0;

	private double _buffor = 0.0;

	public Brush TextForgroundBrush
	{
		get
		{
			return _foregroundBrush;
		}
		set
		{
			_foregroundBrush = value;
		}
	}

	public Brush TextBackgroundBrush
	{
		get
		{
			return _backgroundBrush;
		}
		set
		{
			_backgroundBrush = value;
		}
	}

	public double Time => (_player != null) ? _player.CurrentTime.Milliseconds : 0;

	public double Maximum
	{
		get
		{
			return _progressBar.Maximum;
		}
		set
		{
			_progressBar.Maximum = value;
		}
	}

	public double Minimum
	{
		get
		{
			return _progressBar.Minimum;
		}
		set
		{
			_progressBar.Minimum = value;
		}
	}

	public double Value
	{
		get
		{
			return _value;
		}
		set
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				_value = value / ((_duration.TotalMilliseconds > 0.0) ? _duration.TotalMilliseconds : 1.0) * Maximum;
				try
				{
					_progressBar.Value = _value;
				}
				catch (Exception ex)
				{
					_progressBar.Value = 0.0;
					Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, ex.Message ?? "");
				}
				OnPropertyChanged("Value", ref _value, value);
			});
		}
	}

	public Color BufforBarColor
	{
		get
		{
			return BufforBarColor;
		}
		set
		{
			_rectangleBufferMedia.Fill = new SolidColorBrush(value);
		}
	}

	public double BufforBarValue
	{
		get
		{
			return _buffor;
		}
		set
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				_rectangleBufferMedia.Width = value * base.ActualWidth / ((Maximum > 0.0) ? Maximum : 1.0);
			});
			OnPropertyChanged("BufforBarValue", ref _buffor, value);
		}
	}

	public TimeSpan Duration
	{
		get
		{
			return _duration;
		}
		set
		{
			_duration = value;
		}
	}

	public string ProgressText
	{
		get
		{
			return _progressBarText ?? string.Empty;
		}
		set
		{
			if (_progressBarText != value)
			{
				_progressBarText = value ?? string.Empty;
				OnPropertyChanged("ProgressText");
			}
		}
	}

	public bool PopupVisibility
	{
		get
		{
			return _popupVisibility;
		}
		set
		{
			if (_popupVisibility != value)
			{
				_popupVisibility = value;
				_popup.IsOpen = value;
			}
		}
	}

	public string PopupText
	{
		get
		{
			return _popuptext.Text;
		}
		set
		{
			if (_popuptext.Text != value)
			{
				_popuptext.Text = value;
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public ProgressBarControl()
	{
		InitializeComponent();
		_progressBarText = "00:00:00";
		base.DataContext = this;
		Minimum = 0.0;
		Maximum = 100.0;
		_rectangleMouseOverPoint.Visibility = Visibility.Hidden;
		_popup.IsOpen = false;
		_popuptext.Width = 100.0;
		_popuptext.Background = new DrawingBrush(DrawingBackground());
		_popuptext.Effect = new DropShadowEffect();
		_progressBar.ValueChanged += _progressBar_ValueChanged;
		BufforBarColor = Color.FromArgb(45, 138, 43, 226);
	}

	public ProgressBarControl(IPlayer player)
		: this()
	{
		_player = player;
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
	}

	private void _progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		_value = e.NewValue;
	}

	protected void OnPropertyChanged(string propertyName)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
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

	private void _grid_MouseMove(object sender, MouseEventArgs e)
	{
		Point mouse_position = e.GetPosition(_progressBar);
		double width = _progressBar.ActualWidth;
		double position = mouse_position.X / width * _progressBar.Maximum;
		double time_in_ms = _duration.TotalMilliseconds * position / ((_progressBar.Maximum > 0.0) ? _progressBar.Maximum : 1.0);
		TimeSpan time = TimeSpan.FromMilliseconds(time_in_ms);
		_rectangleMouseOverPoint.Visibility = Visibility.Visible;
		_rectangleMouseOverPoint.Stroke = Brushes.DarkOrange;
		_rectangleMouseOverPoint.StrokeThickness = 3.0;
		_rectangleMouseOverPoint.Margin = new Thickness(e.GetPosition(this).X, 0.0, 0.0, 0.0);
		if (!_popup.IsOpen)
		{
			PopupVisibility = true;
		}
		_popup.HorizontalOffset = mouse_position.X + 20.0;
		_popup.VerticalOffset = mouse_position.Y;
	}

	private void _grid_MouseLeave(object sender, MouseEventArgs e)
	{
		_rectangleMouseOverPoint.Visibility = Visibility.Hidden;
		PopupVisibility = false;
	}

	private Drawing DrawingBackground()
	{
		DrawingGroup drawing_group = new DrawingGroup();
		using (DrawingContext context = drawing_group.Open())
		{
			context.DrawEllipse(_backgroundBrush, new Pen(_foregroundBrush, 5.0), new Point(0.0, 0.0), 35.0, 35.0);
		}
		return drawing_group;
	}
}
