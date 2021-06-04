using System;

namespace ChatModApp.Models
{
    public interface IEmote
    {
        string Code { get; }
        Uri Uri { get; }
    }
}