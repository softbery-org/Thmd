// Version: 0.1.0.17
namespace Thmd.Configuration;

public class UpdateConfig
{
	public bool CheckForUpdates { get; set; }

	public string UpdateUrl { get; set; } = "http://thmdplayer.softbery.org/update.rar";

	public string UpdatePath { get; set; } = "update";

	public string UpdateFileName { get; set; } = "update";

	public string Version { get; set; } = "1.0.0";

	public string VersionUrl { get; set; } = "http://thmdplayer.softbery.org/version.txt";

	public int UpdateInterval { get; set; } = 86400;

	public int UpdateTimeout { get; set; } = 30;
}
