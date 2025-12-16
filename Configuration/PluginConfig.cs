// PluginConfig.cs
// Version: 0.1.17.21
// A class representing the configuration settings for a plugin in the application.
// Stores properties such as the plugin's name, file path, enabled status, version, and description.

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for a plugin in the application.
/// Provides properties to store the plugin's name, file path, enabled status, version, and description.
/// </summary>
public class PluginConfig : IConfig
{
    private readonly object _lock = new();
    /// <summary>
    /// Gets or sets the name of the plugin.
    /// </summary>
    public string PluginName { get; set; }

    /// <summary>
    /// Gets or sets the file path to the plugin.
    /// </summary>
    public string PluginPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the version of the plugin.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets a description of the plugin.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Loads the plugin configuration from the JSON file.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            var loadedConfig = Config.LoadFromJsonFile<PluginConfig>(Config.PluginConfigPath);
            PluginName = loadedConfig.PluginName;
            PluginPath = loadedConfig.PluginPath;
            IsEnabled = loadedConfig.IsEnabled;
            Version = loadedConfig.Version;
            Description = loadedConfig.Description;
        }
    }

    /// <summary>
    /// Saves the plugin configuration to the JSON file.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            Config.SaveToFile(Config.PluginConfigPath, this);
        }
    }
}
