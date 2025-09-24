// Version: 0.1.12.99
using System;

namespace Thmd.Updates;

public class UpdateAvailableEventArgs : EventArgs
{
	public Version NewVersion { get; }

	public UpdateAvailableEventArgs(Version newVersion)
	{
		NewVersion = newVersion;
	}
}
