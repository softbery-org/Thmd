// Version: 0.1.0.35
using System;
using System.Text.RegularExpressions;

namespace Thmd.Helpers;

public class PathCheckHelper
{
	public enum PathEnum
	{
		isNone,
		isFile,
		isUrl
	}

	public static bool IsUrl(string input)
	{
		if (Uri.TryCreate(input, UriKind.Absolute, out var uriResult))
		{
			return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFtp || uriResult.Scheme == Uri.UriSchemeFile;
		}
		return false;
	}

	public static bool IsFilePath(string input)
	{
		bool isWindowsPath = Regex.IsMatch(input, "^[a-zA-Z]:\\\\");
		bool isUnixPath = input.StartsWith("/");
		bool isUncPath = input.StartsWith("\\\\");
		bool hasInvalidUrlChars = Regex.IsMatch(input, "\\s|\\[|\\]|\\{|\\}");
		return isWindowsPath || isUnixPath || isUncPath || hasInvalidUrlChars;
	}

	public static PathEnum Check(string input)
	{
		if (IsUrl(input))
		{
			Console.WriteLine("'" + input + "' to URL");
			return PathEnum.isUrl;
		}
		if (IsFilePath(input))
		{
			Console.WriteLine("'" + input + "' to ścieżka pliku");
			return PathEnum.isFile;
		}
		Console.WriteLine("Nie można określić typu: '" + input + "'");
		return PathEnum.isNone;
	}
}
