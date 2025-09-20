# Thmd Project Documentation

## Introduction

The Thmd project is a comprehensive media player application built using C# with WPF (Windows Presentation Foundation) on the .NET 4.8 framework. It provides a robust set of features for media playback, including support for video and audio files, subtitles, playlists, repeat modes, volume control, fullscreen toggling, and more. The codebase integrates third-party libraries like LibVLCSharp for media playback, SharpCompress for archive extraction, MediaToolkit for media editing, and Newtonsoft.Json for configuration handling.

Key components include:
- **Logging System**: A customizable, asynchronous logging mechanism with support for console and file sinks, category filtering, and metrics.
- **Configuration Management**: A singleton-based configuration class for managing app settings, including logging, subtitles, updates, and playlists.
- **Media Controls**: UserControls for player interfaces, such as control bars, progress bars, repeat controls, and subtitle displays.
- **Media Handling**: Classes for media items, streams, and editors, supporting local files, HLS streaming, and metadata extraction.
- **Utilities**: Helpers for resizing controls, storyboard animations, fullscreen management, and command-line argument processing.

The application emphasizes thread-safe operations, asynchronous tasks for non-blocking UI, and error handling with logging. It is designed for desktop environments, focusing on user-friendly media playback with extensibility through plugins and updates.

- **Version Information**: Various components range from 0.1.0.0 to 0.1.12.71.
- **Namespaces**: `Thmd`, `Thmd.Compress.Rar`, `Thmd.Compress.Zip`, `Thmd.Configuration`, `Thmd.Consolas`, `Thmd.Controls`, `Thmd.Controls.ControlButtons`, `Thmd.Converters`, `Thmd.Logs`, `Thmd.Media`, `Thmd.Media.Effects`, `Thmd.Properties`, `Thmd.Repeats`, `Thmd.Subtitles`, `Thmd.Templates`, `Thmd.Themes`, `Thmd.Updates`, `Thmd.Utilities`, `Thmd.Windowses`.

## Classes

### Logger (in Thmd)

Static class for centralized logging using an `AsyncLogger` instance.

#### Properties
- `Config` (Config): Configuration settings for the logger.
- `Log` (AsyncLogger): The logger instance.

#### Methods
- `AsyncLogger InitLogs()`: Initializes the logging system with console and file sinks.
- `void AddLog(LogLevel level, string message, string[] category = null, Exception exception = null)`: Adds a log entry, extending categories if needed.

### Rarer (in Thmd.Compress.Rar)

Class for extracting RAR archives.

#### Properties
- `_progress` (Action<double>): Progress callback.
- `_cancellationToken` (CancellationToken): Token for cancellation.

#### Methods
- `void UnRar(string source_path, string target_directory)`: Extracts RAR file to directory, handling errors.

### Zipper (in Thmd.Compress.Zip)

Empty class (placeholder for ZIP handling).

### Config (in Thmd.Configuration)

Singleton for application configuration, loaded/saved from JSON.

#### Properties
- `DatabaseConnectionString` (string): DB connection.
- `MaxConnections` (int): Max DB connections.
- `EnableLogging` (bool): Logging enabled.
- `LogsDirectoryPath` (string): Log directory.
- `ApiKey` (string): API key.
- `LibVlcPath` (string): VLC path (default: "libvlc").
- `EnableLibVlc` (bool): VLC enabled.
- `EnableConsoleLogging` (bool): Console logging.
- `LogLevel` (LogLevel): Logging level.
- `SubtitleConfig` (SubtitleConfig): Subtitle settings.
- `Instance` (Config): Singleton instance.
- `PlaylistConfig` (IPlaylistConfig): Playlist config.
- `UpdateConfig` (UpdateConfig): Update config.
- `PerformanceMonitor` (PerformanceMonitorConfig): Performance monitor (typo in code: "PerhormanceMonitor").

#### Constructors
- `Config()`: Sets defaults.

#### Methods
- `static T LoadFromJsonFile<T>(string filePath)`: Loads config from JSON, creates default if missing.
- `static void SaveToFile(string filePath, object obj)`: Saves to JSON.
- `void UpdateAndSave(Action<Config> updateAction, string filePath = "config.json")`: Updates and saves thread-safely.

