using System.Text.Json.Serialization;

namespace ChatModApp.Shared.Models;

public class RobottyRecentMessagesResponse
{
    [JsonPropertyName("messages")] public IReadOnlyList<string> Messages { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("error_code")] public string? ErrorCode { get; set; }
}