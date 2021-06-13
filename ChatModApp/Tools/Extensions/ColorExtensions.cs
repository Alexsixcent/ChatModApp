using UiColor = Windows.UI.Color;
using SysColor = System.Drawing.Color;

namespace ChatModApp.Tools.Extensions
{
    public static class ColorExtensions
    {
        public static UiColor ToUiColor(this SysColor color, byte opacity = byte.MaxValue)
        {
            return UiColor.FromArgb(opacity, color.R, color.G, color.B);
        }
    }
}