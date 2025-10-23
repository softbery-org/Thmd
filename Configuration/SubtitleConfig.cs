// SubtitleConfig.cs
// Version: 0.1.17.14
// A class representing the configuration settings for subtitles in the application.
// Stores properties such as font size, font family, font color, and shadow settings for subtitle display.

using System.Windows.Media;

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for subtitles in the application.
/// Provides properties to define the font size, font family, font color, and shadow settings for subtitle display.
/// </summary>
public class SubtitleConfig
{
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
}
