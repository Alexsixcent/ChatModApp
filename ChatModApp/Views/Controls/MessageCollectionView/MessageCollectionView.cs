using System;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;

namespace ChatModApp.Views.Controls.MessageCollectionView
{
    [TemplatePart(Name = PartItemsRepeater, Type = typeof(ItemsRepeater))]
    [TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartScrollButton, Type = typeof(Button))]
    public sealed partial class MessageCollectionView : Control
    {
        private const string PartItemsRepeater = "ItemsRepeater";
        private const string PartScrollViewer = "ScrollViewer";
        private const string PartScrollButton = "ScrollButton";

        private ScrollViewer? _scrollViewer;
        private ItemsRepeater? _itemsRepeater;
        private Button? _scrollButton;


        public enum ScrollingStateFlags
        {
            None,
            Auto,
            Manual
        }

        public enum PauseStateFlags
        {
            Scrolling,
            ScrollPaused,
            HoverPaused
        }

        public MessageCollectionView()
        {
            DefaultStyleKey = typeof(MessageCollectionView);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_itemsRepeater is not null)
            {
                _itemsRepeater.ItemsSourceView.CollectionChanged -= ItemsSourceViewOnCollectionChanged;
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
            _scrollViewer = (ScrollViewer)GetTemplateChild(PartScrollViewer);
            _scrollButton = (Button)GetTemplateChild(PartScrollButton);

            if (_itemsRepeater is not null)
            {
                _itemsRepeater.ItemsSourceView.CollectionChanged += ItemsSourceViewOnCollectionChanged;
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

        private void ItemsSourceViewOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsSticky && e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
            {
                ScrollToEnd();
            }
        }

        private void ScrollButtonOnClick(object sender, RoutedEventArgs e) => ScrollToEnd();

        private void ScrollViewerOnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (ScrollingState is ScrollingStateFlags.Auto)
            {
                ScrollingState = ScrollingStateFlags.None;
                return;
            }


            if (_scrollViewer!.ScrollableHeight < 0 ||
                Math.Abs(_scrollViewer.VerticalOffset - _scrollViewer.ScrollableHeight) < 0.1)
            {
                if (e.IsIntermediate) return;

                // Scrolled to bottom
                _scrollButton!.Opacity = 0d;
                ScrollingState = ScrollingStateFlags.Auto;
                PausingState = PauseStateFlags.Scrolling;
            }
            else
            {
                _scrollButton!.Opacity = 1d;
                ScrollingState = ScrollingStateFlags.Manual;
                PausingState = PauseStateFlags.ScrollPaused;
            }
        }

        private void ScrollToEnd()
        {
            _scrollButton!.Opacity = 0d;
            ScrollingState = ScrollingStateFlags.Auto;
            PausingState = PauseStateFlags.Scrolling;
            _scrollViewer?.ChangeView(null, _scrollViewer.ScrollableHeight, null, true);
        }
    }
}