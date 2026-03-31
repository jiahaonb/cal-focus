# -*- coding: utf-8 -*-
with open(r"d:\Recently\cal-focus\src\CalFocus.App\Views\Pages\ThemePage.xaml", "w", encoding="utf-8") as f:
    f.write('''<?xml version="1.0" encoding="UTF-8" ?>
<Page
    x:Class="CalFocus.App.Views.Pages.ThemePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    mc:Ignorable="d"
    Background="{ThemeResource AppBackgroundBrush}">

    <ScrollViewer Padding="40" VerticalScrollBarVisibility="Auto">
        <StackPanel Spacing="32" MaxWidth="800" HorizontalAlignment="Left">
            
            <StackPanel Spacing="8">
                <TextBlock FontSize="32" FontWeight="SemiBold" Text="外观与主题" Foreground="{ThemeResource AppTitleBrush}" />
                <TextBlock FontSize="14" Foreground="{ThemeResource AppMutedBrush}" Text="选择属于您的日程界面风格色彩。" />
            </StackPanel>

            <Border Padding="32" CornerRadius="16" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}" Background="{ThemeResource AppCardBrush}">
                <StackPanel Spacing="16">
                    <TextBlock FontSize="18" FontWeight="SemiBold" Text="系统色彩" Foreground="{ThemeResource AppTitleBrush}" />
                    
                    <StackPanel Orientation="Horizontal" Spacing="16">
                        <!-- 极客绿 -->
                        <Button Background="#E5F3F1" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}" Padding="24,16" CornerRadius="12" Click="OnGreenThemeClick">
                            <StackPanel Spacing="8">
                                <Ellipse Width="32" Height="32" Fill="#0D5D56" />
                                <TextBlock Text="Titan 绿" Foreground="#0D5D56" FontWeight="Medium" HorizontalAlignment="Center" />
                            </StackPanel>
                        </Button>

                        <!-- 深海蓝 -->
                        <Button Background="#E8F0FE" BorderThickness="1" BorderBrush="{ThemeResource AppBorderBrush}" Padding="24,16" CornerRadius="12" Click="OnBlueThemeClick">
                            <StackPanel Spacing="8">
                                <Ellipse Width="32" Height="32" Fill="#1E3A8A" />
                                <TextBlock Text="Ocean 蓝" Foreground="#1E3A8A" FontWeight="Medium" HorizontalAlignment="Center" />
                            </StackPanel>
                        </Button>
                    </StackPanel>
                </StackPanel>
            </Border>

        </StackPanel>
    </ScrollViewer>
</Page>''')

with open(r"d:\Recently\cal-focus\src\CalFocus.App\Views\Pages\ThemePage.xaml.cs", "w", encoding="utf-8") as f:
    f.write('''using Microsoft.UI.Xaml;
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
        // Update Default Theme Dictionary
        if (Application.Current.Resources.ThemeDictionaries.TryGetValue("Default", out var defaultDictRaw) && defaultDictRaw is ResourceDictionary defaultDict)
        {
            defaultDict["AppBrandColor"] = HexToColor(brandColorHex);
            defaultDict["AppBackgroundColor"] = HexToColor(bgColorHex);
            
            // Force brushes to update by reassigning them
            defaultDict["AppBrandBrush"] = new SolidColorBrush(HexToColor(brandColorHex));
            defaultDict["AppBackgroundBrush"] = new SolidColorBrush(HexToColor(bgColorHex));
        }

        // To make ThemeResource pick up the changes immediately, we can just toggle the app theme back and forth or navigate
        if (Window.Current?.Content is FrameworkElement rootElement)
        {
            var currentTheme = rootElement.RequestedTheme;
            rootElement.RequestedTheme = ElementTheme.Dark;
            rootElement.RequestedTheme = ElementTheme.Light;
            rootElement.RequestedTheme = currentTheme;
        }

        // Just navigate to ourselves to visually refresh the frame
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
''')

print("ThemePage fixed.")
