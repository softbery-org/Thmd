using System;
using System.Globalization;
using System.Windows.Data;

// Usage in XAML:
// 
//<TextBlock Text = "{MultiBinding Converter={StaticResource ArithmeticMultiConverter}, 
//                  ConverterParameter='+',
//                  <Binding Path="Price" />,
//                  <Binding Path="Tax" />}"
//              />

/// <summary>
/// Multi-value converter for performing arithmetic operations on two numbers (e.g., addition).
/// </summary>
public class ArithmeticMultiConverter : IMultiValueConverter
{
    /// <summary>
    /// Adds two numeric values.
    /// </summary>
    /// <param name="values">Array: [0] = first number, [1] = second number.</param>
    /// <param name="targetType">Target type (double).</param>
    /// <param name="parameter">Operator: "+" (default), "-" or "*".</param>
    /// <param name="culture">Culture.</param>
    /// <returns>Operation result; 0.0 in case of error.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is double num1 && values[1] is double num2)
        {
            var op = parameter as string ?? "+";
            return op switch
            {
                "+" => num1 + num2,
                "-" => num1 - num2,
                "*" => num1 * num2,
                _ => num1 + num2
            };
        }
        return 0.0;
    }

    /// <summary>
    /// Reverse conversion is not supported (returns null).
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return null; // For one-way operations
    }
}
// Version: 0.1.0.38
