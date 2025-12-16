// Version: 0.3.11.8
// File: ElementInteractionHelper.cs
// Utworzono na podstawie ControlControllerHelper.cs z rozszerzoną funkcjonalnością
// Dodano: resize w wielu kierunkach, snap-to-grid, ograniczenia do okna/rodzica, pooling stanu, per-element overrides, globalne ustawienia
// Obsługa Canvas (Canvas.Left/Top) oraz elementów bazujących na Margin
//
/* 
 * Jak używać(krótkie przykłady):
 * Programowo:
 * // Attach
 * ControlControllerHelper.Attach(myControl);
 * // Detach
 * ControlControllerHelper.Detach(myControl);
 * 
 * 
 * XAML(attached property):
 * < Window xmlns: thmd = "clr-namespace:Thmd.Utilities;assembly=YourAssembly" >
 *   < Grid >
 *     < Border thmd: ControlControllerHelper.Enable = "True"
 *             Width = "200" Height = "120"
 *             Background = "LightGray" />
 *   </ Grid >
 * </ Window >
 * 
 * Jeśli chcesz Behavior(opcjonalnie):
 * Zainstaluj NuGet: Microsoft.Xaml.Behaviors.Wpf
 * Odkomentuj #define BEHAVIOR w pliku lub skompiluj z tym symboliem.
 * 
 * Użyj:
 * 
 * < Border >
 *   < i:Interaction.Behaviors >
 *     < thmd:ControlControllerHelper.DraggableBehavior />
 *   </ i:Interaction.Behaviors >
 * </ Border >
 * 
 * Dalsze uwagi / optymalizacje które wprowadziłem:
 * Stany są pooled (ConcurrentStack) — minimalna alokacja przy wielokrotnym attach/detach.
 * Direction lookup zrobiony z prostymi boolami — szybkie.
 * Minimalne użycie VisualTreeHelper: tylko gdy trzeba znaleźć Canvas przodka.
 * Per-element overrides za pomocą AttachedProperties (łatwe w XAML).
 * Z-index bring-to-front bez destrukcji (przywracamy oryginalny indeks po puszczeniu).
 * Snap-to-grid jest skonfigurowalny globalnie lub per-element.
 * Constraints: parent panel or window (konfigurowalne).*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

#if BEHAVIOR
// Jeśli chcesz używać Behavior w XAML, doinstaluj pakiet NuGet:
// Microsoft.Xaml.Behaviors.Wpf
using Microsoft.Xaml.Behaviors;
#endif

namespace Thmd.Utilities
{
    /// <summary>
    /// <para>
    /// Statyczny helper umożliwiający przeciąganie i zmianę rozmiaru elementów FrameworkElement.
    /// Można przypiąć do elementu programowo (Attach/Detach) lub przez właściwość attached w XAML.
    /// </para>
    /// <para>
    /// Funkcjonalności: drag, resize (wielokierunkowy), bring-to-front, snap-to-grid, ograniczenia do kontenera/okna,
    /// obsługa Canvas (Canvas.Left/Top) oraz elementów bazujących na Margin, per-element override ustawień,
    /// globalne ustawienia, pooling stanu i optymalizacje zmniejszające alokacje.
    /// </para>
    /// </summary>
    public static class ElementCanvasInteractionHelper
    {
        #region Public types

        /// <summary>
        /// Flagi określające dozwolone kierunki zmiany rozmiaru i przemieszczania.
        /// </summary>
        [Flags]
        public enum DirectionFlags : byte
        {
            /// <summary>
            /// Brak dozwolonych kierunków.
            /// </summary>
            None = 0,
            /// <summary>
            /// Dozwolony resize z lewej krawędzi.
            /// </summary>
            Left = 1 << 0,
            /// <summary>
            /// Dozwolony resize z prawej krawędzi.
            /// </summary>
            Right = 1 << 1,
            /// <summary>
            /// Dozwolony resize z górnej krawędzi.
            /// </summary>
            Top = 1 << 2,
            /// <summary>
            /// Dozwolony resize z dolnej krawędzi.
            /// </summary>
            Bottom = 1 << 3,
            /// <summary>
            /// Dozwolony resize z narożników (kombinacje powyższych).
            /// </summary>
            TopLeft = Top | Left,
            /// <summary>
            /// Dozwolony resize z narożników (kombinacje powyższych).
            /// </summary>
            TopRight = Top | Right,
            /// <summary>
            /// Dozwolony resize z narożników (kombinacje powyższych).
            /// </summary>
            BottomLeft = Bottom | Left,
            /// <summary>
            /// Dozwolony resize z narożników (kombinacje powyższych).
            /// </summary>
            BottomRight = Bottom | Right,
            /// <summary>
            /// Dozwolone przemieszczanie (drag).
            /// </summary>
            Moving = 1 << 4,
            /// <summary>
            /// Wszystkie kierunki resize.
            /// </summary>
            AllResize = Left | Right | Top | Bottom,
            /// <summary>
            /// Wszystkie kierunki (resize + moving).
            /// </summary>
            All = AllResize | Moving
        }

        /// <summary>
        /// Gdzie stosować ograniczenia ruchu (do okna, do rodzica lub brak ograniczeń).
        /// </summary>
        public enum ConstraintTarget
        {
            /// <summary>Ograniczenia względem aktualnego okna (Window).</summary>
            Window,
            /// <summary>Ograniczenia względem bezpośredniego kontenera (Panel).</summary>
            Parent,
            /// <summary>Bez ograniczeń.</summary>
            None
        }

        /// <summary>
        /// Domyślne ustawienia zachowania. Można je modyfikować globalnie lub nadpisać per-element.
        /// </summary>
        public sealed class Settings
        {
            /// <summary>Grubość obszaru resize (w pikselach).</summary>
            public double ResizeBorderWidth { get; set; } = 4.0;

            /// <summary>Domyślnie dozwolone kierunki (flagi).</summary>
            public DirectionFlags AllowedDirections { get; set; } = DirectionFlags.All;

            /// <summary>Opacity przy przeciąganiu (1.0 = brak zmiany).</summary>
            public double DragOpacity { get; set; } = 0.85;

            /// <summary>Opacity przy zmianie rozmiaru (1.0 = brak zmiany).</summary>
            public double ResizeOpacity { get; set; } = 0.9;

            /// <summary>Czy na czas przeciągania ustawiać element na wierzch (Z-index).</summary>
            public bool BringToFrontOnDrag { get; set; } = true;

            /// <summary>Domyślne miejsce ograniczeń (Window / Parent / None).</summary>
            public ConstraintTarget Constraint { get; set; } = ConstraintTarget.Parent;

            /// <summary>Domyślny snap-to-grid (1 = brak snapu, 10 = snap co 10px).</summary>
            public double SnapGrid { get; set; } = 1.0;

            /// <summary>Domyślny minimalny rozmiar szerokości.</summary>
            public double GlobalMinWidth { get; set; } = 20.0;

            /// <summary>Domyślny minimalny rozmiar wysokości.</summary>
            public double GlobalMinHeight { get; set; } = 20.0;

            /// <summary>Domyślny maksymalny rozmiar szerokości.</summary>
            public double GlobalMaxWidth { get; set; } = double.PositiveInfinity;

            /// <summary>Domyślny maksymalny rozmiar wysokości.</summary>
            public double GlobalMaxHeight { get; set; } = double.PositiveInfinity;
        }

        #endregion

        #region Internal State & Pool

        // Stan przypisany per element (wynajmowany z puli)
        private sealed class State
        {
            public bool IsResizing;
            public bool IsMoving;
            public bool IsMouseCaptured;
            public DirectionFlags ActiveDirection;
            public Point LastMousePosWindow;
            public Thickness OriginalMargin;
            public double OriginalLeft;
            public double OriginalTop;
            public double OriginalWidth;
            public double OriginalHeight;
            public bool UsingCanvas;
            public int OriginalZ;
        }

        // Pool stanów do reużycia (thread-safe)
        private static readonly ConcurrentStack<State> _statePool = new();

        private static State RentState()
        {
            return _statePool.TryPop(out var s) ? s : new State();
        }

        private static void ReturnState(State s)
        {
            s.IsResizing = false;
            s.IsMoving = false;
            s.IsMouseCaptured = false;
            s.ActiveDirection = DirectionFlags.None;
            s.LastMousePosWindow = default;
            s.OriginalMargin = default;
            s.OriginalLeft = s.OriginalTop = s.OriginalWidth = s.OriginalHeight = 0;
            s.UsingCanvas = false;
            s.OriginalZ = 0;
            _statePool.Push(s);
        }

        // Mapa element -> state (przechowujemy tylko aktywne attachy)
        private static readonly Dictionary<FrameworkElement, State> _states = new(capacity: 64);

        /// <summary>Domyślne, globalne ustawienia, które można modyfikować.</summary>
        public static Settings DefaultSettings { get; } = new Settings();

        #endregion

        #region Attached Properties (XAML friendly)

        /// <summary>Włącza/wyłącza helper dla elementu (można ustawić w XAML).</summary>
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(false, OnEnableChanged));
        /// <summary>
        /// Ustawia wartość Enable dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetEnable(DependencyObject d, bool v) => d.SetValue(EnableProperty, v);
        /// <summary>
        /// Pobiera wartość Enable dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool GetEnable(DependencyObject d) => (bool)d.GetValue(EnableProperty);

        /// <summary>Per-element: dozwolone kierunki (nadpisuje DefaultSettings).</summary>
        public static readonly DependencyProperty AllowedDirectionsProperty =
            DependencyProperty.RegisterAttached(
                "AllowedDirections",
                typeof(DirectionFlags?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość AllowedDirections dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetAllowedDirections(DependencyObject d, DirectionFlags? v) => d.SetValue(AllowedDirectionsProperty, v);
        /// <summary>
        /// Pobiera wartość AllowedDirections dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static DirectionFlags? GetAllowedDirections(DependencyObject d) => (DirectionFlags?)d.GetValue(AllowedDirectionsProperty);

        /// <summary>Per-element: gdzie stosować ograniczenia (Window/Parent/None).</summary>
        public static readonly DependencyProperty ConstraintProperty =
            DependencyProperty.RegisterAttached(
                "Constraint",
                typeof(ConstraintTarget?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość Constraint dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetConstraint(DependencyObject d, ConstraintTarget? v) => d.SetValue(ConstraintProperty, v);
        /// <summary>
        /// Pobiera wartość Constraint dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static ConstraintTarget? GetConstraint(DependencyObject d) => (ConstraintTarget?)d.GetValue(ConstraintProperty);

        /// <summary>Per-element: snap-to-grid (nadpisuje DefaultSettings.SnapGrid).</summary>
        public static readonly DependencyProperty SnapGridProperty =
            DependencyProperty.RegisterAttached(
                "SnapGrid",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość SnapGrid dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetSnapGrid(DependencyObject d, double? v) => d.SetValue(SnapGridProperty, v);
        /// <summary>
        /// Pobiera wartość SnapGrid dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetSnapGrid(DependencyObject d) => (double?)d.GetValue(SnapGridProperty);

        /// <summary>Per-element: opacity podczas przeciągania.</summary>
        public static readonly DependencyProperty DragOpacityProperty =
            DependencyProperty.RegisterAttached(
                "DragOpacity",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość DragOpacity dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetDragOpacity(DependencyObject d, double? v) => d.SetValue(DragOpacityProperty, v);
        /// <summary>
        /// Pobiera wartość DragOpacity dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetDragOpacity(DependencyObject d) => (double?)d.GetValue(DragOpacityProperty);

        /// <summary>Per-element: opacity podczas resize.</summary>
        public static readonly DependencyProperty ResizeOpacityProperty =
            DependencyProperty.RegisterAttached(
                "ResizeOpacity",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość ResizeOpacity dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetResizeOpacity(DependencyObject d, double? v) => d.SetValue(ResizeOpacityProperty, v);
        /// <summary>
        /// Pobiera wartość ResizeOpacity dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetResizeOpacity(DependencyObject d) => (double?)d.GetValue(ResizeOpacityProperty);

        /// <summary>Per-element: czy ustawić element na wierzch przy przeciąganiu.</summary>
        public static readonly DependencyProperty BringToFrontProperty =
            DependencyProperty.RegisterAttached(
                "BringToFront",
                typeof(bool?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość BringToFront dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetBringToFront(DependencyObject d, bool? v) => d.SetValue(BringToFrontProperty, v);
        /// <summary>
        /// Pobiera wartość BringToFront dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool? GetBringToFront(DependencyObject d) => (bool?)d.GetValue(BringToFrontProperty);

        /// <summary>Per-element: minimalna szerokość (nadpisanie globalnego).</summary>
        public static readonly DependencyProperty MinWidthOverrideProperty =
            DependencyProperty.RegisterAttached(
                "MinWidthOverride",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość MinWidthOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetMinWidthOverride(DependencyObject d, double? v) => d.SetValue(MinWidthOverrideProperty, v);
        /// <summary>
        /// Pobiera wartość MinWidthOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetMinWidthOverride(DependencyObject d) => (double?)d.GetValue(MinWidthOverrideProperty);

        /// <summary>Per-element: minimalna wysokość (nadpisanie globalnego).</summary>
        public static readonly DependencyProperty MinHeightOverrideProperty =
            DependencyProperty.RegisterAttached(
                "MinHeightOverride",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość MinHeightOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetMinHeightOverride(DependencyObject d, double? v) => d.SetValue(MinHeightOverrideProperty, v);
        /// <summary>
        /// Pobiera wartość MinHeightOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetMinHeightOverride(DependencyObject d) => (double?)d.GetValue(MinHeightOverrideProperty);

        /// <summary>Per-element: maksymalna szerokość (nadpisanie globalnego).</summary>
        public static readonly DependencyProperty MaxWidthOverrideProperty =
            DependencyProperty.RegisterAttached(
                "MaxWidthOverride",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość MaxWidthOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetMaxWidthOverride(DependencyObject d, double? v) => d.SetValue(MaxWidthOverrideProperty, v);
        /// <summary>
        /// Pobiera wartość MaxWidthOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetMaxWidthOverride(DependencyObject d) => (double?)d.GetValue(MaxWidthOverrideProperty);

        /// <summary>Per-element: maksymalna wysokość (nadpisanie globalnego).</summary>
        public static readonly DependencyProperty MaxHeightOverrideProperty =
            DependencyProperty.RegisterAttached(
                "MaxHeightOverride",
                typeof(double?),
                typeof(ElementCanvasInteractionHelper),
                new PropertyMetadata(null));
        /// <summary>
        /// Ustawia wartość MaxHeightOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="v"></param>
        public static void SetMaxHeightOverride(DependencyObject d, double? v) => d.SetValue(MaxHeightOverrideProperty, v);
        /// <summary>
        /// Pobiera wartość MaxHeightOverride dla elementu.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double? GetMaxHeightOverride(DependencyObject d) => (double?)d.GetValue(MaxHeightOverrideProperty);

        #endregion

        /// <summary>
        /// Włącza przesuwanie elementu w obrębie parentCanvas
        /// </summary>
        public static void MakeDraggable(UIElement element, Canvas parentCanvas)
        {
            if (element == null || parentCanvas == null)
                return;

            Point lastPosition = new();

            element.MouseLeftButtonDown += (s, e) =>
            {
                lastPosition = e.GetPosition(parentCanvas);
                element.CaptureMouse();
                e.Handled = true;
            };

            element.MouseMove += (s, e) =>
            {
                if (!element.IsMouseCaptured) return;

                Point currentPosition = e.GetPosition(parentCanvas);
                double dx = currentPosition.X - lastPosition.X;
                double dy = currentPosition.Y - lastPosition.Y;

                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);

                Canvas.SetLeft(element, left + dx);
                Canvas.SetTop(element, top + dy);

                lastPosition = currentPosition;
            };

            element.MouseLeftButtonUp += (s, e) =>
            {
                if (element.IsMouseCaptured)
                    element.ReleaseMouseCapture();
            };
        }

        #region Attach/Detach API

        /// <summary>
        /// <para>Przypina helper do elementu programowo (podłącza eventy).</para>
        /// </summary>
        /// <param name="element">Element, do którego podpinamy helper.</param>
        /// <exception cref="ArgumentNullException">Jeśli element jest null.</exception>
        public static void Attach(FrameworkElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (_states.ContainsKey(element)) return;

            var s = RentState();
            _states[element] = s;

            element.PreviewMouseLeftButtonDown += Element_PreviewMouseLeftButtonDown;
            element.MouseMove += Element_MouseMove;
            element.PreviewMouseLeftButtonUp += Element_PreviewMouseLeftButtonUp;
            element.IsHitTestVisible = true;
        }

        /// <summary>
        /// <para>Odłącza helper od elementu i zwalnia stan do puli.</para>
        /// </summary>
        /// <param name="element">Element, który odpinamy.</param>
        public static void Detach(FrameworkElement element)
        {
            if (element == null) return;
            if (!_states.TryGetValue(element, out var s)) return;

            element.PreviewMouseLeftButtonDown -= Element_PreviewMouseLeftButtonDown;
            element.MouseMove -= Element_MouseMove;
            element.PreviewMouseLeftButtonUp -= Element_PreviewMouseLeftButtonUp;

            _states.Remove(element);
            ReturnState(s);
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;
            if ((bool)e.NewValue) Attach(fe); else Detach(fe);
        }

        #endregion

        #region Event handlers (core logic)

        private static void Element_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            if (!_states.TryGetValue(el, out var state)) return;

            var settings = GetEffectiveSettings(el);
            var localPt = e.GetPosition(el);

            var dir = ComputeDirection(el, localPt, settings.ResizeBorderWidth, settings.AllowedDirections);
            if (dir == DirectionFlags.None) return;

            var window = GetParentWindow(el);
            state.LastMousePosWindow = e.GetPosition(window ?? (IInputElement)el);
            state.OriginalWidth = el.ActualWidth;
            state.OriginalHeight = el.ActualHeight;
            state.OriginalMargin = el.Margin;

            state.UsingCanvas = TryGetCanvasParent(el, out _);
            if (state.UsingCanvas)
            {
                state.OriginalLeft = Canvas.GetLeft(el);
                state.OriginalTop = Canvas.GetTop(el);
                if (double.IsNaN(state.OriginalLeft)) state.OriginalLeft = 0;
                if (double.IsNaN(state.OriginalTop)) state.OriginalTop = 0;
            }

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

        private static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            if (!_states.TryGetValue(el, out var state)) return;

            var settings = GetEffectiveSettings(el);

            if (!state.IsMouseCaptured)
            {
                var local = e.GetPosition(el);
                var dir = ComputeDirection(el, local, settings.ResizeBorderWidth, settings.AllowedDirections);
                UpdateCursor(el, dir);
                return;
            }

            var window = GetParentWindow(el);
            if (window == null) return;
            var current = e.GetPosition(window);

            if (state.IsMoving)
            {
                HandleMove(el, state, current, settings);
            }
            else if (state.IsResizing)
            {
                HandleResize(el, state, current, settings);
            }

            e.Handled = true;
        }

        private static void Element_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            if (!_states.TryGetValue(el, out var state)) return;

            if (state.IsResizing || state.IsMoving)
            {
                var settings = GetEffectiveSettings(el);
                if (state.IsMoving && settings.DragOpacity < 1.0) el.Opacity = 1.0;
                if (state.IsResizing && settings.ResizeOpacity < 1.0) el.Opacity = 1.0;
                if (settings.BringToFrontOnDrag)
                {
                    Panel.SetZIndex(el, state.OriginalZ);
                }

                state.IsResizing = false;
                state.IsMoving = false;
                state.IsMouseCaptured = false;
                state.ActiveDirection = DirectionFlags.None;

                el.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion

        #region Core move/resize logic

        private static void HandleMove(FrameworkElement el, State state, Point currentWindow, Settings settings)
        {
            double dx = currentWindow.X - state.LastMousePosWindow.X;
            double dy = currentWindow.Y - state.LastMousePosWindow.Y;
            // aktualizujemy LastMouse tak, aby następne przesunięcie było relatywne
            state.LastMousePosWindow = currentWindow;

            if (state.UsingCanvas)
            {
                double left = state.OriginalLeft + dx;
                double top = state.OriginalTop + dy;

                var snap = GetElementSnap(el, settings);
                if (snap > 1.0)
                {
                    left = SnapValue(left, snap);
                    top = SnapValue(top, snap);
                }

                ApplyConstraintsForCanvas(el, ref left, ref top, state, settings);

                Canvas.SetLeft(el, left);
                Canvas.SetTop(el, top);

                // aktualizujemy oryginały, by kolejne kroki były relatywne
                state.OriginalLeft = left;
                state.OriginalTop = top;
            }
            else
            {
                // uaktualniamy margin na podstawie poprzednich oryginałów
                var margin = state.OriginalMargin;
                margin.Left = state.OriginalMargin.Left + dx;
                margin.Top = state.OriginalMargin.Top + dy;

                var snap = GetElementSnap(el, settings);
                if (snap > 1.0)
                {
                    margin.Left = SnapValue(margin.Left, snap);
                    margin.Top = SnapValue(margin.Top, snap);
                }

                ApplyConstraintsForMargin(el, ref margin, settings);

                el.Margin = margin;
                // zapisz nowy margin jako baza dla dalszych kroków
                state.OriginalMargin = margin;
            }
        }

        private static void HandleResize(FrameworkElement el, State state, Point currentWindow, Settings settings)
        {
            double dx = currentWindow.X - state.LastMousePosWindow.X;
            double dy = currentWindow.Y - state.LastMousePosWindow.Y;
            state.LastMousePosWindow = currentWindow;

            double newW = state.OriginalWidth;
            double newH = state.OriginalHeight;
            var margin = state.OriginalMargin;

            if ((state.ActiveDirection & DirectionFlags.Right) != 0)
            {
                newW = System.Math.Max(settings.GlobalMinWidth, System.Math.Min(settings.GlobalMaxWidth, newW + dx));
            }
            if ((state.ActiveDirection & DirectionFlags.Bottom) != 0)
            {
                newH = System.Math.Max(settings.GlobalMinHeight, System.Math.Min(settings.GlobalMaxHeight, newH + dy));
            }
            if ((state.ActiveDirection & DirectionFlags.Left) != 0)
            {
                margin.Left = System.Math.Max(0, margin.Left + dx);
                newW = System.Math.Max(settings.GlobalMinWidth, System.Math.Min(settings.GlobalMaxWidth, newW - dx));
            }
            if ((state.ActiveDirection & DirectionFlags.Top) != 0)
            {
                margin.Top = System.Math.Max(0, margin.Top + dy);
                newH = System.Math.Max(settings.GlobalMinHeight, System.Math.Min(settings.GlobalMaxHeight, newH - dy));
            }

            // per-element overrides dla min/max
            var minW = GetMinWidthOverride(el) ?? settings.GlobalMinWidth;
            var minH = GetMinHeightOverride(el) ?? settings.GlobalMinHeight;
            var maxW = GetMaxWidthOverride(el) ?? settings.GlobalMaxWidth;
            var maxH = GetMaxHeightOverride(el) ?? settings.GlobalMaxHeight;

            newW = System.Math.Max(minW, System.Math.Min(maxW, newW));
            newH = System.Math.Max(minH, System.Math.Min(maxH, newH));

            if (state.UsingCanvas)
            {
                if (TryGetCanvasParent(el, out var canvasParent))
                {
                    double left = margin.Left;
                    double top = margin.Top;
                    double canvasW = canvasParent.ActualWidth;
                    double canvasH = canvasParent.ActualHeight;
                    newW = System.Math.Min(newW, canvasW - left);
                    newH = System.Math.Min(newH, canvasH - top);
                }
            }

            var snap = GetElementSnap(el, settings);
            if (snap > 1.0)
            {
                newW = SnapValue(newW, snap);
                newH = SnapValue(newH, snap);
            }

            el.Margin = margin;
            el.Width = newW;
            el.Height = newH;

            state.OriginalWidth = newW;
            state.OriginalHeight = newH;
            state.OriginalMargin = margin;
        }

        #endregion

        #region Helpers: direction, cursor, constraints, z-index, snap

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

            result &= allowed;
            return result;
        }

        private static void UpdateCursor(FrameworkElement el, DirectionFlags dir)
        {
            Cursor cur = dir switch
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

            el.Cursor = cur;
        }

        private static double SnapValue(double v, double grid)
        {
            if (grid <= 1.0) return v;
            return System.Math.Round(v / grid) * grid;
        }

        private static double GetElementSnap(FrameworkElement el, Settings settings)
        {
            var s = GetSnapGrid(el);
            return s ?? settings.SnapGrid;
        }

        private static int GetTopZ(UIElement within)
        {
            if (within is FrameworkElement fe && fe.Parent is Panel parent)
            {
                int top = int.MinValue;
                foreach (UIElement child in parent.Children)
                {
                    top = System.Math.Max(top, Panel.GetZIndex(child));
                }
                return top == int.MinValue ? 0 : top;
            }
            return 0;
        }

        #endregion

        #region Constraint logic

        private static void ApplyConstraintsForCanvas(FrameworkElement el, ref double left, ref double top, State state, Settings settings)
        {
            var constraint = GetConstraint(el) ?? settings.Constraint;
            if (constraint == ConstraintTarget.None) return;

            if (TryGetCanvasParent(el, out var canvasParent))
            {
                double parentW = canvasParent.ActualWidth;
                double parentH = canvasParent.ActualHeight;
                if (constraint == ConstraintTarget.Window)
                {
                    var w = GetParentWindow(el);
                    if (w != null) { parentW = w.ActualWidth; parentH = w.ActualHeight; }
                }

                left = System.Math.Max(0, System.Math.Min(parentW - (el.ActualWidth), left));
                top = System.Math.Max(0, System.Math.Min(parentH - (el.ActualHeight), top));
            }
            else
            {
                var w = GetParentWindow(el);
                if (w != null)
                {
                    left = System.Math.Max(0, System.Math.Min(w.ActualWidth - el.ActualWidth, left));
                    top = System.Math.Max(0, System.Math.Min(w.ActualHeight - el.ActualHeight, top));
                }
            }
        }

        private static void ApplyConstraintsForMargin(FrameworkElement el, ref Thickness margin, Settings settings)
        {
            var constraint = GetConstraint(el) ?? settings.Constraint;
            if (constraint == ConstraintTarget.None) return;

            FrameworkElement parentFe = el.Parent as FrameworkElement;
            if (constraint == ConstraintTarget.Parent && parentFe != null)
            {
                margin.Left = System.Math.Max(0, System.Math.Min(parentFe.ActualWidth - el.ActualWidth, margin.Left));
                margin.Top = System.Math.Max(0, System.Math.Min(parentFe.ActualHeight - el.ActualHeight, margin.Top));
            }
            else if (constraint == ConstraintTarget.Window)
            {
                var w = GetParentWindow(el);
                if (w != null)
                {
                    margin.Left = System.Math.Max(0, System.Math.Min(w.ActualWidth - el.ActualWidth, margin.Left));
                    margin.Top = System.Math.Max(0, System.Math.Min(w.ActualHeight - el.ActualHeight, margin.Top));
                }
            }
        }

        #endregion

        #region Utility

        private static bool TryGetCanvasParent(FrameworkElement el, out Canvas canvas)
        {
            if (el.Parent is Canvas c) { canvas = c; return true; }
            var parent = VisualTreeHelper.GetParent(el) as FrameworkElement;
            while (parent != null && !(parent is Canvas))
            {
                parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
            }
            canvas = parent as Canvas;
            return canvas != null;
        }

        private static Window GetParentWindow(FrameworkElement el)
        {
            return Window.GetWindow(el);
        }

        private static Settings GetEffectiveSettings(FrameworkElement el)
        {
            var s = DefaultSettings;
            var copy = new Settings
            {
                ResizeBorderWidth = s.ResizeBorderWidth,
                AllowedDirections = s.AllowedDirections,
                DragOpacity = s.DragOpacity,
                ResizeOpacity = s.ResizeOpacity,
                BringToFrontOnDrag = s.BringToFrontOnDrag,
                Constraint = s.Constraint,
                SnapGrid = s.SnapGrid,
                GlobalMinWidth = s.GlobalMinWidth,
                GlobalMinHeight = s.GlobalMinHeight,
                GlobalMaxWidth = s.GlobalMaxWidth,
                GlobalMaxHeight = s.GlobalMaxHeight
            };

            var allowed = GetAllowedDirections(el);
            if (allowed.HasValue) copy.AllowedDirections = allowed.Value;

            var constr = GetConstraint(el);
            if (constr.HasValue) copy.Constraint = constr.Value;

            var sg = GetSnapGrid(el);
            if (sg.HasValue) copy.SnapGrid = sg.Value;

            var dp = GetDragOpacity(el);
            if (dp.HasValue) copy.DragOpacity = dp.Value;

            var rp = GetResizeOpacity(el);
            if (rp.HasValue) copy.ResizeOpacity = rp.Value;

            var btf = GetBringToFront(el);
            if (btf.HasValue) copy.BringToFrontOnDrag = btf.Value;

            return copy;
        }

        #endregion

        #region Per-element min/max helpers

        private static double? GetMinWidthOverride(FrameworkElement el) => (double?)el.GetValue(MinWidthOverrideProperty);
        private static double? GetMinHeightOverride(FrameworkElement el) => (double?)el.GetValue(MinHeightOverrideProperty);
        private static double? GetMaxWidthOverride(FrameworkElement el) => (double?)el.GetValue(MaxWidthOverrideProperty);
        private static double? GetMaxHeightOverride(FrameworkElement el) => (double?)el.GetValue(MaxHeightOverrideProperty);

        #endregion

#if BEHAVIOR
        #region Opcjonalny Behavior (wymaga Microsoft.Xaml.Behaviors.Wpf)
        /// <summary>
        /// Behavior ułatwiający użycie w XAML poprzez Interaction.Behaviors.
        /// </summary>
        public class DraggableBehavior : Behavior<FrameworkElement>
        {
            protected override void OnAttached()
            {
                base.OnAttached();
                ElementInteractionHelper.Attach(AssociatedObject);
            }

            protected override void OnDetaching()
            {
                base.OnDetaching();
                ElementInteractionHelper.Detach(AssociatedObject);
            }
        }
        #endregion
#endif

    }
}
