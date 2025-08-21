// Version: 0.1.0.18
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Thmd.Subtitles;

namespace Thmd.Controls;

public partial class SubtitleControl : UserControl
{
	public delegate void TimeHandlerDelegate(object sender, TimeSpan time);

	private TimeSpan _positionTime = TimeSpan.Zero;

	private SubtitleManager _subtitleManager;

	private double _fontSize = 48.0;

	private Brush _backgroundBrush = new SolidColorBrush(Colors.Transparent);

	private Brush _subtitleBrush = new SolidColorBrush(Colors.White);

	private bool _shadowSubtitle = true;

	private FontFamily _fontFamily = new FontFamily("SagoeUI");

	private bool _sizeToFit = true;

	private Size _size;

	private FrameworkElement _parent;

	private string _filePath;

	public FrameworkElement TextBlock => _subtitleTextBlock;

	public bool SubtitleShadow
	{
		get
		{
			return _shadowSubtitle;
		}
		set
		{
			if (_shadowSubtitle != value)
			{
				if (value)
				{
					DropShadowEffect e = new DropShadowEffect();
					_subtitleTextBlock.Effect = e;
				}
				else
				{
					_subtitleTextBlock.Effect = null;
				}
				_shadowSubtitle = value;
			}
		}
	}

	public FontFamily SubtitleFontFamily
	{
		get
		{
			return _fontFamily;
		}
		set
		{
			if (_fontFamily != value)
			{
				_fontFamily = value;
				_subtitleTextBlock.FontFamily = value;
				OnPropertyChanged("SubtitleFontFamily", ref _fontFamily, value);
			}
		}
	}

	public double SubtitleFontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			if (_fontSize != value)
			{
				_subtitleTextBlock.FontSize = value;
				OnPropertyChanged("_fontSize", ref _fontSize, value);
			}
		}
	}

	public Brush SubtitleBackground
	{
		get
		{
			return _backgroundBrush;
		}
		set
		{
			if (_backgroundBrush != value)
			{
				base.Background = value;
				OnPropertyChanged("_backgroundBrush", ref _backgroundBrush, value);
			}
		}
	}

	public Brush SubtitleBrush
	{
		get
		{
			return _subtitleBrush;
		}
		set
		{
			if (_subtitleBrush != value)
			{
				_subtitleTextBlock.Foreground = value;
				OnPropertyChanged("_backgroundBrush", ref _subtitleBrush, value);
			}
		}
	}

	public string FilePath
	{
		get
		{
			return _filePath;
		}
		set
		{
			if (_filePath != value)
			{
				_filePath = value;
				OnPropertyChanged("FilePath", ref _filePath, value);
				_subtitleManager = new SubtitleManager(value);
				GetSubtitle(PositionTime);
			}
		}
	}

	public string Text { get; set; } = string.Empty;

	public TimeSpan PositionTime
	{
		get
		{
			return _positionTime;
		}
		set
		{
			GetSubtitle(value);
			OnPropertyChanged("_positionTime", ref _positionTime, value);
		}
	}

	public event TimeHandlerDelegate TimeChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public SubtitleControl()
	{
		InitializeComponent();
		SubtitleBrush = new SolidColorBrush(Colors.White);
		SubtitleBackground = new SolidColorBrush(Colors.Transparent);
		SubtitleFontSize = 58.0;
		Text = string.Empty;
		SubtitleFontFamily = new FontFamily("Calibri");
	}

	public SubtitleControl(string filePath)
		: this()
	{
		FilePath = filePath;
		_subtitleManager = new SubtitleManager(filePath);
		_subtitleTextBlock.Text = "";
	}

	public SubtitleControl(FrameworkElement parent)
		: this()
	{
		_parent = parent;
		_parent.SizeChanged += OnParentSizeChanged;
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

	protected override void OnRender(DrawingContext drawingContext)
	{
		if (FilePath != null)
		{
			_subtitleManager = new SubtitleManager(FilePath);
			GetSubtitle(PositionTime);
		}
		base.OnRender(drawingContext);
	}

	private async void GetSubtitle(TimeSpan time)
	{
		Task task = Task.Run(delegate
		{
			if (_subtitleManager != null)
			{
				base.Dispatcher.InvokeAsync(delegate
				{
					if (_subtitleManager != null)
					{
						foreach (Subtitle current in _subtitleManager.Subtitles)
						{
							if (time >= current.StartTime)
							{
								string text = string.Join(Environment.NewLine, current.Items);
								_subtitleTextBlock.Text = text;
							}
							if (time >= current.EndTime)
							{
								_subtitleTextBlock.Text = string.Empty;
							}
						}
					}
				});
			}
		});
		await Task.FromResult(task);
	}

	private void OnParentSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_parent != null)
		{
			double newFontSize = e.NewSize.Height / 15.0;
			SubtitleFontSize = ((newFontSize > 10.0) ? newFontSize : 10.0);
		}
	}
}
