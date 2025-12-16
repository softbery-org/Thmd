// Version: 0.1.14.8
using System;
using System.Globalization;
using System.Windows.Data;

namespace Thmd.Converters
{
    /// <summary>
    /// Converts between a percentage value and an absolute width based on a total width parameter.
    /// Useful for dynamic sizing in WPF UI elements, such as progress bars or proportional layouts.
    /// </summary>
    public class PercentageToWidthConverter : IValueConverter
    {
        /// <summary>
        /// Converts a percentage value (0-100) to an absolute width in pixels.
        /// </summary>
        /// <param name="value">The percentage value to convert (double).</param>
        /// <param name="targetType">The target type (typically double for width).</param>
        /// <param name="parameter">The total width used for calculation (double).</param>
        /// <param name="culture">The culture information (unused).</param>
        /// <returns>The calculated width; 0.0 if the conversion fails.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage && parameter is double totalWidth)
            {
                return percentage * totalWidth / 100.0;
            }
            return 0.0;
        }

        /// <summary>
        /// Converts an absolute width back to a percentage value based on the total width.
        /// </summary>
        /// <param name="value">The absolute width to convert (double).</param>
        /// <param name="targetType">The target type (typically double for percentage).</param>
        /// <param name="parameter">The total width used for calculation (double).</param>
        /// <param name="culture">The culture information (unused).</param>
        /// <returns>The calculated percentage; 0.0 if the conversion fails.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is double totalWidth)
            {
                return width * 100.0 / totalWidth;
            }
            return 0.0;
        }
    }
}
