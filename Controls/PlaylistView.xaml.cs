// Version: 0.1.6.8
/*using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

using Thmd.Logs;
using Thmd.Media;

namespace Thmd.Controls;

/// <summary>
/// A custom adorner to display a visual representation of a dragged item with a drop shadow effect.
/// </summary>
public class DragAdorner : Adorner
{
    private readonly UIElement _dragElement;
    private readonly double _offsetX;
    private readonly double _offsetY;

    /// <summary>
    /// Initializes a new instance of the <see cref="DragAdorner"/> class.
    /// </summary>
    /// <param name="adornedElement">The element being adorned (the ListView).</param>
    /// <param name="dragElement">The visual representation of the dragged item.</param>
    /// <param name="mousePosition">The initial mouse position relative to the dragElement.</param>
    public DragAdorner(UIElement adornedElement, UIElement dragElement, Point mousePosition)
        : base(adornedElement)
    {
        _dragElement = dragElement;
        _offsetX = mousePosition.X;
        _offsetY = mousePosition.Y;

        // Apply a drop shadow effect to the dragged item
        var shadowEffect = new DropShadowEffect
        {
            Color = Colors.Black,
            Direction = 315,
            ShadowDepth = 5,
            Opacity = 0.5,
            BlurRadius = 10
        };
        _dragElement.Effect = shadowEffect;

        // Set opacity to make the dragged item slightly transparent
        _dragElement.Opacity = 0.8;
    }

    /// <summary>
    /// Updates the position of the adorner based on the mouse position.
    /// </summary>
    /// <param name="position">The current mouse position relative to the adorned element.</param>
    public void UpdatePosition(Point position)
    {
        // Adjust the position to account for where the user clicked on the item
        _dragElement.RenderTransform = new TranslateTransform(position.X, position.Y);// - _offsetX, position.Y - _offsetY);
        InvalidateVisual();
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => _dragElement;

    protected override Size MeasureOverride(Size constraint)
    {
        _dragElement.Measure(constraint);
        return _dragElement.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _dragElement.Arrange(new Rect(finalSize));
        return finalSize;
    }
}

/// <summary>
/// A custom ListView control for managing a playlist of video media items.
/// Provides functionality to play, pause, remove, and reorder videos, with support for
/// user interactions like double-click, right-click context menu actions, and drag-and-drop reordering
/// with a drop shadow effect for the dragged item.
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
    private ObservableCollection<VideoItem> _videos;

    // Stores the item being dragged during a drag-and-drop operation.
    private VideoItem _draggedItem;

    // Tracks whether a drag operation is in progress.
    private bool _isDragging;

    // The adorner used to display the dragged item with a drop shadow.
    private DragAdorner _dragAdorner;

    private object MovingObject = null;

    // Stores the original background of the item being hovered over during drag.
    private Brush _originalBackground;

    /// <summary>
    /// Gets or sets the collection of videos in the playlist and updates the UI.
    /// </summary>
    public ObservableCollection<VideoItem> Videos
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
    public VideoItem Next => Videos[NextIndex];

    /// <summary>
    /// Gets the previous video in the playlist based on <see cref="PreviousIndex"/>.
    /// </summary>
    public VideoItem Previous => Videos[PreviousIndex];

    /// <summary>
    /// Moves to the next video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    public VideoItem MoveNext
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
    public VideoItem MovePrevious
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
    public VideoItem Current
    {
        get
        {
            return Videos[CurrentIndex];
        }
        set
        {
            Videos[CurrentIndex] = value;
            OnPropertyChanged("One");
        }
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

        Videos = new ObservableCollection<VideoItem>();
        base.DataContext = this;
        base.ItemsSource = Videos;
        
        // Enable drag-and-drop
        AllowDrop = true;

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

        foreach (MenuItem menuItem in _rightClickMenu.Items)
        {
            menuItem.CommandParameter = this.SelectedItem as VideoItem;
            if (menuItem.Header.ToString() == "Play")
                menuItem.Click += MenuItemPlay_Click;
            if (menuItem.Header.ToString() == "Remove")
                menuItem.Click += MenuItemRemove_Click;
            if (menuItem.Header.ToString() == "Move to Top")
                menuItem.Click += MenuItemMoveToTop_Click;
            if (menuItem.Header.ToString() == "Move Upper")
                menuItem.Click += MenuItemMoveUpper_Click;
            if (menuItem.Header.ToString() == "Move Lower")
                menuItem.Click += MenuItemMoveLower_Click;
        }

        // Set the context menu for the ListView
        this.ContextMenu = _rightClickMenu;

        // Subscribe to mouse events
        this.MouseDoubleClick += ListView_MouseDoubleClick;
        this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        this.MouseMove += OnMouseMove;
        this.DragEnter += OnDragEnter;
        this.DragOver += OnDragOver;
        this.DragLeave += OnDragLeave;
        this.Drop += OnDrop;
        this.MouseLeave += OnMouseLeave;
    }

    public PlaylistView(IPlayer player)
        : this()
    {
        _player = player;
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
        foreach (VideoItem video in Videos)
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
    public bool Contains(VideoItem media)
    {
        if (media == null)
        {
            return false;
        }
        return Videos.Any((VideoItem item) => item.Uri == media.Uri);
    }

    /// <summary>
    /// Removes a video from the playlist and adjusts the current index if necessary.
    /// </summary>
    /// <param name="media">The video to remove.</param>
    /// <returns>The removed video, or null if the video was not found.</returns>
    public VideoItem Remove(VideoItem media)
    {
        if (media == null)
        {
            return null;
        }
        int index = Videos.ToList().FindIndex((VideoItem video) => video.Uri == media.Uri);
        if (index >= 0)
        {
            VideoItem item = Videos[index];
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
    public void Add(VideoItem media)
    {
        media.SetPlayer(_player);
        media.PositionChanged += Video_PositionChanged;
        Videos.Add(media);
    }

    /// <summary>
    /// Adds multiple videos to the playlist and sets up their player and event handlers.
    /// </summary>
    /// <param name="medias">The array of videos to add.</param>
    public void Add(VideoItem[] medias)
    {
        foreach (VideoItem media in medias)
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
        if (sender is VideoItem video)
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
        if (base.SelectedItem is VideoItem selectedVideo)
        {
            PlayVideo(selectedVideo);
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Double-clicked to play video " + selectedVideo.Name);
        }
    }

    /// <summary>
    /// Plays a video selected from the context menu.
    /// </summary>
    /// <param name="video">The video to play.</param>
    private void PlayVideo(VideoItem video)
    {
        if (video != null)
        {
            Current?.Stop();
            CurrentIndex = Videos.IndexOf(video);
            video.Play();
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Context menu play video " + video.Name);
        }
    }

    /// <summary>
    /// Removes a video selected from the context menu and shows a confirmation message.
    /// </summary>
    /// <param name="video">The video to remove.</param>
    private void RemoveVideo(VideoItem video)
    {
        if (video != null)
        {
            Remove(video);
            Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Context menu removed video " + video.Name);
        }
    }

    /// <summary>
    /// Moves a video selected from the context menu to the top of the playlist.
    /// </summary>
    /// <param name="video">The video to move to the top.</param>
    private void MoveVideoToTop(VideoItem video)
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

    /// <summary>
    /// Handles the PreviewMouseLeftButtonDown event to initiate a drag operation.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse button event arguments.</param>
    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Check if the click is on a ListViewItem
        ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null)
        {
            _draggedItem = listViewItem.DataContext as VideoItem;
            _isDragging = true;
        }
    }

    /// <summary>
    /// Handles the MouseMove event to start the drag operation and display the drag adorner.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedItem != null && e.LeftButton == MouseButtonState.Pressed)
        {
            // Find the ListViewItem being dragged
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem != null)
            {
                // Create a custom DataTemplate for the adorner
                var template = new DataTemplate();
                var gridFactory = new FrameworkElementFactory(typeof(Grid));

                // Column 0: Name
                var nameGridFactory = new FrameworkElementFactory(typeof(Grid));
                nameGridFactory.SetValue(Grid.ColumnProperty, 0);
                nameGridFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 15, 0));
                var nameTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                nameTextBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Medium);
                nameTextBlockFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                nameTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
                nameGridFactory.AppendChild(nameTextBlockFactory);

                // Column 1: PositionFormatted
                var positionBorderFactory = new FrameworkElementFactory(typeof(Border));
                positionBorderFactory.SetValue(Grid.ColumnProperty, 1);
                positionBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(3, 0, 0, 0));
                positionBorderFactory.SetValue(Border.PaddingProperty, new Thickness(5, 0, 0, 0));
                positionBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(255, 255, 165, 0))); // #FFFFA500
                var positionGridFactory = new FrameworkElementFactory(typeof(Grid));
                positionGridFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(15, 0, 15, 0));
                var positionTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                positionTextBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Medium);
                positionTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("PositionFormatted"));
                positionGridFactory.AppendChild(positionTextBlockFactory);
                positionBorderFactory.AppendChild(positionGridFactory);

                // Column 2: StackPanel with video details
                var detailsBorderFactory = new FrameworkElementFactory(typeof(Border));
                detailsBorderFactory.SetValue(Grid.ColumnProperty, 2);
                detailsBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(3, 0, 0, 0));
                detailsBorderFactory.SetValue(Border.PaddingProperty, new Thickness(5, 0, 0, 0));
                detailsBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(255, 255, 165, 0))); // #FFFFA500
                var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                stackPanelFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(15, 0, 15, 0));

                var durationTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                durationTextBlockFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                durationTextBlockFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Red);
                durationTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Duration"));
                stackPanelFactory.AppendChild(durationTextBlockFactory);

                var fpsTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                fpsTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Fps"));
                stackPanelFactory.AppendChild(fpsTextBlockFactory);

                var frameSizeTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                frameSizeTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("FrameSize"));
                stackPanelFactory.AppendChild(frameSizeTextBlockFactory);

                var formatTextBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                formatTextBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("Format"));
                stackPanelFactory.AppendChild(formatTextBlockFactory);

                detailsBorderFactory.AppendChild(stackPanelFactory);

                // Add all columns to the grid
                gridFactory.AppendChild(nameGridFactory);
                gridFactory.AppendChild(positionBorderFactory);
                gridFactory.AppendChild(detailsBorderFactory);

                template.VisualTree = gridFactory;

                // Create a Grid instance to set ColumnDefinitions
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Create the ContentPresenter with the custom template
                var contentPresenter = new ContentPresenter
                {
                    Content = _draggedItem,
                    ContentTemplate = template
                };

                // Measure and arrange the content presenter to get its size
                contentPresenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                contentPresenter.Arrange(new Rect(contentPresenter.DesiredSize));

                // Create the adorner
                var mousePosition = e.GetPosition(this);
                _dragAdorner = new DragAdorner(this, contentPresenter, mousePosition);
                
                AdornerLayer.GetAdornerLayer(this).Add(_dragAdorner);
                _dragAdorner.UpdatePosition(e.GetPosition(this));

                // Start the drag-and-drop operation
                DataObject data = new DataObject(typeof(VideoItem), _draggedItem);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                
                // Clean up the adorner after the drag operation
                if (_dragAdorner != null)
                {
                    AdornerLayer.GetAdornerLayer(this).Remove(_dragAdorner);
                    _dragAdorner = null;
                }
            }
            _isDragging = false; // Reset dragging state
        }
    }

    /// <summary>
    /// Handles the DragEnter event to specify allowed drop effects.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The drag event arguments.</param>
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        ListViewItem mouseOverItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (mouseOverItem != null)
        {
            _originalBackground = mouseOverItem.Background;
            mouseOverItem.Background = Brushes.LightBlue;
            Logger.Log.Log(LogLevel.Debug, new string[2] { "Console", "File" }, $"PlaylistView: Drag entered over video {(mouseOverItem.DataContext as VideoItem)?.Name}");
        }

        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;

        // Update the adorner position if dragging
        if (_dragAdorner != null)
        {
            _dragAdorner.UpdatePosition(e.GetPosition(this));
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        ListViewItem mouseOverItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (mouseOverItem != null && _originalBackground != null)
        {
            mouseOverItem.Background = _originalBackground;
            Logger.Log.Log(LogLevel.Debug, new string[2] { "Console", "File" }, $"PlaylistView: Drag left video {(mouseOverItem.DataContext as VideoItem)?.Name}");
        }
    }

    /// <summary>
    /// Handles the DragOver event to update the adorner position.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The drag event arguments.</param>
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (_dragAdorner != null)
        {
            _dragAdorner.UpdatePosition(e.GetPosition(this));
        }
        e.Handled = true;
    }

    /// <summary>
    /// Handles the Drop event to reorder the playlist by shifting items based on the drop position.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The drag event arguments.</param>
    private void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            // Obs�uga upuszczenia pliku
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string filePath in files)
                {
                    // Sprawd�, czy plik ma poprawne rozszerzenie
                    if (IsSupportedMediaFile(filePath))
                    {
                        VideoItem newVideo = new VideoItem(filePath);
                        newVideo.SetPlayer(_player);
                        Videos.Add(newVideo);
                        Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"Added video to playlist: {newVideo.Name}");
                    }
                    else
                    {
                        Logger.Log.Log(LogLevel.Warning, new string[2] { "Console", "File" }, $"Unsupported file format: {filePath}");
                    }
                }
                e.Handled = true;
                return;
            }

            // Istniej�ca logika dla przesuwania element�w w li�cie
            if (e.Data.GetDataPresent(typeof(VideoItem)))
            {
                VideoItem droppedVideo = e.Data.GetData(typeof(VideoItem)) as VideoItem;
                var targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (targetItem != null)
                {
                    VideoItem targetVideo = targetItem.DataContext as VideoItem;
                    int oldIndex = Videos.IndexOf(droppedVideo);
                    int newIndex = Videos.IndexOf(targetVideo);

                    if (oldIndex != newIndex)
                    {
                        Videos.RemoveAt(oldIndex);
                        if (oldIndex < newIndex)
                        {
                            newIndex--;
                        }
                        Videos.Insert(newIndex, droppedVideo);
                        Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Shifted video {droppedVideo.Name} from index {oldIndex} to {newIndex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log.Log(LogLevel.Error, new string[2] { "Console", "File" }, $"Error during drop operation: {ex.Message}");
        }
        finally
        {
            _isDragging = false;
            _draggedItem = null;
            if (_dragAdorner != null)
            {
                AdornerLayer.GetAdornerLayer(this).Remove(_dragAdorner);
                _dragAdorner = null;
            }
            e.Handled = true;
        }
    }

    private bool IsSupportedMediaFile(string filePath)
    {
        string[] supportedExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".flv", ".wmv" };
        string extension = System.IO.Path.GetExtension(filePath)?.ToLower();
        return supportedExtensions.Contains(extension);
    }

    /// <summary>
    /// Handles the MouseLeave event to reset the dragging state and remove the adorner.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _isDragging = false;
        _draggedItem = null;
        // Clean up the adorner
        if (_dragAdorner != null)
        {
            AdornerLayer.GetAdornerLayer(this).Remove(_dragAdorner);
            _dragAdorner = null;
        }
    }

    /// <summary>
    /// Helper method to find the ancestor of a given type for a DependencyObject.
    /// </summary>
    /// <typeparam name="T">The type of the ancestor to find.</typeparam>
    /// <param name="current">The starting DependencyObject.</param>
    /// <returns>The ancestor of type T, or null if not found.</returns>
    private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null && !(current is T))
        {
            if (current is Visual)
            {
                current = VisualTreeHelper.GetParent(current);
            }
            else
            {
                current = LogicalTreeHelper.GetParent(current);
            }
        }
        return current as T;
    }

    private void MenuItemPlay_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedItem != null)
            PlayVideo((this.SelectedItem as VideoItem));
    }

    private void MenuItemMoveToTop_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedItem != null)
            MoveVideoToTop((this.SelectedItem as VideoItem));
    }

    private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedItem != null)
            RemoveVideo((this.SelectedItem as VideoItem));
    }

    private void MenuItemMoveUpper_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedItem != null)
        {
            VideoItem video = this.SelectedItem as VideoItem;
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex > 0)
            {
                Videos.RemoveAt(currentIndex);
                Videos.Insert(currentIndex - 1, video);
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Moved video " + video.Name + " upper");
            }
        }
    }

    private void MenuItemMoveLower_Click(object sender, RoutedEventArgs e)
    {
        if (this.SelectedItem != null)
        {
            VideoItem video = this.SelectedItem as VideoItem;
            int currentIndex = Videos.IndexOf(video);
            if (currentIndex < Videos.Count - 1)
            {
                Videos.RemoveAt(currentIndex);
                Videos.Insert(currentIndex + 1, video);
                Logger.Log.Log(LogLevel.Info, new string[2] { "Console", "File" }, "PlaylistView: Moved video " + video.Name + " lower");
            }
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        //this.Items.CurrentChanged += Items_CurrentChanged;
        base.OnMouseDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            MovingObject = sender;
        }
    }

    private void Items_CurrentChanged(object sender, EventArgs e)
    {
        
    }

    /*protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            // Package the data.
            DataObject data = new DataObject();
            //data.SetData(DataFormats.StringFormat, circleUI.Fill.ToString());
            //data.SetData("Double", circleUI.Height);
            data.SetData("Object", this);

            // Initiate the drag-and-drop operation.
            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
    }

    protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
    {
        base.OnGiveFeedback(e);
        // These Effects values are set in the drop target's
        // DragOver event handler.
        if (e.Effects.HasFlag(DragDropEffects.Copy))
        {
            Mouse.SetCursor(Cursors.Cross);
        }
        else if (e.Effects.HasFlag(DragDropEffects.Move))
        {
            Mouse.SetCursor(Cursors.Pen);
        }
        else
        {
            Mouse.SetCursor(Cursors.No);
        }
        e.Handled = true;
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);

        // If the DataObject contains string data, extract it.
        if (e.Data.GetDataPresent(DataFormats.StringFormat))
        {
            string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

            // If the string can be converted into a Brush,
            // convert it and apply it to the ellipse.
            BrushConverter converter = new BrushConverter();
            if (converter.IsValid(dataString))
            {
                Brush newFill = (Brush)converter.ConvertFromString(dataString);
                //circleUI.Fill = newFill;

                // Set Effects to notify the drag source what effect
                // the drag-and-drop operation had.
                // (Copy if CTRL is pressed; otherwise, move.)
                if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }
        e.Handled = true;
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        e.Effects = DragDropEffects.None;

        // If the DataObject contains string data, extract it.
        if (e.Data.GetDataPresent(DataFormats.StringFormat))
        {
            string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

            // If the string can be converted into a Brush, allow copying or moving.
            BrushConverter converter = new BrushConverter();
            if (converter.IsValid(dataString))
            {
                // Set Effects to notify the drag source what effect
                // the drag-and-drop operation will have. These values are
                // used by the drag source's GiveFeedback event handler.
                // (Copy if CTRL is pressed; otherwise, move.)
                if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }
        e.Handled = true;
    }*/
