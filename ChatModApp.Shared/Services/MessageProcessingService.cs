using System.Drawing;
using System.Globalization;
using ChatModApp.Shared.Models.Chat;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Models.Chat.Fragments;
using ChatModApp.Shared.Tools.Extensions;
using ChatModApp.Shared.ViewModels;
using TwitchLib.Client.Models;

namespace ChatModApp.Shared.Services;

public class MessageProcessingService
{
    private static readonly char[] Slash = { '/' }, Space = { ' ' };

    private readonly GlobalStateService _globalStateService;
    private readonly EmotesService _emotesService;
    private readonly TwitchChatService _chatService;

    public MessageProcessingService(GlobalStateService globalStateService,
                                    EmotesService emotesService,
                                    TwitchChatService chatService)
    {
        _globalStateService = globalStateService;
        _emotesService = emotesService;
        _chatService = chatService;
    }

    public ChatMessageViewModel ProcessReceivedMessage(ChatMessage message)
    {
        return new(
                   message.Id,
                   message.DisplayName,
                   GetMessageBadges(message.Channel, message.Badges),
                   GetMessageFragments(message),
                   GetColorFromTwitchHex(message.ColorHex));
    }

    public ChatMessageViewModel ProcessSentMessage(SentMessage message)
    {
        return new(
                   Guid.NewGuid().ToString("N"),
                   message.DisplayName,
                   GetMessageBadges(message.Channel, message.Badges),
                   ParseTextFragment(message.Message, message.Channel, false, false),
                   GetColorFromTwitchHex(message.ColorHex));
    }

    private IEnumerable<IChatBadge> GetMessageBadges(string channel,
                                                     IEnumerable<KeyValuePair<string, string>> badgePairs) =>
        badgePairs.Select(pair => _chatService.ChatBadges.Items
                                              .Where(chatBadge => chatBadge.SetId == pair.Key && chatBadge.Id == pair.Value)
                                              .LastOrDefault(badge => badge.Channel is null || badge.Channel.Login == channel))
                  .Where(b => b is not null);

    private IEnumerable<IMessageFragment> GetMessageFragments(ChatMessage chatMessage)
    {
        var msg = chatMessage.Message;
        var fragments = new List<IMessageFragment>();


        if (chatMessage.EmoteSet.Emotes.Count == 0)
        {
            fragments.AddRange(ParseTextFragment(msg, chatMessage.Channel, false, false));
        }
        else
        {
            chatMessage.EmoteSet.Emotes.Sort((left, right) => left.StartIndex.CompareTo(right.StartIndex));

            var lastEndIndex = 0;
            foreach (var emote in chatMessage.EmoteSet.Emotes)
            {
                if (emote.StartIndex - lastEndIndex > 1)
                {
                    fragments.AddRange(
                                       ParseTextFragment(msg.SubstringAbs(lastEndIndex, emote.StartIndex - 1),
                                                         chatMessage.Channel, lastEndIndex == 0));
                }

                fragments.Add(new EmoteFragment(new TwitchEmote(emote)));
                lastEndIndex = emote.EndIndex + 1;
            }

            if (lastEndIndex < msg.Length - 1)
            {
                fragments.AddRange(ParseTextFragment(msg.Substring(lastEndIndex), chatMessage.Channel,
                                                     endSpace: false));
            }
        }

        return fragments;
    }

    private IEnumerable<IMessageFragment> ParseTextFragment(string? msg, string channel, bool startSpace = true,
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