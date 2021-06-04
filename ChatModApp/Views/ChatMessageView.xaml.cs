using System;
using Windows.UI.Text;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using ChatModApp.Models;
using ChatModApp.Tools.Extensions;
using ChatModApp.ViewModels;
using Microsoft.Toolkit.Uwp.UI.Controls;
using ReactiveUI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ChatModApp.Views
{
    public class ChatMessageViewBase : ReactiveUserControl<ChatMessageViewModel>
    {
    }

    public sealed partial class ChatMessageView
    {
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
                    Text = ViewModel.Username, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(ViewModel.UsernameColor.ToUiColor(byte.MaxValue))
                };
                _message = new Span();

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
                TextFragment text => new Run {Text = text.Text},
                EmoteFragment emote => new InlineUIContainer
                {
                    Child = new ImageEx
                    {
                        IsCacheEnabled = true, 
                        Source = emote.Emote.Uri, 
                        Stretch = Stretch.None
                    }
                },
                _ => throw new ArgumentException("Fragment not of valid type.")
            };
        }
    }
}