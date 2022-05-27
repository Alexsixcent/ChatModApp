using System;
using System.Drawing;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatModApp.Tools;

public class SystemColorToUiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => ((Color?)value)?.ToUiColor();
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}