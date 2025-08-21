// Version: 0.1.0.18
namespace Thmd.Repeats;

// Placeholder for RepeatType enum (assumed to be in Thmd.Repeats)
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
    Current,
    /// <summary>
    /// Repeats all videos in the playlist in order.
    /// </summary>
    All,
    /// <summary>
    /// Plays a random video from the playlist after the current video ends.
    /// </summary>
    Random
}
