# -*- coding: utf-8 -*-
import os

app_xaml = """<?xml version="1.0" encoding="UTF-8" ?>
<Application
    x:Class="CalFocus.App.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:local="using:CalFocus.App"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d">

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
            </ResourceDictionary.MergedDictionaries>

            <!-- Global App Colors -->
            <Color x:Key="AppBrandColor">#0D5D56</Color>
            <Color x:Key="AppBackgroundColor">#F4F7F6</Color>
            <Color x:Key="AppCardColor">#FFFFFF</Color>
            <Color x:Key="AppTextColor">#333333</Color>
            <Color x:Key="AppMutedColor">#8FA3A0</Color>
            <Color x:Key="AppBorderColor">#E5EBE9</Color>
            <Color x:Key="AppTitleColor">#111827</Color>
            <Color x:Key="AppHoverColor">#E0EBE9</Color>

            <!-- Directly defined Brushes (Safest pattern for WinUI 3 XAML Parser) -->
            <SolidColorBrush x:Key="AppBrandBrush" Color="#0D5D56" />
            <SolidColorBrush x:Key="AppBackgroundBrush" Color="#F4F7F6" />
            <SolidColorBrush x:Key="AppCardBrush" Color="#FFFFFF" />
            <SolidColorBrush x:Key="AppTextBrush" Color="#333333" />
            <SolidColorBrush x:Key="AppMutedBrush" Color="#8FA3A0" />
            <SolidColorBrush x:Key="AppBorderBrush" Color="#E5EBE9" />
            <SolidColorBrush x:Key="AppTitleBrush" Color="#111827" />
            <SolidColorBrush x:Key="AppHoverBrush" Color="#E0EBE9" />

        </ResourceDictionary>
    </Application.Resources>
</Application>"""

with open(r"d:\Recently\cal-focus\src\CalFocus.App\App.xaml", "w", encoding="utf-8") as f:
    f.write(app_xaml.strip())

theme_cs = """using Microsoft.UI.Xaml;
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
}"""

with open(r"d:\Recently\cal-focus\src\CalFocus.App\Views\Pages\ThemePage.xaml.cs", "w", encoding="utf-8") as f:
    f.write(theme_cs.strip())

print("Fix applied successfully!")
