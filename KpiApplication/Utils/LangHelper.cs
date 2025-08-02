using System;
using System.Globalization;
using System.Resources;

namespace KpiApplication.Common
{
    public static class LangHelper
    {
        private static readonly ResourceManager _resourceManager =
            new ResourceManager("KpiApplication.Resources.Strings", typeof(LangHelper).Assembly);

        public static string GetString(string key)
        {
            try
            {
                var culture = new CultureInfo(Properties.Settings.Default.AppCulture ?? "en");
                var raw = _resourceManager.GetString(key, culture) ?? $"[[{key}]]";

                return raw.Replace("\\n", Environment.NewLine);
            }
            catch
            {
                return $"[[{key}]]";
            }
        }

        public static void ApplyCulture()
        {
            var culture = new CultureInfo(Properties.Settings.Default.AppCulture ?? "en");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
