using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ChatModApp.Controls;

public class LoadingContentControl : TemplatedControl
{
    public static readonly StyledProperty<LoadingState> StateProperty =
        AvaloniaProperty.Register<LoadingContentControl, LoadingState>(nameof(State));
    
    public LoadingState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
    
    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }
    
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        TransitioningContentControl.PageTransitionProperty.AddOwner<LoadingContentControl>();


    public static readonly StyledProperty<object?> UnloadedContentProperty = AvaloniaProperty.Register<LoadingContentControl, object?>(
     "UnloadedContent");

    public object? UnloadedContent
    {
        get => GetValue(UnloadedContentProperty);
        set => SetValue(UnloadedContentProperty, value);
    }

    public static readonly StyledProperty<object?> LoadingContentProperty = AvaloniaProperty.Register<LoadingContentControl, object?>(
     "LoadingContent");

    public object? LoadingContent
    {
        get => GetValue(LoadingContentProperty);
        set => SetValue(LoadingContentProperty, value);
    }

    public static readonly StyledProperty<object?> LoadedContentProperty = AvaloniaProperty.Register<LoadingContentControl, object?>(
     "LoadedContent");

    public object? LoadedContent
    {
        get => GetValue(LoadedContentProperty);
        set => SetValue(LoadedContentProperty, value);
    }

    public static readonly StyledProperty<object?> FailedContentProperty = AvaloniaProperty.Register<LoadingContentControl, object?>(
     "FailedContent");

    public object? FailedContent
    {
        get => GetValue(FailedContentProperty);
        set => SetValue(FailedContentProperty, value);
    }
    
    public enum LoadingState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed
    }
}