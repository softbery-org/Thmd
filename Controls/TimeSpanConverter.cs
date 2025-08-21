// Version: 0.1.0.17
using System;
using System.Globalization;
using System.Windows.Data;

namespace Thmd.Controls;

public class TimeSpanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is TimeSpan timeSpan)
		{
			return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		}
		return "00:00:00";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
