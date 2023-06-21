using Avalonia;
using Avalonia.Controls.Primitives;

namespace ChatModApp.Controls;

public class ProgressRing : TemplatedControl
{
    public static readonly StyledProperty<bool> IsIndeterminateProperty = AvaloniaProperty.Register<ProgressRing, bool>(
     "IsIndeterminate");

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }
}