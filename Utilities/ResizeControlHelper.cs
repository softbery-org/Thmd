// Version: 0.1.10.22
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Thmd.Utilities;

public class ResizeControlHelper
{
    private enum ResizeDirection
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Moving
    }

    private readonly FrameworkElement _element;

    private bool _isResizing;

    private bool _isMoving;

    private Point _lastMousePosition;

    private Rect _originalBounds;

    private readonly double _resizeBorderWidth = 3.0;

    private ResizeDirection _resizeDirection;

    private bool _isMouseCaptured;

    public ResizeControlHelper(FrameworkElement element)
    {
        _element = element ?? throw new ArgumentNullException("element");
        InitializeEvents();
    }

    public Cursor GetCursor()
    {
        return _element.Cursor;
    }

    private void InitializeEvents()
    {
        _element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
        _element.MouseMove += Element_MouseMove;
        _element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
        _element.Cursor = Cursors.Arrow;
    }

    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point position = e.GetPosition(_element);
        ResizeDirection direction = GetResizeDirection(position);
        if (direction != ResizeDirection.None)
        {
            _lastMousePosition = e.GetPosition(GetParentWindow() as UIElement);//GetParentContainer());
            _originalBounds = new Rect(_element.Margin.Left, _element.Margin.Top, _element.ActualWidth, _element.ActualHeight);
            _resizeDirection = direction;
            _isResizing = direction != ResizeDirection.Moving;
            _isMoving = direction == ResizeDirection.Moving;
            _isMouseCaptured = true;
            _element.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Element_MouseMove(object sender, MouseEventArgs e)
    {
        Point position = e.GetPosition(_element);
        ResizeDirection direction = GetResizeDirection(position);
        if (_isMoving)
        {
            MoveElement(e);
        }
        else if (_isResizing)
        {
            ResizeElement(e);
        }
        else
        {
            UpdateCursor(direction);
        }
    }

    private void ResizeElement(MouseEventArgs e)
    {
        Window window = GetParentWindow();
        if (window != null)
        {
            Point currentPosition = e.GetPosition(window);
            double dx = currentPosition.X - _lastMousePosition.X;
            double dy = currentPosition.Y - _lastMousePosition.Y;

            Thickness newMargin = _element.Margin;
            double newWidth = _element.ActualWidth;
            double newHeight = _element.ActualHeight;

            switch (_resizeDirection)
            {
                case ResizeDirection.Right:
                    newWidth += dx;
                    break;
                case ResizeDirection.Bottom:
                    newHeight += dy;
                    break;
                case ResizeDirection.Left:
                    newMargin.Left += dx;
                    newWidth -= dx;
                    break;
                case ResizeDirection.Top:
                    newMargin.Top += dy;
                    newHeight -= dy;
                    break;
                case ResizeDirection.TopLeft:
                    newMargin.Left += dx;
                    newWidth -= dx;
                    newMargin.Top += dy;
                    newHeight -= dy;
                    break;
                case ResizeDirection.TopRight:
                    newWidth += dx;
                    newMargin.Top += dy;
                    newHeight -= dy;
                    break;
                case ResizeDirection.BottomLeft:
                    newMargin.Left += dx;
                    newWidth -= dx;
                    newHeight += dy;
                    break;
                case ResizeDirection.BottomRight:
                    newWidth += dx;
                    newHeight += dy;
                    break;
            }

            // Minimalny rozmiar
            newWidth = Math.Max(newWidth, _element.MinWidth);
            newHeight = Math.Max(newHeight, _element.MinHeight);
            // Granice okna
            double maxWidth = window.ActualWidth - newMargin.Left;
            double maxHeight = window.ActualHeight - newMargin.Top;

            newWidth = Math.Min(newWidth, maxWidth);
            newHeight = Math.Min(newHeight, maxHeight);

            // Nie pozwalamy na ujemne marginesy
            if (newMargin.Left < 0)
            {
                newWidth += newMargin.Left;
                newMargin.Left = 0;
            }
            if (newMargin.Top < 0)
            {
                newHeight += newMargin.Top;
                newMargin.Top = 0;
            }

            _element.Margin = newMargin;
            _element.Width = newWidth;
            _element.Height = newHeight;
            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
    }

    private Window GetParentWindow()
    {
        return Window.GetWindow(_element);
    }

    private void MoveElement(MouseEventArgs e)
    {
        Window window = GetParentWindow();
        if (window != null)
        {
            Point currentPosition = e.GetPosition(window);
            double dx = currentPosition.X - _lastMousePosition.X;
            double dy = currentPosition.Y - _lastMousePosition.Y;

            Thickness newMargin = _element.Margin;

            // Aktualizacja pozycji
            newMargin.Left += dx;
            newMargin.Top += dy;

            // Granice okna
            double maxLeft = window.ActualWidth - _element.ActualWidth;
            double maxTop = window.ActualHeight - _element.ActualHeight;

            newMargin.Left = Math.Max(0, Math.Min(maxLeft, newMargin.Left));
            newMargin.Top = Math.Max(0, Math.Min(maxTop, newMargin.Top));

            _element.Margin = newMargin;
            _lastMousePosition = currentPosition;
            e.Handled = true;
        }
    }

    private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isResizing || _isMoving)
        {
            _isResizing = false;
            _isMoving = false;
            _element.ReleaseMouseCapture();
            _resizeDirection = ResizeDirection.None;
            _isMouseCaptured = false;
            e.Handled = true;
        }
    }

    private ResizeDirection GetResizeDirection(Point point)
    {
        bool left = point.X < _resizeBorderWidth;
        bool right = point.X > _element.ActualWidth - _resizeBorderWidth;
        bool top = point.Y < _resizeBorderWidth;
        bool bottom = point.Y > _element.ActualHeight - _resizeBorderWidth;
        bool center = point.X >= _resizeBorderWidth && point.X <= _element.ActualWidth - _resizeBorderWidth && point.Y >= _resizeBorderWidth && point.Y <= _element.ActualHeight - _resizeBorderWidth;
        if (top && left)
        {
            return ResizeDirection.TopLeft;
        }
        if (top && right)
        {
            return ResizeDirection.TopRight;
        }
        if (bottom && left)
        {
            return ResizeDirection.BottomLeft;
        }
        if (bottom && right)
        {
            return ResizeDirection.BottomRight;
        }
        if (left)
        {
            return ResizeDirection.Left;
        }
        if (right)
        {
            return ResizeDirection.Right;
        }
        if (top)
        {
            return ResizeDirection.Top;
        }
        if (bottom)
        {
            return ResizeDirection.Bottom;
        }
        if (center)
        {
            return ResizeDirection.Moving;
        }
        return ResizeDirection.None;
    }

    private void UpdateCursor(ResizeDirection direction)
    {
        FrameworkElement element = _element;
        Cursor cursor;
        switch (direction)
        {
            case ResizeDirection.TopLeft:
                cursor = Cursors.SizeNWSE;
                break;
            case ResizeDirection.BottomRight:
                cursor = Cursors.SizeNWSE;
                break;
            case ResizeDirection.TopRight:
                cursor = Cursors.SizeNESW;
                break;
            case ResizeDirection.BottomLeft:
                cursor = Cursors.SizeNESW;
                break;
            case ResizeDirection.Left:
                cursor = Cursors.SizeWE;
                break;
            case ResizeDirection.Right:
                cursor = Cursors.SizeWE;
                break;
            case ResizeDirection.Top:
                cursor = Cursors.SizeNS;
                break;
            case ResizeDirection.Bottom:
                cursor = Cursors.SizeNS;
                break;
            default:
                cursor = Cursors.Arrow;
                break;
        }
        element.Cursor = cursor;
    }

    private UIElement GetParentContainer()
    {
        return VisualTreeHelper.GetParent(_element) as UIElement;
    }
}
