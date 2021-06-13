namespace Tools.Extensions
{
    public static class StringExtension
    {
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
}