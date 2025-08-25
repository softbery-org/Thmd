// Updater.cs
// Version: 0.1.0.78
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Thmd.Logs;

namespace Thmd.Updates;

/// <summary>
/// A class responsible
/// </summary>
public class Updater : IDisposable
{
    // HttpClient instance for making HTTP requests to check for updates and download update packages.
    private readonly HttpClient _httpClient;

    // Flag to indicate whether the object has been disposed.
    private bool _disposed = false;

    // Async logger instance for logging update-related events.
    private AsyncLogger _logger = new AsyncLogger();

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
    public string UpdateManifestUrl { get; }

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
    public event EventHandler<Exception> UpdateFailed;

    /// <summary>
    /// Initializes a new instance of the Updater class with the specified update manifest URL.
    /// </summary>
    /// <param name="updateManifestUrl">The URL to fetch the update manifest from.</param>
    public Updater(string updateManifestUrl)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		_httpClient = new HttpClient();
		UpdateManifestUrl = updateManifestUrl;
		CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
	}

    /// <summary>
    /// Disposes the resources used by the Updater.
    /// </summary>
    public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

    /// <summary>
    /// Disposes the resources used by the Updater.
    /// </summary>
    /// <param name="disposing">true or false</param>
    protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			HttpClient httpClient = _httpClient;
			if (httpClient != null)
			{
				httpClient.Dispose();
			}
		}
		_disposed = true;
	}

    /// <summary>
    /// Checks for updates by fetching the latest version from the update manifest URL
    /// </summary>
    /// <returns>Check for updates</returns>
    public async Task<bool> CheckForUpdatesAsync()
	{
		try
		{
			LatestVersion = ParseVersionFromManifest(await _httpClient.GetStringAsync(UpdateManifestUrl));
			if (LatestVersion > CurrentVersion)
			{
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(LatestVersion));
				_logger.Log(LogLevel.Info, new string[2] { "Console", "File" }, $"New version available: {LatestVersion}");
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
            UpdateFailed?.Invoke(this, ex2);
			_logger.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Error checking for updates: " + ex2.Message);
			return false;
		}
	}

    /// <summary>
    /// Downloads the update package from the specified URL and saves it to a temporary file.
    /// </summary>
    /// <param name="downloadUrl">string</param>
    /// <returns>Download task</returns>
    public async Task DownloadUpdateAsync(string downloadUrl)
	{
		try
		{
			if (!Directory.Exists("update"))
			{
				try
				{
					Directory.CreateDirectory("update");
					_logger.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Directory created: update");
				}
				catch (Exception ex)
				{
					_logger.Log(message: "Error creating update directory: " + ex.Message, level: LogLevel.Error, categories: new string[2] { "Console", "File" });
				}
			}
			TempFilePath = Path.Combine(Path.GetFullPath("update/update"));
			HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl, (HttpCompletionOption)1);
			try
			{
				using Stream streamToRead = await response.Content.ReadAsStreamAsync();
				using FileStream streamToWrite = File.OpenWrite(TempFilePath);
				long totalBytes = response.Content.Headers.ContentLength ?? -1;
				byte[] buffer = new byte[8192];
				long totalBytesRead = 0L;
				while (true)
				{
					int num;
					int bytesRead = num = await streamToRead.ReadAsync(buffer, 0, buffer.Length);
					if (num <= 0)
					{
						break;
					}
					await streamToWrite.WriteAsync(buffer, 0, bytesRead);
					totalBytesRead += bytesRead;
                    ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(totalBytesRead, totalBytes));
				}
			}
			finally
			{
				((IDisposable)response)?.Dispose();
			}
            UpdateCompleted?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
            UpdateFailed?.Invoke(this, ex2);
			_logger.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Error downloading update: " + ex2.Message);
		}
	}

    /// <summary>
    /// Applies the downloaded update by executing the update file and exiting the current
    /// </summary>
    public void ApplyUpdate()
	{
		if (!File.Exists(TempFilePath))
		{
			_logger.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Update package not found", new FileNotFoundException("Update package not found"));
		}
		ProcessStartInfo startInfo = new ProcessStartInfo(TempFilePath)
		{
			UseShellExecute = true,
			Verb = "runas",
		};
		Process.Start(startInfo);
		Environment.Exit(0);
	}

    /// <summary>
    /// Parses the version string from the manifest content.
    /// </summary>
    /// <param name="manifestContent">content represented by string</param>
    /// <returns>version</returns>
    private Version ParseVersionFromManifest(string manifestContent)
	{
		return Version.Parse(manifestContent.Trim());
	}
}
