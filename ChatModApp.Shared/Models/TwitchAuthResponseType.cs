using System.Runtime.Serialization;

namespace ChatModApp.Shared.Models;

public enum TwitchAuthResponseType
{
    [EnumMember(Value = "token")]
    Token
}