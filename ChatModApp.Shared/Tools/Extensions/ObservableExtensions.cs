using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ChatModApp.Shared.Tools.Extensions;

//From https://stackoverflow.com/a/27160392
public static class ObservableExtensions
{
    public static IObservable<T> SampleFirst<T>(
        this IObservable<T> source,
        TimeSpan sampleDuration,
        IScheduler? scheduler = null)
    {
        scheduler = scheduler ?? Scheduler.Default;
        return source.Publish(ps => 
                                  ps.Window(() => ps.Delay(sampleDuration,scheduler))
                                    .SelectMany(x => x.Take(1)));
    }
}