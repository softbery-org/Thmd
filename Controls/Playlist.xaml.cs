// Version: 2.0.0.28
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Effects;
using System.Windows.Threading;

using Thmd.Configuration;
using Thmd.Consolas;
using Thmd.Controls.Effects;
using Thmd.Media;
using Thmd.Utilities; // <-- DODAJ TO

namespace Thmd.Controls;

/// <summary>
/// Playlist control with full management: add/remove, async IO, navigation, sorting, shuffle,
/// move up/down and **fully working drag & drop (reorder + move control)**.
/// </summary>
public partial class Playlist : ListView, INotifyPropertyChanged
{
    private IPlaylistConfig _config = Config.Instance.PlaylistConfig;
    private dynamic _player;
    private readonly ObservableCollection<VideoItem> _videos = new();
    private int _currentIndex = -1;
    private VideoItem _current;
    private VideoItem _next;
    private VideoItem _previous;
    private int? _playnext;

    private ScrollViewer _scrollViewer;
    private DispatcherTimer _scrollTimer;
    private double _scrollVelocity = 0;
    private const double ScrollZone = 20.0;
    private const double MaxScrollSpeed = 14.0;
    private const double ScrollDamping = 0.85;

    // Drag & drop (reordering)
    private Point _dragStartPoint;
    private bool _isDragging;
    private object _draggedItem;
    private FrameworkElement _itemsPresenter;
    private AdornerLayer _adornerLayer;
    private ListViewItem _dropTarget;

    private Point _startPoint;
    private ScrollIndicatorAdorner _topIndicator;
    private ScrollIndicatorAdorner _bottomIndicator;
    private ListViewItem _draggedContainer;  // Fix: Przechowuj referencję do kontenera dla OnDragOver
    private bool _isMouseSubscribed;  // Fix: Flaga dla subskrypcji eventu
    private int _draggedIndex;
    private DragShadowAdorner _dragAdorner;

