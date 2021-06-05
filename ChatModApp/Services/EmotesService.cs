using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatModApp.Models;
using ChatModApp.Models.Emotes;
using ChatModApp.Services.ApiClients;
using ChatModApp.Tools;
using DynamicData;
using Tools.Extensions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client.Models;
using EmoteKeyValue =
    System.Collections.Generic.KeyValuePair<ChatModApp.Models.Emotes.EmoteKey, ChatModApp.Models.IEmote>;

namespace ChatModApp.Services
{
    public class EmotesService : IDisposable
    {
        public IObservableCache<IGroup<IEmote, string, EmoteKey.EmoteType>, EmoteKey.EmoteType> GlobalEmoteGroups
        {
            get;
        }

        public IObservableCache<Grouping<Grouping<IEmote, string, EmoteKey.EmoteType>, EmoteKey.EmoteType, string>,
            string> UserEmoteGroups { get; }

        private readonly TwitchApiService _apiService;
        private readonly TwitchChatService _chatService;
        private readonly AuthenticationService _authService;
        private readonly IBttvApi _bttvApi;
        private readonly IFfzApi _ffzApi;

        private readonly CompositeDisposable _disposable;
        private readonly SourceCache<EmoteKeyValue, EmoteKey> _emotes;
        private readonly IObservableCache<IEmote, string> _globalEmotes;
        private readonly IObservableCache<Grouping<IEmote, string, string>, string> _userEmotes;

        public EmotesService(TwitchApiService apiService, AuthenticationService authService, IBttvApi bttvApi,
                             TwitchChatService chatService, IFfzApi ffzApi)
        {
            _disposable = new CompositeDisposable();
            _emotes = new SourceCache<EmoteKeyValue, EmoteKey>(kv => kv.Key);
            _apiService = apiService;
            _authService = authService;
            _bttvApi = bttvApi;
            _chatService = chatService;
            _ffzApi = ffzApi;

            _apiService.UserConnected
                       .SelectMany(LoadGlobalEmotes)
                       .Subscribe()
                       .DisposeWith(_disposable);

            _chatService.ChannelsJoined.Connect()
                        .OnItemAdded(async s => await LoadChannelEmotes(s))
                        .OnItemRemoved(UnloadChannelEmotes)
                        .Subscribe()
                        .DisposeWith(_disposable);

            _globalEmotes = _emotes.Connect()
                                   .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Global))
                                   .ChangeKey(kv => kv.Key.Code)
                                   .Transform(kv => kv.Value)
                                   .AsObservableCache();

