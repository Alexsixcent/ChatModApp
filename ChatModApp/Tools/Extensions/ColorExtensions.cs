using UiColor = Windows.UI.Color;
using SysColor = System.Drawing.Color;

namespace ChatModApp.Tools.Extensions
{
    public static class ColorExtensions
    {
        public static UiColor ToUiColor(this SysColor color, byte opacity = byte.MaxValue) =>
            UiColor.FromArgb(opacity, color.R, color.G, color.B);

        public static SysColor ToSysColor(this UiColor color) =>
            SysColor.FromArgb(color.A, color.R, color.G, color.B);
    }
}