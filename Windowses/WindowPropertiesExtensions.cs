// Version: 0.1.17.18
using System;
using System.Diagnostics.CodeAnalysis;
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

    [DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

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
		double offsetX = System.Math.Round(window.Width / 2.0);
		double offsetY = System.Math.Round(window.Height / 2.0);
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
				double diff = System.Math.Abs(screen.Top - wnd.Top);
				window.Top = wnd.Top + diff;
			}
			if (wnd.Bottom > screen.Bottom)
			{
				double diff2 = wnd.Bottom - screen.Bottom;
				window.Top = wnd.Top - diff2;
			}
			if (wnd.Left < screen.Left)
			{
				double diff3 = System.Math.Abs(screen.Left - wnd.Left);
				window.Left = wnd.Left + diff3;
			}
			if (wnd.Right > screen.Right)
			{
				double diff4 = wnd.Right - screen.Right;
				window.Left = wnd.Left - diff4;
			}
		}
	}

    public static void GetWindowDpi(IntPtr hwnd, out int dpiX, out int dpiY)
    {
        var handle = MonitorFromWindow(hwnd, MonitorFlag.MONITOR_DEFAULTTOPRIMARY);

        GetDpiForMonitor(handle, MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
    }

    /// <summary>
    /// Determines the function's return value if the window does not intersect any display monitor.
    /// </summary>
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum MonitorFlag : uint
    {
        /// <summary>Returns NULL.</summary>
        MONITOR_DEFAULTTONULL = 0,
        /// <summary>Returns a handle to the primary display monitor.</summary>
        MONITOR_DEFAULTTOPRIMARY = 1,
        /// <summary>Returns a handle to the display monitor that is nearest to the window.</summary>
        MONITOR_DEFAULTTONEAREST = 2
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, MonitorFlag flag);

    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum MonitorDpiType
    {
        /// <summary>
        /// The effective DPI.
        /// This value should be used when determining the correct scale factor for scaling UI elements.
        /// This incorporates the scale factor set by the user for this specific display.
        /// </summary>
        MDT_EFFECTIVE_DPI = 0,
        /// <summary>
        /// The angular DPI.
        /// This DPI ensures rendering at a compliant angular resolution on the screen.
        /// This does not include the scale factor set by the user for this specific display.
        /// </summary>
        MDT_ANGULAR_DPI = 1,
        /// <summary>
        /// The raw DPI.
        /// This value is the linear DPI of the screen as measured on the screen itself.
        /// Use this value when you want to read the pixel density and not the recommended scaling setting.
        /// This does not include the scale factor set by the user for this specific display and is not guaranteed to be a supported DPI value.
        /// </summary>
        MDT_RAW_DPI = 2
    }

    [DllImport("user32.dll")]
    private static extern bool GetDpiForMonitor(IntPtr hwnd, MonitorDpiType dpiType, out int dpiX, out int dpiY);
}
