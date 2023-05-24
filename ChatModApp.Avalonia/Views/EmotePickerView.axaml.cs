using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class EmotePickerView : ReactiveUserControl<EmotePickerViewModel>
{
    private int _pickerTabIndex;

    public EmotePickerView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            TabStrip.WhenAnyValue(strip => strip.SelectedIndex)
                    .Select(i =>(index: i, Tab: IndexToTabItem(i)))
                    .Subscribe(tuple =>
                    {
                        var lastActiveTab = IndexToTabItem(_pickerTabIndex);
                        lastActiveTab.IsVisible = false;
                        tuple.Tab.IsVisible = true;
                        _pickerTabIndex = tuple.index;
                    })
                    .DisposeWith(d);
        });
    }

    private ContentControl IndexToTabItem(int index) =>
        index switch
        {
            0 => FavoriteEmotesList,
            1 => ChannelEmotesList,
            2 => GlobalEmotesList,
            3 => EmojiList,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index,
                                                       "Selected tab index was out of bounds, this should not happen !!")
        };
}