using System;
using System.Reactive;
using Akavache;
using ReactiveUI;

namespace ChatModApp.Tools;

class AkavacheSuspensionDriver<TAppState> : ISuspensionDriver where TAppState : class
{
    private const string AppStateKey = "appState";

    public AkavacheSuspensionDriver() => BlobCache.ApplicationName = "ChatModApp";

    public IObservable<object> LoadState() => BlobCache.UserAccount.GetObject<TAppState>(AppStateKey);

    public IObservable<Unit> SaveState(object state) =>
        BlobCache.UserAccount.InsertObject(AppStateKey, (TAppState) state);
    public IObservable<Unit> InvalidateState() => BlobCache.UserAccount.InvalidateObject<TAppState>(AppStateKey);
}