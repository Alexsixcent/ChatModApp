using System;

namespace ChatModApp.Models.Chat.Fragments
{
    public class UriFragment : IMessageFragment
    {
        public string Text { get; }
        public Uri Uri { get; }

        public UriFragment(Uri uri)
        {
            Uri = uri;
            Text = uri.OriginalString;
        }
        public UriFragment(Uri uri, string text)
        {
            Uri = uri;
            Text = text;
        }
    }
}