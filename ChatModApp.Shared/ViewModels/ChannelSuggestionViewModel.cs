using ChatModApp.Shared.Models;

namespace ChatModApp.Shared.ViewModels;

public class ChannelSuggestionViewModel
{
    public bool IsLive { get; }

    public Uri ThumbnailUrl { get; }

    public ITwitchUser Channel { get; }
    public string DisplayName => Channel.DisplayName;
    public string Login => Channel.Login;
    
    public ChannelSuggestionViewModel(ITwitchUser user, Uri thumbnailUrl, bool isLive = false)
    {
        Channel = user;
        ThumbnailUrl = thumbnailUrl;
        IsLive = isLive;
    }

    public override string ToString()
    {
        return DisplayName;
    }
}