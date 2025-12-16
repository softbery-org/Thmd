// Version: 0.1.17.20
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Thmd.Media;

namespace Thmd.Utilities;

public class VideoTypeChecker
{
	private readonly string _filePath;

	public bool IsAvi { get; }

	public bool IsMp4 { get; }

	public bool IsM3u8 { get; }

	public VideoTypeChecker(string filePath)
	{
		_filePath = filePath;
		string extension = Path.GetExtension(filePath).ToLowerInvariant();
		IsAvi = extension == ".avi";
		IsMp4 = extension == ".mp4";
		IsM3u8 = extension == ".m3u8";
	}
}
