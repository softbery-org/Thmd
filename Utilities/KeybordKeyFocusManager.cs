// Version: 0.0.0.1
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Thmd.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class KeybordKeyFocusManager
    {
        private readonly Window _window;
        private readonly FrameworkElement _mainControl;
        private readonly TimeSpan _timeout;
        private int _pressCount = 0;
        private DispatcherTimer _timer;
        private Key _key;

        /// <summary>
        /// Event triggered on single Escape press.
        /// </summary>
        public event Action? OnSinglePress;
        /// <summary>
        /// Event triggered on double Escape press.
        /// </summary>
        public event Action? OnDoublePress;
        /// <summary>
        /// Event triggered on triple Escape press.
        /// </summary>
        public event Action? OnTriplePress;

        /// <summary>
        /// Color used to highlight the main container on triple Escape press.
        /// </summary>
        public Color HighlightColor { get; set; } = Colors.DeepSkyBlue;
        /// <summary>
        /// Duration of the highlight animation in seconds.
        /// </summary>
        public double HighlightDuration { get; set; } = 0.3;

        /// <summary>
        /// Initializes a new instance of the KeybordKeyFocusManager class.
        /// </summary>
        /// <param name="window">Control window</param>
        /// <param name="mainControl">Main control for focus and subcontrols</param>
        /// <param name="key">Which button the action applies to</param>
        /// <param name="timeoutMs">Treshold time beatween press</param>
        /// <exception cref="ArgumentNullException">Window and control focus exceptions</exception>
        public KeybordKeyFocusManager(Window window, FrameworkElement mainControl, Key key, int timeoutMs = 500)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _mainControl = mainControl ?? throw new ArgumentNullException(nameof(mainControl));
            _timeout = TimeSpan.FromMilliseconds(timeoutMs);
            _key = key;

            _timer = new DispatcherTimer { Interval = _timeout };
            _timer.Tick += (s, e) =>
            {
                _pressCount = 0;
                _timer.Stop();
            };

            _window.PreviewKeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _pressCount++;
                _timer.Stop();
                _timer.Start();

                switch (_pressCount)
                {
                    case 1:
                        ClearFocusFromCurrent();
                        OnSinglePress?.Invoke();
                        break;

                    case 2:
                        ClearFocusFromAll(_mainControl);
                        OnDoublePress?.Invoke();
                        break;

                    case 3:
                        SetFocusToMain();
                        FlashMainContainer();
                        OnTriplePress?.Invoke();
                        _pressCount = 0;
                        break;
                }

                e.Handled = true;
            }
        }

        private void ClearFocusFromCurrent()
        {
            var focused = Keyboard.FocusedElement as UIElement;
            focused?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            Keyboard.ClearFocus();
        }

        private void ClearFocusFromAll(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is IInputElement inputChild && inputChild == Keyboard.FocusedElement)
                    Keyboard.ClearFocus();

                ClearFocusFromAll(child);
            }
        }

        private void SetFocusToMain()
        {
            if (_mainControl is FrameworkElement fe)
            {
                Keyboard.Focus(fe);
                var scope = FocusManager.GetFocusScope(fe);
                FocusManager.SetFocusedElement(scope, fe);
            }
        }

        private void FlashMainContainer()
        {
            if (_mainControl is Panel panel && panel.Background is SolidColorBrush originalBrush)
            {
                var fromColor = originalBrush.Color;
                var toColor = HighlightColor;

                var anim = new ColorAnimation
                {
                    From = fromColor,
                    To = toColor,
                    Duration = TimeSpan.FromSeconds(HighlightDuration / 2),
                    AutoReverse = true,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var brush = new SolidColorBrush(fromColor);
                panel.Background = brush;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        }
    }
}
