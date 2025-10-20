// Repeat.cs
// Version: 0.1.17.11
namespace Thmd.Repeats;

// Placeholder for Repeat enum (assumed to be in Thmd.Repeats)
// Update this enum in the actual Thmd.Repeats namespace to include Random
/// <summary>
/// Defines the repeat modes for media playback.
/// </summary>
public enum RepeatType
{
    /// <summary>
    /// No repeat; playback stops after the current video.
    /// </summary>
    None,
    /// <summary>
    /// Repeats the current video.
    /// </summary>
    One,
    /// <summary>
    /// Repeats all videos in the playlist in order.
    /// </summary>
    All
}
