// Version: 0.1.16.93
using System;

namespace Thmd.Updates;

public class ProgressChangedEventArgs : EventArgs
{
	public long BytesReceived { get; }

	public long TotalBytes { get; }

	public int ProgressPercentage => (int)(BytesReceived * 100 / (TotalBytes > 0 ? TotalBytes : 1));

	public ProgressChangedEventArgs(long bytesReceived, long totalBytes)
	{
		BytesReceived = bytesReceived;
		TotalBytes = totalBytes;
	}
}
