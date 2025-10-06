using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Converts a numeric value (0-1) to a percentage string.
/// </summary>
public static class PercentageConverter
{
    /// <summary>
    /// Converts the value to percentage (e.g., 0.75 → "75%").
    /// </summary>
    /// <param name="value">Numeric value (double).</param>
    /// <param name="targetType">Target type (string).</param>
    /// <param name="parameter">Optional divisor (default 1).</param>
    /// <param name="culture">Culture.</param>
    /// <returns>String with percentages.</returns>
    public static object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && doubleValue >= 0 && doubleValue <= 1)
        {
            double divisor = parameter is double p ? p : 1.0;
            double percentage = (doubleValue / divisor) * 100;
            return $"{percentage:F0}%";
        }
        return "0%";
    }

    /// <summary>
    /// Converts percentage string back to double (e.g., "50%" → 0.5).
    /// </summary>
    public static object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            if (strValue.EndsWith("%"))
            {
                strValue = strValue.TrimEnd('%');
            }
            if (double.TryParse(strValue, NumberStyles.Any, culture, out double percentage))
            {
                return percentage / 100.0;
            }
        }
        return 0.0;
    }
}
// Version: 0.1.0.15
