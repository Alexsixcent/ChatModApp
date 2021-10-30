using Windows.UI.Xaml;

namespace ChatModApp.Views.Controls.MessageCollectionView;

public sealed partial class MessageCollectionView
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource", typeof(object), typeof(MessageCollectionView), new PropertyMetadata(default));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty IsStickyProperty = DependencyProperty.Register(
        "IsSticky", typeof(bool), typeof(MessageCollectionView), new PropertyMetadata(true));

    public bool IsSticky
    {
        get => (bool)GetValue(IsStickyProperty);
        private set => SetValue(IsStickyProperty, value);
    }

    public static readonly DependencyProperty ScrollingStateProperty = DependencyProperty.Register(
        "ScrollingState", typeof(ScrollingStateFlags), typeof(MessageCollectionView), new PropertyMetadata(ScrollingStateFlags.Auto));

    public ScrollingStateFlags ScrollingState
    {
        get => (ScrollingStateFlags)GetValue(ScrollingStateProperty);
        private set => SetValue(ScrollingStateProperty, value);
    }

    public static readonly DependencyProperty PausingStateProperty = DependencyProperty.Register(
        "PausingState", typeof(PauseStateFlags), typeof(MessageCollectionView), new PropertyMetadata(PauseStateFlags.Scrolling));

    public PauseStateFlags PausingState
    {
        get => (PauseStateFlags)GetValue(PausingStateProperty);
        set => SetValue(PausingStateProperty, value);
    }
}