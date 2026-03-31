# -*- coding: utf-8 -*-
with open(r"d:\Recently\cal-focus\src\CalFocus.App\App.xaml", 'w', encoding='utf-8') as f:
    f.write('''<?xml version="1.0" encoding="UTF-8" ?>
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

            <Color x:Key="AppBrandColor">#0D5D56</Color>
            <Color x:Key="AppBackgroundColor">#F4F7F6</Color>
            <Color x:Key="AppCardColor">#FFFFFF</Color>
            <Color x:Key="AppTextColor">#333333</Color>
            <Color x:Key="AppMutedColor">#8FA3A0</Color>
            <Color x:Key="AppBorderColor">#E5EBE9</Color>
            <Color x:Key="AppTitleColor">#111827</Color>
            <Color x:Key="AppHoverColor">#E0EBE9</Color>

            <SolidColorBrush x:Key="AppBrandBrush" Color="{StaticResource AppBrandColor}" />
            <SolidColorBrush x:Key="AppBackgroundBrush" Color="{StaticResource AppBackgroundColor}" />
            <SolidColorBrush x:Key="AppCardBrush" Color="{StaticResource AppCardColor}" />
            <SolidColorBrush x:Key="AppTextBrush" Color="{StaticResource AppTextColor}" />
            <SolidColorBrush x:Key="AppMutedBrush" Color="{StaticResource AppMutedColor}" />
            <SolidColorBrush x:Key="AppBorderBrush" Color="{StaticResource AppBorderColor}" />
            <SolidColorBrush x:Key="AppTitleBrush" Color="{StaticResource AppTitleColor}" />
            <SolidColorBrush x:Key="AppHoverBrush" Color="{StaticResource AppHoverColor}" />

        </ResourceDictionary>
    </Application.Resources>
</Application>''')
print("App resources fixed.")