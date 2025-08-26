// Version: 0.1.1.60
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Thmd.Logs;

namespace Thmd.Media;

public class MediaEditor
{
	private Uri _videoUri;

	public MediaEditor(string video_path)
	{
		_videoUri = new Uri(video_path);
	}

	public MediaEditor(Uri video_uri)
	{
		_videoUri = video_uri;
	}

	public Image GetThumbnail()
	{
		MediaFile inputFile = new MediaFile
		{
			Filename = _videoUri.LocalPath ?? ""
		};
		MediaFile outputFile = new MediaFile
		{
			Filename = Path.GetTempFileName()
		};
		using Engine engine = new Engine();
		try
		{
			engine.GetMetadata(inputFile);
			engine.GetThumbnail(inputFile, outputFile, new ConversionOptions
			{
				Seek = TimeSpan.FromSeconds(1.0)
			});
			Image image = new Image();
			image.Source = new BitmapImage(new Uri(outputFile.Filename));
			return image;
		}
		catch (Exception ex)
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, "Error getting thumbnail: " + ex.Message);
			return null;
		}
	}

	public bool CutVideo(string outputPath, TimeSpan startTime, TimeSpan endTime)
	{
		Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, $"Cutting video from {_videoUri.LocalPath} to {outputPath} from {startTime} to {endTime}");
		MediaFile inputFile = new MediaFile
		{
			Filename = _videoUri.LocalPath ?? ""
		};
		MediaFile outputFile = new MediaFile
		{
			Filename = outputPath ?? ""
		};
		Logger.Log.Log(LogLevel.Info, new string[2] { "File", "Console" }, "Run MediaToolkit engine for cutting.");
		using Engine engine = new Engine();
		try
		{
			engine.GetMetadata(inputFile);
			ConversionOptions options = new ConversionOptions();
			options.CutMedia(startTime, endTime);
			engine.Convert(inputFile, outputFile, options);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log.Log(LogLevel.Error, new string[2] { "File", "Console" }, "Error cutting video: " + ex.Message);
			return false;
		}
	}
}
