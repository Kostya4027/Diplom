using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExamTickets.WPF.Converters;

public class ValidationMessageBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? Brushes.Green : Brushes.Red;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}