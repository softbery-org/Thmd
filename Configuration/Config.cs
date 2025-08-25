// Config.cs
// Version: 0.1.0.77
// A singleton class for managing application configuration settings, including database connections,
// logging, VLC library settings, subtitles, updates, and plugins. Supports loading and saving
// configuration data to a JSON file with thread-safe access.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Thmd.Logs;
using Thmd.Media;
using Thmd.Repeats;

namespace Thmd.Configuration;

/// <summary>
/// A singleton class for managing application configuration settings.
/// Provides properties for database connections, logging, VLC library settings,
/// subtitles, updates, and plugins. Supports loading and saving configuration
/// data to a JSON file with thread-safe access using a singleton pattern.
/// </summary>
public class Config
{
    // Lock object for thread-safe singleton access.
    private static readonly object _lock = new object();

    // Singleton instance of the Config class.
    private static Config _instance;

    // Static instance of PlaylistConfig for easy access.
    private static PlaylistConfig _playlistConfig;

    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string DatabaseConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of database connections allowed.
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether logging is enabled.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets the directory path for storing log files.
    /// </summary>
    public string LogsDirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the API key for external services.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the path to the VLC library.
    /// </summary>
    public string LibVlcPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the VLC library is enabled.
    /// </summary>
    public bool EnableLibVlc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether console logging is enabled.
    /// </summary>
    public bool EnableConsoleLogging { get; set; }

    /// <summary>
    /// Gets or sets the logging level (e.g., Info, Debug, Error).
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the configuration settings for subtitles.
    /// </summary>
    public SubtitleConfig SubtitleConfig { get; set; }

    /// <summary>
    /// Gets or sets the configuration settings for application updates.
    /// </summary>
    public UpdateConfig UpdateConfig { get; set; }

    /// <summary>
    /// Gets or sets an array of plugin configurations.
    /// </summary>
    public PluginConfig[] Plugins { get; set; } = new PluginConfig[0];

    /// <summary>
    /// Gets or sets the configuration settings for playlists.
    /// </summary>
    public PlaylistConfig PlaylistConfig 
    {
        get
        {
            lock (_lock)
            {
                return _playlistConfig ?? (_playlistConfig = LoadFromPlaylist("playlist.json"));
            }
        }
        set 
        { 
            _playlistConfig = value;
        } 
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="Config"/> class.
    /// Loads the configuration from the default file "config.json" if not already initialized.
    /// </summary>
    public static Config Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ?? (_instance = LoadFromFile("config.json"));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Config"/> class with default values.
    /// </summary>
    public Config()
    {
        //Logger.Log.Log(LogLevel.Info, new string[] {"Console", "File"} , "Inicjalizacja domyślnych ustawień konfiguracji.");
        DatabaseConnectionString = "server=localhost;connection=default";
        MaxConnections = 10;
        EnableLogging = true;
        LogsDirectoryPath = "logs";
        ApiKey = "default-key";
        LibVlcPath = "libvlc";
        EnableLibVlc = true;
        LogLevel = LogLevel.Info;
        SubtitleConfig = new SubtitleConfig(24.0, "Arial", Brushes.WhiteSmoke, show_shadow: true, new Shadow());
        UpdateConfig = new UpdateConfig
        {
            CheckForUpdates = true,
            UpdateUrl = "http://thmdplayer.softbery.org/update.rar",
            UpdatePath = "update",
            UpdateFileName = "update",
            Version = "4.0.0",
            VersionUrl = "http://thmdplayer.softbery.org/version.txt",
            UpdateInterval = 86400,
            UpdateTimeout = 30
        };
        PlaylistConfig = LoadFromPlaylist("playlist.json");
    }

    /// <summary>
    /// Loads configuration settings from a specified JSON file.
    /// Creates a new configuration file with default values if the file does not exist.
    /// </summary>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <returns>A <see cref="Config"/> instance populated with the loaded settings.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is an error loading the configuration.</exception>
    public static Config LoadFromFile(string filePath)
    {
        try
        {
            Console.WriteLine($"Load file {filePath}");
            if (!File.Exists(filePath))
            {
                Config defaultConfig = new Config();
                SaveToFile(filePath, defaultConfig);
                return defaultConfig;
            }
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Config>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Błąd ładowania konfiguracji: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Loads configuration settings from a specified playlist file.
    /// </summary>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static PlaylistConfig LoadFromPlaylist(string filePath)
    {
        try
        {
            Console.WriteLine($"Load file {filePath}");
            if (!File.Exists(filePath))
            {
                PlaylistConfig playlistConfig = new PlaylistConfig();
                SaveToFile(filePath, playlistConfig);
                return playlistConfig;
            }
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<PlaylistConfig>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Błąd ładowania konfiguracji: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Saves the current configuration settings to a specified JSON file.
    /// Creates the directory if it does not exist.
    /// </summary>
    /// <param name="filePath">The path to save the JSON configuration file.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is an error saving the configuration.</exception>
    public static void SaveToFile(string filePath, object obj)
    {
        try
        {
            FileInfo file_info = new FileInfo(filePath);
            string directory = file_info.Directory.FullName;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Błąd zapisu konfiguracji: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Updates the configuration settings using the provided action and saves them to a file.
    /// Ensures thread-safe access during the update and save operation.
    /// </summary>
    /// <param name="updateAction">The action to update the configuration settings.</param>
    /// <param name="filePath">The path to save the JSON configuration file (default is "config.json").</param>
    public void UpdateAndSave(Action<Config> updateAction, string filePath = "config.json")
    {
        lock (_lock)
        {
            updateAction(this);
            SaveToFile(filePath, this);
        }
    }
}
