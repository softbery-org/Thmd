using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Inverts a bool value: true becomes false and vice versa.
/// </summary>
public static class InverseBooleanConverter
{
    /// <summary>
    /// Inverts bool.
    /// </summary>
    /// <param name="value">The bool value to invert.</param>
    /// <param name="targetType">Target type (bool).</param>
    /// <param name="parameter">Parameter (unused).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>The inverted bool value.</returns>
    public static object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    /// <summary>
    /// Inverts bool back (same as Convert).
    /// </summary>
    public static object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}
// Version: 0.1.0.35
