// Version: 0.2.0.24

using System;
using System.IO;

using Newtonsoft.Json;

using Thmd.Consolas;
using Thmd.Logs;

namespace Thmd.Configuration;

/// <summary>
/// A singleton that manages application configuration. 
/// It supports JSON read/write and access to settings: database, logs, VLC, OpenAI, etc.
/// </summary>
public sealed class Config
{
    #region Singleton Implementation
    // Lock object for thread safety.
    private static readonly object _lock = new();

    private static Config _instance;
    private static IPlaylistConfig _playlistConfig;
    private static UpdateConfig _updateConfig;
    private static OpenAiConfig _openAiConfig;
    private static PerformanceMonitorConfig _performanceMonitor;

    private string _filePath = string.Empty;
    #endregion

    #region Properties
    /// <summary>
    /// Config singleton instance.
    /// </summary>
    public static Config Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= LoadFromJsonFile<Config>("config/config.json");
            }
        }
    }

    /// <summary>
    /// Path for main configuration file.
    /// </summary>
    public static string ConfigPath => "config/config.json";
    /// <summary>
    /// Path for playlist configuration file.
    /// </summary>
    public static string PlaylistConfigPath => "config/playlist.json";
    /// <summary>
    /// Path for update configuration file.
    /// </summary>
    public static string UpdateConfigPath => "config/update.json";
    /// <summary>
    /// Path for OpenAI configuration file.
    /// </summary>
    public static string OpenAiConfigPath => "config/openai.json";
    /// <summary>
    /// Path for Performance Monitor configuration file.
    /// </summary>
    public static string PerformanceMonitorConfigPath => "config/performance_monitor.json";
    /// <summary>
    /// Path for Subtitles configuration file.
    /// </summary>
    public static string SubtitlesConfigPath => "config/subtitles.json";
    /// <summary>
    /// Connection string for the database.
    /// </summary>
    public string DatabaseConnectionString { get; set; }
    /// <summary>
    /// Application language (e.g., "pl_PL" for Polish).
    /// </summary>
    public string Language { get; set; } = "pl_PL";
    /// <summary>
    /// Maximum number of concurrent connections.
    /// </summary>
    public int MaxConnections { get; set; } = 10;
    /// <summary>
    /// Enables or disables logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    /// <summary>
    /// Path to the logs directory.
    /// </summary>
    public string LogsDirectoryPath { get; set; } = "logs";
    /// <summary>
    /// Path to the LibVLC library.
    /// </summary>
    public string LibVlcPath { get; set; } = "libvlc";
    /// <summary>
    /// Enables or disables the use of LibVLC for media playback.
    /// </summary>
    public bool EnableLibVlc { get; set; } = true;
    /// <summary>
    /// Enables or disables console logging.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;
    /// <summary>
    /// Minimum log level to record.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    /// <summary>
    /// Automatically load the playlist on application start with question.
    /// </summary>
    public bool Question_AutoLoadPlaylist { get; set; } = true;
    /// <summary>
    /// 
    /// </summary>
    public SubtitleConfig SubtitleConfig { get; set; } = new(24.0, "Arial", System.Windows.Media.Brushes.WhiteSmoke, true);
    /// <summary>
    /// Playlist configuration.
    /// </summary>
    public IPlaylistConfig PlaylistConfig
    {
        get
        {
            lock (_lock)
            {
                return _playlistConfig ??= LoadFromJsonFile<PlaylistConfig>("config/playlist.json");
            }
        }
        set => _playlistConfig = value;
    }
    /// <summary>
    /// Update configuration.
    /// </summary>
    public UpdateConfig UpdateConfig
    {
        get
        {
            lock (_lock)
            {
                return _updateConfig ??= LoadFromJsonFile<UpdateConfig>("config/update.json");
            }
        }
        set => _updateConfig = value;
    }
    /// <summary>
    /// OpenAI configuration.
    /// </summary>
    public OpenAiConfig OpenAiConfig
    {
        get
        {
            lock (_lock)
            {
                return _openAiConfig ??= LoadFromJsonFile<OpenAiConfig>("config/openai.json");
            }
        }
        set => _openAiConfig = value;
    }
    /// <summary>
    /// Performance Monitor configuration.
    /// </summary>
    public PerformanceMonitorConfig PerformanceMonitor
    {
        get
        {
            lock (_lock)
            {
                return _performanceMonitor ??= LoadFromJsonFile<PerformanceMonitorConfig>("config/performance_monitor.json");
            }
        }
        set => _performanceMonitor = value;
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Default constructor.
    /// </summary>
    public Config()
    {
        this.WriteLine("Initialize configuration.");
    }
    #endregion

    #region Methods
    /// <summary>
    /// Init object with type T from file JSON or create new, if not exist.
    /// </summary>
    public static T LoadFromJsonFile<T>(string filePath) where T : new()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var defaultConfig = new T();
                SaveToFile(filePath, defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json) ?? new T();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration from {filePath}: {ex.Message}");
            return new T();
        }
    }

    /// <summary>
    /// Save object to JSON file.
    /// </summary>
    public static void SaveToFile(string filePath, object obj)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Directory?.Create();

            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration to {filePath}: {ex.Message}");
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
}
