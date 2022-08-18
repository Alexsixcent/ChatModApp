using Refit;

namespace ChatModApp.Shared.Models.Chat.Emotes;

public abstract class BttvEmote : IEmote
{
    public abstract string Id { get; set; }
    public abstract string Code { get; set; }
    public abstract string ImageType { get; set; }

    public Uri Small => new($"https://cdn.betterttv.net/emote/{Id}/1x");
    public Uri Medium => new($"https://cdn.betterttv.net/emote/{Id}/2x");
    public Uri Large => new($"https://cdn.betterttv.net/emote/{Id}/3x");
    public abstract string? Description { get; set; }
    public abstract string Provider { get; }
}

public sealed class BttvUserEmote : BttvEmote
{
    public BttvUserEmote()
    {
        Id = Code = ImageType = UserId = "";
    }

    [AliasAs("id")] public override string Id { get; set; }

    [AliasAs("code")] public override string Code { get; set; }

    [AliasAs("imageType")] public override string ImageType { get; set; }

    [AliasAs("userId")] public string UserId { get; set; }

    public override string Provider => "BetterTTV Channel";
    public override string? Description { get; set; }
}

public sealed class BttvGlobalEmote : BttvEmote
{
    public BttvGlobalEmote()
    {
        Id = Code = ImageType = UserId = "";
    }

    [AliasAs("id")] public override string Id { get; set; }

    [AliasAs("code")] public override string Code { get; set; }

    [AliasAs("imageType")] public override string ImageType { get; set; }

    [AliasAs("userId")] public string UserId { get; set; }

    public override string Provider => "BetterTTV Global";
    public override string? Description { get; set; }
}

public sealed class BttvSharedEmote : BttvEmote
{
    private BttvUser _user = null!;

    public BttvSharedEmote()
    {
        Id= Code = ImageType = "";
    }

    [AliasAs("id")] public override string Id { get; set; }

    [AliasAs("code")] public override string Code { get; set; }

    [AliasAs("imageType")] public override string ImageType { get; set; }

    [AliasAs("user")]
    public BttvUser User
    {
        get => _user;
        set
        {
            _user = value;
            Description = $"By: {value.DisplayName}";
        }
    }

    public override string Provider => "BetterTTV Shared";
    public override string? Description { get; set; }
}

public class BttvUser
{
    public BttvUser()
    {
        Id = Name = DisplayName = ProviderId = "";
    }

    [AliasAs("id")] public string Id { get; set; }

    [AliasAs("name")] public string Name { get; set; }

    [AliasAs("displayName")] public string DisplayName { get; set; }

    [AliasAs("providerId")] public string ProviderId { get; set; }
}