// Version: 0.1.11.28
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

using Thmd.Consolas;
using Thmd.Converters;
using Thmd.Images;
using Thmd.Media;
using Thmd.Utilities;

namespace Thmd.Views
{
    /// <summary>
    /// Uniwersalny pasek postępu / suwak – obsługuje:
    /// - pasek postępu wideo (z miniaturkami)
    /// - suwak głośności
    /// - suwak equalizera (pionowy)
    /// </summary>
    public partial class ProgressBarView : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private IPlay _player;
        private Engine _engine = new Engine();
        private CancellationTokenSource _thumbnailCts;

        private const int ThumbnailWidth = 320;
        private const int ThumbnailHeight = 180;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<TimeSpan> SeekRequested;
        public event EventHandler<double> ValueChanged;

        #endregion

        #region Dependency Properties

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressBarView),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ProgressBarView),
                new PropertyMetadata(0.0));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ProgressBarView),
                new PropertyMetadata(100.0));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ProgressBarView),
                new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        public bool IsThumbVisible
        {
            get => (bool)GetValue(IsThumbVisibleProperty);
            set => SetValue(IsThumbVisibleProperty, value);
        }
        public static readonly DependencyProperty IsThumbVisibleProperty =
            DependencyProperty.Register(nameof(IsThumbVisible), typeof(bool), typeof(ProgressBarView),
                new PropertyMetadata(true));

        public string ProgressText
        {
            get => (string)GetValue(ProgressTextProperty);
            set => SetValue(ProgressTextProperty, value);
        }
        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register(nameof(ProgressText), typeof(string), typeof(ProgressBarView),
                new PropertyMetadata("00:00:00 / 00:00:00"));

        public string PopupText
        {
            get => (string)GetValue(PopupTextProperty);
            set => SetValue(PopupTextProperty, value);
        }
        public static readonly DependencyProperty PopupTextProperty =
            DependencyProperty.Register(nameof(PopupText), typeof(string), typeof(ProgressBarView),
                new PropertyMetadata(string.Empty));

        public double BufforBarValue
        {
            get => (double)GetValue(BufforBarValueProperty);
            set => SetValue(BufforBarValueProperty, value);
        }
        public static readonly DependencyProperty BufforBarValueProperty =
            DependencyProperty.Register(nameof(BufforBarValue), typeof(double), typeof(ProgressBarView),
                new PropertyMetadata(0.0, OnBufforBarValueChanged));

        public Visibility BufforBarVisibility
        {
            get => (Visibility)GetValue(BufforBarVisibilityProperty);
            set => SetValue(BufforBarVisibilityProperty, value);
        }
        public static readonly DependencyProperty BufforBarVisibilityProperty =
            DependencyProperty.Register(nameof(BufforBarVisibility), typeof(Visibility), typeof(ProgressBarView),
                new PropertyMetadata(Visibility.Collapsed));

        #endregion

        #region Constructor

        public ProgressBarView()
        {
            InitializeComponent();

            Loaded += (s, e) => UpdateLayoutBasedOnOrientation();

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseLeave += OnMouseLeave;
        }

        public ProgressBarView(IPlay player) : this()
        {
            SetPlayer(player);
        }

        #endregion

        #region Public API

        public void SetPlayer(IPlay player)
        {
            _player = player;
        }

        #endregion

        #region Dependency Callbacks

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = (ProgressBarView)d;
            pb.UpdateProgressIndicator();
            pb.UpdateProgressText();
            //pb.OnPropertyChanged(nameof(Value));
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = (ProgressBarView)d;
            pb.UpdateLayoutBasedOnOrientation();
        }

        private static void OnBufforBarValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = (ProgressBarView)d;
            pb.UpdateBufforBar();
        }
        #endregion

        #region Layout Updates

        private void UpdateLayoutBasedOnOrientation()
        {
            // Przełączanie orientacji wewnętrznego ProgressBar
            _progressBar.Orientation = Orientation;

            if (Orientation == Orientation.Vertical)
            {
                _rectangleBufforBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                _rectangleBufforBar.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                _rectangleBufforBar.HorizontalAlignment = HorizontalAlignment.Left;
                _rectangleBufforBar.VerticalAlignment = VerticalAlignment.Stretch;
            }

            UpdateProgressIndicator();
            UpdateBufforBar();
        }

        private void UpdateProgressIndicator()
        {
            if (Maximum <= Minimum) return;

            var percentage = (Value - Minimum) / (Maximum - Minimum);
            _progressBar.Value = percentage * _progressBar.Maximum;
        }

        private void UpdateBufforBar()
        {
            if (_progressBar.ActualWidth <= 0 && _progressBar.ActualHeight <= 0) return;

            var percentage = BufforBarValue / 100.0;

            if (Orientation == Orientation.Horizontal)
            {
                _rectangleBufforBar.Width = percentage * _progressBar.ActualWidth;
                _rectangleBufforBar.Height = double.NaN;
            }
            else
            {
                _rectangleBufforBar.Height = percentage * _progressBar.ActualHeight;
                _rectangleBufforBar.Width = double.NaN;
            }
        }

        private void UpdateProgressText()
        {
            if (_player != null && Maximum > 1000)
            {
                var current = TimeSpan.FromMilliseconds(Value);
                var total = TimeSpan.FromMilliseconds(Maximum);
                ProgressText = total.TotalHours >= 1
                    ? $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss}"
                    : $"{current:mm\\:ss} / {total:mm\\:ss}";
            }
        }

        #endregion

        #region Mouse Interaction

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CaptureMouse();
            UpdateValueFromPosition(e.GetPosition(this));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                UpdateValueFromPosition(e.GetPosition(this));
            }
            else if (_player != null)
            {
                ShowPopup(e.GetPosition(this));
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            _popup.IsOpen = false;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _popup.IsOpen = false;
            _rectangleMouseOverPoint.Visibility = Visibility.Collapsed;
        }

        private void UpdateValueFromPosition(System.Windows.Point point)
        {
            if (ActualWidth <= 0 && ActualHeight <= 0) return;

            double percentage;

            if (Orientation == Orientation.Horizontal)
            {
                percentage = point.X / ActualWidth;
            }
            else
            {
                percentage = 1 - (point.Y / ActualHeight);
            }

            percentage = Math.Clamp(percentage, 0, 1);

            // Oblicz nową wartość w milisekundach
            double newValue = Minimum + percentage * (Maximum - Minimum);

            // Ustaw Value (aktualizuje wewnętrzny ProgressBar)
            Value = newValue;

            ValueChanged?.Invoke(this, newValue);
        }

        private void ShowPopup(System.Windows.Point point)
        {
            if (_player?.Playlist.Current == null || Maximum <= 1000) return; // tylko dla wideo

            double percentage = Orientation == Orientation.Horizontal
                ? point.X / ActualWidth
                : 1 - (point.Y / ActualHeight);

            percentage = Math.Clamp(percentage, 0, 1);

            PopupText = $"{percentage}";

            // Pozycjonowanie wskaźnika i popupu
            if (Orientation == Orientation.Horizontal)
            {
                _rectangleMouseOverPoint.Margin = new Thickness(point.X - 1.5, 0, 0, 0);
                _popup.HorizontalOffset = point.X - (_popup.ActualWidth / 2);
                _popup.VerticalOffset = -10;
            }
            else
            {
                _rectangleMouseOverPoint.Margin = new Thickness(0, point.Y - 1.5, 0, 0);
                _popup.VerticalOffset = point.Y - (_popup.ActualHeight / 2);
                _popup.HorizontalOffset = ActualWidth + 10;
            }

            _rectangleMouseOverPoint.Visibility = Visibility.Visible;
            _popup.IsOpen = true;
        }

        #endregion

        #region PropertyChanged

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
