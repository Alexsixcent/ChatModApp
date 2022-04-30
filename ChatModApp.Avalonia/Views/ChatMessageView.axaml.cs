using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.Models.Chat.Fragments;
using ChatModApp.Shared.ViewModels;
using ChatModApp.Tools;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views;

public partial class ChatMessageView : ReactiveUserControl<ChatMessageViewModel>, IEnableLogger
{
    public ChatMessageView()
    {
        this.WhenActivated(disposable =>
        {
            //TODO: Rework when RichTextBlock and Inlines support comes in
            Observable.FromAsync(token =>
                      {
                          var badges = ViewModel?.Badges.Select(badge => GetImageFromUri(badge.Small, token));
                          var username = Task.FromResult((Control)new TextBlock
                          {
                              Text = ViewModel?.Username + ": ",
                              TextWrapping = TextWrapping.Wrap,
                              TextAlignment = TextAlignment.Left,
                              VerticalAlignment = VerticalAlignment.Center,
                              Margin = new(1,0),
                              FontWeight = FontWeight.Bold,
                              FontSize = 12,
                              Foreground =
                                  new ImmutableSolidColorBrush(ViewModel?.UsernameColor.ToUiColor() ?? Colors.White)
                          });
                          var frags = ViewModel?.Message.Select(frag => GetMsgFragControl(frag, token));
                          return Task.WhenAll(badges.Append(username).Concat(frags));
                      })
                      .LoggedCatch(this, message: "Failed to load message")
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(controls =>
                      {
                          MessagePanel.Children.Clear();
                          MessagePanel.Children.AddRange(controls);
                      })
                      .DisposeWith(disposable);


            // Observable.FromAsync(token =>
            //           {
            //               var tasks = ViewModel?.Badges.Select(badge => CachedBitmapStore.Get(badge.Small, token));
            //               return Task.WhenAll(tasks!);
            //           })
            //           .LoggedCatch(this, message: "Badge image download failed")
            //           .ObserveOn(RxApp.MainThreadScheduler)
            //           .Subscribe(b => BadgeRepeater.Items = b)
            //           .DisposeWith(disposable);
            //
            // Observable.FromAsync(token =>
            //           {
            //               var tasks = ViewModel?.Message.Select<IMessageFragment, Task<object>>(async frag =>
            //                   frag is EmoteFragment e
            //                       ? await CachedBitmapStore.Get(e.Emote.Uri, token)
            //                       : frag);
            //
            //               return Task.WhenAll(tasks!);
            //           })
            //           .LoggedCatch(this, message: "Chat message emote download failed")
            //           .ObserveOn(RxApp.MainThreadScheduler)
            //           .Subscribe(frags => FragmentsRepeater.Items = frags)
            //           .DisposeWith(disposable);
        });
        InitializeComponent();
    }

    private static async Task<Control> GetImageFromUri(Uri uri, CancellationToken cancellationToken = default)
    {
        return new Image
        {
            Stretch = Stretch.None,
            Margin = new(1, 0),
            Source = await CachedBitmapStore.Get(uri, cancellationToken)
        };
    }
    private static async Task<Control> GetMsgFragControl(IMessageFragment frag, CancellationToken cancellationToken = default)
    {
        return frag switch
        {
            EmoteFragment emoteFragment => await GetImageFromUri(emoteFragment.Emote.Uri, cancellationToken),
            TextFragment textFragment => new TextBlock
            {
                Text = textFragment.Text,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            },
            UriFragment uriFragment => new HyperlinkButton
            {
                Content = uriFragment.Text,
                NavigateUri = uriFragment.Uri,
                Padding = new(5,5,5,6)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(frag))
        };
    }

    private void OnSelectTemplateKey(object? sender, SelectTemplateEventArgs e)
    {
        e.TemplateKey = e.DataContext switch
        {
            TextFragment => "TextFragment",
            IImage => "EmoteFragment", //TODO: Replace with EmoteFragment type when we can bind to Uri directly
            UriFragment => "UriFragment",
            _ => "TextFragment"
        };
    }
}