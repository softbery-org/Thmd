// Version: 0.0.0.1
// Nowa wersja ElementInteractionHelper dla Grid (Margin-based)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Thmd.Utilities
{
    public static class ElementGridInteractionHelper
    {
        #region Types

        [Flags]
        public enum DirectionFlags : byte
        {
            None = 0, Left = 1 << 0, Right = 1 << 1,
            Top = 1 << 2, Bottom = 1 << 3,
            TopLeft = Top | Left,
            TopRight = Top | Right,
            BottomLeft = Bottom | Left,
            BottomRight = Bottom | Right,
            Moving = 1 << 4,
            AllResize = Left | Right | Top | Bottom,
            All = AllResize | Moving
        }

        public enum ConstraintTarget { Window, Parent, None }

        public sealed class Settings
        {
            public double ResizeBorderWidth { get; set; } = 4.0;
            public DirectionFlags AllowedDirections { get; set; } = DirectionFlags.All;
            public double DragOpacity { get; set; } = 0.85;
            public double ResizeOpacity { get; set; } = 0.9;
            public bool BringToFrontOnDrag { get; set; } = true;
            public ConstraintTarget Constraint { get; set; } = ConstraintTarget.Parent;
            public double SnapGrid { get; set; } = 1.0;
            public double GlobalMinWidth { get; set; } = 20.0;
            public double GlobalMinHeight { get; set; } = 20.0;
            public double GlobalMaxWidth { get; set; } = double.PositiveInfinity;
            public double GlobalMaxHeight { get; set; } = double.PositiveInfinity;
        }

        private sealed class State
        {
            public bool IsResizing, IsMoving, IsMouseCaptured;
            public DirectionFlags ActiveDirection;
            public Point LastMousePosWindow;
            public Thickness OriginalMargin;
            public double OriginalWidth, OriginalHeight;
            public int OriginalZ;
        }

        private static readonly ConcurrentStack<State> _statePool = new();
        private static readonly Dictionary<FrameworkElement, State> _states = new();

        public static Settings DefaultSettings { get; } = new Settings();

        private static State RentState() => _statePool.TryPop(out var s) ? s : new State();
        private static void ReturnState(State s)
        {
            s.IsResizing = s.IsMoving = s.IsMouseCaptured = false;
            s.ActiveDirection = DirectionFlags.None;
            s.LastMousePosWindow = default;
            s.OriginalMargin = default;
            s.OriginalWidth = s.OriginalHeight = 0;
            s.OriginalZ = 0;
            _statePool.Push(s);
        }

        #endregion

        #region Attached Property

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(ElementGridInteractionHelper),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject d, bool v) => d.SetValue(EnableProperty, v);
        public static bool GetEnable(DependencyObject d) => (bool)d.GetValue(EnableProperty);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;
            if ((bool)e.NewValue) Attach(fe); else Detach(fe);
        }

        #endregion

        #region Attach/Detach

        public static void Attach(FrameworkElement el)
        {
            if (el == null) throw new ArgumentNullException(nameof(el));
            if (_states.ContainsKey(el)) return;

            var s = RentState();
            _states[el] = s;

            el.PreviewMouseLeftButtonDown += El_MouseDown;
            el.MouseMove += El_MouseMove;
            el.PreviewMouseLeftButtonUp += El_MouseUp;
            el.IsHitTestVisible = true;
        }

        public static void Detach(FrameworkElement el)
        {
            if (el == null || !_states.TryGetValue(el, out var s)) return;

            el.PreviewMouseLeftButtonDown -= El_MouseDown;
            el.MouseMove -= El_MouseMove;
            el.PreviewMouseLeftButtonUp -= El_MouseUp;

            _states.Remove(el);
            ReturnState(s);
        }

        #endregion

        #region Event Handlers

        private static void El_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el || !_states.TryGetValue(el, out var state)) return;

            var settings = DefaultSettings;
            var localPt = e.GetPosition(el);

            var dir = ComputeDirection(el, localPt, settings.ResizeBorderWidth, settings.AllowedDirections);
            if (dir == DirectionFlags.None) return;

            var window = Window.GetWindow(el);
            state.LastMousePosWindow = e.GetPosition(window ?? (IInputElement)el);
            state.OriginalWidth = el.ActualWidth;
            state.OriginalHeight = el.ActualHeight;
            state.OriginalMargin = el.Margin;

            if (settings.BringToFrontOnDrag)
            {
                state.OriginalZ = Panel.GetZIndex(el);
                Panel.SetZIndex(el, GetTopZ(el) + 1);
            }

            state.ActiveDirection = dir;
            state.IsResizing = (dir & DirectionFlags.AllResize) != 0;
            state.IsMoving = (dir & DirectionFlags.Moving) != 0;
            state.IsMouseCaptured = true;
            el.CaptureMouse();

            if (state.IsMoving && settings.DragOpacity < 1.0) el.Opacity = settings.DragOpacity;
            if (state.IsResizing && settings.ResizeOpacity < 1.0) el.Opacity = settings.ResizeOpacity;

            e.Handled = true;
        }

        private static void El_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement el || !_states.TryGetValue(el, out var state)) return;
            if (!state.IsMouseCaptured)
            {
                var local = e.GetPosition(el);
                UpdateCursor(el, ComputeDirection(el, local, DefaultSettings.ResizeBorderWidth, DefaultSettings.AllowedDirections));
                return;
            }

            var window = Window.GetWindow(el);
            if (window == null) return;

            var current = e.GetPosition(window);
            double dx = current.X - state.LastMousePosWindow.X;
            double dy = current.Y - state.LastMousePosWindow.Y;
            state.LastMousePosWindow = current;

            if (state.IsMoving) HandleMove(el, state, dx, dy);
            if (state.IsResizing) HandleResize(el, state, dx, dy);

            e.Handled = true;
        }

        private static void El_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el || !_states.TryGetValue(el, out var state)) return;
            if (!state.IsMouseCaptured) return;

            if (state.IsMoving && DefaultSettings.DragOpacity < 1.0) el.Opacity = 1.0;
            if (state.IsResizing && DefaultSettings.ResizeOpacity < 1.0) el.Opacity = 1.0;
            if (DefaultSettings.BringToFrontOnDrag) Panel.SetZIndex(el, state.OriginalZ);

            state.IsMoving = state.IsResizing = state.IsMouseCaptured = false;
            state.ActiveDirection = DirectionFlags.None;
            el.ReleaseMouseCapture();
        }

        #endregion

        #region Move / Resize

        private static void HandleMove(FrameworkElement el, State state, double dx, double dy)
        {
            var m = state.OriginalMargin;
            m.Left += dx; m.Top += dy;

            // Snap
            var snap = DefaultSettings.SnapGrid;
            if (snap > 1.0)
            {
                m.Left = System.Math.Round(m.Left / snap) * snap;
                m.Top = System.Math.Round(m.Top / snap) * snap;
            }

            // Constraints
            var parent = el.Parent as FrameworkElement;
            if (parent != null)
            {
                m.Left = System.Math.Max(0, System.Math.Min(parent.ActualWidth - el.ActualWidth, m.Left));
                m.Top = System.Math.Max(0, System.Math.Min(parent.ActualHeight - el.ActualHeight, m.Top));
            }

            el.Margin = m;
            state.OriginalMargin = m;
        }

        private static void HandleResize(FrameworkElement el, State state, double dx, double dy)
        {
            double newW = state.OriginalWidth;
            double newH = state.OriginalHeight;
            var m = state.OriginalMargin;

            if ((state.ActiveDirection & DirectionFlags.Right) != 0) newW += dx;
            if ((state.ActiveDirection & DirectionFlags.Bottom) != 0) newH += dy;
            if ((state.ActiveDirection & DirectionFlags.Left) != 0) { m.Left += dx; newW -= dx; }
            if ((state.ActiveDirection & DirectionFlags.Top) != 0) { m.Top += dy; newH -= dy; }

            newW = System.Math.Max(DefaultSettings.GlobalMinWidth, System.Math.Min(DefaultSettings.GlobalMaxWidth, newW));
            newH = System.Math.Max(DefaultSettings.GlobalMinHeight, System.Math.Min(DefaultSettings.GlobalMaxHeight, newH));

            el.Width = newW;
            el.Height = newH;
            el.Margin = m;

            state.OriginalWidth = newW;
            state.OriginalHeight = newH;
            state.OriginalMargin = m;
        }

        #endregion

        #region Helpers

        private static DirectionFlags ComputeDirection(FrameworkElement el, Point pt, double borderWidth, DirectionFlags allowed)
        {
            bool left = pt.X <= borderWidth;
            bool right = pt.X >= el.ActualWidth - borderWidth;
            bool top = pt.Y <= borderWidth;
            bool bottom = pt.Y >= el.ActualHeight - borderWidth;

            DirectionFlags result = DirectionFlags.None;
            if (top && left) result = DirectionFlags.TopLeft;
            else if (top && right) result = DirectionFlags.TopRight;
            else if (bottom && left) result = DirectionFlags.BottomLeft;
            else if (bottom && right) result = DirectionFlags.BottomRight;
            else if (left) result = DirectionFlags.Left;
            else if (right) result = DirectionFlags.Right;
            else if (top) result = DirectionFlags.Top;
            else if (bottom) result = DirectionFlags.Bottom;
            else result = DirectionFlags.Moving;

            return result & allowed;
        }

        private static void UpdateCursor(FrameworkElement el, DirectionFlags dir)
        {
            el.Cursor = dir switch
            {
                DirectionFlags.TopLeft => Cursors.SizeNWSE,
                DirectionFlags.BottomRight => Cursors.SizeNWSE,
                DirectionFlags.TopRight => Cursors.SizeNESW,
                DirectionFlags.BottomLeft => Cursors.SizeNESW,
                DirectionFlags.Left => Cursors.SizeWE,
                DirectionFlags.Right => Cursors.SizeWE,
                DirectionFlags.Top => Cursors.SizeNS,
                DirectionFlags.Bottom => Cursors.SizeNS,
                DirectionFlags.Moving => Cursors.SizeAll,
                _ => Cursors.Arrow
            };
        }

        private static int GetTopZ(FrameworkElement el)
        {
            if (el.Parent is Panel parent)
            {
                int top = int.MinValue;
                foreach (UIElement c in parent.Children)
                    top = System.Math.Max(top, Panel.GetZIndex(c));
                return top == int.MinValue ? 0 : top;
            }
            return 0;
        }

        #endregion
    }
}
