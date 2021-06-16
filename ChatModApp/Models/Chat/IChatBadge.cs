using System;

namespace ChatModApp.Models.Chat
{
    public interface IChatBadge
    {
        Uri Small { get; }
        Uri Medium { get; }
        Uri Large { get; }
    }
}