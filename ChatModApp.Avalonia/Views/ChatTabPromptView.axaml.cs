using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Shared.ViewModels;
using ReactiveUI;
using Splat;

namespace ChatModApp.Views;

public partial class ChatTabPromptView : ReactiveUserControl<ChatTabPromptViewModel>, IEnableLogger
{
    public ChatTabPromptView()
    {
        this.WhenActivated(disposable =>
        {
            this.Bind(ViewModel, vm => vm.Channel, v => v.ChannelCompleteBox.SearchText)
                .DisposeWith(disposable);

            var committed = Observable.FromEventPattern(h => ChannelCompleteBox.Committed += h, h => ChannelCompleteBox.Committed -= h)
                                      .Select(_ => ChannelCompleteBox.Text)
                                      .Log(this, "Prompt submitted channel channel");
            
            Observable.FromEventPattern<RoutedEventArgs>(h => SubmitButton.Click += h, h => SubmitButton.Click -= h)
                      .Select(_ => ChannelCompleteBox.Text)
                      .Merge(committed)
                      .Where(t => !string.IsNullOrWhiteSpace(t) && ChannelCompleteBox.SelectedItem is not null)
                      .Select(t => ChannelCompleteBox.Items
                                                     .Cast<ChannelSuggestionViewModel>()
                                                     .FirstOrDefault(model =>
                                                                         model.DisplayName
                                                                              .Equals(t,
                                                                                  StringComparison
                                                                                      .InvariantCultureIgnoreCase)))
                      .WhereNotNull()
                      .SampleFirst(TimeSpan.FromSeconds(1))
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .InvokeCommand(ViewModel, vm => vm.SelectionCommand)
                      .DisposeWith(disposable);
        });

        InitializeComponent();
    }
}