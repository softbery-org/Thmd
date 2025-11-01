// Version: 0.1.10.73
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

using LibVLCSharp.Shared;

using Newtonsoft.Json.Linq;

using Thmd.Converters;
using Thmd.Images;
using Thmd.Media;

namespace Thmd.Controls;

/// <summary>
/// Represents a WPF UserControl for displaying media playback progress in a video player.
/// Supports progress visualization, buffering indication, seek preview via popup, and interaction events.
/// Implements INotifyPropertyChanged for data binding in WPF .NET 4.8.
/// </summary>
public partial class ProgressBarView : UserControl, INotifyPropertyChanged
{
    /// <summary>
    /// Backing field for the total media duration.
    /// </summary>
    private TimeSpan _duration;
    /// <summary>
    /// Backing field for the current progress value (in milliseconds).
    /// </summary>
    private double _value = 0.0;
    /// <summary>
    /// Backing field for the buffering progress value (0-100 scale).
    /// </summary>
    private double _bufforBarValue;

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// Reference to the associated media player for synchronization and seek operations.
    /// </summary>
    private IPlayer _player;

    /// <summary>
    /// For update string builder.
    /// </summary>
    private readonly StringBuilder _sb = new StringBuilder();

    private bool _isDragging = false;
    private CancellationTokenSource? _dragCts;
    private const int DragUpdateIntervalMs = 50; // częstotliwość aktualizacji pozycji (20x/s)

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressBarView"/> class.
    /// </summary>
    /// <remarks>
    /// Sets up the DataContext for data binding, attaches event handlers for value changes and mouse interactions.
    /// Initializes the popup as closed and hides the mouse-over indicator.
    /// </remarks>
    public ProgressBarView()
    {
        Core.Initialize();
        InitializeComponent();

        DataContext = this;
        _progressBar.ValueChanged += ProgressBar_ValueChanged;
        _popup.IsOpen = false;
        _popup.MouseLeave += Popup_MouseLeave;

        _progressBar.MouseMove += async(s, e) =>
        {
            var position = e.GetPosition(_progressBar);
            var previewTime = TimeSpan.FromMilliseconds(position.X / _progressBar.ActualWidth * _progressBar.Maximum);

            PopupText = previewTime.ToString(@"hh\:mm\:ss");
            _popup.IsOpen = true;
            _popup.HorizontalOffset = position.X;
            _rectangleMouseOverPoint.Margin = new Thickness(position.X - 2, 0, 0, 0);

            // Pobranie miniatury z danego czasu
            if (_player != null)
            {
                var frame = await GetFrameAtAsync(previewTime);
                if (frame != null)
                {
                    _popupImage.Source = BitmapHelper.BitmapToImageSource(frame);
                }
            }
            // Jeśli trzymamy lewy przycisk — rozpocznij dynamiczne przewijanie
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ProgressBar_MouseDown(s, e);
            }
        };
    }

    private void ProgressBar_MouseDown(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        Value = position.X / this.ActualWidth * this.Maximum;  // Trigger seek
        ProgressBarMouseEventHandler(this, e);
    }

    /// <summary>
    /// Common handler for progress bar mouse events to perform seeking.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ProgressBarMouseEventHandler(object sender, MouseEventArgs e)
    {
        /*
        if (DesignerProperties.GetIsInDesignMode(this))
            return;
        */
        System.Windows.Point mousePosition = e.GetPosition(this);
        double width = _progressBar.ActualWidth;
        if (width <= 0) return;

        // Calculate the corresponding time from the mouse position using the forward converter
        var time = TimeToPositionConverter.Convert(mousePosition.X, width, _progressBar.Maximum);

        // Set the progress bar value to the milliseconds (as long) and seek the player
        _progressBar.Value = (long)time.TotalMilliseconds;
        _player.Position = time;

        // Update the visual indicator for the mouse position
        _rectangleMouseOverPoint.Margin = new Thickness(mousePosition.X - (_rectangleMouseOverPoint.Width / 2), 0, 0, 0);
    }

    /// <summary>
    /// Handles the MouseLeave event on the popup to close it.
    /// </summary>
    /// <param name="sender">The event sender (Popup).</param>
    /// <param name="e">The mouse event arguments.</param>
    private void Popup_MouseLeave(object sender, MouseEventArgs e)
    {
        _popup.IsOpen = false;
    }

    /// <summary>
    /// Handles changes to the progress bar value, updating the buffering bar margin accordingly.
    /// </summary>
    /// <param name="sender">The event sender (ProgressBar).</param>
    /// <param name="e">The routed property changed event arguments.</param>
    private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _progressBar.Value = e.NewValue;
        _rectangleBufforBar.Margin = new Thickness(_progressBar.ActualWidth / Value, 0, 0, 0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressBarView"/> class with a media player reference.
    /// </summary>
    /// <param name="player">The IPlayer instance to associate for seek and playback synchronization.</param>
    /// <remarks>
    /// Calls the parameterless constructor and sets the player reference.
    /// </remarks>
    public ProgressBarView(IPlayer player) : this()
    {
        _player = player;
    }

    /// <summary>
    /// Sets the reference to the media player for this progress bar.
    /// </summary>
    /// <param name="player">The IPlayer instance to associate.</param>
    public void SetPlayer(IPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// Gets or sets the progress text displayed on the bar (e.g., current time / total duration).
    /// </summary>
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

    /// <summary>
    /// Gets or sets the preview text shown in the popup during mouse hover/seek.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the total duration of the media.
    /// </summary>
    /// <remarks>
    /// Updates the progress bar maximum value and initializes the progress text format.
    /// </remarks>
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

    public async Task<Bitmap> GetFrameAtAsync(TimeSpan time, CancellationToken token = default)
    {
        if (_player.Playlist.Current==null)
            return null;
        if (string.IsNullOrEmpty(_player.Playlist.Current.Uri.LocalPath))
            return null;

        string tempPath = Path.Combine(Path.GetTempPath(), $"vlc_thumb_{Guid.NewGuid():N}.png");

        try
        {
            var lib = new LibVLCSharp.Shared.LibVLC(
            "--no-xlib",
            "--vout=dummy",
            "--aout=dummy",
            "--no-video-title-show",
            "--no-sub-autodetect-file",
            "--intf", "dummy",
            "--no-sout-display-video",
            "--no-osd",
            "--network-caching=100",
            "--avcodec-hw=none"
        );
            using var media = new LibVLCSharp.Shared.Media(lib, _player.Playlist.Current.Uri.LocalPath, FromType.FromPath);
            using var previewPlayer = new MediaPlayer(media);

            var tcs = new TaskCompletionSource<bool>();

            void SnapshotTaken(object? sender, MediaPlayerSnapshotTakenEventArgs e)
            {
                if (File.Exists(e.Filename))
                    tcs.TrySetResult(true);
            }

            previewPlayer.SnapshotTaken += SnapshotTaken;

            previewPlayer.Play();

            // Poczekaj aż się rozpocznie odtwarzanie
            while (previewPlayer.Length <= 0 && !token.IsCancellationRequested)
                await Task.Delay(50, token);

            if (previewPlayer.Length > 0)
            {
                double pos = time.TotalMilliseconds / previewPlayer.Length;
                previewPlayer.Position = (float)Math.Clamp(pos, 0f, 1f);
            }

            // Daj LibVLC chwilę na wyrenderowanie
            await Task.Delay(200, token);

            previewPlayer.TakeSnapshot(0, tempPath, 320, 180);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000, cts.Token));
            if (completed != tcs.Task)
                return null;

            if (!File.Exists(tempPath))
                return null;

            using var bmp = new Bitmap(tempPath);
            return new Bitmap(bmp);
        }
        catch
        {
            return null;
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }

    /// <summary>
    /// Gets or sets the current playback position value (in milliseconds).
    /// </summary>
    /// <remarks>
    /// Updates the underlying Slider value, refreshes the progress text, and raises the SeekRequested event.
    /// </remarks>
    public double Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                _progressBar.Value = value;
                //_rectangleBufforBar.Margin = new Thickness(_progressBar.ActualWidth / Value, 0, 0, 0);
                UpdateProgressText();
                SeekRequested?.Invoke(this, TimeSpan.FromMilliseconds(value));
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Sets the progress value directly on the underlying Slider control.
    /// </summary>
    /// <param name="value">The value to set (in milliseconds).</param>
    public void SetValue(double value)
    {
        _progressBar.Value = value;
    }

    /// <summary>
    /// Gets or sets the buffering progress value, updating the visual buffer bar width.
    /// </summary>
    /// <remarks>
    /// Scales the buffer bar width based on the actual progress bar width and maximum value.
    /// </remarks>
    public double BufforBarValue
    {
        get => _bufforBarValue;
        set
        {
            if (_bufforBarValue != value)
            {
                _bufforBarValue = value;
                _rectangleBufforBar.Width = (_progressBar.ActualWidth > 0 && _progressBar.Maximum > 0) ? (_bufforBarValue * _progressBar.ActualWidth) / _progressBar.Maximum : 0;
                OnPropertyChanged(nameof(BufforBarValue));
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum value of the progress bar (total milliseconds of media duration).
    /// </summary>
    public double Maximum
    {
        get => _progressBar.Maximum;
        set
        {
            _progressBar.Maximum = value;
            OnPropertyChanged(nameof(Maximum));
        }
    }

    /// <summary>
    /// Gets or sets the minimum value of the progress bar (typically 0).
    /// </summary>
    public double Minimum
    {
        get => _progressBar.Minimum;
        set
        {
            _progressBar.Minimum = value;
            OnPropertyChanged(nameof(Minimum));
        }
    }

    /// <summary>
    /// Occurs when a property value changes, enabling data binding notifications in WPF.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Occurs when a seek operation is requested via mouse interaction on the progress bar.
    /// </summary>
    public event EventHandler<TimeSpan> SeekRequested; // Zdarzenie dla przewijania

    /// <summary>
    /// Raises the PropertyChanged event for the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Generic method to raise PropertyChanged with value comparison (legacy implementation).
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <remarks>
    /// This method performs equality checks before raising the event. Use with caution in .NET 4.8.
    /// </remarks>
    private void OnPropertyChanged<T>(string propertyName, ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  // Fix: 'this' zamiast 'field'
        }
    }

    /// <summary>
    /// Updates the ProgressText property based on the current Value and Duration.
    /// </summary>
    /// <remarks>
    /// Formats the current time and total duration as "HH:MM:SS / HH:MM:SS".
    /// </remarks>
    private void UpdateProgressText()
    {
        TimeSpan currentTime = TimeSpan.FromMilliseconds(_progressBar.Value);
        _sb.Clear().AppendFormat("{0:00}:{1:00}:{2:00}/{3:hh\\:mm\\:ss}",
            currentTime.Hours, currentTime.Minutes, currentTime.Seconds, _duration);
        ProgressText = _sb.ToString();
    }

    /// <summary>
    /// Overrides the MouseEnter event to show the mouse-over indicator and reset the popup.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _popup.IsOpen = false;
        _rectangleMouseOverPoint.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Overrides the MouseLeave event to hide the popup and mouse-over indicator.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _popup.IsOpen = false;
        _rectangleMouseOverPoint.Visibility = Visibility.Hidden;
    }
    /// <summary>
    /// Dispose for cleanup progress bar event
    /// </summary>
    public void Dispose()
    {
        _progressBar.ValueChanged -= ProgressBar_ValueChanged;
        _popup.MouseLeave -= Popup_MouseLeave;
    }
}
