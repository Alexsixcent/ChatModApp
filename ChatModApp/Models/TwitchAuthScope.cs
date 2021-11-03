using System.Runtime.Serialization;

namespace ChatModApp.Models;

public enum TwitchAuthScope
{
    #region Kraken API

    [EnumMember(Value = "analytics:read:extensions")]
    AnalyticsReadExtensions,

    [EnumMember(Value = "analytics:read:games")]
    AnalyticsReadGames,

    [EnumMember(Value = "bits:read")]
    BitsRead,

    [EnumMember(Value = "channel:edit:commercial")]
    ChannelEditCommercial,

    [EnumMember(Value = "channel:manage:broadcast")]
    ChannelManageBroadcast,

    [EnumMember(Value = "channel:manage:extensions")]
    ChannelManageExtensions,

    [EnumMember(Value = "channel:manage:redemptions")]
    ChannelManageRedemptions,

    [EnumMember(Value = "channel:manage:videos")]
    ChannelManageVideos,

    [EnumMember(Value = "channel:read:editors")]
    ChannelReadEditors,

    [EnumMember(Value = "channel:read:hype_train")]
    ChannelReadHypeTrain,

    [EnumMember(Value = "channel:read:redemptions")]
    ChannelReadRedemptions,

    [EnumMember(Value = "channel:read:stream_key")]
    ChannelReadStreamKey,

    [EnumMember(Value = "channel:read:subscriptions")]
    ChannelReadSubscriptions,

    [EnumMember(Value = "clips:edit")]
    ClipsEdit,

    [EnumMember(Value = "moderation:read")]
    ModerationRead,

    [EnumMember(Value = "user:edit")]
    UserEdit,

    [EnumMember(Value = "user:edit:follows")]
    UserEditFollows,

    [EnumMember(Value = "user:read:blocked_users")]
    UserReadBlockedUsers,

    [EnumMember(Value = "user:manage:blocked_users")]
    UserManageBlockedUsers,

    [EnumMember(Value = "user:read:broadcast")]
    UserReadBroadcast,

    [EnumMember(Value = "user:read:email")]
    UserReadEmail,

    [EnumMember(Value = "user:read:subscriptions")]
    UserReadSubscriptions,

    #region Chat and PubSub

    [EnumMember(Value = "channel:moderate")]
    ChannelModerate,

    [EnumMember(Value = "chat:edit")]
    ChatEdit,

    [EnumMember(Value = "chat:read")]
    ChatRead,

    [EnumMember(Value = "whispers:read")]
    WhispersRead,

    [EnumMember(Value = "whispers:edit")]
    WhispersEdit,

    #endregion

    #endregion

    #region V5 API

    [EnumMember(Value = "viewing_activity_read")]
    ViewingActivityRead,

    [EnumMember(Value = "user_subscriptions")]
    UserSubscriptions

    #endregion
}