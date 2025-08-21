// Version: 0.1.0.18
using System.Windows;
using Thmd.Logs;

namespace Thmd.Helpers;

public static class FullscreenHelper
{
	private static WindowLastStance _lastWindowStance;

	private static bool _fullscreen;

	public static bool IsFullscreen => _fullscreen;

	public static WindowLastStance LastWindowStance => _lastWindowStance;

	public static void Fullscreen(this object sender)
	{
		FullscreenOnOff(sender);
	}

	private static void FullscreenOnOff(object sender)
	{
		Window window = Window.GetWindow(sender as DependencyObject);
		if (window != null)
		{
			if (window.WindowStyle == WindowStyle.None)
			{
				if (_lastWindowStance != null)
				{
					window.ResizeMode = _lastWindowStance.Mode;
					window.WindowStyle = _lastWindowStance.Style;
					window.WindowState = _lastWindowStance.State;
					_fullscreen = false;
					Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"Exit fullscreen: Change video screen from fullscreen to last stance {_lastWindowStance.State}");
					return;
				}
				window.ResizeMode = ResizeMode.CanResize;
				window.WindowStyle = WindowStyle.SingleBorderWindow;
				window.WindowState = WindowState.Normal;
				_fullscreen = false;
				Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"Exit fullscreen: Change video screen from fullscreen to default stance {_lastWindowStance.State}");
			}
			_lastWindowStance = new WindowLastStance
			{
				Mode = window.ResizeMode,
				State = window.WindowState,
				Style = window.WindowStyle
			};
			window.ResizeMode = ResizeMode.NoResize;
			window.WindowStyle = WindowStyle.None;
			window.WindowState = WindowState.Normal;
			window.WindowState = WindowState.Maximized;
			_fullscreen = true;
			Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"Enter fullscreen: Change video screen to fullscreen from last stance {_lastWindowStance.State}");
		}
		else
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Fullscreen: Window is null");
		}
	}
}
