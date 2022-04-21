using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Models;
using ChatModApp.Models.Chat.Emotes;
using ChatModApp.Services.ApiClients;
using ChatModApp.Tools;
using DynamicData;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using EmoteKeyValue = System.Collections.Generic.KeyValuePair<ChatModApp.Models.Chat.Emotes.EmoteKey,
    ChatModApp.Models.Chat.Emotes.IEmote>;

namespace ChatModApp.Services;

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
        var res1 = await _apiService.Helix.Users.GetUsersAsync(logins: new() { channel.Login }).ConfigureAwait(false);
        var id = int.Parse(res1.Users.Single().Id);

        var (bttv, ffz) = await (_bttvApi.GetUserEmotes(id), _ffzApi.GetChannelEmotes(id)).ConfigureAwait(false);

        _emotes.Edit(updater =>
        {
            if (bttv.IsSuccessStatusCode)
            {
                updater.AddOrUpdate(bttv.Content!.ChannelEmotes.Select(
                                                                       emote => new EmoteKeyValue(
                                                                        new(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Member,
                                                                            emote,
                                                                            channel.Login), emote)));
                updater.AddOrUpdate(bttv.Content.SharedEmotes.Select(
                                                                     emote => new EmoteKeyValue(
                                                                      new(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Member,
                                                                          emote,
                                                                          channel.Login), emote)));
            }

            if (ffz.IsSuccessStatusCode)
            {
                updater.AddOrUpdate(ffz.Content!.Sets.SelectMany(pair => pair.Value.Emoticons)
                                       .Select(emote => new EmoteKeyValue(
                                                                          new(EmoteKey.EmoteType.FrankerZ |
                                                                              EmoteKey.EmoteType.Member,
                                                                              emote, channel.Login), emote)));
            }
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
        var (twitchGlobal, bttv, ffz) =
            await (_apiService.Helix.Chat.GetGlobalEmotesAsync(_authService.TwitchAccessToken),
                   _bttvApi.GetGlobalEmotes(), _ffzApi.GetGlobalEmotes())
                .ConfigureAwait(false);

        var globalFfzEmotes = ffz.DefaultSets.Select(key => ffz.Sets[key]).SelectMany(set => set.Emoticons);

        _emotes.Edit(updater =>
        {
            updater.Clear();
            updater.AddOrUpdate(twitchGlobal.GlobalEmotes
                                            .Select(emote => (TwitchEmote)emote)
                                            .Select(emote => new EmoteKeyValue(
                                                                               new(EmoteKey.EmoteType.Twitch | EmoteKey.EmoteType.Global,
                                                                                   emote),
                                                                               emote)));
            updater.AddOrUpdate(bttv
                                    .Select(emote => new EmoteKeyValue(
                                                                       new(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Global,
                                                                           emote),
                                                                       emote)));
            updater.AddOrUpdate(globalFfzEmotes
                                    .Select(emote => new EmoteKeyValue(
                                                                       new(EmoteKey.EmoteType.FrankerZ | EmoteKey.EmoteType.Global,
                                                                           emote),
                                                                       emote)));
        });

        return Unit.Default;
    }
}