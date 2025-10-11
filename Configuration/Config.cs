// Version: 0.2.0.1

using System;
using System.IO;

using Newtonsoft.Json;

using Thmd.Consolas;
using Thmd.Logs;

namespace Thmd.Configuration;

/// <summary>
/// Singleton zarządzający konfiguracją aplikacji.
/// Obsługuje zapis/odczyt JSON oraz dostęp do ustawień: bazy, logów, VLC, OpenAI, itp.
/// </summary>
public sealed class Config
{
    private static readonly object _lock = new();
    private static Config _instance;

    private static IPlaylistConfig _playlistConfig;
    private static UpdateConfig _updateConfig;
    private static OpenAiConfig _openAiConfig;
    private static PerformanceMonitorConfig _performanceMonitor;

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

    public string DatabaseConnectionString { get; set; }
    public string Language { get; set; } = "pl_PL";
    public int MaxConnections { get; set; } = 10;
    public bool EnableLogging { get; set; } = true;
    public string LogsDirectoryPath { get; set; } = "logs";
    public string LibVlcPath { get; set; } = "libvlc";
    public bool EnableLibVlc { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public SubtitleConfig SubtitleConfig { get; set; } = new(24.0, "Arial", System.Windows.Media.Brushes.WhiteSmoke, true);

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

    public Config()
    {
        this.WriteLine("Inicjalizacja konfiguracji aplikacji.");
    }

    /// <summary>
    /// Ładuje obiekt typu T z pliku JSON lub tworzy nowy, jeśli nie istnieje.
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
            throw new InvalidOperationException($"Błąd ładowania konfiguracji z {filePath}: {ex.Message}", ex);
        }
    }

    private string _filePath = string.Empty;

    /// <summary>
    /// Zapisuje obiekt do pliku JSON.
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
            throw new InvalidOperationException($"Błąd zapisu konfiguracji do {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Aktualizuje i zapisuje konfigurację w sposób bezpieczny dla wątków.
    /// </summary>
    public void UpdateAndSave(Action<Config> updateAction)
    {
        lock (_lock)
        {
            updateAction?.Invoke(this);
            SaveToFile(_filePath, this);
        }
    }
}
