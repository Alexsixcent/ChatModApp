using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using ChatModApp.Models.Chat.Emotes;
using ChatModApp.Models.Chat.Fragments;
using ChatModApp.ViewModels;
using Tools.Extensions;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class MessageProccessingService
    {
        private readonly GlobalStateService _globalStateService;
        private readonly EmotesService _emotesService;

        public MessageProccessingService(GlobalStateService globalStateService, EmotesService emotesService)
        {
            _globalStateService = globalStateService;
            _emotesService = emotesService;
        }

        public ChatMessageViewModel ProcessMessageViewModel(ChatMessage message)
        {
            return new(
                message.Id,
                message.DisplayName,
                GetMessageFragments(message),
                string.IsNullOrEmpty(message.ColorHex)
                    ? Color.Gray
                    : Color.FromArgb(
                        int.Parse(message.ColorHex.TrimStart(1),
                                  NumberStyles.HexNumber)));
        }


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

                    fragments.Add(new EmoteFragment(new TwitchEmote(emote.Id, emote.Name)));
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

        private IEnumerable<IMessageFragment> ParseTextFragment(string msg, string channel, bool startSpace = true,
                                                                bool endSpace = true)
        {
            var fragments = new List<IMessageFragment>();

            foreach (var frag in msg.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var res1 = _emotesService.GlobalEmotes.Lookup(frag);
                if (res1.HasValue)
                {
                    fragments.Add(new EmoteFragment(res1.Value));
                    continue;
                }

                var res2 = _emotesService.UserEmotes.Lookup(channel);
                if (res2.HasValue)
                {
                    var res3 = res2.Value.Cache.Lookup(frag);
                    if (res3.HasValue)
                    {
                        fragments.Add(new EmoteFragment(res3.Value));
                        continue;
                    }
                }

                if (HasValidHost(frag) &&
                    Uri.TryCreate(frag, UriKind.RelativeOrAbsolute, out var uriRes))
                {
                    if (uriRes.IsAbsoluteUri)
                    {
                        if (uriRes.Scheme == Uri.UriSchemeHttp || uriRes.Scheme == Uri.UriSchemeHttps)
                        {
                            fragments.Add(new UriFragment(uriRes));
                            continue;
                        }
                    }
                    else
                    {
                        fragments.Add(new UriFragment(new UriBuilder(frag)
                        {
                            Scheme = Uri.UriSchemeHttps,
                            Port = -1
                        }.Uri, frag));
                        continue;
                    }
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

            if (!startSpace && fragments.First() is TextFragment firstFrag)
                firstFrag.Text = firstFrag.Text.TrimStart(' ');

            if (!endSpace && fragments.Last() is TextFragment lastFrag)
                lastFrag.Text = lastFrag.Text.TrimEnd(' ');

            return fragments;
        }

        private bool HasValidHost(string uri)
        {
            var host = uri
                       .TrimStart("http://")
                       .TrimStart("https://")
                       .Split('/', StringSplitOptions.RemoveEmptyEntries)
                       .FirstOrDefault();

            if (string.IsNullOrEmpty(host))
                return false;

            var i = host.LastIndexOf('.');

            return i > 0
                   && !uri.StartsWith('.')
                   && !uri.EndsWith('.')
                   && _globalStateService.TLDs.Contains(host.Substring(i + 1).ToLowerInvariant());
        }
    }
}