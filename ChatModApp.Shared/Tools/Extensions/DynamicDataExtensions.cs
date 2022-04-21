using DynamicData;

namespace ChatModApp.Tools.Extensions;

public static class DynamicDataExtensions
{
    /// <summary>
    ///  Groups the source on the value returned by group selector factory.  The groupings contains an inner observable list.
    /// </summary>
    /// <typeparam name="TObject">The type of the source object.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TElement">The type of the element.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="keySelector">The group selector.</param>
    /// <param name="elementSelector"></param>
    /// <returns>An observable which emits the change set.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// source
    /// or
    /// keySelector.
    /// </exception>
    public static IObservable<IChangeSet<IGrouping<TKey, TElement>>> GroupByElement<TObject, TKey, TElement>(this IObservable<IChangeSet<TObject>> source, Func<TObject, TKey> keySelector, Func<TObject, TElement> elementSelector)
        where TKey : notnull
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector is null)
            throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector is null)
            throw new ArgumentException(nameof(elementSelector));

        return source.GroupOn(keySelector)
                     .Transform(group => (IGrouping<TKey, TElement>)new ListViewGrouping<TObject, TKey, TElement>(group, elementSelector))
                     .DisposeMany();
    }
}