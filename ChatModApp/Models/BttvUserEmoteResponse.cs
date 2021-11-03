using System.Collections.Generic;
using ChatModApp.Models.Chat.Emotes;
using Refit;

namespace ChatModApp.Models;

public class BttvUserEmoteResponse
{
    [AliasAs("id")]
    public string Id { get; set; }

    [AliasAs("bots")]
    public List<string> Bots { get; set; }

    [AliasAs("channelEmotes")]
    public List<BttvUserEmote> ChannelEmotes { get; set; }

    [AliasAs("sharedEmotes")]
    public List<BttvSharedEmote> SharedEmotes { get; set; }
}