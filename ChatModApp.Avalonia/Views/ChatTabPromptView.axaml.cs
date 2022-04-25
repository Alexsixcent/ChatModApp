using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;

namespace ChatModApp.Views;

public partial class ChatTabPromptView : ReactiveUserControl<ChatTabPromptViewModel>
{
    public ChatTabPromptView()
    {
        this.WhenActivated(disposable =>
        {
            this.Bind(ViewModel, vm => vm.Channel, v => v.ChannelCompleteBox.SearchText)
                .DisposeWith(disposable);

            Observable.FromEventPattern(ChannelCompleteBox, nameof(ChannelCompleteBox.DropDownClosed))
                      .Select(_ => ChannelCompleteBox.Text)
                      .Where(t => !string.IsNullOrWhiteSpace(t) && ChannelCompleteBox.SelectedItem is not null)
                      .Select(t => ChannelCompleteBox.Items
                                                     .Cast<ChannelSuggestionViewModel>()
                                                     .FirstOrDefault(model =>
                                                                         model.DisplayName
                                                                              .Equals(t,
                                                                                  StringComparison
                                                                                      .InvariantCultureIgnoreCase)))
                      .WhereNotNull()
                      .Throttle(TimeSpan.FromSeconds(1))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .InvokeCommand(ViewModel, vm => vm.SelectionCommand)
                      .DisposeWith(disposable);
        });

        InitializeComponent();
    }
}