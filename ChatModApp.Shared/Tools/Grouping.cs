using ChatModApp.Shared.Tools.Extensions;
using CommunityToolkit.Mvvm.Collections;
using DynamicData;
using DynamicData.Binding;

namespace ChatModApp.Shared.Tools;

public class Grouping<TObject, TKey, TGroupKey> : IGroup<TObject, TKey, TGroupKey>, IDisposable
    where TKey : notnull
{
    public Grouping(TGroupKey key, IObservableCache<TObject, TKey> cache)
    {
        Key = key;
        Cache = cache;
    }

    public void Dispose() => Cache.Dispose();


    public TGroupKey Key { get; }
    public IObservableCache<TObject, TKey> Cache { get; }
}

// From: https://stackoverflow.com/a/52928955
// custom IGrouping implementation which is required by ListView
public class ListViewGrouping<TObject, TKey, TElement> : ObservableCollectionExtended<TElement>, IGrouping<TKey, TElement>, IReadOnlyObservableGroup, IDisposable
{
    private readonly IDisposable _subscription;
    public ListViewGrouping(IGroup<TObject, TKey> group, Func<TObject, TElement> elementSelector) 
    {
        if (group == null)
            throw new ArgumentNullException(nameof(group));

        Key = group.GroupKey;
        _subscription = group.List
                             .Connect()
                             .Transform(elementSelector)
                             .ObserveOnMainThread()
                             .Bind(this)
                             .Subscribe();
    }

    public TKey Key { get; }

    public new object? this[int index] => base[index];

    object IReadOnlyObservableGroup.Key => Key ?? throw new InvalidOperationException();

    public void Dispose() => _subscription.Dispose();
}