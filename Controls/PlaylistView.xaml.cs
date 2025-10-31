// Version: 0.1.13.83
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Thmd.Configuration;
using Thmd.Consolas;
using Thmd.Controls.Effects;
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
    #region Private Fields

    private int _currentIndex = 0;
    private IPlayer _player;
    private ContextMenu _rightClickMenu;
    private ObservableCollection<VideoItem> _videos = new ObservableCollection<VideoItem>();

    private bool _isDragging;
    private VideoItem _draggedItem;
    private int _draggedIndex = -1;
    private Point _startPoint;

    private AdornerLayer _adornerLayer;
    private DragShadowAdorner _dragAdorner;

    private ScrollViewer _scrollViewer;
    private DispatcherTimer _scrollTimer;
    private double _scrollVelocity = 0;
    private const double ScrollZone = 60.0;
    private const double MaxScrollSpeed = 14.0;
    private const double ScrollDamping = 0.85;

    private ScrollIndicatorAdorner _topIndicator;
    private ScrollIndicatorAdorner _bottomIndicator;

    private ListViewItem _draggedContainer;  // Fix: Przechowuj referencję do kontenera dla OnDragOver

    private bool _isMouseSubscribed;  // Fix: Flaga dla subskrypcji eventu

    private Configuration.Config _config = new Thmd.Configuration.Config();

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to add a new video to the playlist.
    /// </summary>
    public ICommand AddCommand { get; private set; }

    /// <summary>
    /// Gets the command to remove the selected video from the playlist.
    /// </summary>
    public ICommand RemoveCommand { get; private set; }

    /// <summary>
    /// Gets the command to edit the selected video properties.
    /// </summary>
    public ICommand EditCommand { get; private set; }

    /// <summary>
    /// Gets the command to close or hide the playlist view.
    /// </summary>
    public ICommand CloseCommand { get; private set; }

    /// <summary>
    /// Gets the command to save the current playlist to configuration.
    /// </summary>
    public ICommand SavePlaylistCommand { get; private set; }

    /// <summary>
    /// Gets the command to load a playlist from configuration.
    /// </summary>
    public ICommand LoadPlaylistCommand { get; private set; }

    /// <summary>
    /// Gets the command to clear all videos from the playlist.
    /// </summary>
    public ICommand ClearPlaylistCommand { get; private set; }

    #endregion

    #region Public properties

    /// <summary>
    /// Config
    /// </summary>
    public Configuration.IPlaylistConfig Configuration { get => _config.PlaylistConfig; private set { 
            _config.PlaylistConfig = value;
            OnPropertyChanged(nameof(Configuration));
        } }

    /// <summary>
    /// Gets or sets the collection of videos in the playlist and updates the UI.
    /// </summary>
    /// <value>
    /// The observable collection of <see cref="VideoItem"/> objects representing the playlist.
    /// Updating this property refreshes the ListView's ItemsSource via Dispatcher for thread safety.
    /// </value>
    public ObservableCollection<VideoItem> Videos
    {
        get => _videos;
        set
        {
            Dispatcher.InvokeAsync(() =>
            {
                _videos = value;
                base.ItemsSource = _videos; ;
                //SetValue(VideosProperty, value);
                OnPropertyChanged(nameof(Videos));
            });
        }
    }

    /// <summary>
    /// Dependency property for the Videos collection, enabling binding and style triggers in XAML.
    /// </summary>
    public static readonly DependencyProperty VideosProperty =
        DependencyProperty.Register("Videos", typeof(ObservableCollection<VideoItem>), typeof(PlaylistView),
            new PropertyMetadata(new ObservableCollection<VideoItem>(), OnVideosChanged));

    /// <summary>
    /// Callback invoked when the Videos dependency property changes.
    /// Updates the ListView's ItemsSource to reflect the new collection.
    /// </summary>
    /// <param name="d">The DependencyObject (PlaylistView instance) where the property changed.</param>
    /// <param name="e">Event arguments containing the old and new values.</param>
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
    /// <value>
    /// The zero-based index of the currently playing video. Automatically clamps to valid range [0, Count-1].
    /// </value>
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
    /// <value>
    /// The zero-based index of the next video, wrapping around cyclically.
    /// </value>
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
    /// <value>
    /// The zero-based index of the previous video, wrapping around cyclically.
    /// </value>
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
    /// <value>
    /// The <see cref="VideoItem"/> at the next index, or null if playlist is empty.
    /// </value>
    public VideoItem Next => Videos != null && NextIndex >= 0 ? Videos[NextIndex] : null;

    /// <summary>
    /// Gets the previous video in the playlist based on <see cref="PreviousIndex"/>.
    /// </summary>
    /// <value>
    /// The <see cref="VideoItem"/> at the previous index, or null if playlist is empty.
    /// </value>
    public VideoItem Previous => Videos != null && PreviousIndex >= 0 ? Videos[PreviousIndex] : null;

    /// <summary>
    /// Moves to the next video in the playlist and returns it.
    /// Updates <see cref="CurrentIndex"/> and notifies the UI of the change.
    /// </summary>
    /// <value>
    /// The <see cref="VideoItem"/> now at the current index after moving forward, or null if empty.
    /// </value>
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
    /// <value>
    /// The <see cref="VideoItem"/> now at the current index after moving backward, or null if empty.
    /// </value>
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
    /// <value>
    /// The <see cref="VideoItem"/> at <see cref="CurrentIndex"/>, or null if invalid index.
    /// Setting this replaces the item at the current index and notifies the UI.
    /// </value>
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
    /// Gets the total number of videos in the playlist.
    /// </summary>
    /// <value>
    /// The count of items in the <see cref="Videos"/> collection, or 0 if null.
    /// </value>
    public int Count
    {
        get => Videos != null ? Videos.Count : 0;
    }

    /// <summary>
    /// Occurs when a property value changes, used for data binding.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class.
    /// </summary>
    /// <remarks>
    /// Sets up the DataContext, ItemsSource, drag-and-drop support, commands, context menu,
    /// and event handlers for interactions like double-click and mouse events.
    /// </remarks>
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class.
    /// Configures drag-and-drop, context menu, and auto-scroll timer.
    /// </summary>
    public PlaylistView()
    {
        InitializeComponent();

        InitCommands();

        DataContext = this;
        ItemsSource = Videos;
        AllowDrop = true;

        // Włączenie wirtualizacji UI dla ListView (domyślnie włączone, ale jawnie ustawione dla .NET 4.8)
        // Używa VirtualizingStackPanel jako panel domyślny dla ListView
        this.SetValue(VirtualizingPanel.IsVirtualizingProperty, true);
        this.SetValue(VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
        this.SetValue(ScrollViewer.CanContentScrollProperty, true);
        // Ustawienie ScrollViewer.CanContentScroll na true dla wsparcia wirtualizacji
        if (this.Template.FindName("PART_ScrollViewer", this) is ScrollViewer scrollViewer)
        {
            scrollViewer.CanContentScroll = true;
        }

        CreateContextMenu();

        _scrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        _scrollTimer.Tick += OnScrollTimerTick;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class with a media player reference.
    /// </summary>
    /// <param name="player">The <see cref="IPlayer"/> instance to associate for playback control.</param>
    /// <remarks>
    /// Calls the parameterless constructor and sets the player reference.
    /// </remarks>
    public PlaylistView(IPlayer player) : this()
    {
        _player = player;
    }

    #endregion

    #region Context Menu Setup

    /// <summary>
    /// Creates and assigns the right-click context menu for playlist actions.
    /// </summary>
    private void CreateContextMenu()
    {
        _rightClickMenu = new ContextMenu();

        MenuItem playItem = new MenuItem { Header = "Play" };
        MenuItem removeItem = new MenuItem { Header = "Remove" };
        MenuItem moveUpItem = new MenuItem { Header = "Move Up" };
        MenuItem moveDownItem = new MenuItem { Header = "Move Down" };
        MenuItem moveTopItem = new MenuItem { Header = "Move To Top" };

        playItem.Click += MenuItemPlay_Click;
        removeItem.Click += MenuItemRemove_Click;
        moveUpItem.Click += MenuItemMoveUpper_Click;
        moveDownItem.Click += MenuItemMoveLower_Click;
        moveTopItem.Click += MenuItemMoveToTop_Click;

        _rightClickMenu.Items.Add(playItem);
        _rightClickMenu.Items.Add(removeItem);
        _rightClickMenu.Items.Add(new Separator());
        _rightClickMenu.Items.Add(moveUpItem);
        _rightClickMenu.Items.Add(moveDownItem);
        _rightClickMenu.Items.Add(moveTopItem);

        ContextMenu = _rightClickMenu;
    }

    #endregion

    #region Context Menu Actions

    private void MenuItemPlay_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is VideoItem video)
        {
            _player?.Play(video);
            CurrentIndex = Videos.IndexOf(video);
        }
    }

    private void MenuItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is VideoItem video)
            Videos.Remove(video);
    }

    private void MenuItemMoveUpper_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is VideoItem video)
        {
            int index = Videos.IndexOf(video);
            if (index > 0)
                Videos.Move(index, index - 1);
        }
    }

    private void MenuItemMoveLower_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is VideoItem video)
        {
            int index = Videos.IndexOf(video);
            if (index < Videos.Count - 1)
                Videos.Move(index, index + 1);
        }
    }

    private void MenuItemMoveToTop_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItem is VideoItem video)
        {
            int index = Videos.IndexOf(video);
            if (index > 0)
                Videos.Move(index, 0);
        }
    }

    #endregion

    #region Drag-and-Drop Logic

    /// <summary>
    /// Captures initial mouse position when user presses the left button.
    /// </summary>
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        _startPoint = e.GetPosition(this);
    }

    /// <summary>
    /// Detects drag initiation and begins drag-and-drop operation when movement exceeds threshold.
    /// Ignores border or empty area clicks (drag only starts on ListViewItem).
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            Point pos = e.GetPosition(this);
            Vector diff = _startPoint - pos;

            if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // 🔹 Sprawdź, czy mysz znajduje się nad elementem listy
                var sourceElement = e.OriginalSource as DependencyObject;
                var listViewItem = FindAncestor<ListViewItem>(sourceElement);

                // Jeśli nie kliknięto w element listy (np. border, tło, puste miejsce) → nie zaczynaj drag
                if (listViewItem == null)
                    return;

                if (SelectedItem is VideoItem item)
                {
                    _isDragging = true;
                    _draggedItem = item;
                    _draggedIndex = Items.IndexOf(item);
                    StartDrag(item, e);
                }
            }
        }
    }


    /// <summary>
    /// Handles the MouseLeave event to reset the dragging state and remove the adorner.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    protected void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDragging)
        {
            _isDragging = false;
        }
        _draggedItem = null;
        _draggedIndex = -1;

        Config.Instance.PlaylistConfig.Size = new Size(this.Width, this.Height);
        //Config.Instance.PlaylistConfig.Position = new Point(0, 0);
        EndDrag();
    }

    /// <summary>
    /// Starts drag operation and creates a visual shadow (adorner) for the dragged element.
    /// </summary>
    /// <summary>
    /// Startuje drag z widocznym cieniem via snapshot bitmapy (fix dla niewidocznego adornera).
    /// </summary>
    private void StartDrag(VideoItem item, MouseEventArgs e)
    {
        // Fix: Sprawdź UI thread i STA
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => StartDrag(item, e));  // Rekursja do UI thread
            return;
        }
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            this.WriteLine(new InvalidOperationException("DragDrop wymaga STA threada."));
        }

        _adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (_adornerLayer == null) return;

        var draggedContainer = ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
        if (draggedContainer == null)
        {
            ScrollIntoView(item);
            UpdateLayout();
            draggedContainer = ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
            if (draggedContainer == null) return;
        }

        _draggedContainer = draggedContainer;  // Przechowaj dla OnDragOver

        // Snapshot wizualny
        /*RenderTargetBitmap bitmap = new RenderTargetBitmap(
            (int)draggedContainer.ActualWidth,
            (int)draggedContainer.ActualHeight,
            96, 96,
            PixelFormats.Pbgra32);
        draggedContainer.UpdateLayout();
        bitmap.Render(draggedContainer);

        var shadowImage = new Image
        {
            Source = bitmap,
            Opacity = 0.7,
            RenderTransform = new TranslateTransform(0, 0)
        };

        _dragAdorner = new DragShadowAdorner(this, shadowImage);
        _adornerLayer.Add(_dragAdorner);*/

        // Fix: Subskrybuj globalny Mouse.Move dla ciągłego śledzenia (płynność)
        //if (!_isMouseSubscribed)
        //{
        //    this.MouseMove += OnGlobalMouseMove;  // Globalny event – działa w całej app
        //    _isMouseSubscribed = true;
        //}

        // Fix: IDataObject z ComTypes dla kompatybilności OLE (zapobiega null w OleDoDragDrop)
        var dataObject = new DataObject();
        dataObject.SetData(typeof(VideoItem), item);  // Lub użyj string key: dataObject.SetData("VideoItem", item);

        try
        {
            // Fix: Wywołaj w Dispatcher dla bezpieczeństwa (choć już w UI)
            Dispatcher.Invoke(() =>
            {
                DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Move);
            });
        }
        catch (NullReferenceException ex) when (ex.Message.Contains("OleDoDragDrop"))  // Catch specyficzny
        {
            // Graceful handling: Log lub fallback (np. anuluj drag)
            this.WriteLine($"Drag-drop crash: {ex}");
            // Opcjonalnie: MessageBox.Show("Błąd drag-drop: Operacja anulowana.");
        }
        finally
        {
            StopAutoScroll();
            EndDrag();
        }
    }

    /// <summary>
    /// Updates the adorner position and triggers auto-scroll when near list edges.
    /// </summary>
    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);

        if (_dragAdorner != null && _draggedContainer != null)
        {
            // Fix: Użyj e.GetPosition(this) dla spójności z globalnym eventem
            Point position = e.GetPosition(this);
            double offsetX = position.X - (_draggedContainer.ActualWidth / 2);
            double offsetY = position.Y - (_draggedContainer.ActualHeight / 2);
            _dragAdorner.Offset = new Point(offsetX, offsetY);
            _dragAdorner.InvalidateArrange();
            _dragAdorner.InvalidateVisual();
        }

        // Fix: Aktualizuj IsDragOver dla itemu pod myszą (wizualizacja drop targetu)
        int targetIndex = GetIndexAtPosition(e.GetPosition(this));
        if (targetIndex >= 0)
        {
            var targetItem = ItemContainerGenerator.ContainerFromIndex(targetIndex) as DependencyObject;
            DragDropHelper.SetIsDragOver(targetItem, true);
        }

        CheckAutoScroll(e.GetPosition(this));
    }

    /// <summary>
    /// Handles item drop and updates the order of playlist items.
    /// </summary>
    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);

        // Fix: Sprawdź null przed dostępem i zresetuj IsDragOver
        StopAutoScroll();
        if (e.Data.GetDataPresent(typeof(VideoItem)))
        {
            if (e.Data.GetData(typeof(VideoItem)) is VideoItem droppedItem)
            {
                int oldIndex = Videos.IndexOf(droppedItem);
                Point dropPosition = e.GetPosition(this);
                int newIndex = GetIndexAtPosition(dropPosition);

                if (newIndex < 0) newIndex = Videos.Count - 1;
                if (newIndex != oldIndex)
                {
                    Videos.Move(oldIndex, newIndex);
                    CurrentIndex = newIndex;
                }
            }
        }

        // Fix: Reset attached property dla wszystkich itemów (zapobiega "zablokowanym" stanom)
        foreach (var container in ItemContainerGenerator.Items)
        {
            var itemContainer = ItemContainerGenerator.ContainerFromItem(container) as DependencyObject;
            if (itemContainer != null)
            {
                Thmd.Utilities.DragDropHelper.SetIsDragOver(itemContainer, false);
            }
        }

        EndDrag();
    }

    /// <summary>
    /// Clears drag state and removes adorner after drop is complete.
    /// </summary>
    private void EndDrag()
    {
        _isDragging = false;
        _draggedItem = null;
        _draggedIndex = -1;
        _draggedContainer = null;

        // Fix: Odsubskrybuj Mouse.Move, by uniknąć leaków i niepotrzebnych update'ów
        if (_isMouseSubscribed)
        {
            this.MouseMove -= OnGlobalMouseMove;
            _isMouseSubscribed = false;
        }

        if (_dragAdorner != null && _adornerLayer != null)
        {
            _adornerLayer.Remove(_dragAdorner);
            _dragAdorner = null;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method – finds ancestor of given type in visual tree.
    /// </summary>
    private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T target)
                return target;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGlobalMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _dragAdorner != null && _draggedContainer != null)
        {
            // Fix: Pobierz pozycję globalnie via Mouse.GetPosition(this) – dokładna dla ListView
            Point position = e.GetPosition(this);

            // Centrowanie: Odejmij środek kontenera dla "podążania" za myszą
            double offsetX = position.X - (_draggedContainer.ActualWidth/2);
            double offsetY = position.Y - (_draggedContainer.ActualHeight/2);

            _dragAdorner.Offset = new Point(offsetX/2, offsetY/2);

            // Fix: Wymuś redraw dla natychmiastowego efektu (płynność w .NET 4.8)
            _dragAdorner.InvalidateArrange();
            _dragAdorner.InvalidateVisual();
        }
    }

    /// <summary>
    /// Gets the index of the item at a specific mouse position.
    /// </summary>
    private int GetIndexAtPosition(Point position)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            var item = (ListViewItem)ItemContainerGenerator.ContainerFromIndex(i);
            if (item == null) continue;

            Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
            Point topLeft = item.TranslatePoint(new Point(0, 0), this);

            if (position.Y >= topLeft.Y && position.Y <= topLeft.Y + bounds.Height)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Notifies the UI when a property value changes.
    /// </summary>
    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void ShowIndicator(bool top)
    {
        if (_adornerLayer == null)
            _adornerLayer = AdornerLayer.GetAdornerLayer(this);
        if (_adornerLayer == null)
            return;

        if (top)
        {
            if (_topIndicator == null)
            {
                _topIndicator = new ScrollIndicatorAdorner(this, true);
                _adornerLayer.Add(_topIndicator);
            }
            _topIndicator.FadeIn();
        }
        else
        {
            if (_bottomIndicator == null)
            {
                _bottomIndicator = new ScrollIndicatorAdorner(this, false);
                _adornerLayer.Add(_bottomIndicator);
            }
            _bottomIndicator.FadeIn();
        }
    }

    private void HideIndicator(bool top)
    {
        if (top)
        {
            _topIndicator?.FadeOut();
        }
        else
        {
            _bottomIndicator?.FadeOut();
        }
    }

    #endregion

    #region Methods for Commands

    private void InitCommands()
    {
        AddCommand = new RelayCommand(
            execute: Add,
            canExecute: _ => true);
        RemoveCommand = new RelayCommand(
            execute: Remove,
            canExecute: _ => this._videos.Count > 0);
        EditCommand = new RelayCommand(
            execute: Edit,
            canExecute: _ => this._videos.Count > 0);
        CloseCommand = new RelayCommand(
            execute: Close,
            canExecute: _ => true);
        SavePlaylistCommand = new RelayCommand(
            execute: SavePlaylist,
            canExecute: _ => this._videos.Count > 0);
        LoadPlaylistCommand = new RelayCommand(
            execute: LoadPlaylist,
            canExecute: _ => true);
        ClearPlaylistCommand = new RelayCommand(
            execute: _ => ClearTracksAsync(),
            canExecute: _ => this._videos.Count > 0);
    }

    /// <summary>
    /// Handles the Add command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Triggers the player's GetCurrentFrame method (placeholder for adding media).
    /// </remarks>
    public void Add(object parameter)
    {
        // Logika dla dodawania elementu do playlisty
        //MessageBox.Show("Dodawanie nowego elementu do playlisty.");
        _player.GetCurrentFrame();
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Media File",
            Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.mp3;*.wav;*.flac;*.ts|All Files|*.*",
            Multiselect = true
        };

        bool? result = ofd.ShowDialog();
        if (result == true)
        {
            foreach (string filename in ofd.FileNames)
            {
                var video = new VideoItem(filename);
                this.AddAsync(video);
                _player.InfoBox.DrawText = $"Added to playlist: {video.Name}";
            }
        }
    }

    /// <summary>
    /// Handles the Remove command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Placeholder for removing the selected item (calls NewAsync as example).
    /// </remarks>
    public void Remove(object parameter)
    {
        // Logika dla usuwania elementu z playlisty
        //NewAsync(BaseString, "");
        if (SelectedItem is VideoItem video)
        {
            if (video == _player.Playlist.Current)
            {
                _player.Preview();
                _player.Play();
            }
            this.RemoveAsync(video);
        }
        else
        {
            _player.InfoBox.DrawText = "No item selected to remove.";
        }
    }

    /// <summary>
    /// Handles the Edit command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Shows a message box for editing the selected item (placeholder implementation).
    /// </remarks>
    public void Edit(object parameter)
    {
        // Logika dla edycji elementu playlisty
        MessageBox.Show("Edycja wybranego elementu playlisty.");
    }

    /// <summary>
    /// Handles the Close command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Collapses the playlist visibility to hide it.
    /// </remarks>
    public void Close(object parameter)
    {
        // Logic for hide playlist
        this.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Handles the SavePlaylist command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Invokes the player's SavePlaylistConfig method to persist the playlist.
    /// </remarks>
    public void SavePlaylist(object parameter)
    {
        _player.SavePlaylistConfig();
        _player.InfoBox.DrawText = "Playlist saved.";
    }

    /// <summary>
    /// Handles the LoadPlaylist command execution.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <remarks>
    /// Invokes the player's LoadPlaylistConfig_Question method to restore the playlist.
    /// </remarks>
    public void LoadPlaylist(object parameter)
    {
        _player.LoadPlaylistConfig_Question();
        _player.InfoBox.DrawText = "Playlist loaded.";
    }

    /// <summary>
    /// Sets the reference to the media player for this playlist view.
    /// </summary>
    /// <param name="player">The <see cref="IPlayer"/> instance to associate.</param>
    public void SetPlayer(IPlayer player)
    {
        _player = player;
        _player.InfoBox.DrawText = "Playlist player set.";
    }

    /// <summary>
    /// Clears all videos from the playlist and unsubscribes from their events in a background task.
    /// </summary>
    /// <returns>A task representing the asynchronous clear operation.</returns>
    /// <remarks>
    /// Ensures thread safety by invoking UI updates via Dispatcher.
    /// </remarks>
    public Task ClearTracksAsync()
    {
        return Task.Run(() =>
        {
            if (Videos == null) return;
            var videosToClear = Videos.ToList();
            foreach (VideoItem video in videosToClear)
            {
                video.PositionChanged -= Video_PositionChanged;
                video.MouseDown -= Video_MouseDoubleClick;
            }
            Dispatcher.Invoke(() =>
            {
                Videos.Clear();
                CurrentIndex = -1;
                this.WriteLine("PlaylistView: Cleared all videos from playlist.");
                _player.InfoBox.DrawText = "Playlist cleared.";
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
    /// <remarks>
    /// Unsubscribes events, removes via Dispatcher, and logs the action.
    /// Adjusts CurrentIndex to maintain valid selection.
    /// </remarks>
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
                item.MouseDown -= Video_MouseDoubleClick;
                Dispatcher.Invoke(() =>
                {
                    Videos.RemoveAt(index);
                    if (index < CurrentIndex)
                    {
                        CurrentIndex--;
                    }
                    else if (index == CurrentIndex && Videos.Count > 0)
                    {
                        CurrentIndex = System.Math.Min(CurrentIndex, Videos.Count - 1);
                    }
                    else if (Videos.Count == 0)
                    {
                        CurrentIndex = -1;
                    }
                });
                this.WriteLine($"PlaylistView: Removed video {item.Name} at index {index}");
                _player.InfoBox.DrawText = $"Removed from playlist: {item.Name}";
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
    /// <returns>A task representing the asynchronous new playlist operation.</returns>
    public Task NewAsync(string name, string description)
    {
        return ClearTracksAsync();
    }

    /// <summary>
    /// Adds a single video to the playlist and sets up its player and event handlers in a background task.
    /// </summary>
    /// <param name="media">The video to add.</param>
    /// <returns>A task representing the asynchronous add operation.</returns>
    /// <remarks>
    /// Skips if already present. Sets player reference and subscribes events via Dispatcher.
    /// Logs the addition.
    /// </remarks>
    public Task AddAsync(VideoItem media)
    {
        return Task.Run(() =>
        {
            if (media == null || Contains(media)) return;
            media.SetPlayer(_player);
            media.PositionChanged += Video_PositionChanged;
            media.MouseDown += Video_MouseDoubleClick;

            Dispatcher.Invoke(() => Videos.Add(media));

            // 🔹 Uruchom doczytywanie metadanych w tle bez blokowania UI
            _ = media.LoadMetadataAsync();

            this.WriteLine($"PlaylistView: Added video {media.Name}");
            _player.InfoBox.DrawText = $"Added to playlist: {media.Name}";
        });
    }

    /// <summary>
    /// Adds multiple videos to the playlist and sets up their player and event handlers in a background task.
    /// </summary>
    /// <param name="medias">The array of videos to add.</param>
    /// <returns>A task representing the asynchronous add operation for multiple items.</returns>
    /// <remarks>
    /// Sequentially adds each video using <see cref="AddAsync(VideoItem)"/>.
    /// </remarks>
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
    /// Handles position changes in a video and updates its displayed current time.
    /// </summary>
    /// <param name="sender">The video that triggered the position change.</param>
    /// <param name="newPosition">The new playback position in seconds or milliseconds.</param>
    private void Video_PositionChanged(object sender, double newPosition)
    {
        if (sender is VideoItem video)
        {
            // Zakładamy, że newPosition to czas w sekundach
            TimeSpan position = TimeSpan.FromSeconds(newPosition);

            // 🔹 Zaktualizuj model danych, by UI odświeżył binding
            //video.Position = position.TotalMilliseconds;

            // (Opcjonalnie logowanie diagnostyczne)
            this.WriteLine($"PlaylistView: Position updated {video.Name} -> {position}");
        }
    }

    /// <summary>
    /// Handles double-click events to play the selected video.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    /// <remarks>
    /// Plays the selected VideoItem and logs the action.
    /// </remarks>
    private void Video_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount >= 2)
        {
            if (SelectedItem is VideoItem selectedVideo)
            {
                PlayVideo(selectedVideo);
                this.WriteLine($"PlaylistView: Double-clicked to play video {selectedVideo.Name}");
            }
        }
    }

    /// <summary>
    /// Plays a video selected from the context menu.
    /// </summary>
    /// <param name="video">The video to play.</param>
    /// <remarks>
    /// Stops the current video, updates the index, and invokes player playback via Dispatcher.
    /// Logs the action.
    /// </remarks>
    private void PlayVideo(VideoItem video)
    {
        this.Dispatcher.InvokeAsync(() =>
        {
            if (video != null && _player != null)
            {
                Current?.Stop();
                CurrentIndex = Videos.IndexOf(video);
                _player.Play(video);
                this.WriteLine($"PlaylistView: Context menu play video {video.Name}");
            }
        });
    }

    #endregion

    #region AutoScroll Logic

    /// <summary>
    /// Checks mouse proximity to top/bottom edges and starts auto-scrolling if needed.
    /// Works only when an item is being actively dragged.
    /// </summary>
    private void CheckAutoScroll(Point mousePos)
    {
        // 🔹 Scroll działa tylko podczas przeciągania elementu
        if (!_isDragging)
            return;

        if (_scrollViewer == null)
            _scrollViewer = FindScrollViewer(this);

        if (_scrollViewer == null)
            return;

        bool atTop = mousePos.Y < ScrollZone;
        bool atBottom = mousePos.Y > ActualHeight - ScrollZone;

        if (atTop)
        {
            _scrollVelocity = -MaxScrollSpeed * (1.0 - mousePos.Y / ScrollZone);
            ShowIndicator(top: true);
            HideIndicator(top: false);
            if (!_scrollTimer.IsEnabled)
                _scrollTimer.Start();
        }
        else if (atBottom)
        {
            double distance = (mousePos.Y - (ActualHeight - ScrollZone)) / ScrollZone;
            _scrollVelocity = MaxScrollSpeed * distance;
            ShowIndicator(top: false);
            HideIndicator(top: true);
            if (!_scrollTimer.IsEnabled)
                _scrollTimer.Start();
        }
        else
        {
            _scrollVelocity *= ScrollDamping;
            if (System.Math.Abs(_scrollVelocity) < 0.1)
            {
                _scrollTimer.Stop();
                HideIndicator(top: true);
                HideIndicator(top: false);
            }
        }
    }

    /// <summary>
    /// Smoothly scrolls the list based on calculated velocity (ease-out).
    /// Works only when dragging.
    /// </summary>
    private void OnScrollTimerTick(object sender, EventArgs e)
    {
        // 🔹 Jeśli nie trwa przeciąganie — zatrzymaj przewijanie
        if (!_isDragging)
        {
            StopAutoScroll();
            return;
        }

        if (_scrollViewer == null)
            return;

        if (System.Math.Abs(_scrollVelocity) > 0.05)
        {
            _scrollViewer.ScrollToVerticalOffset(
                System.Math.Max(0, _scrollViewer.VerticalOffset + _scrollVelocity));
            _scrollVelocity *= ScrollDamping;
        }
        else
        {
            _scrollVelocity = 0;
            _scrollTimer.Stop();
        }
    }

    /// <summary>
    /// Stops any ongoing auto-scroll operation.
    /// </summary>
    private void StopAutoScroll()
    {
        _scrollTimer?.Stop();
        _scrollVelocity = 0;
    }

    /// <summary>
    /// Recursively finds the <see cref="ScrollViewer"/> used inside the ListView template.
    /// </summary>
    private ScrollViewer FindScrollViewer(DependencyObject parent)
    {
        if (parent is ScrollViewer viewer)
            return viewer;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }

    #endregion

    #region Relay command class

    /// <summary>
    /// Inner class implementing a simple RelayCommand for MVVM command binding in WPF .NET 4.8.
    /// </summary>
    /// <remarks>
    /// Supports CanExecute predicate for enabling/disabling commands dynamically.
    /// Subscribes to CommandManager.RequerySuggested for automatic CanExecute updates.
    /// </remarks>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional predicate to determine if the command can execute (default: always true).</param>
        /// <exception cref="ArgumentNullException">Thrown if execute is null.</exception>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command (unused in base implementation).</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command (passed to the execute action).</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
    #endregion
}
