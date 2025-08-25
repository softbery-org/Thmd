// Version: 0.1.0.74
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Thmd.Logs;
using Thmd.Media;

namespace Thmd.Controls;

public partial class PlaylistControl : ListView, INotifyPropertyChanged
{
	private int _currentIndex = 0;

	private Playlist _playlist = new Playlist("New list");

	private ContextMenu _rightClickMenu = new ContextMenu();

	private IPlayer _player;

	private GridView _gridView = new GridView();

	private List<GridViewColumn> _columns = new List<GridViewColumn>();

	private int _nextMediaPlayedIndex = -1;

	private Video _nextMediaPlayed = null;

	public int CurrentIndex
	{
		get
		{
			if (base.Items.Count == 0)
			{
				return -1;
			}
			int itemCount = base.Items.Count;
			if (_currentIndex >= itemCount)
			{
				_currentIndex = 0;
			}
			if (_currentIndex < 0)
			{
				_currentIndex = itemCount - 1;
			}
			return _currentIndex;
		}
		set
		{
			_currentIndex = value;
		}
	}

	public int NextIndex
	{
		get
		{
			if (base.Items.Count == 0)
			{
				return -1;
			}
			return (_currentIndex != base.Items.Count - 1) ? (_currentIndex + 1) : 0;
		}
	}

	public int PreviousIndex
	{
		get
		{
			if (base.Items.Count == 0)
			{
				return -1;
			}
			return (_currentIndex == 0) ? (base.Items.Count - 1) : (_currentIndex - 1);
		}
	}

	public Video Next => GetMediaFromListItem(base.Items[NextIndex] as ListViewItem);

	public Video Previous => GetMediaFromListItem(base.Items[PreviousIndex] as ListViewItem);

	public Video MoveNext
	{
		get
		{
			_currentIndex = NextIndex;
			return GetMediaFromListItem(base.Items[CurrentIndex] as ListViewItem);
		}
	}

	public Video MovePrevious
	{
		get
		{
			_currentIndex = PreviousIndex;
			return GetMediaFromListItem(base.Items[CurrentIndex] as ListViewItem);
		}
	}

	public Video Current
	{
		get
		{
			return GetMediaFromListItem(base.Items[CurrentIndex] as ListViewItem);
		}
		set
		{
			_playlist[CurrentIndex] = value;
		}
	}

	public GridView GridView => _gridView;

	public int NextMediaPlayedIndex
	{
		get
		{
			return _nextMediaPlayedIndex;
		}
		set
		{
			_nextMediaPlayedIndex = value;
		}
	}

	public Video NextMediaPlayed
	{
		get
		{
			return _nextMediaPlayed;
		}
		set
		{
			_nextMediaPlayed = value;
		}
	}

	public Video this[int id]
	{
		get
		{
			return base.Items[id] as Video;
		}
		set
		{
			base.Items[id] = value;
		}
	}

	private static event Action GetPlaylist;

	private static event Action PlaylistChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	public PlaylistControl()
	{
		base.Width = 300.0;
		base.Height = 200.0;
		base.MinWidth = 150.0;
		base.MinHeight = 100.0;
		ScrollIntoView(this);
		base.BorderThickness = new Thickness(3.0);
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		PlaylistChanged += OnPlaylistChanged;
		GetPlaylist += OnGetPlaylist;
	}

	public PlaylistControl(params object[] Medias)
		: this()
	{
		try
		{
			Add(Medias);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
	}

	public IEnumerable<Video> GetAllVideos()
	{
		return _playlist.AsReadOnly();
	}

	public void SetGridColumns(string[] columns)
	{
		foreach (string column in columns)
		{
			Binding binding = new Binding(column);
			if (column == "Name")
			{
				binding.Converter = null;
			}
			else if (column == "Duration" || column == "Position")
			{
				binding.Converter = new TimeSpanConverter();
			}
			GridViewColumn column_item = new GridViewColumn
			{
				DisplayMemberBinding = binding,
				Header = column,
				Width = double.NaN
			};
			_gridView.Columns.Add(column_item);
			_columns.Add(column_item);
			Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "Adding column " + column + " for playlist.");
		}
		base.View = GridView;
	}

