using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using ChatModApp.Shared.Tools.Extensions;
using DynamicData;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChatModApp.Shared.Services;

public record EmotePair(string MemberChannel, string Code)
{
    public string MemberChannel { get; } = MemberChannel;

    public string Code { get; } = Code;
}

public class EmotesService : IDisposable
{
    public IObservableList<IEmote> Emotes { get; }
    public IObservableCache<IGlobalEmote, string> GlobalEmotes { get; }
    public IObservableCache<IMemberEmote, EmotePair> UserEmotePairs { get; }

    private readonly TwitchApiService _apiService;
    private readonly TwitchChatService _chatService;
    private readonly AuthenticationService _authService;
    private readonly IBttvApi _bttvApi;
    private readonly IFfzApi _ffzApi;

    private readonly CompositeDisposable _disposable;
    private readonly SourceList<IEmote> _emotes;

    public EmotesService(TwitchApiService apiService, AuthenticationService authService, IBttvApi bttvApi,
                         TwitchChatService chatService, IFfzApi ffzApi)
    {
        _disposable = new();
        _emotes = new SourceList<IEmote>().DisposeWith(_disposable);
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

        Emotes = _emotes.AsObservableList().DisposeWith(_disposable);
        
        GlobalEmotes = _emotes.Connect()
                              .WhereIsType<IEmote, IGlobalEmote>()
                              .AddKey(emote => emote.Code)
                              .AsObservableCache()
                              .DisposeWith(_disposable);

        UserEmotePairs = _emotes.Connect()
                            .WhereIsType<IEmote, IMemberEmote>()
                            .AddKey(emote => new EmotePair(emote.MemberChannel, emote.Code))
                            .AsObservableCache()
                            .DisposeWith(_disposable);
    }

    public void Dispose() => _disposable.Dispose();


    private async Task LoadChannelEmotes(ITwitchChannel channel)
    {
        var userRes = await _apiService.Helix.Users.GetUsersAsync(logins: new() { channel.Login }).ConfigureAwait(false);
        var id = int.Parse(userRes.Users.Single().Id);

        var (res1, res2) = await (_bttvApi.GetUserEmotes(id), _ffzApi.GetChannelEmotes(id)).ConfigureAwait(false);

        var empty = Enumerable.Empty<IMemberEmote>();
        var bttv = res1.Content?.ChannelEmotes
                       .Select(emote =>
                       {
                           emote.Description = $"By: {channel.DisplayName}";
                           return emote;
                       })
                       .Concat(res1.Content?.SharedEmotes ?? empty) ?? empty;
        var ffz = res2.Content?.Sets
                      .SelectMany(pair => pair.Value.Emoticons) ?? empty;
                      
        
        _emotes.AddRange(bttv.Concat(ffz).Select(emote =>
        {
            emote.MemberChannel = channel.Login;
            return emote;
        }));
    }

    private void UnloadChannelEmotes(ITwitchChannel channel)
    {
        var emotes = UserEmotePairs.Items.Where(emote => emote.MemberChannel.Equals(channel.Login,
                                                                   StringComparison.InvariantCultureIgnoreCase));

        _emotes.RemoveMany(emotes);
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    private async Task<Unit> LoadGlobalEmotes(User user, CancellationToken cancel)
    {
        var (res1, res2, res3) =
            await (_apiService.Helix.Chat.GetGlobalEmotesAsync(_authService.TwitchAccessToken),
                   _bttvApi.GetGlobalEmotes(), _ffzApi.GetGlobalEmotes())
                .ConfigureAwait(false);
        
        var twitch = res1.GlobalEmotes
                         .Select(emote => new TwitchEmote(emote));

        var bttv = res2;

        var ffz = res3.Content
                      ?.DefaultSets
                      .Select(key => res3.Content?.Sets[key])
                      .SelectMany(set => set?.Emoticons);
        
        _emotes.Edit(updater =>
        {
            updater.Clear();
            updater.AddRange(twitch.Concat<IGlobalEmote>(bttv).Concat(ffz));
        });

        return Unit.Default;
    }
}