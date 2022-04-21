using System.Globalization;

namespace ChatModApp.Shared.Tools.Extensions;

public static class StringExtension
{
    //Inspired by https://codereview.stackexchange.com/a/102623
    public static bool HasConsecutiveChar(this string source, string s, int repeatCount = 2)
    {
        if (s is null) 
            throw new ArgumentNullException(nameof(s));
        if (repeatCount < 1)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), $"The sequence length can't be negative or zero");

        var charEnumerator = StringInfo.GetTextElementEnumerator(source);
        var count = 0;
        while (charEnumerator.MoveNext())
        {
            if (s == charEnumerator.GetTextElement())
            {
                if (++count >= repeatCount)
                    return true;
            }
            else
                count = 0;
        }

        return false;
    }

    public static string TrimStart(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString))
        {
            return target;
        }

        var result = target;
        while (result.StartsWith(trimString))
        {
            result = result.Substring(trimString.Length);
        }

        return result;
    }

    public static string TrimEnd(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString))
        {
            return target;
        }

        var result = target;
        while (result.EndsWith(trimString))
        {
            result = result.Substring(0, result.Length - trimString.Length);
        }

        return result;
    }

    public static string TrimStart(this string target, int number) =>
        target.Substring(number, target.Length - number);

    public static string TrimEnd(this string target, int number) => target.Substring(0, target.Length - number);

    public static string SubstringAbs(this string target, int startIndex, int endIndex) =>
        target.Substring(startIndex, endIndex - startIndex + 1);
}