    // Commands
    public ICommand AddCommand { get; private set; }
    public ICommand RemoveCommand { get; private set; }
    public ICommand EditCommand { get; private set; }
    public ICommand CloseCommand { get; private set; }
    public ICommand SavePlaylistCommand { get; private set; }
    public ICommand LoadPlaylistCommand { get; private set; }
    public ICommand ClearPlaylistCommand { get; private set; }
    public ICommand MoveUpCommand { get; private set; }
    public ICommand MoveDownCommand { get; private set; }
    public ICommand ShuffleCommand { get; private set; }
    public ICommand SortByNameCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    #region Public Properties
    public ObservableCollection<VideoItem> Videos => _videos;
    public int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            if (_currentIndex != value)
            {
                _currentIndex = value;
                OnPropertyChanged();
                UpdateNavigation();
            }
        }
    }
    
    public VideoItem Current
    {
        get => _current;
        private set { _current = value; OnPropertyChanged(); }
    }
    public VideoItem Next
    {
        get => _next;
        private set { _next = value; OnPropertyChanged(); }
    }
    public VideoItem Previous
    {
        get => _previous;
        private set { _previous = value; OnPropertyChanged(); }
    }
    public int Count => _videos.Count;

    public int? PlayNext
    {
        get => _playnext;
        set=> _playnext = value;
    }
    #endregion

    #region Constructor
    public Playlist()
    {
        InitializeComponentSafe();
        DataContext = this;

        // KLUCZOWE: Jedno miejsce na ItemsSource
        base.ItemsSource = _videos;

        AllowDrop = true;

        _videos.CollectionChanged += (s, e) =>
        {
            if (_currentIndex >= _videos.Count) _currentIndex = _videos.Count - 1;
            if (_videos.Count == 0) _currentIndex = -1;
            UpdateNavigation();
            CommandManager.InvalidateRequerySuggested();
        };

        InitCommands();

        // === PRZECIĄGANIE CAŁEJ KONTROLKI ===
        //Loaded += (s, e) => ControlControllerHelper.Attach(this);
    }

    public Playlist(IPlayer player) : this()
    {
        _player = player;
    }

    private void InitializeComponentSafe()
    {
        try
        {
            var method = GetType().GetMethod("InitializeComponent",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            method?.Invoke(this, null);
        }
        catch { /* ignore */ }
    }
    #endregion

    #region Commands
    private void InitCommands()
    {
        AddCommand = new RelayCommand(async _ => await AddCommandExecuteAsync(), _ => true);
        RemoveCommand = new RelayCommand(async _ => await RemoveCommandExecuteAsync(), _ => Count > 0);
        EditCommand = new RelayCommand(_ => Edit(null), _ => Count > 0);
        CloseCommand = new RelayCommand(_ => Close(null), _ => true);
        SavePlaylistCommand = new RelayCommand(_ => SavePlaylist(null), _ => Count > 0);
        LoadPlaylistCommand = new RelayCommand(_ => LoadPlaylist(null), _ => true);
        ClearPlaylistCommand = new RelayCommand(async _ => await ClearTracksAsync(), _ => Count > 0);
        MoveUpCommand = new RelayCommand(_ => MoveUp(), _ => CanMoveUp());
        MoveDownCommand = new RelayCommand(_ => MoveDown(), _ => CanMoveDown());
        ShuffleCommand = new RelayCommand(_ => Shuffle(), _ => Count > 1);
        SortByNameCommand = new RelayCommand(_ => SortByName(), _ => Count > 1);
    }

    private bool CanMoveUp() => SelectedItem is VideoItem m && _videos.IndexOf(m) > 0;
    private bool CanMoveDown() => SelectedItem is VideoItem m && _videos.IndexOf(m) < _videos.Count - 1;
    #endregion

    #region Add / Remove
    public async Task AddAsync(VideoItem media)
    {
        if (media == null || Contains(media)) return;

        TrySetPlayerOnItem(media);
        //media.PositionChanged += Video_PositionChanged;
        await TryLoadMetadataAsync(media);

        await Dispatcher.InvokeAsync(() =>
        {
            _videos.Add(media);
            if (CurrentIndex < 0 && _videos.Count == 1) CurrentIndex = 0;
            TrySetInfoBoxText($"Added: {media.Name}");
        });
    }

    public async Task AddAsync(VideoItem[] medias)
    {
        if (medias == null) return;
        foreach (var m in medias) await AddAsync(m);
    }

    public async Task<VideoItem> RemoveAsync(VideoItem media)
    {
        if (media == null) return null;

        int index = _videos.IndexOf(media);
        if (index < 0) return null;

        //media.PositionChanged -= Video_PositionChanged;

        await Dispatcher.InvokeAsync(() =>
        {
            _videos.RemoveAt(index);
            if (index < CurrentIndex) CurrentIndex--;
            else if (index == CurrentIndex && _videos.Count > 0)
                CurrentIndex = System.Math.Min(CurrentIndex, _videos.Count - 1);
            else if (_videos.Count == 0) CurrentIndex = -1;
            UpdateNavigation();
            TrySetInfoBoxText($"Removed: {media.Name}");
        });

        return media;
    }

    public void RemoveSelected() => _ = RemoveAsync(SelectedItem as VideoItem);
    public Task ClearTracksAsync() => Task.Run(() =>
    {
        //foreach (var v in _videos.ToList()) v.PositionChanged -= Video_PositionChanged;
        Dispatcher.Invoke(() =>
        {
            _videos.Clear();
            CurrentIndex = -1;
            UpdateNavigation();
            TrySetInfoBoxText("Playlist cleared.");
        });
    });
    #endregion

    #region Navigation
    private void UpdateNavigation()
    {
        if (_videos.Count == 0 || CurrentIndex < 0)
        {
            Current = Next = Previous = null;
            return;
        }

        Current = _videos[CurrentIndex];
        Next = CurrentIndex + 1 < _videos.Count ? _videos[CurrentIndex + 1] : null;
        Previous = CurrentIndex > 0 ? _videos[CurrentIndex - 1] : null;
    }
    #endregion

    #region Sorting / Move
    public void SortByName()
    {
        var sorted = _videos.OrderBy(v => v?.Name ?? "").ToList();
        RebuildCollection(sorted, "Playlist sorted by name.");
    }

    public void Shuffle()
    {
        var rnd = new Random();
        var list = _videos.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        RebuildCollection(list, "Playlist shuffled.");
    }

    private void RebuildCollection(List<VideoItem> newList, string message)
    {
        var current = Current;
        Dispatcher.Invoke(() =>
        {
            _videos.Clear();
            foreach (var v in newList) _videos.Add(v);
            CurrentIndex = current != null ? System.Math.Max(0, _videos.IndexOf(current)) : (_videos.Count > 0 ? 0 : -1);
            UpdateNavigation();
            TrySetInfoBoxText(message);
        });
    }

    public void MoveUp() => MoveItem(-1);
    public void MoveDown() => MoveItem(1);

    private void MoveItem(int direction)
    {
        if (SelectedItem is not VideoItem sel) return;
        int idx = _videos.IndexOf(sel);
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= _videos.Count) return;

        Dispatcher.Invoke(() =>
        {
            _videos.Move(idx, newIdx);
            SelectedItem = sel;
            if (CurrentIndex == idx) CurrentIndex = newIdx;
            else if (CurrentIndex == newIdx) CurrentIndex = idx;
            UpdateNavigation();
        });
    }
    #endregion

    #region Drag-and-Drop Logic    
    private void PlayVideo(VideoItem video)
    {
        if (video != null && _player != null)
        {
            _player?.Play(video);
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        _startPoint = e.GetPosition(this);
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        if (SelectedItem is VideoItem video)
        {
            PlayVideo(video);
            CurrentIndex = Videos.IndexOf(video);
            this.WriteLine($"Double click on {video.Name}");
            _ = _player.InfoBox.DrawText = $"Play {video.Name}.";
        }
        base.OnMouseDoubleClick(e);
    }

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

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDragging)
        {
            _isDragging = false;
        }
        _draggedItem = null;
        _draggedIndex = -1;

        _config.Size = new Size(this.Width, this.Height);
        _config.MediaList = CreateMediaList();

        EndDrag();
    }

    private List<string> CreateMediaList()
    {
        var list = new List<string>();
        foreach (var video in Videos)
        {
            list.Add(video.Name);
        }
        return list;
    }

    private void StartDrag(VideoItem item, MouseEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => StartDrag(item, e));  // Rekursja do UI thread
            return;
        }
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            this.WriteLine(new InvalidOperationException("DragDrop need STA thread."));
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

        // Fix: IDataObject z ComTypes dla kompatybilności OLE (zapobiega null w OleDoDragDrop)
        var dataObject = new DataObject();
        dataObject.SetData(typeof(VideoItem), item);  // Lub użyj string key: dataObject.SetData("VideoItem", item);

        try
        {
            // Fix: Wywołaj w Dispatcher dla bezpieczeństwa (choć już w UI)
            Dispatcher.InvokeAsync(() =>
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

    #region AutoScroll Logic
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

    private void StopAutoScroll()
    {
        _scrollTimer?.Stop();
        _scrollVelocity = 0;
    }

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

    #region Helper methods
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

    private void OnGlobalMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _dragAdorner != null && _draggedContainer != null)
        {
            // Fix: Pobierz pozycję globalnie via Mouse.GetPosition(this) – dokładna dla ListView
            Point position = e.GetPosition(this);

            // Centrowanie: Odejmij środek kontenera dla "podążania" za myszą
            double offsetX = position.X - (_draggedContainer.ActualWidth / 2);
            double offsetY = position.Y - (_draggedContainer.ActualHeight / 2);

            _dragAdorner.Offset = new Point(offsetX / 2, offsetY / 2);

            // Fix: Wymuś redraw dla natychmiastowego efektu (płynność w .NET 4.8)
            _dragAdorner.InvalidateArrange();
            _dragAdorner.InvalidateVisual();
        }
    }

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

    #region Helpers
    private async Task TryLoadMetadataAsync(VideoItem item) { try { await item.LoadMetadataAsync(); } catch { } }
    private void TrySetPlayerOnItem(VideoItem item)
    {
        try { item?.GetType().GetMethod("SetPlayer")?.Invoke(item, new object[] { _player }); }
        catch { }
    }
    private void TrySetInfoBoxText(string text)
    {
        try { _player?.InfoBox.DrawText = text; } catch { }
    }
    public bool Contains(VideoItem media) => media != null && _videos.Any(v => UriEquals(v?.Uri, media.Uri));
    private static bool UriEquals(Uri a, Uri b) => a != null && b != null && (a == b || string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase));
    private void Video_PositionChanged(object sender, double pos) => ((VideoItem)sender).Position = TimeSpan.FromSeconds(pos).TotalMilliseconds;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion

    #region Command Handlers
    private async Task AddCommandExecuteAsync()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Media File",
            Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.mp3;*.wav;*.flac;*.ts;*.m3u8;*.hlsarc|All Files|*.*",
            Multiselect = true
        };
        if (ofd.ShowDialog() == true)
            foreach (var f in ofd.FileNames)
                await AddAsync(new VideoItem(f));
    }

    private async Task RemoveCommandExecuteAsync()
    {
        if (SelectedItem is VideoItem m)
        {
            if (m == Current) try { _player.Preview(); _player.Play(); } catch { }
            await RemoveAsync(m);
        }
        else TrySetInfoBoxText("No item selected.");
    }

    public void Edit(object p) => MessageBox.Show("Edit selected item.");
    public void Close(object p) => Visibility = Visibility.Collapsed;
    public void SavePlaylist(object p) { try { _player.SavePlaylistConfig(); } catch { } TrySetInfoBoxText("Saved."); }
    public void LoadPlaylist(object p) { try { _player.LoadPlaylistConfig_Question(); } catch { } TrySetInfoBoxText("Loaded."); }
    #endregion

    public void SetPlayer(object player)
    {
        _player = player;
        Dispatcher.InvokeAsync(() => TrySetInfoBoxText("Player connected."));
    }
}

// === RELAY COMMAND ===
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object parameter) => _execute(parameter);

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
