using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Formats a string according to the given pattern (e.g., "{0:C}" for currency).
/// </summary>
public static class StringFormatConverter
{
    /// <summary>
    /// Formats the value according to the parameter (string as format).
    /// </summary>
    /// <param name="value">The value to format (e.g., double).</param>
    /// <param name="targetType">The target type (string).</param>
    /// <param name="parameter">The format pattern (e.g., "C" for currency).</param>
    /// <param name="culture">The culture for formatting.</param>
    /// <returns>The formatted string.</returns>
    public static object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && parameter is string format)
        {
            return string.Format(culture ?? CultureInfo.CurrentCulture, "{0:" + format + "}", value);
        }
        return value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Reverse conversion is not supported (returns null).
    /// </summary>
    public static object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null; // Or parsing implementation if needed
    }
}
// Version: 0.1.0.15
