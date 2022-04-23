using System.Runtime.Serialization;

namespace ChatModApp.Shared.Tools;

[DataContract]
public class AppState
{
    [DataMember]
    public string? TwitchAccessToken { get; set; }
}