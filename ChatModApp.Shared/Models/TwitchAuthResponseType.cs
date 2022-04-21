using System.Runtime.Serialization;

namespace ChatModApp.Models;

public enum TwitchAuthResponseType
{
    [EnumMember(Value = "token")]
    Token
}