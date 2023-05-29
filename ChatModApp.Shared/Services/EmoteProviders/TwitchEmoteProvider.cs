using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class TwitchEmoteProvider : IEmoteProvider
{
    private readonly TwitchApiService _api;

    public TwitchEmoteProvider(TwitchApiService api)
    {
        _api = api;
    }

    public IObservable<IEmote> LoadConnectedEmotes(ITwitchUser connected) =>
        Observable.FromAsync(() => _api.Helix.Chat.GetGlobalEmotesAsync())
                  .SelectMany(res => res.GlobalEmotes)
                  .Select(emote => new TwitchEmote(emote));
}