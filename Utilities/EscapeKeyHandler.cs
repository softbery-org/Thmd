// Version: 1.0.0.1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Thmd.Utilities
{
    /// <summary>
    /// After double press ESC remove fokus from each controls and set it for specified control.
    /// </summary>
    public static class EscapeKeyHandler
    {
        private static DateTime _lastEscapeTime = DateTime.MinValue;
        private static readonly TimeSpan _doublePressThreshold = TimeSpan.FromMilliseconds(500);
        private static bool _isWaitingForSecondPress = false;

        /// <summary>
        /// Attaches double ESC key press handler to the specified window.
        /// </summary>
        /// <param name="window">window</param>
        /// <param name="onSinglePress">run single action</param>
        /// <param name="onDoublePress">run single action</param>
        /// <param name="pressThreshold">time in miliseconds to press button</param>

        public static void Attach(Window window, Action onSinglePress, Action onDoublePress, Control control, int pressThreshold = 500)
        {
            if (window == null)
                return;

            window.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    var now = DateTime.Now;
                    var timeSinceLast = now - _lastEscapeTime;

                    if (timeSinceLast < TimeSpan.FromMilliseconds(pressThreshold))
                    {
                        // 🔹 Second ESC — double press
                        _isWaitingForSecondPress = false;
                        onDoublePress?.Invoke();
                    }
                    else
                    {
                        // 🔹 First ESC — one press
                        _isWaitingForSecondPress = true;

                        // Wait a waill — if no second press ESC, find out it's single press
                        window.Dispatcher.BeginInvoke(new Action(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(_doublePressThreshold);
                            if (_isWaitingForSecondPress)
                            {
                                _isWaitingForSecondPress = false;
                                onSinglePress?.Invoke();
                            }
                        }), DispatcherPriority.Input);
                    }

                    _lastEscapeTime = now;
                    e.Handled = true;
                }
            };
        }
    }
}
