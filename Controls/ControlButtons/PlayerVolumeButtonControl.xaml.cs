// Version: 0.1.1.86
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Thmd.Controls.ControlButtons;

public partial class PlayerVolumeButtonControl : UserControl
{
	public ProgressBarControl VolumeProgressBar
	{
		get
		{
			return _volumeProgressBar;
		}
		set
		{
			_volumeProgressBar.ProgressText = $"Volume: {_volumeProgressBar.Value}";
            _volumeProgressBar = value;
		}
	}

    public PlayerVolumeButtonControl()
	{
		InitializeComponent();
	}
}
