using System.Windows;
using System.Windows.Media;
using Kitsune7Den.Models;

namespace Kitsune7Den.Services;

public class ThemeService
{
    private readonly AppSettings _settings;

    public static readonly string[] AvailableThemes = ["Kitsune", "Midnight", "Forest", "Accessible"];

    public string CurrentTheme => _settings.Theme;

    public ThemeService(AppSettings settings)
    {
        _settings = settings;
    }

    public void ApplyTheme(string themeName)
    {
        if (!AvailableThemes.Contains(themeName))
            themeName = "Kitsune";

        var app = Application.Current;

        // Load the theme resource dictionary
        var themeUri = new Uri($"Resources/Themes/{themeName}.xaml", UriKind.Relative);
        var themeDict = new ResourceDictionary { Source = themeUri };

        // Read colors from the theme
        var colorMap = new (string colorKey, string brushKey)[]
        {
            ("BgDark", "BgDarkBrush"),
            ("BgMedium", "BgMediumBrush"),
            ("BgLight", "BgLightBrush"),
            ("Accent", "AccentBrush"),
            ("AccentHover", "AccentHoverBrush"),
            ("TextPrimary", "TextPrimaryBrush"),
            ("TextSecondary", "TextSecondaryBrush"),
            ("BorderColor", "BorderBrush"),
            ("SuccessColor", "SuccessBrush"),
            ("WarningColor", "WarningBrush"),
        };

        // Replace theme dictionary in merged dictionaries
        var merged = app.Resources.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i].Source?.OriginalString.Contains("Themes/") == true)
            {
                merged.RemoveAt(i);
                break;
            }
        }
        merged.Insert(0, themeDict);

        // Now update the Color resources and Brush resources at the app level
        // This makes DynamicResource bindings pick up the new values
        foreach (var (colorKey, brushKey) in colorMap)
        {
            if (themeDict[colorKey] is Color color)
            {
                app.Resources[colorKey] = color;
                app.Resources[brushKey] = new SolidColorBrush(color);
            }
        }

        _settings.Theme = themeName;
        _settings.Save();
    }
}
