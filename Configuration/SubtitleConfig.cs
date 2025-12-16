// SubtitleConfig.cs
// Version: 0.1.17.21
// A class representing the configuration settings for subtitles in the application.
// Stores properties such as font size, font family, font color, and shadow settings for subtitle display.

using System.Windows.Media;

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for subtitles in the application.
/// Provides properties to define the font size, font family, font color, and shadow settings for subtitle display.
/// </summary>
public class SubtitleConfig : IConfig
{
    private readonly object _lock = new();
    /// <summary>
    /// Gets or sets the font size for subtitles.
    /// </summary>
    public double FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font family for subtitles.
    /// </summary>
    public FontFamily FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font color for subtitles.
    /// </summary>
    public Brush FontColor { get; set; }

    /// <summary>
    /// Gets or sets the shadow settings for subtitles.
    /// </summary>
    public Shadow Shadow { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleConfig"/> class with default subtitle settings.
    /// </summary>
    public SubtitleConfig()
    {
        FontSize = 24.0;
        FontFamily = new FontFamily("Arial");
        FontColor = Brushes.White;
        Shadow = new Shadow
        {
            Color = Colors.Black,
            ShadowDepth = 0.0,
            Opacity = 0.5,
            BlurRadius = 10.0,
            Visible = true
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleConfig"/> class by copying settings from another instance.
    /// </summary>
    /// <param name="config">The <see cref="SubtitleConfig"/> instance to copy settings from.</param>
    public SubtitleConfig(SubtitleConfig config)
    {
        FontSize = config.FontSize;
        FontFamily = config.FontFamily;
        FontColor = config.FontColor;
        Shadow = config.Shadow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleConfig"/> class by loading settings from the specified JSON configuration file.
    /// </summary>
    /// <param name="configPath">The path to the JSON configuration file.</param>
    public SubtitleConfig(string configPath)
    {
        lock (_lock)
        {
            var loadedConfig = Config.LoadFromJsonFile<SubtitleConfig>(configPath);
            FontSize = loadedConfig.FontSize;
            FontFamily = loadedConfig.FontFamily;
            FontColor = loadedConfig.FontColor;
            Shadow = loadedConfig.Shadow;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleConfig"/> class with specified subtitle settings.
    /// </summary>
    /// <param name="size">The font size for subtitles.</param>
    /// <param name="fontfamily">The name of the font family for subtitles.</param>
    /// <param name="color">The font color for subtitles.</param>
    /// <param name="show_shadow">A value indicating whether the shadow is visible.</param>
    /// <param name="shadow">The shadow settings for subtitles. If null, a default shadow is created.</param>
    public SubtitleConfig(double size, string fontfamily, Brush color, bool show_shadow, Shadow shadow = null)
    {
        FontSize = size;
        FontFamily = new FontFamily(fontfamily);
        FontColor = color;
        Shadow = shadow ?? new Shadow
        {
            Color = Colors.Black,
            ShadowDepth = 0.0,
            Opacity = 0.5,
            BlurRadius = 10.0,
            Visible = show_shadow
        };
    }

    /// <summary>
    /// Loads the subtitle configuration from the JSON file.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            var loadedConfig = Config.LoadFromJsonFile<SubtitleConfig>(Config.SubtitlesConfigPath);
            FontSize = loadedConfig.FontSize;
            FontFamily = loadedConfig.FontFamily;
            FontColor = loadedConfig.FontColor;
            Shadow = loadedConfig.Shadow;
        }
    }

    /// <summary>
    /// Saves the subtitle configuration to the JSON file.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            Config.SaveToFile(Config.SubtitlesConfigPath, this);
        }
    }
}
