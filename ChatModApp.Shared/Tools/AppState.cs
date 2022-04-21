using System.Runtime.Serialization;

namespace ChatModApp.Tools;

[DataContract]
class AppState
{
    public string TwitchAccessToken { get; set; }
}