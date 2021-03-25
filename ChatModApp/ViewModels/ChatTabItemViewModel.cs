using System;
using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ChatModApp.ViewModels
{
    public class ChatTabItemViewModel : ReactiveObject, IScreen, IDisposable
    {
        [Reactive]
        public string Title { get; set; }
        public RoutingState Router { get; }

        private readonly CompositeDisposable _disposables;

        public ChatTabItemViewModel(ChatTabPromptViewModel promptViewModel)
        {
            _disposables = new CompositeDisposable();
            Title = "ChatTab";
            Router = new RoutingState();

            promptViewModel.HostScreen = this;
            Router.NavigateAndReset
                .Execute(promptViewModel)
                .Subscribe()
                .DisposeWith(_disposables);

            promptViewModel.DisposeWith(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}