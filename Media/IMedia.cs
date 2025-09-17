// Version: 0.1.9.33
using System;

namespace Thmd.Media;

/// <summary>
///  Interface representing a media item.
/// </summary>
public interface IMedia
{
    /// <summary>
    /// Gets or sets media source.
    /// </summary>
    string Source { get; set; }
    /// <summary>
    /// Gets or sets media title.
    /// </summary>
	string Title { get; set; }
    /// <summary>
    /// Gets media duration.
    /// </summary>
    TimeSpan Duration { get; }
}