### PlaylistConfig (in Thmd.Configuration) : IPlaylistConfig

Class for playlist configuration.

#### Properties
- `Name` (string): Playlist name.
- `EnableShuffle` (bool): Shuffle enabled (default: true).
- `Repeat` (string): Repeat mode (default: "None").
- `AutoPlay` (bool): Auto-play (default: true).
- `MediaList` (List<string>): Media paths.
- `Container` (PlaylistContainer): Position and size.

### PlaylistContainer (in Thmd.Configuration)

Class for playlist UI container.

#### Properties
- `Position` (Thickness): Margin (default: 0).
- `Size` (Size): Size (default: 300x400).

### IPlaylistConfig (in Thmd.Configuration)

Interface for playlist config (properties as in PlaylistConfig).

### PerformanceMonitorConfig (in Thmd.Configuration)

Class for performance monitoring config.

#### Properties
- `EnablePerformanceMonitoring` (bool): Enabled (default: false).
- `MonitoringInterval` (int): Interval in seconds (default: 60).
- `LogFilePath` (string): Log path (default: "performance.log").

#### Constructors
- `PerformanceMonitorConfig()`: Sets defaults.

### PluginConfig (in Thmd.Configuration)

Class for plugin config.

#### Properties
- `PluginName` (string): Name.
- `PluginPath` (string): Path.
- `IsEnabled` (bool): Enabled.
- `Version` (string): Version.
- `Description` (string): Description.

### Shadow (in Thmd.Configuration)

Class for shadow effect config.

#### Properties
- `Color` (Color): Color (default: Black).
- `ShadowDepth` (double): Depth (default: 0.0).
- `Opacity` (double): Opacity (default: 0.5).
- `BlurRadius` (double): Blur (default: 10.0).
- `Visible` (bool): Visible (default: true).

### SubtitleConfig (in Thmd.Configuration)

Class for subtitle config.

#### Properties
- `FontSize` (double): Size.
- `FontFamily` (FontFamily): Family.
- `FontColor` (Brush): Color.
- `Shadow` (Shadow): Shadow settings.

#### Constructors
- `SubtitleConfig(double size, string fontfamily, Brush color, bool show_shadow, Shadow shadow = null)`: Initializes with values.

### UpdateConfig (in Thmd.Configuration)

Class for update config.

#### Properties
- `CheckForUpdates` (bool): Check updates (default: true).
- `UpdateUrl` (string): Update URL (default: "http://thmdplayer.softbery.org/update.rar").
- `UpdatePath` (string): Path (default: "update").
- `UpdateFileName` (string): Filename (default: "update.rar").
- `Version` (string): Version (default: "1.0.0").
- `VersionUrl` (string): Version URL (default: "http://thmdplayer.softbery.org/version.txt").
- `UpdateInterval` (int): Interval seconds (default: 86400).
- `UpdateTimeout` (int): Timeout seconds (default: 30).

### ConsoleWriteLine (in Thmd.Consolas)

Static extension class for console output.

#### Delegates
- `GetClassNameDelegate(object sender)`: Returns class name.

#### Methods
- `private string GetName(object sender)`: Gets class name.
- `void WriteLine(this object sender, Exception ex)`: Logs exception.
- `void WriteLine(this object sender, string msg)`: Logs message.

### VideoTypeChecker (in Thmd.Utilities)

Class to check video file types.

#### Properties
- `IsAvi` (bool): AVI file.
- `IsMp4` (bool): MP4 file.
- `IsM3u8` (bool): M3U8 file.

#### Constructors
- `VideoTypeChecker(string filePath)`: Initializes based on file extension.

#### Methods
- `DownloadM3u8(string outputPath)`: Downloads M3U8 stream.
- `async Task<string[]> ParseM3u8Async()`: Parses M3U8 playlist.
- `Stream GetStream()`: Returns stream for M3U8.
- `ValidateM3u8()`: Validates M3U8 file.
- `override string ToString()`: Returns video type.

