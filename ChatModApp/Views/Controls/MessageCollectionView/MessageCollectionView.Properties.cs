using Windows.UI.Xaml;

namespace ChatModApp.Views.Controls.MessageCollectionView;

public sealed partial class MessageCollectionView
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource",
        typeof(object),
        typeof(MessageCollectionView),
        new(default));

    public static readonly DependencyProperty IsStickyProperty = DependencyProperty.Register(
        "IsSticky",
        typeof(bool),
        typeof(MessageCollectionView),
        new(true));

    public static readonly DependencyProperty ScrollingStateProperty = DependencyProperty.Register(
        "ScrollingState",
        typeof(ScrollingStateFlags),
        typeof(MessageCollectionView),
        new(ScrollingStateFlags.Auto));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool IsSticky
    {
        get => (bool)GetValue(IsStickyProperty);
        private set => SetValue(IsStickyProperty, value);
    }

    public ScrollingStateFlags ScrollingState
    {
        get => (ScrollingStateFlags)GetValue(ScrollingStateProperty);
        private set => SetValue(ScrollingStateProperty, value);
    }
}