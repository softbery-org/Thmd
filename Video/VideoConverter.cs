// Version: 0.1.17.18
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Thmd.Video
{
    /// <summary>
    /// Provides functionality for converting video files using FFmpeg with progress reporting.
    /// </summary>
    public class VideoConverter
    {
        /// <summary>
        /// Occurs when the conversion progress changes (in percent).
        /// </summary>
        public event Action<int> ProgressChanged;

        /// <summary>
        /// Occurs when the conversion process completes successfully.
        /// </summary>
        public event Action<string> ConversionCompleted;

        /// <summary>
        /// Occurs when the conversion process fails.
        /// </summary>
        public event Action<string> ConversionFailed;

        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoConverter"/> class.
        /// </summary>
        /// <param name="ffmpegPath">Path to the FFmpeg executable.</param>
        /// <param name="ffprobePath">Path to the FFprobe executable.</param>
        public VideoConverter(string ffmpegPath = "ffmpeg", string ffprobePath = "ffprobe")
        {
            _ffmpegPath = ffmpegPath;
            _ffprobePath = ffprobePath;
        }

        /// <summary>
        /// Converts a video file to MP4 format using FFmpeg.
        /// Reports conversion progress based on duration from FFprobe.
        /// </summary>
        /// <param name="inputFile">Full path to the input video file.</param>
        /// <param name="outputFile">Full path to the output video file.</param>
        public async Task ConvertToMp4Async(string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
            {
                ConversionFailed?.Invoke($"Input file '{inputFile}' does not exist.");
                return;
            }

            // Step 1: Get total video duration using ffprobe
            double duration = await GetVideoDurationAsync(inputFile);
            if (duration <= 0)
            {
                ConversionFailed?.Invoke("Failed to retrieve video duration.");
                return;
            }

            // Step 2: Start FFmpeg conversion with real-time progress tracking
            string ffmpegArgs = $"-y -i \"{inputFile}\" -c copy -bsf:a aac_adtstoasc " +
                                $"\"{outputFile}\" -progress pipe:2 -nostats";

            try
            {
                using var process = new Process();
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = ffmpegArgs;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.ErrorDataReceived += (s, e) => ParseProgress(e.Data, duration);

                process.Start();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                    ConversionCompleted?.Invoke($"Conversion completed successfully: {outputFile}");
                else
                    ConversionFailed?.Invoke($"Conversion failed. Exit code: {process.ExitCode}");
            }
            catch (Exception ex)
            {
                ConversionFailed?.Invoke($"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Uses FFprobe to retrieve the duration of a video file in seconds.
        /// </summary>
        /// <param name="inputFile">The full path to the input video file.</param>
        /// <returns>The duration of the video in seconds, or 0 if retrieval fails.</returns>
        private async Task<double> GetVideoDurationAsync(string inputFile)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = _ffprobePath;
                process.StartInfo.Arguments =
                    $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
                    return duration;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FFprobe error: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Parses FFmpeg progress output (from -progress pipe:2)
        /// and calculates the conversion percentage based on total duration.
        /// </summary>
        /// <param name="data">A line of text from FFmpeg's stderr stream.</param>
        /// <param name="totalDuration">The total duration of the video in seconds.</param>
        private void ParseProgress(string data, double totalDuration)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            // FFmpeg emits "out_time_ms=xxxxx" lines during conversion
            var match = Regex.Match(data, @"out_time_ms=(\d+)");
            if (match.Success && double.TryParse(match.Groups[1].Value, out double outTimeMs))
            {
                double currentSeconds = outTimeMs / 1_000_000.0;
                int percent = (int)System.Math.Min((currentSeconds / totalDuration) * 100, 100);
                ProgressChanged?.Invoke(percent);
            }
        }
    }
}
