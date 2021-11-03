using System;
using DynamicData;

namespace ChatModApp.Tools;

public class Grouping<TObject, TKey, TGroupKey> : IGroup<TObject, TKey, TGroupKey>, IDisposable where TKey : notnull
{
    public TGroupKey Key { get; }
    public IObservableCache<TObject, TKey> Cache { get; }

    public Grouping(TGroupKey key, IObservableCache<TObject, TKey> cache)
    {
        Key = key;
        Cache = cache;
    }

    public void Dispose()
    {
        Cache.Dispose();
    }
}