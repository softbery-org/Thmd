// Version: 0.1.0.16
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Thmd.Logs;

namespace Thmd.Helpers;

public static class StoryboardHelper
{
	public static async Task HideByStoryboard(this Control sender, Storyboard storyboard)
	{
		if (storyboard != null)
		{
			await sender.Hide(storyboard);
		}
	}

	public static async Task ShowByStoryboard(this Control sender, Storyboard storyboard)
	{
		if (storyboard != null)
		{
			await sender.Show(storyboard);
		}
	}

	private static async Task Hide(this Control sender, Storyboard storyboard)
	{
		Task task = Task.Run(delegate
		{
			try
			{
				sender.Dispatcher.Invoke(delegate
				{
					if (sender.IsVisible && !sender.IsMouseOver)
					{
						storyboard.AutoReverse = false;
						storyboard.Begin(sender, HandoffBehavior.Compose, isControllable: false);
					}
				});
			}
			catch (Exception ex)
			{
				Logger.Log.Log(LogLevel.Error, "Console", ex.Message ?? "");
				Logger.Log.Log(LogLevel.Error, "File", ex.Message ?? "");
			}
		});
		await Task.FromResult(task).Result;
	}

	private static async Task Show(this Control sender, Storyboard storyboard)
	{
		Task task = Task.Run(delegate
		{
			try
			{
				sender.Dispatcher.Invoke(delegate
				{
					sender.Visibility = Visibility.Visible;
					storyboard.AutoReverse = false;
					storyboard.Begin(sender, HandoffBehavior.Compose, isControllable: false);
				});
			}
			catch (Exception ex)
			{
				Logger.Log.Log(LogLevel.Error, "Console", ex.Message ?? "");
				Logger.Log.Log(LogLevel.Error, "File", ex.Message ?? "");
			}
		});
		await Task.FromResult(task).Result;
	}
}
