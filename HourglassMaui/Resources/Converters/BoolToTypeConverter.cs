// HourglassMaui/Resources/Converters/BoolToTypeConverter.cs
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace HourglassMaui.Resources.Converters
{
    public class BoolToTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isWebsite && targetType == typeof(string))
            {
                return isWebsite ? "Website" : "Application";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}