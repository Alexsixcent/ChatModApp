using System;
using System.Drawing;
using Windows.UI.Xaml.Data;
using ChatModApp.Tools.Extensions;

namespace ChatModApp.Converters;

public class SystemToUiColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => 
        ((Color)value).ToUiColor();

    public object ConvertBack(object value, Type targetType, object parameter, string language) => 
        ((Windows.UI.Color)value).ToSysColor();
}