	private ListViewItem CreateListItem(Video media)
	{
		try
		{
			ListViewItem item = new ListViewItem();
			media.SetPlayer(_player);
			media.PositionChanged += Media_PositionChanged;
			item.Content = new MediaViewModel(media);
			item.MouseDoubleClick += OnPlaylistItemMouseDoubleClick;
			item.MouseDown += OnListItemMouseClick;
			RoutedCommand play = new RoutedCommand("Play", typeof(PlaylistControl));
			_rightClickMenu = new ContextMenu();
			_rightClickMenu.Items.Add(new MenuItem
			{
				Header = "Play",
				Command = play
			});
			_rightClickMenu.Items.Add(new MenuItem
			{
				Header = "Pause"
			});
			_rightClickMenu.Items.Add(new MenuItem
			{
				Header = "Stop"
			});
			_rightClickMenu.CommandBindings.Add(new CommandBinding(play, delegate
			{
				_player.Play(media);
			}));
			item.ContextMenu = _rightClickMenu;
			item.Effect = new DropShadowEffect
			{
				Color = Colors.Black,
				BlurRadius = 10.0,
				ShadowDepth = 0.0,
				Opacity = 0.5
			};
			Logger.Log.Log(LogLevel.Info, "Console", "Add media: " + media.Name + " to playlist.");
			return item;
		}
		catch (Exception ex)
		{
			Logger.Log.Log(LogLevel.Error, "Console", ex.Message ?? "");
			return null;
		}
	}

	private void Media_PositionChanged(object sender, double e)
	{
		Video media = (Video)sender;
		if (media == null)
		{
			return;
		}
		base.Dispatcher.InvokeAsync(delegate
		{
			ListViewItem listViewItem = base.Items.Cast<ListViewItem>().FirstOrDefault((ListViewItem i) => ((MediaViewModel)i.Content).Media == media);
			if (listViewItem != null)
			{
				((MediaViewModel)listViewItem.Content).Position = TimeSpan.FromMilliseconds(e);
			}
		});
	}

	private Video GetMediaFromListItem(ListViewItem item)
	{
		Video result = null;
		base.Dispatcher.InvokeAsync(delegate
		{
			result = (item?.Content as MediaViewModel)?.Media;
		});
		return result;
	}

	private void OnPlaylistChanged()
	{
		PlaylistControl.PlaylistChanged?.Invoke();
	}

	private void OnGetPlaylist()
	{
		_playlist = (Playlist)base.ItemsSource.Cast<Playlist>();
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

	private void OnPlaylistItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (base.SelectedItem != null)
		{
			ListViewItem item = base.SelectedItem as ListViewItem;
			Video media = GetMediaFromListItem(item);
			_currentIndex = base.SelectedIndex;
			_player.Play();
		}
	}

	private void OnListItemMouseClick(object sender, MouseButtonEventArgs e)
	{
		if (base.SelectedItem != null && e.RightButton == MouseButtonState.Pressed)
		{
			_rightClickMenu.PlacementTarget = sender as ListViewItem;
		}
	}

	private void OnControlClose(object sender, RoutedEventArgs e)
	{
		base.Visibility = Visibility.Collapsed;
	}

	public void ClearTracks()
	{
		base.Items.Clear();
	}

	public bool Contains(Video media)
	{
		return base.Items.Cast<ListViewItem>().Any((ListViewItem item) => ((MediaViewModel)item.Content).Media == media);
	}

	public Video Remove(Video media)
	{
		ListViewItem item = base.Items.Cast<ListViewItem>().FirstOrDefault((ListViewItem i) => ((MediaViewModel)i.Content).Media == media);
		if (item != null)
		{
			base.Items.Remove(item);
		}
		return media;
	}

	public void New(string name, string description)
	{
		ClearTracks();
	}

	public void Add(Video media)
	{
		_currentIndex++;
		base.Items.Add(CreateListItem(media));
	}

	public void Add(Video[] medias)
	{
		foreach (Video media in medias)
		{
			Video m = media;
			_currentIndex++;
			base.Items.Add(CreateListItem(m));
		}
	}

	public void Add(params object[] medias)
	{
		if (medias == null)
		{
			return;
		}
		Parallel.ForEach(medias, delegate(object media)
		{
			Video m = media as Video;
			if (m != null)
			{
				base.Dispatcher.Invoke(delegate
				{
					_currentIndex++;
					base.Items.Add(CreateListItem(m));
				});
			}
		});
	}
}
