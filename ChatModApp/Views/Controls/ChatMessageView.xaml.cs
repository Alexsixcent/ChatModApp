using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ChatModApp.Models.Chat.Fragments;
using ChatModApp.Tools.Extensions;
using ChatModApp.ViewModels;
using ReactiveUI;
using Color = System.Drawing.Color;

namespace ChatModApp.Views
{
    public class ChatMessageViewBase : ReactiveUserControl<ChatMessageViewModel>
    {
    }

    public sealed partial class ChatMessageView
    {
        private const int InlineImagesVerticalOffset = 2;

        private Span _badges;
        private Run _username;
        private Span _message;

        public ChatMessageView()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                _badges = new Span();
                _username = new Run
                {
                    Text = ViewModel?.Username,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(ViewModel?.UsernameColor.ToUiColor() ?? Color.Gray.ToUiColor())
                };
                _message = new Span();

                foreach (var badge in ViewModel.Badges)
                {
                    _badges.Inlines.Add(new InlineUIContainer
                    {
                        Child = new Image
                        {
                            Source = new BitmapImage(badge.Small),
                            Stretch = Stretch.None,
                            Margin = new Thickness(2, 0, 2, 0),
                            RenderTransform = new TranslateTransform {Y = InlineImagesVerticalOffset}
                        }
                    });
                }

                foreach (var fragment in ViewModel.Message)
                {
                    _message.Inlines.Add(CreateFragmentInline(fragment));
                }

                MessageTextBlock.Blocks.Add(new Paragraph
                {
                    Inlines =
                    {
                        _badges, _username, new Run {Text = ": "}, _message
                    }
                });
            });
        }


        private static Inline CreateFragmentInline(IMessageFragment fragment)
        {
            return fragment switch
            {
                TextFragment textFrag => new Run { Text = textFrag.Text },
                EmoteFragment emoteFrag => new InlineUIContainer
                {
                    Child = new Image
                    {
                        Source = new BitmapImage(emoteFrag.Emote.Uri),
                        Stretch = Stretch.None,
                        Margin = new Thickness(2, 0, 2, 0),
                        RenderTransform = new TranslateTransform {Y = InlineImagesVerticalOffset}
                    },
                },
                UriFragment uriFrag => new Hyperlink
                {
                    Inlines = {new Run {Text = uriFrag.Text}},
                    NavigateUri = uriFrag.Uri,
                    UnderlineStyle = UnderlineStyle.None,
                },
                _ => throw new ArgumentException("Fragment not of valid type.")
            };
        }
    }
}