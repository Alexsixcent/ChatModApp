using ChatModApp.Shared.Models;
using ChatModApp.Shared.Tools;
using DynamicData;

namespace ChatModApp.Shared.Services;

public class ChatTabService
{
    public IObservableCache<IChatTabItem, Guid> TabCache { get; }
    public IObservable<IChangeSet<IChatTabItem>> Tabs { get; }

    private readonly SourceList<IChatTabItem> _tabs;


    public ChatTabService(AppState state)
    {
        if (state.OpenedTabs is SourceList<IChatTabItem> list)
            _tabs = list;
        else
        {
            _tabs = new();
            state.OpenedTabs = _tabs;
        }
        
        Tabs = _tabs.Connect();
        TabCache = Tabs.AddKey(item => item.Id).AsObservableCache();
    }

    public void AddTab(IChatTabItem tabItem) => _tabs.Add(tabItem);

    public void RemoveTab(Guid id)
    {
        _tabs.Edit(list =>
        {
            var tab = TabCache.Lookup(id);
            if (tab.HasValue)
                list.Remove(tab.Value);
        });
    }
}