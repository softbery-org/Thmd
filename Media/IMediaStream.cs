// Version: 0.1.16.93
using System;
using System.IO;
using System.Threading.Tasks;

namespace Thmd.Media
{
    /// <summary>
    /// Represents a generic media stream.
    /// Provides methods to access the media content and retrieve its duration.
    /// </summary>
    public interface IMediaStream : IDisposable
    {
        /// <summary>
        /// Asynchronously retrieves the media stream.
        /// </summary>
        /// <returns>A <see cref="Stream"/> containing the media data.</returns>
        Task<Stream> GetStreamAsync();

        /// <summary>
        /// Retrieves the media stream synchronously.
        /// </summary>
        /// <returns>A <see cref="Stream"/> containing the media data.</returns>
        Task<Stream> GetStream();

        /// <summary>
        /// Gets the duration of the media stream in seconds.
        /// </summary>
        /// <returns>The duration of the media stream.</returns>
        double GetDuration();
    }
}
