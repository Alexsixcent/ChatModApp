using System;
using System.Reactive.Disposables;
using ChatModApp.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.ViewModels;

public class ChatTabItemViewModel : ReactiveObject, IChatTabItem, IScreen, IDisposable
{
    [Reactive]
    public string Title { get; set; }
    [Reactive]
    public ITwitchChannel? Channel { get; set; }

    public Guid Id { get; }
    public RoutingState Router { get; }

    private readonly CompositeDisposable _disposables;

    public ChatTabItemViewModel(ChatTabPromptViewModel promptViewModel)
    {
        _disposables = new();
        Id = Guid.NewGuid();
        Title = "ChatTab";
        Router = new();

        promptViewModel.HostScreen = this;
        promptViewModel.ParentTabId = Id;

        Router.NavigateAndReset
              .Execute(promptViewModel)
              .Subscribe()
              .DisposeWith(_disposables);

        promptViewModel.DisposeWith(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}