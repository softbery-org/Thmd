// Version: 0.1.17.13
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Thmd.Logs;

namespace Thmd.Media
{
    /// <summary>
    /// Provides video editing utilities such as generating thumbnails and cutting video segments.
    /// </summary>
    public class MediaEditor
    {
        private Uri _videoUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaEditor"/> class with a video file path.
        /// </summary>
        /// <param name="video_path">The path to the video file.</param>
        public MediaEditor(string video_path)
        {
            _videoUri = new Uri(video_path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaEditor"/> class with a video URI.
        /// </summary>
        /// <param name="video_uri">The URI of the video file.</param>
        public MediaEditor(Uri video_uri)
        {
            _videoUri = video_uri;
        }

        /// <summary>
        /// Generates a thumbnail image from the video at 1 second into playback.
        /// </summary>
        /// <returns>An <see cref="Image"/> containing the thumbnail, or null if generation fails.</returns>
        public Image GetThumbnail()
        {
            MediaFile inputFile = new MediaFile
            {
                Filename = _videoUri.LocalPath ?? ""
            };
            MediaFile outputFile = new MediaFile
            {
                Filename = Path.GetTempFileName()
            };

            using Engine engine = new Engine();
            try
            {
                engine.GetMetadata(inputFile);
                engine.GetThumbnail(inputFile, outputFile, new ConversionOptions
                {
                    Seek = TimeSpan.FromSeconds(1.0)
                });

                Image image = new Image();
                image.Source = new BitmapImage(new Uri(outputFile.Filename));
                return image;
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, "Error getting thumbnail: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cuts a segment from the video and saves it to the specified output path.
        /// </summary>
        /// <param name="outputPath">The path to save the cut video segment.</param>
        /// <param name="startTime">The start time of the segment to cut.</param>
        /// <param name="endTime">The end time of the segment to cut.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool CutVideo(string outputPath, TimeSpan startTime, TimeSpan endTime)
        {
            Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, $"Cutting video from {_videoUri.LocalPath} to {outputPath} from {startTime} to {endTime}");

            MediaFile inputFile = new MediaFile
            {
                Filename = _videoUri.LocalPath ?? ""
            };
            MediaFile outputFile = new MediaFile
            {
                Filename = outputPath ?? ""
            };

            Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, "Run MediaToolkit engine for cutting.");

            using Engine engine = new Engine();
            try
            {
                engine.GetMetadata(inputFile);
                ConversionOptions options = new ConversionOptions();
                options.CutMedia(startTime, endTime);
                engine.Convert(inputFile, outputFile, options);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, "Error cutting video: " + ex.Message);
                return false;
            }
        }
    }
}
