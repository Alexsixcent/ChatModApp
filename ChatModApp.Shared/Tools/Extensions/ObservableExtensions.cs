using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using Serilog;

namespace ChatModApp.Shared.Tools.Extensions;

public static class ObservableExtensions
{
    //From https://stackoverflow.com/a/27160392
    public static IObservable<T> SampleFirst<T>(
        this IObservable<T> source,
        TimeSpan sampleDuration,
        IScheduler? scheduler = null)
    {
        scheduler = scheduler ?? Scheduler.Default;
        return source.Publish(ps =>
                                  ps.Window(() => ps.Delay(sampleDuration, scheduler))
                                    .SelectMany(x => x.Take(1)));
    }

    public static IObservable<T> ObserveOnMainThread<T>(this IObservable<T> source)
        => source.ObserveOn(RxApp.MainThreadScheduler);

    public static IObservable<T> ObserveOnThreadPool<T>(this IObservable<T> source)
        => source.ObserveOn(RxApp.TaskpoolScheduler);

    /// <summary>
    /// From https://stackoverflow.com/a/19000595
    /// An exponential back off strategy which starts with 1 second and then 4, 9, 16...
    /// </summary>
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
    public static readonly Func<int, TimeSpan> ExponentialBackoff = n => TimeSpan.FromSeconds(Math.Pow(n, 2));

    public static IObservable<T> RetryWithBackoffStrategy<T, TException>(
        this IObservable<T> source, int retryCount, TimeSpan duration, IScheduler? scheduler = null)
        where TException : Exception
    {
        return source.RetryWithBackoffStrategy(retryCount, _ => duration, exception => exception is TException,
                                               scheduler);
    }

    /// <summary>
    /// Returns a cold observable which retries (re-subscribes to) the source observable on error up to the 
    /// specified number of times or until it successfully terminates. Allows for customizable back off strategy.
    /// </summary>
    /// <param name="source">The source observable.</param>
    /// <param name="retryCount">The number of attempts of running the source observable before failing.</param>
    /// <param name="strategy">The strategy to use in backing off, exponential by default.</param>
    /// <param name="retryOnError">A predicate determining for which exceptions to retry. Defaults to all</param>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>
    /// A cold observable which retries (re-subscribes to) the source observable on error up to the 
    /// specified number of times or until it successfully terminates.
    /// </returns>
    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public static IObservable<T> RetryWithBackoffStrategy<T>(
        this IObservable<T> source,
        int retryCount = 3,
        Func<int, TimeSpan>? strategy = null,
        Func<Exception, bool>? retryOnError = null,
        IScheduler? scheduler = null)
    {
        strategy = strategy ?? ExponentialBackoff;
        scheduler = scheduler ?? RxApp.TaskpoolScheduler;
        retryOnError ??= e => true;

        var attempt = 0;

        return Observable.Defer(() =>
                                    (++attempt == 1
                                         ? source
                                         : source.DelaySubscription(strategy(attempt - 1), scheduler))
                                    .Select(item => new Tuple<bool, T, Exception>(true, item, null!))
                                    .Catch<Tuple<bool, T, Exception>, Exception>(e =>
                                    {
                                        if (retryOnError(e))
                                        {
                                            Log.Warning(e,
                                                        "Retry n°{Attempt}/{RetryCount}: Observable raised exception, retrying...",
                                                        attempt - 1, retryCount);
                                            return Observable.Throw<Tuple<bool, T, Exception>>(e);
                                        }

                                        Log.Error(e,
                                                  "Retry n°{Attempt}/{RetryCount}: Observable raised unhandled exception",
                                                  attempt - 1, retryCount);
                                        return Observable.Return(new Tuple<bool, T, Exception>(false, default!, e));
                                    }))
                         .Retry(retryCount)
                         .SelectMany(t => t.Item1 ? Observable.Return(t.Item2) : Observable.Throw<T>(t.Item3));
    }
}