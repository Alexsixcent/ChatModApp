using System.Globalization;
using System.Reactive.Linq;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Tools.Extensions;
using ReactiveUI;

namespace ChatModApp.Shared.Services.EmoteProviders;

public sealed class EmojiEmoteProvider : IEmoteProvider
{
    private const float MaxVersion = 13.0F;
    private readonly HttpClient _client;

    public EmojiEmoteProvider(HttpClient client)
    {
        _client = client;
    }

    public IObservable<IGlobalEmote> LoadGlobalEmotes()
    {
        return GetEmojis();
    }

    private IObservable<EmojiEmote> GetEmojis()
    {
        const string groupPrefix = "# group:";
        const string subgroupPrefix = "# subgroup:";
        const string commentPrefix = "#";

        return Observable
               .FromAsync(token => _client.GetStreamAsync("https://unicode.org/Public/emoji/14.0/emoji-test.txt",
                                                          token))
               .ReadLinesToEnd()
               .WhereNotNullOrWhiteSpace()
               .Select(line =>
               {
                   (string line, string? group, string? subgroup) data = (line, null, null);

                   if (line.StartsWith(groupPrefix, StringComparison.Ordinal))
                   {
                       data.group = FormatName(line[groupPrefix.Length..]);
                       return data;
                   }

                   if (!line.StartsWith(subgroupPrefix, StringComparison.Ordinal))
                       return data;

                   data.subgroup = FormatName(line[subgroupPrefix.Length..]);
                   return data;
               })
               .Scan((l, r) =>
               {
                   r.group ??= l.group;
                   r.subgroup ??= l.subgroup;
                   return r;
               })
               .Where(tuple => !tuple.line.StartsWith(commentPrefix, StringComparison.Ordinal))
               .Select(tuple =>
               {
                   var value = ParseEmoji(tuple.line);
                   return value is null
                              ? null
                              : new EmojiEmote(value.Value.name, value.Value.value, tuple.group!, tuple.subgroup!);
               })
               .WhereNotNull();
    }

    private static (string name, string value)? ParseEmoji(string line)
    {
        var parts = line.Split(new[] { ';', '#' }, 3);

        if (parts[1].Trim() != "fully-qualified")
            return null;

        var versionAndName = parts[2].Split('E', 2)[1].Split(' ', 2);
        var version = float.Parse(versionAndName[0], NumberFormatInfo.InvariantInfo);

        if (version > MaxVersion)
            return null;

        var name = FormatName(versionAndName[1]);

        if (char.IsDigit(name[0])) name = "_" + name;

        var surrogates = parts[0].Trim()
                                 .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => int.Parse(x, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo))
                                 .Select(char.ConvertFromUtf32);

        var value = string.Concat(surrogates);

        return (name, value);
    }

    private static string FormatName(string source)
    {
        var parts = source
                    .Replace(':', '_')
                    .Replace('-', ' ')
                    .Replace("&", "And")
                    .Replace("#", "NumberSign")
                    .Replace("*", "Asterisk")
                    .Replace(",", string.Empty)
                    .Replace("’", string.Empty)
                    .Split(new[] { ' ', '-', ',', '’', '!', '“', '”', '(', ')', '.' },
                           StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x));

        return string.Concat(parts);
    }
}