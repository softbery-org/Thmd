// Version: 0.1.7.83
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;

using Thmd.Subtitles;

namespace Thmd.Controls;

/// <summary>
/// A custom user control for displaying subtitles from an SRT file with support for formatting tags
/// such as &lt;i&gt;, &lt;b&gt;, &lt;u&gt;, &lt;font color="..."&gt;, and &lt;font size="..."&gt;.
/// </summary>
public partial class SubtitleControl : UserControl
{
    /// <summary>
    /// Delegate for handling time change events.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="time">The current time position in the subtitle timeline.</param>
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
    private TextStyle _textStyle = TextStyle.Normal;

    /// <summary>
    /// Gets the TextBlock used to display the subtitle text.
    /// </summary>
    public FrameworkElement TextBlock => _subtitleTextBlock;

    /// <summary>
    /// Gets or sets a value indicating whether a shadow effect is applied to the subtitle text.
    /// </summary>
    public bool SubtitleShadow
    {
        get => _shadowSubtitle;
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

    /// <summary>
    /// Gets or sets the font family for the subtitle text.
    /// </summary>
    public FontFamily SubtitleFontFamily
    {
        get => _fontFamily;
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

    /// <summary>
    /// Gets or sets the font size for the subtitle text.
    /// </summary>
    /// <remarks>
    /// This serves as the default font size unless overridden by &lt;font size="..."&gt; tags in the subtitle text.
    /// </remarks>
    public double SubtitleFontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _subtitleTextBlock.FontSize = value;
                OnPropertyChanged("_fontSize", ref _fontSize, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the background brush for the subtitle control.
    /// </summary>
    public Brush SubtitleBackground
    {
        get => _backgroundBrush;
        set
        {
            if (_backgroundBrush != value)
            {
                base.Background = value;
                OnPropertyChanged("_backgroundBrush", ref _backgroundBrush, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the foreground brush for the subtitle text.
    /// </summary>
    /// <remarks>
    /// This serves as the default text color unless overridden by &lt;font color="..."&gt; tags in the subtitle text.
    /// </remarks>
    public Brush SubtitleBrush
    {
        get => _subtitleBrush;
        set
        {
            if (_subtitleBrush != value)
            {
                _subtitleTextBlock.Foreground = value;
                OnPropertyChanged("_subtitleBrush", ref _subtitleBrush, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the path to the SRT subtitle file.
    /// </summary>
    /// <remarks>
    /// Setting this property initializes the subtitle manager and triggers subtitle processing.
    /// </remarks>
    public string FilePath
    {
        get => _filePath;
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

    /// <summary>
    /// Gets or sets the current subtitle text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base text style for the subtitle text block.
    /// </summary>
    /// <remarks>
    /// This applies a uniform style to the entire text block. Inline formatting tags
    /// (&lt;i&gt;, &lt;b&gt;, etc.) override this style for specific text segments.
    /// </remarks>
    public TextStyle TextStyle
    {
        get => _textStyle;
        set
        {
            if (_textStyle != value)
            {
                if (value == TextStyle.Bold)
                    _subtitleTextBlock.FontWeight = FontWeights.Bold;
                else if (value == TextStyle.Italic)
                    _subtitleTextBlock.FontStyle = FontStyles.Italic;
                else if (value == TextStyle.BoldItalic)
                {
                    _subtitleTextBlock.FontWeight = FontWeights.Bold;
                    _subtitleTextBlock.FontStyle = FontStyles.Italic;
                }
                else
                {
                    _subtitleTextBlock.FontWeight = FontWeights.Normal;
                }
                OnPropertyChanged("TextStyle", ref _textStyle, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current time position in the subtitle timeline.
    /// </summary>
    /// <remarks>
    /// Setting this property triggers the retrieval and display of the appropriate subtitle.
    /// </remarks>
    public TimeSpan PositionTime
    {
        get => _positionTime;
        set
        {
            GetSubtitle(value);
            OnPropertyChanged("_positionTime", ref _positionTime, value);
        }
    }

    /// <summary>
    /// Occurs when the subtitle time position changes.
    /// </summary>
    public event TimeHandlerDelegate TimeChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleControl"/> class.
    /// </summary>
    public SubtitleControl()
    {
        InitializeComponent();
        SubtitleBrush = new SolidColorBrush(Colors.White);
        SubtitleBackground = new SolidColorBrush(Colors.Transparent);
        SubtitleFontSize = 58.0;
        Text = string.Empty;
        SubtitleFontFamily = new FontFamily("Calibri");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleControl"/> class with a specified SRT file path.
    /// </summary>
    /// <param name="filePath">The path to the SRT subtitle file.</param>
    public SubtitleControl(string filePath) : this()
    {
        FilePath = filePath;
        _subtitleManager = new SubtitleManager(filePath);
        _subtitleTextBlock.Text = "";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleControl"/> class with a parent element for size adjustment.
    /// </summary>
    /// <param name="parent">The parent element used to adjust font size based on its dimensions.</param>
    public SubtitleControl(FrameworkElement parent) : this()
    {
        _parent = parent;
        _parent.SizeChanged += OnParentSizeChanged;
    }

    /// <summary>
    /// Raises the PropertyChanged event for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
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
        PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Renders the control and initializes subtitle processing if a file path is set.
    /// </summary>
    /// <param name="drawingContext">The drawing context used for rendering.</param>
    protected override void OnRender(DrawingContext drawingContext)
    {
        if (FilePath != null)
        {
            _subtitleManager = new SubtitleManager(FilePath);
            GetSubtitle(PositionTime);
        }
        base.OnRender(drawingContext);
    }

    /// <summary>
    /// Retrieves and displays the subtitle corresponding to the specified time.
    /// </summary>
    /// <param name="time">The current time position in the subtitle timeline.</param>
    private async void GetSubtitle(TimeSpan time)
    {
        await Task.Run(() =>
        {
            if (_subtitleManager != null)
            {
                base.Dispatcher.InvokeAsync(() =>
                {
                    if (_subtitleManager != null)
                    {
                        _subtitleTextBlock.Inlines.Clear(); // Clear previous inlines
                        bool subtitleFound = false;

                        foreach (Subtitle current in _subtitleManager.Subtitles)
                        {
                            if (time >= current.StartTime && time < current.EndTime)
                            {
                                string text = string.Join(Environment.NewLine, current.Items);
                                ProcessSubtitleText(text);
                                subtitleFound = true;
                            }
                        }

                        if (!subtitleFound)
                        {
                            _subtitleTextBlock.Inlines.Add(new Run(string.Empty));
                        }
                    }
                });
            }
        });
    }

    /// <summary>
    /// Processes subtitle text to apply formatting based on tags such as &lt;i&gt;, &lt;b&gt;, &lt;u&gt;, 
    /// &lt;font color="..."&gt;, and &lt;font size="..."&gt;.
    /// </summary>
    /// <param name="text">The subtitle text containing formatting tags.</param>
    /// <remarks>
    /// This method parses the input text for formatting tags and applies the corresponding styles
    /// (italic, bold, underline, color, and font size) to the subtitle text block.
    /// Invalid color or size values fall back to the default <see cref="SubtitleBrush"/> or <see cref="SubtitleFontSize"/>.
    /// </remarks>
    private void ProcessSubtitleText(string text)
    {
        // Split text by formatting tags while preserving delimiters
        var parts = Regex.Split(text, "(<i>|</i>|<b>|</b>|<u>|</u>|<font\\s+color=\"[^\"]*\">|<font\\s+size=\"[^\"]*\">|</font>)");
        bool isItalic = false;
        bool isBold = false;
        bool isUnderline = false;
        Brush currentColor = _subtitleBrush; // Default to SubtitleBrush
        double? currentFontSize = null; // Null means use default SubtitleFontSize

        foreach (var part in parts)
        {
            // Handle font color tag
            if (Regex.IsMatch(part, "<font\\s+color=\"[^\"]*\">"))
            {
                var match = Regex.Match(part, "<font\\s+color=\"([^\"]*)\">");
                if (match.Success)
                {
                    string colorName = match.Groups[1].Value;
                    try
                    {
                        currentColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
                    }
                    catch
                    {
                        currentColor = _subtitleBrush; // Fallback to default if color is invalid
                    }
                }
                continue;
            }
            // Handle font size tag
            else if (Regex.IsMatch(part, "<font\\s+size=\"[^\"]*\">"))
            {
                var match = Regex.Match(part, "<font\\s+size=\"([^\"]*)\">");
                if (match.Success)
                {
                    string sizeValue = match.Groups[1].Value;
                    if (double.TryParse(sizeValue, out double fontSize))
                    {
                        currentFontSize = fontSize > 0 ? fontSize : _fontSize; // Use default if invalid
                    }
                    else
                    {
                        currentFontSize = _fontSize; // Fallback to default
                    }
                }
                continue;
            }
            else if (part == "</font>")
            {
                currentColor = _subtitleBrush; // Reset to default color
                currentFontSize = null; // Reset to default font size
                continue;
            }
            else if (part == "<i>")
            {
                isItalic = true;
                continue;
            }
            else if (part == "</i>")
            {
                isItalic = false;
                continue;
            }
            else if (part == "<b>")
            {
                isBold = true;
                continue;
            }
            else if (part == "</b>")
            {
                isBold = false;
                continue;
            }
            else if (part == "<u>")
            {
                isUnderline = true;
                continue;
            }
            else if (part == "</u>")
            {
                isUnderline = false;
                continue;
            }

            if (!string.IsNullOrEmpty(part))
            {
                var run = new Run(part)
                {
                    Foreground = currentColor,
                    FontSize = currentFontSize ?? _fontSize // Use default font size if null
                };
                Inline current = run;

                // Apply bold
                if (isBold)
                {
                    current = new Bold(current);
                }

                // Apply italic
                if (isItalic)
                {
                    current = new Italic(current);
                }

                // Apply underline
                if (isUnderline)
                {
                    current = new Underline(current);
                }

                _subtitleTextBlock.Inlines.Add(current);
            }
        }
    }

    /// <summary>
    /// Handles size changes of the parent element to adjust the subtitle font size.
    /// </summary>
    /// <param name="sender">The parent element that raised the event.</param>
    /// <param name="e">The event data containing the new size information.</param>
    private void OnParentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_parent != null)
        {
            double newFontSize = e.NewSize.Height / 15.0;
            SubtitleFontSize = ((newFontSize > 10.0) ? newFontSize : 10.0);
        }
    }
}

/// <summary>
/// Defines text style options for the subtitle control.
/// </summary>
public enum TextStyle
{
    /// <summary>
    /// Normal text style with no bold or italic formatting.
    /// </summary>
    Normal,
    /// <summary>
    /// Bold text style.
    /// </summary>
    Bold,
    /// <summary>
    /// Italic text style.
    /// </summary>
    Italic,
    /// <summary>
    /// Combined bold and italic text style.
    /// </summary>
    BoldItalic
}
