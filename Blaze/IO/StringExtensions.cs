namespace Blaze.IO
{
    internal static class StringExtensions
    {
        public static bool HasUpperCase(this string str)
        {
            return str.ToLower() != str;
        }
    }
}
