namespace ChatModApp.AuthCallback;

public class AuthenticatedEventArgs : EventArgs
{
    public Uri CallbackUri { get; }

    public AuthenticatedEventArgs(Uri callbackUri)
    {
        CallbackUri = callbackUri;
    }
}

public sealed class AuthTriggeredService
{
    public event EventHandler<AuthenticatedEventArgs>? Authenticated;

    internal void Authenticate(AuthenticatedEventArgs args)
    {
        Authenticated?.Invoke(this, args);
    }
}