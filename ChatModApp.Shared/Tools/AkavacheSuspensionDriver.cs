using System.Reactive;
using Akavache;
using ReactiveUI;

namespace ChatModApp.Shared.Tools;

public class AkavacheSuspensionDriver<TAppState> : ISuspensionDriver where TAppState : class
{
    private const string AppStateKey = "appState";

    public AkavacheSuspensionDriver() => BlobCache.ApplicationName = "ChatModApp";

    public IObservable<object> LoadState() =>
#if DEBUG
        BlobCache.LocalMachine.GetObject<TAppState>(AppStateKey)!;
#else
        BlobCache.Secure.GetObject<TAppState>(AppStateKey)!;
#endif

    public IObservable<Unit> SaveState(object state) =>
#if DEBUG
        BlobCache.LocalMachine.InsertObject(AppStateKey, (TAppState)state);
#else
        BlobCache.Secure.InsertObject(AppStateKey, (TAppState)state);
#endif

    public IObservable<Unit> InvalidateState() =>
#if DEBUG
        BlobCache.LocalMachine.InvalidateObject<TAppState>(AppStateKey);
#else
        BlobCache.Secure.InvalidateObject<TAppState>(AppStateKey);
#endif
}