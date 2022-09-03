using System.Collections.Immutable;
using System.Globalization;
using ChatModApp.Shared.Models.Chat.Emotes;

namespace ChatModApp.Shared.Services;

public class GlobalStateService
{
    public ImmutableHashSet<string> TLDs { get; private set; }

    
    private readonly HttpClient _client;

    
    public GlobalStateService(HttpClient client)
    {
        _client = client;
        TLDs = ImmutableHashSet<string>.Empty;
    }

    public async Task Initialize()
    {
        var tldTask = GetTLDs();
        var emojiTask = GetEmojis().ToListAsync();

        TLDs = ImmutableHashSet.CreateRange(await tldTask);
        var emojis = await emojiTask;
    }

    private async Task<IEnumerable<string>> GetTLDs()
    {
        var mapping = new IdnMapping();

        var tldText = await _client.GetStringAsync("https://data.iana.org/TLD/tlds-alpha-by-domain.txt")
                                   .ConfigureAwait(false);

        return tldText
               .Split('\n', StringSplitOptions.RemoveEmptyEntries)
               .Skip(1)
               .Select(s => mapping.GetUnicode(s.ToLowerInvariant()));
    }

    private const float MaxVersion = 13.0F;
    
    private async IAsyncEnumerable<EmojiEmote> GetEmojis()
    {
        const string groupPrefix = "# group:";
        const string subgroupPrefix = "# subgroup:";
        const string commentPrefix = "#";
        
        var emojisText = await _client.GetStreamAsync("https://unicode.org/Public/emoji/14.0/emoji-test.txt")
                                      .ConfigureAwait(false);

        var reader = new StreamReader(emojisText);
        
        var group = string.Empty;
        var subgroup = string.Empty;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith(groupPrefix, StringComparison.Ordinal))
            {
                group = FormatName(line[groupPrefix.Length..]);
                continue;
            }

            if (line.StartsWith(subgroupPrefix, StringComparison.Ordinal))
            {
                subgroup = FormatName(line[subgroupPrefix.Length..]);
                continue;
            }

            if (line.StartsWith(commentPrefix, StringComparison.Ordinal))
                continue;

            var emoji = ParseEmoji(line);
            if (emoji is not null)
            {
                yield return new(emoji.Value.name, emoji.Value.value, group, subgroup);
            }
        }
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
                    .Split(new[] { ' ', '-', ',', '’', '!', '“', '”', '(', ')', '.' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x));

        return string.Concat(parts);
    }
}