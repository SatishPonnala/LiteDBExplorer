using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace LiteDBExplorer.Helpers
{
    public class ThemeManager
    {
        public static event EventHandler<ElementTheme>? ThemeChanged;
        public static Window? MainWindowReference { get; set; }

        public static void SetTheme(ElementTheme theme)
        {
            if (MainWindowReference?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
            ThemeChanged?.Invoke(null, theme);
        }

        public static void ToggleTheme()
        {
            var currentTheme = GetCurrentTheme();
            var newTheme = currentTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
            SetTheme(newTheme);
        }

        public static ElementTheme GetCurrentTheme()
        {
            if (MainWindowReference?.Content is FrameworkElement element)
            {
                return element.RequestedTheme;
            }
            return ElementTheme.Default;
        }
    }
}