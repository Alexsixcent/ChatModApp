using ChatModApp.Shared.Models;
using Refit;

namespace ChatModApp.Shared.Services.ApiClients;

public interface IRobottyApi
{
    [Get("/recent-messages/{channel}")]
    public Task<ApiResponse<RobottyRecentMessagesResponse>> GetRecentMessages([AliasAs("channel")] string channel, CancellationToken cancellationToken = default);
}