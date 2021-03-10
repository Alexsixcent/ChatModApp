using System.Runtime.Serialization;

namespace ChatModApp
{
    [DataContract]
    class AppState
    {
        public string TwitchAccessToken { get; set; }
    }
}
