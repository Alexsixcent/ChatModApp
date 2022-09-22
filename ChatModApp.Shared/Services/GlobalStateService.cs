using System.Collections.Immutable;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using ChatModApp.Shared.Models.Chat.Emotes;
using ChatModApp.Shared.Tools.Extensions;
using ReactiveUI;

namespace ChatModApp.Shared.Services;

public sealed class GlobalStateService
{
    public ImmutableHashSet<string> TLDs { get; private set; }


    private readonly HttpClient _client;
    private readonly BlazorHostingService _blazorService;
    private readonly EmotesService _emotesService;


    public GlobalStateService(BlazorHostingService blazorService, EmotesService emotesService, HttpClient client)
    {
        _blazorService = blazorService;
        _emotesService = emotesService;
        _client = client;
        TLDs = ImmutableHashSet<string>.Empty;
    }

    public async Task Initialize()
    {
        var tldObv = GetTLDs().ToArray().ToTask();
        var emojiObv = GetEmojis().ToArray().ToTask();

        var (tlds, emojis) = await (tldObv, emojiObv);
        
        TLDs = ImmutableHashSet.CreateRange(tlds);
        
        if (!BlazorHostingService.IsBlazorAuthDisabled)
            await _blazorService.StartBlazor();
    }

    private IObservable<string> GetTLDs()
    {
        var mapping = new IdnMapping();

        return Observable
               .FromAsync(token => _client.GetStreamAsync("https://data.iana.org/TLD/tlds-alpha-by-domain.txt",
                                                          token))
               .ReadLinesToEnd()
               .Skip(1)
               .WhereNotNullOrWhiteSpace()
               .Select(s => mapping.GetUnicode(s.ToLowerInvariant()));
    }

    private const float MaxVersion = 13.0F;

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
                   return value is null ? null 
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