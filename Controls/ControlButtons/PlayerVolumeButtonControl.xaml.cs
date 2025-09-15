// Version: 0.1.9.4
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

using Vlc.DotNet.Wpf;

namespace Thmd.Controls.ControlButtons;

public partial class PlayerVolumeButtonControl : UserControl
{
	public ProgressBarBox VolumeProgressBar
	{
		get
		{
			return _volumeProgressBar;
		}
		set
		{
			_volumeProgressBar.ProgressText = $"Volume: {_volumeProgressBar.Value}";
            VolumeProgressBar.MouseMove += VolumeProgressBar_MouseMove;
			_volumeProgressBar = value;
		}
	}

    public PlayerVolumeButtonControl()
	{
		InitializeComponent();
	}

    /// <summary>
    /// Handles mouse move events on the volume progress bar to adjust the volume.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void VolumeProgressBar_MouseMove(object sender, MouseEventArgs e)
    {
        double position = e.GetPosition(sender as ProgressBarBox).X;
        double width = (sender as ProgressBarBox).ActualWidth;
        double result = position / width * (sender as ProgressBarBox).Maximum;
            (sender as ProgressBarBox).PopupText = $"Volume: {(int)result}";
       
    }
}
