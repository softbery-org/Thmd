// Config.cs
// Version: 0.1.11.4
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

    // Static instance of IPlaylistConfig for easy access.
    private static IPlaylistConfig _playlistConfig;
    private static UpdateConfig _updateConfig;

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

    public static Config Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ?? (_instance = LoadFromJsonFile<Config>("config.json"));
            }
        }
    }

    private IPlaylistConfig _playerConfig;

    public IPlaylistConfig PlaylistConfig
    {
        get
        {
            lock (_lock)
            {
                return _playlistConfig ?? (_playlistConfig = LoadFromJsonFile<PlaylistConfig>("update.json"));
            }
        }
        set
        {
            _playlistConfig = value;
        }
    }

    public UpdateConfig UpdateConfig
    {
        get
        {
            lock (_lock)
            {
                return _updateConfig ?? (_updateConfig = LoadFromJsonFile<UpdateConfig>("update.json"));
            }
        }
        set
        {
            _updateConfig = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Config"/> class with default values.
    /// </summary>
    public Config()
    {
        //Logger.Log.Log(LogLevel.Info, new string[] {"Console", "File"} , "Inicjalizacja domy�lnych ustawie� konfiguracji.");
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
            UpdateFileName = "update.rar",
            Version = "4.0.0",
            VersionUrl = "http://thmdplayer.softbery.org/version.txt",
            UpdateInterval = 86400,
            UpdateTimeout = 30
        };
        PlaylistConfig = LoadFromJsonFile<PlaylistConfig>("playlist.json");
    }

    /// <summary>
    /// Loads configuration settings from a specified JSON file.
    /// Creates a new configuration file with default values if the file does not exist.
    /// </summary>
    /// <typeparam name="T">The type of configuration to load.</typeparam>
    /// <param name="filePath">The path to the JSON configuration file.</param>
    /// <returns>An instance of type <typeparamref name="T"/> populated with the loaded settings.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is an error loading the configuration.</exception>
    public static T LoadFromJsonFile<T>(string filePath) where T : new()
    {
        try
        {
            Console.WriteLine($"Loading file {filePath}");
            if (!File.Exists(filePath))
            {
                T defaultConfig = new T();
                SaveToFile(filePath, defaultConfig);
                return defaultConfig;
            }
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd ładowania konfiguracji: {ex.Message}", ex);
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
