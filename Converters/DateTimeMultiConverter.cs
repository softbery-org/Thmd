using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;

// Usage in XAML
// First, register the converter in resources (e.g., in App.xaml or window):
//
// <Window.Resources>
//    <local:DateTimeMultiConverter x:Key="DateTimeMultiConverter" />
// </Window.Resources>
//
// Example binding in TextBlock (displays combined DateTime):
// <TextBlock>
//    <TextBlock.Text>
//        <MultiBinding Converter="{StaticResource DateTimeMultiConverter}">
//            <Binding Path="SelectedDate" />  <!-- Source: date -->
//            <Binding Path="SelectedTime" />  <!-- Source: time -->
//        </MultiBinding>
//    </TextBlock.Text>
// </TextBlock>

/// <summary>
/// Multi-value converter that combines DateTime (date) and TimeSpan (time) into one DateTime.
/// Useful for bindings where date and time are stored separately.
/// </summary>
public class DateTimeMultiConverter : IMultiValueConverter
{
    /// <summary>
    /// Converts an array of values (date and time) to a single DateTime.
    /// </summary>
    /// <param name="values">Array of values: [0] = DateTime (date), [1] = TimeSpan (time).</param>
    /// <param name="targetType">Target type (DateTime).</param>
    /// <param name="parameter">Parameter (unused).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>DateTime with combined date and time; DateTime.MinValue in case of error.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is DateTime date && values[1] is TimeSpan time)
        {
            return date.Date + time; // Combines date with time
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// Converts DateTime back to an array [DateTime (date), TimeSpan (time)].
    /// </summary>
    /// <param name="value">DateTime to decompose.</param>
    /// <param name="targetTypes">Array of target types (DateTime and TimeSpan).</param>
    /// <param name="parameter">Parameter (unused).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>Array with date and time; null in case of error.</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime && targetTypes.Length >= 2)
        {
            return new object[] { dateTime.Date, dateTime.TimeOfDay };
        }
        return null;
    }
}
// Version: 0.1.0.33
