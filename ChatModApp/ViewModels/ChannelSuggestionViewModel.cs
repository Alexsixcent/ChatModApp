using System;

namespace ChatModApp.ViewModels;

public class ChannelSuggestionViewModel
{
    public bool IsLive { get; }
    public Uri ThumbnailUrl { get; }
    public string DisplayName { get; }
    public string Login { get; }

    public ChannelSuggestionViewModel(string login, string displayName, Uri thumbnailUrl, bool isLive = false)
    {
        Login = login;
        DisplayName = displayName;
        ThumbnailUrl = thumbnailUrl;
        IsLive = isLive;
    }
}