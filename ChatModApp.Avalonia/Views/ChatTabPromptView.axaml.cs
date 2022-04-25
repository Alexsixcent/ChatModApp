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
            Observable.FromEventPattern(ChannelCompleteBox, nameof(ChannelCompleteBox.DropDownClosed))
                      .Select(_ => ChannelCompleteBox.Text)
                      .Where(t => !string.IsNullOrWhiteSpace(t))
                      .Select(t => ChannelCompleteBox.Items
                                                     .Cast<ChannelSuggestionViewModel>()
                                                     .FirstOrDefault(model =>
                                                                         model.DisplayName
                                                                              .Equals(t, StringComparison.InvariantCultureIgnoreCase)))
                      .WhereNotNull()
                      .InvokeCommand(ViewModel, vm => vm.SelectionCommand)
                      .DisposeWith(disposable);
        });

        InitializeComponent();
    }
}