using System;
using System.Globalization;
using System.Windows.Input;

// Usage in XAML:
// First, add the converter to resources (e.g., in App.xaml or window):
//
// <Window.Resources>
//      <local:EnumToBooleanConverter x:Key="EnumToBool" />
// </Window.Resources>
//
// Example with enum Status { Active, Inactive }:
// <CheckBox IsChecked="{Binding CurrentStatus, Converter={StaticResource EnumToBool}, ConverterParameter=Active}"
//          Translate="Active" />

/// <summary>
/// Converts an enum value to bool: true if the enum value matches the parameter.
/// Useful for bindings with checkboxes or radiobuttons based on enums.
/// </summary>
public static class EnumToBooleanConverter
{
    /// <summary>
    /// Converts an enum value to bool.
    /// </summary>
    /// <param name="value">The current enum value to check.</param>
    /// <param name="targetType">The target type (bool).</param>
    /// <param name="parameter">The expected enum value (as string name or enum object).</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>true if value == parameter; false otherwise.</returns>
    public static object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var enumType = value.GetType();
        if (!enumType.IsEnum)
            return false;

        object expectedValue;
        if (parameter is string paramString)
        {
            if (Enum.IsDefined(enumType, paramString))
            {
                expectedValue = Enum.Parse(enumType, paramString);
            }
            else
            {
                return false;
            }
        }
        else
        {
            expectedValue = parameter;
        }

        return value.Equals(expectedValue);
    }

    /// <summary>
    /// Converts bool back to enum (optional: returns parameter if true).
    /// </summary>
    /// <param name="value">The bool value to convert.</param>
    /// <param name="targetType">The target type (enum).</param>
    /// <param name="parameter">The expected enum value.</param>
    /// <param name="culture">Culture (unused).</param>
    /// <returns>Parameter (enum value) if true; null otherwise.</returns>
    public static object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return parameter;
        }
        return null;
    }
}
// Version: 0.1.0.15
