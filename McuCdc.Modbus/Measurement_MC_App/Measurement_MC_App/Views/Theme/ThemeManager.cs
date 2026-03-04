using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Measurement_MC_App.Views.Theme
{
    public static class ThemeManager
    {
        private static Uri GetThemeUri(string themeName)
            => new($"/Measurement_MC_App;component/Views/Theme/{themeName}.xaml", UriKind.Relative);

        public static void Apply(string themeName) // "Dark" | "Light"
        {
            var app = Application.Current;
            if (app == null) return;

            var merged = app.Resources.MergedDictionaries;

            var old = merged.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("/Views/Theme/", StringComparison.OrdinalIgnoreCase));

            if (old != null) merged.Remove(old);

            merged.Insert(0, new ResourceDictionary { Source = GetThemeUri(themeName) });
        }

        public static string DetectCurrentTheme()
        {
            var app = Application.Current;
            if (app == null) return "Dark";

            var theme = app.Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("/Views/Theme/", StringComparison.OrdinalIgnoreCase));

            if (theme?.Source?.OriginalString.Contains("Light", StringComparison.OrdinalIgnoreCase) == true)
                return "Light";

            return "Dark";
        }
    }
}
