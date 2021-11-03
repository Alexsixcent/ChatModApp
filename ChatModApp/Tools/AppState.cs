using System.Runtime.Serialization;

namespace ChatModApp;

[DataContract]
internal class AppState
{
    public string TwitchAccessToken { get; set; }
}