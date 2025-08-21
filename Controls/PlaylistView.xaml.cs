// Version: 0.1.0.3
// PlaylistView.cs
// A custom ListView control for managing and displaying a playlist of video media items.
// This class provides functionality for playing, pausing, removing, and reordering videos
// in a playlist, with support for user interactions such as double-click and right-click
// context menu actions. It implements INotifyPropertyChanged for data binding and
// uses an ObservableCollection to dynamically update the UI when the playlist changes.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Thmd.Logs;
using Thmd.Media;

namespace Thmd.Controls;

/// <summary>
/// A custom ListView control for managing a playlist of video media items.
/// Provides functionality to play, pause, remove, and reorder videos, with support for
/// user interactions like double-click and right-click context menu actions.
/// Implements INotifyPropertyChanged for data binding and uses an ObservableCollection
/// to dynamically update the UI when the playlist changes.
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
    private ObservableCollection<Video> _videos;

    /// <summary>
    /// Gets the command to play a selected video.
    /// </summary>
    public ICommand PlayCommand { get; }

    /// <summary>
    /// Gets the command to pause the currently playing video.
    /// </summary>
    public ICommand PauseCommand { get; }

    /// <summary>
    /// Gets the command to remove a video from the playlist.
    /// </summary>
    public ICommand RemoveCommand { get; }

    /// <summary>
    /// Gets the command to move a video to the top of the playlist.
    /// </summary>
    public ICommand MoveToTopCommand { get; }

    /// <summary>
    /// Gets or sets the collection of videos in the playlist and updates the UI.
    /// </summary>
    public ObservableCollection<Video> Videos
    {
        get
        {
            return _videos;
        }
        set
        {
            _videos = value;
            base.ItemsSource = _videos;
            OnPropertyChanged("Videos");
        }
    }

    /// <summary>
    /// Gets or sets the index of the current video, ensuring valid bounds.
    /// Returns -1 if the playlist is empty.
    /// </summary>
    public int CurrentIndex
    {
        get
        {
            if (Videos.Count == 0)
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
            OnPropertyChanged("CurrentIndex");
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
            if (Videos.Count == 0)
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
            if (Videos.Count == 0)
            {
                return -1;
            }
            return (_currentIndex == 0) ? (Videos.Count - 1) : (_currentIndex - 1);
        }
    }

    /// <summary>
    /// Gets the next video in the playlist based on <see cref="NextIndex"/>.
    /// </summary>
    public Video Next => Videos[NextIndex];

    /// <summary>
    /// Gets the previous video in the playlist based on <see cref="PreviousIndex"/>.
    /// </summary>
    public Video Previous => Videos[PreviousIndex];

    /// <summary>
    /// Moves to the next video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    public Video MoveNext
    {
        get
        {
            _currentIndex = NextIndex;
            OnPropertyChanged("CurrentIndex");
            return Videos[CurrentIndex];
        }
    }

    /// <summary>
    /// Moves to the previous video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    public Video MovePrevious
    {
        get
        {
            _currentIndex = PreviousIndex;
            OnPropertyChanged("CurrentIndex");
            return Videos[CurrentIndex];
        }
    }

    /// <summary>
    /// Gets or sets the current video in the playlist.
    /// </summary>
    public Video Current
    {
        get
        {
            return Videos[CurrentIndex];
        }
        set
        {
            Videos[CurrentIndex] = value;
            OnPropertyChanged("Current");
        }
    }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class.
    /// </summary>
    /// <param name="player">The media player used to control video playback.</param>
    public PlaylistView(IPlayer player)
    {
        InitializeComponent();
        _player = player;
        Videos = new ObservableCollection<Video>();
        base.DataContext = this;
        base.ItemsSource = Videos;
        PlayCommand = new RelayCommand<Video>(PlayVideo);
        PauseCommand = new RelayCommand<Video>(PauseVideo);
        RemoveCommand = new RelayCommand<Video>(RemoveVideo);
        MoveToTopCommand = new RelayCommand<Video>(MoveVideoToTop);
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify the UI of property changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Clears all videos from the playlist and unsubscribes from their events.
    /// </summary>
    public void ClearTracks()
    {
        foreach (Video video in Videos)
        {
            video.PositionChanged -= Video_PositionChanged;
        }
        Videos.Clear();
    }

    /// <summary>
    /// Checks if a video is already in the playlist based on its URI.
    /// </summary>
    /// <param name="media">The video to check for.</param>
    /// <returns>True if the video is in the playlist; otherwise, false.</returns>
    public bool Contains(Video media)
    {
        if (media == null)
        {
            return false;
        }
        return Videos.Any((Video item) => item.Uri == media.Uri);
    }

    /// <summary>
    /// Removes a video from the playlist and adjusts the current index if necessary.
    /// </summary>
    /// <param name="media">The video to remove.</param>
    /// <returns>The removed video, or null if the video was not found.</returns>
    public Video Remove(Video media)
    {
        if (media == null)
        {
            return null;
        }
        int index = Videos.ToList().FindIndex((Video video) => video.Uri == media.Uri);
        if (index >= 0)
        {
            Video item = Videos[index];
            item.PositionChanged -= Video_PositionChanged;
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
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Removed video {item.Name} at index {index}");
            return item;
        }
        return null;
    }

    /// <summary>
    /// Clears the current playlist to start a new one.
    /// </summary>
    /// <param name="name">The name of the new playlist (not currently used).</param>
    /// <param name="description">The description of the new playlist (not currently used).</param>
    public void New(string name, string description)
    {
        ClearTracks();
    }

    /// <summary>
    /// Adds a single video to the playlist and sets up its player and event handlers.
    /// </summary>
    /// <param name="media">The video to add.</param>
    public void Add(Video media)
    {
        media.SetPlayer(_player);
        media.PositionChanged += Video_PositionChanged;
        Videos.Add(media);
    }

    /// <summary>
    /// Adds multiple videos to the playlist and sets up their player and event handlers.
    /// </summary>
    /// <param name="medias">The array of videos to add.</param>
    public void Add(Video[] medias)
    {
        foreach (Video media in medias)
        {
            media.SetPlayer(_player);
            media.PositionChanged += Video_PositionChanged;
            Videos.Add(media);
        }
    }

    /// <summary>
    /// Handles position changes in a video and logs the event.
    /// </summary>
    /// <param name="sender">The video that triggered the position change.</param>
    /// <param name="newPosition">The new position of the video playback.</param>
    private void Video_PositionChanged(object sender, double newPosition)
    {
        if (sender is Video video)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Position changed for {video.Name} to {newPosition}");
        }
    }

    /// <summary>
    /// Handles double-click events to play the selected video.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (base.SelectedItem is Video selectedVideo)
        {
            CurrentIndex = Videos.IndexOf(selectedVideo);
            if (Current != selectedVideo)
            {
                Current?.Stop();
            }
            selectedVideo.Play();
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Double-clicked to play video " + selectedVideo.Name);
        }
    }

    /// <summary>
    /// Handles right-click events to show the context menu (currently incomplete).
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void ListView_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            Video selectedVideo = base.SelectedItem as Video;
            if (selectedVideo == null)
            {
            }
        }
    }

    /// <summary>
    /// Plays a video selected from the context menu.
    /// </summary>
    /// <param name="video">The video to play.</param>
    private void PlayVideo(Video video)
    {
        if (video != null)
        {
            CurrentIndex = Videos.IndexOf(video);
            Current?.Stop();
            video.Play();
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Context menu play video " + video.Name);
        }
    }

    /// <summary>
    /// Pauses a video selected from the context menu.
    /// </summary>
    /// <param name="video">The video to pause.</param>
    private void PauseVideo(Video video)
    {
        if (video != null)
        {
            video.Pause();
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Context menu paused video " + video.Name);
        }
    }

    /// <summary>
    /// Removes a video selected from the context menu and shows a confirmation message.
    /// </summary>
    /// <param name="video">The video to remove.</param>
    private void RemoveVideo(Video video)
    {
        if (video != null)
        {
            MessageBox.Show("Removing video");
            Remove(video);
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Context menu removed video " + video.Name);
        }
    }

    /// <summary>
    /// Moves a video selected from the context menu to the top of the playlist.
    /// </summary>
    /// <param name="video">The video to move to the top.</param>
    private void MoveVideoToTop(Video video)
    {
        if (video != null)
        {
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex > 0)
            {
                Videos.RemoveAt(currentIndex);
                Videos.Insert(0, video);
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Moved video " + video.Name + " to top");
            }
        }
    }
}
