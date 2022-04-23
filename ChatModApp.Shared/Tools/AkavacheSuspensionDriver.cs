using System.Reactive;
using Akavache;
using ReactiveUI;

namespace ChatModApp.Shared.Tools;

public class AkavacheSuspensionDriver<TAppState> : ISuspensionDriver where TAppState : class
{
    private const string AppStateKey = "appState";

    public AkavacheSuspensionDriver() => BlobCache.ApplicationName = "ChatModApp";

    public IObservable<object> LoadState() => BlobCache.Secure.GetObject<TAppState>(AppStateKey)!;

    public IObservable<Unit> SaveState(object state) =>
        BlobCache.Secure.InsertObject(AppStateKey, (TAppState) state);
    public IObservable<Unit> InvalidateState() => BlobCache.Secure.InvalidateObject<TAppState>(AppStateKey);
}