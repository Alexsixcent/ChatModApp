using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using ChatModApp.Models.Chat;
using ChatModApp.Models.Chat.Fragments;

namespace ChatModApp.Views.Controls.MessageView;

public partial class MessageView
{
    public static readonly DependencyProperty MessageFragmentsProperty = DependencyProperty.Register(
        "MessageFragments", typeof(IEnumerable<IMessageFragment>), typeof(MessageView),
        new PropertyMetadata(default(IEnumerable<IMessageFragment>)));

    public IEnumerable<IMessageFragment> MessageFragments
    {
        get => (IEnumerable<IMessageFragment>)GetValue(MessageFragmentsProperty);
        set => SetValue(MessageFragmentsProperty, value);
    }

    public static readonly DependencyProperty ChatBadgesProperty = DependencyProperty.Register(
        "ChatBadges", typeof(IEnumerable<IChatBadge>), typeof(MessageView), new PropertyMetadata(default(IEnumerable<IChatBadge>)));

    public IEnumerable<IChatBadge> ChatBadges
    {
        get => (IEnumerable<IChatBadge>)GetValue(ChatBadgesProperty);
        set => SetValue(ChatBadgesProperty, value);
    }

    public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
        "Username", typeof(string), typeof(MessageView), new PropertyMetadata(default(string)));

    public string Username
    {
        get => (string)GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public static readonly DependencyProperty UsernameColorProperty = DependencyProperty.Register(
        "UsernameColor", typeof(Color), typeof(MessageView), new PropertyMetadata(default(Color)));

    public Color UsernameColor
    {
        get => (Color)GetValue(UsernameColorProperty);
        set => SetValue(UsernameColorProperty, value);
    }
}