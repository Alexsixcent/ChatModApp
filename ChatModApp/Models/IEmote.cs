using System;

namespace ChatModApp.Models
{
    public interface IEmote
    {
        string Code { get; }
        int Id { get; }
        Uri Uri { get;  }

        int Height { get; }
        int Width { get; }
    }
}