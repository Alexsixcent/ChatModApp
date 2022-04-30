using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Color = System.Drawing.Color;

namespace ChatModApp.Tools;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Convert((Color?)value);

    private static IBrush Convert(Color? col)
    {
        return new ImmutableSolidColorBrush(col?.ToUiColor() ?? Colors.White);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}