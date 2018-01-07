namespace toofz
{
    internal static class StringExtensions
    {
        public static bool TryIndexOf(
            this string str,
            string value,
            out int index)
        {
            index = str.IndexOf(value);

            return index > -1;
        }

        public static bool TryIndexOf(
            this string str,
            char value,
            int startIndex,
            out int index)
        {
            index = str.IndexOf(value, startIndex);

            return index > -1;
        }
    }
}