### WindowLastStance (in Thmd.Utilities)

Class for storing window state.

#### Properties
- `State` (WindowState): Window state.
- `Mode` (ResizeMode): Resize mode.
- `Style` (WindowStyle): Window style.

### WindowPropertiesExtensions (in Thmd.Windowses)

Static class for window positioning extensions.

#### Structs
- `W32Point`: X, Y coordinates.
- `W32MonitorInfo`: Monitor info.
- `W32Rect`: Rectangle coordinates.

#### Methods
- `bool ActivateCenteredToMouse(this Window window)`: Activates window centered to mouse.
- `void ShowCenteredToMouse(this Window window)`: Shows window centered to mouse.
- `private void ComputeTopLeft(ref Window window)`: Computes top-left position.

## Suggested Improvements and Additions

### Improvements

1. **Zipper Class Completion (Thmd.Compress.Zip)**:
   - The `Zipper` class is currently empty. Implement ZIP file handling using SharpCompress for compression and extraction, similar to `Rarer`. Add methods like `Zip` and `UnZip` with progress callbacks and cancellation support.
   - Example:
  ```csharp
     public void Zip(string sourcePath, string targetFile, Action<double> progress = null, CancellationToken token = default)
     {
         // Implement ZIP creation with SharpCompress
     }
  ```

   2. **Logging Enhancements (Logger)**:
  

- Add support for additional sinks (e.g., database, network) for centralized logging in distributed systems.
- Introduce log rotation policies based on time (e.g., daily logs) in FileSink.
- Add structured logging support (e.g., key-value pairs) for better analytics.
Example:
```csharp
public static void AddStructuredLog(LogLevel level, Dictionary<string, object> properties, string message)
{
    _log.Log(level, _categories.ToArray(), JsonConvert.SerializeObject(properties), null);
}
```

3. **Config Validation (Config)**:

-Add validation for configuration properties (e.g., valid LogsDirectoryPath, ApiKey format).
-Use data annotations or a custom validator to ensure settings are valid before saving.
Example:
```csharp
public void Validate()
{
    if (string.IsNullOrEmpty(LogsDirectoryPath) || !Directory.Exists(LogsDirectoryPath))
        throw new ConfigurationException("Invalid LogsDirectoryPath");
}
```

4. **Error Handling in Rarer**:

- Improve error messages in `UnRar` by including specific error details (e.g., file corruption, insufficient permissions).
- Log exceptions using `Logger.AddLog` instead of `WriteLine`.
Example:
```csharp
catch (Exception ex)
{
    Logger.AddLog(LogLevel.Error, $"Failed to extract RAR: {ex.Message}", new[] { "File" }, ex);
}
```


5. **VideoTypeChecker Enhancements**:

- Expand file type detection beyond extensions by checking file headers (e.g., MP4 magic numbers).
- Add support for more formats (e.g., MKV, WMV).
Example:
```csharp
public bool IsMkv => Path.GetExtension(_filePath).ToLowerInvariant() == ".mkv" || CheckMkvHeader();
private bool CheckMkvHeader()
{
    using (var stream = File.OpenRead(_filePath))
    {
        byte[] header = new byte[4];
        stream.Read(header, 0, 4);
        return header.SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }); // MKV header
    }
}
```


6. **WindowPropertiesExtensions Robustness**:

- Handle multi-monitor edge cases better (e.g., negative coordinates, virtual desktops).
- Add DPI awareness for high-resolution displays.
Example:
```csharp
private static void ComputeTopLeft(ref Window window)
{
    var dpi = VisualTreeHelper.GetDpi(window);
    // Adjust calculations for DPI scaling
}
```


7. **Performance Monitoring**:

- Fix typo in `PerformanceMonitor` property name (PerhormanceMonitor → PerformanceMonitor).
- Add CPU/memory usage tracking in `PerformanceMonitorConfig`.
Example:
```csharp
public double CpuUsage { get; private set; }
public void Monitor()
{
    using (var counter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
    {
        CpuUsage = counter.NextValue();
    }
}
```


