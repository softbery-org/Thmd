// Version: 0.1.13.53
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Thmd.Consolas;
using Thmd.Media;
using Thmd.Utilities;

namespace Thmd.Controls;

/// <summary>
/// A custom ListView control for managing a playlist of video media items.
/// Provides functionality to play, pause, remove, and reorder videos, with support for
/// user interactions like double-click, right-click context menu actions, and drag-and-drop reordering
/// with a drop shadow effect for the dragged item.
/// Implements INotifyPropertyChanged for data binding and uses an ObservableCollection
/// to dynamically update the UI when the playlist changes.
/// Supports background thread operations for non-UI tasks using Task and Dispatcher for UI updates.
/// </summary>
public partial class PlaylistView : ListView, INotifyPropertyChanged
{
    // Stores the index of the currently selected video in the playlist.
    private int _currentIndex = 0;

    // Reference to the media player used to control video playback.
    private IPlayer _player;

    // Context menu for right-click interactions with playlist items.
    private ContextMenu _rightClickMenu;

    // Collection of videos in the playlist, bound to the ListView's ItemsSource.
    private ObservableCollection<VideoItem> _videos = new ObservableCollection<VideoItem>();

    // Stores the item being dragged during a drag-and-drop operation.
    private VideoItem _draggedItem;

    // Tracks whether a drag operation is in progress.
    private bool _isDragging;

    // 
    private int _draggedIndex = -1;

    //
    private int _lastTargetIndex = -1;

    // Stores the original background of the item being hovered over during drag.
    private Brush _originalBackground;

    // Minimum distance to start drag (to avoid accidental drags)
    private const double DragThreshold = 10.0;

    // Point where the mouse was pressed down
    private Point _startPoint;

    /// <summary>
    /// Add command
    /// </summary>
    public ICommand AddCommand { get; private set; }

    /// <summary>
    /// Remove command
    /// </summary>
    public ICommand RemoveCommand { get; private set; }

    /// <summary>
    /// Edit command
    /// </summary>
    public ICommand EditCommand { get; private set; }

    /// <summary>
    /// Close command
    /// </summary>
    public ICommand CloseCommand { get; private set; }

    public ICommand SavePlaylistCommand { get; private set; }
    public ICommand LoadPlaylistCommand { get; private set; }

    /// <summary>
    /// Gets or sets the collection of videos in the playlist and updates the UI.
    /// </summary>
    public ObservableCollection<VideoItem> Videos
    {
        get => _videos;
        set
        {
            Dispatcher.InvokeAsync(() =>
            {
                _videos = value;
                base.ItemsSource = _videos;;
                SetValue(VideosProperty, value);
                OnPropertyChanged(nameof(Videos));
            });
        }
    }

    public static readonly DependencyProperty VideosProperty =
        DependencyProperty.Register("Videos", typeof(ObservableCollection<VideoItem>), typeof(PlaylistView),
            new PropertyMetadata(new ObservableCollection<VideoItem>(), OnVideosChanged));

