using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Services;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TwitchLib.Api.Core.Enums;

namespace ChatModApp.Shared.ViewModels;

public sealed class UserListViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    public ViewModelActivator Activator { get; }

    public ReactiveCommand<Unit, Unit> ChattersLoadCommand { get; private set; }

    [ObservableAsProperty] public ITwitchUser? SrcChannel { get; }

    [Reactive] public string? UserSearchText { get; set; }

    public ReadOnlyObservableCollection<IGrouping<UserType, string>> UsersList => _usersList;


    private readonly TwitchChatService _chatService;
    private readonly SourceList<TwitchChatter> _chatters;
    private ReadOnlyObservableCollection<IGrouping<UserType, string>> _usersList;


    public UserListViewModel(TwitchChatService chatService)
    {
        _chatService = chatService;
        Activator = new();
        _chatters = new();

        this.WhenActivated(d =>
        {
            var searchChanged = this.WhenValueChanged(vm => vm.UserSearchText)
                                    .DistinctUntilChanged(StringComparer.InvariantCultureIgnoreCase)
                                    .Select(_ => (Func<TwitchChatter, bool>)ChatterFilter)
                                    .ObserveOnThreadPool();

            _chatters.Connect()
                     .ObserveOnThreadPool()
                     .Filter(searchChanged)
                     .GroupByElement(c => c.Type, c => c.DisplayName)
                     .ObserveOnMainThread()
                     .Bind(out _usersList)
                     .Subscribe()
                     .DisposeWith(d);

            ChattersLoadCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var list =
                    await _chatService.GetChatUserList(SrcChannel ??
                                                       throw new NullReferenceException("Channel can't be null"))
                                      .ConfigureAwait(false);

                _chatters.Edit(e =>
                {
                    e.Clear();
                    e.AddRange(list);
                });
            }).DisposeWith(d);
        });
    }

    private bool ChatterFilter(TwitchChatter f) =>
        string.IsNullOrWhiteSpace(UserSearchText) || f.DisplayName.Contains(UserSearchText);


    public void Dispose()
    {
        Activator.Dispose();
    }
}