/*}
*/
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using Thmd.Logs;
using Thmd.Media;

namespace Thmd.Controls;

/// <summary>
/// A custom adorner to display a visual representation of a dragged item with a drop shadow effect.
/// </summary>
public class DragAdorner : Adorner
{
    private readonly UIElement _dragElement;
    private readonly double _offsetX;
    private readonly double _offsetY;

    /// <summary>
    /// Initializes a new instance of the <see cref="DragAdorner"/> class.
    /// </summary>
    /// <param name="adornedElement">The element being adorned (the ListView).</param>
    /// <param name="dragElement">The visual representation of the dragged item.</param>
    /// <param name="mousePosition">The initial mouse position relative to the dragElement.</param>
    public DragAdorner(UIElement adornedElement, UIElement dragElement, Point mousePosition)
        : base(adornedElement)
    {
        _dragElement = dragElement;
        _offsetX = mousePosition.X;
        _offsetY = mousePosition.Y;

        // Apply a drop shadow effect to the dragged item
        var shadowEffect = new DropShadowEffect
        {
            Color = Colors.Black,
            Direction = 315,
            ShadowDepth = 5,
            Opacity = 0.5,
            BlurRadius = 10
        };
        _dragElement.Effect = shadowEffect;

        // Set opacity to make the dragged item slightly transparent
        _dragElement.Opacity = 0.8;

        // Ensure the adorner layer is clipped if needed
        IsClipEnabled = true;
    }

