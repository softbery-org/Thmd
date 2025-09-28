// Version: 0.1.15.4
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Thmd.Windowses;

public static class WindowPropertiesExtensions
{
	public struct W32Point
	{
		public int X;

		public int Y;
	}

	internal struct W32MonitorInfo
	{
		public int Size;

		public W32Rect Monitor;

		public W32Rect WorkArea;

		public uint Flags;
	}

	internal struct W32Rect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(ref W32Point pt);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref W32MonitorInfo lpmi);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromPoint(W32Point pt, uint dwFlags);

	public static bool ActivateCenteredToMouse(this Window window)
	{
		ComputeTopLeft(ref window);
		return window.Activate();
	}

	public static void ShowCenteredToMouse(this Window window)
	{
		WindowStartupLocation oldLocation = window.WindowStartupLocation;
		window.WindowStartupLocation = WindowStartupLocation.Manual;
		ComputeTopLeft(ref window);
		window.Show();
		window.WindowStartupLocation = oldLocation;
	}

	private static void ComputeTopLeft(ref Window window)
	{
		W32Point pt = default;
		if (!GetCursorPos(ref pt))
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
		}
		IntPtr monHandle = MonitorFromPoint(pt, 2u);
		W32MonitorInfo monInfo = new W32MonitorInfo
		{
			Size = Marshal.SizeOf(typeof(W32MonitorInfo))
		};
		if (!GetMonitorInfo(monHandle, ref monInfo))
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
		}
		W32Rect monitor = monInfo.WorkArea;
		double offsetX = Math.Round(window.Width / 2.0);
		double offsetY = Math.Round(window.Height / 2.0);
		double top = pt.Y - offsetY;
		double left = pt.X - offsetX;
		Rect screen = new Rect(new Point(monitor.Left, monitor.Top), new Point(monitor.Right, monitor.Bottom));
		Rect wnd = new Rect(new Point(left, top), new Point(left + window.Width, top + window.Height));
		window.Top = wnd.Top;
		window.Left = wnd.Left;
		if (!screen.Contains(wnd))
		{
			if (wnd.Top < screen.Top)
			{
				double diff = Math.Abs(screen.Top - wnd.Top);
				window.Top = wnd.Top + diff;
			}
			if (wnd.Bottom > screen.Bottom)
			{
				double diff2 = wnd.Bottom - screen.Bottom;
				window.Top = wnd.Top - diff2;
			}
			if (wnd.Left < screen.Left)
			{
				double diff3 = Math.Abs(screen.Left - wnd.Left);
				window.Left = wnd.Left + diff3;
			}
			if (wnd.Right > screen.Right)
			{
				double diff4 = wnd.Right - screen.Right;
				window.Left = wnd.Left - diff4;
			}
		}
	}
}
