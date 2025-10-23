using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Converts a bool value to Visibility: true = Visible, false = Collapsed.
/// </summary>
public static class BooleanToVisibilityConverter
{
    /// <summary>
    /// Converts bool to Visibility.
    /// </summary>
    /// <param name="value">The bool value to convert.</param>
    /// <param name="targetType">The target type (Visibility).</param>
    /// <param name="parameter">Parameter (unused).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>Visibility.Visible if true, Visibility.Collapsed if false.</returns>
    public static object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    /// <summary>
    /// Converts Visibility back to bool (optional).
    /// </summary>
    /// <param name="value">The Visibility value to convert.</param>
    /// <param name="targetType">The target type (bool).</param>
    /// <param name="parameter">Parameter (unused).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>true if Visible, false otherwise.</returns>
    public static object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}
// Version: 0.1.0.36