    /// <summary>
    /// Updates the position of the adorner based on the mouse position.
    /// </summary>
    /// <param name="position">The current mouse position relative to the adorned element.</param>
    public void UpdatePosition(Point position)
    {
        // Adjust the position to account for where the user clicked on the item
        _dragElement.RenderTransform = new TranslateTransform(position.X - _offsetX, position.Y - _offsetY);
        InvalidateVisual();
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => _dragElement;

    protected override Size MeasureOverride(Size constraint)
    {
        _dragElement.Measure(constraint);
        return _dragElement.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _dragElement.Arrange(new Rect(finalSize));
        return finalSize;
    }
}

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
    private ObservableCollection<VideoItem> _videos;

    // Stores the item being dragged during a drag-and-drop operation.
    private VideoItem _draggedItem;

    // Tracks whether a drag operation is in progress.
    private bool _isDragging;

    // The adorner used to display the dragged item with a drop shadow.
    private DragAdorner _dragAdorner;

    // Stores the original background of the item being hovered over during drag.
    private Brush _originalBackground;

    // Minimum distance to start drag (to avoid accidental drags)
    private const double DragThreshold = 10.0;

    // Point where the mouse was pressed down
    private Point _startPoint;

    /// <summary>
    /// Gets or sets the collection of videos in the playlist and updates the UI.
    /// </summary>
    public ObservableCollection<VideoItem> Videos
    {
        get => _videos;
        set
        {
            _videos = value;
            Dispatcher.Invoke(() =>
            {
                base.ItemsSource = _videos;
                OnPropertyChanged("Videos");
            });
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

        Videos = new ObservableCollection<VideoItem>();
        base.DataContext = this;
        base.ItemsSource = Videos;

        // Enable drag-and-drop
        AllowDrop = true;

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
        this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        this.MouseMove += OnMouseMove;
        this.DragEnter += OnDragEnter;
        this.DragOver += OnDragOver;
        this.DragLeave += OnDragLeave;
        this.Drop += OnDrop;
        this.MouseLeave += OnMouseLeave;
    }

    public PlaylistView(IPlayer player) : this()
    {
        _player = player;
    }

    public void SetPlayer(IPlayer player)
    {
        _player = player;
        /*if (Videos != null)
        {
            foreach (var video in Videos)
            {
                video.SetPlayer(_player);
            }
        }*/
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
            Dispatcher.Invoke(() => Videos.Add(media));
            Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Added video {media.Name}");
        });
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

