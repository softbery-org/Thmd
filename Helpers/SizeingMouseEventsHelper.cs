// Version: 0.1.0.18
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Thmd.Helpers;

public class SizeingMouseEventsHelper
{
	private static FrameworkElement _element;

	private static Point _mouseClickPoint = default;

	private static bool _leftEdge = false;

	private static bool _rightEdge = false;

	private static bool _topEdge = false;

	private static bool _bottomEdge = false;

	private static bool _moving = false;

	public SizeingMouseEventsHelper(FrameworkElement element)
	{
		_element = element;
	}

	public static void OnControlMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (sender != null)
		{
			_element = sender as Control;
			Mouse.SetCursor(Cursors.Arrow);
			_leftEdge = false;
			_rightEdge = false;
			_topEdge = false;
			_bottomEdge = false;
			_moving = false;
			_element.ReleaseMouseCapture();
		}
	}

	public static void OnControlMouseDown(object sender, MouseButtonEventArgs e)
	{
		Control c = sender as Control;
		_mouseClickPoint = e.GetPosition(c);
		if (_mouseClickPoint.X <= c.BorderThickness.Left)
		{
			_leftEdge = true;
		}
		if (_mouseClickPoint.Y <= c.BorderThickness.Top)
		{
			_topEdge = true;
		}
		if (_mouseClickPoint.X >= c.ActualWidth - c.BorderThickness.Right)
		{
			_rightEdge = true;
		}
		if (_mouseClickPoint.Y >= c.ActualHeight - c.BorderThickness.Bottom)
		{
			_bottomEdge = true;
		}
		if (_mouseClickPoint.X > c.BorderThickness.Left && _mouseClickPoint.X < c.ActualWidth - c.BorderThickness.Right && _mouseClickPoint.Y > c.BorderThickness.Top && _mouseClickPoint.Y < c.ActualHeight - c.BorderThickness.Bottom)
		{
			_moving = true;
		}
		c.CaptureMouse();
	}

	public static void OnControlMouseLeave(object sender, MouseEventArgs e)
	{
		Mouse.SetCursor(Cursors.Arrow);
		_leftEdge = false;
		_rightEdge = false;
		_topEdge = false;
		_bottomEdge = false;
		_moving = false;
		Control c = sender as Control;
		_mouseClickPoint = default;
		c.ReleaseMouseCapture();
	}

	public static void OnControlMouseMove(object sender, MouseEventArgs e)
	{
		Control _element = sender as Control;
		Point current_position = e.GetPosition(_element);
		double width = _element.Width;
		if (current_position.X <= _element.BorderThickness.Left || current_position.X >= _element.ActualWidth - _element.BorderThickness.Right)
		{
			Mouse.SetCursor(Cursors.SizeWE);
		}
		if (current_position.Y <= _element.BorderThickness.Right || current_position.Y >= _element.ActualHeight - _element.BorderThickness.Bottom)
		{
			Mouse.SetCursor(Cursors.SizeNS);
		}
		if (current_position.X <= _element.BorderThickness.Left && current_position.Y <= _element.BorderThickness.Top)
		{
			Mouse.SetCursor(Cursors.SizeNWSE);
		}
		if (_element.IsMouseCaptured)
		{
			if (_leftEdge)
			{
				(_element.Margin, _element.Width) = Resize_LeftEdge(_element, e);
			}
			if (_rightEdge)
			{
				Resize_RightEdge(_element, e);
			}
			if (_topEdge)
			{
				Resize_TopEdge(_element, e);
			}
			if (_bottomEdge)
			{
				Resize_BottomEdge(_element, e);
			}
			if (_moving)
			{
				MoveElement(_element, e);
			}
			width = Math.Max(_element.Width, _element.MinWidth);
			double height = Math.Max(_element.Height, _element.MinHeight);
			_mouseClickPoint = e.GetPosition(_element);
		}
	}

	private static (Thickness, double) Resize_LeftEdge(Control control, MouseEventArgs e)
	{
		double width = control.Width;
		double deltaX = e.GetPosition(control).X - _mouseClickPoint.X;
		double left_margin = control.Margin.Left;
		width -= deltaX;
		left_margin += deltaX;
		if (width < control.MinWidth)
		{
			width = control.MinWidth;
			left_margin = width;
			deltaX = 0.0;
		}
		if (width > control.MaxWidth)
		{
			width = control.MaxWidth;
			left_margin = control.Margin.Left;
			deltaX = 0.0;
		}
		control.Margin = new Thickness(left_margin, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
		control.Width = width;
		return (control.Margin, deltaX);
	}

	private static void Resize_RightEdge(Control control, MouseEventArgs e)
	{
		double width = control.Width;
		width = e.GetPosition(control).X;
		if (width <= control.MinWidth)
		{
			width = control.MinWidth;
		}
		if (width > control.MaxWidth)
		{
			width = control.MaxWidth;
		}
		control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
		control.Width = width;
	}

	private static void Resize_TopEdge(Control control, MouseEventArgs e)
	{
		double height = control.Height;
		double top_margin = control.Margin.Top;
		double deltaY = e.GetPosition(control).Y - _mouseClickPoint.Y;
		height -= deltaY;
		top_margin += deltaY;
		if (height <= control.MinHeight)
		{
			height = control.MinHeight;
			top_margin = control.Margin.Top;
		}
		if (height > control.MaxHeight)
		{
			height = control.MaxHeight;
			top_margin = control.Margin.Top;
		}
		control.Margin = new Thickness(control.Margin.Left, top_margin, control.Margin.Right, control.Margin.Bottom);
		control.Height = height;
	}

	private static void Resize_BottomEdge(Control control, MouseEventArgs e)
	{
		double height = control.Height;
		height = e.GetPosition(control).Y;
		if (height <= control.MinHeight)
		{
			height = control.MinHeight;
		}
		if (height > control.MaxHeight)
		{
			height = control.MaxHeight;
		}
		control.Margin = new Thickness(control.Margin.Left, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
		control.Height = height;
	}

	private static void MoveElement(Control control, MouseEventArgs e)
	{
		if (VisualTreeHelper.GetParent(control) is UIElement container)
		{
			Point mouse_container_position = e.GetPosition(container);
			double x = mouse_container_position.X - _mouseClickPoint.X;
			double y = mouse_container_position.Y - _mouseClickPoint.Y;
			Console.WriteLine(x + ":" + y);
			control.Margin = new Thickness(0.0);
			control.RenderTransform = new TranslateTransform(x, y);
		}
	}
}
