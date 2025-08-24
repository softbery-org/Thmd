// Version: 0.1.0.35
using System;
using System.Globalization;
using System.Windows.Data;
using Thmd.Media;

namespace Thmd.Controls;

public class IsCurrentVideoConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Video video && parameter is Video currentVideo)
		{
			return video == currentVideo;
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
