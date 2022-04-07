using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using ChatModApp.ViewModels;
using ReactiveUI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ChatModApp.Views.Pages;

public class ChatTabPromptViewBase : ReactivePage<ChatTabPromptViewModel> { }

public sealed partial class ChatTabPromptView
{
    public ChatTabPromptView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ChannelSuggestions, v => v.ChannelSuggestBox.ItemsSource)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.Channel, v => v.ChannelSuggestBox.Text)
                .DisposeWith(disposables);

            ChannelSuggestBox.Events()
                             .QuerySubmitted
                             .Select(tuple => tuple.args.ChosenSuggestion as ChannelSuggestionViewModel)
                             .WhereNotNull()
                             .InvokeCommand(ViewModel, vm => vm.SelectionCommand);
        });
    }
}