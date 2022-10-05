using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class BetterTTVEmoteProvider : IEmoteProvider
{
    private readonly ILogger<BetterTTVEmoteProvider> _logger;
    private readonly IBttvApi _api;

    public BetterTTVEmoteProvider(ILogger<BetterTTVEmoteProvider> logger, IBttvApi api)
    {
        _logger = logger;
        _api = api;
    }

    public IObservable<IGlobalEmote> LoadGlobalEmotes() =>
        _api.GetGlobalEmotes()
            .Do(list => _logger.LogDebug("BTTV: Fetched {Count} global emotes : {Emotes}",
                                         list.Count,
                                         list.Select(e => e.Code)))
            .SelectMany(list => list);

    public IObservable<IMemberEmote> LoadChannelEmotes(ITwitchChannel channel)
    {
        var content = _api.GetUserEmotes(channel.Id)
                          .Select(res => res.Content)
                          .WhereNotNull()
                          .Do(res => _logger.LogDebug("BTTV: Fetched {Count} emotes in {Channel}; Shared = {SharedEmotes}, Channel = {ChanEmotes}",
                                                      res.ChannelEmotes.Count+res.SharedEmotes.Count,
                                                      channel,
                                                      res.SharedEmotes.Select(e => e.Code),
                                                      res.ChannelEmotes.Select(e => e.Code)))
                          .Publish()
                          .RefCount();

        var channelEmotes = content.SelectMany(res => res.ChannelEmotes)
                                   .Select(emote =>
                                   {
                                       emote.Description = $"By: {channel.DisplayName}";
                                       return (IMemberEmote)emote;
                                   });
        var sharedEmotes = content.SelectMany(res => res.SharedEmotes);

        return channelEmotes.Merge(sharedEmotes)
                            .Select(emote =>
                            {
                                emote.MemberChannel = channel;
                                return emote;
                            });
    }
}