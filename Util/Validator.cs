using System.Text.RegularExpressions;

namespace ForecastingModule.Util
{
    public sealed class Validator
    {
        public static bool IsWholeNumber(string input)
        {
            return int.TryParse(input, out _);
        }

        public static string RemoveNonNumericCharacters(string input)
        {
            return Regex.Replace(input, @"[^0-9]", ""); // Replace all non-numeric characters
        }
    }
}
