// CommandLineHandler.cs
// Version: 0.1.11.80
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

using Thmd.Logs; // Dla logowania, jeśli dostępne

namespace Thmd.Utilities
{
    public static class CommandLineHandler
    {
        public static double? InitialVolume { get; set; } = 100;
        public static string PlaylistToLoadOnStartup { get; set; } = "playlist.json";
        public static bool StartInFullscreen { get; set; } = false;
        public static string MediaFileToOpenOnStartup { get; set; } = "mediafile.mp4";

        private static readonly (string Flag, string Description)[] AvailableFlags = {
            ("-h", "Display this help message and exit."),
            ("--help", "Display this help message and exit."),
            ("-f <file>", "Open and play the specified media file (e.g., thmdplayer.exe -f \"C:\\video.mp4\")."),
            ("--debug", "run program and run performance monitor for applikation"),
            ("--fullscreen", "Start the player in fullscreen mode."),
            ("--playlist <file>", "Load the specified playlist file (e.g., thmdplayer.exe --playlist \"C:\\playlist.m3u\")."),
            ("--volume <level>", "Set initial volume level (0-100, e.g., thmdplayer.exe --volume 50).")
        };

        public static bool ProcessArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return true;
            }

            bool isHelpRequested = args.Any(arg => arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                                                   arg.Equals("--help", StringComparison.OrdinalIgnoreCase));

            if (isHelpRequested)
            {
                ShowHelp();
                Application.Current?.Shutdown(0);
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "-f":
                    case "--file":
                        if (i + 1 < args.Length)
                        {
                            string filePath = args[++i];
                            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                            {
                                MediaFileToOpenOnStartup = filePath; // Zaktualizowana przestrzeń nazw
                                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Will open file: {filePath}");
                            }
                            else
                            {
                                ShowError("Invalid or non-existent file path provided.");
                            }
                        }
                        else
                        {
                            ShowError("File path expected after -f or --file flag.");
                        }
                        break;
                    case "-d":
                    case "--debug":
                        Console.WriteLine(Process.GetCurrentProcess().ProcessName);
                        Process.Start("PerformanceMonitorApp.exe", "--process " + Process.GetCurrentProcess().ProcessName);
                        break;

                    case "--fullscreen":
                        StartInFullscreen = true; // Zaktualizowana przestrzeń nazw
                        Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, "Starting in fullscreen mode.");
                        break;

                    case "--playlist":
                        if (i + 1 < args.Length)
                        {
                            string playlistPath = args[++i];
                            if (!string.IsNullOrEmpty(playlistPath) && System.IO.File.Exists(playlistPath))
                            {
                                PlaylistToLoadOnStartup = playlistPath; // Zaktualizowana przestrzeń nazw
                                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Will load playlist: {playlistPath}");
                            }
                            else
                            {
                                ShowError("Invalid or non-existent playlist path provided.");
                            }
                        }
                        else
                        {
                            ShowError("Playlist path expected after --playlist flag.");
                        }
                        break;

                    case "--volume":
                        if (i + 1 < args.Length)
                        {
                            string volumeStr = args[++i];
                            if (double.TryParse(volumeStr, out double volume) && volume >= 0 && volume <= 100)
                            {
                                InitialVolume = volume; // Zaktualizowana przestrzeń nazw
                                Logger.Log.Log(LogLevel.Info, new[] { "Console", "File" }, $"Initial volume set to: {volume}");
                            }
                            else
                            {
                                ShowError("Invalid volume level (must be 0-100).");
                            }
                        }
                        else
                        {
                            ShowError("Volume level expected after --volume flag.");
                        }
                        break;

                    default:
                        if (arg.StartsWith("-") || arg.StartsWith("--"))
                        {
                            ShowError($"Unknown flag: {args[i]}");
                        }
                        break;
                }
            }

            return true;
        }

        private static void ShowHelp()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "ThmdPlayer";

            Console.WriteLine($@"
{title} v{version} - Command Line Usage
======================================

Usage: thmdplayer.exe [options]

Available options:
");

            foreach (var (flag, description) in AvailableFlags)
            {
                Console.WriteLine($"  {flag,-20} {description}");
            }

            Console.WriteLine(@"
Examples:
  thmdplayer.exe -h                    (Show this help)
  thmdplayer.exe -f ""C:\Videos\movie.mp4""  (Open and play a specific file)
  thmdplayer.exe --fullscreen          (Start in fullscreen mode)
  thmdplayer.exe --playlist ""playlist.m3u"" --volume 70  (Load playlist and set volume)

For more details, run the application without arguments.
");
        }

        private static void ShowError(string message)
        {
            Console.WriteLine($"Error: {message}");
            Console.WriteLine("Use -h or --help for usage information.");
            Logger.Log.Log(LogLevel.Error, new[] { "Console", "File" }, message);
        }
    }
}
