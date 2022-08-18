using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using ChatModApp.Shared.Tools;
using DynamicData;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using EmoteKeyValue = System.Collections.Generic.KeyValuePair<ChatModApp.Shared.Models.Chat.Emotes.EmoteKey,
    ChatModApp.Shared.Models.Chat.Emotes.IEmote>;

namespace ChatModApp.Shared.Services;

public class EmotesService : IDisposable
{
    public IObservableCache<IEmote, string> GlobalEmotes { get; }
    public IObservableCache<Grouping<IEmote, string, string>, string> UserEmotes { get; }

    public IObservableCache<IGroup<IEmote, string, EmoteKey.EmoteType>, EmoteKey.EmoteType> GlobalEmoteGroups { get; }

    public IObservableCache<Grouping<Grouping<IEmote, string, EmoteKey.EmoteType>, EmoteKey.EmoteType, string>,
        string> UserEmoteGroups { get; }

    private readonly TwitchApiService _apiService;
    private readonly TwitchChatService _chatService;
    private readonly AuthenticationService _authService;
    private readonly IBttvApi _bttvApi;
    private readonly IFfzApi _ffzApi;

    private readonly CompositeDisposable _disposable;
    private readonly SourceCache<EmoteKeyValue, EmoteKey> _emotes;

    public EmotesService(TwitchApiService apiService, AuthenticationService authService, IBttvApi bttvApi,
                         TwitchChatService chatService, IFfzApi ffzApi)
    {
        _disposable = new();
        _emotes = new(kv => kv.Key);
        _apiService = apiService;
        _authService = authService;
        _bttvApi = bttvApi;
        _chatService = chatService;
        _ffzApi = ffzApi;

        _apiService.UserConnected
                   .SelectMany(LoadGlobalEmotes)
                   .Subscribe()
                   .DisposeWith(_disposable);

        _chatService.ChannelsJoined
                    .OnItemAdded(async s => await LoadChannelEmotes(s))
                    .OnItemRemoved(UnloadChannelEmotes)
                    .Subscribe()
                    .DisposeWith(_disposable);

        GlobalEmotes = _emotes.Connect()
                              .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Global))
                              .ChangeKey(kv => kv.Key.Code)
                              .Transform(kv => kv.Value)
                              .AsObservableCache();

        UserEmotes = _emotes.Connect()
                            .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Member))
                            .Group(kv => kv.Key.Channel)
                            .Transform(group => new Grouping<IEmote, string, string>(
                                        group.Key,
                                        group.Cache.Connect()
                                             .ChangeKey(kv => kv.Value.Code)
                                             .Transform(pair => pair.Value)
                                             .AsObservableCache()))
                            .DisposeMany()
                            .AsObservableCache();


        GlobalEmoteGroups = _emotes.Connect()
                                   .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Global))
                                   .Group(kv => kv.Key.Type)
                                   .Transform(group =>
                                                  (IGroup<IEmote, string, EmoteKey.EmoteType>)new
                                                      Grouping<IEmote, string, EmoteKey.EmoteType>(
                                                       group.Key,
                                                       group.Cache.Connect()
                                                            .ChangeKey(kv => kv.Value.Code)
                                                            .Transform(pair => pair.Value)
                                                            .AsObservableCache()))
                                   .DisposeMany()
                                   .AsObservableCache();

        UserEmoteGroups = _emotes.Connect()
                                 .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Member))
                                 .Group(kv => kv.Key.Channel)
                                 .Transform(group =>
                                                new Grouping<Grouping<IEmote, string, EmoteKey.EmoteType>,
                                                    EmoteKey.EmoteType,
                                                    string>(group.Key, group.Cache.Connect()
                                                                            .Group(kv => kv.Key.Type)
                                                                            .Transform(innerGroup =>
                                                                                new Grouping<IEmote, string,
                                                                                    EmoteKey.EmoteType>(
                                                                                 innerGroup.Key,
                                                                                 innerGroup.Cache.Connect()
                                                                                     .ChangeKey(
                                                                                      kv => kv.Value
                                                                                          .Code)
                                                                                     .Transform(kv => kv
                                                                                         .Value)
                                                                                     .AsObservableCache()))
                                                                            .DisposeMany()
                                                                            .AsObservableCache()))
                                 .DisposeMany()
                                 .AsObservableCache();

        _emotes.DisposeWith(_disposable);
        GlobalEmotes.DisposeWith(_disposable);
        UserEmotes.DisposeWith(_disposable);

        GlobalEmoteGroups.DisposeWith(_disposable);
        UserEmoteGroups.DisposeWith(_disposable);
    }

    public void Dispose() => _disposable.Dispose();


    private async Task LoadChannelEmotes(ITwitchChannel channel)
    {
        var userRes = await _apiService.Helix.Users.GetUsersAsync(logins: new() { channel.Login }).ConfigureAwait(false);
        var id = int.Parse(userRes.Users.Single().Id);

        var (res1, res2) = await (_bttvApi.GetUserEmotes(id), _ffzApi.GetChannelEmotes(id)).ConfigureAwait(false);

        const EmoteKey.EmoteType bttvKey = EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Member,
                                 ffzKey = EmoteKey.EmoteType.FrankerZ | EmoteKey.EmoteType.Member;

        var bttv = res1.Content?.ChannelEmotes
                       .Select(emote =>
                       {
                           emote.Description = $"By: {channel.DisplayName}";
                           return emote;
                       })
                       .Concat(res1.Content?.SharedEmotes ?? Enumerable.Empty<IEmote>())
                       .Select(emote => new EmoteKeyValue(new(bttvKey, emote, channel.Login), emote));
        var ffz = res2.Content?.Sets
                      .SelectMany(pair => pair.Value.Emoticons)
                      .Select(emote => new EmoteKeyValue(new(ffzKey, emote, channel.Login), emote));
                      
        
        _emotes.Edit(updater =>
        {
            updater.AddOrUpdate(bttv ?? Enumerable.Empty<EmoteKeyValue>());
            updater.AddOrUpdate(ffz ?? Enumerable.Empty<EmoteKeyValue>());
        });
    }

    private void UnloadChannelEmotes(ITwitchChannel channel)
    {
        var group = UserEmoteGroups.Lookup(channel.Login);
        if (!group.HasValue)
            return;

        var emotes =
            group.Value.Cache.Items.SelectMany(grouping =>
                                                   grouping.Cache.Items.Select(emote => new EmoteKey(grouping.Key,
                                                                                   emote, channel.Login)));

        _emotes.Remove(emotes);
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    private async Task<Unit> LoadGlobalEmotes(User user, CancellationToken cancel)
    {
        var (res1, res2, res3) =
            await (_apiService.Helix.Chat.GetGlobalEmotesAsync(_authService.TwitchAccessToken),
                   _bttvApi.GetGlobalEmotes(), _ffzApi.GetGlobalEmotes())
                .ConfigureAwait(false);

        const EmoteKey.EmoteType twitchKey = EmoteKey.EmoteType.Twitch | EmoteKey.EmoteType.Global,
                                 bttvKey = EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Global,
                                 ffzKey = EmoteKey.EmoteType.FrankerZ | EmoteKey.EmoteType.Global;

        var twitch = res1.GlobalEmotes
                         .Select(emote => new TwitchEmote(emote))
                         .Select(emote => new EmoteKeyValue(new(twitchKey, emote), emote));

        var bttv = res2
            .Select(emote => new EmoteKeyValue(new(bttvKey, emote), emote));

        var ffz = res3.DefaultSets
                      .Select(key => res3.Sets[key])
                      .SelectMany(set => set.Emoticons)
                      .Select(emote => new EmoteKeyValue(new(ffzKey, emote), emote));


        _emotes.Edit(updater =>
        {
            updater.Clear();
            updater.AddOrUpdate(twitch.Concat(bttv).Concat(ffz));
        });

        return Unit.Default;
    }
}