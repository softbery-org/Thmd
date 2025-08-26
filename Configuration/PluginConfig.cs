// PluginConfig.cs
// Version: 0.1.1.20
// A class representing the configuration settings for a plugin in the application.
// Stores properties such as the plugin's name, file path, enabled status, version, and description.

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for a plugin in the application.
/// Provides properties to store the plugin's name, file path, enabled status, version, and description.
/// </summary>
public class PluginConfig
{
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
}
