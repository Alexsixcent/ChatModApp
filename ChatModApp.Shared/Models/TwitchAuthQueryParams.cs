using Refit;

namespace ChatModApp.Shared.Models;

public class TwitchAuthQueryParams
{
    [AliasAs("client_id")]
    public string ClientId { get; set; }

    [AliasAs("redirect_uri")]
    public Uri RedirectUri { get; set; }

    [AliasAs("response_type")]
    public TwitchAuthResponseType ResponseType { get; set; }
        
    [AliasAs("scope")]
    public IEnumerable<TwitchAuthScope> Scopes { get; set; }

    [AliasAs("force_verify")]
    public bool? ForceVerify { get; set; }

    [AliasAs("state")]
    public string? State { get; set; }
}