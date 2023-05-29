using System.Drawing;
using System.Globalization;
using System.Reactive.Linq;
using ChatModApp.Shared.Models;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Models.Chat.Fragments;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Shared.ViewModels;
using DynamicData;
using TwitchLib.Client.Models;

namespace ChatModApp.Shared.Services;

public class MessageProcessingService
{
    public IObservable<IChangeSet<IChatMessage, string>> ChannelMessages { get; }


    private static readonly char[] Slash = { '/' }, Space = { ' ' };

    private readonly GlobalStateService _globalStateService;
    private readonly EmotesService _emotesService;
    private readonly TwitchChatService _chatService;
    private readonly IObservableCache<ITwitchChannel,string> _roomIdToChannels;
    private readonly IObservableCache<ITwitchChannel,string> _roomLoginToChannels;
    private readonly SourceCache<IChatMessage, string> _chatMessageCache;

    public MessageProcessingService(GlobalStateService globalStateService,
                                    EmotesService emotesService,
                                    TwitchChatService chatService)
    {
        _globalStateService = globalStateService;
        _emotesService = emotesService;
        _chatService = chatService;
        _roomIdToChannels = chatService.ChannelsJoined.AddKey(channel => channel.Id).AsObservableCache();
        _roomLoginToChannels = chatService.ChannelsJoined.AddKey(channel => channel.Login).AsObservableCache();
        _chatMessageCache = new(msg => msg.Id);
        
        var sent = chatService.ChatMessageSent
                              .ObserveOnThreadPool()
                              .Select(ProcessSentMessage);

        var newSub = chatService.ChatNewSub
                             .ObserveOnThreadPool()
                             .Select(ProcessNewSubscriberMessage);

        var reSub = chatService.ChatResubbed
                               .ObserveOnThreadPool()
                               .Select(ProcessReSubscriberMessage);

        chatService.ChatMessageReceived
                   .ObserveOnThreadPool()
                   .Select(ProcessReceivedMessage)
                   .Cast<IChatMessage>()
                   .Merge(sent)
                   .Merge(newSub)
                   .Merge(reSub)
                   .Subscribe(msg => _chatMessageCache.AddOrUpdate(msg));

        ChannelMessages = _chatMessageCache.Connect();
    }

    private ChatMessageViewModel ProcessReceivedMessage(ChatMessage message)
    {
        var channel = _roomIdToChannels.Lookup(message.RoomId).Value;
        return new()
        {
            Id = message.Id,
            Channel = channel,
            Username = message.DisplayName,
            Badges = GetMessageBadges(channel, message.Badges),
            Message = GetMessageFragments(channel, message.Message, message.EmoteSet),
            UsernameColor = GetColorFromTwitchHex(message.ColorHex)
        };
    }

