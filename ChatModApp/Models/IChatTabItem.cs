using System;

namespace ChatModApp.Models
{
    public interface IChatTabItem
    {
        Guid Id { get; }
        string Title { get; set; }
        string Channel { get; set; }
    }
}