8. **Thread Safety in Logger**:

- Ensure _categories list is thread-safe using `ConcurrentBag<string>` or `locking`.
Example:
```csharp
private static readonly ConcurrentBag<string> _categories = new ConcurrentBag<string> { "Console", "File" };
```
```csharp




### Additions

1. **Cloud Integration**:

- Add support for cloud storage (e.g., Dropbox, Google Drive) for media files and playlists.
- Implement a CloudMediaStream class in Thmd.Media.
Example:
```csharp
public class CloudMediaStream : IMediaStream
{
    public async Task<Stream> GetStreamAsync(string cloudUrl, CancellationToken token)
    {
        // Integrate with cloud SDK
    }
}
```


2. **Plugin System Expansion**:

- Enhance PluginConfig with a plugin manager to dynamically load/unload plugins (e.g., codecs, UI themes).
Example:
```csharp
public class PluginManager
{
    public void LoadPlugin(string path)
    {
        var assembly = Assembly.LoadFrom(path);
        // Initialize plugin
    }
}
```


3. **Subtitle Synchronization**:

- Add subtitle delay adjustment in SubtitleConfig and SubtitleControl.
Example:
```csharp
public double SubtitleDelay { get; set; } // In milliseconds
public void AdjustSubtitleTiming(double delay)
{
    SubtitleDelay += delay;
    // Update subtitle rendering
}
```


4. **Accessibility Features**:

- Add keyboard shortcuts for media controls (e.g., space for play/pause).
- Implement screen reader support for ControlBar and SubtitleControl.
Example:
```csharp
public class ControlBar
{
    private void InitializeShortcuts()
    {
        InputBindings.Add(new KeyBinding(new RelayCommand(TogglePlayPause), Key.Space, ModifierKeys.None));
    }
}
```


5. **Analytics Integration**:

- Add telemetry for usage tracking (e.g., most played media, errors) with opt-in consent.
Example:
```csharp
public class Analytics
{
    public void TrackEvent(string eventName, Dictionary<string, object> properties)
    {
        if (Config.Instance.EnableAnalytics)
        {
            // Send to analytics service
        }
    }
}
```


6. **Unit Tests**:

- Add unit tests for critical components (Logger, VideoTypeChecker, Config) using MSTest or NUnit.
Example:
```csharp
[TestClass]
public class LoggerTests
{
    [TestMethod]
    public void AddLog_ShouldAddCategory()
    {
        Logger.AddLog(LogLevel.Info, "Test", new[] { "TestCategory" });
        Assert.IsTrue(Logger.Categories.Contains("TestCategory"));
    }
}
```


7. **UI Themes**:

- Expand Thmd.Themes with dynamic theme switching (e.g., light/dark modes).
Example:
```csharp
public class ThemeManager
{
    public void ApplyTheme(string themeName)
    {
        ResourceDictionary theme = new ResourceDictionary { Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative) };
        Application.Current.Resources.MergedDictionaries.Add(theme);
    }
}
```


8. **Media Metadata Extraction**:

- Enhance VideoItem to extract metadata (e.g., title, artist, album) using TagLibSharp.
Example:
```csharp
public Dictionary<string, string> GetMetadata()
{
    using (var file = TagLib.File.Create(Uri.LocalPath))
    {
        return new Dictionary<string, string>
        {
            { "Title", file.Tag.Title },
            { "Artist", file.Tag.FirstPerformer }
        };
    }
}
```



### Usage Example

```csharp
using Thmd;

class Program
{
    static void Main()
    {
        Logger.InitLogs();
        var config = Config.Instance;
        config.Validate(); // Validate config
        Logger.AddLog(LogLevel.Info, "Application started", new[] { "Startup" });
        // Initialize player with cloud integration
        var player = new Player();
        player.Play(new CloudMediaStream("https://cloud.example.com/video.mp4"));
    }
}
```

### Conclusion
The proposed improvements enhance robustness, performance, and extensibility, while the additions introduce modern features like cloud integration, accessibility, and analytics. These changes would make Thmd a more competitive and user-friendly media player, aligning with current industry standards.