    /// <summary>
    /// Handles the PreviewMouseLeftButtonDown event to initiate a drag operation.
    /// </summary>
    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null)
        {
            _draggedItem = listViewItem.DataContext as VideoItem;
        }
    }

    /// <summary>
    /// Handles the MouseMove event to start the drag operation and display the drag adorner.
    /// </summary>
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null) return;

        Point currentPoint = e.GetPosition(this);
        Vector offset = currentPoint - _startPoint;
        if (Math.Abs(offset.X) < DragThreshold && Math.Abs(offset.Y) < DragThreshold) return;

        ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(listViewItem);

            Image dragImage = new Image
            {
                Source = rtb,
                Width = listViewItem.ActualWidth,
                Height = listViewItem.ActualHeight,
                Opacity = 0.8
            };

            var shadowEffect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 5,
                Opacity = 0.5,
                BlurRadius = 10
            };
            dragImage.Effect = shadowEffect;

            _dragAdorner = new DragAdorner(this, dragImage, _startPoint);
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer != null)
            {
                adornerLayer.Add(_dragAdorner);
            }

            DataObject data = new DataObject(typeof(VideoItem), _draggedItem);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);

            if (adornerLayer != null && _dragAdorner != null)
            {
                adornerLayer.Remove(_dragAdorner);
            }
            _dragAdorner = null;
        }
        _isDragging = false;
        _draggedItem = null;
    }

    /// <summary>
    /// Handles the DragEnter event to specify allowed drop effects and highlight drop target.
    /// </summary>
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        ListViewItem targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (targetItem != null)
        {
            _originalBackground = targetItem.Background;
            targetItem.Background = Brushes.LightBlue;
        }

        if (_dragAdorner != null)
        {
            _dragAdorner.UpdatePosition(e.GetPosition(this));
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles the DragLeave event to restore original background.
    /// </summary>
    private void OnDragLeave(object sender, DragEventArgs e)
    {
        ListViewItem targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (targetItem != null && _originalBackground != null)
        {
            targetItem.Background = _originalBackground;
        }

        if (_dragAdorner != null)
        {
            _dragAdorner.UpdatePosition(e.GetPosition(this));
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles the DragOver event to update the adorner position.
    /// </summary>
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (_dragAdorner != null)
        {
            _dragAdorner.UpdatePosition(e.GetPosition(this));
        }
        e.Handled = true;
    }

    /// <summary>
    /// Handles the Drop event to reorder the playlist or add files in a background task.
    /// </summary>
    private async void OnDrop(object sender, DragEventArgs e)
    {
        Point dropPosition = e.GetPosition(this);

        // Handle file drop
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string filePath in files)
            {
                if (IsSupportedMediaFile(filePath))
                {
                    VideoItem newVideo = new VideoItem(filePath);
                    await AddAsync(newVideo);
                    int insertIndex = await Dispatcher.InvokeAsync(() => GetInsertIndex(dropPosition));
                    await Dispatcher.InvokeAsync(() => Videos.Move(Videos.IndexOf(newVideo), insertIndex));
                    Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Added video {newVideo.Name} at index {insertIndex}");
                }
                else
                {
                    Logger.Log.Log(Thmd.Logs.LogLevel.Warning, new string[2] { "Console", "File" }, $"Unsupported file format: {filePath}");
                }
            }
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        // Handle item reorder
        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            VideoItem droppedVideo = (VideoItem)e.Data.GetData(typeof(VideoItem));
            if (droppedVideo != null && Videos.Contains(droppedVideo))
            {
                ListViewItem targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                int oldIndex = Videos.IndexOf(droppedVideo);
                int newIndex = oldIndex;

                if (targetItem != null)
                {
                    VideoItem targetVideo = targetItem.DataContext as VideoItem;
                    newIndex = Videos.IndexOf(targetVideo);
                }
                else
                {
                    newIndex = await Dispatcher.InvokeAsync(() => GetInsertIndex(dropPosition));
                }

                if (oldIndex != newIndex)
                {
                    await Dispatcher.InvokeAsync(() => Videos.Move(oldIndex, newIndex));
                    Logger.Log.Log(Thmd.Logs.LogLevel.Info, new string[2] { "Console", "File" }, $"PlaylistView: Moved video {droppedVideo.Name} from {oldIndex} to {newIndex}");
                }
            }
            e.Effects = DragDropEffects.Move;
        }

        ListViewItem targetItemRestore = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (targetItemRestore != null && _originalBackground != null)
        {
            targetItemRestore.Background = _originalBackground;
        }

        _isDragging = false;
        _draggedItem = null;
        if (_dragAdorner != null)
        {
            AdornerLayer.GetAdornerLayer(this)?.Remove(_dragAdorner);
            _dragAdorner = null;
        }
        e.Handled = true;
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
        if (_dragAdorner != null)
        {
            AdornerLayer.GetAdornerLayer(this)?.Remove(_dragAdorner);
            _dragAdorner = null;
        }
        if (_originalBackground != null)
        {
            _originalBackground = null;
        }
    }

    /// <summary>
    /// Helper method to find the ancestor of a given type for a DependencyObject.
    /// </summary>
    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null && !(current is T))
        {
            if (current is Visual)
            {
                current = VisualTreeHelper.GetParent(current);
            }
            else
            {
                current = LogicalTreeHelper.GetParent(current);
            }
        }
        return current as T;
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
}
