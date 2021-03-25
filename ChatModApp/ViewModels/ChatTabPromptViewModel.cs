using System;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ChatModApp.ViewModels
{
    public class ChatTabPromptViewModel : ReactiveObject, IDisposable, IRoutableViewModel
    {
        public string? UrlPathSegment => Guid.NewGuid().ToString().Substring(0, 5);
        public IScreen HostScreen { get; set; }
        public ReactiveCommand<string, Unit> OpenCommand { get; }
        [Reactive]
        public string Channel { get; private set; }


        private readonly CompositeDisposable _disposables;

        public ChatTabPromptViewModel(ChatViewModel chatViewModel)
        {
            _disposables = new CompositeDisposable();
            Channel = "";

            OpenCommand = ReactiveCommand.Create<string>(s =>
            {
                chatViewModel.Channel = s;
                chatViewModel.HostScreen = HostScreen;
                HostScreen.Router.Navigate.Execute(chatViewModel).Subscribe();

                chatViewModel.Initialize();
            });

            OpenCommand.DisposeWith(_disposables);
            chatViewModel.DisposeWith(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}