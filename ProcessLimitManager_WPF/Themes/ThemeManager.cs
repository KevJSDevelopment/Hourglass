using System.Windows;
using System.Windows.Media;

namespace ProcessLimitManager.WPF.Themes
{
    public enum ThemeType
    {
        Light,
        Dark
    }

    public static class ThemeManager
    {
        public static void ApplyTheme(ThemeType theme)
        {
            var app = Application.Current;
            if (app == null) return;

            var resources = app.Resources;
            System.Diagnostics.Debug.WriteLine($"Applying {theme} theme");

            try
            {
                if (theme == ThemeType.Dark)
                {
                    UpdateResource(resources, "BackgroundBrush", "DarkPrimaryBackground");
                    UpdateResource(resources, "SecondaryBackgroundBrush", "DarkSecondaryBackground");
                    UpdateResource(resources, "BorderBrush", "DarkBorderColor");
                    UpdateResource(resources, "ForegroundBrush", "DarkTextColor");
                }
                else
                {
                    UpdateResource(resources, "BackgroundBrush", "LightPrimaryBackground");
                    UpdateResource(resources, "SecondaryBackgroundBrush", "LightSecondaryBackground");
                    UpdateResource(resources, "BorderBrush", "LightBorderColor");
                    UpdateResource(resources, "ForegroundBrush", "LightTextColor");
                }

                System.Diagnostics.Debug.WriteLine("Theme applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private static void UpdateResource(ResourceDictionary resources, string brushKey, string colorKey)
        {
            try
            {
                if (resources[colorKey] is Color color)
                {
                    var brush = new SolidColorBrush(color);
                    if (!brush.IsFrozen && brush.CanFreeze)
                    {
                        brush.Freeze();
                    }
                    resources[brushKey] = brush;
                    System.Diagnostics.Debug.WriteLine($"Updated {brushKey} with {colorKey}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Color {colorKey} not found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating {brushKey}: {ex.Message}");
            }
        }
    }
}