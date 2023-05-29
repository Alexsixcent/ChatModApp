using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Shared.Services.EmoteProviders;

public interface IEmoteProvider
{
    IObservable<IGlobalEmote> LoadGlobalEmotes()
        => Observable.Empty<IGlobalEmote>();

    IObservable<IMemberEmote> LoadChannelEmotes(ITwitchUser user)
        => Observable.Empty<IMemberEmote>();
    
    IObservable<IEmote> LoadConnectedEmotes(ITwitchUser connected) 
        => Observable.Empty<IEmote>();
}