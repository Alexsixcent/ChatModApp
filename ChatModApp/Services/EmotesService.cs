using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ChatModApp.Models;
using DynamicData;
using DynamicData.Kernel;
using Tools;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client.Models;

namespace ChatModApp.Services
{
    public class EmotesService : IService
    {
        private readonly TwitchApiService _apiService;
        private readonly AuthenticationService _authService;

        private readonly SourceCache<IEmote, string> _globalEmotes;

        public EmotesService(TwitchApiService apiService, AuthenticationService authService)
        {
            _globalEmotes = new SourceCache<IEmote, string>(emote => emote.Code);

            _apiService = apiService;
            _authService = authService;

            _apiService.Connected
                .Select(user => Connect(user).ToObservable())
                .Concat()
                .Subscribe();
        }

        public Task Initialize() => Task.CompletedTask;

        public IEnumerable<IMessageFragment> GetMessageFragments(ChatMessage message)
        {
            var fragments = new List<IMessageFragment>();

            foreach (var frag in message.Message.Split(' '))
            {
                var res = _globalEmotes.Lookup(frag);
                if (res.HasValue)
                {
                    fragments.Add(new EmoteFragment(res.Value));
                }
                else
                {
                    var last = fragments.LastOrDefault();
                    if (last is TextFragment text)
                    {
                        text.Text += ' ' + frag;
                    }
                    else
                    {
                        fragments.Add(new TextFragment {Text = frag});
                    }
                }
            }

            return fragments;
        }


        private async Task Connect(User user)
        {
            var emotes = await _apiService.V5.Users.GetUserEmotesAsync(user.Id, _authService.TwitchAccessToken);

            var emoteList =
                (from emoteSet in emotes.EmoteSets
                    from emote in emoteSet.Value
                    select new TwitchEmote(emote.Id, emote.Code));

            _globalEmotes.AddOrUpdate(emoteList);
        }
    }
}