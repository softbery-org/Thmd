// Version: 0.1.3.78
// StoryboardHelper.cs
// A static helper class that provides extension methods for animating the visibility of WPF controls
// using storyboards. It supports asynchronous hiding and showing of controls with error handling
// and logging for animation operations.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Thmd.Logs;

namespace Thmd.Helpers;

/// <summary>
/// A static helper class that provides extension methods for animating the visibility of WPF controls
/// using storyboards. Supports asynchronous hiding and showing of controls with error handling
/// and logging for animation operations.
/// </summary>
public static class StoryboardHelper
{
    /// <summary>
    /// Asynchronously hides a control using the specified storyboard animation if the control is visible
    /// and the mouse is not over it.
    /// </summary>
    /// <param name="sender">The control to hide.</param>
    /// <param name="storyboard">The storyboard animation to apply for hiding the control.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task HideByStoryboard(this Control sender, Storyboard storyboard)
    {
        if (storyboard != null)
        {
            await sender.Hide(storyboard);
        }
    }

    /// <summary>
    /// Asynchronously shows a control using the specified storyboard animation, making it visible.
    /// </summary>
    /// <param name="sender">The control to show.</param>
    /// <param name="storyboard">The storyboard animation to apply for showing the control.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ShowByStoryboard(this Control sender, Storyboard storyboard)
    {
        if (storyboard != null)
        {
            await sender.Show(storyboard);
        }
    }

    /// <summary>
    /// Asynchronously hides a control using the specified storyboard animation if the control is visible
    /// and the mouse is not over it. Logs any errors that occur during the operation.
    /// </summary>
    /// <param name="sender">The control to hide.</param>
    /// <param name="storyboard">The storyboard animation to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task Hide(this Control sender, Storyboard storyboard)
    {
        Task task = Task.Run(delegate
        {
            try
            {
                sender.Dispatcher.InvokeAsync(delegate
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
                Logger.Log.Log(LogLevel.Error, new string[] { "Console", "File" }, ex.Message ?? "");
            }
        });
        await Task.FromResult(task).Result;
    }

    /// <summary>
    /// Asynchronously shows a control by setting its visibility to Visible and applying the specified
    /// storyboard animation. Logs any errors that occur during the operation.
    /// </summary>
    /// <param name="sender">The control to show.</param>
    /// <param name="storyboard">The storyboard animation to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task Show(this Control sender, Storyboard storyboard)
    {
        Task task = Task.Run(delegate
        {
            try
            {
                sender.Dispatcher.InvokeAsync(delegate
                {
                    sender.Visibility = Visibility.Visible;
                    storyboard.AutoReverse = false;
                    storyboard.Begin(sender, HandoffBehavior.Compose, isControllable: false);
                });
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new string[] { "Console", "File" }, ex.Message ?? "");
            }
        });
        await Task.FromResult(task).Result;
    }
}
