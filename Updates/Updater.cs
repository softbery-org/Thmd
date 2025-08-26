// Updater.cs
// Version: 0.1.2.9
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

using SharpCompress.Archives;
using SharpCompress.Common;

using Thmd.Configuration;
using Thmd.Logs;

namespace Thmd.Updates;

/// <summary>
/// A class responsible for checking, downloading, and applying application updates.
/// Uses configuration settings from the Config class and supports asynchronous operations
/// with progress reporting and error handling.
/// </summary>
public class Updater : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AsyncLogger _logger;
    private bool _disposed = false;
    private readonly Config _config;

    /// <summary>
    /// Gets the current version of the application.
    /// </summary>
    public Version CurrentVersion { get; private set; }

    /// <summary>
    /// Gets the latest version available from the update manifest.
    /// </summary>
    public Version LatestVersion { get; private set; }

    /// <summary>
    /// Gets the URL to fetch the update manifest from.
    /// </summary>
    public string UpdateManifestUrl => _config.UpdateConfig.VersionUrl;

    /// <summary>
    /// Gets the path to the temporary file where the update package is downloaded.
    /// </summary>
    public string TempFilePath { get; private set; }

    /// <summary>
    /// Occurs when a new update is available.
    /// </summary>
    public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

    /// <summary>
    /// Occurs when there is progress in downloading the update.
    /// </summary>
    public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

    /// <summary>
    /// Occurs when the update process is completed successfully.
    /// </summary>
    public event EventHandler UpdateCompleted;

    /// <summary>
    /// Occurs when an error happens during the update process.
    /// </summary>
    public event EventHandler<UpdateErrorEventArgs> UpdateFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Updater"/> class using settings from the <see cref="Config"/> class.
    /// </summary>
    public Updater()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Config.Instance.UpdateConfig.UpdateTimeout)
        };
        _logger = new AsyncLogger();
        _config = Config.Instance;
        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0, 99);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="Updater"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="Updater"/>.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            _httpClient?.Dispose();
            _logger?.Dispose();
        }
        _disposed = true;
    }

    /// <summary>
    /// Checks for updates by fetching the latest version from the update manifest URL.
    /// </summary>
    /// <returns>True if an update is available; otherwise, false.</returns>
    public async Task<bool> CheckForUpdatesAsync()
    {
        if (!_config.UpdateConfig.CheckForUpdates)
        {
            _logger.Log(LogLevel.Info, new[] { "Console", "File" }, "Update checking is disabled in configuration.");
            return false;
        }

        try
        {
            string manifestContent = await _httpClient.GetStringAsync(UpdateManifestUrl);
            LatestVersion = ParseVersionFromManifest(manifestContent);
            if (LatestVersion > CurrentVersion)
            {
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(LatestVersion));
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"New version available: {LatestVersion}");
                return true;
            }
            _logger.Log(LogLevel.Info, new[] { "Console", "File" }, "No new updates available.");
            return false;
        }
        catch (Exception ex)
        {
            UpdateFailed?.Invoke(this, new UpdateErrorEventArgs(ex));
            _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Error checking for updates: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Downloads the update package from the configured URL and saves it to a temporary file.
    /// </summary>
    /// <returns>A task representing the asynchronous download operation.</returns>
    public async Task DownloadUpdateAsync()
    {
        try
        {
            string updateDir = Path.GetFullPath(_config.UpdateConfig.UpdatePath);
            if (!Directory.Exists(updateDir))
            {
                Directory.CreateDirectory(updateDir);
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Directory created: {updateDir}");
            }

            TempFilePath = Path.Combine(updateDir, _config.UpdateConfig.UpdateFileName);
            using HttpResponseMessage response = await _httpClient.GetAsync(_config.UpdateConfig.UpdateUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using Stream streamToRead = await response.Content.ReadAsStreamAsync();
            using FileStream streamToWrite = File.OpenWrite(TempFilePath);
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            byte[] buffer = new byte[8192];
            long totalBytesRead = 0;

            while (true)
            {
                int bytesRead = await streamToRead.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead <= 0)
                {
                    break;
                }
                await streamToWrite.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
                ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(totalBytesRead, totalBytes));
            }

            UpdateCompleted?.Invoke(this, EventArgs.Empty);
            _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Update downloaded to: {TempFilePath}");
        }
        catch (Exception ex)
        {
            UpdateFailed?.Invoke(this, new UpdateErrorEventArgs(ex));
            _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Error downloading update: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Applies the downloaded update by extracting the archive (RAR or ZIP) and replacing files in the current application directory, then exiting the application.
    /// </summary>
    public void ApplyUpdate()
    {
        if (string.IsNullOrEmpty(TempFilePath) || !File.Exists(TempFilePath))
        {
            var ex = new FileNotFoundException($"Update package not found at: {TempFilePath}");
            UpdateFailed?.Invoke(this, new UpdateErrorEventArgs(ex));
            _logger.Log(LogLevel.Error, new[] { "Console", "File" }, ex.Message, ex);
            return;
        }

        try
        {
            string updateDir = Path.GetFullPath(_config.UpdateConfig.UpdatePath);
            string extractDir = Path.Combine(updateDir, "extracted");
            string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("Unable to determine application directory.");
            string extension = Path.GetExtension(TempFilePath)?.ToLowerInvariant();

            // Create extraction directory
            if (!Directory.Exists(extractDir))
            {
                Directory.CreateDirectory(extractDir);
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Extraction directory created: {extractDir}");
            }

            // Extract the archive based on its type
            if (extension == ".zip")
            {
                ZipFile.ExtractToDirectory(TempFilePath, extractDir, System.Text.Encoding.UTF8);
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Extracted ZIP archive to: {extractDir}");
            }
            else if (extension == ".rar")
            {
                using var archive = ArchiveFactory.Open(TempFilePath);
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(extractDir, new ExtractionOptions
                        {
                            Overwrite = true,
                            ExtractFullPath = true
                        });
                    }
                }
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Extracted RAR archive to: {extractDir}");
            }
            else
            {
                var ex = new NotSupportedException($"Unsupported archive format: {extension}");
                UpdateFailed?.Invoke(this, new UpdateErrorEventArgs(ex));
                _logger.Log(LogLevel.Error, new[] { "Console", "File" }, ex.Message, ex);
                return;
            }

            // Copy extracted files to the application directory, overwriting existing files
            CopyDirectory(extractDir, appDir);
            _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Copied extracted files to: {appDir}");

            // Clean up: Delete extracted directory and original archive
            try
            {
                Directory.Delete(extractDir, recursive: true);
                File.Delete(TempFilePath);
                _logger.Log(LogLevel.Info, new[] { "Console", "File" }, $"Cleaned up: {extractDir} and {TempFilePath}");
            }
            catch (Exception cleanupEx)
            {
                _logger.Log(LogLevel.Warning, new[] { "Console", "File" },
                    $"Failed to clean up extracted files: {cleanupEx.Message}", cleanupEx);
            }

            _logger.Log(LogLevel.Info, new[] { "Console", "File" }, "Update applied successfully. Exiting application.");
            ScheduleFileReplacement(extractDir, appDir);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            UpdateFailed?.Invoke(this, new UpdateErrorEventArgs(ex));
            _logger.Log(LogLevel.Error, new[] { "Console", "File" }, $"Error applying update: {ex.Message}", ex);
        }
    }

    private void ScheduleFileReplacement(string sourceDir, string destDir)
    {
        string batchFile = Path.Combine(_config.UpdateConfig.UpdatePath, "update.bat");
        string script = $"@echo off\n" +
                        $"timeout /t 2\n" +
                        $"xcopy \"{sourceDir}\" \"{destDir}\" /E /H /C /I /Y\n" +
                        $"del \"{batchFile}\"\n";
        File.WriteAllText(batchFile, script);
        Process.Start(new ProcessStartInfo
        {
            FileName = batchFile,
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    /// <summary>
    /// Recursively copies all files and directories from the source directory to the destination directory, overwriting existing files.
    /// </summary>
    /// <param name="sourceDir">The source directory to copy from.</param>
    /// <param name="destDir">The destination directory to copy to.</param>
    private void CopyDirectory(string sourceDir, string destDir)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] subDirs = dir.GetDirectories();
            FileInfo[] files = dir.GetFiles();

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (FileInfo file in files)
            {
                string destFilePath = Path.Combine(destDir, file.Name);
                try
                {
                    file.CopyTo(destFilePath, overwrite: true);
                    _logger.Log(LogLevel.Info, new[] { "Console", "File" },
                        $"Copied file: {destFilePath}");
                }
                catch (IOException ioEx)
                {
                    _logger.Log(LogLevel.Warning, new[] { "Console", "File" },
                        $"Failed to copy file {file.FullName}: {ioEx.Message}", ioEx);
                    // Continue copying other files
                }
            }

            foreach (DirectoryInfo subDir in subDirs)
            {
                string destSubDir = Path.Combine(destDir, subDir.Name);
                CopyDirectory(subDir.FullName, destSubDir);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses the version string from the manifest content.
    /// </summary>
    /// <param name="manifestContent">The content of the update manifest.</param>
    /// <returns>The parsed version.</returns>
    /// <exception cref="ArgumentException">Thrown if the manifest content is not a valid version string.</exception>
    private Version ParseVersionFromManifest(string manifestContent)
    {
        if (string.IsNullOrWhiteSpace(manifestContent) || !Version.TryParse(manifestContent.Trim(), out var version))
        {
            throw new ArgumentException("Invalid version format in manifest content.");
        }
        return version;
    }
}

/// <summary>
/// Event arguments for the UpdateFailed event.
/// </summary>
public class UpdateErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred during the update process.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateErrorEventArgs"/> class with the specified exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <exception cref="ArgumentNullException">Thrown if the exception is null.</exception>
    public UpdateErrorEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