    private ChatMessageViewModel ProcessSentMessage(SentMessage message)
    {
        var channel = _roomLoginToChannels.Lookup(message.Channel).Value;
        return new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Channel = channel,
            Username = message.DisplayName,
            Badges = GetMessageBadges(channel, message.Badges),
            Message = ParseTextFragment(message.Message, channel, false, false),
            UsernameColor = GetColorFromTwitchHex(message.ColorHex)
        };
    }

    private ChatSubViewModel ProcessNewSubscriberMessage(Subscriber sub)
    {
        return new()
        {
            Id = sub.Id,
            Channel = _roomIdToChannels.Lookup(sub.RoomId).Value,
            Username = sub.DisplayName,
            Plan = sub.SubscriptionPlanName,
            Streak = sub.MsgParamStreakMonths,
            Months = 0,
            Parsed = sub.SystemMessageParsed.TrimStart(' ')
                        .TrimStart(sub.DisplayName)
                        .TrimStart(' '),
        };
    }

    private ChatSubViewModel ProcessReSubscriberMessage(ReSubscriber reSub)
    {
        var channel = _roomIdToChannels.Lookup(reSub.RoomId).Value;
        ChatMessageViewModel? message = null;
        
        if (!string.IsNullOrWhiteSpace(reSub.ResubMessage))
        {
            message = new()
            {
                Id = reSub.Id,
                Channel = channel,
                Username = reSub.DisplayName,
                Badges = GetMessageBadges(channel, reSub.Badges),
                Message = GetMessageFragments(channel, reSub.ResubMessage, new(reSub.EmoteSet, reSub.ResubMessage)),
                UsernameColor = GetColorFromTwitchHex(reSub.ColorHex)
            };
        }
        
        return new()
        {
            Id = reSub.Id,
            Channel = channel,
            Username = reSub.DisplayName,
            Plan = reSub.SubscriptionPlanName,
            Streak = reSub.MsgParamStreakMonths,
            Months = reSub.Months,
            Parsed = reSub.SystemMessageParsed.TrimStart(' ').TrimStart(reSub.DisplayName).TrimStart(' '),
            Message = message
        };
    }

    private IEnumerable<IChatBadge> GetMessageBadges(ITwitchChannel channel,
                                                     IEnumerable<KeyValuePair<string, string>> badgePairs) =>
        badgePairs.Select(pair => _chatService.ChatBadges.Items
                                              .Where(chatBadge =>
                                                         chatBadge.SetId == pair.Key && chatBadge.Id == pair.Value)
                                              .LastOrDefault(badge => badge.Channel is null ||
                                                                      badge.Channel == channel))
                  .Where(b => b is not null)!;

    private IEnumerable<IMessageFragment> GetMessageFragments(ITwitchChannel channel, string message, EmoteSet emoteSet)
    {
        var fragments = new List<IMessageFragment>();

        if (emoteSet.Emotes.Count == 0)
        {
            fragments.AddRange(ParseTextFragment(message, channel, false, false));
        }
        else
        {
            var emotes = emoteSet.Emotes.OrderBy(e => e.StartIndex);

            var lastEndIndex = 0;
            foreach (var emote in emotes)
            {
                if (emote.StartIndex - lastEndIndex > 1)
                {
                    fragments.AddRange(ParseTextFragment(message.SubstringAbs(lastEndIndex, emote.StartIndex - 1),
                                                         channel, lastEndIndex == 0));
                }

                fragments.Add(new EmoteFragment(new TwitchEmote(emote)));
                lastEndIndex = emote.EndIndex + 1;
            }

            if (lastEndIndex < message.Length - 1)
            {
                fragments.AddRange(ParseTextFragment(message[lastEndIndex..], channel,
                                                     endSpace: false));
            }
        }

        return fragments;
    }
    
    private IEnumerable<IMessageFragment> ParseTextFragment(string? msg, ITwitchChannel channel, bool startSpace = true,
                                                            bool endSpace = true)
    {
        if (msg is null)
            return Array.Empty<IMessageFragment>();

        var fragments = new List<IMessageFragment>();

        foreach (var frag in msg.Split(Space, StringSplitOptions.RemoveEmptyEntries))
        {
            var res1 = _emotesService.GlobalEmotes.Lookup(frag);
            if (res1.HasValue)
            {
                fragments.Add(new EmoteFragment(res1.Value));
                continue;
            }

            var res2 = _emotesService.UserEmotePairs.Lookup(new(channel, frag));
            if (res2.HasValue)
            {
                fragments.Add(new EmoteFragment(res2.Value));
                continue;
            }

            if (HasValidHost(frag) &&
                Uri.TryCreate(frag, UriKind.RelativeOrAbsolute, out var uriRes))
            {
                fragments.Add(new UriFragment(uriRes));
                continue;
            }

            if (fragments.LastOrDefault() is TextFragment text)
            {
                text.Text += frag + ' ';
            }
            else
            {
                fragments.Add(new TextFragment(' ' + frag + ' '));
            }
        }

        if (fragments.Count == 0)
        {
            fragments.Add(new TextFragment(msg));
        }
        else
        {
            if (!startSpace && fragments.First() is TextFragment firstFrag)
                firstFrag.Text = firstFrag.Text.TrimStart(' ');

            if (!endSpace && fragments.Last() is TextFragment lastFrag)
                lastFrag.Text = lastFrag.Text.TrimEnd(' ');
        }

        return fragments;
    }

    private bool HasValidHost(string uri)
    {
        var host = uri
                   .TrimStart("http://")
                   .TrimStart("https://")
                   .Split(Slash, StringSplitOptions.RemoveEmptyEntries)
                   .FirstOrDefault();

        if (string.IsNullOrEmpty(host))
            return false;

        if (uri.HasConsecutiveChar(".", 2))
            return false;

        var i = host.LastIndexOf('.');

        return i > 0
               && !uri.StartsWith(".")
               && !uri.EndsWith(".")
               && _globalStateService.TLDs.Contains(host.Substring(i + 1).ToLowerInvariant());
    }

    private static Color GetColorFromTwitchHex(string hex)
    {
        return string.IsNullOrWhiteSpace(hex)
                   ? Color.Gray
                   : Color.FromArgb(
                                    int.Parse(hex.TrimStart(1),
                                              NumberStyles.HexNumber));
    }
}