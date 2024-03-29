﻿using ChatModApp.Shared.Models;
using DynamicData;

namespace ChatModApp.Shared.Services;

public class ChatTabService
{
    public IObservableCache<IChatTabItem, Guid> TabCache { get; }
    public IObservable<IChangeSet<IChatTabItem>> Tabs { get; }


    private readonly SourceCache<IChatTabItem, Guid> _tabs;

    public ChatTabService()
    {
        _tabs = new(item => item.Id);
        TabCache = _tabs.AsObservableCache();
        Tabs = _tabs.Connect()
                    .RemoveKey();
    }

    public void AddTab(IChatTabItem tabItem)
    {
        _tabs.AddOrUpdate(tabItem);
    }

    public void RemoveTab(Guid id)
    {
        _tabs.RemoveKey(id);
    }
}