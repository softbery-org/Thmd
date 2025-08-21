// Version: 0.1.0.18
using System;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using Thmd.Logs;

namespace Thmd.Configuration;

public class Config
{
	private static readonly object _lock = new object();

	private static Config _instance;

	public string DatabaseConnectionString { get; set; }

	public int MaxConnections { get; set; }

	public bool EnableLogging { get; set; }

	public string LogsDirectoryPath { get; set; }

	public string ApiKey { get; set; }

	public string LibVlcPath { get; set; }

	public bool EnableLibVlc { get; set; }

	public bool EnableConsoleLogging { get; set; }

	public LogLevel LogLevel { get; set; }

	public SubtitleConfig SubtitleConfig { get; set; }

	public UpdateConfig UpdateConfig { get; set; }

	public PluginConfig[] Plugins { get; set; } = new PluginConfig[0];

	public static Config Instance
	{
		get
		{
			lock (_lock)
			{
				return _instance ?? (_instance = LoadFromFile("config.json"));
			}
		}
	}

	public Config()
	{
		DatabaseConnectionString = "server=localhost;connection=default";
		MaxConnections = 10;
		EnableLogging = true;
		LogsDirectoryPath = "logs";
		ApiKey = "default-key";
		LibVlcPath = "libvlc";
		EnableLibVlc = true;
		LogLevel = LogLevel.Info;
		SubtitleConfig = new SubtitleConfig(24.0, "Arial", Brushes.WhiteSmoke, show_shadow: true, new Shadow());
		UpdateConfig = new UpdateConfig
		{
			CheckForUpdates = true,
			UpdateUrl = "http://thmdplayer.softbery.org/update.rar",
			UpdatePath = "update",
			UpdateFileName = "update",
			Version = "3.0.0",
			VersionUrl = "http://thmdplayer.softbery.org/version.txt",
			UpdateInterval = 86400,
			UpdateTimeout = 30
		};
	}

	public static Config LoadFromFile(string filePath)
	{
		try
		{
			Console.WriteLine(filePath);
			if (!File.Exists(filePath))
			{
				Config defaultConfig = new Config();
				defaultConfig.SaveToFile(filePath);
				return defaultConfig;
			}
			string json = File.ReadAllText(filePath);
			return JsonConvert.DeserializeObject<Config>(json);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Błąd ładowania konfiguracji: " + ex.Message, ex);
		}
	}

	public void SaveToFile(string filePath)
	{
		try
		{
			FileInfo file_info = new FileInfo(filePath);
			string directory = file_info.Directory.FullName;
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(filePath, json);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Błąd zapisu konfiguracji: " + ex.Message, ex);
		}
	}

	public void UpdateAndSave(Action<Config> updateAction, string filePath = "config.json")
	{
		lock (_lock)
		{
			updateAction(this);
			SaveToFile(filePath);
		}
	}
}
