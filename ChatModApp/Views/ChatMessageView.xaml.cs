using System;
using System.Reactive.Disposables;
using Windows.UI;
using Windows.UI.Xaml.Media;
using ChatModApp.ViewModels;
using ReactiveUI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ChatModApp.Views
{
    public class ChatMessageViewBase : ReactiveUserControl<ChatMessageViewModel>
    {
    }

    public sealed partial class ChatMessageView
    {
        public ChatMessageView()
        {
            InitializeComponent();

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, vm => vm.Username, v => v.Username.Text)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel, vm => vm.Message, v => v.Message.Text)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel, vm => vm.UsernameColor, v => v.Username.Foreground, color => new SolidColorBrush(Color.FromArgb(byte.MaxValue, color.R, color.G, color.B)))
                    .DisposeWith(disposable);
            });
        }
    }
}