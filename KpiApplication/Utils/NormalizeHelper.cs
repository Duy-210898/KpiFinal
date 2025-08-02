using System.Linq;
using System.Text.RegularExpressions;

namespace KpiApplication.Utils
{
    public static class NormalizeHelper
    {
        public static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var cleaned = new string(input
                .Where(c => !char.IsControl(c) && c != '\u200B' && c != '\u00A0') 
                .ToArray());

            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }
    }
}
