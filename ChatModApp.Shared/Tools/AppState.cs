using System.Runtime.Serialization;
using ChatModApp.Shared.Converters;
using ChatModApp.Shared.Models;
using DynamicData;
using Newtonsoft.Json;

namespace ChatModApp.Shared.Tools;

[DataContract]
public class AppState
{
    [DataMember] public string? TwitchAccessToken { get; set; }

    [JsonConverter(typeof(ObservableListConverter<IChatTabItem>))]
    [DataMember] public IObservableList<IChatTabItem>? OpenedTabs { get; set; }
}