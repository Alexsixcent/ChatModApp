using Avalonia;

namespace ChatModApp.Tools;

public static class ControlExtensions
{
    public static bool IsSameValue(this AvaloniaPropertyChangedEventArgs args) => args.OldValue == args.NewValue;
}