            _userEmotes = _emotes.Connect()
                                 .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Member))
                                 .Group(kv => kv.Key.Channel)
                                 .Transform(group => new Grouping<IEmote, string, string>(
                                                group.Key,
                                                group.Cache.Connect().ChangeKey(kv => kv.Value.Code)
                                                     .Transform(pair => pair.Value).AsObservableCache()))
                                 .DisposeMany()
                                 .AsObservableCache();


            GlobalEmoteGroups = _emotes.Connect()
                                       .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Global))
                                       .Group(kv => kv.Key.Type)
                                       .Transform(group =>
                                                      (IGroup<IEmote, string, EmoteKey.EmoteType>) new
                                                          Grouping<IEmote, string, EmoteKey.EmoteType>(
                                                              group.Key,
                                                              group.Cache.Connect()
                                                                   .ChangeKey(kv => kv.Value.Code)
                                                                   .Transform(pair => pair.Value)
                                                                   .AsObservableCache()))
                                       .DisposeMany()
                                       .AsObservableCache();

            UserEmoteGroups = _emotes.Connect()
                                     .Filter(kv => kv.Key.Type.HasFlag(EmoteKey.EmoteType.Member))
                                     .Group(kv => kv.Key.Channel)
                                     .Transform(group =>
                                                    new Grouping<Grouping<IEmote, string, EmoteKey.EmoteType>,
                                                        EmoteKey.EmoteType,
                                                        string>(group.Key, group.Cache.Connect()
                                                                    .Group(kv => kv.Key.Type)
                                                                    .Transform(innerGroup =>
                                                                                   new Grouping<IEmote, string,
                                                                                       EmoteKey.EmoteType>(
                                                                                       innerGroup.Key,
                                                                                       innerGroup.Cache.Connect()
                                                                                           .ChangeKey(
                                                                                               kv => kv.Value.Code)
                                                                                           .Transform(kv => kv.Value)
                                                                                           .AsObservableCache()))
                                                                    .DisposeMany()
                                                                    .AsObservableCache()))
                                     .DisposeMany()
                                     .AsObservableCache();

            _emotes.DisposeWith(_disposable);
            _globalEmotes.DisposeWith(_disposable);
            _userEmotes.DisposeWith(_disposable);

            GlobalEmoteGroups.DisposeWith(_disposable);
            UserEmoteGroups.DisposeWith(_disposable);
        }

        public void Dispose() => _disposable.Dispose();

        public IEnumerable<IMessageFragment> GetMessageFragments(ChatMessage chatMessage)
        {
            var msg = chatMessage.Message;
            var fragments = new List<IMessageFragment>();


            if (chatMessage.EmoteSet.Emotes.Count == 0)
            {
                fragments.AddRange(ParseTextFragment(msg, chatMessage.Channel));
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
                                              chatMessage.Channel));
                    }

                    fragments.Add(new EmoteFragment(new TwitchEmote(int.Parse(emote.Id), emote.Name)));
                    lastEndIndex = emote.EndIndex + 1;
                }

                if (lastEndIndex < msg.Length - 1)
                {
                    fragments.AddRange(ParseTextFragment(msg.Substring(lastEndIndex), chatMessage.Channel));
                }
            }

            return fragments;
        }

        private async Task LoadChannelEmotes(string channel)
        {
            var res = await _apiService.Helix.Users.GetUsersAsync(logins: new List<string> {channel});

            var id = int.Parse(res.Users.Single().Id);

            var bttvEmotes = await _bttvApi.GetUserEmotes(id);

            var ffzEmotes = await _ffzApi.GetChannelEmotes(id);

            _emotes.Edit(updater =>
            {
                updater.AddOrUpdate(bttvEmotes.ChannelEmotes.Select(
                                        emote => new EmoteKeyValue(
                                            new EmoteKey(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Member, emote,
                                                         channel), emote)));
                updater.AddOrUpdate(bttvEmotes.SharedEmotes.Select(
                                        emote => new EmoteKeyValue(
                                            new EmoteKey(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Member, emote,
                                                         channel), emote)));

                updater.AddOrUpdate(ffzEmotes.Sets.SelectMany(pair => pair.Value.Emoticons)
                                             .Select(emote => new EmoteKeyValue(
                                                         new EmoteKey(
                                                             EmoteKey.EmoteType.FrankerZ | EmoteKey.EmoteType.Member,
                                                             emote, channel), emote)));
            });
        }

        private void UnloadChannelEmotes(string channel)
        {
            var group = UserEmoteGroups.Lookup(channel);
            if (!@group.HasValue)
                return;

            var emotes =
                @group.Value.Cache.Items.SelectMany(
                    grouping => grouping.Cache.Items.Select(emote => new EmoteKey(grouping.Key, emote, channel)));


            _emotes.Remove(emotes);
        }


        private IEnumerable<IMessageFragment> ParseTextFragment(string msg, string channel)
        {
            var fragments = new List<IMessageFragment>();

            foreach (var frag in msg.Split(' '))
            {
                var res1 = _globalEmotes.Lookup(frag);
                if (res1.HasValue)
                {
                    fragments.Add(new EmoteFragment(res1.Value));
                    continue;
                }

                var res2 = _userEmotes.Lookup(channel);
                if (res2.HasValue)
                {
                    var res3 = res2.Value.Cache.Lookup(frag);
                    if (res3.HasValue)
                    {
                        fragments.Add(new EmoteFragment(res3.Value));
                        continue;
                    }
                }

                if (fragments.LastOrDefault() is TextFragment text)
                {
                    text.Text += ' ' + frag;
                }
                else
                {
                    fragments.Add(new TextFragment(frag));
                }
            }

            return fragments;
        }


        private async Task<Unit> LoadGlobalEmotes(User user, CancellationToken cancel)
        {
            var globalEmotes = await _apiService.V5.Users.GetUserEmotesAsync(user.Id, _authService.TwitchAccessToken);

            var globalTwitchEmotes =
                (from emoteSet in globalEmotes.EmoteSets
                 from emote in emoteSet.Value
                 select new TwitchGlobalEmote(emote.Id, emote.Code));

            var globalBttvEmotes = await _bttvApi.GetGlobalEmotes();

            var ffzRes = await _ffzApi.GetGlobalEmotes();

            var globalFfzEmotes = ffzRes.DefaultSets.Select(key => ffzRes.Sets[key]).SelectMany(set => set.Emoticons);

            _emotes.Edit(updater =>
            {
                updater.Clear();
                updater.AddOrUpdate(globalTwitchEmotes.Select(
                                        emote => new EmoteKeyValue(
                                            new EmoteKey(EmoteKey.EmoteType.Twitch | EmoteKey.EmoteType.Global, emote),
                                            emote)));
                updater.AddOrUpdate(globalBttvEmotes.Select(
                                        emote => new EmoteKeyValue(
                                            new EmoteKey(EmoteKey.EmoteType.Bttv | EmoteKey.EmoteType.Global, emote),
                                            emote)));
                updater.AddOrUpdate(globalFfzEmotes.Select(
                                        emote => new EmoteKeyValue(
                                            new EmoteKey(EmoteKey.EmoteType.FrankerZ | EmoteKey.EmoteType.Global,
                                                         emote), emote)));
            });

            return Unit.Default;
        }
    }
}