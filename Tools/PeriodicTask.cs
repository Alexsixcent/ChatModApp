using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tools
{
    public static class PeriodicTask
    {
        public static async Task Run<T>(Func<CancellationToken, T> func, TimeSpan dueTime, TimeSpan period, CancellationToken cancellationToken = default)
            where T : Task
        {
            await Task.Delay(dueTime, cancellationToken).ConfigureAwait(false);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();
                await func(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                
                await Task.Delay(period - stopwatch.Elapsed, cancellationToken);
            }
        }
    }
}