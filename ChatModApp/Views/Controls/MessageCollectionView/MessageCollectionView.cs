using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;

namespace ChatModApp.Views.Controls.MessageCollectionView
{
#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    [TemplatePart(Name = PartItemsRepeater, Type = typeof(ItemsRepeater))]
    [TemplatePart(Name = PartScrollView, Type = typeof(ScrollView))]
    [TemplatePart(Name = PartScrollButton, Type = typeof(Button))]
    public sealed partial class MessageCollectionView : Control
    {
        private const string PartItemsRepeater = "ItemsRepeater";
        private const string PartScrollView = "ScrollView";
        private const string PartScrollButton = "ScrollButton";

        private ScrollView? _scrollViewer;
        private ItemsRepeater? _itemsRepeater;
        private Button? _scrollButton;

        private double _lastScrollVerticalOffset;

        public enum ScrollingStateFlags
        {
            Auto,
            Manual,
            Hover //TODO: Implement scrolling pause
        }

        public MessageCollectionView()
        {
            DefaultStyleKey = typeof(MessageCollectionView);

            RegisterPropertyChangedCallback(ScrollingStateProperty, OnScrollingStateChanged);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_itemsRepeater is not null)
            {
                //TODO
            }

            if (_scrollViewer is not null)
            {
                _scrollViewer.ViewChanged -= ScrollViewerOnViewChanged;
            }

            if (_scrollButton is not null)
            {
                _scrollButton.Click -= ScrollButtonOnClick;
            }

            _itemsRepeater = (ItemsRepeater)GetTemplateChild(PartItemsRepeater);
            _scrollViewer = (ScrollView)GetTemplateChild(PartScrollView);
            _scrollButton = (Button)GetTemplateChild(PartScrollButton);

            if (_itemsRepeater is not null)
            {
                //TODO
            }

            if (_scrollViewer is not null)
            {
                _scrollViewer.ViewChanged += ScrollViewerOnViewChanged;
            }

            if (_scrollButton is not null)
            {
                _scrollButton.Click += ScrollButtonOnClick;
            }
        }

        private void ScrollViewerOnViewChanged(ScrollView sender, object args)
        {
            var scrollDiff = _lastScrollVerticalOffset - _scrollViewer!.VerticalOffset;
            if (scrollDiff > 0 && !IsNearBottom())
            {
                //Scrolled up away from bottom, so disable auto scrolling
                ScrollingState = ScrollingStateFlags.Manual;
            }
            else if (ScrollingState is not ScrollingStateFlags.Auto && scrollDiff < 0 && IsNearBottom())
            {
                //Scrolled down into sticky zone, so enable auto scrolling
                ScrollingState = ScrollingStateFlags.Auto;
            }

            _lastScrollVerticalOffset = _scrollViewer.VerticalOffset;
        }

        private void OnScrollingStateChanged(DependencyObject sender, DependencyProperty dp)
        {
            _scrollButton!.Opacity = ScrollingState is ScrollingStateFlags.Auto ? 0d : 1d;
        }

        private void ScrollButtonOnClick(object sender, RoutedEventArgs e) => ScrollToEnd();

        private bool IsNearBottom(double verticalOffset) =>
            _scrollViewer!.ScrollableHeight - verticalOffset < _scrollViewer.ViewportHeight*0.02;

        private bool IsNearBottom() => IsNearBottom(_scrollViewer!.VerticalOffset);

        private void ScrollToEnd() =>
            _scrollViewer!.ScrollTo(0d, _scrollViewer.ExtentHeight,
                                    new(ScrollingAnimationMode.Disabled));
    }
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
}