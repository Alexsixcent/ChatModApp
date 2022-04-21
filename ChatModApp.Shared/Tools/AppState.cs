using System.Runtime.Serialization;

namespace ChatModApp.Shared.Tools;

[DataContract]
class AppState
{
    public string TwitchAccessToken { get; set; }
}