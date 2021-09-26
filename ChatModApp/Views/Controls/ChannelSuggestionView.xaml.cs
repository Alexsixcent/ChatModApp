using System.Reactive.Disposables;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ChatModApp.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views
{
    public class ChannelSuggestionViewBase : ReactiveUserControl<ChannelSuggestionViewModel>
    {
    }

    public sealed partial class ChannelSuggestionView
    {
        public ChannelSuggestionView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.ThumbnailUrl, v => v.Thumbnail.ProfilePicture, uri => new BitmapImage(uri))
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.DisplayName, v => v.Channel.Text)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel, vm => vm.IsLive, v => v.StreamStatus.Opacity, b => b ? 1.0 : 0.0)
                    .DisposeWith(disposables);
            });
        }
    }
}