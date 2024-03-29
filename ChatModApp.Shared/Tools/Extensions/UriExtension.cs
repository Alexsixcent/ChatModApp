﻿namespace ChatModApp.Shared.Tools.Extensions;

internal static class UriExtension
{
    public static Uri RewriteHttps(this Uri originalUri)
    {
        var uri = !originalUri.IsAbsoluteUri ? new(originalUri.OriginalString, UriKind.Absolute) : originalUri;
            
        return new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttps,
            Port = uri.IsDefaultPort ? -1 : uri.Port // -1 => default port for scheme
        }.Uri;
    }
}