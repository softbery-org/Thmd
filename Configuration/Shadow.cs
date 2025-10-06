// Shadow.cs
// Version: 0.1.16.91
// A class representing the configuration settings for a shadow effect in the application.
// Stores properties such as color, depth, opacity, blur radius, and visibility for a shadow effect.

using System.Windows.Media;

namespace Thmd.Configuration;

/// <summary>
/// Represents the configuration settings for a shadow effect in the application.
/// Provides properties to define the color, depth, opacity, blur radius, and visibility of a shadow.
/// </summary>
public class Shadow
{
    /// <summary>
    /// Gets or sets the color of the shadow.
    /// Defaults to <see cref="Colors.Black"/>.
    /// </summary>
    public Color Color { get; set; } = Colors.Black;

    /// <summary>
    /// Gets or sets the depth of the shadow, determining its offset from the element.
    /// Defaults to 0.0.
    /// </summary>
    public double ShadowDepth { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the opacity of the shadow, ranging from 0.0 (fully transparent) to 1.0 (fully opaque).
    /// Defaults to 0.5.
    /// </summary>
    public double Opacity { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the blur radius of the shadow, controlling the softness of the shadow edges.
    /// Defaults to 10.0.
    /// </summary>
    public double BlurRadius { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets a value indicating whether the shadow is visible.
    /// Defaults to true.
    /// </summary>
    public bool Visible { get; set; } = true;
}
