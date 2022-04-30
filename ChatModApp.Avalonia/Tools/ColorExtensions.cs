namespace ChatModApp.Tools;

using UiColor = Avalonia.Media.Color;
using SysColor = System.Drawing.Color;

public static class ColorExtensions
{
    public static UiColor ToUiColor(this SysColor color, byte opacity = byte.MaxValue) =>
        UiColor.FromArgb(opacity, color.R, color.G, color.B);
}