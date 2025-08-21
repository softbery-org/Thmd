// Version: 0.1.0.17
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Thmd.Logs;

namespace Thmd.Updates;

public class Updater : IDisposable
{
	private readonly HttpClient _httpClient;

	private bool _disposed = false;

	private AsyncLogger _logger = new AsyncLogger();

	public Version CurrentVersion { get; private set; }

	public Version LatestVersion { get; private set; }

	public string UpdateManifestUrl { get; }

	public string TempFilePath { get; private set; }

	public event EventHandler<UpdateAvailableEventArgs> UpdateAvailable;

	public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

	public event EventHandler UpdateCompleted;

	public event EventHandler<Exception> UpdateFailed;

	public Updater(string updateManifestUrl)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		_httpClient = new HttpClient();
		UpdateManifestUrl = updateManifestUrl;
		CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

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

	public void ApplyUpdate()
	{
		if (!File.Exists(TempFilePath))
		{
			_logger.Log(LogLevel.Error, new string[2] { "Console", "File" }, "Update package not found", new FileNotFoundException("Update package not found"));
		}
		ProcessStartInfo startInfo = new ProcessStartInfo(TempFilePath)
		{
			UseShellExecute = true,
			Verb = "runas"
		};
		Process.Start(startInfo);
		Environment.Exit(0);
	}

	private Version ParseVersionFromManifest(string manifestContent)
	{
		return Version.Parse(manifestContent.Trim());
	}
}
