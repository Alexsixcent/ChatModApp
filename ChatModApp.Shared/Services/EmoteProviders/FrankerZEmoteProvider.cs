using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Services.ApiClients;
using ReactiveUI;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class FrankerZEmoteProvider : IEmoteProvider
{
    private readonly IFfzApi _api;

    public FrankerZEmoteProvider(IFfzApi api)
    {
        _api = api;
    }

    public IObservable<IGlobalEmote> LoadGlobalEmotes() =>
        _api.GetGlobalEmotes()
            .Select(res => res.Content?.Sets)
            .WhereNotNull()
            .SelectMany(sets => sets.Values)
            .SelectMany(set => set.Emoticons);

    public IObservable<IMemberEmote> LoadChannelEmotes(ITwitchChannel channel) =>
        _api.GetChannelEmotes(channel.Id)
            .Select(res => res.Content?.Sets)
            .WhereNotNull()
            .SelectMany(sets => sets.Values)
            .SelectMany(set => set.Emoticons)
            .Select(emote =>
            {
                emote.MemberChannel = channel;
                return emote;
            });
}