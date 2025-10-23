// UpdateConfig.cs
// Version: 0.1.17.12
// A class representing the configuration settings for application updates.
// Stores properties such as update check settings, URLs, file paths, version information, and timing settings.

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for application updates.
/// Provides properties to define whether updates are checked, the URLs for updates and version information,
/// file paths, version details, and timing settings for update checks.
/// </summary>
public class UpdateConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the application should check for updates.
    /// Defaults to true.
    /// </summary>
    public bool CheckForUpdates { get; set; }

    /// <summary>
    /// Gets or sets the URL for downloading the update package.
    /// Defaults to "http://thmdplayer.softbery.org/update.rar".
    /// </summary>
    public string UpdateUrl { get; set; } = "http://thmdplayer.softbery.org/update.rar";

    /// <summary>
    /// Gets or sets the local directory path where update files are stored.
    /// Defaults to "update".
    /// </summary>
    public string UpdatePath { get; set; } = "update";

    /// <summary>
    /// Gets or sets the name of the update file.
    /// Defaults to "update".
    /// </summary>
    public string UpdateFileName { get; set; } = "update.rar";

    /// <summary>
    /// Gets or sets the current version of the application.
    /// Defaults to "1.0.0".
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the URL for retrieving the latest version information.
    /// Defaults to "http://thmdplayer.softbery.org/version.txt".
    /// </summary>
    public string VersionUrl { get; set; } = "http://thmdplayer.softbery.org/version.txt";

    /// <summary>
    /// Gets or sets the interval (in seconds) between update checks.
    /// Defaults to 86400 seconds (24 hours).
    /// </summary>
    public int UpdateInterval { get; set; } = 86400;

    /// <summary>
    /// Gets or sets the timeout (in seconds) for update operations.
    /// Defaults to 30 seconds.
    /// </summary>
    public int UpdateTimeout { get; set; } = 30;
}
