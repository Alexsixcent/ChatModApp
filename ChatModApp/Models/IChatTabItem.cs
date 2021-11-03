using System;
using System.ComponentModel;

namespace ChatModApp.Models;

public interface IChatTabItem : INotifyPropertyChanged
{
    Guid Id { get; }
    string Title { get; set; }
    ITwitchChannel? Channel { get; set; }
}