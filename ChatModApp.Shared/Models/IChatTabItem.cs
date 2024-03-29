﻿using System.ComponentModel;

namespace ChatModApp.Shared.Models;

public interface IChatTabItem : INotifyPropertyChanged
{
    Guid Id { get; }
    string Title { get; set; }
    ITwitchUser? Channel { get; set; }
    Uri? ChannelIcon { get; set; }
}