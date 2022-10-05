using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class FrankerZEmoteProvider : IEmoteProvider
{
    private readonly ILogger<FrankerZEmoteProvider> _logger;
    private readonly IFfzApi _api;

    public FrankerZEmoteProvider(ILogger<FrankerZEmoteProvider> logger, IFfzApi api)
    {
        _logger = logger;
        _api = api;
    }

    public IObservable<IGlobalEmote> LoadGlobalEmotes() =>
        _api.GetGlobalEmotes()
            .Select(res => res.Content?.Sets)
            .WhereNotNull()
            .Do(sets => _logger.LogDebug("FFZ: Fetched {Count} global emotes : {Emotes}",
                                         sets.Values.SelectMany(set => set.Emoticons).Count(),
                                         sets.Values.SelectMany(set => set.Emoticons).Select(e => e.Code)))
            .SelectMany(sets => sets.Values)
            .SelectMany(set => set.Emoticons);

    public IObservable<IMemberEmote> LoadChannelEmotes(ITwitchChannel channel) =>
        _api.GetChannelEmotes(channel.Id)
            .Select(res => res.Content?.Sets)
            .WhereNotNull()
            .Do(sets => _logger.LogDebug("FFZ: Fetched {Count} emotes in {Channel} : {Emotes}",
                                        sets.Values.SelectMany(set => set.Emoticons).Count(),
                                         channel,
                                         sets.Values.SelectMany(set => set.Emoticons).Select(e => e.Code)))
            .SelectMany(sets => sets.Values)
            .SelectMany(set => set.Emoticons)
            .Select(emote =>
            {
                emote.MemberChannel = channel;
                return emote;
            });
}