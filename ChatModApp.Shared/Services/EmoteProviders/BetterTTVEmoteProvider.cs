using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using ReactiveUI;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class BetterTTVEmoteProvider : IEmoteProvider
{
    private readonly IBttvApi _api;

    public BetterTTVEmoteProvider(IBttvApi api)
    {
        _api = api;
    }

    public IObservable<IGlobalEmote> LoadGlobalEmotes() =>
        _api.GetGlobalEmotes()
            .SelectMany(list => list);

    public IObservable<IMemberEmote> LoadChannelEmotes(ITwitchChannel channel)
    {
        var content = _api.GetUserEmotes(channel.Id)
                          .Select(res => res.Content)
                          .WhereNotNull()
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