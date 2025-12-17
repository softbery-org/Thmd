// IPlaylistConfig.cs
// Version: 0.1.17.25

using System.Collections.Generic;
using System.Windows;

using Thmd.Media;

namespace Thmd.Configuration;

/// <summary>
/// Interfejs definiuj�cy ustawienia konfiguracji playlisty.
/// </summary>
public interface IPlaylistConfig
{
    /// <summary>
    /// Nazwa playlisty.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Czy tryb losowego odtwarzania (shuffle) jest w��czony.
    /// </summary>
    bool EnableShuffle { get; set; }

    /// <summary>
    /// Tryb powtarzania odtwarzania (np. None, One, All).
    /// Zalecane: u�yj enuma RepeatType zamiast string.
    /// </summary>
    string Repeat { get; set; }

    /// <summary>
    /// Czy automatycznie rozpocz�� odtwarzanie po za�adowaniu playlisty.
    /// </summary>
    bool AutoPlay { get; set; }

    /// <summary>
    /// Lista �cie�ek do plik�w multimedialnych lub URI.
    /// </summary>
    List<string> MediaList { get; set; }

    /// <summary>
    /// Lista �cie�ek do plik�w napis�w powi�zanych z mediami (ta sama kolejno�� co MediaList).
    /// </summary>
    List<string> Subtitles { get; set; }

    /// <summary>
    /// Czy napisy s� widoczne podczas odtwarzania.
    /// </summary>
    bool SubtitleVisible { get; set; }

    /// <summary>
    /// Indeks obecnie odtwarzanego elementu na li�cie.
    /// </summary>
    int Current { get; set; }

    /// <summary>
    /// Lista wci��/punkt�w czasowych w wideo (np. chaptery, bookmarki).
    /// </summary>
    List<VideoIndent> Indents { get; set; }

    /// <summary>
    /// Pozycja okna playlisty na ekranie.
    /// </summary>
    Point Position { get; set; }

    /// <summary>
    /// Rozmiar okna playlisty.
    /// </summary>
    Size Size { get; set; }
}

/// <summary>
/// Klasa przechowuj�ca ustawienia playlisty.
/// Jest �adowana i zapisywana automatycznie przez Config.Instance.PlaylistConfig.
/// </summary>
public class PlaylistConfig : IPlaylistConfig
{
    public string Name { get; set; } = "Default Playlist";

    public bool EnableShuffle { get; set; } = true;

    // Sugestia: lepiej zmieni� na enum, np. public RepeatType Repeat { get; set; } = RepeatType.None;
    public string Repeat { get; set; } = "None";

    public bool AutoPlay { get; set; } = true;

    public List<string> MediaList { get; set; } = new List<string>();

    public List<string> Subtitles { get; set; } = new List<string>();

    public bool SubtitleVisible { get; set; } = true;

    public int Current { get; set; } = 0;

    public List<VideoIndent> Indents { get; set; } = new List<VideoIndent>();

    public Point Position { get; set; } = new Point(100, 100);

    public Size Size { get; set; } = new Size(600, 400);
}
