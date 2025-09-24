// Version: 0.1.12.99
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Thmd.Utilities;

public class ShowHideControlsHelper
{
	public static async Task Show(Control control, TimeSpan time)
	{
		await Task.Delay(time);
		control.Visibility = Visibility.Visible;
	}

	public static async Task Hide(Control control, TimeSpan time)
	{
		await Task.Delay(time);
		control.Visibility = Visibility.Collapsed;
	}
}
