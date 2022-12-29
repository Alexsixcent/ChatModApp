using ChatModApp.Shared.Models;

namespace ChatModApp.Shared.ViewModels;

public class ChannelSuggestionViewModel
{
    public bool IsLive { get; }

    public Uri ThumbnailUrl { get; }

    public ITwitchChannel Channel { get; }
    public string DisplayName => Channel.DisplayName;
    public string Login => Channel.Login;
    
    public ChannelSuggestionViewModel(ITwitchChannel channel, Uri thumbnailUrl, bool isLive = false)
    {
        Channel = channel;
        ThumbnailUrl = thumbnailUrl;
        IsLive = isLive;
    }

    public override string ToString()
    {
        return DisplayName;
    }
}