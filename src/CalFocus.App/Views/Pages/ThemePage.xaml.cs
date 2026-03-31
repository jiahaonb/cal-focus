using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Windows.UI;

namespace CalFocus.App.Views.Pages;

public sealed partial class ThemePage : Page
{
    public ThemePage()
    {
        InitializeComponent();
    }

    private void OnGreenThemeClick(object sender, RoutedEventArgs e)
    {
        ApplyThemeColor("#0D5D56", "#F4F7F6");
    }

    private void OnBlueThemeClick(object sender, RoutedEventArgs e)
    {
        ApplyThemeColor("#1E3A8A", "#F3F6FB");
    }

    private void ApplyThemeColor(string brandColorHex, string bgColorHex)
    {
        // Just update Application.Current.Resources root directly
        Application.Current.Resources["AppBrandColor"] = HexToColor(brandColorHex);
        Application.Current.Resources["AppBackgroundColor"] = HexToColor(bgColorHex);
        
        Application.Current.Resources["AppBrandBrush"] = new SolidColorBrush(HexToColor(brandColorHex));
        Application.Current.Resources["AppBackgroundBrush"] = new SolidColorBrush(HexToColor(bgColorHex));

        // Reload frame to visually refresh the UI
        if (this.Frame != null)
        {
            var type = this.GetType();
            this.Frame.Navigate(type);
        }
    }

    private static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte a = 255;
        byte r = 255, g = 255, b = 255;

        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        else if (hex.Length == 6)
        {
            r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }

        return Color.FromArgb(a, r, g, b);
    }
}