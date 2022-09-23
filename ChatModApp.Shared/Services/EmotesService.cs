using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.EmoteProviders;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;

namespace ChatModApp.Shared.Services;

public sealed record EmotePair(ITwitchChannel MemberChannel, string Code)
{
    public ITwitchChannel MemberChannel { get; } = MemberChannel;

    public string Code { get; } = Code;
}

public sealed class EmotesService : IDisposable
{
    public IObservableList<IEmote> Emotes { get; }
    public IObservableCache<IGlobalEmote, string> GlobalEmotes { get; }
    public IObservableCache<IMemberEmote, EmotePair> UserEmotePairs { get; }

    private readonly TwitchApiService _apiService;
    private readonly TwitchChatService _chatService;
    private readonly AuthenticationService _authService;
    private readonly IEmoteProvider[] _emoteProviders;

    private readonly CompositeDisposable _disposable;
    private readonly SourceList<IEmoteProvider> _providers;

    public EmotesService(TwitchApiService apiService, AuthenticationService authService,
                         TwitchChatService chatService, IEmoteProvider[] emoteProviders)
    {
        _disposable = new();
        _providers = new SourceList<IEmoteProvider>().DisposeWith(_disposable);
        _apiService = apiService;
        _authService = authService;
        _chatService = chatService;
        _emoteProviders = emoteProviders;

        var providers = _providers
                        .Connect()
                        .ObserveOnThreadPool()
                        .RefCount();
        var globalEmotes = providers.TransformMany(provider => provider.LoadGlobalEmotes()
                                                                       .ToEnumerable())
                                    .RefCount();
        var connectedEmotes = _apiService.UserConnected
                                         .ObserveOnThreadPool()
                                         .Select(user => new TwitchChannel(user.Id, user.DisplayName, user.Login))
                                         .SelectMany(user => providers.TransformMany(provider => provider
                                                         .LoadConnectedEmotes(user)
                                                         .ToEnumerable()));

        var memberEmotes = _chatService.ChannelsJoined
                                       .ObserveOnThreadPool()
                                       .Transform(channel => providers
                                                             .TransformMany(provider => provider
                                                                                .LoadChannelEmotes(channel)
                                                                                .ToEnumerable())
                                                             .DisposeMany()
                                                             .AsObservableList())
                                       .DisposeMany()
                                       .TransformMany(list => list)
                                       .RefCount();

        Emotes = globalEmotes.Cast(e => (IEmote)e)
                             .Or(connectedEmotes, memberEmotes.Cast(e => (IEmote)e))
                             .AsObservableList()
                             .DisposeWith(_disposable);


        GlobalEmotes = globalEmotes
                       .AddKey(emote => emote.Code)
                       .AsObservableCache()
                       .DisposeWith(_disposable);

        UserEmotePairs = memberEmotes
                         .AddKey(emote => new EmotePair(emote.MemberChannel, emote.Code))
                         .AsObservableCache()
                         .DisposeWith(_disposable);
    }

    public void Initialize() => _providers.AddRange(_emoteProviders);

    public void Dispose() => _disposable.Dispose();
}