    private static void OnVideosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var playlistView = (PlaylistView)d;
        playlistView.Dispatcher.Invoke(() =>
        {
            playlistView.ItemsSource = playlistView.Videos;
        });
    }

    /// <summary>
    /// Gets or sets the index of the current video, ensuring valid bounds.
    /// Returns -1 if the playlist is empty.
    /// </summary>
    public int CurrentIndex
    {
        get
        {
            if (Videos == null || Videos.Count == 0)
            {
                return -1;
            }
            int itemCount = Videos.Count;
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
            Dispatcher.Invoke(() => OnPropertyChanged("CurrentIndex"));
        }
    }

    /// <summary>
    /// Gets the index of the next video in the playlist, looping to 0 if at the end.
    /// Returns -1 if the playlist is empty.
    /// </summary>
    public int NextIndex
    {
        get
        {
            if (Videos == null || Videos.Count == 0)
            {
                return -1;
            }
            return (_currentIndex != Videos.Count - 1) ? (_currentIndex + 1) : 0;
        }
    }

    /// <summary>
    /// Gets the index of the previous video in the playlist, looping to the end if at the start.
    /// Returns -1 if the playlist is empty.
    /// </summary>
    public int PreviousIndex
    {
        get
        {
            if (Videos == null || Videos.Count == 0)
            {
                return -1;
            }
            return (_currentIndex == 0) ? (Videos.Count - 1) : (_currentIndex - 1);
        }
    }

    /// <summary>
    /// Gets the next video in the playlist based on <see cref="NextIndex"/>.
    /// </summary>
    public VideoItem Next => Videos != null && NextIndex >= 0 ? Videos[NextIndex] : null;

    /// <summary>
    /// Gets the previous video in the playlist based on <see cref="PreviousIndex"/>.
    /// </summary>
    public VideoItem Previous => Videos != null && PreviousIndex >= 0 ? Videos[PreviousIndex] : null;

    /// <summary>
    /// Moves to the next video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    public VideoItem MoveNext
    {
        get
        {
            if (Videos == null || Videos.Count == 0) return null;
            _currentIndex = NextIndex;
            Dispatcher.Invoke(() => OnPropertyChanged("CurrentIndex"));
            return Videos[CurrentIndex];
        }
    }

    /// <summary>
    /// Moves to the previous video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    public VideoItem MovePrevious
    {
        get
        {
            if (Videos == null || Videos.Count == 0) return null;
            _currentIndex = PreviousIndex;
            Dispatcher.Invoke(() => OnPropertyChanged("CurrentIndex"));
            return Videos[CurrentIndex];
        }
    }

    /// <summary>
    /// Gets or sets the current video in the playlist.
    /// </summary>
    public VideoItem Current
    {
        get
        {
            return Videos != null && CurrentIndex >= 0 ? Videos[CurrentIndex] : null;
        }
        set
        {
            if (Videos != null && CurrentIndex >= 0)
            {
                Videos[CurrentIndex] = value;
                Dispatcher.Invoke(() => OnPropertyChanged("Current"));
            }
        }
    }

    public int Count
    {
        get => Videos != null ? Videos.Count : 0;
    }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class.
    /// </summary>
    public PlaylistView()
    {
        InitializeComponent();

        base.DataContext = this;
        base.ItemsSource = Videos;

        // Enable drag-and-drop
        AllowDrop = true;

        // Initialize commands
        AddCommand = new RelayCommand(Add);
        RemoveCommand = new RelayCommand(Remove);
        EditCommand = new RelayCommand(Edit);
        CloseCommand = new RelayCommand(Close);
        SavePlaylistCommand = new RelayCommand(SavePlaylist);
        LoadPlaylistCommand = new RelayCommand(LoadPlaylist);

        // Initialize the context menu
        _rightClickMenu = new ContextMenu();
        MenuItem playItem = new MenuItem { Header = "Play" };
        MenuItem removeItem = new MenuItem { Header = "Remove" };
        MenuItem moveToTopItem = new MenuItem { Header = "Move to Top" };
        MenuItem moveUpper = new MenuItem { Header = "Move Upper" };
        MenuItem moveLower = new MenuItem { Header = "Move Lower" };
        _rightClickMenu.Items.Add(playItem);
        _rightClickMenu.Items.Add(removeItem);
        _rightClickMenu.Items.Add(moveUpper);
        _rightClickMenu.Items.Add(moveLower);
        _rightClickMenu.Items.Add(moveToTopItem);

        // Event handlers for menu items
        playItem.Click += MenuItemPlay_Click;
        removeItem.Click += MenuItemRemove_Click;
        moveToTopItem.Click += MenuItemMoveToTop_Click;
        moveUpper.Click += MenuItemMoveUpper_Click;
        moveLower.Click += MenuItemMoveLower_Click;

        // Set the context menu for the ListView
        this.ContextMenu = _rightClickMenu;

        // Subscribe to mouse events
        this.MouseDoubleClick += ListView_MouseDoubleClick;
    }

    // Metoda dla przycisku "Dodaj"
    private void Add(object parameter)
    {
        // Logika dla dodawania elementu do playlisty
        //MessageBox.Show("Dodawanie nowego elementu do playlisty.");
        _player.GetCurrentFrame();
        
    }

    // Metoda dla przycisku "Usuń"
    private void Remove(object parameter)
    {
        // Logika dla usuwania elementu z playlisty
        //NewAsync(BaseString, "");
    }

    // Metoda dla przycisku "Edytuj"
    private void Edit(object parameter)
    {
        // Logika dla edycji elementu playlisty
        MessageBox.Show("Edycja wybranego elementu playlisty.");
    }

    // Metoda dla przycisku "Zamknij"
    private void Close(object parameter)
    {
        // Logic for hide playlist
        this.Visibility = Visibility.Collapsed;
    }

    private void SavePlaylist(object parameter)
    {
        _player.SavePlaylistConfig();        
    }

    public void LoadPlaylist(object parameter)
    {
        _player.LoadPlaylistConfig();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
    }

    public PlaylistView(IPlayer player) : this()
    {
        _player = player;
    }

    public void SetPlayer(IPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify the UI of property changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Clears all videos from the playlist and unsubscribes from their events in a background task.
    /// </summary>
    public Task ClearTracksAsync()
    {
        return Task.Run(() =>
        {
            if (Videos == null) return;
            var videosToClear = Videos.ToList();
            foreach (VideoItem video in videosToClear)
            {
                video.PositionChanged -= Video_PositionChanged;
                video.MouseDown -= Media_MouseDown;
            }
            Dispatcher.Invoke(() =>
            {
                Videos.Clear();
                CurrentIndex = -1;
            });
        });
    }

    /// <summary>
    /// Checks if a video is already in the playlist based on its URI.
    /// </summary>
    /// <param name="media">The video to check for.</param>
    /// <returns>True if the video is in the playlist; otherwise, false.</returns>
    public bool Contains(VideoItem media)
    {
        if (media == null || Videos == null) return false;
        return Videos.Any(item => item.Uri == media.Uri);
    }

    /// <summary>
    /// Removes a video from the playlist and adjusts the current index if necessary in a background task.
    /// </summary>
    /// <param name="media">The video to remove.</param>
    /// <returns>A task that returns the removed video, or null if the video was not found.</returns>
    public Task<VideoItem> RemoveAsync(VideoItem media)
    {
        return Task.Run(() =>
        {
            if (media == null || Videos == null) return null;
            int index = Videos.ToList().FindIndex(video => video.Uri == media.Uri);
            if (index >= 0)
            {
                VideoItem item = Videos[index];
                item.PositionChanged -= Video_PositionChanged;
                item.MouseDown -= Media_MouseDown;
                Dispatcher.Invoke(() =>
                {
                    Videos.RemoveAt(index);
                    if (index < CurrentIndex)
                    {
                        CurrentIndex--;
                    }
                    else if (index == CurrentIndex && Videos.Count > 0)
                    {
                        CurrentIndex = Math.Min(CurrentIndex, Videos.Count - 1);
                    }
                    else if (Videos.Count == 0)
                    {
                        CurrentIndex = -1;
                    }
                });
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Removed video {item.Name} at index {index}");
                return item;
            }
            return null;
        });
    }

    /// <summary>
    /// Clears the current playlist to start a new one in a background task.
    /// </summary>
    /// <param name="name">The name of the new playlist (not currently used).</param>
    /// <param name="description">The description of the new playlist (not currently used).</param>
    public Task NewAsync(string name, string description)
    {
        return ClearTracksAsync();
    }

    /// <summary>
    /// Adds a single video to the playlist and sets up its player and event handlers in a background task.
    /// </summary>
    /// <param name="media">The video to add.</param>
    public Task AddAsync(VideoItem media)
    {
        return Task.Run(() =>
        {
            if (media == null || Contains(media)) return;
            media.SetPlayer(_player);
            media.PositionChanged += Video_PositionChanged;
            media.MouseDown += Media_MouseDown;
            Dispatcher.Invoke(() => Videos.Add(media));
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Added video {media.Name}");
        });
    }

    private void Media_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.WriteLine("CLICKED");
    }

    /// <summary>
    /// Adds multiple videos to the playlist and sets up their player and event handlers in a background task.
    /// </summary>
    /// <param name="medias">The array of videos to add.</param>
    public Task AddAsync(VideoItem[] medias)
    {
        return Task.Run(async () =>
        {
            if (medias == null) return;
            foreach (VideoItem media in medias)
            {
                await AddAsync(media);
            }
        });
    }

    /// <summary>
    /// Handles position changes in a video and logs the event.
    /// </summary>
    /// <param name="sender">The video that triggered the position change.</param>
    /// <param name="newPosition">The new position of the video playback.</param>
    private void Video_PositionChanged(object sender, double newPosition)
    {
        if (sender is VideoItem video)
        {
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Position changed for {video.Name} to {newPosition}");
        }
    }

    /// <summary>
    /// Handles double-click events to play the selected video.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedItem is VideoItem selectedVideo)
        {
            PlayVideo(selectedVideo);
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Double-clicked to play video {selectedVideo.Name}");
        }
    }

    /// <summary>
    /// Plays a video selected from the context menu.
    /// </summary>
    /// <param name="video">The video to play.</param>
    private void PlayVideo(VideoItem video)
    {
        this.Dispatcher.InvokeAsync(() =>
        {
            if (video != null && _player != null)
            {
                Current?.Stop();
                CurrentIndex = Videos.IndexOf(video);
                _player.Play(video);
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Context menu play video {video.Name}");
            }
        });
    }

    /// <summary>
    /// Removes a video selected from the context menu and shows a confirmation message.
    /// </summary>
    /// <param name="video">The video to remove.</param>
    private async void RemoveVideo(VideoItem video)
    {
        if (video != null)
        {
            bool confirm = await Dispatcher.InvokeAsync(() =>
                MessageBox.Show($"Remove {video.Name} from playlist?", "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
            );
            if (confirm)
            {
                await RemoveAsync(video);
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Context menu removed video {video.Name}");
            }
        }
    }

    /// <summary>
    /// Moves a video selected from the context menu to the top of the playlist.
    /// </summary>
    /// <param name="video">The video to move to the top.</param>
    private void MoveVideoToTop(VideoItem video)
    {
        if (video != null && Videos != null)
        {
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex > 0)
            {
                Dispatcher.Invoke(() => Videos.Move(currentIndex, 0));
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Moved video {video.Name} to top");
            }
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null)
        {
            _draggedItem = (VideoItem)listViewItem.DataContext;
            _draggedIndex = Videos.IndexOf(_draggedItem);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
        {
            var data = new DataObject(typeof(VideoItem), _draggedItem);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
        }
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        e.Effects = DragDropEffects.None;

        // Reset IsDragOver dla wszystkich elementów
        foreach (var item in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is ListViewItem listViewItem)
            {
                DragDropHelper.SetIsDragOver(listViewItem, false);
            }
        }

        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            var targetPoint = e.GetPosition(this);
            var (targetItem, insertAfter) = GetItemAtPoint(targetPoint);
            if (targetItem != null)
            {
                int targetIndex = Videos.IndexOf(targetItem);
                if (targetIndex >= 0 && targetIndex != _lastTargetIndex && targetIndex != _draggedIndex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Videos.Move(_draggedIndex, targetIndex);
                        _draggedIndex = targetIndex;
                        _lastTargetIndex = targetIndex;
                    });
                }
                // Ustaw IsDragOver na elemencie docelowym
                var listViewItem = FindAncestor<ListViewItem>((DependencyObject)VisualTreeHelper.HitTest(this, targetPoint)?.VisualHit);
                if (listViewItem != null)
                {
                    DragDropHelper.SetIsDragOver(listViewItem, true);
                }
                e.Effects = DragDropEffects.Move;
            }
        }
        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        foreach (var item in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is ListViewItem listViewItem)
            {
                DragDropHelper.SetIsDragOver(listViewItem, false);
            }
        }
        e.Handled = true;
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        foreach (var item in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is ListViewItem listViewItem)
            {
                DragDropHelper.SetIsDragOver(listViewItem, false);
            }
        }
        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            var droppedItem = (VideoItem)e.Data.GetData(typeof(VideoItem));
            var targetPoint = e.GetPosition(this);
            var (targetItem, insertAfter) = GetItemAtPoint(targetPoint);
            if (targetItem != null)
            {
                int targetIndex = Videos.IndexOf(targetItem);
                if (targetIndex >= 0 && targetIndex != _draggedIndex)
                {
                    if (_draggedIndex != -1)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Videos.Move(_draggedIndex, targetIndex);
                            this.WriteLine($"PlaylistView: Moved video {droppedItem.Name}:{_draggedIndex} to index {targetIndex}");
                        });
                    }
                }
            }
        }
        _draggedItem = null;
        _draggedIndex = -1;
        _lastTargetIndex = -1;
        e.Handled = true;
    }

    private (VideoItem, bool) GetItemAtPoint(Point point)
    {
        var hitTestResult = VisualTreeHelper.HitTest(this, point);
        if (hitTestResult != null)
        {
            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)hitTestResult.VisualHit);
            if (listViewItem != null)
            {
                var item = (VideoItem)listViewItem.DataContext;
                if (item != null && Videos.Contains(item))
                {
                    // Oblicz pozycję kursora względem ListViewItem
                    var relativePoint = point;
                    var scrollViewer = FindAncestor<ScrollViewer>(this);
                    if (scrollViewer != null)
                    {
                        relativePoint = this.TranslatePoint(point, listViewItem);
                    }
                    var itemHeight = listViewItem.ActualHeight;
                    bool insertAfter = relativePoint.Y > itemHeight / 2;
                    return (item, insertAfter);
                }
            }
        }
        return (null, false);
    }

    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null && !(current is T))
        {
            current = VisualTreeHelper.GetParent(current);
        }
        return current as T;
    }

    /// <summary>
    /// Gets the insert index based on drop position.
    /// </summary>
    private int GetInsertIndex(Point position)
    {
        if (Videos == null || Videos.Count == 0) return 0;
        HitTestResult hitTest = VisualTreeHelper.HitTest(this, position);
        ListViewItem item = FindAncestor<ListViewItem>((DependencyObject)hitTest.VisualHit);
        if (item != null)
        {
            return ItemContainerGenerator.IndexFromContainer(item);
        }
        return Videos.Count;
    }

    private bool IsSupportedMediaFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        string[] supportedExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".flv", ".wmv", ".m4v", ".webm" };
        string extension = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
        return supportedExtensions.Contains(extension);
    }

    /// <summary>
    /// Handles the MouseLeave event to reset the dragging state and remove the adorner.
    /// </summary>
    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDragging)
        {
            _isDragging = false;
        }
        _draggedItem = null;
        _draggedIndex = -1;
        _lastTargetIndex = -1;
    }

    private void MenuItemPlay_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
            PlayVideo(SelectedItem as VideoItem);
    }

    private void MenuItemMoveToTop_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
            MoveVideoToTop(SelectedItem as VideoItem);
    }

    private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
            RemoveVideo(SelectedItem as VideoItem);
    }

    private void MenuItemMoveUpper_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            VideoItem video = SelectedItem as VideoItem;
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex > 0)
            {
                Dispatcher.Invoke(() => Videos.Move(currentIndex, currentIndex - 1));
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Moved video {video.Name} upper");
            }
        }
    }

    private void MenuItemMoveLower_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem != null)
        {
            VideoItem video = SelectedItem as VideoItem;
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex < Videos.Count - 1)
            {
                Dispatcher.Invoke(() => Videos.Move(currentIndex, currentIndex + 1));
                Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Moved video {video.Name} lower");
            }
        }
    }

    // Helper RelayCommand class, implement ICommand
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
