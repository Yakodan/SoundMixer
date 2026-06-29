namespace SoundMixer.App.Helpers;

public static class ThemeManager
{
    public static readonly string[] AvailableThemes = new[]
    {
        "DarkPurple",
        "DarkBlue",
        "DarkGreen",
        "DarkRed",
        "LightClean",
        "LightWarm"
    };

    public static string CurrentTheme { get; private set; } = "DarkPurple";

    public static void ApplyTheme(string themeName)
    {
        try
        {
            var uri = new Uri($"Resources/Themes/{themeName}.xaml", UriKind.Relative);
            
            System.Diagnostics.Debug.WriteLine($"[Theme] Загрузка: {uri}");

            var dict = new System.Windows.ResourceDictionary { Source = uri };
            
            System.Diagnostics.Debug.WriteLine($"[Theme] Загружен словарь: {dict.Count} ресурсов");

            var mergedDicts = App.Current.Resources.MergedDictionaries;

            // Удаляем старую тему
            for (int i = mergedDicts.Count - 1; i >= 0; i--)
            {
                var d = mergedDicts[i];
                if (d.Source != null && d.Source.OriginalString.Contains("Themes/"))
                {
                    System.Diagnostics.Debug.WriteLine($"[Theme] Удаляю старую: {d.Source}");
                    mergedDicts.RemoveAt(i);
                }
            }

            // Добавляем новую тему первой
            mergedDicts.Insert(0, dict);
            CurrentTheme = themeName;

            System.Diagnostics.Debug.WriteLine($"[Theme] Применена: {themeName}, всего словарей: {mergedDicts.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Theme] ОШИБКА при загрузке '{themeName}': {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Theme] StackTrace: {ex.StackTrace}");
        }
    }

    public static string GetDisplayName(string themeName)
    {
        return themeName switch
        {
            "DarkPurple" => "Dark Purple",
            "DarkBlue" => "Dark Blue",
            "DarkGreen" => "Dark Green",
            "DarkRed" => "Dark Red",
            "LightClean" => "Light Clean",
            "LightWarm" => "Light Warm",
            _ => themeName
        };
    }
}