// Version: 0.1.0.16
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Controls;

public partial class ControlBarControl : UserControl
{
	private IPlayer _player;

	private string _videoName;

	private string _videoTime;

	public Button BtnPlay => _playerBtnControl._btnPlay;

	public Button BtnStop => _playerBtnControl._btnStop;

	public Button BtnNext => _playerBtnControl._btnNext;

	public Button BtnPrevious => _playerBtnControl._btnPrevious;

	public Button BtnVolumeUp => _playerBtnVolume._btnVolumeUp;

	public Button BtnVolumeDown => _playerBtnVolume._btnVolumeDown;

	public Button BtnMute => _playerBtnVolume._btnMute;

	public Button BtnSettingsWindow => _playerBtnSecondRow._btnSettings;

	public Button BtnSubtitle => _playerBtnSecondRow._btnSubtitle;

	public Button BtnUpdate => _playerBtnSecondRow._btnUpdate;

	public Button BtnOpen => _playerBtnControl._btnOpen;

	public Button BtnPlaylist => _playerBtnSecondRow._btnPlaylist;

	public Button BtnFullscreen => _playerBtnSecondRow._btnFullscreen;

	public string VideoName
	{
		get
		{
			return _videoName;
		}
		set
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				if (_videoNameTextBlock != null)
				{
					_videoNameTextBlock.Text = value;
				}
			});
			OnPropertyChanged("VideoName", ref _videoName, value);
		}
	}

	public string VideoTime
	{
		get
		{
			return _videoTime;
		}
		set
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				if (_videoTimeTextBlock != null)
				{
					_videoTimeTextBlock.Text = value;
				}
			});
			OnPropertyChanged("VideoTime", ref _videoTime, value);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public ControlBarControl()
	{
		InitializeComponent();
	}

	private void _repeatRandom_Checked(object sender, RoutedEventArgs e)
	{
		_repeatOne.IsChecked = false;
		_repeatAll.IsChecked = false;
		_repeatNone.IsChecked = false;
		_player.Repeat = RepeatType.Random;
	}

	private void _repeatNone_Checked(object sender, RoutedEventArgs e)
	{
		_repeatAll.IsChecked = false;
		_repeatOne.IsChecked = false;
		_repeatRandom.IsChecked = false;
		_player.Repeat = RepeatType.None;
	}

	private void _repeatAll_Checked(object sender, RoutedEventArgs e)
	{
		_repeatNone.IsChecked = false;
		_repeatOne.IsChecked = false;
		_repeatRandom.IsChecked = false;
		_player.Repeat = RepeatType.All;
	}

	private void _repeatOne_Checked(object sender, RoutedEventArgs e)
	{
		_repeatRandom.IsChecked = false;
		_repeatAll.IsChecked = false;
		_repeatNone.IsChecked = false;
		_player.Repeat = RepeatType.Current;
	}

	protected void OnPropertyChanged(string propertyName)
	{
		if (this.PropertyChanged != null)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	private void OnPropertyChanged<T>(string propertyName, ref T field, T value)
	{
		if (field != null || value == null)
		{
			if (field == null)
			{
				return;
			}
			object obj = value;
			if (field.Equals(obj))
			{
				return;
			}
		}
		field = value;
		this.PropertyChanged?.Invoke(field, new PropertyChangedEventArgs(propertyName));
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
		_repeatOne.Checked += _repeatOne_Checked;
		_repeatAll.Checked += _repeatAll_Checked;
		_repeatNone.Checked += _repeatNone_Checked;
		_repeatRandom.Checked += _repeatRandom_Checked;
		_repeatNone.IsChecked = true;
	}
}
