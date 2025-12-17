// Version: 0.2.0.38

using System;
using System.IO;
using System.Windows;

using Newtonsoft.Json;

using Thmd.Consolas;
using Thmd.Logs;

namespace Thmd.Configuration;

/// <summary>
/// A singleton that manages application configuration. 
/// It supports JSON read/write and access to settings: database, logs, VLC, OpenAI, etc.
/// </summary>
public sealed class Config : IConfig
{
    #region Singleton
    private static readonly object _lock = new();
    private static readonly Lazy<Config> _instance = new(() => new Config(), true);

    public static Config Instance => _instance.Value;

    #endregion

    #region �cie�ki do plik�w konfiguracyjnych

    private static readonly string ConfigDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");
    public static string PlaylistConfigPath => Path.Combine(ConfigDirectory, "playlist.json");
    public static string ControlbarConfigPath => Path.Combine(ConfigDirectory, "controlbar.json");
    public static string UpdateConfigPath => Path.Combine(ConfigDirectory, "update.json");
    public static string AiConfigPath => Path.Combine(ConfigDirectory, "ai.json");
    public static string PerformanceMonitorConfigPath => Path.Combine(ConfigDirectory, "performance_monitor.json");
    public static string SubtitlesConfigPath => Path.Combine(ConfigDirectory, "subtitle.json");

    #endregion

    #region Podkonfiguracje (lazy loading)

    private SubtitleConfig _subtitleConfig;
    private IPlaylistConfig _playlistConfig;
    private ControlbarConfig _controlbarConfig;
    private UpdateConfig _updateConfig;
    private AiConfig _aiConfig;
    private PerformanceMonitorConfig _performanceMonitor;
    private string _filePath;

    public SubtitleConfig SubtitleConfig =>
        _subtitleConfig ??= LoadConfig<SubtitleConfig>(SubtitlesConfigPath);

    public IPlaylistConfig PlaylistConfig
    {
        get => _playlistConfig ??= LoadConfig<PlaylistConfig>(PlaylistConfigPath);
        set => _playlistConfig = value;
    }

    public ControlbarConfig ControlbarConfig
    {
        get => _controlbarConfig ??= LoadConfig<ControlbarConfig>(ControlbarConfigPath);
        set => _controlbarConfig = value;
    }

    public UpdateConfig UpdateConfig
    {
        get => _updateConfig ??= LoadConfig<UpdateConfig>(UpdateConfigPath);
        set => _updateConfig = value;
    }

    public AiConfig AiConfig
    {
        get => _aiConfig ??= LoadConfig<AiConfig>(AiConfigPath);
        set => _aiConfig = value;
    }

    public PerformanceMonitorConfig PerformanceMonitor
    {
        get => _performanceMonitor ??= LoadConfig<PerformanceMonitorConfig>(PerformanceMonitorConfigPath);
        set => _performanceMonitor = value;
    }

    #endregion

    #region Ustawienia g��wne

    // Database
    public string DatabaseConnectionString { get; set; } = "Data Source=database.db;";
    // System
    public string Language { get; set; } = "pl_PL";

    // Player
    public int PlayerVolume { get; set; } = 100;
    public bool AutoloadPlaylist { get; set; } = true;
    public int MaxConnections { get; set; } = 10;

    // Logs
    public bool LoggingEnabled { get; set; } = true;
    public string LogsDirectoryPath { get; set; } = "logs";
    
    public bool EnableConsolLogging { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Info;

    // ffmpeg
    public string FfmpegPath { get; set; } = "ffmpeg";

    // Vlc
    public string VlcLibPath { get; set; } = "libvlc";
    public bool VlcLibEnabled { get; set; } = true;

    // Playlist
    public bool PlaylistEnableShuffle { get; set; } = false;
    public string PlaylistRepeat { get; set; } = "None";
    public bool PlaylistAutoPlay { get; set; } = true;


    public WindowState MainWindowState { get; set; } = WindowState.Normal;
    public double MainWindowWidth { get; set; } = 800;
    public double MainWindowHeight { get; set; } = 600;
    public double MainWindowTop { get; set; } = 100;
    public double MainWindowLeft { get; set; } = 100;

    public static string PluginConfigPath { get; internal set; }

    #endregion

    #region Konstruktor prywatny

    private Config()
    {
        this.WriteLine("Initialize main configuration.");
        EnsureConfigDirectoryExists();
    }

    private void EnsureConfigDirectoryExists()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    #endregion

    #region Methods
    /// <summary>
    /// Init object with type T from file JSON or create new, if not exist.
    /// </summary>
    public static T LoadFromJsonFile<T>(string filePath) where T : new()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    var defaultConfig = new T();
                    SaveToFile(filePath, defaultConfig);
                    return defaultConfig;
                }

                Console.WriteLine($"Loading configuration from {filePath}.");
                var json = File.ReadAllText(filePath);
                Console.WriteLine($"Configuration content: {json}");

                return JsonConvert.DeserializeObject<T>(json) ?? new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration from {filePath}: {ex.Message}");
                return new T();
            }
        }
    }

    /// <summary>
    /// Save object to JSON file.
    /// </summary>
    public static void SaveToFile(string filePath, object obj)
    {
        lock (_lock)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Directory?.Create();

                var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Configuration saved to {filePath}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration to {filePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Update and save configuration in a thread-safe manner.
    /// </summary>
    public void UpdateAndSave(Action<Config> updateAction)
    {
        lock (_lock)
        {
            updateAction?.Invoke(this);
            SaveToFile(_filePath, this);
        }
    }
    #endregion

    #region Metody pomocnicze

    public static T LoadConfig<T>(string filePath) where T : new()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var defaultConfig = new T();
                SaveConfig(filePath, defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<T>(json);
            return config ?? new T();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config {filePath}: {ex.Message}");
            return new T();
        }
    }

    public static void SaveConfig(string filePath, object config)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config {filePath}: {ex.Message}");
        }
    }

    #endregion

    #region Load / Save g��wnej konfiguracji

    public void Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Save(); // zapisujemy domyślną konfigurację
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            var loaded = JsonConvert.DeserializeObject<Config>(json);

            DatabaseConnectionString = loaded.DatabaseConnectionString;
            Language = loaded.Language;
            MaxConnections = loaded.MaxConnections;
            LoggingEnabled = loaded.LoggingEnabled;
            PlayerVolume = loaded.PlayerVolume;
            LogsDirectoryPath = loaded.LogsDirectoryPath;
            VlcLibPath = loaded.VlcLibPath;
            VlcLibEnabled = loaded.VlcLibEnabled;
            EnableConsolLogging = loaded.EnableConsolLogging;
            LogLevel = loaded.LogLevel;

            // Playlist
            AutoloadPlaylist = loaded.AutoloadPlaylist;


            MainWindowState = loaded.MainWindowState;
            MainWindowWidth = loaded.MainWindowWidth;
            MainWindowHeight = loaded.MainWindowHeight;
            MainWindowTop = loaded.MainWindowTop;
            MainWindowLeft = loaded.MainWindowLeft;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading main config: {ex.Message}");
        }
    }

    public void Save()
    {
        SaveConfig(ConfigPath, this);
    }

    #endregion
}
