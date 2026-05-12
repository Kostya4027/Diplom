using System;
using System.Globalization;
using System.Windows.Data;

namespace ExamTickets.WPF.Converters
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringEqualsConverter : IValueConverter
    {
        // Преобразует строку в bool (совпадает ли со значением параметра)
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var left = value?.ToString();
            var right = parameter?.ToString();

            if (left is null || right is null)
                return false;

            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        // Преобразует обратно: при Checked возвращаем строковый параметр, иначе ничего не меняем
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return parameter?.ToString();
            }

            return Binding.DoNothing;
        }
    }
}